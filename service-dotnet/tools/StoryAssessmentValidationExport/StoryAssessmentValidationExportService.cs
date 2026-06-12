using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;

namespace StoryAssessmentValidationExport;

public sealed class StoryAssessmentValidationExportService
{
    private readonly PbirScoringService _scoringService;

    public StoryAssessmentValidationExportService()
    {
        _scoringService = new PbirScoringService(
            new PbirProjectService(NullLogger<PbirProjectService>.Instance),
            NullLogger<PbirScoringService>.Instance);
    }

    public async Task<StoryAssessmentValidationExportReport> CreateReportAsync(string reportPath)
    {
        var result = await _scoringService.ScoreAsync(reportPath).ConfigureAwait(false);
        var pages = (result.PageScores is { Count: > 0 }
            ? result.PageScores
            : [CreateSyntheticPageScore(result)])
            .Select(ShapePageExport)
            .ToList();

        return new StoryAssessmentValidationExportReport
        {
            Title = "Internal Validation Export",
            ContractNotice = "Not User-Facing Contract",
            ReportPath = string.IsNullOrWhiteSpace(result.ReportPath) ? reportPath : result.ReportPath,
            GeneratedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            Pages = pages,
            CrossPageNarrative = ShapeCrossPageNarrative(GetInternalProperty(result, "InternalCrossPageNarrativeAssessment")),
        };
    }

    public async Task<string> ExportAsync(string reportPath, string? outputDirectory = null)
    {
        var report = await CreateReportAsync(reportPath).ConfigureAwait(false);
        var exportDirectory = outputDirectory ?? GetDefaultOutputDirectory(report.ReportPath);
        Directory.CreateDirectory(exportDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(exportDirectory, "story-assessment-validation.json"),
            StoryAssessmentValidationJsonRenderer.Render(report)).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            Path.Combine(exportDirectory, "story-assessment-validation.md"),
            StoryAssessmentValidationMarkdownRenderer.Render(report)).ConfigureAwait(false);

        return exportDirectory;
    }

    private static string GetDefaultOutputDirectory(string reportRootPath)
    {
        var parent = Directory.GetParent(reportRootPath)?.FullName ?? reportRootPath;
        return Path.Combine(parent, "story-assessment-validation-export");
    }

    private static PageScore CreateSyntheticPageScore(ScoreResult result)
    {
        var pageScore = new PageScore
        {
            PageId = result.ScoredPageId ?? "single-page",
            PageName = result.ScoredPageName ?? "Scored Page",
            GestaltScore = result.GestaltScore,
            CognitiveLoadScore = result.CognitiveLoadScore,
            DataInkScore = result.DataInkScore,
            AccessibilityScore = result.AccessibilityScore,
            VisualBestPracticesScore = result.VisualBestPracticesScore,
            EnterpriseGovernanceScore = result.EnterpriseGovernanceScore,
            StephenFewScore = result.StephenFewScore,
            TufteScore = result.TufteScore,
            GraphicalPerceptionScore = result.GraphicalPerceptionScore,
            DensityScore = result.DensityScore,
            NarrativeScore = result.NarrativeScore,
            Feedback = result.Feedback,
            Recommendations = result.Recommendations,
            FrameworkWeights = result.FrameworkWeights,
            DataVisualCount = result.DataVisualCount,
            NavigationVisualCount = result.NavigationVisualCount,
            HiddenVisualCount = result.HiddenVisualCount,
            VisualMetadata = result.VisualMetadata,
            InferredStorySummary = result.InferredStorySummary,
            PageIntentProfile = result.PageIntentProfile,
            ActionabilityBreakdown = result.ActionabilityBreakdown,
            BenchmarkComparison = result.BenchmarkComparison,
            ReportConsistency = result.ReportConsistencySummary,
            ScoringError = null,
            PerStateScores = result.PerStateScores,
        };

        SetInternalProperty(pageScore, "InternalStorySignalRegistry", GetInternalProperty(result, "InternalStorySignalRegistry"));
        SetInternalProperty(pageScore, "InternalStoryAssessmentArchetypeClassification", GetInternalProperty(result, "InternalStoryAssessmentArchetypeClassification"));
        SetInternalProperty(pageScore, "InternalStorySpecialPageAssessment", GetInternalProperty(result, "InternalStorySpecialPageAssessment"));
        SetInternalProperty(pageScore, "InternalStorySemanticCoherenceAssessment", GetInternalProperty(result, "InternalStorySemanticCoherenceAssessment"));
        SetInternalProperty(pageScore, "InternalStoryFilterTopologyAssessment", GetInternalProperty(result, "InternalStoryFilterTopologyAssessment"));
        SetInternalProperty(pageScore, "InternalStoryGapAssessment", GetInternalProperty(result, "InternalStoryGapAssessment"));
        SetInternalProperty(pageScore, "InternalStoryConfidenceBreakdownAssessment", GetInternalProperty(result, "InternalStoryConfidenceBreakdownAssessment"));

        return pageScore;
    }

    private static StoryAssessmentValidationExportPage ShapePageExport(PageScore page)
    {
        var signalRegistry = GetInternalProperty(page, "InternalStorySignalRegistry");
        var archetypeClassification = GetInternalProperty(page, "InternalStoryAssessmentArchetypeClassification");
        var specialPageAssessment = GetInternalProperty(page, "InternalStorySpecialPageAssessment");
        var semanticCoherence = GetInternalProperty(page, "InternalStorySemanticCoherenceAssessment");
        var filterTopology = GetInternalProperty(page, "InternalStoryFilterTopologyAssessment");
        var storyGaps = GetInternalProperty(page, "InternalStoryGapAssessment");
        var confidenceBreakdown = GetInternalProperty(page, "InternalStoryConfidenceBreakdownAssessment");

        var promotionStates = new SortedSet<string>(StringComparer.Ordinal);
        var surfaceScopes = new SortedSet<string>(StringComparer.Ordinal);
        CollectPromotionAndScope(specialPageAssessment, promotionStates, surfaceScopes);
        CollectPromotionAndScope(archetypeClassification, promotionStates, surfaceScopes);
        CollectPromotionAndScope(semanticCoherence, promotionStates, surfaceScopes);
        CollectPromotionAndScope(filterTopology, promotionStates, surfaceScopes);
        CollectPromotionAndScope(storyGaps, promotionStates, surfaceScopes);
        CollectPromotionAndScope(confidenceBreakdown, promotionStates, surfaceScopes);

        return new StoryAssessmentValidationExportPage
        {
            PageName = page.PageName,
            DetectedStory = page.InferredStorySummary?.InferredStory ?? page.InferredStorySummary?.StoryArchetype ?? "No public story detected.",
            SignalRegistrySummary = ShapeSignalRegistrySummary(signalRegistry),
            SpecialPageResult = ShapeSpecialPageResult(specialPageAssessment),
            ArchetypeClassification = ShapeArchetypeClassification(archetypeClassification),
            ArchetypeSuppressionStatus = ShapeArchetypeSuppressionStatus(archetypeClassification),
            SemanticCoherenceResult = ShapeSemanticCoherence(semanticCoherence),
            CoherenceTuningDetails = ShapeCoherenceTuningDetails(semanticCoherence),
            CompetingStoryStatus = GetStringProperty(semanticCoherence, "CompetingStoryStatus"),
            FilterTopologyResult = ShapeFilterTopology(filterTopology),
            StoryGaps = ShapeStoryGaps(storyGaps),
            ConfidenceBreakdown = ShapeConfidenceBreakdown(confidenceBreakdown),
            PromotionStates = promotionStates.ToList(),
            SurfaceScopes = surfaceScopes.ToList(),
        };
    }

    private static StoryAssessmentValidationExportCrossPageNarrative? ShapeCrossPageNarrative(object? assessment)
    {
        if (assessment is null)
        {
            return null;
        }

        var pages = GetEnumerableProperty(assessment, "Pages").ToList();
        var pageRoles = pages.Select(page => new StoryAssessmentValidationExportPageRole
        {
            PageName = GetStringPropertyOrFallback(page, "PageName", "Unknown Page"),
            Role = GetStringPropertyOrFallback(GetInternalProperty(page, "RoleAssignment"), "PrimaryRole", "Unavailable"),
            Confidence = GetStringPropertyOrFallback(GetInternalProperty(page, "RoleAssignment"), "Confidence", "Unavailable"),
        }).ToList();
        var orphanDecisions = pages.Select(page => new StoryAssessmentValidationExportOrphanDecision
        {
            PageName = GetStringPropertyOrFallback(page, "PageName", "Unknown Page"),
            OrphanState = GetStringPropertyOrFallback(page, "OrphanState", "Unavailable"),
        }).ToList();
        var scoreSummary = GetInternalProperty(assessment, "ScoreSummary");
        var dimensions = GetEnumerablePropertyIfPresent(scoreSummary, "Dimensions")
            .Select(dimension => new StoryAssessmentValidationExportNarrativeDimension
            {
                DimensionId = GetStringPropertyOrFallback(dimension, "DimensionId", "Unavailable"),
                Score = double.TryParse(GetStringProperty(dimension, "Score"), out var score) ? score : 0d,
                Confidence = GetStringPropertyOrFallback(dimension, "Confidence", "Unavailable"),
            })
            .ToList();
        var gaps = GetEnumerableProperty(assessment, "Gaps")
            .Select(gap => new StoryAssessmentValidationExportNarrativeGap
            {
                GapId = GetStringPropertyOrFallback(gap, "GapId", "unavailable"),
                StableId = GetStringPropertyOrFallback(gap, "StableId", "unavailable"),
                Summary = GetStringPropertyOrFallback(gap, "Summary", "No internal report-level narrative gaps available."),
                Confidence = GetStringPropertyOrFallback(gap, "Confidence", "Unavailable"),
            })
            .ToList();

        return new StoryAssessmentValidationExportCrossPageNarrative
        {
            DominantReportObjective = GetStringPropertyOrFallback(assessment, "DominantReportObjective", "Unavailable"),
            MainNarrativePath = GetStringListPropertyOrFallback(
                GetInternalProperty(assessment, "Graph"),
                "MainNarrativePath",
                "No internal main narrative path available."),
            PageRoles = pageRoles.Count > 0
                ? pageRoles
                :
                [
                    new StoryAssessmentValidationExportPageRole
                    {
                        PageName = "Unknown Page",
                        Role = "Unavailable",
                        Confidence = "Unavailable",
                    },
                ],
            OrphanDecisions = orphanDecisions.Count > 0
                ? orphanDecisions
                :
                [
                    new StoryAssessmentValidationExportOrphanDecision
                    {
                        PageName = "Unknown Page",
                        OrphanState = "Unavailable",
                    },
                ],
            DimensionScores = dimensions.Count > 0
                ? dimensions
                :
                [
                    new StoryAssessmentValidationExportNarrativeDimension
                    {
                        DimensionId = "Unavailable",
                        Confidence = "Unavailable",
                    },
                ],
            ReportLevelGaps = gaps.Count > 0
                ? gaps
                :
                [
                    new StoryAssessmentValidationExportNarrativeGap
                    {
                        GapId = "unavailable",
                        StableId = "unavailable",
                        Summary = "No internal report-level narrative gaps available.",
                        Confidence = "Unavailable",
                    },
                ],
        };
    }

    private static IReadOnlyList<string> ShapeSignalRegistrySummary(object? signalRegistry)
    {
        if (signalRegistry is null)
        {
            return ["No internal signal registry available."];
        }

        return GetEnumerableProperty(signalRegistry, "Entries")
            .Select(entry =>
            {
                var id = GetStringProperty(entry, "Id");
                var fired = GetStringProperty(entry, "Fired");
                var surfaceScope = GetStringProperty(entry, "SurfaceScope");
                var promotionState = GetStringProperty(entry, "PromotionState");
                return $"{id}: {(string.Equals(fired, "True", StringComparison.Ordinal) ? "fired" : "missing")} [{surfaceScope}/{promotionState}]";
            })
            .ToList();
    }

    private static string ShapeSpecialPageResult(object? specialPageAssessment)
    {
        if (specialPageAssessment is null)
        {
            return "No internal special page result available.";
        }

        return $"PageType={GetStringProperty(specialPageAssessment, "PageType")}; " +
               $"Confidence={GetStringProperty(specialPageAssessment, "Confidence")}; " +
               $"TreatAsPrimaryNarrativePage={GetStringProperty(specialPageAssessment, "TreatAsPrimaryNarrativePage")}; " +
               $"SuppressNormalStoryGaps={GetStringProperty(specialPageAssessment, "SuppressNormalStoryGaps")}; " +
               $"SuppressGenericArchetypePromotion={GetStringProperty(specialPageAssessment, "SuppressGenericArchetypePromotion")}";
    }

    private static string ShapeArchetypeClassification(object? archetypeClassification)
    {
        if (archetypeClassification is null)
        {
            return "No internal archetype classification available.";
        }

        return $"Best Fit={GetStringProperty(archetypeClassification, "BestFitArchetypeId")}; " +
               $"SurfaceScope={GetStringProperty(archetypeClassification, "SurfaceScope")}; " +
               $"PromotionState={GetStringProperty(archetypeClassification, "PromotionState")}";
    }

    private static string ShapeArchetypeSuppressionStatus(object? archetypeClassification)
    {
        if (archetypeClassification is null)
        {
            return "No archetype suppression status available.";
        }

        return $"Disposition={GetStringProperty(archetypeClassification, "ArchetypePromotionDisposition")}; " +
               $"SuppressedBySpecialPageType={GetStringProperty(archetypeClassification, "SuppressedBySpecialPageType")}";
    }

    private static string ShapeSemanticCoherence(object? semanticCoherence)
    {
        if (semanticCoherence is null)
        {
            return "No internal semantic coherence result available.";
        }

        return $"Classification={GetStringProperty(semanticCoherence, "CoherenceClassification")}; " +
               $"DominantConcept={GetStringProperty(semanticCoherence, "DominantConcept")}; " +
               $"Confidence={GetStringProperty(semanticCoherence, "Confidence")}; " +
               $"PromotionState={GetStringProperty(semanticCoherence, "PromotionState")}; " +
               $"ScoringMode={GetStringProperty(semanticCoherence, "ScoringMode")}";
    }

    private static IReadOnlyList<string> ShapeCoherenceTuningDetails(object? semanticCoherence)
    {
        if (semanticCoherence is null)
        {
            return ["No semantic coherence tuning details available."];
        }

        return GetStringListProperty(semanticCoherence, "TuningDetails");
    }

    private static string ShapeFilterTopology(object? filterTopology)
    {
        if (filterTopology is null)
        {
            return "No internal filter topology result available.";
        }

        var slicerCount = GetStringProperty(filterTopology, "SlicerCount");
        var pageFilterCount = GetStringProperty(filterTopology, "PageFilterCount");
        var reportFilterCount = GetStringProperty(filterTopology, "ReportFilterCount");
        var surfaceScope = GetStringProperty(filterTopology, "SurfaceScope");
        var promotionState = GetStringProperty(filterTopology, "PromotionState");
        return $"Slicers={slicerCount}; PageFilters={pageFilterCount}; ReportFilters={reportFilterCount}; SurfaceScope={surfaceScope}; PromotionState={promotionState}";
    }

    private static IReadOnlyList<StoryAssessmentValidationExportGap> ShapeStoryGaps(object? storyGaps)
    {
        if (storyGaps is null)
        {
            return
            [
                new StoryAssessmentValidationExportGap
                {
                    GapId = "unavailable",
                    Description = "No internal story gaps available.",
                    RemediationLayer = "Unavailable",
                    Confidence = "Unavailable",
                    IsFutureContractCandidate = false,
                },
            ];
        }

        return GetEnumerableProperty(storyGaps, "Gaps")
            .Select(gap =>
            {
                return new StoryAssessmentValidationExportGap
                {
                    GapId = GetStringProperty(gap, "GapId"),
                    Description = GetStringProperty(gap, "Description"),
                    RemediationLayer = GetStringProperty(gap, "RemediationLayer"),
                    Confidence = GetStringProperty(gap, "Confidence"),
                    IsFutureContractCandidate = bool.TryParse(GetStringProperty(gap, "IsFutureContractCandidate"), out var candidate) && candidate,
                };
            })
            .ToList();
    }

    private static IReadOnlyList<StoryAssessmentValidationExportConfidenceDimension> ShapeConfidenceBreakdown(object? confidenceBreakdown)
    {
        if (confidenceBreakdown is null)
        {
            return
            [
                new StoryAssessmentValidationExportConfidenceDimension
                {
                    DimensionId = "Unavailable",
                    DimensionLabel = "Unavailable",
                    Rating = "NotAssessed",
                    ConfidenceDrivers = ["No internal confidence breakdown available."],
                    ConfidenceReducers = [],
                    MissingSignals = [],
                    EvidenceReferences = [],
                    Explanation = "No internal confidence breakdown available.",
                    Actionability = "NotActionable",
                    PromotionState = "Internal",
                    SurfaceScope = "PbirSpecific",
                },
            ];
        }

        return GetEnumerableProperty(confidenceBreakdown, "Dimensions")
            .Select(dimension => new StoryAssessmentValidationExportConfidenceDimension
            {
                DimensionId = GetStringProperty(dimension, "DimensionId"),
                DimensionLabel = GetStringProperty(dimension, "DimensionLabel"),
                Rating = GetStringProperty(dimension, "Rating"),
                ConfidenceDrivers = GetStringListProperty(dimension, "ConfidenceDrivers"),
                ConfidenceReducers = GetStringListProperty(dimension, "ConfidenceReducers"),
                MissingSignals = GetStringListProperty(dimension, "MissingSignals"),
                EvidenceReferences = GetEnumerableProperty(dimension, "EvidenceReferences")
                    .Select(reference => $"{GetStringProperty(reference, "SourceType")}:{GetStringProperty(reference, "ReferenceId")}")
                    .ToList(),
                Explanation = GetStringProperty(dimension, "Explanation"),
                Actionability = GetStringProperty(dimension, "Actionability"),
                PromotionState = GetStringProperty(dimension, "PromotionState"),
                SurfaceScope = GetStringProperty(dimension, "SurfaceScope"),
            })
            .ToList();
    }

    private static void CollectPromotionAndScope(object? value, ISet<string> promotionStates, ISet<string> surfaceScopes)
    {
        if (value is null)
        {
            return;
        }

        AddIfNotBlank(promotionStates, TryGetStringProperty(value, "PromotionState"));
        AddIfNotBlank(surfaceScopes, TryGetStringProperty(value, "SurfaceScope"));

        foreach (var child in GetEnumerablePropertyIfPresent(value, "ArchetypeResults"))
        {
            AddIfNotBlank(promotionStates, TryGetStringProperty(child, "PromotionState"));
            AddIfNotBlank(surfaceScopes, TryGetStringProperty(child, "SurfaceScope"));
        }

        foreach (var child in GetEnumerablePropertyIfPresent(value, "Signals"))
        {
            AddIfNotBlank(promotionStates, TryGetStringProperty(child, "PromotionState"));
            AddIfNotBlank(surfaceScopes, TryGetStringProperty(child, "SurfaceScope"));
        }

        foreach (var child in GetEnumerablePropertyIfPresent(value, "Gaps"))
        {
            AddIfNotBlank(promotionStates, TryGetStringProperty(child, "PromotionState"));
        }

        foreach (var child in GetEnumerablePropertyIfPresent(value, "Dimensions"))
        {
            AddIfNotBlank(promotionStates, TryGetStringProperty(child, "PromotionState"));
            AddIfNotBlank(surfaceScopes, TryGetStringProperty(child, "SurfaceScope"));
        }
    }

    private static object? GetInternalProperty(object source, string propertyName)
    {
        return source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(source);
    }

    private static void SetInternalProperty(object source, string propertyName, object? value)
    {
        source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(source, value);
    }

    private static string GetStringProperty(object? source, string propertyName)
    {
        return TryGetStringProperty(source, propertyName) ?? string.Empty;
    }

    private static string GetStringPropertyOrFallback(object? source, string propertyName, string fallback)
    {
        return string.IsNullOrWhiteSpace(TryGetStringProperty(source, propertyName))
            ? fallback
            : GetStringProperty(source, propertyName);
    }

    private static string? TryGetStringProperty(object? source, string propertyName)
    {
        return source?.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(source)?.ToString();
    }

    private static IReadOnlyList<object> GetEnumerableProperty(object source, string propertyName)
    {
        return GetEnumerablePropertyIfPresent(source, propertyName).ToList();
    }

    private static IEnumerable<object> GetEnumerablePropertyIfPresent(object? source, string propertyName)
    {
        if (source is null)
        {
            return [];
        }

        var value = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(source);
        return value is System.Collections.IEnumerable enumerable
            ? enumerable.Cast<object>()
            : [];
    }

    private static IReadOnlyList<string> GetStringListProperty(object source, string propertyName)
    {
        return GetEnumerablePropertyIfPresent(source, propertyName)
            .Select(item => item?.ToString() ?? string.Empty)
            .ToList();
    }

    private static IReadOnlyList<string> GetStringListPropertyOrFallback(object? source, string propertyName, string fallback)
    {
        var values = GetEnumerablePropertyIfPresent(source, propertyName)
            .Select(item => item?.ToString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();

        return values.Count > 0 ? values : [fallback];
    }

    private static void AddIfNotBlank(ISet<string> set, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            set.Add(value);
        }
    }
}
