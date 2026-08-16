using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

/// <summary>
/// Backend-only mutation boundary. Serialization/materialization is intentionally not hidden here;
/// callers must pass the validated IR to the existing serializer/orchestration services.
/// </summary>
internal sealed class LocalPbirMutationProviderService
{
    private readonly PbirLocalReportReader _reader;
    private readonly PbirMutationPlanner _planner;
    private readonly PbirMutationExecutor _executor;

    internal LocalPbirMutationProviderService()
        : this(new PbirLocalReportReader(), new PbirMutationPlanner(), new PbirMutationExecutor())
    {
    }

    internal LocalPbirMutationProviderService(
        PbirLocalReportReader reader,
        PbirMutationPlanner planner,
        PbirMutationExecutor executor)
    {
        _reader = reader;
        _planner = planner;
        _executor = executor;
    }

    internal PbirLocalReportImportSnapshot Import(string sourceDirectory) => _reader.Import(sourceDirectory);

    internal PbirMutationPlan Plan(PbirLocalReportImportSnapshot snapshot, LocalPbirMutationRequest request) => _planner.Plan(snapshot, request);

    internal PbirMutationExecutionResult Execute(PbirMutationPlan plan) => _executor.Execute(plan);
}
