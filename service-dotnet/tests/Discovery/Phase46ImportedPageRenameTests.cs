using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase46ImportedPageRenameTests
{
    [Fact]
    public void RenamePage_PreservesIdentityUnrelatedDocumentsAndFidelity()
    {
        var sourcePage = "{\"$schema\":\"page-schema\",\"name\":\"page-folder\",\"displayName\":\"Before\",\"visualInteractions\":[{\"source\":\"visual-1\"}],\"futureProperty\":{\"keep\":true}}";
        var secondPage = "{\"$schema\":\"page-schema\",\"name\":\"second-folder\",\"displayName\":\"Second\",\"future\":true}";
        var visual = "{\"$schema\":\"visual-schema\",\"name\":\"visual-1\",\"position\":{\"x\":1,\"y\":2,\"width\":3,\"height\":4}}";
        var envelope = new PbirAuthoringEnvelope(
            PbirAuthoringEnvelopeContract.SchemaVersionV1,
            [
                Item(PbirAuthoringOwnerKind.Page, "page-1", "pages/page-folder/page.json", "page-folder", sourcePage),
                Item(PbirAuthoringOwnerKind.Page, "page-2", "pages/second-folder/page.json", "second-folder", secondPage),
                Item(PbirAuthoringOwnerKind.Visual, "visual-1", "pages/page-folder/visuals/visual-1/visual.json", "visual-1", visual)
            ],
            "source");
        var ir = CreateIr(envelope);
        var snapshot = new PbirLocalReportImportSnapshot(
            PbirLocalReportImportContract.SchemaVersionV1, "source", 
            new(ir, new(PbirIntermediateRepresentationValidationDiagnostics.Empty), PbirIntermediateRepresentationReadinessState.ReadyForSerializer),
            new Dictionary<string, string> { ["page-1"] = "page-folder", ["page-2"] = "second-folder" },
            new Dictionary<string, string> { ["visual-1"] = "visual-1" }, new Dictionary<string, string>(), []);
        var request = new LocalPbirMutationRequest(
            LocalPbirMutationRequestContract.SchemaVersionV1, "phase46-rename", "source", "output", "target",
            [new(LocalPbirMutationOperationKind.RenamePage, new(PageId: "page-1"), DisplayName: "After")]);

        var planner = new PbirMutationPlanner();
        var firstPlan = planner.Plan(snapshot, request);
        var first = new PbirAuthoringMergeService().Resolve(new PbirMutationExecutor().Execute(firstPlan).IrState.Ir!);
        var secondPlan = planner.Plan(snapshot, request);
        var second = new PbirAuthoringMergeService().Resolve(new PbirMutationExecutor().Execute(secondPlan).IrState.Ir!);
        var fidelity = new PbirAuthoringFidelityService().Compare(envelope, first);

        Assert.True(firstPlan.IsValid, string.Join("; ", firstPlan.Diagnostics.Select(x => x.Message)));
        Assert.Equal(["page-1"], firstPlan.AffectedPages);
        Assert.Equal(["page-1", "page-2"], firstPlan.Snapshot.IrState.Ir!.Pages.Select(page => page.PageId));
        Assert.Equal("page-folder", ir.Pages.Single(page => page.PageId == "page-1").PageIdentity);
        Assert.Equal("page-1", ir.Visuals.Single().PageId);
        Assert.Equal(first.Documents.Select(document => document.Content), second.Documents.Select(document => document.Content));
        Assert.Equal(first.ChangedPaths, second.ChangedPaths);
        Assert.True(fidelity.IsFidelityReady, string.Join(",", fidelity.UnexpectedPaths));
        Assert.Contains("pages/page-folder/page.json", fidelity.IntentionallyChanged);
        Assert.DoesNotContain("pages/second-folder/page.json", fidelity.ChangedPaths);
        Assert.DoesNotContain("pages/page-folder/visuals/visual-1/visual.json", fidelity.ChangedPaths);
        using var renamed = JsonDocument.Parse(first.Documents.Single(document => document.OwnerId == "page-1").Content);
        Assert.Equal("After", renamed.RootElement.GetProperty("displayName").GetString());
        Assert.Equal("page-folder", renamed.RootElement.GetProperty("name").GetString());
        Assert.True(renamed.RootElement.GetProperty("futureProperty").GetProperty("keep").GetBoolean());
    }

    [Fact]
    public void RenamePage_RejectsPageWithoutPinnedDisplayNameOwner()
    {
        using var source = JsonDocument.Parse("{\"$schema\":\"page-schema\",\"name\":\"page-folder\"}");
        var item = new PbirAuthoringEnvelopeItem(PbirAuthoringOwnerKind.Page, "page-1", "pages/page-folder/page.json", "schema", "1.0.0", PbirAuthoringPreservationClass.TypedSupported, source.RootElement.Clone(), source.RootElement.GetRawText(), "hash", ["$schema", "name"], new("page-folder", null, null));
        var ir = CreateIr(new(PbirAuthoringEnvelopeContract.SchemaVersionV1, [item], "source"));
        var snapshot = new PbirLocalReportImportSnapshot(PbirLocalReportImportContract.SchemaVersionV1, "source", new(ir, new(PbirIntermediateRepresentationValidationDiagnostics.Empty), PbirIntermediateRepresentationReadinessState.ReadyForSerializer), new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>(), []);
        var request = new LocalPbirMutationRequest(LocalPbirMutationRequestContract.SchemaVersionV1, "phase46-invalid-owner", "source", "output", "target", [new(LocalPbirMutationOperationKind.RenamePage, new(PageId: "page-1"), DisplayName: "After")]);

        var plan = new PbirMutationPlanner().Plan(snapshot, request);

        Assert.False(plan.IsValid);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "PBIR46-PAGE-002");
        Assert.Empty(new PbirMutationExecutor().Execute(plan).IrState.Ir!.Pages.Where(page => page.DisplayName == "After"));
    }

    private static PbirAuthoringEnvelopeItem Item(PbirAuthoringOwnerKind ownerKind, string ownerId, string path, string identity, string content)
    {
        using var document = JsonDocument.Parse(content);
        return new(ownerKind, ownerId, path, "schema", "1.0.0", PbirAuthoringPreservationClass.TypedSupported, document.RootElement.Clone(), content, $"hash-{ownerId}", document.RootElement.EnumerateObject().Select(property => property.Name).ToArray(), new(identity, null, null));
    }

    private static PbirIntermediateRepresentation CreateIr(PbirAuthoringEnvelope envelope) => new(
        new("ir", PbirIntermediateRepresentationContract.SchemaVersionV1, DateTime.UnixEpoch), new("manifest", "spec"),
        [new("page-1", "page-folder", "", "", 0, "Before"), new("page-2", "second-folder", "", "", 1, "Second")],
        [new("visual-1", "page-1", "card", "", "visual-1", [], 0, new(1, 2, 3, 4), [])],
        [], new("page-1", [], [], []), new([], [], [], []), new([], [], []), new([], []), new("", "", ""), null, envelope);
}
