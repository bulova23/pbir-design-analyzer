using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.CrossPageNarrative;

public sealed class CrossPageNarrativeInputBuilderTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ModelsNamespace = "PowerBIModelingService.Services.Pbir.Models";
    private const string BuilderTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativeInputBuilder";

    [Fact(DisplayName = "Input builder extracts ordered page narrative inputs from existing page scores")]
    public void Build_ExtractsOrderedPageInputs()
    {
        var overview = CreatePageScore(
            pageId: "page-overview",
            pageName: "Overview",
            intentProfile: "executive",
            storyArchetype: "NarrativeWalkthrough",
            inferredStory: "Executive revenue overview",
            drillPathPresent: false);
        var detail = CreatePageScore(
            pageId: "page-detail",
            pageName: "Region Detail",
            intentProfile: "analytical",
            storyArchetype: "Comparison",
            inferredStory: "Regional comparison detail",
            drillPathPresent: true);

        SetInternalSpecialPageAssessment(overview, pageType: "Unknown", treatAsPrimaryNarrativePage: true, suppressNormalStoryGaps: false);
        SetInternalSpecialPageAssessment(detail, pageType: "Unknown", treatAsPrimaryNarrativePage: true, suppressNormalStoryGaps: false);

        var input = InvokeBuild([overview, detail], []);
        var pages = ReadObjectList(GetPropertyValue(input, "Pages"));

        Assert.Equal(2, pages.Count);
        Assert.Equal("page-overview", GetStringProperty(pages[0], "PageId"));
        Assert.Equal("Overview", GetStringProperty(pages[0], "PageName"));
        Assert.Equal(0, GetIntProperty(pages[0], "PageIndex"));
        Assert.Equal("executive", GetStringProperty(pages[0], "IntentProfile"));
        Assert.Equal("NarrativeWalkthrough", GetStringProperty(pages[0], "StoryArchetype"));
        Assert.Equal("Executive revenue overview", GetStringProperty(pages[0], "InferredStory"));
        Assert.False(GetBoolProperty(pages[0], "DrillPathPresent"));

        Assert.Equal("page-detail", GetStringProperty(pages[1], "PageId"));
        Assert.Equal(1, GetIntProperty(pages[1], "PageIndex"));
        Assert.True(GetBoolProperty(pages[1], "DrillPathPresent"));
    }

    [Fact(DisplayName = "Input builder carries explicit observed edges and degrades gracefully when page metadata is sparse")]
    public void Build_PreservesExplicitEdges_AndHandlesSparseInputs()
    {
        var sparse = new PageScore
        {
            PageId = "page-appendix",
            PageName = "Appendix",
            GuidedStoryImprovements = new GuidedStoryImprovements(),
            ReportConsistencyNotes = [],
            Recommendations = [],
            Feedback = [],
        };

        SetInternalSpecialPageAssessment(sparse, pageType: "ReferenceLegal", treatAsPrimaryNarrativePage: false, suppressNormalStoryGaps: true);

        var explicitEdge = CreateExplicitEdge(
            sourcePageId: "page-overview",
            targetPageId: "page-appendix",
            edgeType: "SupportingContext");

        var input = InvokeBuild([sparse], [explicitEdge]);
        var pages = ReadObjectList(GetPropertyValue(input, "Pages"));
        var explicitEdges = ReadObjectList(GetPropertyValue(input, "ExplicitEdges"));

        var page = Assert.Single(pages);
        Assert.Equal("Appendix", GetStringProperty(page, "PageName"));
        Assert.Equal("ReferenceLegal", GetStringProperty(page, "SpecialPageType"));
        Assert.False(GetBoolProperty(page, "TreatAsPrimaryNarrativePage"));
        Assert.True(GetBoolProperty(page, "SuppressNormalStoryGaps"));
        Assert.Equal(string.Empty, GetStringProperty(page, "IntentProfile"));
        Assert.Equal(string.Empty, GetStringProperty(page, "InferredStory"));

        var preservedEdge = Assert.Single(explicitEdges);
        Assert.Equal("page-overview", GetStringProperty(preservedEdge, "SourcePageId"));
        Assert.Equal("page-appendix", GetStringProperty(preservedEdge, "TargetPageId"));
        Assert.Equal("SupportingContext", GetStringProperty(preservedEdge, "EdgeType"));
        Assert.Equal("Observed", GetStringProperty(preservedEdge, "ObservationKind"));
    }

    private static object InvokeBuild(IReadOnlyList<PageScore> pages, IReadOnlyList<object> explicitEdges)
    {
        var builderType = CoreAssembly.GetType(BuilderTypeName, throwOnError: false);
        Assert.NotNull(builderType);

        var edgeType = RequireType("CrossPageNarrativeEdge");
        var buildMethod = builderType!.GetMethod(
            "Build",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(buildMethod);

        var typedEdges = CreateTypedList(edgeType, explicitEdges);
        var result = buildMethod!.Invoke(null, [pages, typedEdges]);

        Assert.NotNull(result);
        return result!;
    }

    private static PageScore CreatePageScore(
        string pageId,
        string pageName,
        string intentProfile,
        string storyArchetype,
        string inferredStory,
        bool drillPathPresent)
    {
        return new PageScore
        {
            PageId = pageId,
            PageName = pageName,
            InferredStorySummary = new PageStorySummary
            {
                IntentProfile = intentProfile,
                StoryArchetype = storyArchetype,
                InferredStory = inferredStory,
                Confidence = "High",
                Evidence = ["test"],
            },
            PageIntentProfile = new PageIntentProfileSummary
            {
                InferredProfile = intentProfile,
                ActionabilityExpectation = "High",
                ReviewGuidance = ["test"],
                Evidence = ["test"],
            },
            ActionabilityBreakdown = new ActionabilityBreakdown
            {
                Score = drillPathPresent ? 80 : 60,
                TargetBenchmarkPresent = true,
                ExceptionVisibility = true,
                UrgencySignaling = false,
                PriorPeriodContext = true,
                DrillPathPresent = drillPathPresent,
                ExpectationLevel = "High",
                Strengths = ["test"],
                Gaps = [],
                Summary = "test",
            },
            GuidedStoryImprovements = new GuidedStoryImprovements(),
            ReportConsistencyNotes = ["test"],
            Recommendations = [],
            Feedback = [],
        };
    }

    private static void SetInternalSpecialPageAssessment(
        PageScore page,
        string pageType,
        bool treatAsPrimaryNarrativePage,
        bool suppressNormalStoryGaps)
    {
        var assessmentType = RequireType("StorySpecialPageAssessment");
        var pageTypeEnum = RequireType("StorySpecialPageType");
        var confidenceEnum = RequireType("StorySpecialPageConfidence");
        var promotionStateEnum = RequireType("StoryAssessmentPromotionState");
        var surfaceScopeEnum = RequireType("StoryAssessmentSurfaceScope");

        var assessment = Activator.CreateInstance(assessmentType);
        Assert.NotNull(assessment);

        SetProperty(assessment!, "PageType", Enum.Parse(pageTypeEnum, pageType));
        SetProperty(assessment!, "Confidence", Enum.Parse(confidenceEnum, "High"));
        SetProperty(assessment!, "EvidenceReferences", CreateEmptyTypedList("StorySpecialPageEvidenceReference"));
        SetProperty(assessment!, "Reason", "test");
        SetProperty(assessment!, "PromotionState", Enum.Parse(promotionStateEnum, "Internal"));
        SetProperty(assessment!, "SurfaceScope", Enum.Parse(surfaceScopeEnum, "PbirSpecific"));
        SetProperty(assessment!, "TreatAsPrimaryNarrativePage", treatAsPrimaryNarrativePage);
        SetProperty(assessment!, "SuppressNormalStoryGaps", suppressNormalStoryGaps);
        SetProperty(assessment!, "SuppressGenericArchetypePromotion", pageType != "Unknown");

        var property = typeof(PageScore).GetProperty(
            "InternalStorySpecialPageAssessment",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);
        property!.SetValue(page, assessment);
    }

    private static object CreateExplicitEdge(string sourcePageId, string targetPageId, string edgeType)
    {
        var type = RequireType("CrossPageNarrativeEdge");
        var edgeTypeEnum = RequireType("CrossPageNarrativeEdgeType");
        var observationKindEnum = RequireType("CrossPageNarrativeEdgeObservationKind");
        var edge = Activator.CreateInstance(type);
        Assert.NotNull(edge);

        SetProperty(edge!, "SourcePageId", sourcePageId);
        SetProperty(edge!, "TargetPageId", targetPageId);
        SetProperty(edge!, "EdgeType", Enum.Parse(edgeTypeEnum, edgeType));
        SetProperty(edge!, "ObservationKind", Enum.Parse(observationKindEnum, "Observed"));
        SetProperty(edge!, "Strength", 1.0d);
        SetProperty(edge!, "Evidence", new[] { "test" });

        return edge!;
    }

    private static Type RequireType(string typeName)
    {
        var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }

    private static object CreateEmptyTypedList(string typeName)
    {
        var itemType = RequireType(typeName);
        return Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;
    }

    private static object CreateTypedList(Type itemType, IEnumerable<object> items)
    {
        var listType = typeof(List<>).MakeGenericType(itemType);
        var list = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add");
        Assert.NotNull(addMethod);

        foreach (var item in items)
        {
            addMethod!.Invoke(list, [item]);
        }

        return list;
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }

    private static object? GetPropertyValue(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        return property!.GetValue(target);
    }

    private static List<object> ReadObjectList(object? value)
    {
        return value is System.Collections.IEnumerable enumerable
            ? enumerable.Cast<object>().ToList()
            : [];
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        return GetPropertyValue(target, propertyName)?.ToString() ?? string.Empty;
    }

    private static bool GetBoolProperty(object target, string propertyName)
    {
        return (bool)(GetPropertyValue(target, propertyName) ?? false);
    }

    private static int GetIntProperty(object target, string propertyName)
    {
        return (int)(GetPropertyValue(target, propertyName) ?? -1);
    }
}
