using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace PowerBIModelingService.Services;

/// <summary>
/// Builds a report metadata tree from PBIR definition files.
/// </summary>
public sealed class PbirTreeBuilder
{
    private readonly ILogger<PbirTreeBuilder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PbirTreeBuilder"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public PbirTreeBuilder(ILogger<PbirTreeBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Builds a hierarchical report tree from the PBIR definition folder.
    /// </summary>
    /// <param name="location">Resolved report location.</param>
    /// <returns>Tree data suitable for JSON serialization.</returns>
    public Dictionary<string, object> BuildTree(PbirReportLocation location)
    {
        if (location is null)
        {
            throw new ArgumentNullException(nameof(location));
        }

        var basePath = location.WorkspaceRootPath;
        var reportPath = ToWorkspaceRelativePath(location.ReportJsonPath, basePath);
        var pages = BuildPages(location, basePath);
        var themeNode = BuildThemeNode(location, basePath);

        var tree = new Dictionary<string, object>
        {
            ["name"]  = location.ReportName,
            ["path"]  = reportPath,
            ["pages"] = pages
        };

        if (themeNode is not null)
        {
            tree["theme"] = themeNode;
        }

        return tree;
    }

    // Build page nodes with nested visual nodes.
    private List<object> BuildPages(PbirReportLocation location, string basePath)
    {
        var pages = new List<object>();
        var pagesRoot = Path.Combine(location.DefinitionPath, "pages");
        var orderedPageIds = GetPageOrder(pagesRoot);

        if (orderedPageIds.Count == 0)
        {
            orderedPageIds = Directory.Exists(pagesRoot)
                ? Directory.GetDirectories(pagesRoot)
                    .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!)
                    .ToList()
                : new List<string>();
        }

        foreach (var pageId in orderedPageIds)
        {
            if (string.IsNullOrWhiteSpace(pageId))
            {
                continue;
            }

            var pageFolder = Path.Combine(pagesRoot, pageId);
            var pageJsonPath = Path.Combine(pageFolder, "page.json");
            if (!File.Exists(pageJsonPath))
            {
                _logger.LogDebug("Skipping page folder without page.json: {Path}", pageFolder);
                continue;
            }

            var pageNode = LoadJsonNode(pageJsonPath);
            var pageName = pageNode?["name"]?.GetValue<string>() ?? pageId;
            var displayName = pageNode?["displayName"]?.GetValue<string>() ?? pageName;
            var pagePath = ToWorkspaceRelativePath(pageJsonPath, basePath);

            var visuals = BuildVisuals(pageFolder, basePath);

            pages.Add(new
            {
                name = pageName,
                displayName = displayName,
                path = pagePath,
                visuals = visuals
            });
        }

        return pages;
    }

    // Build visual nodes for a given page folder.
    private List<object> BuildVisuals(string pageFolder, string basePath)
    {
        var visuals = new List<object>();
        var visualsRoot = Path.Combine(pageFolder, "visuals");
        if (!Directory.Exists(visualsRoot))
        {
            return visuals;
        }

        var visualFolders = Directory.GetDirectories(visualsRoot)
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToArray();
        foreach (var visualFolder in visualFolders)
        {
            var visualJsonPath = Path.Combine(visualFolder, "visual.json");
            if (!File.Exists(visualJsonPath))
            {
                continue;
            }

            var visualNode = LoadJsonNode(visualJsonPath);
            var visualName = visualNode?["name"]?.GetValue<string>() ?? Path.GetFileName(visualFolder);
            var visualType = visualNode?["visual"]?["visualType"]?.GetValue<string>();
            var visualPath = ToWorkspaceRelativePath(visualJsonPath, basePath);

            visuals.Add(new
            {
                name = visualName,
                visualType = visualType,
                path = visualPath
            });
        }

        return visuals;
    }

    // Read page order from pages.json if available.
    private List<string> GetPageOrder(string pagesRoot)
    {
        var pagesMetadataPath = Path.Combine(pagesRoot, "pages.json");
        if (!File.Exists(pagesMetadataPath))
        {
            return new List<string>();
        }

        var pagesNode = LoadJsonNode(pagesMetadataPath);
        var pageOrderNode = pagesNode?["pageOrder"] as JsonArray;
        if (pageOrderNode is null)
        {
            return new List<string>();
        }

        return pageOrderNode
            .Select(node => node?.GetValue<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList();
    }

    // Load JSON with loose parsing and return a JsonNode tree.
    private JsonNode? LoadJsonNode(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            return JsonNode.Parse(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse PBIR JSON file: {Path}", filePath);
            return null;
        }
    }

    // Build the theme node from report.json "theme" field and the definition folder.
    private object? BuildThemeNode(PbirReportLocation location, string basePath)
    {
        var reportNode = LoadJsonNode(location.ReportJsonPath);
        if (reportNode is null)
        {
            return null;
        }

        var themeNode = reportNode["theme"];
        if (themeNode is null)
        {
            return null;
        }

        var themeName = themeNode["name"]?.GetValue<string>();
        var themeHref = themeNode["href"]?.GetValue<string>();

        // Resolve custom theme file path if href is a relative path inside the definition folder.
        string? themeSourcePath = null;
        if (!string.IsNullOrWhiteSpace(themeHref))
        {
            var candidate = Path.IsPathRooted(themeHref)
                ? themeHref
                : Path.Combine(location.DefinitionPath, themeHref);

            if (File.Exists(candidate))
            {
                themeSourcePath = ToWorkspaceRelativePath(candidate, basePath);
            }
            else
            {
                themeSourcePath = themeHref; // preserve href as-is (may be a URL or external ref)
            }
        }

        return new
        {
            name       = themeName,
            sourcePath = themeSourcePath,
        };
    }

    // Convert absolute path to workspace-relative path.
    private string ToWorkspaceRelativePath(string path, string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return path;
        }

        return Path.GetRelativePath(basePath, path);
    }
}
