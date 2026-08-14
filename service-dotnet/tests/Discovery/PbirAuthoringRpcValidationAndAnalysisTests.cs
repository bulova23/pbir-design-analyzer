using PowerBIModelingService.PbirAuthoringRpc;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using PowerBIModelingService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirAuthoringRpcValidationAndAnalysisTests
{
    [Fact]
    public async Task Validate_UsesTheStoredGeneratedArtifactAndSchemaValidator()
    {
        var output = Directory.CreateTempSubdirectory("pbir-rpc-validate-");
        try
        {
            var dispatcher = new PbirAuthoringRpcDispatcher();
            var generated = await dispatcher.DispatchAsync(new(
                PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Generate,
                new(new(CreateRequest(output.FullName)))));

            var validated = await dispatcher.DispatchAsync(new(
                PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Validate,
                Validate: new(generated.GenerateResult!.Artifact!)));

            Assert.True(validated.Succeeded, string.Join(" | ", validated.Diagnostics.Select(diagnostic => diagnostic.Code)));
            Assert.True(validated.ValidateResult!.IsValid);
            Assert.Equal(generated.ArtifactIdentity, validated.ArtifactIdentity);
        }
        finally
        {
            output.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Analyze_ReturnsTheExistingAnalyzerScoreForTheGeneratedReport()
    {
        var output = Directory.CreateTempSubdirectory("pbir-rpc-analyze-");
        try
        {
            var request = CreateRequest(output.FullName);
            var direct = await new LocalPbirGenerationProviderService().GenerateAndVerifyAsync(request);
            var response = await new PbirAuthoringRpcDispatcher().DispatchAsync(new(
                PbirAuthoringRpcContract.SchemaVersionV1, PbirAuthoringRpcOperation.Analyze,
                Analyze: new(Path.Combine(output.FullName, request.TargetDirectoryName))));

            Assert.True(response.Succeeded, string.Join(" | ", response.Diagnostics.Select(diagnostic => diagnostic.Code)));
            Assert.Equal(direct.RoundTrip!.Score.CompositeScore, response.Analyzer!.Score);
            Assert.Equal(direct.RoundTrip.PageCount, response.Analyzer.PageCount);
        }
        finally
        {
            output.Delete(recursive: true);
        }
    }

    private static LocalPbirGenerationRequest CreateRequest(string outputBase) => new(
        LocalPbirGenerationRequestContract.SchemaVersionV1, "phase45-rpc-analysis", "Sales", "overview", "Overview",
        "revenue-card", "card", "Sales.SemanticModel", "Revenue", "Sales", "Revenue",
        new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc), outputBase, "report");
}
