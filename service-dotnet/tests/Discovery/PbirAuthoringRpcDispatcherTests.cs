using PowerBIModelingService.PbirAuthoringRpc;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirAuthoringRpcDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_RejectsNullRequestWithInvalidRequestWithoutExceptionText()
    {
        var response = await new PbirAuthoringRpcDispatcher().DispatchAsync(null);

        Assert.False(response.Succeeded);
        Assert.Equal(PbirAuthoringRpcErrorCategory.InvalidRequest, response.Error?.Category);
        Assert.NotEmpty(response.Error?.Summary);
        Assert.DoesNotContain("Exception", response.Error?.Summary ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DispatchAsync_RejectsWrongVersionAndMultipleOperationPayloads()
    {
        var response = await new PbirAuthoringRpcDispatcher().DispatchAsync(new(
            "other/v1",
            PbirAuthoringRpcOperation.Generate,
            new PbirAuthoringGenerateRequest(null!),
            new PbirAuthoringImportRequest("/tmp")));

        Assert.False(response.Succeeded);
        Assert.Equal(PbirAuthoringRpcErrorCategory.InvalidRequest, response.Error?.Category);
        Assert.Equal("PBIR-RPC-REQUEST-001", response.Error?.Code);
    }

    [Fact]
    public async Task DispatchAsync_RejectsUnknownOperationWithoutAddingAnOperation()
    {
        var response = await new PbirAuthoringRpcDispatcher().DispatchAsync(new(
            PbirAuthoringRpcContract.SchemaVersionV1,
            (PbirAuthoringRpcOperation)999));

        Assert.False(response.Succeeded);
        Assert.Equal(PbirAuthoringRpcErrorCategory.InvalidRequest, response.Error?.Category);
        Assert.Equal("PBIR-RPC-REQUEST-002", response.Error?.Code);
    }

    [Fact]
    public async Task DispatchAsync_RejectsGenerationUnionWithNoSelectedVersion()
    {
        var response = await new PbirAuthoringRpcDispatcher().DispatchAsync(new(
            PbirAuthoringRpcContract.SchemaVersionV1,
            PbirAuthoringRpcOperation.Generate,
            new(new PbirAuthoringGenerationRequest((LocalPbirGenerationRequest)null!))));

        Assert.False(response.Succeeded);
        Assert.Equal(PbirAuthoringRpcErrorCategory.InvalidRequest, response.Error?.Category);
        Assert.Equal("PBIR-RPC-REQUEST-003", response.Error?.Code);
    }

    [Fact]
    public async Task DispatchAsync_AnalyzeRequiresOnlyAnAnalysisInput()
    {
        var response = await new PbirAuthoringRpcDispatcher().DispatchAsync(new(
            PbirAuthoringRpcContract.SchemaVersionV1,
            PbirAuthoringRpcOperation.Analyze,
            Analyze: new()));

        Assert.False(response.Succeeded);
        Assert.Equal(PbirAuthoringRpcErrorCategory.InvalidRequest, response.Error?.Category);
        Assert.Equal("PBIR-RPC-REQUEST-003", response.Error?.Code);
        Assert.DoesNotContain("generation", response.Error?.Summary ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
