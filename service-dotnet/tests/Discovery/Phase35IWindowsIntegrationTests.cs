using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35IWindowsIntegrationTests
{
    [Fact(Skip = "NotApplicable: Phase35I Windows integration requires a real Windows worker."), Trait("Category", "WindowsIntegration")]
    public void SuccessfulInertLaunchUsesWindowsContainment() => RequireWindows();

    [Fact(Skip = "NotApplicable: Phase35I Windows integration requires a real Windows worker."), Trait("Category", "WindowsIntegration")]
    public void SuspendedLaunchAssignsBeforeResume() => RequireWindows();

    [Fact(Skip = "NotApplicable: Phase35I Windows integration requires a real Windows worker."), Trait("Category", "WindowsIntegration")]
    public void JobObjectLimitsAndNoBreakawayAreObserved() => RequireWindows();

    [Fact(Skip = "NotApplicable: Phase35I Windows integration requires a real Windows worker."), Trait("Category", "WindowsIntegration")]
    public void ChildAndNestedChildRemainOwnedByTheJob() => RequireWindows();

    [Fact(Skip = "NotApplicable: Phase35I Windows integration requires a real Windows worker."), Trait("Category", "WindowsIntegration")]
    public void TimeoutTerminatesTheOwnedProcessTree() => RequireWindows();

    [Fact(Skip = "NotApplicable: Phase35I Windows integration requires a real Windows worker."), Trait("Category", "WindowsIntegration")]
    public void CancellationTerminatesOnlyTheOwnedJob() => RequireWindows();

    [Fact(Skip = "NotApplicable: Phase35I Windows integration requires a real Windows worker."), Trait("Category", "WindowsIntegration")]
    public void ExplicitEnvironmentExcludesSyntheticParentSecrets() => RequireWindows();

    [Fact(Skip = "NotApplicable: Phase35I Windows integration requires a real Windows worker."), Trait("Category", "WindowsIntegration")]
    public void RestrictedTokenDeniesTestOwnedAclTarget() => RequireWindows();

    [Fact(Skip = "NotApplicable: Phase35I Windows integration requires a real Windows worker."), Trait("Category", "WindowsIntegration")]
    public void CleanupAndKillOnCloseLeaveNoOwnedProcess() => RequireWindows();

    [Fact(Skip = "NotApplicable: Phase35I Windows integration requires a real Windows worker."), Trait("Category", "WindowsIntegration")]
    public void NativeFailuresMapToClosedFailureTaxonomy() => RequireWindows();

    private static void RequireWindows() { }
}
