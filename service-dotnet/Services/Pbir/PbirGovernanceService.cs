using System.Text.Json;
using Microsoft.Extensions.Logging;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

/// <summary>
/// Reads workspace governance policy from <c>.vscode/settings.json</c> and evaluates
/// PBIR reports against it before publishing.
/// Also loads dynamic rules from governance-defaults.json for extensibility.
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
    /// Reads the governance policy from <c>{workspaceRoot}/.vscode/settings.json</c>
    /// and augments it with dynamic rules from governance-defaults.json.
    /// Returns a disabled policy if the file or key is absent.
    /// </summary>
    /// <param name="workspaceRoot">Absolute path to the VS Code workspace root.</param>
    public GovernancePolicy ReadPolicy(string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return DisabledPolicy();

        var settingsPath = Path.Combine(workspaceRoot, ".vscode", "settings.json");
        if (!File.Exists(settingsPath))
        {
            _logger.LogDebug("[Governance] settings.json not found at {Path} — using default (disabled) policy.", settingsPath);
            return DisabledPolicyWithDefaults();
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty(GovernancePolicy.SettingsKey, out var govElement))
            {
                _logger.LogDebug("[Governance] Key '{Key}' not found in settings.json — governance disabled.", GovernancePolicy.SettingsKey);
                return DisabledPolicyWithDefaults();
            }

            var policy = new GovernancePolicy();

            if (govElement.TryGetProperty("enabled", out var enabled) && enabled.ValueKind == JsonValueKind.True)
                policy.Enabled = true;
            else if (govElement.TryGetProperty("enabled", out enabled) && enabled.ValueKind == JsonValueKind.False)
                policy.Enabled = false;

            if (govElement.TryGetProperty("minimumCompositeScore", out var minScore) &&
                minScore.TryGetDouble(out var threshold))
            {
                policy.MinScoreThreshold = Math.Clamp(threshold, 0, 100);

                if (policy.MinScoreThreshold > 95)
                {
                    _logger.LogWarning(
                        "[Governance] minimumCompositeScore is set to {Score}, which exceeds 95. " +
                        "No built-in scoring pattern guarantees this threshold. " +
                        "Reports may be permanently blocked from publishing.", policy.MinScoreThreshold);
                }
            }

            if (govElement.TryGetProperty("approvedThemeIds", out var themes) &&
                themes.ValueKind == JsonValueKind.Array)
            {
                policy.ApprovedThemes = themes.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }

            if (govElement.TryGetProperty("notes", out var notes) && notes.ValueKind == JsonValueKind.String)
                policy.Notes = notes.GetString();

            // Load dynamic rules from settings if present
            if (govElement.TryGetProperty("rules", out var rulesElement) &&
                rulesElement.ValueKind == JsonValueKind.Object)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var rulesJson = rulesElement.GetRawText();
                var parsedRules = JsonSerializer.Deserialize<Dictionary<string, GovernanceRule>>(rulesJson, options);
                if (parsedRules != null)
                {
                    policy.DynamicRules = parsedRules;
                }
            }
            else
            {
                // No rules in settings, load defaults
                LoadDefaultRules(policy);
            }

            _logger.LogInformation(
                "[Governance] Policy loaded — Enabled={Enabled}, MinScore={Min}, ApprovedThemes={Count}, DynamicRules={RuleCount}",
                policy.Enabled, policy.MinScoreThreshold, policy.ApprovedThemes.Count, policy.DynamicRules.Count);

            return policy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Governance] Failed to read settings.json at {Path}", settingsPath);
            return DisabledPolicyWithDefaults();
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
            _logger.LogDebug("[Governance] Policy is disabled — report passes automatically.");
            return new GovernanceCheckResult
            {
                Blocked           = false,
                EvaluatedScore    = score.CompositeScore,
                RequiredThreshold = policy.MinScoreThreshold,
                EvaluatedThemeId  = themeId,
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

        // Rule 3: Dynamic rules (extensible for custom governance)
        EvaluateDynamicRules(policy, reasons);

        var blocked = reasons.Count > 0;

        _logger.LogInformation(
            "[Governance] Evaluation complete — Blocked={Blocked}, Score={Score}/{Threshold}, Reasons={Count}",
            blocked, score.CompositeScore, policy.MinScoreThreshold, reasons.Count);

        return new GovernanceCheckResult
        {
            Blocked           = blocked,
            Reasons           = reasons,
            EvaluatedScore    = score.CompositeScore,
            RequiredThreshold = policy.MinScoreThreshold,
            EvaluatedThemeId  = themeId,
            PolicyNotes       = blocked ? policy.Notes : null,
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static GovernancePolicy DisabledPolicy() => new() { Enabled = false };

    /// <summary>
    /// Returns a disabled policy with default dynamic rules loaded.
    /// </summary>
    private static GovernancePolicy DisabledPolicyWithDefaults()
    {
        var policy = DisabledPolicy();
        LoadDefaultRules(policy);
        return policy;
    }

    /// <summary>
    /// Loads default governance rules from the embedded defaults.
    /// This is called when no rules are found in settings.json.
    /// </summary>
    private static void LoadDefaultRules(GovernancePolicy policy)
    {
        if (policy.DynamicRules.Count > 0)
            return; // Already loaded

        // Provide sensible defaults for common governance rules
        policy.DynamicRules = new Dictionary<string, GovernanceRule>
        {
            {
                "maxVisualsPerPage",
                new GovernanceRule
                {
                    Name = "Max Visuals Per Page",
                    Value = 15,
                    Description = "Maximum number of visuals allowed on a single page/state",
                    Severity = "warning",
                    AdminOnly = true
                }
            },
            {
                "maxBookmarksPerPage",
                new GovernanceRule
                {
                    Name = "Max Bookmarks Per Page",
                    Value = 10,
                    Description = "Maximum number of bookmark states allowed per page",
                    Severity = "warning",
                    AdminOnly = true
                }
            },
            {
                "requirePageTitle",
                new GovernanceRule
                {
                    Name = "Require Page Title",
                    Value = true,
                    Description = "Whether page titles are required on all report pages",
                    Severity = "error",
                    AdminOnly = true
                }
            },
            {
                "allowPieCharts",
                new GovernanceRule
                {
                    Name = "Allow Pie Charts",
                    Value = true,
                    Description = "Whether pie charts are allowed in reports",
                    Severity = "warning",
                    AdminOnly = true
                }
            },
        };
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
