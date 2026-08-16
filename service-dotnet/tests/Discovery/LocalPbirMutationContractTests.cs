using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class LocalPbirMutationContractTests
{
    [Fact]
    public void MutationContract_IsAdditiveAndClosed()
    {
        Assert.Equal("local-pbir-mutation-request/v1", LocalPbirMutationRequestContract.SchemaVersionV1);
        Assert.Equal("local-pbir-mutation-result/v1", LocalPbirMutationResultContract.SchemaVersionV1);
        Assert.Contains(LocalPbirMutationOperationKind.AddPage, LocalPbirMutationOperationKindCatalog.All);
        Assert.Contains(LocalPbirMutationOperationKind.UpdateSlicer, LocalPbirMutationOperationKindCatalog.All);
    }

    [Fact]
    public void AuthoringMutationInventory_HasOneTypedMergeBoundary()
    {
        Assert.Equal(PbirAuthoringMutationClassification.TypedAndMergeable,
            PbirAuthoringMutationInventory.Classify(LocalPbirMutationOperationKind.RenamePage));
        Assert.Equal(PbirAuthoringMutationClassification.TypedAndMergeable,
            PbirAuthoringMutationInventory.Classify(LocalPbirMutationOperationKind.ResizeVisual));
        Assert.Equal(PbirAuthoringMutationClassification.PreservedButNotAuthorable,
            PbirAuthoringMutationInventory.Classify(LocalPbirMutationOperationKind.UpdateTheme));
        Assert.Equal(PbirAuthoringMutationClassification.Unsupported,
            PbirAuthoringMutationInventory.Classify(LocalPbirMutationOperationKind.RemoveVisual));
    }
}
