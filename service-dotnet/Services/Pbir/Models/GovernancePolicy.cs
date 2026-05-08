using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Pbir.Models;

/// <summary>
/// Represents a single governance rule with metadata.
/// </summary>
public sealed class GovernanceRule
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    [JsonConverter(typeof(JsonElementConverter))]
    public object Value { get; set; } = new object();

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "warning";

    [JsonPropertyName("adminOnly")]
    public bool AdminOnly { get; set; } = true;
}

/// <summary>
/// Custom JSON converter to handle rule values that can be string, number, boolean, or other types.
/// </summary>
public class JsonElementConverter : System.Text.Json.Serialization.JsonConverter<object>
{
    public override object Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            System.Text.Json.JsonTokenType.String => reader.GetString() ?? string.Empty,
            System.Text.Json.JsonTokenType.Number => reader.TryGetInt32(out var intVal) ? intVal : reader.GetDouble(),
            System.Text.Json.JsonTokenType.True => true,
            System.Text.Json.JsonTokenType.False => false,
            _ => reader.GetString() ?? string.Empty,
        };
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, object value, System.Text.Json.JsonSerializerOptions options)
    {
        if (value is string str)
            writer.WriteStringValue(str);
        else if (value is int i)
            writer.WriteNumberValue(i);
        else if (value is double d)
            writer.WriteNumberValue(d);
        else if (value is bool b)
            writer.WriteBooleanValue(b);
        else
            writer.WriteStringValue(value?.ToString() ?? string.Empty);
    }
}

/// <summary>
/// Represents the enterprise governance policy loaded from VS Code workspace settings.
/// Corporate governance is opt-in and remains disabled until a workspace policy is
/// explicitly configured and enabled by a Power BI champion or admin.
/// </summary>
public sealed class GovernancePolicy
{
    /// <summary>The legacy nested VS Code settings key used to read this policy.</summary>
    public const string SettingsKey = "powerbi-modeling.governance";

    /// <summary>The flat VS Code settings key that enables governance enforcement.</summary>
    public const string EnabledSettingsKey = "powerbi-modeling.governance.enabled";

    /// <summary>The flat VS Code settings key for the minimum composite score threshold.</summary>
    public const string MinimumCompositeScoreSettingsKey = "powerbi-modeling.governance.minimumCompositeScore";

    /// <summary>The flat VS Code settings key for approved theme identifiers.</summary>
    public const string ApprovedThemeIdsSettingsKey = "powerbi-modeling.governance.approvedThemeIds";

    /// <summary>The flat VS Code settings key for optional governance notes.</summary>
    public const string NotesSettingsKey = "powerbi-modeling.governance.notes";

    /// <summary>The flat VS Code settings key for advanced governance rules.</summary>
    public const string RulesSettingsKey = "powerbi-modeling.governance.rules";

    /// <summary>
    /// Gets or sets whether governance enforcement is active.
    /// When <c>false</c>, all checks pass automatically. Default: <c>false</c>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether any workspace governance policy
    /// was explicitly configured. This distinguishes "not configured" from
    /// "configured but disabled".
    /// </summary>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// Gets or sets the minimum composite score required for a report to pass governance
    /// and be eligible for publish. Default: 70.
    /// </summary>
    public double MinScoreThreshold { get; set; } = 70;

    /// <summary>
    /// Gets or sets the list of approved theme names. An empty list means any theme is allowed.
    /// </summary>
    public List<string> ApprovedThemes { get; set; } = [];

    /// <summary>
    /// Gets or sets optional free-text governance notes surfaced to the report author
    /// when a governance check fails.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets dynamic governance rules loaded from configuration.
    /// Format: { "ruleId": GovernanceRule, ... }
    /// Extensible to support custom rules beyond the hardcoded few.
    /// </summary>
    [JsonPropertyName("rules")]
    public Dictionary<string, GovernanceRule> DynamicRules { get; set; } = new();
}
