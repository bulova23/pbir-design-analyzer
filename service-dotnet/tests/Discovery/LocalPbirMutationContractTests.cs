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
}
