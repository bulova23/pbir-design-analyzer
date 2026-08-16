using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir.CrossPageNarrative;

internal static class CrossPageNarrativeInputBuilder
{
    public static CrossPageNarrativeReportInput Build(
        IReadOnlyList<PageScore> pages,
        IReadOnlyList<CrossPageNarrativeEdge>? explicitEdges = null)
    {
        var pageInputs = pages
            .Select((page, index) => BuildPageInput(page, index))
            .ToList();

        return new CrossPageNarrativeReportInput
        {
            Pages = pageInputs,
            ExplicitEdges = explicitEdges ?? Array.Empty<CrossPageNarrativeEdge>(),
        };
    }

    private static CrossPageNarrativePageInput BuildPageInput(PageScore page, int pageIndex)
    {
        var specialPageAssessment = page.InternalStorySpecialPageAssessment;

        return new CrossPageNarrativePageInput
        {
            PageId = page.PageId,
            PageName = page.PageName,
            PageIndex = pageIndex,
            IntentProfile = page.PageIntentProfile?.InferredProfile ?? page.InferredStorySummary?.IntentProfile ?? string.Empty,
            StoryArchetype = page.InferredStorySummary?.StoryArchetype ?? string.Empty,
            InferredStory = page.InferredStorySummary?.InferredStory ?? string.Empty,
            DrillPathPresent = page.ActionabilityBreakdown?.DrillPathPresent ?? false,
            GuidedStoryImprovementIds = page.GuidedStoryImprovements.HighPriorityImprovements
                .Concat(page.GuidedStoryImprovements.MediumPriorityImprovements)
                .Select(improvement => improvement.Id)
                .Distinct(StringComparer.Ordinal)
                .ToList(),
            ReportConsistencyNotes = page.ReportConsistencyNotes,
            VisualMetadata = page.VisualMetadata,
            DataVisualCount = page.DataVisualCount,
            NavigationVisualCount = page.NavigationVisualCount,
            SpecialPageType = specialPageAssessment?.PageType.ToString() ?? "Unknown",
            TreatAsPrimaryNarrativePage = specialPageAssessment?.TreatAsPrimaryNarrativePage ?? true,
            SuppressNormalStoryGaps = specialPageAssessment?.SuppressNormalStoryGaps ?? false,
        };
    }
}

internal sealed class CrossPageNarrativeReportInput
{
    public IReadOnlyList<CrossPageNarrativePageInput> Pages { get; init; } =
        Array.Empty<CrossPageNarrativePageInput>();

    public IReadOnlyList<CrossPageNarrativeEdge> ExplicitEdges { get; init; } =
        Array.Empty<CrossPageNarrativeEdge>();
}

internal sealed class CrossPageNarrativePageInput
{
    public required string PageId { get; init; }

    public required string PageName { get; init; }

    public int PageIndex { get; init; }

    public string IntentProfile { get; init; } = string.Empty;

    public string StoryArchetype { get; init; } = string.Empty;

    public string InferredStory { get; init; } = string.Empty;

    public bool DrillPathPresent { get; init; }

    public IReadOnlyList<string> GuidedStoryImprovementIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ReportConsistencyNotes { get; init; } = Array.Empty<string>();

    public PageVisualMetadataSummary? VisualMetadata { get; init; }

    public int DataVisualCount { get; init; }

    public int NavigationVisualCount { get; init; }

    public string SpecialPageType { get; init; } = "Unknown";

    public bool TreatAsPrimaryNarrativePage { get; init; } = true;

    public bool SuppressNormalStoryGaps { get; init; }
}
