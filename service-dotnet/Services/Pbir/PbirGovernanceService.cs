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
        EvaluateDynamicRules(policy, reasons);

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
    /// Evaluates dynamic rules from the policy. This is extensible for custom governance logic.
    /// Currently a placeholder for rule evaluation; organizations can override with custom rules.
    /// </summary>
    private void EvaluateDynamicRules(GovernancePolicy policy, List<string> reasons)
    {
        if (policy.DynamicRules.Count == 0)
            return;

        // Placeholder for dynamic rule evaluation.
        // Organizations can add custom rules here and they will be evaluated against reports.
        // Example: if a rule value is "requirePageTitle" and it's true, check that pages have titles.

        _logger.LogDebug("[Governance] Evaluating {RuleCount} dynamic rules.", policy.DynamicRules.Count);
    }
}
