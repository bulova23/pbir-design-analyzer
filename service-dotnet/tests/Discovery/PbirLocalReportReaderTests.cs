using System.Text.Json;
using System.Text.Json.Nodes;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirLocalReportReaderTests
{
    [Fact]
    public void Reader_ImportsSerializerOutputAndPreservesFolderIdentities()
    {
        var generated = new LocalPbirGenerationProviderService().Generate(new LocalPbirGenerationRequest(
            LocalPbirGenerationRequestContract.SchemaVersionV1, "phase36-sales-card", "Sales", "Overview", "Overview", "RevenueCard", "card", "Sales.SemanticModel", "Revenue", "Sales", "Revenue", new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc), Path.GetTempPath(), "phase36-output"));
        Assert.True(generated.Artifact is not null, string.Join("; ", generated.Diagnostics.Select(x => x.Message)));
        var directory = Path.Combine(Path.GetTempPath(), $"pbir-reader-{Guid.NewGuid():N}");
        try
        {
            foreach (var file in generated.Artifact!.Files)
            {
                var path = Path.Combine(directory, file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, file.Content);
            }
            var imported = new PbirLocalReportReader().Import(directory);
            Assert.Empty(imported.Diagnostics);
            Assert.Equal(generated.Artifact.Files.Count, imported.FileHashes.Count);
            Assert.Single(imported.PageIdentities);
            Assert.Single(imported.VisualIdentities);
            Assert.Equal(imported.VisualIdentities.Keys.Single(), imported.IrState.Ir!.Visuals.Single().VisualId);
            Assert.NotNull(imported.IrState.Ir.AuthoringEnvelope);
            Assert.Contains(imported.IrState.Ir.AuthoringEnvelope!.Items, item => item.OwnerKind == PbirAuthoringOwnerKind.Page);
            Assert.Contains(imported.IrState.Ir.AuthoringEnvelope.Items, item => item.OwnerKind == PbirAuthoringOwnerKind.Visual);
            Assert.Equal(
                imported.PageIdentities[imported.IrState.Ir.Pages.Single().PageId],
                imported.IrState.Ir.AuthoringEnvelope.Items.Single(item => item.OwnerKind == PbirAuthoringOwnerKind.Page).Identity!.ImportedIdentity);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ReaderAndSerializer_RoundTrip_PreservesImportedOwnedDocuments()
    {
        var inputs = PbirDeployableSerializerServiceTests.CreateReadyInputs();
        var sourceArtifact = new PbirDeployableSerializerService().CreateArtifacts(inputs.IrState, inputs.SerializerRequest, inputs.DeployableRequest).Artifact!;
        var directory = Path.Combine(Path.GetTempPath(), $"pbir-roundtrip-{Guid.NewGuid():N}");
        try
        {
            foreach (var file in sourceArtifact.Files)
            {
                var path = Path.Combine(directory, file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, file.Content);
            }

            var imported = new PbirLocalReportReader().Import(directory);
            Assert.Empty(imported.Diagnostics);
            var resolved = new PbirAuthoringMergeService().Resolve(imported.IrState.Ir!);
            foreach (var item in imported.IrState.Ir!.AuthoringEnvelope!.Items.Where(item => item.SourceContent is not null))
            {
                var output = Assert.Single(resolved.Documents, document => document.RelativePath == item.OwnedRelativePath);
                Assert.Equal(item.SourceContent, output.Content);
            }
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Reader_RejectsUnsupportedOwnedSchemaInsteadOfReturningReadyEnvelope()
    {
        var inputs = PbirDeployableSerializerServiceTests.CreateReadyInputs();
        var sourceArtifact = new PbirDeployableSerializerService().CreateArtifacts(inputs.IrState, inputs.SerializerRequest, inputs.DeployableRequest).Artifact!;
        var directory = Path.Combine(Path.GetTempPath(), $"pbir-admission-{Guid.NewGuid():N}");
        try
        {
            foreach (var file in sourceArtifact.Files)
            {
                var path = Path.Combine(directory, file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, file.Content);
            }

            var pagePath = Directory.GetFiles(Path.Combine(directory, "definition", "pages"), "page.json", SearchOption.AllDirectories).Single();
            var page = JsonDocument.Parse(File.ReadAllText(pagePath)).RootElement.Clone();
            var pageObject = JsonNode.Parse(page.GetRawText())!.AsObject();
            pageObject["$schema"] = "https://schemas.example.invalid/unsupported-page.json";
            File.WriteAllText(pagePath, pageObject.ToJsonString());

            var imported = new PbirLocalReportReader().Import(directory);

            Assert.Equal(PbirIntermediateRepresentationReadinessState.Blocked, imported.IrState.Readiness);
            Assert.Contains(imported.Diagnostics, diagnostic => diagnostic.Code == "PBIR43-IMPORT-001");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Phase43Fixture_PreservesAdmittedFuturePropertyAndInteractionDuringResize()
    {
        var inputs = PbirDeployableSerializerServiceTests.CreateReadyInputs();
        var sourceArtifact = new PbirDeployableSerializerService().CreateArtifacts(inputs.IrState, inputs.SerializerRequest, inputs.DeployableRequest).Artifact!;
        var directory = Path.Combine(Path.GetTempPath(), $"pbir-phase43-fixture-{Guid.NewGuid():N}");
        try
        {
            foreach (var file in sourceArtifact.Files)
            {
                var content = file.Content;
                if (file.RelativePath.EndsWith("/visual.json", StringComparison.Ordinal))
                {
                    var visual = JsonNode.Parse(content)!.AsObject();
                    visual["futureAdmittedProperty"] = new JsonObject { ["keep"] = true };
                    content = visual.ToJsonString();
                }
                if (file.RelativePath.EndsWith("/page.json", StringComparison.Ordinal))
                {
                    var page = JsonNode.Parse(content)!.AsObject();
                    page["visualInteractions"] = new JsonArray
                    {
                        new JsonObject { ["source"] = "visual:revenue-card", ["target"] = "visual:revenue-table", ["type"] = "DataFilter" }
                    };
                    content = page.ToJsonString();
                }
                var path = Path.Combine(directory, file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, content);
            }

            var imported = new PbirLocalReportReader().Import(directory);
            var target = imported.IrState.Ir!.Visuals.First();
            var request = new LocalPbirMutationRequest(
                LocalPbirMutationRequestContract.SchemaVersionV1,
                "phase43-fixture-resize",
                directory,
                Path.GetTempPath(),
                "unused",
                [new(LocalPbirMutationOperationKind.ResizeVisual, new(PageId: target.PageId, VisualId: target.VisualId), Layout: new(11, 22, 333, 222))]);
            var plan = new PbirMutationPlanner().Plan(imported, request);
            var execution = new PbirMutationExecutor().Execute(plan);
            var resolved = new PbirAuthoringMergeService().Resolve(execution.IrState.Ir!);
            var fidelity = new PbirAuthoringFidelityService().Compare(imported.IrState.Ir.AuthoringEnvelope!, resolved);

            Assert.True(plan.IsValid);
            Assert.True(fidelity.IsFidelityReady, string.Join(",", fidelity.UnexpectedPaths));
            Assert.All(imported.IrState.Ir.AuthoringEnvelope!.Items.Where(item => item.OwnerKind == PbirAuthoringOwnerKind.Visual), item =>
            {
                var output = resolved.Documents.Single(document => document.RelativePath == item.OwnedRelativePath);
                Assert.Contains("futureAdmittedProperty", output.Content, StringComparison.Ordinal);
            });
            var pageOutput = resolved.Documents.Single(document => document.OwnerKind == PbirAuthoringOwnerKind.Page);
            Assert.Contains("visualInteractions", pageOutput.Content, StringComparison.Ordinal);
            Assert.Contains(target.VisualId, imported.VisualIdentities.Keys);
            Assert.Equal(target.VisualId, imported.IrState.Ir.AuthoringEnvelope.Items.Single(item => item.OwnerKind == PbirAuthoringOwnerKind.Visual && item.OwnerId == target.VisualId).Identity!.ImportedIdentity);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
