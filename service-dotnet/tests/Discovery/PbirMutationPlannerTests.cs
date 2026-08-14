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
}
