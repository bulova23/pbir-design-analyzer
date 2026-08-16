using PowerBIModelingService.Services.Discovery;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35FContainmentTests
{
    [Fact]
    public void CurrentMacOsSelectionFailsClosedWithPerControlEvidence()
    {
        var decision = new Phase35FContainmentSelector().Evaluate();

        Assert.False(decision.IsAdmitted);
        Assert.Equal(Phase35FMechanism.NoneLocalMacOs, decision.SelectedMechanism);
        Assert.Equal(Phase35FFailureCode.PlatformContainmentUnavailable, decision.Failure);
        Assert.Contains(decision.Capabilities, item => item.Name == "filesystem-read" && item.State == Phase35FControlState.Unsupported);
        Assert.Contains(decision.Capabilities, item => item.Name == "network-denial" && item.State == Phase35FControlState.Unsupported);
        Assert.Contains(decision.Capabilities, item => item.Name == "child-process-denial" && item.State == Phase35FControlState.Unsupported);
        Assert.DoesNotContain(decision.Capabilities, item => item.State == Phase35FControlState.Enforced);
        Assert.NotEmpty(decision.EvidenceHash);
    }

    [Fact]
    public void CodeSigningAndHardenedRuntimeAreNotReportedAsContainment()
    {
        var decision = new Phase35FContainmentSelector().Evaluate();

        Assert.Equal(Phase35FControlState.PartiallyEnforced, decision.Capabilities.Single(item => item.Name == "process-identity-binding").State);
        Assert.Equal(Phase35FControlState.Unsupported, decision.Capabilities.Single(item => item.Name == "filesystem-write").State);
        Assert.Equal(Phase35FControlState.Unsupported, decision.Capabilities.Single(item => item.Name == "network-denial").State);
    }
}
