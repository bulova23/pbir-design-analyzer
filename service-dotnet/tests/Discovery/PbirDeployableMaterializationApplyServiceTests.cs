using System.Text;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirDeployableMaterializationApplyServiceTests
{
    [Fact]
    public void Apply_ApprovedAbsentTarget_PublishesCompleteExactArtifactTransactionally()
    {
        using var directory = new PbirDeployableMaterializationPreviewServiceTests.TemporaryDirectory();
        var inputs = PbirDeployableMaterializationPreviewServiceTests.CreateInputs("Sales.Report");
        var preview = CreatePreview(inputs, directory.Path);
        var request = CreateApplyRequest(preview, inputs, "transaction-create-1");

        var state = new PbirDeployableMaterializationApplyService().Apply(
            inputs.Artifact, inputs.Manifest, preview, request, directory.Path);

        Assert.Equal(PbirDeployableMaterializationReadinessState.Applied, state.Readiness);
        Assert.NotNull(state.Result);
        foreach (var file in inputs.Artifact.Files)
        {
            var path = Path.Combine(directory.Path, "Sales.Report", file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(Encoding.UTF8.GetBytes(file.Content), File.ReadAllBytes(path));
        }
        Assert.False(File.Exists(Path.Combine(directory.Path, "Sales.Report", "report.json")));
        Assert.True(File.Exists(Path.Combine(directory.Path, ".pbir-design-analyzer", "materialization", "targets", preview.TargetKey, "current-receipt.json")));
    }

    [Fact]
    public void Apply_StalePreview_FailsBeforePublishing()
    {
        using var directory = new PbirDeployableMaterializationPreviewServiceTests.TemporaryDirectory();
        var inputs = PbirDeployableMaterializationPreviewServiceTests.CreateInputs("Sales.Report");
        var preview = CreatePreview(inputs, directory.Path);
        Directory.CreateDirectory(Path.Combine(directory.Path, "Sales.Report"));
        File.WriteAllText(Path.Combine(directory.Path, "Sales.Report", "user.txt"), "preserve me");

        var state = new PbirDeployableMaterializationApplyService().Apply(
            inputs.Artifact, inputs.Manifest, preview, CreateApplyRequest(preview, inputs, "transaction-stale-1"), directory.Path);

        Assert.Null(state.Result);
        Assert.NotEqual(PbirDeployableMaterializationReadinessState.Applied, state.Readiness);
        Assert.Equal("preserve me", File.ReadAllText(Path.Combine(directory.Path, "Sales.Report", "user.txt")));
        Assert.False(File.Exists(Path.Combine(directory.Path, "Sales.Report", "definition.pbir")));
    }

    [Fact]
    public void Apply_IdenticalArtifactsToDifferentAuthorizedTargets_WritesByteIdenticalTrees()
    {
        using var firstDirectory = new PbirDeployableMaterializationPreviewServiceTests.TemporaryDirectory();
        using var secondDirectory = new PbirDeployableMaterializationPreviewServiceTests.TemporaryDirectory();
        var inputs = PbirDeployableMaterializationPreviewServiceTests.CreateInputs("Sales.Report");

        ApplyTo(firstDirectory.Path, inputs, "transaction-repeat-a");
        ApplyTo(secondDirectory.Path, inputs, "transaction-repeat-b");

        foreach (var file in inputs.Artifact.Files)
        {
            var relative = file.RelativePath.Replace('/', Path.DirectorySeparatorChar);
            Assert.Equal(
                File.ReadAllBytes(Path.Combine(firstDirectory.Path, "Sales.Report", relative)),
                File.ReadAllBytes(Path.Combine(secondDirectory.Path, "Sales.Report", relative)));
        }
    }

    [Fact]
    public void Apply_InjectedStagingFailure_DoesNotExposePartialTarget()
    {
        using var directory = new PbirDeployableMaterializationPreviewServiceTests.TemporaryDirectory();
        var inputs = PbirDeployableMaterializationPreviewServiceTests.CreateInputs("Sales.Report");
        var preview = CreatePreview(inputs, directory.Path);
        var fileSystem = new ThrowingFileSystem(new PbirDeployableMaterializationFileSystem(), throwOnCreateNumber: 4);

        var state = new PbirDeployableMaterializationApplyService(fileSystem).Apply(
            inputs.Artifact, inputs.Manifest, preview, CreateApplyRequest(preview, inputs, "transaction-failure-1"), directory.Path);

        Assert.Null(state.Result);
        Assert.NotEqual(PbirDeployableMaterializationReadinessState.Applied, state.Readiness);
        Assert.False(Directory.Exists(Path.Combine(directory.Path, "Sales.Report")));

        var retryPreview = CreatePreview(inputs, directory.Path);
        var retry = new PbirDeployableMaterializationApplyService().Apply(
            inputs.Artifact, inputs.Manifest, retryPreview,
            CreateApplyRequest(retryPreview, inputs, "transaction-failure-retry"), directory.Path);
        Assert.Equal(PbirDeployableMaterializationReadinessState.Applied, retry.Readiness);
    }

    [Fact]
    public void Apply_CancelledDuringStaging_AbortsWithoutExposingPartialTarget()
    {
        using var directory = new PbirDeployableMaterializationPreviewServiceTests.TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        var inputs = PbirDeployableMaterializationPreviewServiceTests.CreateInputs("Sales.Report");
        var preview = CreatePreview(inputs, directory.Path);
        var fileSystem = new CancellingFileSystem(
            new PbirDeployableMaterializationFileSystem(), cancellation, cancelOnCreateNumber: 3);

        Assert.Throws<OperationCanceledException>(() =>
            new PbirDeployableMaterializationApplyService(fileSystem).Apply(
                inputs.Artifact,
                inputs.Manifest,
                preview,
                CreateApplyRequest(preview, inputs, "transaction-cancelled-1"),
                directory.Path,
                cancellation.Token));

        Assert.False(Directory.Exists(Path.Combine(directory.Path, "Sales.Report")));
        var paths = new PbirDeployableMaterializationPathPolicy().Resolve(
            directory.Path, "Sales.Report", inputs.Artifact.Files.Select(file => file.RelativePath).ToArray());
        var transaction = new PbirDeployableMaterializationTransactionStore(
            new PbirDeployableMaterializationFileSystem(), new PbirDeployableMaterializationCanonicalJson())
            .ReadTransaction(paths, "transaction-cancelled-1");
        Assert.Equal(PbirDeployableMaterializationJournalPhase.Aborted, transaction.Phase);
    }

    [Fact]
    public void Apply_ReusedTransactionIdAndNoChangePreview_FailClosed()
    {
        using var directory = new PbirDeployableMaterializationPreviewServiceTests.TemporaryDirectory();
        var inputs = PbirDeployableMaterializationPreviewServiceTests.CreateInputs("Sales.Report");
        ApplyTo(directory.Path, inputs, "transaction-reused");
        var noChange = CreatePreview(inputs, directory.Path);

        var noChangeApply = new PbirDeployableMaterializationApplyService().Apply(
            inputs.Artifact, inputs.Manifest, noChange,
            CreateApplyRequest(noChange, inputs, "transaction-no-change"), directory.Path);
        Assert.Null(noChangeApply.Result);

        var alternate = CreateAlternateInputs("Sales.Report");
        var replace = CreatePreview(alternate, directory.Path);
        var reused = new PbirDeployableMaterializationApplyService().Apply(
            alternate.Artifact, alternate.Manifest, replace,
            CreateApplyRequest(replace, alternate, "transaction-reused"), directory.Path);
        Assert.Null(reused.Result);
    }

    [Fact]
    public void Apply_ManagedPriorTarget_ReplacesAndRollbackRestoresExactPriorBytes()
    {
        using var directory = new PbirDeployableMaterializationPreviewServiceTests.TemporaryDirectory();
        var first = PbirDeployableMaterializationPreviewServiceTests.CreateInputs("Sales.Report");
        ApplyTo(directory.Path, first, "transaction-managed-first");
        var originalDefinition = File.ReadAllBytes(Path.Combine(directory.Path, "Sales.Report", "definition.pbir"));
        var second = CreateAlternateInputs("Sales.Report");

        var replacementPreviewState = new PbirDeployableMaterializationPreviewService().CreatePreview(
            second.Artifact, second.Manifest, second.Request, directory.Path);
        Assert.Equal(PbirDeployableMaterializationReadinessState.ReadyToReplaceManaged, replacementPreviewState.Readiness);
        var replacementPreview = Assert.IsType<PbirDeployableMaterializationPreview>(replacementPreviewState.Preview);
        Assert.Equal(PbirDeployableMaterializationDisposition.ReplaceManaged, replacementPreview.Disposition);
        var apply = new PbirDeployableMaterializationApplyService().Apply(
            second.Artifact, second.Manifest, replacementPreview,
            CreateApplyRequest(replacementPreview, second, "transaction-managed-second"), directory.Path);
        var applied = Assert.IsType<PbirDeployableMaterializationApplyResult>(apply.Result);
        Assert.NotEqual(originalDefinition, File.ReadAllBytes(Path.Combine(directory.Path, "Sales.Report", "definition.pbir")));

        var rollback = new PbirDeployableMaterializationRollbackService().Rollback(
            new PbirDeployableMaterializationRollbackRequest(
                PbirDeployableMaterializationRollbackRequestContract.SchemaVersionV1,
                "rollback:managed-second", applied.TransactionId, "Sales.Report", replacementPreview.TargetKey,
                applied.TransactionHash, applied.CurrentReceiptHash, applied.CommittedTargetStateHash,
                true, PbirDeployableMaterializationExecutionPolicy.LocalMutationOnly),
            directory.Path);

        Assert.Equal(PbirDeployableMaterializationReadinessState.RolledBack, rollback.Readiness);
        Assert.Equal(originalDefinition, File.ReadAllBytes(Path.Combine(directory.Path, "Sales.Report", "definition.pbir")));
    }

    internal static PbirDeployableMaterializationPreview CreatePreview(
        PbirDeployableMaterializationPreviewServiceTests.MaterializationInputs inputs,
        string outputBase) => Assert.IsType<PbirDeployableMaterializationPreview>(
            new PbirDeployableMaterializationPreviewService().CreatePreview(inputs.Artifact, inputs.Manifest, inputs.Request, outputBase).Preview);

    internal static PbirDeployableMaterializationApplyRequest CreateApplyRequest(
        PbirDeployableMaterializationPreview preview,
        PbirDeployableMaterializationPreviewServiceTests.MaterializationInputs inputs,
        string transactionId) => new(
            PbirDeployableMaterializationApplyRequestContract.SchemaVersionV1,
            $"apply:{transactionId}", transactionId, preview.PreviewId, preview.Hashes.SelfHash,
            inputs.Artifact.ArtifactId, inputs.Artifact.Hashes.ArtifactHash,
            inputs.Manifest.ManifestId, inputs.Manifest.Hashes.ManifestHash,
            preview.Hashes.TargetStateHash, true, true,
            PbirDeployableMaterializationExecutionPolicy.LocalMutationOnly);

    private static void ApplyTo(
        string outputBase,
        PbirDeployableMaterializationPreviewServiceTests.MaterializationInputs inputs,
        string transactionId)
    {
        var preview = CreatePreview(inputs, outputBase);
        var state = new PbirDeployableMaterializationApplyService().Apply(
            inputs.Artifact, inputs.Manifest, preview, CreateApplyRequest(preview, inputs, transactionId), outputBase);
        Assert.Equal(PbirDeployableMaterializationReadinessState.Applied, state.Readiness);
    }

    private static PbirDeployableMaterializationPreviewServiceTests.MaterializationInputs CreateAlternateInputs(string targetName)
    {
        var ready = PbirDeployableSerializerServiceTests.CreateReadyInputs();
        var request = ready.DeployableRequest with
        {
            RequestId = "pbirDeployableSerializerRequest:phase30-alternate",
            DatasetReference = new PbirDatasetReference(new PbirDatasetReferenceByPath("Finance.SemanticModel"))
        };
        var serialized = new PbirDeployableSerializerService().CreateArtifacts(ready.IrState, ready.SerializerRequest, request);
        var artifact = Assert.IsType<PbirDeployableArtifact>(serialized.Artifact);
        var manifest = Assert.IsType<PbirDeployableManifest>(serialized.Manifest);
        var previewRequest = new PbirDeployableMaterializationPreviewRequest(
            PbirDeployableMaterializationPreviewRequestContract.SchemaVersionV1,
            "materializationPreview:phase30-alternate", artifact.ArtifactId, artifact.Hashes.ArtifactHash,
            manifest.ManifestId, manifest.Hashes.ManifestHash, targetName, "preview",
            PbirDeployableMaterializationExecutionPolicy.PreviewOnly);
        return new(artifact, manifest, previewRequest);
    }

    private sealed class ThrowingFileSystem(
        IPbirDeployableMaterializationFileSystem inner,
        int throwOnCreateNumber) : IPbirDeployableMaterializationFileSystem
    {
        private int _createCount;
        public string GetFullPath(string path) => inner.GetFullPath(path);
        public bool DirectoryExists(string path) => inner.DirectoryExists(path);
        public bool FileExists(string path) => inner.FileExists(path);
        public void CreateDirectory(string path) => inner.CreateDirectory(path);
        public IEnumerable<string> EnumerateEntries(string path) => inner.EnumerateEntries(path);
        public FileAttributes GetAttributes(string path) => inner.GetAttributes(path);
        public byte[] ReadAllBytes(string path) => inner.ReadAllBytes(path);
        public void WriteAllBytesCreateNew(string path, byte[] content)
        {
            if (++_createCount == throwOnCreateNumber) throw new IOException("Injected staging failure.");
            inner.WriteAllBytesCreateNew(path, content);
        }
        public void WriteAllBytesReplace(string path, byte[] content) => inner.WriteAllBytesReplace(path, content);
        public IDisposable OpenExclusiveLock(string path) => inner.OpenExclusiveLock(path);
        public void MoveDirectory(string source, string destination) => inner.MoveDirectory(source, destination);
        public void MoveFile(string source, string destination, bool overwrite) => inner.MoveFile(source, destination, overwrite);
        public void DeleteFile(string path) => inner.DeleteFile(path);
    }

    private sealed class CancellingFileSystem(
        IPbirDeployableMaterializationFileSystem inner,
        CancellationTokenSource cancellation,
        int cancelOnCreateNumber) : IPbirDeployableMaterializationFileSystem
    {
        private int _createCount;
        public string GetFullPath(string path) => inner.GetFullPath(path);
        public bool DirectoryExists(string path) => inner.DirectoryExists(path);
        public bool FileExists(string path) => inner.FileExists(path);
        public void CreateDirectory(string path) => inner.CreateDirectory(path);
        public IEnumerable<string> EnumerateEntries(string path) => inner.EnumerateEntries(path);
        public FileAttributes GetAttributes(string path) => inner.GetAttributes(path);
        public byte[] ReadAllBytes(string path) => inner.ReadAllBytes(path);
        public void WriteAllBytesCreateNew(string path, byte[] content)
        {
            inner.WriteAllBytesCreateNew(path, content);
            if (++_createCount == cancelOnCreateNumber) cancellation.Cancel();
        }
        public void WriteAllBytesReplace(string path, byte[] content) => inner.WriteAllBytesReplace(path, content);
        public IDisposable OpenExclusiveLock(string path) => inner.OpenExclusiveLock(path);
        public void MoveDirectory(string source, string destination) => inner.MoveDirectory(source, destination);
        public void MoveFile(string source, string destination, bool overwrite) => inner.MoveFile(source, destination, overwrite);
        public void DeleteFile(string path) => inner.DeleteFile(path);
    }
}
