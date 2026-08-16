using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase45DirectTypedAuthoringBoundaryTests
{
    [Fact]
    public void GenerationProvider_IsDirectlyCallableWithTypedRequestAndPinnedSchema()
    {
        var request = CreateRequest();

        var result = new LocalPbirGenerationProviderService().Generate(request);

        Assert.Equal(LocalPbirGenerationReadinessState.Generated, result.Readiness);
        Assert.Equal(LocalPbirGenerationRequestContract.SchemaVersionV1, request.SchemaVersion);
        Assert.True(result.Validation!.IsValid);
        Assert.NotNull(result.Artifact);
        Assert.NotNull(result.Manifest);
    }

    [Fact]
    public void GenerationProvider_RejectsWrongPinnedSchemaWithoutPartialArtifact()
    {
        var result = new LocalPbirGenerationProviderService().Generate(
            CreateRequest() with { SchemaVersion = "local-pbir-generation-request/v0" });

        Assert.Equal(LocalPbirGenerationReadinessState.Rejected, result.Readiness);
        Assert.Null(result.Artifact);
        Assert.Null(result.Manifest);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Field == "schemaVersion");
    }

    [Fact]
    public void MergeBoundary_PreservesOpaqueContentWhileApplyingTypedLayout()
    {
        using var source = JsonDocument.Parse("{\"$schema\":\"schema\",\"position\":{\"x\":1,\"y\":2,\"width\":10,\"height\":20,\"z\":7},\"futureProperty\":{\"keep\":true}}");
        var item = new PbirAuthoringEnvelopeItem(
            PbirAuthoringOwnerKind.Visual, "visual-1", "pages/page-1/visuals/visual-1/visual.json", "schema", "1.0.0",
            PbirAuthoringPreservationClass.TypedSupported, source.RootElement.Clone(), source.RootElement.GetRawText(), "source-hash",
            ["$schema", "position", "futureProperty"], new("visual-1", null, null));
        var ir = CreateIr(new(PbirAuthoringEnvelopeContract.SchemaVersionV1, [item], "definition-hash")) with
        {
            Visuals = [new("visual-1", "page-1", "card", "slot-1", "semantic-1", [], 0, new(100, 110, 120, 130), [])]
        };

        var resolved = new PbirAuthoringMergeService().Resolve(ir);

        var document = Assert.Single(resolved.Documents);
        Assert.Contains("futureProperty", document.Content, StringComparison.Ordinal);
        Assert.Contains("\"x\": 100", document.Content, StringComparison.Ordinal);
        Assert.Equal("visual-1", item.Identity!.PreferredIdentity);

        var fidelity = new PbirAuthoringFidelityService().Compare(ir.AuthoringEnvelope!, resolved);
        Assert.DoesNotContain(fidelity.UnexpectedPaths, path => path.Contains("futureProperty", StringComparison.Ordinal));
    }

    [Fact]
    public void GenerationProvider_IsDeterministicAcrossDirectInvocations()
    {
        var first = new LocalPbirGenerationProviderService().Generate(CreateRequest());
        var second = new LocalPbirGenerationProviderService().Generate(CreateRequest());

        Assert.Equal(first.Manifest!.Hashes, second.Manifest!.Hashes);
        Assert.Equal(first.Artifact!.Files.Select(file => (file.RelativePath, file.Content, file.HashSha256)),
            second.Artifact!.Files.Select(file => (file.RelativePath, file.Content, file.HashSha256)));
    }

    [Fact]
    public void BoundaryDoesNotRegisterRpcOrExposeGenericMutationSurface()
    {
        var serviceAssembly = typeof(LocalPbirGenerationProviderService).Assembly;
        var rpcHost = serviceAssembly.GetTypes().Where(type => type.Name.Contains("RpcHost", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(rpcHost);
        Assert.DoesNotContain(typeof(LocalPbirGenerationProviderService).GetMethods(), method => method.Name.Contains("Json", StringComparison.OrdinalIgnoreCase));
    }

    private static LocalPbirGenerationRequest CreateRequest() => new(
        LocalPbirGenerationRequestContract.SchemaVersionV1,
        "phase45-direct-boundary",
        "Sales",
        "Overview",
        "Overview",
        "RevenueCard",
        "card",
        "Sales.SemanticModel",
        "Revenue",
        "Sales",
        "Revenue",
        new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc),
        Path.GetTempPath(),
        "phase45-direct-output");

    private static PbirIntermediateRepresentation CreateIr(PbirAuthoringEnvelope envelope) => new(
        new("ir", PbirIntermediateRepresentationContract.SchemaVersionV1, DateTime.UnixEpoch),
        new("manifest", "spec"), [], [], [], new("", [], [], []), new([], [], [], []),
        new([], [], []), new([], []), new("", "", ""), null, envelope);
}
