extern alias RpcHost;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using AnalyzerRpcDispatcher = RpcHost::PowerBIModelingService.RpcHost.AnalyzerRpcDispatcher;
using AnalyzerServices = RpcHost::PowerBIModelingService.RpcHost.AnalyzerServices;
using PbirAuthoringRpcAdapter = RpcHost::PowerBIModelingService.RpcHost.PbirAuthoringRpcAdapter;
using PbirAuthoringRpcHostContract = RpcHost::PowerBIModelingService.RpcHost.PbirAuthoringRpcHostContract;
using PbirGovernanceService = PowerBIModelingService.Services.Pbir.PbirGovernanceService;
using PbirProjectService = PowerBIModelingService.Services.PbirProjectService;
using PbirScoringService = PowerBIModelingService.Services.Pbir.PbirScoringService;
using PbirTreeBuilder = PowerBIModelingService.Services.PbirTreeBuilder;

namespace ServiceDotnet.Tests;

public sealed class PbirAuthoringRpcAdapterTests
{
    [Fact]
    public async Task Adapter_RejectsMissingParamsBeforeOperationDispatch()
    {
        var response = await new PbirAuthoringRpcAdapter().HandleAsync(null, null, CancellationToken.None);

        Assert.Equal("invalidRequest", response.GetProperty("error").GetProperty("category").GetString());
        Assert.Equal("PBIR-RPC-REQUEST-001", response.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("The authoring request must be a bounded JSON object.", response.GetProperty("error").GetProperty("summary").GetString());
    }

    [Fact]
    public async Task Adapter_RejectsMutationAndValidateBeforeCoreDispatch()
    {
        var adapter = new PbirAuthoringRpcAdapter();

        var mutation = await adapter.HandleAsync(JsonDocument.Parse("{\"operation\":\"mutate\"}").RootElement, null, CancellationToken.None);
        var validation = await adapter.HandleAsync(JsonDocument.Parse("{\"operation\":\"validate\"}").RootElement, null, CancellationToken.None);

        Assert.Equal("unsupportedAuthoring", mutation.GetProperty("error").GetProperty("category").GetString());
        Assert.Equal("invalidRequest", validation.GetProperty("error").GetProperty("category").GetString());
    }

    [Fact]
    public async Task Adapter_RejectsPublicMutationKindsOutsideCuratedCatalog()
    {
        var request = JsonDocument.Parse("""
            {
              "schemaVersion":"pbir-authoring-rpc/v1",
              "operation":"mutate",
              "mutate":{
                "mode":"preview",
                "snapshot":{"schemaVersion":"pbir-authoring-rpc-snapshot/v1","snapshotId":"snapshot","sourceIdentity":{"sourceDirectoryName":"report","contentHash":"hash","fileCount":0}},
                "request":{"schemaVersion":"local-pbir-mutation-request/v1","mutationId":"mutation","sourceDirectory":"","outputBaseDirectory":"","targetDirectoryName":"","operations":[{"kind":"addVisual","target":{"pageId":"page"}}]}
              }
            }
            """).RootElement;

        var response = await new PbirAuthoringRpcAdapter().HandleAsync(request, null, CancellationToken.None);

        Assert.Equal("unsupportedAuthoring", response.GetProperty("error").GetProperty("category").GetString());
        Assert.Equal("PBIR-RPC-MUTATE-008", response.GetProperty("error").GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("renamePage")]
    [InlineData("addPage")]
    [InlineData("removePage")]
    [InlineData("movePage")]
    [InlineData("moveVisual")]
    [InlineData("resizeVisual")]
    public async Task Adapter_AllowsEachCuratedMutationKindToReachCore(string kind)
    {
        var request = JsonDocument.Parse("""
            {
              "schemaVersion":"pbir-authoring-rpc/v1",
              "operation":"mutate",
              "mutate":{
                "mode":"preview",
                "snapshot":{"schemaVersion":"pbir-authoring-rpc-snapshot/v1","snapshotId":"missing","sourceIdentity":{"sourceDirectoryName":"report","contentHash":"hash","fileCount":0}},
                "request":{"schemaVersion":"local-pbir-mutation-request/v1","mutationId":"mutation","sourceDirectory":"","outputBaseDirectory":"","targetDirectoryName":"","operations":[{"kind":"REPLACE_KIND","target":{"pageId":"page","visualId":"visual"}}]}
              }
            }
            """).RootElement.Clone();
        using var normalized = JsonDocument.Parse(request.GetRawText().Replace("REPLACE_KIND", kind, StringComparison.Ordinal));

        var response = await new PbirAuthoringRpcAdapter().HandleAsync(normalized.RootElement, null, CancellationToken.None);

        Assert.NotEqual("PBIR-RPC-MUTATE-008", response.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Adapter_RejectsPublicRequestsContainingMoreThanOneOperation()
    {
        var request = JsonDocument.Parse("""
            {
              "schemaVersion":"pbir-authoring-rpc/v1",
              "operation":"mutate",
              "mutate":{
                "mode":"preview",
                "snapshot":{"schemaVersion":"pbir-authoring-rpc-snapshot/v1","snapshotId":"snapshot","sourceIdentity":{"sourceDirectoryName":"report","contentHash":"hash","fileCount":0}},
                "request":{"schemaVersion":"local-pbir-mutation-request/v1","mutationId":"mutation","sourceDirectory":"","outputBaseDirectory":"","targetDirectoryName":"","operations":[{"kind":"renamePage","target":{"pageId":"page"},"displayName":"One"},{"kind":"renamePage","target":{"pageId":"page"},"displayName":"Two"}]}
              }
            }
            """).RootElement;

        var response = await new PbirAuthoringRpcAdapter().HandleAsync(request, null, CancellationToken.None);

        Assert.Equal("unsupportedAuthoring", response.GetProperty("error").GetProperty("category").GetString());
        Assert.Equal("PBIR-RPC-MUTATE-009", response.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Adapter_DeserializesTypedGenerateAndPreservesStructuredResponseFields()
    {
        var output = Directory.CreateTempSubdirectory("pbir-rpc-adapter-");
        try
        {
            var request = JsonSerializer.SerializeToDocument(new
            {
                schemaVersion = "pbir-authoring-rpc/v1",
                operation = "generate",
                generate = new
                {
                    request = new
                    {
                        v1 = new
                        {
                            schemaVersion = "local-pbir-generation-request/v1",
                            requestId = "adapter",
                            reportName = "Sales",
                            pageId = "overview",
                            pageDisplayName = "Overview",
                            visualId = "card",
                            visualType = "card",
                            datasetPath = "Sales.SemanticModel",
                            measureToken = "Revenue",
                            measureEntity = "Sales",
                            measureProperty = "Revenue",
                            generatedUtc = "2026-08-14T00:00:00Z",
                            outputBaseDirectory = output.FullName,
                            targetDirectoryName = "report",
                        },
                    },
                },
            }).RootElement;
            var response = await new PbirAuthoringRpcAdapter().HandleAsync(request, null, CancellationToken.None);

            Assert.True(response.GetProperty("succeeded").GetBoolean());
            Assert.True(response.GetProperty("artifactIdentity").GetProperty("artifactHash").GetString()!.Length > 0);
            Assert.True(response.GetProperty("timing").GetProperty("dispatchMilliseconds").GetInt64() >= 0);
        }
        finally
        {
            output.Delete(recursive: true);
        }
    }

    [Fact]
    public void HostRouteIsOneSharedAuthoringMethod()
    {
        Assert.Equal("pbir/authoring", PbirAuthoringRpcHostContract.Operation);
        Assert.Contains(PbirAuthoringRpcHostContract.Operation, AnalyzerRpcDispatcher.KnownMethods);
    }
}
