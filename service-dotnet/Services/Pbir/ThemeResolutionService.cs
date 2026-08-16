using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace PowerBIModelingService.Services.Pbir;

internal sealed class ThemeResolutionService
{
    private readonly ILogger _logger;

    public ThemeResolutionService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal List<string> ResolveThemeColors(JsonObject reportJson, PbirReportLocation location)
    {
        var themeNode = reportJson["theme"];
        if (themeNode is null)
        {
            return [];
        }

        var href = themeNode["href"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(href) && !href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var themeFilePath = Path.GetFullPath(
                Path.Combine(location.WorkspaceRootPath, href.TrimStart('/', '\\')));

            if (File.Exists(themeFilePath))
            {
                return ParseThemeFile(themeFilePath);
            }
        }

        return [];
    }

    private List<string> ParseThemeFile(string filePath)
    {
        try
        {
            var json = ReadJsonObject(filePath);
            if (json["dataColors"] is JsonArray arr)
            {
                return arr
                    .Select(node => node?.GetValue<string>())
                    .Where(color => !string.IsNullOrWhiteSpace(color) && color!.StartsWith('#'))
                    .Select(color => color!)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Scoring] Could not parse theme file: {Path}", filePath);
        }

        return [];
    }

    private static JsonObject ReadJsonObject(string filePath)
    {
        var text = File.ReadAllText(filePath);
        return JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidOperationException($"File is not a JSON object: {filePath}");
    }
}
