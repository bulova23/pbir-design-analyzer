using System.Text;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirDeployableMaterializationContractTests
{
    [Theory]
    [InlineData("../escape")]
    [InlineData("nested/target")]
    [InlineData("nested\\target")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData(".pbir-design-analyzer")]
    [InlineData("report.pbip")]
    [InlineData("report.SemanticModel")]
    [InlineData("target.")]
    [InlineData("target ")]
    [InlineData("C:target")]
    public void PathPolicy_RejectsHostileOrReservedTargetNames(string targetName)
    {
        using var directory = new TemporaryDirectory();
        var result = new PbirDeployableMaterializationPathPolicy().Resolve(directory.Path, targetName, []);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void PathPolicy_ResolvesSafeTargetAndStableKeyBelowAbsoluteBase()
    {
        using var directory = new TemporaryDirectory();
        var result = new PbirDeployableMaterializationPathPolicy().Resolve(
            directory.Path,
            "Sales.Report",
            ["definition.pbir", "definition/pages/pages.json"]);

        Assert.True(result.IsValid);
        Assert.Equal(Path.Combine(directory.Path, "Sales.Report"), result.CanonicalTargetPath);
        Assert.StartsWith(directory.Path, result.ControlRootPath, StringComparison.Ordinal);
        Assert.Matches("^[0-9a-f]{32}$", result.TargetKey);
    }

    [Fact]
    public void PathPolicy_RejectsLegacyRootReportTraversalAndPlatformCollisions()
    {
        using var directory = new TemporaryDirectory();
        var policy = new PbirDeployableMaterializationPathPolicy();

        Assert.False(policy.Resolve(directory.Path, "Sales.Report", ["report.json"]).IsValid);
        Assert.False(policy.Resolve(directory.Path, "Sales.Report", ["../definition.pbir"]).IsValid);
        var collision = policy.Resolve(directory.Path, "Sales.Report", ["definition/A.json", "definition/a.json"]);
        Assert.Equal(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS(), !collision.IsValid);
    }

    [Fact]
    public void Contracts_ExposeExactPhase30VersionsAndStates()
    {
        Assert.Equal("pbir-deployable-materialization-preview-request/v1", PbirDeployableMaterializationPreviewRequestContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-materialization-preview/v1", PbirDeployableMaterializationPreviewContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-materialization-control-root/v1", PbirDeployableMaterializationControlRootContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-materialization-apply-request/v1", PbirDeployableMaterializationApplyRequestContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-materialization-transaction/v1", PbirDeployableMaterializationTransactionContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-materialization-apply-result/v1", PbirDeployableMaterializationApplyResultContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-materialization-receipt/v1", PbirDeployableMaterializationReceiptContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-materialization-rollback-request/v1", PbirDeployableMaterializationRollbackRequestContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-materialization-rollback-result/v1", PbirDeployableMaterializationRollbackResultContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-materialization-diagnostics/v1", PbirDeployableMaterializationDiagnosticsContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-materialization-readiness/v1", PbirDeployableMaterializationReadinessContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-materialization-lineage/v1", PbirDeployableMaterializationLineageContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-materialization-hashes/v1", PbirDeployableMaterializationHashesContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-target-inventory/v1", PbirDeployableTargetInventoryContract.SchemaVersionV1);

        Assert.Equal(
            new[] { "Absent", "EmptyDirectory", "Files" },
            Enum.GetNames<PbirDeployableTargetState>());
        Assert.Equal(
            new[] { "Create", "ReplaceManaged", "NoChanges", "BlockedConflict", "RecoveryRequired" },
            Enum.GetNames<PbirDeployableMaterializationDisposition>());
    }

    [Fact]
    public void CanonicalJson_TargetInventory_HasExactBytesAndStableOrdering()
    {
        var inventory = new PbirDeployableTargetInventory(
            PbirDeployableTargetInventoryContract.SchemaVersionV1,
            PbirDeployableTargetState.Files,
            [
                new("z.json", 1, new string('b', 64)),
                new("definition.pbir", 123, "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")
            ]);

        var canonical = new PbirDeployableMaterializationCanonicalJson();
        var bytes = canonical.SerializeTargetInventory(inventory);
        var text = Encoding.UTF8.GetString(bytes);

        Assert.Equal(
            "{\"schemaVersion\":\"pbir-deployable-target-inventory/v1\",\"targetState\":\"files\",\"files\":[{\"relativePath\":\"definition.pbir\",\"byteLength\":123,\"hashSha256\":\"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef\"},{\"relativePath\":\"z.json\",\"byteLength\":1,\"hashSha256\":\"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\"}]}",
            text);
        Assert.False(text.EndsWith('\n'));
        Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        Assert.Matches("^[0-9a-f]{64}$", canonical.ComputeSha256(bytes));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pbir-phase30-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
