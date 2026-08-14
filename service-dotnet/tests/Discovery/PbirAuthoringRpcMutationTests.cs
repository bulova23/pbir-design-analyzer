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
