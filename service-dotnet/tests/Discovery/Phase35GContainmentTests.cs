using PowerBIModelingService.Services.Discovery;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35GContainmentTests
{
    [Fact]
    public void DecisionSelectsRemoteBoundaryWithoutEnablingExecution()
    {
        var decision = Phase35GContainmentArchitectureDecisionRecord.Current;

        Assert.Equal("phase35g-containment-architecture/v1", decision.ContractVersion);
        Assert.Equal(Phase35GContainmentType.RemoteControlled, decision.SelectedContainment);
        Assert.Equal(Phase35GDecisionStatus.SelectedNotEnabled, decision.Status);
        Assert.False(decision.ProviderExecutionEnabled);
        Assert.Contains(decision.RequiredPrerequisites, item => item.Contains("Windows worker", StringComparison.Ordinal));
    }

    [Fact]
    public void Phase35FMechanismContractCanRepresentFutureBoundariesWithoutSelectingThem()
    {
        Assert.Equal(Phase35FMechanism.LocalVirtualized, Enum.Parse<Phase35FMechanism>("LocalVirtualized"));
        Assert.Equal(Phase35FMechanism.RemoteControlled, Enum.Parse<Phase35FMechanism>("RemoteControlled"));
        Assert.Equal(Phase35FMechanism.NoneLocalMacOs, new Phase35FContainmentSelector().Evaluate().SelectedMechanism);
    }
}
