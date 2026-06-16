using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PowerBIModelingService.Services.Pbir;

internal sealed class ScoringConfigurationService
{
    private readonly ILogger _logger;

    public ScoringConfigurationService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Dictionary<string, double> ExtractFrameworkWeights(JsonElement? config)
    {
        var weights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        if (!config.HasValue || config.Value.ValueKind != JsonValueKind.Object)
        {
            _logger.LogWarning("[ExtractFrameworkWeights] Config is null or not an object, using default Design Analyzer configuration");
            return GetDefaultFrameworkWeights();
        }

        if (config.Value.TryGetProperty("frameworks", out var frameworksArray) &&
            frameworksArray.ValueKind == JsonValueKind.Array)
        {
            _logger.LogInformation("[ExtractFrameworkWeights] Found {FrameworkCount} frameworks in config array", frameworksArray.GetArrayLength());

            foreach (var framework in frameworksArray.EnumerateArray())
            {
                if (framework.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                string? id = null;
                if (framework.TryGetProperty("id", out var idProp))
                {
                    id = idProp.GetString();
                }

                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var normalizedId = NormalizeFrameworkId(id);

                var isEnabled = false;
                if (framework.TryGetProperty("enabled", out var enabledProp))
                {
                    isEnabled = enabledProp.ValueKind == JsonValueKind.True;
                }

                double weight = 0;
                if (isEnabled && framework.TryGetProperty("weight", out var weightProp))
                {
                    try
                    {
                        weight = weightProp.GetDouble();
                    }
                    catch
                    {
                    }
                }

                weights[normalizedId] = weight;
                _logger.LogDebug("[ExtractFrameworkWeights]   {FrameworkId} → {NormalizedId}: enabled={Enabled}, weight={Weight}%", id, normalizedId, isEnabled, weight);
            }

            if (weights.Count > 0)
            {
                _logger.LogInformation(
                    "[ExtractFrameworkWeights] Extracted {WeightCount} framework weights: {Weights}",
                    weights.Count,
                    string.Join(", ", weights.Select(kv => $"{kv.Key}={kv.Value}%")));
            }
            else
            {
                _logger.LogWarning("[ExtractFrameworkWeights] Frameworks array was present but no enabled frameworks found!");
            }

            return weights;
        }

        _logger.LogWarning("[ExtractFrameworkWeights] No 'frameworks' array found in config, trying legacy format");

        var legacyFrameworks = new[]
        {
            "gestalt", "cognitiveLoad", "dataInk", "graphicalPerception", "accessibility",
            "visualBestPractices", "governance", "stephenFew", "tufte", "density", "narrative"
        };

        foreach (var framework in legacyFrameworks)
        {
            if (config.Value.TryGetProperty(framework, out var frameworkObj) &&
                frameworkObj.ValueKind == JsonValueKind.Object)
            {
                var isEnabled = true;
                if (frameworkObj.TryGetProperty("enabled", out var enabledProp))
                {
                    isEnabled = enabledProp.ValueKind == JsonValueKind.True;
                }

                double weight = 0;
                if (isEnabled && frameworkObj.TryGetProperty("weight", out var weightProp))
                {
                    try
                    {
                        weight = weightProp.GetDouble();
                    }
                    catch
                    {
                    }
                }

                weights[framework] = weight;
                _logger.LogDebug("[ExtractFrameworkWeights] (legacy) {Framework}: enabled={Enabled}, weight={Weight}%", framework, isEnabled, weight);
            }
        }

        if (weights.Count > 0)
        {
            _logger.LogInformation(
                "[ExtractFrameworkWeights] Extracted {WeightCount} legacy weights: {Weights}",
                weights.Count,
                string.Join(", ", weights.Select(kv => $"{kv.Key}={kv.Value}%")));
            return weights;
        }

        _logger.LogWarning("[ExtractFrameworkWeights] No weights found in either new or legacy format - using default Design Analyzer configuration");
        return GetDefaultFrameworkWeights();
    }

    public NavigationScoringSettings ExtractNavigationScoringSettings(JsonElement? config)
    {
        var defaults = new NavigationScoringSettings(
            Enabled: true,
            WeightPercent: 25,
            WarningNavigationCount: 8,
            WarningHiddenVisualCount: 5);

        if (!config.HasValue || config.Value.ValueKind != JsonValueKind.Object)
        {
            return defaults;
        }

        if (!config.Value.TryGetProperty("navigationScoring", out var navigationScoring) ||
            navigationScoring.ValueKind != JsonValueKind.Object)
        {
            return defaults;
        }

        var enabled = defaults.Enabled;
        if (navigationScoring.TryGetProperty("enabled", out var enabledProp) &&
            enabledProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            enabled = enabledProp.GetBoolean();
        }

        var weightPercent = defaults.WeightPercent;
        if (navigationScoring.TryGetProperty("weight", out var weightProp) &&
            weightProp.ValueKind == JsonValueKind.Number)
        {
            try
            {
                weightPercent = Math.Clamp(weightProp.GetDouble(), 0, 100);
            }
            catch
            {
                weightPercent = defaults.WeightPercent;
            }
        }

        return defaults with
        {
            Enabled = enabled,
            WeightPercent = weightPercent,
        };
    }

    public GovernanceRules ExtractGovernanceRules(JsonElement? config)
    {
        var rules = new GovernanceRules(MaxVisualsPerPage: 10, AllowPieCharts: false, RequirePageTitle: true);

        if (!config.HasValue || !config.Value.TryGetProperty("governance", out var governanceArray) ||
            governanceArray.ValueKind != JsonValueKind.Array)
        {
            return rules;
        }

        foreach (var rule in governanceArray.EnumerateArray())
        {
            if (rule.ValueKind != JsonValueKind.Object || !rule.TryGetProperty("id", out var idProp))
            {
                continue;
            }

            var id = idProp.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(id) || !rule.TryGetProperty("value", out var valueProp))
            {
                continue;
            }

            switch (id)
            {
                case "maxVisuals":
                case "maxVisualsPerPage":
                    if (valueProp.ValueKind == JsonValueKind.Number && valueProp.TryGetInt32(out var maxVisuals))
                    {
                        rules = rules with { MaxVisualsPerPage = Math.Max(1, maxVisuals) };
                    }
                    break;
                case "allowPie":
                case "allowPieCharts":
                    if (valueProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        rules = rules with { AllowPieCharts = valueProp.GetBoolean() };
                    }
                    break;
                case "requireTitle":
                case "requirePageTitle":
                    if (valueProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        rules = rules with { RequirePageTitle = valueProp.GetBoolean() };
                    }
                    break;
            }
        }

        return rules;
    }

    internal static string NormalizeFrameworkId(string id)
    {
        return id.ToLowerInvariant() switch
        {
            "gestalt" => "gestalt",
            "cognitive" or "cognitivelload" => "cognitiveLoad",
            "dataink" or "data-ink" => "dataInk",
            "graphical" or "graphicalperception" => "graphicalPerception",
            "accessibility" or "wcag" => "accessibility",
            "visual" or "visualbestpractices" => "visualBestPractices",
            "governance" or "enterprisegovernance" => "governance",
            "stephen" or "stephenfew" => "stephenFew",
            "tufte" or "tufeminimalism" => "tufte",
            "density" or "dashboarddensity" => "density",
            "narrative" or "narrativedesign" => "narrative",
            _ => id
        };
    }

    private static Dictionary<string, double> GetDefaultFrameworkWeights() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["gestalt"] = 30,
        ["cognitiveLoad"] = 20,
        ["dataInk"] = 15,
        ["graphicalPerception"] = 0,
        ["accessibility"] = 15,
        ["visualBestPractices"] = 20,
        ["governance"] = 0,
        ["stephenFew"] = 0,
        ["tufte"] = 0,
        ["density"] = 0,
        ["narrative"] = 0,
    };
}

internal readonly record struct GovernanceRules(int MaxVisualsPerPage, bool AllowPieCharts, bool RequirePageTitle);

internal readonly record struct NavigationScoringSettings(
    bool Enabled,
    double WeightPercent,
    int WarningNavigationCount,
    int WarningHiddenVisualCount)
{
    public double WeightMultiplier => WeightPercent / 100.0;
}
