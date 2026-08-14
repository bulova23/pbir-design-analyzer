using System.Text.Json;

namespace PowerBIModelingService.RpcHost;

internal enum RpcHandlerResultKind
{
    Success,
    Error,
    NoResponse,
}

internal sealed record RpcHandlerResult(
    RpcHandlerResultKind Kind,
    object? Result = null,
    int ErrorCode = 0,
    string ErrorMessage = "")
{
    internal static RpcHandlerResult Success(object? result) =>
        new(RpcHandlerResultKind.Success, Result: result);

    internal static RpcHandlerResult Error(int code, string message) =>
        new(RpcHandlerResultKind.Error, ErrorCode: code, ErrorMessage: message);

    internal static RpcHandlerResult NoResponse() => new(RpcHandlerResultKind.NoResponse);
}

internal interface IRpcRequestHandler : IAsyncDisposable
{
    Task<RpcHandlerResult> HandleAsync(
        ParsedJsonRpcRequest request,
        CancellationToken cancellationToken);
}

internal sealed class AnalyzerRpcDispatcher : IRpcRequestHandler
{
    internal static readonly IReadOnlySet<string> KnownMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        "initialize",
        "initialized",
        "textDocument/didOpen",
        "textDocument/didChange",
        "textDocument/didClose",
        "workspace/didChangeWatchedFiles",
        "workspace/didChangeConfiguration",
        "$/setTrace",
        "$/logTrace",
        "model/ping",
        "model/pbir/getTree",
        "model/pbir/scoreReport",
        "model/pbir/governanceCheck",
        PbirMaterializationRpcContract.PreviewOperation,
        PbirMaterializationRpcContract.ApplyOperation,
        PbirMaterializationRpcContract.RecoveryOperation,
        PbirAuthoringRpcHostContract.Operation,
    };

    private readonly AnalyzerServices _services;
    private readonly PbirMaterializationRpcAdapter _materializationAdapter;
    private readonly PbirAuthoringRpcAdapter _authoringAdapter;
    private int _disposed;

    internal AnalyzerRpcDispatcher(
        AnalyzerServices services,
        PbirMaterializationRpcAdapter? materializationAdapter = null,
        PbirAuthoringRpcAdapter? authoringAdapter = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _materializationAdapter = materializationAdapter ?? new PbirMaterializationRpcAdapter(
            new PowerBIModelingService.Services.Discovery.PbirMaterializationOrchestrationService());
        _authoringAdapter = authoringAdapter ?? new PbirAuthoringRpcAdapter();
    }

    public async Task<RpcHandlerResult> HandleAsync(
        ParsedJsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        cancellationToken.ThrowIfCancellationRequested();

        var result = request.Method switch
        {
            "initialize" => RpcHandlerResult.Success(new
            {
                capabilities = new { },
                serverInfo = new { name = "PBIR Design Analyzer Backend", version = Program.BackendVersion },
            }),
            "initialized" or
            "textDocument/didOpen" or
            "textDocument/didChange" or
            "textDocument/didClose" or
            "workspace/didChangeWatchedFiles" or
            "workspace/didChangeConfiguration" or
            "$/setTrace" or
            "$/logTrace" => RpcHandlerResult.NoResponse(),
            "model/ping" => RpcHandlerResult.Success(Success(new
            {
                status = "ready",
                connected = false,
                version = Program.BackendVersion,
            })),
            "model/pbir/getTree" => RpcHandlerResult.Success(await HandleGetTreeAsync(request.Params, cancellationToken)
                .ConfigureAwait(false)),
            "model/pbir/scoreReport" => RpcHandlerResult.Success(await HandleScoreReportAsync(request.Params, cancellationToken)
                .ConfigureAwait(false)),
            "model/pbir/governanceCheck" => RpcHandlerResult.Success(await HandleGovernanceCheckAsync(request.Params, cancellationToken)
                .ConfigureAwait(false)),
            PbirMaterializationRpcContract.PreviewOperation or
            PbirMaterializationRpcContract.ApplyOperation or
            PbirMaterializationRpcContract.RecoveryOperation => RpcHandlerResult.Success(
                await HandleMaterializationAsync(request.Method, request.Params, request.ParamsUtf8, cancellationToken).ConfigureAwait(false)),
            PbirAuthoringRpcHostContract.Operation => RpcHandlerResult.Success(
                await _authoringAdapter.HandleAsync(request.Params, request.ParamsUtf8, cancellationToken).ConfigureAwait(false)),
            _ => RpcHandlerResult.Error(-32601, "Method not found."),
        };

        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    private Task<object> HandleMaterializationAsync(
        string operation,
        JsonElement? parameters,
        ReadOnlyMemory<byte>? parametersUtf8,
        CancellationToken cancellationToken)
    {
        if (!parameters.HasValue ||
            (parametersUtf8.HasValue && parametersUtf8.Value.Length > PbirMaterializationRpcValidation.MaxRequestPayloadBytes))
        {
            return Task.FromResult<object>(PbirMaterializationRpcValidation.Invalid(
                string.Empty, operation, "PBIR-RPC-REQUEST-001", "request"));
        }

        return HandleMaterializationCoreAsync(operation, parameters.Value, cancellationToken);
    }

    private async Task<object> HandleMaterializationCoreAsync(
        string operation,
        JsonElement parameters,
        CancellationToken cancellationToken) =>
        await _materializationAdapter.HandleAsync(operation, parameters, cancellationToken).ConfigureAwait(false);

    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _disposed, 1);
        return ValueTask.CompletedTask;
    }

    private Task<object> HandleGetTreeAsync(JsonElement? parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reportPath = ReadString(parameters, "reportPath");
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
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<object>(Success(tree));
    }

    private async Task<object> HandleScoreReportAsync(
        JsonElement? parameters,
        CancellationToken cancellationToken)
    {
        var reportPath = ReadString(parameters, "reportPath");
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            return Failure("Parameter 'reportPath' is required.");
        }

        var pageName = ReadString(parameters, "pageName");
        var config = ReadProperty(parameters, "config");
        cancellationToken.ThrowIfCancellationRequested();

        var score = await _services.ScoringService
            .ScoreAsync(reportPath, config, pageName)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return Success(score);
    }

    private async Task<object> HandleGovernanceCheckAsync(
        JsonElement? parameters,
        CancellationToken cancellationToken)
    {
        var reportPath = ReadString(parameters, "reportPath");
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            return Failure("Parameter 'reportPath' is required.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var location = _services.ProjectService.TryGetReportLocation(reportPath);
        if (location is null)
        {
            return Failure($"No PBIR report definition found at '{reportPath}'. Ensure a .Report folder exists.");
        }

        var themeId = ReadString(parameters, "themeId");
        var policy = _services.GovernanceService.ReadPolicy(location.WorkspaceRootPath);
        var score = await _services.ScoringService.ScoreAsync(reportPath).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var result = _services.GovernanceService.Evaluate(policy, score, themeId);

        return Success(result);
    }

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

    private static string? ReadString(JsonElement? parameters, string propertyName)
    {
        var property = ReadProperty(parameters, propertyName);
        return property?.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
    }

    private static JsonElement? ReadProperty(JsonElement? parameters, string propertyName)
    {
        if (!parameters.HasValue || parameters.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return parameters.Value.TryGetProperty(propertyName, out var property)
            ? property.Clone()
            : null;
    }
}

internal static class RpcHostRouteInventory
{
    internal static readonly IReadOnlySet<string> KnownMethods =
        new HashSet<string>(AnalyzerRpcDispatcher.KnownMethods, StringComparer.Ordinal)
        {
            "$/cancelRequest",
            "shutdown",
            "exit",
        };
}
