using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirMaterializationOrchestrationServiceTests
{
    [Fact]
    public void Preview_MapsAbsentEmptyExactMatchAndConflictWithoutMutation()
    {
        using var directory = new TemporaryDirectory();
        var input = CreateInput(directory.Path);
        var service = new PbirMaterializationOrchestrationService();

        var absent = service.Preview(CreatePreviewRequest(input));
        Assert.Equal(PbirMaterializationOrchestrationOutcome.Absent, absent.Outcome);
        Assert.NotNull(absent.ValidatedPreview);

        Directory.CreateDirectory(Path.Combine(directory.Path, input.TargetDirectoryName));
        var empty = service.Preview(CreatePreviewRequest(input));
        Assert.Equal(PbirMaterializationOrchestrationOutcome.Empty, empty.Outcome);

        var serialized = Serialize(input);
        PbirDeployableMaterializationPreviewServiceTests.WriteArtifact(
            Path.Combine(directory.Path, input.TargetDirectoryName), serialized.Artifact!);
        var exact = service.Preview(CreatePreviewRequest(input));
        Assert.Equal(PbirMaterializationOrchestrationOutcome.ExactMatch, exact.Outcome);

        File.WriteAllText(Path.Combine(directory.Path, input.TargetDirectoryName, "unmanaged.txt"), "user data");
        var conflict = service.Preview(CreatePreviewRequest(input));
        Assert.Equal(PbirMaterializationOrchestrationOutcome.Conflict, conflict.Outcome);
    }

    [Fact]
    public void Preview_RejectsUnsupportedOperationUnsafeDestinationAndCancellation()
    {
        using var directory = new TemporaryDirectory();
        var input = CreateInput(directory.Path);
        var service = new PbirMaterializationOrchestrationService();

        var unsupported = service.Preview(CreatePreviewRequest(input) with { RequestedOperation = "deploy" });
        Assert.Equal(PbirMaterializationOrchestrationOutcome.UnsupportedOperation, unsupported.Outcome);

        var unsafeResult = service.Preview(CreatePreviewRequest(input with { TargetDirectoryName = "../escape" }));
        Assert.Equal(PbirMaterializationOrchestrationOutcome.UnsafeDestination, unsafeResult.Outcome);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = service.Preview(CreatePreviewRequest(input), cancellation.Token);
        Assert.Equal(PbirMaterializationOrchestrationOutcome.Cancelled, cancelled.Outcome);
    }

    [Fact]
    public void Apply_RequiresExactPreviewIdentityAndExplicitApproval()
    {
        using var directory = new TemporaryDirectory();
        var input = CreateInput(directory.Path);
        var service = new PbirMaterializationOrchestrationService();
        var preview = service.Preview(CreatePreviewRequest(input));
        var request = CreateApplyRequest(input, preview.ValidatedPreview!);

        var unapproved = service.Apply(request with { ApplyApproved = false });
        Assert.Equal(PbirMaterializationOrchestrationOutcome.InvalidRequest, unapproved.Outcome);

        var mismatched = service.Apply(request with
        {
            ValidatedPreview = request.ValidatedPreview with { PreviewHash = new string('0', 64) },
            TransactionId = "transaction-mismatched"
        });
        Assert.Equal(PbirMaterializationOrchestrationOutcome.StalePreview, mismatched.Outcome);
        Assert.False(Directory.Exists(Path.Combine(directory.Path, input.TargetDirectoryName)));
    }

    [Fact]
    public void Apply_DelegatesExactBytesAndReturnsTypedAppliedResult()
    {
        using var directory = new TemporaryDirectory();
        var input = CreateInput(directory.Path);
        var service = new PbirMaterializationOrchestrationService();
        var preview = service.Preview(CreatePreviewRequest(input));

        var applied = service.Apply(CreateApplyRequest(input, preview.ValidatedPreview!));

        Assert.Equal(PbirMaterializationOrchestrationOutcome.Applied, applied.Outcome);
        Assert.Equal("phase31-transaction", applied.TransactionId);
        Assert.True(applied.RollbackAvailable);
        var serialized = Serialize(input);
        foreach (var file in serialized.Artifact!.Files)
        {
            var path = Path.Combine(directory.Path, input.TargetDirectoryName, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.Equal(file.Content, File.ReadAllText(path));
            Assert.Equal(file.HashSha256, applied.WrittenFiles.Single(value => value.RelativePath == file.RelativePath).HashSha256);
        }
    }

    [Fact]
    public void Apply_DestinationChangedAfterPreview_ReturnsStalePreview()
    {
        using var directory = new TemporaryDirectory();
        var input = CreateInput(directory.Path);
        var service = new PbirMaterializationOrchestrationService();
        var preview = service.Preview(CreatePreviewRequest(input));
        var target = Path.Combine(directory.Path, input.TargetDirectoryName);
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "user.txt"), "changed after preview");

        var result = service.Apply(CreateApplyRequest(input, preview.ValidatedPreview!));

        Assert.Equal(PbirMaterializationOrchestrationOutcome.StalePreview, result.Outcome);
        Assert.Equal("changed after preview", File.ReadAllText(Path.Combine(target, "user.txt")));
    }

    [Fact]
    public void Preview_AndApply_MapManagedReplacementAndTransactionReuse()
    {
        using var directory = new TemporaryDirectory();
        var first = CreateInput(directory.Path);
        var service = new PbirMaterializationOrchestrationService();
        var firstPreview = service.Preview(CreatePreviewRequest(first));
        Assert.Equal(PbirMaterializationOrchestrationOutcome.Applied,
            service.Apply(CreateApplyRequest(first, firstPreview.ValidatedPreview!)).Outcome);

        var second = CreateAlternateInput(directory.Path);
        var replacementPreview = service.Preview(CreatePreviewRequest(second) with { RequestId = "phase31-preview-replacement" });
        Assert.Equal(PbirMaterializationOrchestrationOutcome.ManagedReplacement, replacementPreview.Outcome);

        var reused = service.Apply(CreateApplyRequest(second, replacementPreview.ValidatedPreview!) with
        {
            RequestId = "phase31-apply-reused",
            TransactionId = "phase31-transaction"
        });
        Assert.Equal(PbirMaterializationOrchestrationOutcome.TransactionReused, reused.Outcome);
    }

    [Fact]
    public void Apply_CancelledDuringStaging_ReturnsCancelledWithoutTargetPublication()
    {
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        var input = CreateInput(directory.Path);
        var fileSystem = new CancellingFileSystem(
            new PbirDeployableMaterializationFileSystem(), cancellation, cancelOnCreateNumber: 3);
        var service = new PbirMaterializationOrchestrationService(
            new PbirDeployableSerializerService(),
            new PbirDeployableMaterializationPreviewService(
                new PbirDeployableMaterializationSafetyGate(),
                new PbirDeployableMaterializationCanonicalJson(),
                fileSystem),
            new PbirDeployableMaterializationApplyService(fileSystem));
        var preview = service.Preview(CreatePreviewRequest(input));

        var result = service.Apply(CreateApplyRequest(input, preview.ValidatedPreview!), cancellation.Token);

        Assert.Equal(PbirMaterializationOrchestrationOutcome.Cancelled, result.Outcome);
        Assert.False(Directory.Exists(Path.Combine(directory.Path, input.TargetDirectoryName)));
    }

    [Fact]
    public async Task Apply_ConcurrentRequests_CommitAtMostOnceAndFailClosed()
    {
        using var directory = new TemporaryDirectory();
        var input = CreateInput(directory.Path);
        var previewService = new PbirMaterializationOrchestrationService();
        var preview = previewService.Preview(CreatePreviewRequest(input));
        using var start = new ManualResetEventSlim(false);

        Task<PbirMaterializationOrchestrationResult> Run(string transactionId) => Task.Run(() =>
        {
            start.Wait();
            return new PbirMaterializationOrchestrationService().Apply(
                CreateApplyRequest(input, preview.ValidatedPreview!) with { TransactionId = transactionId });
        });

        var first = Run("phase31-concurrent-a");
        var second = Run("phase31-concurrent-b");
        start.Set();
        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, results.Count(result => result.Outcome == PbirMaterializationOrchestrationOutcome.Applied));
        Assert.Contains(results, result => result.Outcome is
            PbirMaterializationOrchestrationOutcome.StalePreview or
            PbirMaterializationOrchestrationOutcome.RecoveryRequired or
            PbirMaterializationOrchestrationOutcome.Failure);
    }

    [Fact]
    public void InspectRecovery_IsReadOnlyAndReportsActiveTransaction()
    {
        using var directory = new TemporaryDirectory();
        var input = CreateInput(directory.Path);
        var service = new PbirMaterializationOrchestrationService();
        var previewResult = service.Preview(CreatePreviewRequest(input));
        var serialized = Serialize(input);
        var phase30Preview = new PbirDeployableMaterializationPreviewService().CreatePreview(
            serialized.Artifact!, serialized.Manifest!, CreatePhase30PreviewRequest(input, serialized, "phase31-preview"), directory.Path).Preview!;
        var fileSystem = new PbirDeployableMaterializationFileSystem();
        var canonical = new PbirDeployableMaterializationCanonicalJson();
        var paths = new PbirDeployableMaterializationPathPolicy(fileSystem).Resolve(
            directory.Path, input.TargetDirectoryName, serialized.Artifact!.Files.Select(file => file.RelativePath).ToArray());
        var store = new PbirDeployableMaterializationTransactionStore(fileSystem, canonical);
        store.EnsureControlRoot(paths);
        store.Begin(paths, phase30Preview, PbirDeployableMaterializationApplyServiceTests.CreateApplyRequest(
            phase30Preview,
            new PbirDeployableMaterializationPreviewServiceTests.MaterializationInputs(serialized.Artifact!, serialized.Manifest!, CreatePhase30PreviewRequest(input, serialized, "phase31-preview")),
            "phase31-interrupted"), null);
        var before = Snapshot(directory.Path);

        var result = service.InspectRecovery(new(
            PbirMaterializationOrchestrationRecoveryRequestContract.SchemaVersionV1,
            "phase31-recovery", "inspectRecovery", input, previewResult.ValidatedPreview!.PreviewRequestId));

        Assert.Equal(PbirMaterializationOrchestrationOutcome.RecoveryRequired, result.Outcome);
        Assert.Equal("phase31-interrupted", result.ActiveTransactionRef);
        Assert.Equal(before, Snapshot(directory.Path));
    }

    internal static PbirMaterializationOrchestrationInput CreateInput(string outputBase, string target = "Sales.Report")
    {
        var ready = PbirDeployableSerializerServiceTests.CreateReadyInputs();
        return new(ready.IrState, ready.SerializerRequest, ready.DeployableRequest, outputBase, target);
    }

    private static PbirMaterializationOrchestrationInput CreateAlternateInput(string outputBase)
    {
        var ready = PbirDeployableSerializerServiceTests.CreateReadyInputs();
        var deployable = ready.DeployableRequest with
        {
            RequestId = "pbirDeployableSerializerRequest:phase31-alternate",
            DatasetReference = new PbirDatasetReference(new PbirDatasetReferenceByPath("Finance.SemanticModel"))
        };
        return new(ready.IrState, ready.SerializerRequest, deployable, outputBase, "Sales.Report");
    }

    internal static PbirMaterializationOrchestrationPreviewRequest CreatePreviewRequest(PbirMaterializationOrchestrationInput input) =>
        new(PbirMaterializationOrchestrationPreviewRequestContract.SchemaVersionV1, "phase31-preview", "preview", input);

    internal static PbirMaterializationOrchestrationApplyRequest CreateApplyRequest(
        PbirMaterializationOrchestrationInput input,
        PbirMaterializationOrchestrationPreviewIdentity identity) =>
        new(PbirMaterializationOrchestrationApplyRequestContract.SchemaVersionV1, "phase31-apply", "apply", input, identity, "phase31-transaction", true);

    internal static PbirDeployableSerializerState Serialize(PbirMaterializationOrchestrationInput input) =>
        new PbirDeployableSerializerService().CreateArtifacts(input.IrState, input.SerializerRequest, input.DeployableSerializerRequest);

    private static PbirDeployableMaterializationPreviewRequest CreatePhase30PreviewRequest(
        PbirMaterializationOrchestrationInput input,
        PbirDeployableSerializerState serialized,
        string requestId) =>
        new(PbirDeployableMaterializationPreviewRequestContract.SchemaVersionV1, requestId,
            serialized.Artifact!.ArtifactId, serialized.Artifact.Hashes.ArtifactHash,
            serialized.Manifest!.ManifestId, serialized.Manifest.Hashes.ManifestHash,
            input.TargetDirectoryName, "preview", PbirDeployableMaterializationExecutionPolicy.PreviewOnly);

    private static string[] Snapshot(string root) => Directory
        .EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories)
        .Select(path => Path.GetRelativePath(root, path))
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();

    internal sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pbir-phase31-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }
        public void Dispose() => Directory.Delete(Path, recursive: true);
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
