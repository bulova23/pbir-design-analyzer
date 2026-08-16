using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir.CrossPageNarrative;

internal static class PageNarrativeRoleClassifier
{
    public static CrossPageNarrativeRoleAssignment Classify(CrossPageNarrativePageInput page, int pageCount)
    {
        if (!string.Equals(page.SpecialPageType, "Unknown", StringComparison.Ordinal))
        {
            return new CrossPageNarrativeRoleAssignment
            {
                PrimaryRole = page.SpecialPageType switch
                {
                    "Tooltip" => CrossPageNarrativeRoleId.Tooltip,
                    "Qna" => CrossPageNarrativeRoleId.Qna,
                    "ReferenceLegal" => CrossPageNarrativeRoleId.ReferenceLegal,
                    "ValidationSandbox" => CrossPageNarrativeRoleId.ValidationSandbox,
                    _ => CrossPageNarrativeRoleId.SupportingContext,
                },
                Confidence = CrossPageNarrativeRoleConfidence.High,
                Evidence =
                [
                    $"specialPage:{page.SpecialPageType}",
                ],
            };
        }

        var evidence = new List<string>();
        var scoreByRole = new Dictionary<CrossPageNarrativeRoleId, int>
        {
            [CrossPageNarrativeRoleId.Overview] = 0,
            [CrossPageNarrativeRoleId.ExecutiveSummary] = 0,
            [CrossPageNarrativeRoleId.OperationalMonitor] = 0,
            [CrossPageNarrativeRoleId.ComparativeAnalysis] = 0,
            [CrossPageNarrativeRoleId.DiagnosticInvestigation] = 0,
            [CrossPageNarrativeRoleId.DetailDrill] = 0,
            [CrossPageNarrativeRoleId.ScenarioExploration] = 0,
            [CrossPageNarrativeRoleId.ExceptionAnalysis] = 0,
            [CrossPageNarrativeRoleId.SupportingContext] = 0,
        };

        var normalizedName = Normalize(page.PageName);
        var normalizedTitle = Normalize(page.VisualMetadata?.VisiblePageTitle);
        var normalizedStory = Normalize(page.InferredStory);
        var normalizedIntent = Normalize(page.IntentProfile);
        var normalizedArchetype = Normalize(page.StoryArchetype);
        var visualTypes = (page.VisualMetadata?.Visuals ?? [])
            .Select(visual => Normalize(visual.VisualType))
            .ToList();

        bool hasTable = visualTypes.Any(type => type.Contains("table", StringComparison.Ordinal));
        bool hasKpiCard = visualTypes.Any(type => type is "card" or "kpivisual" or "multirowcard");
        bool hasScenarioControl = page.VisualMetadata?.SlicerCount > 0 &&
                                  (normalizedName.Contains("scenario", StringComparison.Ordinal) ||
                                   normalizedStory.Contains("scenario", StringComparison.Ordinal));

        if (page.PageIndex == 0 && (normalizedName.Contains("overview", StringComparison.Ordinal) ||
                                    normalizedTitle.Contains("overview", StringComparison.Ordinal)))
        {
            scoreByRole[CrossPageNarrativeRoleId.Overview] += 4;
            evidence.Add("firstPageOverviewCue");
        }

        if (normalizedIntent.Contains("executive", StringComparison.Ordinal) ||
            normalizedStory.Contains("executive", StringComparison.Ordinal))
        {
            scoreByRole[CrossPageNarrativeRoleId.Overview] += 2;
            scoreByRole[CrossPageNarrativeRoleId.ExecutiveSummary] += 2;
            evidence.Add("executiveIntent");
        }

        if (normalizedArchetype.Contains("narrativewalkthrough", StringComparison.Ordinal))
        {
            scoreByRole[CrossPageNarrativeRoleId.Overview] += 1;
            scoreByRole[CrossPageNarrativeRoleId.ExecutiveSummary] += 1;
            evidence.Add("narrativeWalkthroughStory");
        }

        if (page.DrillPathPresent)
        {
            scoreByRole[CrossPageNarrativeRoleId.DetailDrill] += 3;
            evidence.Add("drillPathPresent");
        }

        if (hasTable)
        {
            scoreByRole[CrossPageNarrativeRoleId.DetailDrill] += 3;
            evidence.Add("tableDetailVisual");
        }

        if (normalizedName.Contains("detail", StringComparison.Ordinal) ||
            normalizedTitle.Contains("detail", StringComparison.Ordinal))
        {
            scoreByRole[CrossPageNarrativeRoleId.DetailDrill] += 2;
            evidence.Add("detailNamingCue");
        }

        if (normalizedArchetype.Contains("comparison", StringComparison.Ordinal))
        {
            scoreByRole[CrossPageNarrativeRoleId.ComparativeAnalysis] += 2;
            evidence.Add("comparisonStory");
        }

        if (hasScenarioControl)
        {
            scoreByRole[CrossPageNarrativeRoleId.ScenarioExploration] += 4;
            evidence.Add("scenarioControlCue");
        }

        if (hasKpiCard && normalizedIntent.Contains("operational", StringComparison.Ordinal))
        {
            scoreByRole[CrossPageNarrativeRoleId.OperationalMonitor] += 3;
            evidence.Add("operationalKpiCue");
        }

        if (normalizedName.Contains("exception", StringComparison.Ordinal) ||
            normalizedStory.Contains("exception", StringComparison.Ordinal))
        {
            scoreByRole[CrossPageNarrativeRoleId.ExceptionAnalysis] += 3;
            evidence.Add("exceptionCue");
        }

        if (normalizedName.Contains("diagnostic", StringComparison.Ordinal) ||
            normalizedStory.Contains("root cause", StringComparison.Ordinal))
        {
            scoreByRole[CrossPageNarrativeRoleId.DiagnosticInvestigation] += 3;
            evidence.Add("diagnosticCue");
        }

        var ordered = scoreByRole
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key)
            .ToList();

        var primaryRole = ordered[0].Value > 0
            ? ordered[0].Key
            : CrossPageNarrativeRoleId.SupportingContext;
        var primaryScore = ordered[0].Value;
        var competingScore = ordered.Count > 1 ? ordered[1].Value : 0;

        bool hasExecutiveConflict = primaryRole == CrossPageNarrativeRoleId.DetailDrill &&
                                    (scoreByRole[CrossPageNarrativeRoleId.Overview] > 0 ||
                                     scoreByRole[CrossPageNarrativeRoleId.ExecutiveSummary] > 0);
        var confidence = DetermineConfidence(primaryRole, primaryScore, competingScore, hasExecutiveConflict);
        return new CrossPageNarrativeRoleAssignment
        {
            PrimaryRole = primaryRole,
            Confidence = confidence,
            Evidence = evidence,
            SecondaryHints = ordered
                .Where(entry => entry.Value > 0 && entry.Key != primaryRole)
                .Select(entry => entry.Key.ToString())
                .ToList(),
        };
    }

    private static CrossPageNarrativeRoleConfidence DetermineConfidence(
        CrossPageNarrativeRoleId primaryRole,
        int primaryScore,
        int competingScore,
        bool hasExecutiveConflict)
    {
        if (primaryRole == CrossPageNarrativeRoleId.SupportingContext)
        {
            return CrossPageNarrativeRoleConfidence.Low;
        }

        if (hasExecutiveConflict)
        {
            return CrossPageNarrativeRoleConfidence.Medium;
        }

        if (primaryScore >= 5 && primaryScore - competingScore >= 2)
        {
            return CrossPageNarrativeRoleConfidence.High;
        }

        if (primaryScore >= 3)
        {
            return CrossPageNarrativeRoleConfidence.Medium;
        }

        return CrossPageNarrativeRoleConfidence.Low;
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }
}
