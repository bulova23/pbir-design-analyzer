namespace PowerBIModelingService.Services.Pbir;

/// <summary>
/// Generates layout states from bookmark definitions.
/// A layout state represents a specific combination of visual visibility states.
/// </summary>
public sealed class LayoutStateGenerator
{
    /// <summary>
    /// Represents a single layout state (a specific visibility configuration of visuals).
    /// </summary>
    public sealed record LayoutState(
        string StateName,
        List<string> VisibleVisualIds,
        List<string> HiddenVisualIds);

    /// <summary>
    /// Generates layout states for a page based on its bookmarks.
    /// If no bookmarks exist, returns a single "Default" state with all visuals visible.
    /// If bookmarks exist, returns one state per bookmark plus the "Default" state.
    /// </summary>
    /// <param name="pageVisualIds">All visual IDs on the page.</param>
    /// <param name="bookmarks">Bookmarks affecting this page (from BookmarkParser).</param>
    /// <returns>List of LayoutState objects representing all possible visibility configurations.</returns>
    public static List<LayoutState> GenerateStates(List<string> pageVisualIds, List<BookmarkParser.BookmarkDefinition> bookmarks)
    {
        var states = new List<LayoutState>();

        // Default state: all visuals visible
        states.Add(new LayoutState(
            "Default",
            pageVisualIds.ToList(),
            []));

        // If no bookmarks, return only the default state
        if (bookmarks.Count == 0)
        {
            return states;
        }

        // For each bookmark, create a state with its visibility configuration
        foreach (var bookmark in bookmarks)
        {
            // Bookmark shows some visuals, hides others
            var visibleIds = pageVisualIds
                .Where(id => bookmark.ControlledVisualIds.Contains(id))
                .ToList();

            var hiddenIds = pageVisualIds
                .Where(id => !bookmark.ControlledVisualIds.Contains(id))
                .ToList();

            states.Add(new LayoutState(
                bookmark.DisplayName,
                visibleIds,
                hiddenIds));
        }

        return states;
    }

    /// <summary>
    /// Counts the number of unique layout states for a page.
    /// Returns 1 if no bookmarks, or N+1 if N bookmarks exist (default + one per bookmark).
    /// </summary>
    public static int CountLayoutStates(List<BookmarkParser.BookmarkDefinition> bookmarks)
    {
        // Default state + one per bookmark
        return 1 + bookmarks.Count;
    }
}
