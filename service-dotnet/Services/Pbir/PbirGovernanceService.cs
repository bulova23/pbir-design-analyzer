using System.Text.Json;
using Microsoft.Extensions.Logging;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

/// <summary>
/// Reads workspace governance policy from <c>.vscode/settings.json</c> and evaluates
/// PBIR reports against it before publishing.
/// </summary>
public sealed class PbirGovernanceService
{
    private readonly ILogger<PbirGovernanceService> _logger;

    public PbirGovernanceService(ILogger<PbirGovernanceService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Policy reading ───────────────────────────────────────────────────────

    /// <summary>
    /// Reads the governance policy from <c>{workspaceRoot}/.vscode/settings.json</c>.
    /// Supports the current flat VS Code settings keys and the older nested object format
    /// for backward compatibility. Returns a disabled policy if nothing is configured.
    /// </summary>
    /// <param name="workspaceRoot">Absolute path to the VS Code workspace root.</param>
    public GovernancePolicy ReadPolicy(string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return DisabledPolicy();

        var settingsPath = Path.Combine(workspaceRoot, ".vscode", "settings.json");
        if (!File.Exists(settingsPath))
        {
            _logger.LogDebug("[Governance] settings.json not found at {Path} — governance not configured.", settingsPath);
            return DisabledPolicy();
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            using var doc = JsonDocument.Parse(json);

            if (TryReadFlatPolicy(doc.RootElement, out var flatPolicy))
            {
                LogLoadedPolicy(flatPolicy, "flat");
                return flatPolicy;
            }

            if (TryReadLegacyPolicy(doc.RootElement, out var legacyPolicy))
            {
                _logger.LogInformation("[Governance] Loaded legacy nested governance settings from '{Key}'. Prefer flat VS Code settings keys going forward.", GovernancePolicy.SettingsKey);
                LogLoadedPolicy(legacyPolicy, "legacy");
                return legacyPolicy;
            }

            _logger.LogDebug("[Governance] No governance settings found in {Path} — governance not configured.", settingsPath);
            return DisabledPolicy();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Governance] Failed to read settings.json at {Path}", settingsPath);
            return DisabledPolicy();
        }
    }

    // ── Evaluation ───────────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates a scored report against the given governance policy.
    /// </summary>
    /// <param name="policy">The governance policy to check against.</param>
    /// <param name="score">The score result from <c>PbirScoringService</c>.</param>
    /// <param name="themeId">The theme identifier (name or file path) to check against the approved list.</param>
    public GovernanceCheckResult Evaluate(GovernancePolicy policy, ScoreResult score, string? themeId)
    {
        if (!policy.Enabled)
        {
            var policyState = policy.IsConfigured ? "disabled" : "notConfigured";
            var statusMessage = policy.IsConfigured
                ? "Workspace governance is configured but disabled. Publish blocking is off."
                : "No workspace governance policy is enabled. Publish blocking is off until a workspace policy is explicitly enabled.";

            _logger.LogDebug("[Governance] Policy is {State} — report is not blocked.", policyState);
            return new GovernanceCheckResult
            {
                PolicyState       = policyState,
                PolicyConfigured  = policy.IsConfigured,
                PolicyEnabled     = false,
                StatusMessage     = statusMessage,
                Blocked           = false,
                EvaluatedScore    = score.CompositeScore,
                RequiredThreshold = policy.IsConfigured ? policy.MinScoreThreshold : 0,
                EvaluatedThemeId  = string.IsNullOrWhiteSpace(themeId) ? null : themeId,
            };
        }

        var reasons = new List<string>();

        // Rule 1: composite score threshold
        if (score.CompositeScore < policy.MinScoreThreshold)
        {
            reasons.Add(
                $"Composite score {score.CompositeScore:F1} is below the minimum required score of {policy.MinScoreThreshold:F1}. " +
                "Review the analyzer recommendations and improve layout, fonts, and colour variance before publishing.");
        }

        // Rule 2: approved theme list (empty list = all themes allowed)
        if (policy.ApprovedThemes.Count > 0)
        {
            var normalised = (themeId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalised))
            {
                reasons.Add(
                    "Theme validation is enabled, but no theme name was supplied. " +
                    "Re-run the governance check and enter the report theme name.");
            }
            else
            {
                var approved = policy.ApprovedThemes
                    .Any(t => string.Equals(t, normalised, StringComparison.OrdinalIgnoreCase));

                if (!approved)
                {
                    var approvedList = string.Join(", ", policy.ApprovedThemes.Select(t => $"'{t}'"));
                    reasons.Add(
                        $"Theme '{normalised}' is not on the approved list. " +
                        $"Approved themes: {approvedList}. Update the report theme to match an approved option before publishing.");
                }
            }
        }

        // Rule 3: Dynamic rules (extensible for custom governance)
        EvaluateDynamicRules(policy, score, themeId, reasons);

        var blocked = reasons.Count > 0;

        _logger.LogInformation(
            "[Governance] Evaluation complete — Blocked={Blocked}, Score={Score}/{Threshold}, Reasons={Count}",
            blocked, score.CompositeScore, policy.MinScoreThreshold, reasons.Count);

        return new GovernanceCheckResult
        {
            PolicyState       = "enabled",
            PolicyConfigured  = true,
            PolicyEnabled     = true,
            StatusMessage     = blocked
                ? "Workspace governance policy blocked publishing."
                : "Workspace governance policy passed.",
            Blocked           = blocked,
            Reasons           = reasons,
            EvaluatedScore    = score.CompositeScore,
            RequiredThreshold = policy.MinScoreThreshold,
            EvaluatedThemeId  = string.IsNullOrWhiteSpace(themeId) ? null : themeId,
            PolicyNotes       = blocked ? policy.Notes : null,
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static GovernancePolicy DisabledPolicy() => new()
    {
        Enabled = false,
        IsConfigured = false,
    };

    /// <summary>
    /// Tries to read governance from flat VS Code settings keys such as
    /// <c>powerbi-modeling.governance.enabled</c>.
    /// </summary>
    private static bool TryReadFlatPolicy(JsonElement root, out GovernancePolicy policy)
    {
        policy = DisabledPolicy();

        var found = false;
        policy = new GovernancePolicy();

        if (root.TryGetProperty(GovernancePolicy.EnabledSettingsKey, out var enabled))
        {
            found = true;
            if (enabled.ValueKind is JsonValueKind.True or JsonValueKind.False)
                policy.Enabled = enabled.GetBoolean();
        }

        if (root.TryGetProperty(GovernancePolicy.MinimumCompositeScoreSettingsKey, out var minScore) &&
            minScore.TryGetDouble(out var threshold))
        {
            found = true;
            policy.MinScoreThreshold = Math.Clamp(threshold, 0, 100);
        }

        if (root.TryGetProperty(GovernancePolicy.ApprovedThemeIdsSettingsKey, out var themes) &&
            themes.ValueKind == JsonValueKind.Array)
        {
            found = true;
            policy.ApprovedThemes = ReadThemes(themes);
        }

        if (root.TryGetProperty(GovernancePolicy.NotesSettingsKey, out var notes) &&
            notes.ValueKind == JsonValueKind.String)
        {
            found = true;
            policy.Notes = notes.GetString();
        }

        if (root.TryGetProperty(GovernancePolicy.RulesSettingsKey, out var rulesElement) &&
            rulesElement.ValueKind == JsonValueKind.Object)
        {
            found = true;
            policy.DynamicRules = ReadRules(rulesElement);
        }

        if (!found)
        {
            policy = DisabledPolicy();
            return false;
        }

        policy.IsConfigured = true;
        return true;
    }

    /// <summary>
    /// Tries to read governance from the older nested settings object
    /// at key <c>powerbi-modeling.governance</c>.
    /// </summary>
    private static bool TryReadLegacyPolicy(JsonElement root, out GovernancePolicy policy)
    {
        policy = DisabledPolicy();
        if (!root.TryGetProperty(GovernancePolicy.SettingsKey, out var govElement) ||
            govElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        policy = new GovernancePolicy
        {
            IsConfigured = true,
        };

        PopulatePolicyFromElement(policy, govElement);
        return true;
    }

    private static void PopulatePolicyFromElement(GovernancePolicy policy, JsonElement govElement)
    {
        if (govElement.TryGetProperty("enabled", out var enabled) &&
            enabled.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            policy.Enabled = enabled.GetBoolean();
        }

        if (govElement.TryGetProperty("minimumCompositeScore", out var minScore) &&
            minScore.TryGetDouble(out var threshold))
        {
            policy.MinScoreThreshold = Math.Clamp(threshold, 0, 100);
        }

        if (govElement.TryGetProperty("approvedThemeIds", out var themes) &&
            themes.ValueKind == JsonValueKind.Array)
        {
            policy.ApprovedThemes = ReadThemes(themes);
        }

        if (govElement.TryGetProperty("notes", out var notes) &&
            notes.ValueKind == JsonValueKind.String)
        {
            policy.Notes = notes.GetString();
        }

        if (govElement.TryGetProperty("rules", out var rulesElement) &&
            rulesElement.ValueKind == JsonValueKind.Object)
        {
            policy.DynamicRules = ReadRules(rulesElement);
        }
    }

    private static List<string> ReadThemes(JsonElement themes) =>
        themes.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

    private static Dictionary<string, GovernanceRule> ReadRules(JsonElement rulesElement)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rulesJson = rulesElement.GetRawText();
        return JsonSerializer.Deserialize<Dictionary<string, GovernanceRule>>(rulesJson, options)
            ?? new Dictionary<string, GovernanceRule>();
    }

    private void LogLoadedPolicy(GovernancePolicy policy, string format)
    {
        if (policy.MinScoreThreshold > 95)
        {
            _logger.LogWarning(
                "[Governance] minimumCompositeScore is set to {Score}, which exceeds 95. " +
                "No built-in scoring pattern guarantees this threshold. " +
                "Reports may be permanently blocked from publishing.", policy.MinScoreThreshold);
        }

        _logger.LogInformation(
            "[Governance] Policy loaded from {Format} settings — Configured={Configured}, Enabled={Enabled}, MinScore={Min}, ApprovedThemes={Count}, DynamicRules={RuleCount}",
            format, policy.IsConfigured, policy.Enabled, policy.MinScoreThreshold, policy.ApprovedThemes.Count, policy.DynamicRules.Count);
    }

    /// <summary>
    /// Known first-party Power BI visual type identifiers (case-insensitive). Anything outside this
    /// set is treated as a custom (third-party) visual for the <c>allowCustomVisuals</c> rule.
    /// </summary>
    private static readonly HashSet<string> _knownVisualTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "barChart", "columnChart", "clusteredBarChart", "clusteredColumnChart",
        "stackedBarChart", "stackedColumnChart", "hundredPercentStackedBarChart", "hundredPercentStackedColumnChart",
        "lineChart", "areaChart", "stackedAreaChart", "lineStackedColumnComboChart", "lineClusteredColumnComboChart",
        "pieChart", "donutChart",
        "scatterChart", "treemap", "waterfallChart", "funnel", "gauge",
        "card", "multiRowCard", "kpi",
        "tableEx", "pivotTable", "matrix",
        "slicer", "filterSlicer", "advancedSlicer",
        "map", "filledMap", "shapeMap", "azureMap",
        "image", "textbox", "shape", "basicShape", "actionButton", "navigationButton",
        "pageNavigator", "bookmarkNavigator",
        "decompositionTreeVisual", "qnaVisual", "keyDriversVisual", "aiNarrativesVisual",
        "ribbonChart",
    };

    /// <summary>
    /// Evaluates dynamic governance rules from <see cref="GovernancePolicy.DynamicRules"/> against the
    /// scored report. Each recognized rule reads the relevant facts from <paramref name="score"/>
    /// (per-page visual counts, hidden visuals, slicers, pie usage, custom visual presence, white space,
    /// visible page titles) or from <paramref name="themeId"/> and appends a human-readable reason to
    /// <paramref name="reasons"/> when the rule is violated.
    ///
    /// <para>Recognized rules (all 10 ship in <c>governance-defaults.json</c>):</para>
    /// <list type="bullet">
    /// <item><c>maxVisualsPerPage</c> — per-page visible-visual count must be ≤ value.</item>
    /// <item><c>maxHiddenVisuals</c> — total hidden-visual count must be ≤ value.</item>
    /// <item><c>minWhiteSpaceRatio</c> — per-page <c>1 - occupiedArea/canvasArea</c> must be ≥ value.</item>
    /// <item><c>allowPieCharts</c> — when <c>false</c>, pie/donut charts block publishing.</item>
    /// <item><c>allowCustomVisuals</c> — when <c>false</c>, third-party visual types block publishing.</item>
    /// <item><c>requirePageTitle</c> — when <c>true</c>, every page must have a strict visible title
    /// (top-band, non-vague). Uses <see cref="PageVisualMetadataSummary.StrictVisiblePageTitle"/>.</item>
    /// <item><c>requireFilterPanel</c> — when <c>true</c>, every page must include at least one slicer.</item>
    /// <item><c>themeStandard</c> — the report theme must match the configured standard.</item>
    /// </list>
    ///
    /// <para>Deferred rules (recognized but not yet enforced; require per-page bookmark/state data
    /// that lands with REC-01):</para>
    /// <list type="bullet">
    /// <item><c>maxBookmarksPerPage</c></item>
    /// <item><c>maxLayoutStatesPerPage</c></item>
    /// </list>
    /// </summary>
    private void EvaluateDynamicRules(
        GovernancePolicy policy,
        ScoreResult score,
        string? themeId,
        List<string> reasons)
    {
        if (policy.DynamicRules.Count == 0)
            return;

        _logger.LogDebug("[Governance] Evaluating {RuleCount} dynamic rules.", policy.DynamicRules.Count);

        foreach (var (ruleId, rule) in policy.DynamicRules)
        {
            try
            {
                switch (ruleId)
                {
                    case "maxVisualsPerPage":
                        EvaluateMaxVisualsPerPage(rule, score, reasons);
                        break;
                    case "maxHiddenVisuals":
                        EvaluateMaxHiddenVisuals(rule, score, reasons);
                        break;
                    case "minWhiteSpaceRatio":
                        EvaluateMinWhiteSpaceRatio(rule, score, reasons);
                        break;
                    case "allowPieCharts":
                        EvaluateAllowPieCharts(rule, score, reasons);
                        break;
                    case "allowCustomVisuals":
                        EvaluateAllowCustomVisuals(rule, score, reasons);
                        break;
                    case "requirePageTitle":
                        EvaluateRequirePageTitle(rule, score, reasons);
                        break;
                    case "requireFilterPanel":
                        EvaluateRequireFilterPanel(rule, score, reasons);
                        break;
                    case "themeStandard":
                        EvaluateThemeStandard(rule, themeId, reasons);
                        break;
                    case "maxBookmarksPerPage":
                    case "maxLayoutStatesPerPage":
                        _logger.LogDebug(
                            "[Governance] Rule '{RuleId}' is recognized but not yet enforced; requires per-page bookmark data.",
                            ruleId);
                        break;
                    default:
                        _logger.LogDebug(
                            "[Governance] Rule '{RuleId}' is not a recognized governance rule and was skipped.",
                            ruleId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[Governance] Rule '{RuleId}' threw during evaluation and was skipped.",
                    ruleId);
            }
        }
    }

    // ── Per-rule evaluators ──────────────────────────────────────────────────

    private static void EvaluateMaxVisualsPerPage(GovernanceRule rule, ScoreResult score, List<string> reasons)
    {
        if (!TryGetIntegerThreshold(rule.Value, out var threshold) || threshold < 0)
            return;

        var pagesOverLimit = EnumeratePageMetadata(score)
            .Where(entry => CountVisibleVisuals(entry.Metadata) > threshold)
            .Select(entry => $"'{entry.PageName}' has {CountVisibleVisuals(entry.Metadata)} visible visuals")
            .ToList();

        if (pagesOverLimit.Count > 0)
        {
            reasons.Add(
                $"Rule 'maxVisualsPerPage': {pagesOverLimit.Count} page(s) exceed the configured limit of {threshold} visible visual(s): " +
                $"{string.Join("; ", pagesOverLimit)}. Reduce the per-page visual count or split the page.");
        }
    }

    private static void EvaluateMaxHiddenVisuals(GovernanceRule rule, ScoreResult score, List<string> reasons)
    {
        if (!TryGetIntegerThreshold(rule.Value, out var threshold) || threshold < 0)
            return;

        int totalHidden = score.HiddenVisualCount;
        if (totalHidden > threshold)
        {
            reasons.Add(
                $"Rule 'maxHiddenVisuals': report contains {totalHidden} hidden visual(s), which exceeds the configured limit of {threshold}. " +
                "Remove unused hidden visuals or reduce bookmark-driven overlays.");
        }
    }

    private static void EvaluateMinWhiteSpaceRatio(GovernanceRule rule, ScoreResult score, List<string> reasons)
    {
        if (!TryGetDoubleThreshold(rule.Value, out var threshold) || threshold <= 0 || threshold >= 1)
            return;

        var offenders = new List<string>();

        foreach (var (pageName, metadata) in EnumeratePageMetadata(score))
        {
            var ratio = ComputeWhiteSpaceRatio(metadata);
            if (ratio is double r && r < threshold)
            {
                offenders.Add($"'{pageName}' has {r * 100:F0}% white space");
            }
        }

        if (offenders.Count > 0)
        {
            reasons.Add(
                $"Rule 'minWhiteSpaceRatio': {offenders.Count} page(s) fall below the configured minimum of {threshold * 100:F0}% white space: " +
                $"{string.Join("; ", offenders)}. Increase spacing between visuals or reduce visual count.");
        }
    }

    private static void EvaluateAllowPieCharts(GovernanceRule rule, ScoreResult score, List<string> reasons)
    {
        if (rule.Value is not bool allowed || allowed)
            return;

        var pagesWithPies = new List<string>();
        foreach (var (pageName, metadata) in EnumeratePageMetadata(score))
        {
            if (metadata.Visuals.Any(v => !v.IsHidden && IsPieOrDonut(v.VisualType)))
            {
                pagesWithPies.Add($"'{pageName}'");
            }
        }

        if (pagesWithPies.Count > 0)
        {
            reasons.Add(
                $"Rule 'allowPieCharts': pie or donut charts were detected on {pagesWithPies.Count} page(s) ({string.Join(", ", pagesWithPies)}). " +
                "The current policy does not allow pie or donut charts — replace them with bar or column charts.");
        }
    }

    private static void EvaluateAllowCustomVisuals(GovernanceRule rule, ScoreResult score, List<string> reasons)
    {
        if (rule.Value is not bool allowed || allowed)
            return;

        var customByPage = new List<string>();
        foreach (var (pageName, metadata) in EnumeratePageMetadata(score))
        {
            var customTypes = metadata.Visuals
                .Where(v => !v.IsHidden && !string.IsNullOrWhiteSpace(v.VisualType))
                .Select(v => v.VisualType)
                .Where(t => !_knownVisualTypes.Contains(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (customTypes.Count > 0)
            {
                customByPage.Add($"'{pageName}' uses {string.Join(", ", customTypes)}");
            }
        }

        if (customByPage.Count > 0)
        {
            reasons.Add(
                $"Rule 'allowCustomVisuals': custom (third-party) visuals were detected: {string.Join("; ", customByPage)}. " +
                "The current policy does not allow custom visuals — replace them with built-in Power BI visuals.");
        }
    }

    private static void EvaluateRequirePageTitle(GovernanceRule rule, ScoreResult score, List<string> reasons)
    {
        if (rule.Value is not bool required || !required)
            return;

        var missing = EnumeratePageMetadata(score)
            .Where(entry => string.IsNullOrWhiteSpace(entry.Metadata.StrictVisiblePageTitle))
            .Select(entry => $"'{entry.PageName}'")
            .ToList();

        if (missing.Count > 0)
        {
            reasons.Add(
                $"Rule 'requirePageTitle': {missing.Count} page(s) lack a meaningful visible title in the top band ({string.Join(", ", missing)}). " +
                "Add a non-vague page title near the top of the canvas.");
        }
    }

    private static void EvaluateRequireFilterPanel(GovernanceRule rule, ScoreResult score, List<string> reasons)
    {
        if (rule.Value is not bool required || !required)
            return;

        var missing = EnumeratePageMetadata(score)
            .Where(entry => entry.Metadata.SlicerCount == 0)
            .Select(entry => $"'{entry.PageName}'")
            .ToList();

        if (missing.Count > 0)
        {
            reasons.Add(
                $"Rule 'requireFilterPanel': {missing.Count} page(s) have no slicer or filter control ({string.Join(", ", missing)}). " +
                "Add at least one slicer per page or relax the rule.");
        }
    }

    private static void EvaluateThemeStandard(GovernanceRule rule, string? themeId, List<string> reasons)
    {
        if (rule.Value is not string expected || string.IsNullOrWhiteSpace(expected))
            return;

        var normalizedThemeId = (themeId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedThemeId))
        {
            reasons.Add(
                $"Rule 'themeStandard': the configured standard theme is '{expected}', but no theme name was supplied for evaluation. " +
                "Re-run the governance check and enter the report theme name.");
            return;
        }

        if (!string.Equals(normalizedThemeId, expected, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add(
                $"Rule 'themeStandard': report theme '{normalizedThemeId}' does not match the configured standard theme '{expected}'. " +
                "Update the report theme to match the standard before publishing.");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IEnumerable<(string PageName, PageVisualMetadataSummary Metadata)> EnumeratePageMetadata(ScoreResult score)
    {
        if (score.PageScores is { Count: > 0 } pages)
        {
            foreach (var page in pages)
            {
                if (page.VisualMetadata is null)
                    continue;
                yield return (page.PageName, page.VisualMetadata);
            }
        }
        else if (score.VisualMetadata is not null)
        {
            yield return (score.VisualMetadata.PageName, score.VisualMetadata);
        }
    }

    private static int CountVisibleVisuals(PageVisualMetadataSummary metadata) =>
        metadata.Visuals.Count(v => !v.IsHidden);

    private static double? ComputeWhiteSpaceRatio(PageVisualMetadataSummary metadata)
    {
        if (metadata.CanvasWidth is not double width || metadata.CanvasHeight is not double height
            || width <= 0 || height <= 0)
        {
            return null;
        }

        double canvasArea = width * height;
        double occupied = 0;
        foreach (var visual in metadata.Visuals)
        {
            if (visual.IsHidden) continue;
            if (visual.Width <= 0 || visual.Height <= 0) continue;
            occupied += visual.Width * visual.Height;
        }

        // Clamp to [0, 1] so overlapping visuals (which would otherwise produce > canvasArea) still
        // yield a 0 white-space ratio rather than a negative number.
        double occupiedRatio = Math.Min(1.0, Math.Max(0.0, occupied / canvasArea));
        return 1.0 - occupiedRatio;
    }

    private static bool IsPieOrDonut(string visualType) =>
        visualType.Contains("pie", StringComparison.OrdinalIgnoreCase)
        || visualType.Contains("donut", StringComparison.OrdinalIgnoreCase);

    private static bool TryGetIntegerThreshold(object? value, out int threshold)
    {
        threshold = 0;
        switch (value)
        {
            case int i:
                threshold = i;
                return true;
            case long l when l >= int.MinValue && l <= int.MaxValue:
                threshold = (int)l;
                return true;
            case double d when !double.IsNaN(d):
                threshold = (int)Math.Round(d);
                return true;
            case string s when int.TryParse(s, out var parsed):
                threshold = parsed;
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetDoubleThreshold(object? value, out double threshold)
    {
        threshold = 0;
        switch (value)
        {
            case double d when !double.IsNaN(d):
                threshold = d;
                return true;
            case int i:
                threshold = i;
                return true;
            case long l:
                threshold = l;
                return true;
            case string s when double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed):
                threshold = parsed;
                return true;
            default:
                return false;
        }
    }
}
