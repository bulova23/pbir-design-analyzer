using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.CrossPageNarrative;

public sealed class CrossPageNarrativeGapBuilderTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string GapBuilderTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativeGapBuilder";
    private const string PageInputTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativePageInput";

    [Fact(DisplayName = "Gap builder creates a missing executive entry point gap when the report has no framing page")]
    public void Build_CreatesMissingExecutiveEntryPointGap()
    {
        var pages = new[] { CreatePageInput("Analysis"), CreatePageInput("Detail") };
        var roleAssignments = CreateRoleAssignments(("Analysis", "ComparativeAnalysis"), ("Detail", "DetailDrill"));
        var orphanStates = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["page-analysis"] = "Connected",
            ["page-detail"] = "Connected",
        };

        var gaps = Build(pages, roleAssignments, orphanStates, score: 62);

        Assert.Contains(gaps, gap => GetStringProperty(gap, "GapId") == "MissingExecutiveEntryPoint");
    }

    [Fact(DisplayName = "Gap builder creates orphan detail recommendations from unused drill targets")]
    public void Build_CreatesOrphanDetailGap()
    {
        var pages = new[] { CreatePageInput("Detail") };
        var roleAssignments = CreateRoleAssignments(("Detail", "DetailDrill"));
        var orphanStates = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["page-detail"] = "UnusedDrillTarget",
        };

        var gaps = Build(pages, roleAssignments, orphanStates, score: 48);
        var orphanGap = Assert.Single(gaps.Where(gap => GetStringProperty(gap, "GapId") == "OrphanDetailPage"));

        Assert.Equal("gap.report.orphan-detail-page", GetStringProperty(orphanGap, "StableId"));
        Assert.Equal("Restructure", GetStringProperty(orphanGap, "RemediationLayer"));
        Assert.NotEmpty(ReadObjectList(GetPropertyValue(orphanGap, "EvidenceReferences")));
    }

    private static List<object> Build(
        IReadOnlyList<object> pages,
        object roleAssignments,
        IReadOnlyDictionary<string, string> orphanStates,
        double score)
    {
        var gapBuilderType = CoreAssembly.GetType(GapBuilderTypeName, throwOnError: false);
        Assert.NotNull(gapBuilderType);
        var pageInputType = CoreAssembly.GetType(PageInputTypeName, throwOnError: false);
        Assert.NotNull(pageInputType);

        var method = gapBuilderType!.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var typedPages = CreateTypedList(pageInputType!, pages);
        var result = method!.Invoke(null, [typedPages, roleAssignments, orphanStates, score]);
        return ReadObjectList(result);
    }

    private static object CreatePageInput(string pageName)
    {
        var pageInputType = CoreAssembly.GetType(PageInputTypeName, throwOnError: false);
        Assert.NotNull(pageInputType);

        var input = Activator.CreateInstance(pageInputType!);
        Assert.NotNull(input);
        SetProperty(input!, "PageId", $"page-{pageName.ToLowerInvariant()}");
        SetProperty(input!, "PageName", pageName);
        SetProperty(input!, "PageIndex", 0);
        SetProperty(input!, "IntentProfile", string.Empty);
        SetProperty(input!, "StoryArchetype", string.Empty);
        SetProperty(input!, "InferredStory", string.Empty);
        SetProperty(input!, "DrillPathPresent", pageName == "Detail");
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

    private static string GetStringProperty(object target, string propertyName)
    {
        return GetPropertyValue(target, propertyName)?.ToString() ?? string.Empty;
    }
}
