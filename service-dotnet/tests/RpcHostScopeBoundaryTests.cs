extern alias RpcHost;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using AnalyzerRpcDispatcher = RpcHost::PowerBIModelingService.RpcHost.AnalyzerRpcDispatcher;
using AnalyzerServices = RpcHost::PowerBIModelingService.RpcHost.AnalyzerServices;
using JsonRpcRequestParser = RpcHost::PowerBIModelingService.RpcHost.JsonRpcRequestParser;
using PbirGovernanceService = PowerBIModelingService.Services.Pbir.PbirGovernanceService;
using PbirProjectService = PowerBIModelingService.Services.PbirProjectService;
using PbirScoringService = PowerBIModelingService.Services.Pbir.PbirScoringService;
using PbirTreeBuilder = PowerBIModelingService.Services.PbirTreeBuilder;
using RpcHandlerResultKind = RpcHost::PowerBIModelingService.RpcHost.RpcHandlerResultKind;
using RpcHostRouteInventory = RpcHost::PowerBIModelingService.RpcHost.RpcHostRouteInventory;
using RpcTransportOptions = RpcHost::PowerBIModelingService.RpcHost.RpcTransportOptions;

namespace ServiceDotnet.Tests;

public sealed class RpcHostScopeBoundaryTests
{
    private static readonly string[] ExpectedMethods =
    [
        "$/cancelRequest",
        "$/logTrace",
        "$/setTrace",
        "exit",
        "initialize",
        "initialized",
        "model/pbir/getTree",
        "model/pbir/governanceCheck",
        "model/pbir/scoreReport",
        "model/ping",
        "pbir/authoring",
        "pbir/materialization/apply",
        "pbir/materialization/preview",
        "pbir/materialization/recovery/inspect",
        "shutdown",
        "textDocument/didChange",
        "textDocument/didClose",
        "textDocument/didOpen",
        "workspace/didChangeConfiguration",
        "workspace/didChangeWatchedFiles",
    ];

    [Fact]
    public void HostRouteInventory_AddsOnlyStandardCancellationToExistingMethods()
    {
        Assert.Equal(ExpectedMethods, RpcHostRouteInventory.KnownMethods.OrderBy(method => method));
    }

    [Fact]
    public async Task Dispatcher_PreservesInitializePingNotificationAndUnknownMethodContracts()
    {
        await using var dispatcher = CreateDispatcher();

        var initialize = await dispatcher.HandleAsync(Parse("initialize"), CancellationToken.None);
        var ping = await dispatcher.HandleAsync(Parse("model/ping"), CancellationToken.None);
        var notification = await dispatcher.HandleAsync(Parse("initialized", includeId: false), CancellationToken.None);
        var unknown = await dispatcher.HandleAsync(Parse("future/unknown"), CancellationToken.None);

        Assert.Equal(RpcHandlerResultKind.Success, initialize.Kind);
        Assert.Contains("PBIR Design Analyzer Backend", JsonSerializer.Serialize(initialize.Result));
        Assert.Equal(RpcHandlerResultKind.Success, ping.Kind);
        Assert.Contains("\"status\":\"ready\"", JsonSerializer.Serialize(ping.Result));
        Assert.Equal(RpcHandlerResultKind.NoResponse, notification.Kind);
        Assert.Equal(RpcHandlerResultKind.Error, unknown.Kind);
        Assert.Equal(-32601, unknown.ErrorCode);
        Assert.Equal("Method not found.", unknown.ErrorMessage);
    }

    [Theory]
    [InlineData("model/pbir/getTree")]
    [InlineData("model/pbir/scoreReport")]
    [InlineData("model/pbir/governanceCheck")]
    public async Task Dispatcher_PreservesExistingInvalidParameterFailureEnvelope(string method)
    {
        await using var dispatcher = CreateDispatcher();

        var response = await dispatcher.HandleAsync(Parse(method), CancellationToken.None);
        var json = JsonSerializer.Serialize(response.Result);

        Assert.Equal(RpcHandlerResultKind.Success, response.Kind);
        Assert.Contains("\"success\":false", json);
        Assert.Contains("reportPath", json);
    }

    [Fact]
    public void GenericTransportFiles_HaveNoApplicationAdapterProviderSkillsOrUiAuthority()
    {
        var rpcHostDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "RpcHost"));
        var genericFiles = new[]
        {
            "RpcTransportOptions.cs",
            "JsonRpcProtocol.cs",
            "JsonRpcFraming.cs",
            "RpcResponseWriter.cs",
            "RpcRequestRegistry.cs",
            "SimpleJsonRpcServer.cs",
        };
        var forbidden = new[]
        {
            "PbirMaterializationOrchestrationService",
            "PbirDeployable",
            "Phase31",
            "RuntimeProvider",
            "MicrosoftSkill",
            "Process.Start",
            "HttpClient",
            "vscode-extension",
            "VS Code",
            "webview",
            "deployment",
            "publishing",
        };

        foreach (var file in genericFiles)
        {
            var path = Path.Combine(rpcHostDirectory, file);
            Assert.True(File.Exists(path), $"Missing generic transport source: {path}");
            var source = File.ReadAllText(path);
            foreach (var term in forbidden)
            {
                Assert.DoesNotContain(term, source, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static AnalyzerRpcDispatcher CreateDispatcher()
    {
        var projectService = new PbirProjectService(NullLogger<PbirProjectService>.Instance);
        var services = new AnalyzerServices(
            projectService,
            new PbirTreeBuilder(NullLogger<PbirTreeBuilder>.Instance),
            new PbirScoringService(projectService, NullLogger<PbirScoringService>.Instance),
            new PbirGovernanceService(NullLogger<PbirGovernanceService>.Instance));
        return new AnalyzerRpcDispatcher(services);
    }

    private static RpcHost::PowerBIModelingService.RpcHost.ParsedJsonRpcRequest Parse(
        string method,
        bool includeId = true)
    {
        var id = includeId ? "\"id\":1," : string.Empty;
        var json = $"{{\"jsonrpc\":\"2.0\",{id}\"method\":{JsonSerializer.Serialize(method)},\"params\":{{}}}}";
        return JsonRpcRequestParser.Parse(
            Encoding.UTF8.GetBytes(json),
            RpcTransportOptions.Production).Request!;
    }
}
