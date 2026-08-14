using System.Reflection;
using PowerBIModelingService.Services.Discovery.Models;
using PowerBIModelingService.PbirAuthoringRpc;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirAuthoringRpcContractTests
{
    [Fact]
    public void Contract_UsesIndependentVersionAndExactlyFiveClosedOperations()
    {
        Assert.Equal("pbir-authoring-rpc/v1", PbirAuthoringRpcContract.SchemaVersionV1);
        Assert.Equal(
            (IReadOnlyList<PbirAuthoringRpcOperation>)[PbirAuthoringRpcOperation.Generate, PbirAuthoringRpcOperation.Import,
             PbirAuthoringRpcOperation.Mutate, PbirAuthoringRpcOperation.Validate,
             PbirAuthoringRpcOperation.Analyze],
            PbirAuthoringRpcOperationCatalog.All);
    }

    [Fact]
    public void GenerationUnion_ContainsAllExistingRequestVersionsWithoutNewSchema()
    {
        Assert.Equal(
            (IReadOnlyList<PbirAuthoringGenerationRequestKind>)[PbirAuthoringGenerationRequestKind.V1, PbirAuthoringGenerationRequestKind.V2,
             PbirAuthoringGenerationRequestKind.V3, PbirAuthoringGenerationRequestKind.V4,
             PbirAuthoringGenerationRequestKind.V5, PbirAuthoringGenerationRequestKind.V6,
             PbirAuthoringGenerationRequestKind.V7],
            PbirAuthoringGenerationRequestKindCatalog.All);

        Assert.Equal(LocalPbirGenerationRequestContract.SchemaVersionV1,
            new PbirAuthoringGenerationRequest(new LocalPbirGenerationRequest(
                LocalPbirGenerationRequestContract.SchemaVersionV1, "id", "report", "page", "Page",
                "visual", "card", "dataset", "measure", "Model", "Measure", DateTime.UnixEpoch,
                "/tmp", "target")).KindSchemaVersion);
    }

    [Fact]
    public void SnapshotHandle_DoesNotExposeInternalIrOrRawFileContent()
    {
        var propertyNames = typeof(PbirAuthoringSnapshotHandle)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(property => property.Name != "EqualityContract")
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(
            (IReadOnlyList<string>)[nameof(PbirAuthoringSnapshotHandle.SchemaVersion), nameof(PbirAuthoringSnapshotHandle.SnapshotId),
             nameof(PbirAuthoringSnapshotHandle.SourceIdentity)], propertyNames);
        Assert.DoesNotContain(propertyNames, name => name.Contains("Ir", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("File", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ErrorCategories_AreClosedAndResultsCarrySharedObservations()
    {
        Assert.Equal(
            (IReadOnlyList<PbirAuthoringRpcErrorCategory>)[PbirAuthoringRpcErrorCategory.InvalidRequest, PbirAuthoringRpcErrorCategory.ImportFailed,
             PbirAuthoringRpcErrorCategory.UnsupportedAuthoring, PbirAuthoringRpcErrorCategory.MutationConflict,
             PbirAuthoringRpcErrorCategory.ValidationFailed, PbirAuthoringRpcErrorCategory.AnalyzerFailed,
             PbirAuthoringRpcErrorCategory.InternalFailure],
            PbirAuthoringRpcErrorCategoryCatalog.All);

        Assert.Contains(nameof(PbirAuthoringRpcResponse.Diagnostics), typeof(PbirAuthoringRpcResponse).GetProperties().Select(x => x.Name));
        Assert.Contains(nameof(PbirAuthoringRpcResponse.Timing), typeof(PbirAuthoringRpcResponse).GetProperties().Select(x => x.Name));
    }
}
