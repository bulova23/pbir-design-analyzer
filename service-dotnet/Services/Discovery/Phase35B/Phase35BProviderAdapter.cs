namespace PowerBIModelingService.Services.Discovery;

internal interface IPhase35BProviderAdapter
{
    string ProviderId { get; }
    string AdapterVersion { get; }
    IReadOnlyList<Phase35ACapability> Capabilities { get; }
    Phase35BAdapterValidation ValidateRequest(Phase35ARequest request);
    Phase35AReadinessResult DeclareReadiness(Phase35ARequest request);
    Phase35BExecutionPlan DescribeExecutionPlan(Phase35ARequest request);
    Task<Phase35BOfflineExecutionResult> ExecuteOfflineAsync(Phase35BExecutionContext context, CancellationToken cancellationToken);
}
