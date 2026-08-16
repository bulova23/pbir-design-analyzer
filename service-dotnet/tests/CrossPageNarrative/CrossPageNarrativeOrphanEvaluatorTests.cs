using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.CrossPageNarrative;

public sealed class CrossPageNarrativeOrphanEvaluatorTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string OrphanEvaluatorTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativeOrphanEvaluator";
    private const string NavigationEvaluatorTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativeNavigationEvaluator";
    private const string InputTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativeReportInput";
    private const string PageInputTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativePageInput";

    [Fact(DisplayName = "Orphan evaluator flags drill pages without inbound narrative support as unused drill targets")]
    public void Evaluate_FlagsUnusedDrillTarget()
    {
        var input = CreateReportInput(CreatePageInput("page-detail", "Detail", drillPathPresent: true, specialPageType: "Unknown"));
        var roleAssignments = CreateRoleAssignments(("Detail", "DetailDrill"));
        var graph = CreateGraph([]);

        var result = EvaluateOrphans(input, roleAssignments, graph);

        Assert.Equal("UnusedDrillTarget", GetStringProperty(result, "page-detail"));
    }

    [Fact(DisplayName = "Orphan evaluator downgrades appendix-like disconnected pages to advisory orphan state")]
    public void Evaluate_DowngradesAppendixDisconnection()
    {
        var input = CreateReportInput(CreatePageInput("page-appendix", "Appendix", drillPathPresent: false, specialPageType: "ReferenceLegal"));
        var roleAssignments = CreateRoleAssignments(("Appendix", "ReferenceLegal"));
        var graph = CreateGraph([]);

        var result = EvaluateOrphans(input, roleAssignments, graph);

        Assert.Equal("AdvisoryDisconnectedSpecialPage", GetStringProperty(result, "page-appendix"));
    }

    [Fact(DisplayName = "Navigation evaluator penalizes disconnected drill-heavy reports and rewards connected paths")]
    public void EvaluateNavigation_ScoresConnectedPathsHigher()
    {
        var connectedInput = CreateReportInput(
            CreatePageInput("page-overview", "Overview", drillPathPresent: false, specialPageType: "Unknown"),
            CreatePageInput("page-detail", "Detail", drillPathPresent: true, specialPageType: "Unknown"));
        var connectedRoles = CreateRoleAssignments(("Overview", "Overview"), ("Detail", "DetailDrill"));
        var connectedGraph = CreateGraph([CreateEdge("page-overview", "page-detail", "Drillthrough", "Observed")]);
        var connectedOrphans = EvaluateOrphans(connectedInput, connectedRoles, connectedGraph);

        var disconnectedInput = CreateReportInput(CreatePageInput("page-detail", "Detail", drillPathPresent: true, specialPageType: "Unknown"));
        var disconnectedRoles = CreateRoleAssignments(("Detail", "DetailDrill"));
        var disconnectedGraph = CreateGraph([]);
        var disconnectedOrphans = EvaluateOrphans(disconnectedInput, disconnectedRoles, disconnectedGraph);

        var connectedScores = EvaluateNavigation(connectedInput, connectedGraph, connectedOrphans);
        var disconnectedScores = EvaluateNavigation(disconnectedInput, disconnectedGraph, disconnectedOrphans);

        Assert.True(GetScore(connectedScores, "Navigation") > GetScore(disconnectedScores, "Navigation"));
        Assert.True(GetScore(connectedScores, "Actionability") > GetScore(disconnectedScores, "Actionability"));
    }

    private static Dictionary<string, string> EvaluateOrphans(object input, object roleAssignments, object graph)
    {
        var evaluatorType = CoreAssembly.GetType(OrphanEvaluatorTypeName, throwOnError: false);
        Assert.NotNull(evaluatorType);

        var method = evaluatorType!.GetMethod("Evaluate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [input, roleAssignments, graph]);
        Assert.NotNull(result);

        return ((System.Collections.IEnumerable)result!)
            .Cast<object>()
            .ToDictionary(
                entry => entry.GetType().GetProperty("Key", BindingFlags.Instance | BindingFlags.Public)!.GetValue(entry)?.ToString() ?? string.Empty,
                entry => entry.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)!.GetValue(entry)?.ToString() ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static List<object> EvaluateNavigation(object input, object graph, IDictionary<string, string> orphanStates)
    {
        var evaluatorType = CoreAssembly.GetType(NavigationEvaluatorTypeName, throwOnError: false);
        Assert.NotNull(evaluatorType);

        var method = evaluatorType!.GetMethod("Evaluate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [input, graph, orphanStates]);
        return ReadObjectList(result);
    }

    private static object CreateReportInput(params object[] pages)
    {
        var reportInputType = CoreAssembly.GetType(InputTypeName, throwOnError: false);
        var pageInputType = CoreAssembly.GetType(PageInputTypeName, throwOnError: false);
        Assert.NotNull(reportInputType);
        Assert.NotNull(pageInputType);

        var input = Activator.CreateInstance(reportInputType!);
        Assert.NotNull(input);
        SetProperty(input!, "Pages", CreateTypedList(pageInputType!, pages));
        SetProperty(input!, "ExplicitEdges", CreateTypedList(RequireType("CrossPageNarrativeEdge"), []));
        return input!;
    }

    private static object CreatePageInput(string pageId, string pageName, bool drillPathPresent, string specialPageType)
    {
        var pageInputType = CoreAssembly.GetType(PageInputTypeName, throwOnError: false);
        Assert.NotNull(pageInputType);

        var input = Activator.CreateInstance(pageInputType!);
        Assert.NotNull(input);

        SetProperty(input!, "PageId", pageId);
        SetProperty(input!, "PageName", pageName);
        SetProperty(input!, "PageIndex", pageId == "page-overview" ? 0 : 1);
        SetProperty(input!, "IntentProfile", string.Empty);
        SetProperty(input!, "StoryArchetype", string.Empty);
        SetProperty(input!, "InferredStory", string.Empty);
        SetProperty(input!, "DrillPathPresent", drillPathPresent);
        SetProperty(input!, "GuidedStoryImprovementIds", Array.Empty<string>());
        SetProperty(input!, "ReportConsistencyNotes", Array.Empty<string>());
        SetProperty(input!, "VisualMetadata", null);
        SetProperty(input!, "DataVisualCount", 0);
        SetProperty(input!, "NavigationVisualCount", 0);
        SetProperty(input!, "SpecialPageType", specialPageType);
        SetProperty(input!, "TreatAsPrimaryNarrativePage", specialPageType == "Unknown");
        SetProperty(input!, "SuppressNormalStoryGaps", specialPageType != "Unknown");
        return input!;
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

    private static object CreateGraph(IEnumerable<object> edges)
    {
        var graphType = RequireType("CrossPageNarrativeGraph");
        var edgeType = RequireType("CrossPageNarrativeEdge");
        var graph = Activator.CreateInstance(graphType);
        Assert.NotNull(graph);

        SetProperty(graph!, "PageIds", Array.Empty<string>());
        SetProperty(graph!, "Edges", CreateTypedList(edgeType, edges));
        SetProperty(graph!, "Segments", new List<IReadOnlyList<string>>());
        SetProperty(graph!, "MainNarrativePath", Array.Empty<string>());
        return graph!;
    }

    private static object CreateEdge(string sourcePageId, string targetPageId, string edgeTypeName, string observationKind)
    {
        var edgeType = RequireType("CrossPageNarrativeEdge");
        var edgeKindEnum = RequireType("CrossPageNarrativeEdgeType");
        var observationEnum = RequireType("CrossPageNarrativeEdgeObservationKind");
        var edge = Activator.CreateInstance(edgeType);
        Assert.NotNull(edge);

        SetProperty(edge!, "SourcePageId", sourcePageId);
        SetProperty(edge!, "TargetPageId", targetPageId);
        SetProperty(edge!, "EdgeType", Enum.Parse(edgeKindEnum, edgeTypeName));
        SetProperty(edge!, "ObservationKind", Enum.Parse(observationEnum, observationKind));
        SetProperty(edge!, "Strength", 1.0d);
        SetProperty(edge!, "Evidence", new[] { "test" });
        return edge!;
    }

    private static double GetScore(IEnumerable<object> dimensions, string dimensionId)
    {
        var dimension = dimensions.Single(item => GetStringProperty(item, "DimensionId") == dimensionId);
        return (double)(GetPropertyValue(dimension, "Score") ?? 0d);
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

    private static string GetStringProperty(IDictionary<string, string> values, string key)
    {
        return values[key];
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        return GetPropertyValue(target, propertyName)?.ToString() ?? string.Empty;
    }
}
