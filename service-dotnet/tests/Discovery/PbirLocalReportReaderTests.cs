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
}
