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
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
