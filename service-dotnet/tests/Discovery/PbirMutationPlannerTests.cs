using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirMutationPlannerTests
{
    [Fact]
    public void Planner_ResolvesExistingVisualAndExecutorPreservesPageIdentity()
    {
        var snapshot = CreateSnapshot();
        var request = new LocalPbirMutationRequest(
            LocalPbirMutationRequestContract.SchemaVersionV1, "m1", "source", "output", "target",
            [new(LocalPbirMutationOperationKind.ResizeVisual, new(PageId: "page-1", VisualId: "visual-1"), Layout: new(10, 20, 300, 200))]);
        var plan = new PbirMutationPlanner().Plan(snapshot, request);
        var result = new PbirMutationExecutor().Execute(plan);
        Assert.True(plan.IsValid);
        Assert.Equal("page-1", result.IrState.Ir!.Pages.Single().PageId);
        Assert.Equal(300, result.IrState.Ir.Visuals.Single().Layout!.Width);
    }

    [Fact]
    public void Planner_AcceptsRenamePageAndRejectsInvalidDisplayNames()
    {
        var snapshot = CreateSnapshot();
        var planner = new PbirMutationPlanner();

        var valid = planner.Plan(snapshot, CreateRenameRequest(snapshot, "Renamed Page"));
        var empty = planner.Plan(snapshot, CreateRenameRequest(snapshot, "  "));
        var unknown = planner.Plan(snapshot, CreateRenameRequest(snapshot, "Renamed Page", "missing-page"));

        Assert.True(valid.IsValid);
        Assert.Single(valid.Operations);
        Assert.Contains(empty.Diagnostics, diagnostic => diagnostic.Code == "PBIR46-PAGE-001" && diagnostic.Field == "displayName");
        Assert.Contains(unknown.Diagnostics, diagnostic => diagnostic.Code == "PBIR42-TARGET-001");
    }

    [Fact]
    public void Planner_RejectsInvalidPagePositionsAndDuplicateTargets()
    {
        var snapshot = CreateSnapshot();
        var planner = new PbirMutationPlanner();

        var invalidMove = planner.Plan(snapshot, new(
            LocalPbirMutationRequestContract.SchemaVersionV1, "move-page", "source", "output", "target",
            [new(LocalPbirMutationOperationKind.MovePage, new(PageId: "page-1"), Order: -1)]));
        var duplicateTarget = planner.Plan(snapshot, new(
            LocalPbirMutationRequestContract.SchemaVersionV1, "duplicate", "source", "output", "target",
            [new(LocalPbirMutationOperationKind.ResizeVisual, new(VisualId: "visual-1"), Layout: new(0, 0, 100, 100)),
             new(LocalPbirMutationOperationKind.ResizeVisual, new(VisualId: "visual-1"), Layout: new(0, 0, 200, 200))]));

        Assert.Contains(invalidMove.Diagnostics, diagnostic => diagnostic.Code == "PBIR48-PAGE-INVALID-POSITION");
        Assert.Contains(duplicateTarget.Diagnostics, diagnostic => diagnostic.Code == "PBIR48-DUPLICATE-TARGET");
    }

    [Fact]
    public void Planner_ProducesTypedSemanticDiffsForCuratedOperations()
    {
        var snapshot = CreateTwoPageSnapshot();
        var planner = new PbirMutationPlanner();
        var cases = new[]
        {
            (LocalPbirMutationOperationKind.AddPage, new LocalPbirMutationOperation(LocalPbirMutationOperationKind.AddPage, Page: new("page-3", "Page 3", 2))),
            (LocalPbirMutationOperationKind.RemovePage, new LocalPbirMutationOperation(LocalPbirMutationOperationKind.RemovePage, new(PageId: "page-2"))),
            (LocalPbirMutationOperationKind.MovePage, new LocalPbirMutationOperation(LocalPbirMutationOperationKind.MovePage, new(PageId: "page-2"), Order: 0)),
            (LocalPbirMutationOperationKind.MoveVisual, new LocalPbirMutationOperation(LocalPbirMutationOperationKind.MoveVisual, new(PageId: "page-2", VisualId: "visual-1"))),
            (LocalPbirMutationOperationKind.ResizeVisual, new LocalPbirMutationOperation(LocalPbirMutationOperationKind.ResizeVisual, new(VisualId: "visual-1"), Layout: new(8, 8, 120, 120))),
        };

        foreach (var (kind, operation) in cases)
        {
            var plan = planner.Plan(snapshot, new(LocalPbirMutationRequestContract.SchemaVersionV1, kind.ToString(), "source", "output", "target", [operation]));
            Assert.True(plan.IsValid, string.Join(" | ", plan.Diagnostics.Select(diagnostic => diagnostic.Code)));
            Assert.Contains(plan.Diffs, diff => diff.Kind == kind switch
            {
                LocalPbirMutationOperationKind.AddPage => LocalPbirMutationSemanticDiffKind.PageAdded,
                LocalPbirMutationOperationKind.RemovePage => LocalPbirMutationSemanticDiffKind.PageRemoved,
                LocalPbirMutationOperationKind.MovePage => LocalPbirMutationSemanticDiffKind.PageMoved,
                LocalPbirMutationOperationKind.MoveVisual => LocalPbirMutationSemanticDiffKind.VisualMoved,
                _ => LocalPbirMutationSemanticDiffKind.VisualResized,
            });
        }
    }

    [Fact]
    public void Planner_AssignsDeterministicIdentityWhenAddPageOmitsPageId()
    {
        var snapshot = CreateSnapshot();
        var request = new LocalPbirMutationRequest(
            LocalPbirMutationRequestContract.SchemaVersionV1, "add-page", "source", "output", "target",
            [new(LocalPbirMutationOperationKind.AddPage, Page: new("", "Details", 1))]);

        var first = new PbirMutationPlanner().Plan(snapshot, request);
        var second = new PbirMutationPlanner().Plan(snapshot, request);

        Assert.True(first.IsValid);
        Assert.Equal(first.Diffs.Single().ObjectId, second.Diffs.Single().ObjectId);
        Assert.StartsWith("page:", first.Diffs.Single().ObjectId, StringComparison.Ordinal);
    }

    private static LocalPbirMutationRequest CreateRenameRequest(
        PbirLocalReportImportSnapshot snapshot,
        string displayName,
        string? pageId = null) =>
        new(
            LocalPbirMutationRequestContract.SchemaVersionV1,
            "rename-page",
            snapshot.SourceDirectory,
            "output",
            "target",
            [new(LocalPbirMutationOperationKind.RenamePage, new(PageId: pageId ?? "page-1"), DisplayName: displayName)]);

    private static PbirLocalReportImportSnapshot CreateSnapshot()
    {
        var visual = new PbirIntermediateRepresentationVisual("visual-1", "page-1", "card", "", "visual-1", [], 0, new(0, 0, 100, 100), []);
        var ir = new PbirIntermediateRepresentation(
            new("ir", PbirIntermediateRepresentationContract.SchemaVersionV1, DateTime.UnixEpoch), new("manifest", "spec"),
            [new("page-1", "page-folder", "", "", 0, "Page")], [visual],
            [new("semantic", "page-1", [], [], ["visual-1"], [], "", [])],
            new("page-1", [], [], []), new([], [], [], []), new([], [], []), new([], []), new("", "", ""));
        var state = new PbirIntermediateRepresentationState(ir, new(PbirIntermediateRepresentationValidationDiagnostics.Empty), PbirIntermediateRepresentationReadinessState.ReadyForSerializer);
        return new(PbirLocalReportImportContract.SchemaVersionV1, "source", state, new Dictionary<string, string> { ["page-1"] = "page-folder" }, new Dictionary<string, string> { ["visual-1"] = "visual-folder" }, new Dictionary<string, string>(), []);
    }

    private static PbirLocalReportImportSnapshot CreateTwoPageSnapshot()
    {
        var visual = new PbirIntermediateRepresentationVisual("visual-1", "page-1", "card", "", "visual-1", [], 0, new(0, 0, 100, 100), []);
        var ir = new PbirIntermediateRepresentation(
            new("ir", PbirIntermediateRepresentationContract.SchemaVersionV1, DateTime.UnixEpoch), new("manifest", "spec"),
            [new("page-1", "page-folder", "", "", 0, "Page 1"), new("page-2", "page-folder-2", "", "", 1, "Page 2")], [visual],
            [new("semantic", "page-1", [], [], ["visual-1"], [], "", [])],
            new("page-1", [], [], []), new([], [], [], []), new([], [], []), new([], []), new("", "", ""));
        var state = new PbirIntermediateRepresentationState(ir, new(PbirIntermediateRepresentationValidationDiagnostics.Empty), PbirIntermediateRepresentationReadinessState.ReadyForSerializer);
        return new(PbirLocalReportImportContract.SchemaVersionV1, "source", state, new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>(), []);
    }
}
