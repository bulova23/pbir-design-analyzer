using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.CrossPageNarrative;

public sealed class CrossPageNarrativeConsistencyEvaluatorTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string EvaluatorTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativeConsistencyEvaluator";
    private const string InputTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativeReportInput";
    private const string PageInputTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativePageInput";

    [Fact(DisplayName = "Consistency evaluator rewards coherent summary-to-detail report flow")]
    public void Evaluate_RewardsCoherentFlow()
    {
        var input = CreateReportInput(
            CreatePageInput("page-overview", "Overview", "Revenue executive overview"),
            CreatePageInput("page-detail", "Region Detail", "Revenue regional detail"));
        var roleAssignments = CreateRoleAssignments(("Overview", "Overview"), ("Region Detail", "DetailDrill"));
        var graph = CreateGraph(
            CreateEdge("page-overview", "page-detail", "OrderedNext", "Observed"),
            CreateEdge("page-overview", "page-detail", "SummaryToDetail", "Inferred"));

        var dimensions = Evaluate(input, roleAssignments, graph);

        Assert.True(GetScore(dimensions, "Flow") >= 80);
        Assert.True(GetScore(dimensions, "Consistency") >= 70);
        Assert.True(GetScore(dimensions, "Continuity") >= 70);
    }

    [Fact(DisplayName = "Consistency evaluator penalizes abrupt business-context shifts and naming mismatches")]
    public void Evaluate_PenalizesContextShift()
    {
        var input = CreateReportInput(
            CreatePageInput("page-summary", "Revenue Summary", "Revenue executive overview"),
            CreatePageInput("page-detail", "Headcount Detail", "Headcount staffing detail"));
        var roleAssignments = CreateRoleAssignments(("Revenue Summary", "ExecutiveSummary"), ("Headcount Detail", "DetailDrill"));
        var graph = CreateGraph(
            CreateEdge("page-summary", "page-detail", "OrderedNext", "Observed"));

        var dimensions = Evaluate(input, roleAssignments, graph);
        var consistency = FindDimension(dimensions, "Consistency");

        Assert.True(GetScore(dimensions, "Consistency") < 70);
        Assert.Contains(ReadStringList(GetPropertyValue(consistency, "WeakeningEvidence")), item => item.Contains("context shift", StringComparison.OrdinalIgnoreCase));
    }

    private static List<object> Evaluate(object input, object roleAssignments, object graph)
    {
        var evaluatorType = CoreAssembly.GetType(EvaluatorTypeName, throwOnError: false);
        Assert.NotNull(evaluatorType);

        var method = evaluatorType!.GetMethod("Evaluate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [input, roleAssignments, graph]);
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

    private static object CreatePageInput(string pageId, string pageName, string inferredStory)
    {
        var pageInputType = CoreAssembly.GetType(PageInputTypeName, throwOnError: false);
        Assert.NotNull(pageInputType);

        var input = Activator.CreateInstance(pageInputType!);
        Assert.NotNull(input);

        SetProperty(input!, "PageId", pageId);
        SetProperty(input!, "PageName", pageName);
        SetProperty(input!, "PageIndex", pageId.EndsWith("detail", StringComparison.Ordinal) ? 1 : 0);
        SetProperty(input!, "IntentProfile", string.Empty);
        SetProperty(input!, "StoryArchetype", string.Empty);
        SetProperty(input!, "InferredStory", inferredStory);
        SetProperty(input!, "DrillPathPresent", pageId.EndsWith("detail", StringComparison.Ordinal));
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

    private static object CreateGraph(params object[] edges)
    {
        var graphType = RequireType("CrossPageNarrativeGraph");
        var edgeType = RequireType("CrossPageNarrativeEdge");
        var graph = Activator.CreateInstance(graphType);
        Assert.NotNull(graph);

        SetProperty(graph!, "PageIds", new[] { "page-overview", "page-detail" });
        SetProperty(graph!, "Edges", CreateTypedList(edgeType, edges));
        SetProperty(
            graph!,
            "Segments",
            new List<IReadOnlyList<string>>
            {
                new List<string> { "page-overview", "page-detail" },
            });
        SetProperty(graph!, "MainNarrativePath", new[] { "page-overview", "page-detail" });
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

    private static object FindDimension(IEnumerable<object> dimensions, string dimensionId)
    {
        return dimensions.Single(dimension => GetStringProperty(dimension, "DimensionId") == dimensionId);
    }

    private static double GetScore(IEnumerable<object> dimensions, string dimensionId)
    {
        var dimension = FindDimension(dimensions, dimensionId);
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
