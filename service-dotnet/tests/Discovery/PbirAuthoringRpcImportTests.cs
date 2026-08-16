using PowerBIModelingService.PbirAuthoringRpc;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirAuthoringRpcImportTests
{
    [Fact]
    public async Task Import_ReturnsOpaqueVersionedHandleForSupportedPbirDirectory()
    {
        var source = Directory.CreateTempSubdirectory("pbir-rpc-import-");
        try
        {
            var generated = await new LocalPbirGenerationProviderService().GenerateAndVerifyAsync(CreateRequest(source.FullName));
            WriteArtifact(source.FullName, generated.Artifact!);

            var response = await new PbirAuthoringRpcDispatcher().DispatchAsync(new(
                PbirAuthoringRpcContract.SchemaVersionV1,
                PbirAuthoringRpcOperation.Import,
                Import: new(source.FullName)));

            Assert.True(response.Succeeded, string.Join(" | ", response.Diagnostics.Select(diagnostic => diagnostic.Code)));
            Assert.Equal(PbirAuthoringRpcContract.SnapshotHandleSchemaVersionV1, response.ImportResult!.Snapshot.SchemaVersion);
            Assert.NotEmpty(response.ImportResult.Snapshot.SnapshotId);
            Assert.DoesNotContain("irState", System.Text.Json.JsonSerializer.Serialize(response.ImportResult), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fileHashes", System.Text.Json.JsonSerializer.Serialize(response.ImportResult), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            source.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Import_RejectsDirectoryOutsideSupportedPbirShape()
    {
        var source = Directory.CreateTempSubdirectory("pbir-rpc-invalid-import-");
        try
        {
            var response = await new PbirAuthoringRpcDispatcher().DispatchAsync(new(
                PbirAuthoringRpcContract.SchemaVersionV1,
                PbirAuthoringRpcOperation.Import,
                Import: new(source.FullName)));

            Assert.False(response.Succeeded);
            Assert.Equal(PbirAuthoringRpcErrorCategory.ImportFailed, response.Error?.Category);
        }
        finally
        {
            source.Delete(recursive: true);
        }
    }

    private static LocalPbirGenerationRequest CreateRequest(string outputBase) => new(
        LocalPbirGenerationRequestContract.SchemaVersionV1, "phase45-rpc-import", "Sales", "overview", "Overview",
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
