using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using PowerBIModelingService.Services.Pbir.CustomVisualEvidence;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

internal sealed class ReportModelLoader
{
    private readonly ILogger _logger;

    public ReportModelLoader(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal LoadedReportModel LoadReportModel(PbirReportLocation location)
    {
        var reportJson = ReadJsonObject(location.ReportJsonPath);
        var pages = LoadAllPages(location);
        var reportFilters = ParseScopedFilterDefinitions(reportJson["reportFilters"], StoryFilterScope.Report, "report");
        return new LoadedReportModel(reportJson, pages, reportFilters);
    }

    private List<PageData> LoadAllPages(PbirReportLocation location)
    {
        var pagesRoot = Path.Combine(location.DefinitionPath, "pages");
        if (!Directory.Exists(pagesRoot))
        {
            return [];
        }

        var pages = new List<PageData>();
        foreach (var pageId in GetOrderedPageIds(pagesRoot))
        {
            if (string.IsNullOrWhiteSpace(pageId))
            {
                continue;
            }

            var pageDir = Path.Combine(pagesRoot, pageId);
            if (!Directory.Exists(pageDir))
            {
                _logger.LogDebug("[Scoring] Skipping page id without folder: {PageId}", pageId);
                continue;
            }

            var pageJsonPath = Path.Combine(pageDir, "page.json");
            if (!File.Exists(pageJsonPath))
            {
                continue;
            }

            try
            {
                var pageJson = ReadJsonObject(pageJsonPath);
                var displayName = pageJson["displayName"]?.GetValue<string>() ?? Path.GetFileName(pageDir);
                var visuals = ParseVisuals(pageJson);
                if (visuals.Count == 0)
                {
                    visuals = ParseVisualsFromDirectory(pageDir);
                }

                pages.Add(new PageData
                {
                    Name = pageJson["name"]?.GetValue<string>() ?? Path.GetFileName(pageDir),
                    DisplayName = displayName,
                    Visuals = visuals,
                    Canvas = ParseCanvasMetadata(pageJson),
                    PageFilters = ParseScopedFilterDefinitions(
                        pageJson["pageFilters"] ?? pageJson["filterConfig"]?["filters"],
                        StoryFilterScope.Page,
                        displayName),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Scoring] Could not read page {Dir}", pageDir);
            }
        }

        return pages;
    }

    private List<string> GetOrderedPageIds(string pagesRoot)
    {
        var pagesMetadataPath = Path.Combine(pagesRoot, "pages.json");
        if (File.Exists(pagesMetadataPath))
        {
            try
            {
                var pagesMetadata = ReadJsonObject(pagesMetadataPath);
                if (pagesMetadata["pageOrder"] is JsonArray pageOrder)
                {
                    var orderedPageIds = pageOrder
                        .Select(node => node?.GetValue<string>())
                        .Where(pageId => !string.IsNullOrWhiteSpace(pageId))
                        .Select(pageId => pageId!)
                        .ToList();

                    if (orderedPageIds.Count > 0)
                    {
                        return orderedPageIds;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Scoring] Failed to parse page order metadata: {Path}", pagesMetadataPath);
            }
        }

        return Directory.GetDirectories(pagesRoot)
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .Select(Path.GetFileName)
            .Where(pageId => !string.IsNullOrWhiteSpace(pageId))
            .Select(pageId => pageId!)
            .ToList();
    }

    private List<VisualData> ParseVisuals(JsonObject pageJson)
    {
        var visuals = new List<VisualData>();
        if (pageJson["visuals"] is not JsonArray arr)
        {
            return visuals;
        }

        foreach (var item in arr)
        {
            if (item is not JsonObject visualObject)
            {
                continue;
            }

            var visualId = visualObject["id"]?.GetValue<string>() ?? string.Empty;
            var isHidden = ReadBooleanNode(visualObject["isHidden"]) ?? false;
            visuals.Add(CreateVisualData(
                visualJson: visualObject,
                visual: visualObject,
                visualId: visualId,
                visualType: visualObject["type"]?.GetValue<string>() ?? string.Empty,
                x: TryDouble(visualObject, "x"),
                y: TryDouble(visualObject, "y"),
                w: TryDouble(visualObject, "width"),
                h: TryDouble(visualObject, "height"),
                isHidden: isHidden,
                sourceContext: $"page visual '{visualId}'"));
        }

        return OrderVisualsDeterministically(visuals);
    }

    private List<VisualData> ParseVisualsFromDirectory(string pageDir)
    {
        var visuals = new List<VisualData>();
        var visualsDir = Path.Combine(pageDir, "visuals");
        if (!Directory.Exists(visualsDir))
        {
            return visuals;
        }

        foreach (var visualDir in Directory.GetDirectories(visualsDir).OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal))
        {
            var visualJsonPath = Path.Combine(visualDir, "visual.json");
            if (!File.Exists(visualJsonPath))
            {
                continue;
            }

            try
            {
                var visualJson = ReadJsonObject(visualJsonPath);
                var visualName = visualJson["name"]?.GetValue<string>() ?? Path.GetFileName(visualDir);
                var position = visualJson["position"] as JsonObject;

                var x = position?["x"]?.GetValue<double>() ?? 0;
                var y = position?["y"]?.GetValue<double>() ?? 0;
                var w = position?["width"]?.GetValue<double>() ?? 0;
                var h = position?["height"]?.GetValue<double>() ?? 0;
                var visual = visualJson["visual"] as JsonObject;
                var visualType = visual?["visualType"]?.GetValue<string>() ?? "unknown";
                var isHidden = visualJson["isHidden"]?.GetValue<bool>() ?? false;

                visuals.Add(CreateVisualData(
                    visualJson: visualJson,
                    visual: visual,
                    visualId: visualName,
                    visualType: visualType,
                    x: x,
                    y: y,
                    w: w,
                    h: h,
                    isHidden: isHidden,
                    sourceContext: visualJsonPath));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Scoring] Could not parse visual definition at {Path}", visualJsonPath);
            }
        }

        return OrderVisualsDeterministically(visuals);
    }

    private static List<VisualData> OrderVisualsDeterministically(IEnumerable<VisualData> visuals)
    {
        return visuals
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .ThenBy(visual => visual.W)
            .ThenBy(visual => visual.H)
            .ThenBy(visual => visual.Id, StringComparer.Ordinal)
            .ThenBy(visual => visual.Type, StringComparer.Ordinal)
            .ToList();
    }

    private static CanvasMetadata? ParseCanvasMetadata(JsonObject pageJson)
    {
        var width = ReadDoubleNode(pageJson["width"])
            ?? ReadNestedDouble(pageJson, "canvas", "width")
            ?? ReadNestedDouble(pageJson, "pageSize", "width")
            ?? ReadNestedDouble(pageJson, "size", "width");
        var height = ReadDoubleNode(pageJson["height"])
            ?? ReadNestedDouble(pageJson, "canvas", "height")
            ?? ReadNestedDouble(pageJson, "pageSize", "height")
            ?? ReadNestedDouble(pageJson, "size", "height");

        return width is > 0 && height is > 0
            ? new CanvasMetadata(width.Value, height.Value)
            : null;
    }

    private static List<FilterDefinitionData> ParseScopedFilterDefinitions(
        JsonNode? filtersNode,
        StoryFilterScope scope,
        string sourcePrefix)
    {
        if (filtersNode is not JsonArray filtersArray)
        {
            return [];
        }

        var definitions = new List<FilterDefinitionData>();
        for (int index = 0; index < filtersArray.Count; index++)
        {
            var filterNode = filtersArray[index];
            if (filterNode is not JsonObject filterObject)
            {
                definitions.Add(new FilterDefinitionData(
                    SourceId: $"{sourcePrefix}-{index + 1}",
                    Scope: scope,
                    DisplayLabel: $"{scope} filter {index + 1}",
                    FieldHints: Array.Empty<string>(),
                    HierarchyPattern: null,
                    HierarchyDepth: 0,
                    FilterType: null,
                    PlacementZone: null,
                    IsMalformed: true));
                continue;
            }

            var fieldHints = ReadStringValues(filterObject["field"])
                .Concat(ReadStringValues(filterObject["fields"]))
                .Concat(ReadStringValues(filterObject["target"]))
                .Concat(ReadStringValues(filterObject["column"]))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var hierarchyLevels = FindValuesRecursive(filterObject, (IReadOnlyList<string>)["hierarchy"])
                .SelectMany(ReadStringValues)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var hierarchyPattern = hierarchyLevels.Count >= 2 ? string.Join(" > ", hierarchyLevels) : null;
            var displayLabel = FirstNonBlank(
                ReadFirstString(filterObject, ["displayName", "label", "name", "title"]),
                fieldHints.FirstOrDefault(),
                $"{scope} filter {index + 1}")!;
            var filterType = ReadFirstString(filterObject, ["filterType", "type", "mode"]);

            definitions.Add(new FilterDefinitionData(
                SourceId: $"{sourcePrefix}-{index + 1}",
                Scope: scope,
                DisplayLabel: displayLabel,
                FieldHints: fieldHints,
                HierarchyPattern: hierarchyPattern,
                HierarchyDepth: hierarchyLevels.Count > 0 ? hierarchyLevels.Count : InferHierarchyDepth(fieldHints, hierarchyPattern),
                FilterType: filterType,
                PlacementZone: null,
                IsMalformed: fieldHints.Count == 0 && string.IsNullOrWhiteSpace(filterType)));
        }

        return definitions;
    }

    private VisualData CreateVisualData(
        JsonObject visualJson,
        JsonObject? visual,
        string visualId,
        string visualType,
        double x,
        double y,
        double w,
        double h,
        bool isHidden,
        string sourceContext)
    {
        return new VisualData
        {
            Id = visualId,
            Type = visualType,
            X = x,
            Y = y,
            W = w,
            H = h,
            IsHidden = isHidden,
            Text = ParseVisualTextMetadata(visualJson, visualId, visualType, sourceContext),
            Labels = ParseVisualLabelMetadata(visualJson, visualId, sourceContext),
            FieldRoles = ParseVisualFieldRoleMetadata(visualJson, visualId, sourceContext),
            Formatting = ParseVisualFormattingMetadata(visualJson, visualId, sourceContext),
            Filter = ParseVisualFilterTopologyMetadata(visualJson, visualId, sourceContext),
            CustomVisualEvidence = visual is null ? null : CustomVisualEvidenceExtractor.Extract(visual, visualType),
        };
    }

    private VisualTextMetadata ParseVisualTextMetadata(
        JsonObject visualJson,
        string visualId,
        string visualType,
        string sourceContext)
    {
        return TryParseVisualComponent(
            visualId,
            "text",
            sourceContext,
            () =>
            {
                var titleText = FirstNonBlank(
                    ExtractVisibleObjectText(visualJson, ["title", "visualTitle", "header"]),
                    ExtractVisibleScalarText(visualJson, ["titleText", "visualTitleText"]));
                var subtitleText = FirstNonBlank(
                    ExtractVisibleObjectText(visualJson, ["subtitle", "subTitle"]),
                    ExtractVisibleScalarText(visualJson, ["subtitleText", "subTitleText"]));
                string? textBoxText = null;
                if (IsTextVisualType(visualType))
                {
                    textBoxText = FirstNonBlank(
                        ExtractVisibleObjectText(visualJson, ["textbox", "textBox", "body"]),
                        ExtractVisibleScalarText(visualJson, ["textBoxText", "bodyText", "text"]),
                        ExtractTextRunContent(visualJson));
                }

                return new VisualTextMetadata(titleText, subtitleText, textBoxText);
            },
            VisualTextMetadata.Empty);
    }

    private VisualLabelMetadata ParseVisualLabelMetadata(
        JsonObject visualJson,
        string visualId,
        string sourceContext)
    {
        return TryParseVisualComponent(
            visualId,
            "labels",
            sourceContext,
            () =>
            {
                var hasLegend = ExtractPresenceFlag(visualJson, ["legend"], ["hasLegend", "showLegend"]);
                var hasAxisLabels = ExtractPresenceFlag(
                    visualJson,
                    ["axis", "xAxis", "yAxis", "categoryAxis", "valueAxis"],
                    ["hasAxisLabels", "showAxisLabels"]);
                var hasDataLabels = ExtractPresenceFlag(
                    visualJson,
                    ["dataLabels", "labels"],
                    ["hasDataLabels", "showDataLabels"]);

                return new VisualLabelMetadata(hasLegend, hasAxisLabels, hasDataLabels);
            },
            VisualLabelMetadata.Empty);
    }

    private VisualFieldRoleMetadata ParseVisualFieldRoleMetadata(
        JsonObject visualJson,
        string visualId,
        string sourceContext)
    {
        return TryParseVisualComponent(
            visualId,
            "field roles",
            sourceContext,
            () =>
            {
                var categoryHints = CollectRoleHints(visualJson, ["fieldRoles", "roles", "projections"], ["category", "categories"]);
                var valueHints = CollectRoleHints(visualJson, ["fieldRoles", "roles", "projections"], ["value", "values"]);
                var seriesHints = CollectRoleHints(visualJson, ["fieldRoles", "roles", "projections"], ["series", "legend"]);
                var measureHints = CollectRoleHints(visualJson, ["fieldRoles", "roles", "projections"], ["measure", "measures", "value", "values"]);
                return new VisualFieldRoleMetadata(categoryHints, valueHints, seriesHints, measureHints);
            },
            VisualFieldRoleMetadata.Empty);
    }

    private VisualFormattingMetadata ParseVisualFormattingMetadata(
        JsonObject visualJson,
        string visualId,
        string sourceContext)
    {
        return TryParseVisualComponent(
            visualId,
            "formatting",
            sourceContext,
            () =>
            {
                var backgroundFillColor = FirstNonBlank(
                    ExtractColorFromObjects(visualJson, ["background", "backgroundFill"]),
                    ExtractColorFromObjects(visualJson, ["fill"]));
                var fontColor = FirstNonBlank(
                    ExtractColorFromObjects(visualJson, ["font", "foreground", "fontColor"]),
                    ExtractColorFromObjects(visualJson, ["labelColor", "textColor", "foregroundColor"]));
                var hasBorder = ExtractPresenceFlag(visualJson, ["border", "outline"], ["showBorder", "hasBorder"]);
                var cornerRadius = ExtractNumericSetting(visualJson, ["corners"], ["radius"])
                    ?? ExtractScalarNumber(visualJson, ["cornerRadius"]);
                var hasShadow = ExtractPresenceFlag(visualJson, ["shadow", "dropShadow", "elevation"], ["showShadow", "hasShadow"]);
                return new VisualFormattingMetadata(backgroundFillColor, fontColor, hasBorder, cornerRadius, hasShadow);
            },
            VisualFormattingMetadata.Empty);
    }

    private FilterTopologyMetadata ParseVisualFilterTopologyMetadata(
        JsonObject visualJson,
        string visualId,
        string sourceContext)
    {
        return TryParseVisualComponent(
            visualId,
            "filter topology",
            sourceContext,
            () =>
            {
                var fieldHints = CollectRoleHints(
                    visualJson,
                    ["fieldRoles", "roles", "projections", "filter", "slicer", "field"],
                    ["category", "categories", "field", "fields", "column", "columns"]);
                var hierarchyLevels = FindValuesRecursive(visualJson, (IReadOnlyList<string>)["hierarchy"])
                    .SelectMany(ReadStringValues)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var hierarchyPattern = hierarchyLevels.Count >= 2
                    ? string.Join(" > ", hierarchyLevels)
                    : fieldHints.FirstOrDefault(hint =>
                        hint.Contains("hierarchy", StringComparison.OrdinalIgnoreCase) ||
                        hint.Contains("year", StringComparison.OrdinalIgnoreCase) ||
                        hint.Contains("quarter", StringComparison.OrdinalIgnoreCase) ||
                        hint.Contains("month", StringComparison.OrdinalIgnoreCase));
                var filterType = FirstNonBlank(
                    ReadStringNode(visualJson["filterType"]),
                    ReadFirstString(visualJson, ["mode", "selectionMode", "type"]));

                return new FilterTopologyMetadata(
                    fieldHints,
                    hierarchyPattern,
                    hierarchyLevels.Count > 0 ? hierarchyLevels.Count : InferHierarchyDepth(fieldHints, hierarchyPattern),
                    filterType);
            },
            FilterTopologyMetadata.Empty);
    }

    private static JsonObject ReadJsonObject(string filePath)
    {
        var text = File.ReadAllText(filePath);
        return JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidOperationException($"File is not a JSON object: {filePath}");
    }

    private static double TryDouble(JsonObject obj, string key)
    {
        if (obj[key] is JsonNode node)
        {
            try
            {
                return node.GetValue<double>();
            }
            catch
            {
            }
        }

        return 0.0;
    }

    private T TryParseVisualComponent<T>(
        string visualId,
        string componentName,
        string sourceContext,
        Func<T> parser,
        T fallback)
    {
        try
        {
            return parser();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[Scoring] Failed to parse visual {VisualId} {ComponentName} metadata from {SourceContext}", visualId, componentName, sourceContext);
            return fallback;
        }
    }

    private static bool IsTextVisualType(string visualType) =>
        visualType.Equals("textbox", StringComparison.OrdinalIgnoreCase);

    private static string? ExtractVisibleObjectText(JsonNode? root, IReadOnlyList<string> objectNames)
    {
        foreach (var obj in FindObjectsRecursive(root, objectNames))
        {
            if (!IsObjectVisible(obj))
            {
                continue;
            }

            var directText = NormalizeText(ReadFirstString(obj, ["text", "value", "displayText", "titleText", "subtitleText", "content"]));
            if (!string.IsNullOrWhiteSpace(directText))
            {
                return directText;
            }

            var runText = ExtractTextRunContent(obj);
            if (!string.IsNullOrWhiteSpace(runText))
            {
                return runText;
            }
        }

        return null;
    }

    private static string? ExtractVisibleScalarText(JsonNode? root, IReadOnlyList<string> propertyNames)
    {
        foreach (var node in FindValuesRecursive(root, propertyNames))
        {
            var text = NormalizeText(ReadStringNode(node));
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private static string? ExtractTextRunContent(JsonNode? root)
    {
        var values = FindValuesRecursive(root, ["textRuns", "runs", "paragraphs"])
            .SelectMany(CollectTextLeaves)
            .Select(NormalizeText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        return values.Count == 0
            ? null
            : string.Join(" ", values.Distinct(StringComparer.Ordinal));
    }

    private static IEnumerable<string> CollectTextLeaves(JsonNode? node)
    {
        if (node is null)
        {
            yield break;
        }

        if (node is JsonObject obj)
        {
            var text = NormalizeText(ReadFirstString(obj, ["text", "value", "content", "displayText"]));
            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text;
            }

            foreach (var child in obj)
            {
                foreach (var nested in CollectTextLeaves(child.Value))
                {
                    yield return nested;
                }
            }

            yield break;
        }

        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                foreach (var nested in CollectTextLeaves(item))
                {
                    yield return nested;
                }
            }
        }
    }

    private static bool? ExtractPresenceFlag(
        JsonNode? root,
        IReadOnlyList<string> objectNames,
        IReadOnlyList<string> scalarNames)
    {
        foreach (var obj in FindObjectsRecursive(root, objectNames))
        {
            var explicitValue = ReadFirstBoolean(obj, ["visible", "show", "enabled"]);
            if (explicitValue.HasValue)
            {
                return explicitValue.Value;
            }

            return true;
        }

        foreach (var node in FindValuesRecursive(root, scalarNames))
        {
            var boolValue = ReadBooleanNode(node);
            if (boolValue.HasValue)
            {
                return boolValue.Value;
            }
        }

        return null;
    }

    private static List<string> CollectRoleHints(
        JsonNode? root,
        IReadOnlyList<string> containerNames,
        IReadOnlyList<string> roleNames)
    {
        var hints = new List<string>();
        foreach (var container in FindObjectsRecursive(root, containerNames))
        {
            foreach (var roleName in roleNames)
            {
                if (TryGetPropertyCaseInsensitive(container, roleName, out var roleNode))
                {
                    hints.AddRange(ReadStringValues(roleNode));
                }
            }
        }

        return hints
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ExtractColorFromObjects(JsonNode? root, IReadOnlyList<string> objectNames)
    {
        foreach (var obj in FindObjectsRecursive(root, objectNames))
        {
            var color = ExtractColor(obj);
            if (!string.IsNullOrWhiteSpace(color))
            {
                return color;
            }
        }

        return null;
    }

    private static string? ExtractColor(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue)
        {
            return NormalizeColor(ReadStringNode(node));
        }

        if (node is JsonObject obj)
        {
            var direct = NormalizeColor(ReadFirstString(obj, ["color", "hex", "value"]));
            if (!string.IsNullOrWhiteSpace(direct))
            {
                return direct;
            }

            foreach (var key in new[] { "solid", "fill", "foreground", "background" })
            {
                if (TryGetPropertyCaseInsensitive(obj, key, out var nested))
                {
                    var color = ExtractColor(nested);
                    if (!string.IsNullOrWhiteSpace(color))
                    {
                        return color;
                    }
                }
            }
        }

        return null;
    }

    private static double? ExtractNumericSetting(
        JsonNode? root,
        IReadOnlyList<string> objectNames,
        IReadOnlyList<string> scalarNames)
    {
        foreach (var obj in FindObjectsRecursive(root, objectNames))
        {
            var number = ReadFirstDouble(obj, scalarNames);
            if (number.HasValue)
            {
                return number.Value;
            }
        }

        return null;
    }

    private static double? ExtractScalarNumber(JsonNode? root, IReadOnlyList<string> propertyNames)
    {
        foreach (var node in FindValuesRecursive(root, propertyNames))
        {
            var number = ReadDoubleNode(node);
            if (number.HasValue)
            {
                return number.Value;
            }
        }

        return null;
    }

    private static IEnumerable<JsonObject> FindObjectsRecursive(JsonNode? node, IReadOnlyList<string> propertyNames) =>
        FindValuesRecursive(node, propertyNames).OfType<JsonObject>();

    private static IEnumerable<JsonNode> FindValuesRecursive(JsonNode? node, IReadOnlyList<string> propertyNames)
    {
        if (node is null)
        {
            yield break;
        }

        var nameSet = new HashSet<string>(propertyNames, StringComparer.OrdinalIgnoreCase);
        foreach (var value in FindValuesRecursiveBySet(node, nameSet))
        {
            yield return value;
        }
    }

    private static IEnumerable<JsonNode> FindValuesRecursiveBySet(JsonNode? node, HashSet<string> propertyNames)
    {
        if (node is null)
        {
            yield break;
        }

        if (node is JsonObject obj)
        {
            foreach (var child in obj)
            {
                if (propertyNames.Contains(child.Key) && child.Value is not null)
                {
                    yield return child.Value;
                }

                foreach (var nested in FindValuesRecursiveBySet(child.Value, propertyNames))
                {
                    yield return nested;
                }
            }

            yield break;
        }

        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                foreach (var nested in FindValuesRecursiveBySet(item, propertyNames))
                {
                    yield return nested;
                }
            }
        }
    }

    private static string? ReadFirstString(JsonObject obj, IReadOnlyList<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetPropertyCaseInsensitive(obj, propertyName, out var node))
            {
                var text = ReadStringNode(node);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static bool? ReadFirstBoolean(JsonObject obj, IReadOnlyList<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetPropertyCaseInsensitive(obj, propertyName, out var node))
            {
                var value = ReadBooleanNode(node);
                if (value.HasValue)
                {
                    return value.Value;
                }
            }
        }

        return null;
    }

    private static double? ReadFirstDouble(JsonObject obj, IReadOnlyList<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetPropertyCaseInsensitive(obj, propertyName, out var node))
            {
                var value = ReadDoubleNode(node);
                if (value.HasValue)
                {
                    return value.Value;
                }
            }
        }

        return null;
    }

    private static double? ReadNestedDouble(JsonObject obj, string objectName, string propertyName)
    {
        if (!TryGetPropertyCaseInsensitive(obj, objectName, out var child) || child is not JsonObject childObject)
        {
            return null;
        }

        return ReadFirstDouble(childObject, [propertyName]);
    }

    private static bool TryGetPropertyCaseInsensitive(JsonObject obj, string propertyName, out JsonNode? value)
    {
        foreach (var child in obj)
        {
            if (string.Equals(child.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = child.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static IReadOnlyList<string> ReadStringValues(JsonNode? node)
    {
        if (node is null)
        {
            return [];
        }

        if (node is JsonArray arr)
        {
            return arr
                .SelectMany(ReadStringValues)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
        }

        if (node is JsonObject obj)
        {
            var values = new List<string>();
            var direct = ReadFirstString(obj, ["displayName", "friendlyName", "name", "value", "field", "label", "queryRef"]);
            if (!string.IsNullOrWhiteSpace(direct))
            {
                values.Add(direct);
            }

            var description = ReadFirstString(obj, ["description"]);
            if (!string.IsNullOrWhiteSpace(description))
            {
                values.Add(description);
            }

            if (TryGetPropertyCaseInsensitive(obj, "synonyms", out var synonymsNode))
            {
                values.AddRange(ReadStringValues(synonymsNode));
            }

            if (TryGetPropertyCaseInsensitive(obj, "aliases", out var aliasesNode))
            {
                values.AddRange(ReadStringValues(aliasesNode));
            }

            if (TryGetPropertyCaseInsensitive(obj, "alias", out var aliasNode))
            {
                values.AddRange(ReadStringValues(aliasNode));
            }

            foreach (var child in obj)
            {
                values.AddRange(ReadStringValues(child.Value));
            }

            return values;
        }

        var scalar = ReadStringNode(node);
        return string.IsNullOrWhiteSpace(scalar) ? [] : [scalar];
    }

    private static bool IsObjectVisible(JsonObject obj)
    {
        var hidden = ReadFirstBoolean(obj, ["isHidden", "hidden"]);
        if (hidden == true)
        {
            return false;
        }

        var visible = ReadFirstBoolean(obj, ["visible", "show", "enabled"]);
        return visible != false;
    }

    private static int InferHierarchyDepth(
        IReadOnlyList<string> fieldHints,
        string? hierarchyPattern)
    {
        if (!string.IsNullOrWhiteSpace(hierarchyPattern))
        {
            return hierarchyPattern.Split('>', StringSplitOptions.RemoveEmptyEntries).Length;
        }

        var normalized = string.Join(" ", fieldHints).ToLowerInvariant();
        int depth = 0;
        foreach (var token in new[] { "year", "quarter", "month", "week", "day" })
        {
            if (normalized.Contains(token, StringComparison.Ordinal))
            {
                depth++;
            }
        }

        return depth >= 2 ? depth : 0;
    }

    private static string? ReadStringNode(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        try
        {
            return node.GetValue<string>();
        }
        catch
        {
            return null;
        }
    }

    private static bool? ReadBooleanNode(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        try
        {
            return node.GetValue<bool>();
        }
        catch
        {
            var text = ReadStringNode(node);
            return bool.TryParse(text, out var parsed) ? parsed : null;
        }
    }

    private static double? ReadDoubleNode(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        try
        {
            return node.GetValue<double>();
        }
        catch
        {
            var text = ReadStringNode(node);
            return double.TryParse(text, out var parsed) ? parsed : null;
        }
    }

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(' ', value.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string? NormalizeColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith("#", StringComparison.Ordinal))
        {
            normalized = $"#{normalized}";
        }

        if (normalized.Length is not (7 or 9))
        {
            return null;
        }

        for (int i = 1; i < normalized.Length; i++)
        {
            if (!Uri.IsHexDigit(normalized[i]))
            {
                return null;
            }
        }

        return normalized.ToUpperInvariant();
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
