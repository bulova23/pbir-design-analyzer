using PowerBIModelingService.PbirAuthoringRpc;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirAuthoringRpcGenerationTests
{
    [Fact]
    public async Task Generate_V1DelegatesWithoutChangingArtifactIdentityOrAnalyzerSummary()
    {
        var output = Directory.CreateTempSubdirectory("pbir-rpc-generate-");
        try
        {
            var request = CreateRequest(output.FullName);
            var direct = await new LocalPbirGenerationProviderService().GenerateAndVerifyAsync(request);
            var rpc = await new PbirAuthoringRpcDispatcher().DispatchAsync(new(
                PbirAuthoringRpcContract.SchemaVersionV1,
                PbirAuthoringRpcOperation.Generate,
                new(new(request))));

            Assert.True(rpc.Succeeded, string.Join(" | ", rpc.Diagnostics.Select(diagnostic => diagnostic.Code)));
            Assert.Equal(direct.Artifact!.Hashes.ArtifactHash, rpc.ArtifactIdentity!.ArtifactHash);
            Assert.Equal(direct.Manifest!.Hashes.ManifestHash, rpc.ArtifactIdentity.ManifestHash);
            Assert.Equal(direct.RoundTrip!.Score.CompositeScore, rpc.Analyzer!.Score);
            Assert.True(rpc.Timing.DispatchMilliseconds >= 0);
            Assert.True(rpc.Timing.OrchestrationMilliseconds >= 0);
        }
        finally
        {
            output.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Generate_V2ThroughV7_PreservesExistingTypedRequestVersions()
    {
        var requests = new Func<string, object>[]
        {
            output => new LocalPbirGenerationRequestV2(LocalPbirGenerationRequestContract.SchemaVersionV2, "rpc-v2", "Sales", "Sales.SemanticModel", DateTime.UnixEpoch, output, "report", [new("overview", "Overview", 0)], [new("card", "overview", "card", 0, new(0, 0, 320, 160), [new("revenue", "Revenue", LocalPbirGenerationBindingKind.Measure, LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")])]),
            output => new LocalPbirGenerationRequestV3(LocalPbirGenerationRequestContract.SchemaVersionV3, "rpc-v3", "Sales", "Sales.SemanticModel", DateTime.UnixEpoch, output, "report", [new("overview", "Overview", 0)], [new("card", "overview", "card", 0, new(0, 0, 320, 160), [new("revenue", "Revenue", LocalPbirGenerationBindingKind.Measure, LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")])]),
            output => new LocalPbirGenerationRequestV4(LocalPbirGenerationRequestContract.SchemaVersionV4, "rpc-v4", "Sales", "Sales.SemanticModel", DateTime.UnixEpoch, output, "report", [new("overview", "Overview", 0)], [new("card", "overview", "card", 0, new(0, 0, 320, 160), [new("revenue", "Revenue", LocalPbirGenerationBindingKind.Measure, LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")])]),
            output => new LocalPbirGenerationRequestV5(LocalPbirGenerationRequestContract.SchemaVersionV5, "rpc-v5", "Sales", "Sales.SemanticModel", DateTime.UnixEpoch, output, "report", [new("overview", "Overview", 0)], [new("card", "overview", "card", 0, new(0, 0, 320, 160), [new("revenue", "Revenue", LocalPbirGenerationBindingKind.Measure, LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")])]),
            output => new LocalPbirGenerationRequestV6(LocalPbirGenerationRequestContract.SchemaVersionV6, "rpc-v6", "Sales", "Sales.SemanticModel", DateTime.UnixEpoch, output, "report", [new("overview", "Overview", 0)], [new("card", "overview", "card", 0, new(0, 0, 320, 160), [new("revenue", "Revenue", LocalPbirGenerationBindingKind.Measure, LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")])]),
            output => new LocalPbirGenerationRequestV7(LocalPbirGenerationRequestContract.SchemaVersionV7, "rpc-v7", "Sales", "Sales.SemanticModel", DateTime.UnixEpoch, output, "report", [new("overview", "Overview", 0)], [new("card", "overview", "card", 0, new(0, 0, 320, 160), [new("revenue", "Revenue", LocalPbirGenerationBindingKind.Measure, LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")])])
        };

        foreach (var (factory, index) in requests.Select((factory, index) => (factory, index)))
        {
            var output = Directory.CreateTempSubdirectory($"pbir-rpc-v{index + 2}-");
            try
            {
                var typed = factory(output.FullName);
                var response = await new PbirAuthoringRpcDispatcher().DispatchAsync(new(
                    PbirAuthoringRpcContract.SchemaVersionV1,
                    PbirAuthoringRpcOperation.Generate,
                    new(typed switch
                    {
                        LocalPbirGenerationRequestV2 v2 => new PbirAuthoringGenerationRequest(v2),
                        LocalPbirGenerationRequestV3 v3 => new PbirAuthoringGenerationRequest(v3),
                        LocalPbirGenerationRequestV4 v4 => new PbirAuthoringGenerationRequest(v4),
                        LocalPbirGenerationRequestV5 v5 => new PbirAuthoringGenerationRequest(v5),
                        LocalPbirGenerationRequestV6 v6 => new PbirAuthoringGenerationRequest(v6),
                        LocalPbirGenerationRequestV7 v7 => new PbirAuthoringGenerationRequest(v7),
                        _ => throw new InvalidOperationException()
                    })));
                Assert.True(response.Succeeded, $"v{index + 2}: {response.Error?.Summary}");
                Assert.NotNull(response.ArtifactIdentity);
            }
            finally
            {
                output.Delete(recursive: true);
            }
        }
    }

    private static LocalPbirGenerationRequest CreateRequest(string outputBase) => new(
        LocalPbirGenerationRequestContract.SchemaVersionV1,
        "phase45-rpc-v1",
        "Sales",
        "overview",
        "Overview",
        "revenue-card",
        "card",
        "Sales.SemanticModel",
        "Revenue",
        "Sales",
        "Revenue",
        new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc),
        outputBase,
        "report");
}
