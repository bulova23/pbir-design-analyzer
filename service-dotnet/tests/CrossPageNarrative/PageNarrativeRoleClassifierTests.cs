using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.CrossPageNarrative;

public sealed class PageNarrativeRoleClassifierTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string InputTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativePageInput";
    private const string ClassifierTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.PageNarrativeRoleClassifier";

    [Fact(DisplayName = "Role classifier maps special pages to suppressed narrative roles")]
    public void Classify_MapsSpecialPages()
    {
        Assert.Equal("Tooltip", GetPrimaryRole(Classify(CreatePageInput("Tooltip Page", specialPageType: "Tooltip"))));
        Assert.Equal("Qna", GetPrimaryRole(Classify(CreatePageInput("Q&A", specialPageType: "Qna"))));
        Assert.Equal("ReferenceLegal", GetPrimaryRole(Classify(CreatePageInput("Legal", specialPageType: "ReferenceLegal"))));
        Assert.Equal("ValidationSandbox", GetPrimaryRole(Classify(CreatePageInput("Sandbox", specialPageType: "ValidationSandbox"))));
    }

    [Fact(DisplayName = "Role classifier identifies framing overview pages from first-position executive cues")]
    public void Classify_IdentifiesOverviewPages()
    {
        var assignment = Classify(CreatePageInput(
            pageName: "Overview",
            pageIndex: 0,
            intentProfile: "executive",
            storyArchetype: "NarrativeWalkthrough",
            inferredStory: "Executive overview of revenue performance",
            visiblePageTitle: "Revenue Overview"));

        Assert.Equal("Overview", GetPrimaryRole(assignment));
        Assert.Equal("High", GetConfidence(assignment));
    }

    [Fact(DisplayName = "Role classifier detects drill-oriented detail pages from drill and table posture")]
    public void Classify_IdentifiesDetailDrillPages()
    {
        var assignment = Classify(CreatePageInput(
            pageName: "Region Detail",
            pageIndex: 2,
            intentProfile: "analytical",
            storyArchetype: "Comparison",
            inferredStory: "Regional performance detail",
            drillPathPresent: true,
            visualTypes: ["tableEx", "barChart"]));

        Assert.Equal("DetailDrill", GetPrimaryRole(assignment));
        Assert.Equal("High", GetConfidence(assignment));
    }

    [Fact(DisplayName = "Role classifier downgrades confidence when summary and detail cues conflict")]
    public void Classify_DowngradesConfidenceForConflictingEvidence()
    {
        var assignment = Classify(CreatePageInput(
            pageName: "Executive Detail",
            pageIndex: 0,
            intentProfile: "executive",
            storyArchetype: "NarrativeWalkthrough",
            inferredStory: "Executive revenue detail",
            drillPathPresent: true,
            visualTypes: ["tableEx", "barChart"],
            visiblePageTitle: "Executive Detail"));

        Assert.Equal("DetailDrill", GetPrimaryRole(assignment));
        Assert.Equal("Medium", GetConfidence(assignment));
    }

    private static object Classify(object pageInput)
    {
        var classifierType = CoreAssembly.GetType(ClassifierTypeName, throwOnError: false);
        Assert.NotNull(classifierType);

        var method = classifierType!.GetMethod("Classify", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [pageInput, 4]);
        Assert.NotNull(result);
        return result!;
    }

    private static object CreatePageInput(
        string pageName,
        int pageIndex = 0,
        string intentProfile = "",
        string storyArchetype = "",
        string inferredStory = "",
        bool drillPathPresent = false,
        string specialPageType = "Unknown",
        string? visiblePageTitle = null,
        IReadOnlyList<string>? visualTypes = null)
    {
        var inputType = CoreAssembly.GetType(InputTypeName, throwOnError: false);
        Assert.NotNull(inputType);

        var input = Activator.CreateInstance(inputType!);
        Assert.NotNull(input);

        SetProperty(input!, "PageId", $"page-{pageName.ToLowerInvariant().Replace(' ', '-')}");
        SetProperty(input!, "PageName", pageName);
        SetProperty(input!, "PageIndex", pageIndex);
        SetProperty(input!, "IntentProfile", intentProfile);
        SetProperty(input!, "StoryArchetype", storyArchetype);
        SetProperty(input!, "InferredStory", inferredStory);
        SetProperty(input!, "DrillPathPresent", drillPathPresent);
        SetProperty(input!, "GuidedStoryImprovementIds", Array.Empty<string>());
        SetProperty(input!, "ReportConsistencyNotes", Array.Empty<string>());
        SetProperty(input!, "VisualMetadata", BuildVisualMetadata(pageName, visiblePageTitle, visualTypes ?? []));
        SetProperty(input!, "DataVisualCount", (visualTypes ?? []).Count);
        SetProperty(input!, "NavigationVisualCount", 0);
        SetProperty(input!, "SpecialPageType", specialPageType);
        SetProperty(input!, "TreatAsPrimaryNarrativePage", specialPageType == "Unknown");
        SetProperty(input!, "SuppressNormalStoryGaps", specialPageType != "Unknown");

        return input!;
    }

    private static PageVisualMetadataSummary BuildVisualMetadata(
        string pageName,
        string? visiblePageTitle,
        IReadOnlyList<string> visualTypes)
    {
        return new PageVisualMetadataSummary
        {
            PageName = pageName,
            VisiblePageTitle = visiblePageTitle,
            VisualCount = visualTypes.Count,
            VisibleTitleVisualCount = string.IsNullOrWhiteSpace(visiblePageTitle) ? 0 : 1,
            TextVisualCount = 0,
            SlicerCount = 0,
            LegendVisualCount = 0,
            AxisLabelVisualCount = 0,
            DataLabelVisualCount = 0,
            FormattedVisualCount = 0,
            Visuals = visualTypes.Select((visualType, index) => new VisualMetadataItem
            {
                VisualId = $"v{index + 1}",
                VisualType = visualType,
                X = 0,
                Y = 0,
                Width = 100,
                Height = 100,
                IsHidden = false,
                IsNavigationElement = false,
                IsDecorative = false,
                IsSlicer = visualType.Contains("slicer", StringComparison.OrdinalIgnoreCase),
                VisibleTitleText = visiblePageTitle,
                BestVisibleText = visiblePageTitle,
                HasVisibleTitleIntent = !string.IsNullOrWhiteSpace(visiblePageTitle),
            }).ToList(),
        };
    }

    private static string GetPrimaryRole(object assignment)
    {
        return GetPropertyValue(assignment, "PrimaryRole")?.ToString() ?? string.Empty;
    }

    private static string GetConfidence(object assignment)
    {
        return GetPropertyValue(assignment, "Confidence")?.ToString() ?? string.Empty;
    }

    private static object? GetPropertyValue(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        return property!.GetValue(target);
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }
}
