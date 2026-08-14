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

    [Fact]
    public void Contract_ExposesPhase37TypedAuthoringCatalog()
    {
        Assert.Equal("local-pbir-generation-request/v2", LocalPbirGenerationRequestContract.SchemaVersionV2);
        Assert.Contains("card", LocalPbirGenerationProviderContract.SupportedVisualTypes);
        Assert.Contains("table", LocalPbirGenerationProviderContract.SupportedVisualTypes);
    }

    [Fact]
    public void Contract_ExposesPhase38TypedAuthoringContract()
    {
        Assert.Equal("local-pbir-generation-request/v3", LocalPbirGenerationRequestContract.SchemaVersionV3);
        Assert.Equal(LocalPbirGenerationInteractionMode.CrossFilter, new LocalPbirGenerationInteractionSettings(LocalPbirGenerationInteractionMode.CrossFilter).Mode);
    }

    [Fact]
    public void Generate_Phase38FormattedReport_EmitsThemesFiltersFormattingAndInteractions()
    {
        var outputBase = Directory.CreateTempSubdirectory("pbir-phase38-");
        try
        {
            var result = new LocalPbirGenerationProviderService().Generate(CreatePhase38Request(outputBase.FullName));

            Assert.Equal(LocalPbirGenerationReadinessState.Generated, result.Readiness);
            Assert.True(result.Validation!.IsValid);
            var report = result.Artifact!.Files.Single(file => file.RelativePath == "definition/report.json").Content;
            var page = result.Artifact.Files.First(file => file.RelativePath.EndsWith("/page.json", StringComparison.Ordinal) && file.Content.Contains("HighlightFilter", StringComparison.Ordinal)).Content;
            var visual = result.Artifact.Files.First(file => file.RelativePath.EndsWith("/visual.json", StringComparison.Ordinal) && file.Content.Contains("fontSize", StringComparison.Ordinal)).Content;
            Assert.Contains("customTheme", report, StringComparison.Ordinal);
            Assert.Contains("filterConfig", report, StringComparison.Ordinal);
            Assert.Contains("visualInteractions", page, StringComparison.Ordinal);
            Assert.Contains("fontSize", visual, StringComparison.Ordinal);
            Assert.Contains("formatString", visual, StringComparison.Ordinal);
            Assert.Contains("HighlightFilter", page, StringComparison.Ordinal);
        }
        finally
        {
            outputBase.Delete(recursive: true);
        }
    }

    [Fact]
    public void Generate_Phase38SameRequest_ProducesByteIdenticalArtifactAndHashes()
    {
        var outputBase = Directory.CreateTempSubdirectory("pbir-phase38-");
        try
        {
            var request = CreatePhase38Request(outputBase.FullName);
            var first = new LocalPbirGenerationProviderService().Generate(request);
            var second = new LocalPbirGenerationProviderService().Generate(request);

            Assert.Equal(first.Artifact!.Hashes, second.Artifact!.Hashes);
            Assert.Equal(first.Manifest!.Hashes, second.Manifest!.Hashes);
            Assert.Equal(first.Artifact.Files.Select(file => (file.RelativePath, file.Content, file.HashSha256)), second.Artifact.Files.Select(file => (file.RelativePath, file.Content, file.HashSha256)));
        }
        finally
        {
            outputBase.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    public void Generate_Phase38InvalidColor_FailsClosed(string color)
    {
        var request = CreatePhase38Request() with
        {
            Theme = new LocalPbirGenerationTheme("Sales", BackgroundColor: new LocalPbirGenerationColor(color))
        };

        var result = new LocalPbirGenerationProviderService().Generate(request);

        Assert.Equal(LocalPbirGenerationReadinessState.Rejected, result.Readiness);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == "PBIR38-FORMAT-COLOR-001");
    }

    [Fact]
    public void Generate_Phase38DuplicateFilterField_FailsClosed()
    {
        var request = CreatePhase38Request() with
        {
            ReportFilters =
            [
                new("region-a", LocalPbirGenerationBindingKind.Dimension, "Sales", "Region", "North"),
                new("region-b", LocalPbirGenerationBindingKind.Dimension, "Sales", "Region", "South")
            ]
        };

        var result = new LocalPbirGenerationProviderService().Generate(request);

        Assert.Equal(LocalPbirGenerationReadinessState.Rejected, result.Readiness);
        Assert.Contains(result.Diagnostics, item => item.Code == "PBIR38-FILTER-DUPLICATE-001");
    }

    [Fact]
    public void Generate_Phase38UnsupportedVisualFormatting_FailsClosed()
    {
        var request = CreatePhase38Request() with
        {
            Visuals = [CreatePhase38Request().Visuals[0] with { Authoring = new(Table: new()) }]
        };

        var result = new LocalPbirGenerationProviderService().Generate(request);

        Assert.Equal(LocalPbirGenerationReadinessState.Rejected, result.Readiness);
        Assert.Contains(result.Diagnostics, item => item.Code == "PBIR38-FORMAT-UNSUPPORTED-001");
    }

    [Fact]
    public async Task Generate_Phase38FormattedReport_RoundTripsThroughAnalyzer()
    {
        var outputBase = Directory.CreateTempSubdirectory("pbir-phase38-");
        try
        {
            var result = await new LocalPbirGenerationProviderService().GenerateAndVerifyAsync(CreatePhase38Request(outputBase.FullName));
            Assert.True(result.Readiness == LocalPbirGenerationReadinessState.RoundTripVerified, string.Join(" | ", result.Diagnostics.Select(item => $"{item.Code}:{item.Field}:{item.Message}").Concat(result.Materialization?.Diagnostics.Items.Select(item => $"{item.Code}:{item.Field}:{item.Message}") ?? [])));
            Assert.Equal(2, result.RoundTrip!.PageCount);
            Assert.Equal(3, result.RoundTrip.VisualCount);
            Assert.NotNull(result.Performance);
            Console.WriteLine($"PHASE38_TIMING generationMs={result.Performance!.GenerationMilliseconds} materializationMs={result.Performance.MaterializationMilliseconds} analyzerMs={result.Performance.AnalyzerMilliseconds}");
            Console.WriteLine($"PHASE38_SCORE composite={result.RoundTrip.Score.CompositeScore}");
        }
        finally
        {
            outputBase.Delete(recursive: true);
        }
    }

    [Fact]
    public void Generate_Phase36Request_RemainsCompatibleWithTypedNormalization()
    {
        var result = new LocalPbirGenerationProviderService().Generate(CreateRequest());

        Assert.Equal(LocalPbirGenerationReadinessState.Generated, result.Readiness);
        Assert.Equal(1, result.GeneratedPageCount);
        Assert.Equal(1, result.GeneratedVisualCount);
    }

    [Fact]
    public void Generate_Phase37DuplicateIdentity_FailsClosedWithoutPartialArtifact()
    {
        var request = CreatePhase37Request() with
        {
            Pages =
            [
                CreatePage("overview", "Overview", 0),
                CreatePage("overview", "Detail", 1)
            ]
        };

        var result = new LocalPbirGenerationProviderService().Generate(request);

        Assert.Equal(LocalPbirGenerationReadinessState.Rejected, result.Readiness);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == "PBIR37-REQUEST-DUPLICATE-ID-001");
    }

    [Fact]
    public void Generate_Phase37InvalidLayout_FailsClosedWithoutPartialArtifact()
    {
        var request = CreatePhase37Request() with
        {
            Visuals =
            [
                CreateCard("overview-card", "overview", 0, new(0, 0, 640, 400)),
                CreateTable("overview-table", "overview", 1, new(320, 200, 640, 400))
            ]
        };

        var result = new LocalPbirGenerationProviderService().Generate(request);

        Assert.Equal(LocalPbirGenerationReadinessState.Rejected, result.Readiness);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == "PBIR37-LAYOUT-OVERLAP-001");
    }

    [Fact]
    public void Generate_Phase37MultiPageRequest_ProducesTypedCardAndTableArtifact()
    {
        var outputBase = Directory.CreateTempSubdirectory("pbir-phase37-");
        try
        {
            var result = new LocalPbirGenerationProviderService().Generate(CreatePhase37Request(outputBase.FullName));

            Assert.Equal(LocalPbirGenerationReadinessState.Generated, result.Readiness);
            Assert.Equal(2, result.GeneratedPageCount);
            Assert.Equal(3, result.GeneratedVisualCount);
            Assert.NotNull(result.Artifact);
            Assert.True(result.Validation!.IsValid);
            Assert.Equal(3, result.Artifact!.Files.Count(file => file.RelativePath.EndsWith("/visual.json", StringComparison.Ordinal)));
            Assert.Contains(result.Manifest!.SupportedFeatures, feature => feature == "table");
        }
        finally
        {
            outputBase.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Generate_Phase37MultiPageRequest_MaterializesAndScoresRoundTrip()
    {
        var outputBase = Directory.CreateTempSubdirectory("pbir-phase37-");
        try
        {
            var result = await new LocalPbirGenerationProviderService().GenerateAndVerifyAsync(
                CreatePhase37Request(outputBase.FullName));

            Assert.Equal(LocalPbirGenerationReadinessState.RoundTripVerified, result.Readiness);
            Assert.Equal(PbirMaterializationOrchestrationOutcome.Applied, result.Materialization!.Outcome);
            Assert.Equal(2, result.RoundTrip!.PageCount);
            Assert.Equal(3, result.RoundTrip.VisualCount);
            Assert.Equal(2, result.RoundTrip.Score.PageScores!.Count);
            Assert.NotNull(result.Performance);
            Assert.True(result.Performance!.GenerationMilliseconds >= 0);
            Console.WriteLine($"PHASE37_TIMING generationMs={result.Performance.GenerationMilliseconds} materializationMs={result.Performance.MaterializationMilliseconds} analyzerMs={result.Performance.AnalyzerMilliseconds}");
            Console.WriteLine($"PHASE37_SCORE composite={result.RoundTrip.Score.CompositeScore}");
        }
        finally
        {
            outputBase.Delete(recursive: true);
        }
    }

    [Fact]
    public void Generate_Phase37SameRequest_ProducesByteIdenticalArtifactAndHashes()
    {
        var outputBase = Directory.CreateTempSubdirectory("pbir-phase37-");
        try
        {
            var request = CreatePhase37Request(outputBase.FullName);
            var first = new LocalPbirGenerationProviderService().Generate(request);
            var second = new LocalPbirGenerationProviderService().Generate(request);

            Assert.Equal(first.Artifact!.Hashes, second.Artifact!.Hashes);
            Assert.Equal(first.Manifest!.Hashes, second.Manifest!.Hashes);
            Assert.Equal(
                first.Artifact.Files.Select(file => (file.RelativePath, file.Content, file.HashSha256)),
                second.Artifact.Files.Select(file => (file.RelativePath, file.Content, file.HashSha256)));
            Console.WriteLine($"PHASE37_HASH artifact={first.Artifact.Hashes.ArtifactHash} manifest={first.Manifest.Hashes.ManifestHash} fileSet={first.Artifact.Hashes.FileSetHash} lineage={first.Artifact.Hashes.LineageHash}");
        }
        finally
        {
            outputBase.Delete(recursive: true);
        }
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

    private static LocalPbirGenerationRequestV2 CreatePhase37Request(string? outputBase = null) =>
        new(
            SchemaVersion: LocalPbirGenerationRequestContract.SchemaVersionV2,
            RequestId: "phase37-sales-authoring",
            ReportName: "Sales",
            DatasetPath: "Sales.SemanticModel",
            GeneratedUtc: new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc),
            OutputBaseDirectory: outputBase ?? Path.GetTempPath(),
            TargetDirectoryName: "phase37-output",
            Pages:
            [
                CreatePage("overview", "Overview", 0),
                CreatePage("detail", "Detail", 1)
            ],
            Visuals:
            [
                CreateCard("overview-card", "overview", 0, new(0, 0, 320, 160)),
                CreateTable("overview-table", "overview", 1, new(0, 176, 640, 360)),
                CreateTable("detail-table", "detail", 0, new(0, 0, 960, 520))
            ]);

    private static LocalPbirGenerationPage CreatePage(string id, string displayName, int order) =>
        new(id, displayName, order);

    private static LocalPbirGenerationRequestV3 CreatePhase38Request(string? outputBase = null) =>
        new(
            SchemaVersion: LocalPbirGenerationRequestContract.SchemaVersionV3,
            RequestId: "phase38-sales-rich-authoring",
            ReportName: "Sales",
            DatasetPath: "Sales.SemanticModel",
            GeneratedUtc: new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc),
            OutputBaseDirectory: outputBase ?? Path.GetTempPath(),
            TargetDirectoryName: "phase38-output",
            Pages:
            [
                new("overview", "Overview", 0, new(
                    Background: new("#F7F9FC"),
                    Filters: [new("page-region", LocalPbirGenerationBindingKind.Dimension, "Sales", "Region", "North")])),
                CreatePage("detail", "Detail", 1)
            ],
            Visuals:
            [
                CreateCard("overview-card", "overview", 0, new(0, 0, 320, 160)) with
                {
                    Authoring = new(Card: new(
                        Title: "Revenue",
                        Label: new(FontFamily: "Aptos", FontSize: 18, FontWeight: LocalPbirGenerationFontWeight.Bold, Color: new("#123456")),
                        NumberFormat: "#,##0.00",
                        Box: new(Background: new("#FFFFFF"), BorderColor: new("#123456"), BorderWidth: 1, Padding: new(8, 8, 8, 8))))
                },
                CreateTable("overview-table", "overview", 1, new(0, 176, 640, 360)) with
                {
                    Authoring = new(Table: new(
                        Title: "Regional Sales",
                        Header: new(FontSize: 12, FontWeight: LocalPbirGenerationFontWeight.Bold, Color: new("#FFFFFF")),
                        Row: new(FontSize: 11),
                        AlternateRowColor: new("#EEF3F8"),
                        NumberFormat: "#,##0",
                        WidthBehavior: LocalPbirGenerationTableWidthBehavior.FitToPage))
                },
                CreateTable("detail-table", "detail", 0, new(0, 0, 960, 520))
            ],
            Theme: new("Sales Light", "Aptos", 12, new("#F7F9FC"), new("#123456"), [new("#123456"), new("#E67E22")]),
            ReportFilters: [new("report-year", LocalPbirGenerationBindingKind.Dimension, "Sales", "Year", "2026")],
            Metadata: new("Codex", "Phase 38 representative formatted report", "Sales Rich Authoring"),
            Interaction: new(LocalPbirGenerationInteractionMode.CrossHighlight),
            Layout: new(24, 16, LocalPbirGenerationTextAlignment.Left, 8));

    private static LocalPbirGenerationVisual CreateCard(
        string id,
        string pageId,
        int order,
        LocalPbirGenerationLayout layout) =>
        new(
            id,
            pageId,
            "card",
            order,
            layout,
            [new("revenue", "Revenue", LocalPbirGenerationBindingKind.Measure, "Sales", "Revenue")]);

    private static LocalPbirGenerationVisual CreateTable(
        string id,
        string pageId,
        int order,
        LocalPbirGenerationLayout layout) =>
        new(
            id,
            pageId,
            "table",
            order,
            layout,
            [
                new("region", "Region", LocalPbirGenerationBindingKind.Dimension, "Sales", "Region"),
                new("revenue", "Revenue", LocalPbirGenerationBindingKind.Measure, "Sales", "Revenue")
            ]);
}
