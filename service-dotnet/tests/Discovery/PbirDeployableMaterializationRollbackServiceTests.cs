using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirDeployableMaterializationRollbackServiceTests
{
    [Fact]
    public void Rollback_CurrentApplyFromAbsent_QuarantinesAppliedTreeAndRestoresAbsence()
    {
        using var directory = new PbirDeployableMaterializationPreviewServiceTests.TemporaryDirectory();
        var inputs = PbirDeployableMaterializationPreviewServiceTests.CreateInputs("Sales.Report");
        var preview = PbirDeployableMaterializationApplyServiceTests.CreatePreview(inputs, directory.Path);
        var apply = new PbirDeployableMaterializationApplyService().Apply(
            inputs.Artifact, inputs.Manifest, preview,
            PbirDeployableMaterializationApplyServiceTests.CreateApplyRequest(preview, inputs, "transaction-rollback-1"),
            directory.Path);
        var applied = Assert.IsType<PbirDeployableMaterializationApplyResult>(apply.Result);
        var request = CreateRollbackRequest(preview, applied);

        var state = new PbirDeployableMaterializationRollbackService().Rollback(request, directory.Path);

        Assert.Equal(PbirDeployableMaterializationReadinessState.RolledBack, state.Readiness);
        Assert.NotNull(state.Result);
        Assert.Equal(PbirDeployableTargetState.Absent, state.Result.RestoredTargetState);
        Assert.False(Directory.Exists(Path.Combine(directory.Path, "Sales.Report")));
        Assert.True(Directory.Exists(Path.Combine(
            directory.Path, ".pbir-design-analyzer", "materialization", "targets", preview.TargetKey,
            "transactions", "transaction-rollback-1", "quarantine")));
    }

    [Fact]
    public void Rollback_MutatedAppliedTarget_FailsClosedAndPreservesMutation()
    {
        using var directory = new PbirDeployableMaterializationPreviewServiceTests.TemporaryDirectory();
        var inputs = PbirDeployableMaterializationPreviewServiceTests.CreateInputs("Sales.Report");
        var preview = PbirDeployableMaterializationApplyServiceTests.CreatePreview(inputs, directory.Path);
        var apply = new PbirDeployableMaterializationApplyService().Apply(
            inputs.Artifact, inputs.Manifest, preview,
            PbirDeployableMaterializationApplyServiceTests.CreateApplyRequest(preview, inputs, "transaction-rollback-2"),
            directory.Path);
        var applied = Assert.IsType<PbirDeployableMaterializationApplyResult>(apply.Result);
        File.WriteAllText(Path.Combine(directory.Path, "Sales.Report", "hostile.txt"), "do not lose");

        var state = new PbirDeployableMaterializationRollbackService().Rollback(CreateRollbackRequest(preview, applied), directory.Path);

        Assert.Null(state.Result);
        Assert.NotEqual(PbirDeployableMaterializationReadinessState.RolledBack, state.Readiness);
        Assert.Equal("do not lose", File.ReadAllText(Path.Combine(directory.Path, "Sales.Report", "hostile.txt")));
    }

    [Fact]
    public void Rollback_InterruptedBeforeTargetMutation_RecoversJournaledPreState()
    {
        using var directory = new PbirDeployableMaterializationPreviewServiceTests.TemporaryDirectory();
        var inputs = PbirDeployableMaterializationPreviewServiceTests.CreateInputs("Sales.Report");
        var preview = PbirDeployableMaterializationApplyServiceTests.CreatePreview(inputs, directory.Path);
        var fileSystem = new PbirDeployableMaterializationFileSystem();
        var canonical = new PbirDeployableMaterializationCanonicalJson();
        var paths = new PbirDeployableMaterializationPathPolicy(fileSystem).Resolve(
            directory.Path, "Sales.Report", inputs.Artifact.Files.Select(file => file.RelativePath).ToArray());
        var store = new PbirDeployableMaterializationTransactionStore(fileSystem, canonical);
        store.EnsureControlRoot(paths);
        var transaction = store.Begin(
            paths, preview,
            PbirDeployableMaterializationApplyServiceTests.CreateApplyRequest(preview, inputs, "transaction-recover-initialized"),
            previousReceiptHash: null);
        var request = new PbirDeployableMaterializationRollbackRequest(
            PbirDeployableMaterializationRollbackRequestContract.SchemaVersionV1,
            "recover:initialized", transaction.TransactionId, "Sales.Report", preview.TargetKey,
            transaction.TransactionHash, null, preview.Hashes.TargetStateHash,
            true, PbirDeployableMaterializationExecutionPolicy.LocalMutationOnly);

        var state = new PbirDeployableMaterializationRollbackService().Rollback(request, directory.Path);

        Assert.Equal(PbirDeployableMaterializationReadinessState.RolledBack, state.Readiness);
        Assert.Equal(PbirDeployableMaterializationRecoveryDisposition.RecoveredInterruptedApply, state.Result!.RecoveryDisposition);
        Assert.Equal(PbirDeployableTargetState.Absent, state.Result.RestoredTargetState);
    }

    private static PbirDeployableMaterializationRollbackRequest CreateRollbackRequest(
        PbirDeployableMaterializationPreview preview,
        PbirDeployableMaterializationApplyResult applied) => new(
            PbirDeployableMaterializationRollbackRequestContract.SchemaVersionV1,
            $"rollback:{applied.TransactionId}", applied.TransactionId, "Sales.Report", preview.TargetKey,
            applied.TransactionHash, applied.CurrentReceiptHash, applied.CommittedTargetStateHash,
            true, PbirDeployableMaterializationExecutionPolicy.LocalMutationOnly);
}
