using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using Serilog;
using Serilog.Events;

namespace PowerBIModelingService.LspHost;

/// <summary>
/// Minimal PBIR analyzer host.
/// Keeps the LSP/JSON-RPC transport shape for v1 while exposing only the
/// analyzer workflows that remain in product scope.
/// </summary>
public static class Program
{
    public static async Task<int> Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                standardErrorFromLevel: LogEventLevel.Verbose)
            .CreateLogger();

        try
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
            var services = BuildServices(loggerFactory);
            var server = new SimpleJsonRpcServer(
                services,
                Console.In,
                Console.Out,
                loggerFactory.CreateLogger<SimpleJsonRpcServer>());

            await server.RunAsync().ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "[PBIR Host] Fatal error");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }
    }

    private static AnalyzerServices BuildServices(ILoggerFactory loggerFactory)
    {
        var projectService = new PbirProjectService(loggerFactory.CreateLogger<PbirProjectService>());
        var treeBuilder = new PbirTreeBuilder(loggerFactory.CreateLogger<PbirTreeBuilder>());
        var scoringService = new PbirScoringService(
            projectService,
            loggerFactory.CreateLogger<PbirScoringService>());
        var governanceService = new PbirGovernanceService(
            loggerFactory.CreateLogger<PbirGovernanceService>());

        return new AnalyzerServices(projectService, treeBuilder, scoringService, governanceService);
    }
}

internal sealed record AnalyzerServices(
    PbirProjectService ProjectService,
    PbirTreeBuilder TreeBuilder,
    PbirScoringService ScoringService,
    PbirGovernanceService GovernanceService);

internal sealed class SimpleJsonRpcServer
{
    private readonly AnalyzerServices _services;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly ILogger<SimpleJsonRpcServer> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = CreateJsonSerializerOptions();

    private bool _isRunning = true;

    public SimpleJsonRpcServer(
        AnalyzerServices services,
        TextReader input,
        TextWriter output,
        ILogger<SimpleJsonRpcServer> logger)
    {
        _services = services;
        _input = input;
        _output = output;
        _logger = logger;
    }

    internal static JsonSerializerOptions CreateJsonSerializerOptions() => new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task RunAsync()
    {
        _logger.LogInformation("[PBIR Host] Ready for requests");

        while (_isRunning)
        {
            JsonRpcRequest? request;

            try
            {
                request = await ReadRequestAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PBIR Host] Failed to read request");
                continue;
            }

            if (request is null)
            {
                _logger.LogInformation("[PBIR Host] Input stream closed");
                _isRunning = false;
                break;
            }

            try
            {
                await HandleRequestAsync(request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PBIR Host] Unhandled request error for {Method}", request.Method);
                if (ShouldReply(request.Id))
                {
                    await WriteErrorAsync(request.Id, -32603, ex.Message).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task<JsonRpcRequest?> ReadRequestAsync()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? line;

        while ((line = await _input.ReadLineAsync().ConfigureAwait(false)) != null && !string.IsNullOrEmpty(line))
        {
            var parts = line.Split(':', 2);
            if (parts.Length == 2)
            {
                headers[parts[0].Trim()] = parts[1].Trim();
            }
        }

        if (line is null)
        {
            return null;
        }

        if (!headers.TryGetValue("Content-Length", out var lengthValue) ||
            !int.TryParse(lengthValue, out var contentLength) ||
            contentLength <= 0)
        {
            return null;
        }

        var buffer = new char[contentLength];
        var offset = 0;

        while (offset < contentLength)
        {
            var read = await _input.ReadBlockAsync(buffer, offset, contentLength - offset).ConfigureAwait(false);
            if (read <= 0)
            {
                break;
            }

            offset += read;
        }

        if (offset != contentLength)
        {
            throw new InvalidOperationException($"Expected {contentLength} request bytes but read {offset}.");
        }

        var content = new string(buffer);
        return JsonSerializer.Deserialize<JsonRpcRequest>(content, _jsonOptions);
    }

    private async Task HandleRequestAsync(JsonRpcRequest request)
    {
        switch (request.Method)
        {
            case "initialize":
                await WriteResultAsync(request.Id, new
                {
                    capabilities = new { },
                    serverInfo = new { name = "PBIR Design Analyzer Backend", version = "0.1.11" },
                }).ConfigureAwait(false);
                return;

            case "initialized":
            case "textDocument/didOpen":
            case "textDocument/didChange":
            case "textDocument/didClose":
            case "workspace/didChangeWatchedFiles":
            case "workspace/didChangeConfiguration":
            case "$/setTrace":
            case "$/logTrace":
                return;

            case "shutdown":
                _isRunning = false;
                await WriteResultAsync(request.Id, result: null).ConfigureAwait(false);
                return;

            case "exit":
                _isRunning = false;
                return;

            case "model/ping":
                await WriteResultAsync(request.Id, Success(new
                {
                    status = "ready",
                    connected = false,
                })).ConfigureAwait(false);
                return;

            case "model/pbir/getTree":
                await WriteResultAsync(request.Id, await HandleGetTreeAsync(request.Params).ConfigureAwait(false)).ConfigureAwait(false);
                return;

            case "model/pbir/scoreReport":
                await WriteResultAsync(request.Id, await HandleScoreReportAsync(request.Params).ConfigureAwait(false)).ConfigureAwait(false);
                return;

            case "model/pbir/governanceCheck":
                await WriteResultAsync(request.Id, await HandleGovernanceCheckAsync(request.Params).ConfigureAwait(false)).ConfigureAwait(false);
                return;

            default:
                if (ShouldReply(request.Id))
                {
                    await WriteErrorAsync(request.Id, -32601, $"Method not found: {request.Method}").ConfigureAwait(false);
                }
                return;
        }
    }

    private Task<object> HandleGetTreeAsync(JsonElement? @params)
    {
        var reportPath = ReadString(@params, "reportPath");
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            return Task.FromResult<object>(Failure("Parameter 'reportPath' is required."));
        }

        var location = _services.ProjectService.TryGetReportLocation(reportPath);
        if (location is null)
        {
            return Task.FromResult<object>(
                Failure($"No PBIR report definition found at '{reportPath}'. Ensure a .Report folder exists."));
        }

        var tree = _services.TreeBuilder.BuildTree(location);
        return Task.FromResult<object>(Success(tree));
    }

    private async Task<object> HandleScoreReportAsync(JsonElement? @params)
    {
        var reportPath = ReadString(@params, "reportPath");
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            return Failure("Parameter 'reportPath' is required.");
        }

        var pageName = ReadString(@params, "pageName");
        var config = ReadProperty(@params, "config");

        var score = await _services.ScoringService
            .ScoreAsync(reportPath, config, pageName)
            .ConfigureAwait(false);

        return Success(score);
    }

    private async Task<object> HandleGovernanceCheckAsync(JsonElement? @params)
    {
        var reportPath = ReadString(@params, "reportPath");
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            return Failure("Parameter 'reportPath' is required.");
        }

        var location = _services.ProjectService.TryGetReportLocation(reportPath);
        if (location is null)
        {
            return Failure($"No PBIR report definition found at '{reportPath}'. Ensure a .Report folder exists.");
        }

        var themeId = ReadString(@params, "themeId");
        var policy = _services.GovernanceService.ReadPolicy(location.WorkspaceRootPath);
        var score = await _services.ScoringService.ScoreAsync(reportPath).ConfigureAwait(false);
        var result = _services.GovernanceService.Evaluate(policy, score, themeId);

        return Success(result);
    }

    private async Task WriteResultAsync(JsonElement? id, object? result)
    {
        if (!ShouldReply(id))
        {
            return;
        }

        await WriteMessageAsync(new
        {
            jsonrpc = "2.0",
            id = id,
            result,
        }).ConfigureAwait(false);
    }

    private async Task WriteErrorAsync(JsonElement? id, int code, string message)
    {
        await WriteMessageAsync(new
        {
            jsonrpc = "2.0",
            id = id,
            error = new
            {
                code,
                message,
            },
        }).ConfigureAwait(false);
    }

    private async Task WriteMessageAsync(object payload)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var contentLength = Encoding.UTF8.GetByteCount(json);

        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await _output.WriteAsync($"Content-Length: {contentLength}\r\n\r\n").ConfigureAwait(false);
            await _output.WriteAsync(json.AsMemory()).ConfigureAwait(false);
            await _output.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static bool ShouldReply(JsonElement? id) =>
        id.HasValue &&
        id.Value.ValueKind is not JsonValueKind.Null &&
        id.Value.ValueKind is not JsonValueKind.Undefined;

    private static object Success(object? data) => new Dictionary<string, object?>
    {
        ["success"] = true,
        ["data"] = data,
    };

    private static object Failure(string error) => new Dictionary<string, object?>
    {
        ["success"] = false,
        ["error"] = error,
    };

    private static string? ReadString(JsonElement? @params, string propertyName)
    {
        var property = ReadProperty(@params, propertyName);
        return property?.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
    }

    private static List<string> ReadStringArray(JsonElement? @params, string propertyName)
    {
        var property = ReadProperty(@params, propertyName);
        if (property is null || property.Value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property.Value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToList();
    }

    private static JsonElement? ReadProperty(JsonElement? @params, string propertyName)
    {
        if (!@params.HasValue || @params.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return @params.Value.TryGetProperty(propertyName, out var property)
            ? property.Clone()
            : null;
    }
}

internal sealed class JsonRpcRequest
{
    public string Jsonrpc { get; set; } = "2.0";

    public JsonElement? Id { get; set; }

    public string Method { get; set; } = string.Empty;

    public JsonElement? Params { get; set; }
}
