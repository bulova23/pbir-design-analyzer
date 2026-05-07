using System.Text.Json;
using System.Text.Json.Nodes;

namespace PowerBIModelingService.Services.Pbir;

/// <summary>
/// Parses Power BI bookmark definitions from report.json.
/// Extracts bookmark metadata including name, target visuals, and display state.
/// </summary>
public sealed class BookmarkParser
{
    /// <summary>
    /// Represents a single bookmark and the visuals it controls.
    /// </summary>
    public sealed record BookmarkDefinition(
        string Id,
        string DisplayName,
        List<string> ControlledVisualIds,
        string? Description = null);

    /// <summary>
    /// Parses all bookmarks from report.json.
    /// Returns a list of BookmarkDefinition records.
    /// If no bookmarks found, returns empty list (not null).
    /// </summary>
    /// <param name="reportJson">The report.json root object.</param>
    /// <returns>List of bookmarks found in the report, or empty list if none.</returns>
    public static List<BookmarkDefinition> ParseBookmarks(JsonObject reportJson)
    {
        var bookmarks = new List<BookmarkDefinition>();

        // Try to access bookmarks from report.json
        // Power BI stores bookmarks in the report definition
        if (reportJson["bookmarks"] is not JsonArray bookmarksArray)
        {
            return bookmarks; // No bookmarks found
        }

        foreach (var item in bookmarksArray)
        {
            if (item is not JsonObject bookmarkJson) continue;

            var id = bookmarkJson["id"]?.GetValue<string>() ?? string.Empty;
            if (string.IsNullOrEmpty(id)) continue;

            var displayName = bookmarkJson["displayName"]?.GetValue<string>() ?? id;
            var description = bookmarkJson["description"]?.GetValue<string>();

            // Extract controlled visuals from bookmark actions
            var controlledVisualIds = ExtractControlledVisuals(bookmarkJson);

            bookmarks.Add(new BookmarkDefinition(id, displayName, controlledVisualIds, description));
        }

        return bookmarks;
    }

    /// <summary>
    /// Extracts visual IDs that are controlled by a bookmark's actions.
    /// Bookmarks typically control visibility, filtering, or other properties of specific visuals.
    /// </summary>
    private static List<string> ExtractControlledVisuals(JsonObject bookmarkJson)
    {
        var visualIds = new List<string>();

        // Look for bookmarks in the state section
        if (bookmarkJson["state"] is not JsonObject stateJson)
        {
            return visualIds;
        }

        // Iterate through pages in state
        foreach (var pageItem in stateJson.ToList())
        {
            if (pageItem.Value is not JsonObject pageState) continue;

            // Each page can have visual states
            if (pageState["visuals"] is not JsonObject visualsObject)
            {
                continue;
            }

            // Extract visual IDs
            foreach (var visualItem in visualsObject.ToList())
            {
                var visualId = visualItem.Key;
                if (!string.IsNullOrEmpty(visualId))
                {
                    visualIds.Add(visualId);
                }
            }
        }

        return visualIds;
    }

    /// <summary>
    /// Checks if a specific visual is hidden in a given bookmark state.
    /// </summary>
    /// <param name="bookmarkJson">The bookmark definition.</param>
    /// <param name="visualId">The visual ID to check.</param>
    /// <returns>True if the visual is hidden in this bookmark state, false otherwise.</returns>
    public static bool IsVisualHiddenInBookmark(JsonObject bookmarkJson, string visualId)
    {
        if (bookmarkJson["state"] is not JsonObject stateJson)
        {
            return false;
        }

        // Iterate through pages to find this visual's state
        foreach (var pageItem in stateJson.ToList())
        {
            if (pageItem.Value is not JsonObject pageState) continue;

            if (pageState["visuals"] is not JsonObject visualsObject)
            {
                continue;
            }

            if (visualsObject[visualId] is not JsonObject visualState)
            {
                continue;
            }

            // Check if visual is marked as hidden
            if (visualState["state"] is JsonObject stateProps)
            {
                if (stateProps["visibility"]?.GetValue<string>() == "hidden")
                {
                    return true;
                }
            }
        }

        return false;
    }
}
