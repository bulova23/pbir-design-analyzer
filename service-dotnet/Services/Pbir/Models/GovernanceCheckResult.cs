namespace PowerBIModelingService.Services.Pbir.Models;

/// <summary>
/// The result of evaluating a <see cref="GovernancePolicy"/> against a scored PBIR report.
/// </summary>
public sealed class GovernanceCheckResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the report is blocked from publishing.
    /// <c>true</c> if one or more governance rules failed; <c>false</c> if the report passes.
    /// </summary>
    public bool Blocked { get; set; }

    /// <summary>
    /// Gets or sets the list of human-readable reasons explaining why the report is blocked.
    /// Empty when <see cref="Blocked"/> is <c>false</c>.
    /// </summary>
    public List<string> Reasons { get; set; } = [];

    /// <summary>
    /// Gets or sets the composite score that was evaluated against the policy threshold.
    /// </summary>
    public double EvaluatedScore { get; set; }

    /// <summary>
    /// Gets or sets the minimum threshold required by the policy.
    /// </summary>
    public double RequiredThreshold { get; set; }

    /// <summary>
    /// Gets or sets the theme identifier that was evaluated (report path used as proxy if theme name unavailable).
    /// </summary>
    public string? EvaluatedThemeId { get; set; }

    /// <summary>
    /// Gets or sets the optional notes from the governance policy (surfaced to the report author on failure).
    /// </summary>
    public string? PolicyNotes { get; set; }
}
