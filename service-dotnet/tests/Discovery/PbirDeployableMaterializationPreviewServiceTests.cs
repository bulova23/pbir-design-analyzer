using System.Text;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirDeployableMaterializationPreviewServiceTests
{
    [Fact]
    public void CreatePreview_AbsentTarget_IsReadOnlyAndPlansCompleteValidatedInventory()
    {
        using var directory = new TemporaryDirectory();
        var inputs = CreateInputs("Sales.Report");
        var before = Snapshot(directory.Path);

        var state = new PbirDeployableMaterializationPreviewService().CreatePreview(
            inputs.Artifact,
            inputs.Manifest,
            inputs.Request,
            directory.Path);

        Assert.True(
            state.Readiness == PbirDeployableMaterializationReadinessState.ReadyToCreate,
            string.Join(" | ", state.Diagnostics.Items.Select(item => $"{item.Code}:{item.Path}:{item.Message}")));
        var preview = Assert.IsType<PbirDeployableMaterializationPreview>(state.Preview);
        Assert.Equal(PbirDeployableMaterializationDisposition.Create, preview.Disposition);
        Assert.Equal(PbirDeployableTargetState.Absent, preview.TargetInventory.TargetState);
        Assert.Equal(inputs.Manifest.Files, preview.PlannedFiles);
        Assert.Equal(inputs.Artifact.Lineage.ImmutableLineage, preview.Lineage.Phase29Lineage.ImmutableLineage);
        Assert.Equal(before, Snapshot(directory.Path));
        Assert.False(Directory.Exists(Path.Combine(directory.Path, ".pbir-design-analyzer")));
    }

    [Fact]
    public void CreatePreview_ExactArtifact_IsNoChanges()
    {
        using var directory = new TemporaryDirectory();
        var inputs = CreateInputs("Sales.Report");
        WriteArtifact(Path.Combine(directory.Path, "Sales.Report"), inputs.Artifact);

        var state = new PbirDeployableMaterializationPreviewService().CreatePreview(
            inputs.Artifact,
            inputs.Manifest,
            inputs.Request,
            directory.Path);

        Assert.True(
            state.Readiness == PbirDeployableMaterializationReadinessState.NoChanges,
            string.Join(" | ", state.Diagnostics.Items.Select(item => $"{item.Code}:{item.Path}:{item.Message}")));
        Assert.Equal(PbirDeployableMaterializationDisposition.NoChanges, state.Preview!.Disposition);
    }

    [Fact]
    public void CreatePreview_UnmanagedNonemptyTarget_BlocksWithoutMutation()
    {
        using var directory = new TemporaryDirectory();
        var inputs = CreateInputs("Sales.Report");
        var target = Path.Combine(directory.Path, "Sales.Report");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "stale.txt"), "user data");
        var before = Snapshot(directory.Path);

        var state = new PbirDeployableMaterializationPreviewService().CreatePreview(
            inputs.Artifact,
            inputs.Manifest,
            inputs.Request,
            directory.Path);

        Assert.Equal(PbirDeployableMaterializationReadinessState.Blocked, state.Readiness);
        Assert.NotNull(state.Preview);
        Assert.Equal(PbirDeployableMaterializationDisposition.BlockedConflict, state.Preview.Disposition);
        Assert.Equal(before, Snapshot(directory.Path));
    }

    [Fact]
    public void CreatePreview_TamperedPhase29Artifact_FailsClosed()
    {
        using var directory = new TemporaryDirectory();
        var inputs = CreateInputs("Sales.Report");
        var tampered = inputs.Artifact with
        {
            Files = inputs.Artifact.Files.Select((file, index) => index == 0 ? file with { Content = file.Content + " " } : file).ToArray()
        };

        var state = new PbirDeployableMaterializationPreviewService().CreatePreview(
            tampered,
            inputs.Manifest,
            inputs.Request,
            directory.Path);

        Assert.Null(state.Preview);
        Assert.Equal(PbirDeployableMaterializationReadinessState.Blocked, state.Readiness);
        Assert.NotEmpty(state.Diagnostics.Items);
    }

    [Fact]
    public void CreatePreview_IncompleteTransaction_RequiresRecovery()
    {
        using var directory = new TemporaryDirectory();
        var inputs = CreateInputs("Sales.Report");
        var preview = Assert.IsType<PbirDeployableMaterializationPreview>(
            new PbirDeployableMaterializationPreviewService().CreatePreview(
                inputs.Artifact, inputs.Manifest, inputs.Request, directory.Path).Preview);
        var fileSystem = new PbirDeployableMaterializationFileSystem();
        var canonical = new PbirDeployableMaterializationCanonicalJson();
        var paths = new PbirDeployableMaterializationPathPolicy(fileSystem).Resolve(
            directory.Path, "Sales.Report", inputs.Artifact.Files.Select(file => file.RelativePath).ToArray());
        var store = new PbirDeployableMaterializationTransactionStore(fileSystem, canonical);
        store.EnsureControlRoot(paths);
        store.Begin(
            paths,
            preview,
            PbirDeployableMaterializationApplyServiceTests.CreateApplyRequest(preview, inputs, "transaction-interrupted"),
            previousReceiptHash: null);

        var state = new PbirDeployableMaterializationPreviewService().CreatePreview(
            inputs.Artifact, inputs.Manifest, inputs.Request, directory.Path);

        Assert.Equal(PbirDeployableMaterializationReadinessState.RecoveryRequired, state.Readiness);
        Assert.Equal(PbirDeployableMaterializationDisposition.RecoveryRequired, state.Preview!.Disposition);
        Assert.Equal("transaction-interrupted", state.Preview.ActiveTransactionRef);
    }

    internal static MaterializationInputs CreateInputs(string targetName)
    {
        var ready = PbirDeployableSerializerServiceTests.CreateReadyInputs();
        var serialized = new PbirDeployableSerializerService().CreateArtifacts(ready.IrState, ready.SerializerRequest, ready.DeployableRequest);
        var artifact = Assert.IsType<PbirDeployableArtifact>(serialized.Artifact);
        var manifest = Assert.IsType<PbirDeployableManifest>(serialized.Manifest);
        var request = new PbirDeployableMaterializationPreviewRequest(
            PbirDeployableMaterializationPreviewRequestContract.SchemaVersionV1,
            "materializationPreview:phase30-fixture",
            artifact.ArtifactId,
            artifact.Hashes.ArtifactHash,
            manifest.ManifestId,
            manifest.Hashes.ManifestHash,
            targetName,
            "preview",
            PbirDeployableMaterializationExecutionPolicy.PreviewOnly);
        return new MaterializationInputs(artifact, manifest, request);
    }

    internal static void WriteArtifact(string target, PbirDeployableArtifact artifact)
    {
        foreach (var file in artifact.Files)
        {
            var path = Path.Combine(target, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes(file.Content));
        }
    }

    private static string[] Snapshot(string root) => Directory
        .EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories)
        .Select(path => Path.GetRelativePath(root, path))
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();

    internal sealed record MaterializationInputs(
        PbirDeployableArtifact Artifact,
        PbirDeployableManifest Manifest,
        PbirDeployableMaterializationPreviewRequest Request);

    internal sealed class TemporaryDirectory : IDisposable
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
