using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirMaterializationOrchestrationContractTests
{
    [Fact]
    public void Contracts_ExposeExactPhase31VersionsAndOutcomes()
    {
        Assert.Equal("pbir-materialization-orchestration-preview-request/v1", PbirMaterializationOrchestrationPreviewRequestContract.SchemaVersionV1);
        Assert.Equal("pbir-materialization-orchestration-apply-request/v1", PbirMaterializationOrchestrationApplyRequestContract.SchemaVersionV1);
        Assert.Equal("pbir-materialization-orchestration-recovery-request/v1", PbirMaterializationOrchestrationRecoveryRequestContract.SchemaVersionV1);
        Assert.Equal("pbir-materialization-orchestration-preview-identity/v1", PbirMaterializationOrchestrationPreviewIdentityContract.SchemaVersionV1);
        Assert.Equal("pbir-materialization-orchestration-result/v1", PbirMaterializationOrchestrationResultContract.SchemaVersionV1);
        Assert.Equal("pbir-materialization-orchestration-diagnostics/v1", PbirMaterializationOrchestrationDiagnosticsContract.SchemaVersionV1);

        Assert.Equal(
            new[]
            {
                "Absent", "Empty", "ExactMatch", "ManagedReplacement", "Conflict", "RecoveryRequired",
                "Applied", "StalePreview", "InvalidRequest", "UnsafeDestination", "UnsupportedOperation",
                "SchemaFailure", "TransactionReused", "Cancelled", "Failure"
            },
            Enum.GetNames<PbirMaterializationOrchestrationOutcome>());
    }

    [Fact]
    public void ApplyRequest_CarriesValidatedPreviewAndFreshTransactionIdentity()
    {
        var properties = typeof(PbirMaterializationOrchestrationApplyRequest).GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Contains("ValidatedPreview", properties);
        Assert.Contains("TransactionId", properties);
        Assert.Contains("ApplyApproved", properties);
        Assert.Contains("RequestedOperation", properties);
        Assert.DoesNotContain("ForceOverwrite", properties);
    }
}
