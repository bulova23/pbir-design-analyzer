using PowerBIModelingService.Services.Discovery.Models;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class LocalPbirGenerationProviderServiceTests
{
    [Fact]
    public void Contract_ExposesBackendOnlyPhase36V1Shape()
    {
        Assert.Equal("local-pbir-generation-request/v1", LocalPbirGenerationRequestContract.SchemaVersionV1);
        Assert.Equal("card", LocalPbirGenerationProviderContract.SupportedVisualType);
        Assert.Contains("artifact", LocalPbirGenerationResultContract.RequiredFieldInventory);
        Assert.Contains("roundTrip.score", LocalPbirGenerationResultContract.RequiredFieldInventory);
    }

    [Theory]
    [InlineData("../Sales.SemanticModel")]
    [InlineData("/Sales.SemanticModel")]
    [InlineData("C:/Sales.SemanticModel")]
    public void Generate_UnsafeDatasetPath_FailsClosed(string path)
    {
        var result = new LocalPbirGenerationProviderService().Generate(CreateRequest() with { DatasetPath = path });

        Assert.Equal(LocalPbirGenerationReadinessState.Rejected, result.Readiness);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == "PBIR36-REQUEST-PATH-001");
    }

    [Fact]
    public void Generate_UnsupportedVisualType_FailsClosed()
    {
        var result = new LocalPbirGenerationProviderService().Generate(CreateRequest() with { VisualType = "table" });

        Assert.Equal(LocalPbirGenerationReadinessState.Rejected, result.Readiness);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == "PBIR36-REQUEST-VISUAL-001");
    }

    [Fact]
    public void Generate_ValidRequest_ProducesOnePageOneCardArtifact()
    {
        var result = new LocalPbirGenerationProviderService().Generate(CreateRequest());

        Assert.Equal(LocalPbirGenerationReadinessState.Generated, result.Readiness);
        Assert.NotNull(result.Artifact);
        Assert.True(result.Validation!.IsValid);
        Assert.Equal(6, result.Artifact!.Files.Count);
        Assert.Contains(result.Artifact.Files, file => file.RelativePath == "definition/report.json");
        Assert.Single(result.Artifact.Files.Where(file => file.RelativePath.EndsWith("/visual.json", StringComparison.Ordinal)));
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task Generate_ValidRequest_MaterializesAndScoresRoundTrip()
    {
        var outputBase = Directory.CreateTempSubdirectory("pbir-phase36-");
        try
        {
            var result = await new LocalPbirGenerationProviderService().GenerateAndVerifyAsync(
                CreateRequest(outputBase.FullName));

            Assert.Equal(LocalPbirGenerationReadinessState.RoundTripVerified, result.Readiness);
            Assert.Equal(PbirMaterializationOrchestrationOutcome.Applied, result.Materialization!.Outcome);
            Assert.NotNull(result.RoundTrip?.Score);
            Assert.Single(result.RoundTrip!.Score.PageScores!);
            Assert.Equal(1, result.RoundTrip.PageCount);
            Assert.Equal(1, result.RoundTrip.VisualCount);
        }
        finally
        {
            outputBase.Delete(recursive: true);
        }
    }

    [Fact]
    public void Generate_SameRequest_ProducesByteIdenticalArtifactAndHashes()
    {
        var outputBase = Directory.CreateTempSubdirectory("pbir-phase36-");
        try
        {
            var request = CreateRequest(outputBase.FullName);
            var first = new LocalPbirGenerationProviderService().Generate(request);
            var second = new LocalPbirGenerationProviderService().Generate(request);

            Assert.Equal(first.Artifact!.Hashes, second.Artifact!.Hashes);
            Assert.Equal(first.Manifest!.Hashes, second.Manifest!.Hashes);
            Assert.Equal(
                first.Artifact.Files.Select(file => (file.RelativePath, file.Content, file.HashSha256)),
                second.Artifact.Files.Select(file => (file.RelativePath, file.Content, file.HashSha256)));
        }
        finally
        {
            outputBase.Delete(recursive: true);
        }
    }

    [Fact]
    public void Generate_MissingMeasureField_FailsClosedWithoutPartialArtifact()
    {
        var result = new LocalPbirGenerationProviderService().Generate(CreateRequest() with { MeasureProperty = string.Empty });

        Assert.Equal(LocalPbirGenerationReadinessState.Rejected, result.Readiness);
        Assert.Null(result.Artifact);
        Assert.Null(result.Manifest);
        Assert.Contains(result.Diagnostics, item => item.Code == "PBIR36-REQUEST-ID-001");
    }

    private static LocalPbirGenerationRequest CreateRequest(string? outputBase = null) =>
        new(
            SchemaVersion: LocalPbirGenerationRequestContract.SchemaVersionV1,
            RequestId: "phase36-sales-card",
            ReportName: "Sales",
            PageId: "Overview",
            PageDisplayName: "Overview",
            VisualId: "RevenueCard",
            VisualType: "card",
            DatasetPath: "Sales.SemanticModel",
            MeasureToken: "Revenue",
            MeasureEntity: "Sales",
            MeasureProperty: "Revenue",
            GeneratedUtc: new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc),
            OutputBaseDirectory: outputBase ?? Path.GetTempPath(),
            TargetDirectoryName: "phase36-output");
}
