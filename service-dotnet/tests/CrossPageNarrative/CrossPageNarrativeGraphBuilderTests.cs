using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.CrossPageNarrative;

public sealed class CrossPageNarrativeGraphBuilderTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string InputTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativeReportInput";
    private const string PageInputTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativePageInput";
    private const string BuilderTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativeGraphBuilder";

    [Fact(DisplayName = "Graph builder creates ordered adjacency edges for adjacent report pages")]
    public void Build_AddsOrderedAdjacencyEdges()
    {
        var reportInput = CreateReportInput(
            CreatePageInput("page-overview", "Overview"),
            CreatePageInput("page-detail", "Detail"));
        var roleAssignments = CreateRoleAssignments(("Overview", "Overview"), ("Detail", "DetailDrill"));

        var graph = Build(reportInput, roleAssignments);
        var edges = ReadObjectList(GetPropertyValue(graph, "Edges"));

        Assert.Contains(edges, edge =>
            GetStringProperty(edge, "SourcePageId") == "page-overview" &&
            GetStringProperty(edge, "TargetPageId") == "page-detail" &&
            GetStringProperty(edge, "EdgeType") == "OrderedNext");
        Assert.Contains(edges, edge =>
            GetStringProperty(edge, "SourcePageId") == "page-detail" &&
            GetStringProperty(edge, "TargetPageId") == "page-overview" &&
            GetStringProperty(edge, "EdgeType") == "OrderedPrevious");
    }

    [Fact(DisplayName = "Graph builder preserves explicit observed edges and infers summary-to-detail transitions")]
    public void Build_PreservesExplicitEdges_AndInfersSummaryToDetail()
    {
        var reportInput = CreateReportInput(
            CreatePageInput("page-overview", "Overview"),
            CreatePageInput("page-detail", "Region Detail"),
            CreateExplicitEdge("page-overview", "page-detail", "Drillthrough"));
        var roleAssignments = CreateRoleAssignments(("Overview", "Overview"), ("Region Detail", "DetailDrill"));

        var graph = Build(reportInput, roleAssignments);
        var edges = ReadObjectList(GetPropertyValue(graph, "Edges"));

        Assert.Contains(edges, edge =>
            GetStringProperty(edge, "SourcePageId") == "page-overview" &&
            GetStringProperty(edge, "TargetPageId") == "page-detail" &&
            GetStringProperty(edge, "EdgeType") == "Drillthrough" &&
            GetStringProperty(edge, "ObservationKind") == "Observed");
        Assert.Contains(edges, edge =>
            GetStringProperty(edge, "SourcePageId") == "page-overview" &&
            GetStringProperty(edge, "TargetPageId") == "page-detail" &&
            GetStringProperty(edge, "EdgeType") == "SummaryToDetail" &&
            GetStringProperty(edge, "ObservationKind") == "Inferred");
    }

    [Fact(DisplayName = "Graph builder segments appendix-like pages into a separate narrative island")]
    public void Build_SegmentsAppendixPagesIntoSeparateIsland()
    {
        var reportInput = CreateReportInput(
            CreatePageInput("page-overview", "Overview"),
            CreatePageInput("page-analysis", "Analysis"),
            CreatePageInput("page-appendix", "Appendix"));
        var roleAssignments = CreateRoleAssignments(
            ("Overview", "Overview"),
            ("Analysis", "ComparativeAnalysis"),
            ("Appendix", "ReferenceLegal"));

        var graph = Build(reportInput, roleAssignments);
        var segments = ReadNestedStringLists(GetPropertyValue(graph, "Segments"));
        var mainPath = ReadStringList(GetPropertyValue(graph, "MainNarrativePath"));

        Assert.Equal(2, segments.Count);
        Assert.Contains(segments, segment => segment.SequenceEqual(["page-overview", "page-analysis"]));
        Assert.Contains(segments, segment => segment.SequenceEqual(["page-appendix"]));
        Assert.Equal(["page-overview", "page-analysis"], mainPath);
    }

    private static object Build(object reportInput, object roleAssignments)
    {
        var builderType = CoreAssembly.GetType(BuilderTypeName, throwOnError: false);
        Assert.NotNull(builderType);

        var method = builderType!.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var graph = method!.Invoke(null, [reportInput, roleAssignments]);
        Assert.NotNull(graph);
        return graph!;
    }

    private static object CreateReportInput(params object[] items)
    {
        var reportInputType = CoreAssembly.GetType(InputTypeName, throwOnError: false);
        Assert.NotNull(reportInputType);

        var pageInputType = CoreAssembly.GetType(PageInputTypeName, throwOnError: false);
        Assert.NotNull(pageInputType);

        var edgeType = RequireType("CrossPageNarrativeEdge");
        var reportInput = Activator.CreateInstance(reportInputType!);
        Assert.NotNull(reportInput);

        var pages = items.Where(item => item.GetType().FullName == PageInputTypeName).ToList();
        var edges = items.Where(item => item.GetType() == edgeType).ToList();

        SetProperty(reportInput!, "Pages", CreateTypedList(pageInputType!, pages));
        SetProperty(reportInput!, "ExplicitEdges", CreateTypedList(edgeType, edges));
        return reportInput!;
    }

    private static object CreatePageInput(string pageId, string pageName)
    {
        var pageInputType = CoreAssembly.GetType(PageInputTypeName, throwOnError: false);
        Assert.NotNull(pageInputType);

        var input = Activator.CreateInstance(pageInputType!);
        Assert.NotNull(input);

        SetProperty(input!, "PageId", pageId);
        SetProperty(input!, "PageName", pageName);
        SetProperty(input!, "PageIndex", pageId switch
        {
            "page-overview" => 0,
            "page-analysis" => 1,
            "page-detail" => 1,
            _ => 2,
        });
        SetProperty(input!, "IntentProfile", string.Empty);
        SetProperty(input!, "StoryArchetype", string.Empty);
        SetProperty(input!, "InferredStory", string.Empty);
        SetProperty(input!, "DrillPathPresent", false);
        SetProperty(input!, "GuidedStoryImprovementIds", Array.Empty<string>());
        SetProperty(input!, "ReportConsistencyNotes", Array.Empty<string>());
        SetProperty(input!, "VisualMetadata", null);
        SetProperty(input!, "DataVisualCount", 0);
        SetProperty(input!, "NavigationVisualCount", 0);
        SetProperty(input!, "SpecialPageType", "Unknown");
        SetProperty(input!, "TreatAsPrimaryNarrativePage", true);
        SetProperty(input!, "SuppressNormalStoryGaps", false);
        return input!;
    }

    private static object CreateExplicitEdge(string sourcePageId, string targetPageId, string edgeType)
    {
        var type = RequireType("CrossPageNarrativeEdge");
        var edgeTypeEnum = RequireType("CrossPageNarrativeEdgeType");
        var observationEnum = RequireType("CrossPageNarrativeEdgeObservationKind");
        var edge = Activator.CreateInstance(type);
        Assert.NotNull(edge);

        SetProperty(edge!, "SourcePageId", sourcePageId);
        SetProperty(edge!, "TargetPageId", targetPageId);
        SetProperty(edge!, "EdgeType", Enum.Parse(edgeTypeEnum, edgeType));
        SetProperty(edge!, "ObservationKind", Enum.Parse(observationEnum, "Observed"));
        SetProperty(edge!, "Strength", 1.0d);
        SetProperty(edge!, "Evidence", new[] { "test" });
        return edge!;
    }

    private static object CreateRoleAssignments(params (string PageName, string RoleName)[] assignments)
    {
        var roleAssignmentType = RequireType("CrossPageNarrativeRoleAssignment");
        var roleEnum = RequireType("CrossPageNarrativeRoleId");
        var confidenceEnum = RequireType("CrossPageNarrativeRoleConfidence");
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), roleAssignmentType);
        var dictionary = Activator.CreateInstance(dictionaryType)!;
        var addMethod = dictionaryType.GetMethod("Add");
        Assert.NotNull(addMethod);

        foreach (var (pageName, roleName) in assignments)
        {
            var assignment = Activator.CreateInstance(roleAssignmentType);
            Assert.NotNull(assignment);
            SetProperty(assignment!, "PrimaryRole", Enum.Parse(roleEnum, roleName));
            SetProperty(assignment!, "Confidence", Enum.Parse(confidenceEnum, "High"));
            SetProperty(assignment!, "Evidence", new[] { "test" });
            SetProperty(assignment!, "SecondaryHints", Array.Empty<string>());
            addMethod!.Invoke(dictionary, [pageName, assignment!]);
        }

        return dictionary;
    }

    private static Type RequireType(string typeName)
    {
        var type = CoreAssembly.GetType($"PowerBIModelingService.Services.Pbir.Models.{typeName}", throwOnError: false);
        Assert.NotNull(type);
        return type!;
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

    private static List<List<string>> ReadNestedStringLists(object? value)
    {
        return value is System.Collections.IEnumerable outer
            ? outer.Cast<object>()
                .Select(item => ReadStringList(item))
                .ToList()
            : [];
    }

    private static List<string> ReadStringList(object? value)
    {
        return value is System.Collections.IEnumerable enumerable
            ? enumerable.Cast<object>().Select(item => item?.ToString() ?? string.Empty).ToList()
            : [];
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        return GetPropertyValue(target, propertyName)?.ToString() ?? string.Empty;
    }
}
