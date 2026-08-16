namespace PowerBIModelingService.Services.Discovery;

internal enum Phase35GContainmentType
{
    LocalMacOSProcess,
    LocalVirtualized,
    RemoteControlled
}

internal enum Phase35GDecisionStatus
{
    SelectedNotEnabled,
    NotSelected,
    Disqualified
}

internal sealed record Phase35GContainmentArchitectureDecision(
    string ContractVersion,
    Phase35GContainmentType SelectedContainment,
    Phase35GDecisionStatus Status,
    bool ProviderExecutionEnabled,
    string Rationale,
    IReadOnlyList<string> RequiredPrerequisites);

internal static class Phase35GContainmentArchitectureDecisionRecord
{
    internal const string ContractVersion = "phase35g-containment-architecture/v1";

    internal static Phase35GContainmentArchitectureDecision Current => new(
        ContractVersion,
        Phase35GContainmentType.RemoteControlled,
        Phase35GDecisionStatus.SelectedNotEnabled,
        false,
        "Controlled remote execution is the only compared boundary that can host a future Windows-dependent provider while keeping local macOS admission closed.",
        [
            "authenticated private execution API with independent remote authorization",
            "Windows worker isolation and exact certification/policy binding",
            "independently tamper-evident correlated audit and replay protection",
            "defense-in-depth remote and local artifact scanning with controlled retrieval",
            "scoped short-lived credential grants and deterministic cancellation/recovery",
            "provider-specific conformance and artifact validation"
        ]);
}
