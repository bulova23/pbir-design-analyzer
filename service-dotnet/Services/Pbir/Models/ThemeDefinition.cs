namespace PowerBIModelingService.Services.Pbir.Models;

/// <summary>
/// Stores a validated colour palette definition along with WCAG-computed accessibility metadata.
/// Used by <see cref="PbirThemeService"/> for import and by the scoring engine for the Theme
/// (Visual Best Practices) sub-score.
/// </summary>
public sealed class ThemeDefinition
{
    /// <summary>Gets or sets the theme name (matches the <c>name</c> field in the theme JSON).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordered list of hex colour values in the palette (e.g. <c>"#1F4E79"</c>).
    /// Duplicates are removed during import. Maximum of 8 colours is enforced.
    /// </summary>
    public List<string> HexColors { get; set; } = [];

    /// <summary>Gets or sets the number of colour pairs in the palette that pass WCAG 2.1 AA normal-text (≥ 4.5:1) contrast against white.</summary>
    public int WcagAaPassCount { get; set; }

    /// <summary>Gets or sets the number of colour pairs that fail WCAG 2.1 AA normal-text contrast against white.</summary>
    public int WcagAaFailCount { get; set; }

    /// <summary>Gets or sets the source path or URL this theme was imported from. Null for themes created in-wizard.</summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// Gets or sets human-readable descriptions of WCAG 2.1 AA contrast violations found during validation.
    /// Empty when every colour pair passes.
    /// </summary>
    public List<string> WcagViolations { get; set; } = [];
}
