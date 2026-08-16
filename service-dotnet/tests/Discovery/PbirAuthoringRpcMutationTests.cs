using PowerBIModelingService.PbirAuthoringRpc;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirAuthoringRpcMutationTests
{
    [Fact]
    public async Task Mutate_UsesPlannerExecutorSerializerAndAnalyzerForAResize()
    {
        var root = Directory.CreateTempSubdirectory("pbir-rpc-mutate-");
        var source = Path.Combine(root.FullName, "source");
        var output = Path.Combine(root.FullName, "output");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(output);
        try
        {
            var generated = await new LocalPbirGenerationProviderService().GenerateAndVerifyAsync(CreateRequest(root.FullName));
            WriteArtifact(source, generated.Artifact!);
            var imported = new PbirLocalReportReader().Import(source);
            var visualId = imported.IrState.Ir!.Visuals.Single().VisualId;
            var dispatcher = new PbirAuthoringRpcDispatcher();
            var import = await dispatcher.DispatchAsync(new(
                PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Import,
                Import: new(source)));

            var mutation = new LocalPbirMutationRequest(
                LocalPbirMutationRequestContract.SchemaVersionV1, "phase45-resize", source, output, "mutated",
                [new(LocalPbirMutationOperationKind.ResizeVisual, new(VisualId: visualId), Layout: new(X: 8, Y: null, Width: null, Height: null))],
                "Sales.SemanticModel", "phase45-resize");
            var response = await dispatcher.DispatchAsync(new(
                PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Mutate,
                Mutate: new(import.ImportResult!.Snapshot, mutation)));

            Assert.True(response.Succeeded, $"{response.Error?.Category}:{response.Error?.Code}:{response.Error?.Summary} | {string.Join(" | ", response.Diagnostics.Select(diagnostic => diagnostic.Code))}");
            Assert.NotNull(response.MutateResult!.Artifact);
            Assert.NotNull(response.Analyzer);
            Assert.NotNull(response.Fidelity);
            Assert.True(response.Timing.SerializationMilliseconds >= 0);
            Assert.True(response.Timing.AnalyzerMilliseconds >= 0);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Mutate_MapsPlannerConflictToStableMutationConflict()
    {
        var dispatcher = new PbirAuthoringRpcDispatcher();
        var request = new LocalPbirMutationRequest(
            LocalPbirMutationRequestContract.SchemaVersionV1, "phase45-conflict", "/missing", "/tmp", "target", [],
            null, null);
        var response = await dispatcher.DispatchAsync(new(
            PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Mutate,
            Mutate: new(new(PbirAuthoringRpcContract.SnapshotHandleSchemaVersionV1, "missing", new("missing", "hash", 0)), request)));

        Assert.False(response.Succeeded);
        Assert.Equal(PbirAuthoringRpcErrorCategory.InvalidRequest, response.Error?.Category);
    }

    [Fact]
    public async Task Preview_ReturnsPlannerOwnedRenameAndDoesNotMaterialize()
    {
        var root = Directory.CreateTempSubdirectory("pbir-rpc-preview-");
        var source = Path.Combine(root.FullName, "source");
        var output = Path.Combine(root.FullName, "output");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(output);
        try
        {
            var generated = await new LocalPbirGenerationProviderService().GenerateAndVerifyAsync(CreateRequest(root.FullName));
            WriteArtifact(source, generated.Artifact!);
            var dispatcher = new PbirAuthoringRpcDispatcher();
            var import = await dispatcher.DispatchAsync(new(PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Import, Import: new(source)));
            var pageId = import.ImportResult!.Pages.Single().PageId;
            var request = RenameRequest(import.ImportResult.Snapshot, source, output, pageId, "Renamed", LocalPbirMutationRequestContract.SchemaVersionV1);

            var response = await dispatcher.DispatchAsync(new(
                PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Mutate,
                Mutate: new(import.ImportResult.Snapshot, request, PbirAuthoringMutationMode.Preview)));

            Assert.True(response.Succeeded, response.Error?.Summary);
            Assert.NotNull(response.MutateResult!.Preview);
            Assert.Equal(pageId, response.MutateResult.Preview.TargetPageId);
            Assert.Equal("overview", response.MutateResult.Preview.CurrentDisplayName);
            Assert.Equal("Renamed", response.MutateResult.Preview.ProposedDisplayName);
            Assert.True(response.MutateResult.Preview.ExecutionAdmissible);
            Assert.Equal(LocalPbirMutationOperationKind.RenamePage, response.MutateResult.Preview.Payload!.Kind);
            Assert.Contains(response.MutateResult.Preview.Diffs!, diff => diff.Kind == LocalPbirMutationSemanticDiffKind.PageRenamed && diff.ObjectId == pageId);
            Assert.False(Directory.Exists(Path.Combine(output, "mutated")));
            Assert.Null(response.MutateResult.Artifact);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Preview_SameNameIsAdvisoryNoOpAndCannotExecute()
    {
        var root = Directory.CreateTempSubdirectory("pbir-rpc-noop-");
        var source = Path.Combine(root.FullName, "source");
        Directory.CreateDirectory(source);
        try
        {
            var generated = await new LocalPbirGenerationProviderService().GenerateAndVerifyAsync(CreateRequest(root.FullName));
            WriteArtifact(source, generated.Artifact!);
            var dispatcher = new PbirAuthoringRpcDispatcher();
            var import = await dispatcher.DispatchAsync(new(PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Import, Import: new(source)));
            var pageId = import.ImportResult!.Pages.Single().PageId;
            var request = RenameRequest(import.ImportResult.Snapshot, source, root.FullName, pageId, "overview", LocalPbirMutationRequestContract.SchemaVersionV1);

            var response = await dispatcher.DispatchAsync(new(
                PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Mutate,
                Mutate: new(import.ImportResult.Snapshot, request, PbirAuthoringMutationMode.Preview)));

            Assert.True(response.Succeeded);
            Assert.True(response.MutateResult!.Preview!.IsNoOp);
            Assert.False(response.MutateResult.Preview.ExecutionAdmissible);
            Assert.Equal(0, response.MutateResult.ChangedPageCount);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Execute_ReturnsNewArtifactHandleAndLeavesImportedSnapshotUnchanged()
    {
        var root = Directory.CreateTempSubdirectory("pbir-rpc-execute-rename-");
        var source = Path.Combine(root.FullName, "source");
        var output = Path.Combine(root.FullName, "output");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(output);
        try
        {
            var generated = await new LocalPbirGenerationProviderService().GenerateAndVerifyAsync(CreateRequest(root.FullName));
            WriteArtifact(source, generated.Artifact!);
            var sourceBefore = File.ReadAllText(Directory.GetFiles(source, "page.json", SearchOption.AllDirectories).Single());
            var dispatcher = new PbirAuthoringRpcDispatcher();
            var import = await dispatcher.DispatchAsync(new(PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Import, Import: new(source)));
            var pageId = import.ImportResult!.Pages.Single().PageId;
            var snapshot = import.ImportResult.Snapshot;
            var request = RenameRequest(snapshot, source, output, pageId, "Executive Summary", LocalPbirMutationRequestContract.SchemaVersionV1);

            var response = await dispatcher.DispatchAsync(new(
                PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Mutate,
                Mutate: new(snapshot, request, PbirAuthoringMutationMode.Execute)));

            Assert.True(response.Succeeded, response.Error?.Summary);
            Assert.NotNull(response.MutateResult!.Artifact);
            Assert.NotNull(response.MutateResult.Comparison);
            Assert.Equal(
                response.MutateResult.Comparison!.After.Score - response.MutateResult.Comparison.Before.Score,
                response.MutateResult.Comparison.ScoreDelta,
                precision: 10);
            Console.WriteLine($"Phase48 timing dispatch={response.Timing.DispatchMilliseconds} orchestration={response.Timing.OrchestrationMilliseconds} planning={response.Timing.PlanningMilliseconds} preview={response.Timing.PreviewMilliseconds} serialization={response.Timing.SerializationMilliseconds} analyzerBefore={response.Timing.AnalyzerBeforeMilliseconds} analyzerAfter={response.Timing.AnalyzerMilliseconds}");
            Assert.Contains(pageId, response.MutateResult.Preview!.AffectedPageIds);
            Assert.Equal(sourceBefore, File.ReadAllText(Directory.GetFiles(source, "page.json", SearchOption.AllDirectories).Single()));

            var analysis = await dispatcher.DispatchAsync(new(
                PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Analyze,
                Analyze: new(Artifact: response.MutateResult.Artifact)));
            Assert.True(analysis.Succeeded, analysis.Error?.Summary);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    private static LocalPbirMutationRequest RenameRequest(
        PbirAuthoringSnapshotHandle snapshot,
        string source,
        string output,
        string pageId,
        string displayName,
        string schemaVersion) => new(
        schemaVersion, "phase47-rename", source, output, "mutated",
        [new(LocalPbirMutationOperationKind.RenamePage, new(PageId: pageId), DisplayName: displayName)]);

    private static LocalPbirGenerationRequest CreateRequest(string outputBase) => new(
        LocalPbirGenerationRequestContract.SchemaVersionV1, "phase45-rpc-mutate", "Sales", "overview", "Overview",
        "revenue-card", "card", "Sales.SemanticModel", "Revenue", "Sales", "Revenue",
        new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc), outputBase, "generated");

    private static void WriteArtifact(string root, PbirDeployableArtifact artifact)
    {
        foreach (var file in artifact.Files)
        {
            var path = Path.Combine(root, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file.Content);
        }
    }
}
