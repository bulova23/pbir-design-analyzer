using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

public sealed class ScoringConfigurationServiceTests
{
    private readonly ScoringConfigurationService _service = new(NullLogger.Instance);

    [Fact]
    public void ExtractFrameworkWeights_UsesDefaults_WhenConfigMissing()
    {
        var weights = _service.ExtractFrameworkWeights(config: null);

        Assert.Equal(30, weights["gestalt"]);
        Assert.Equal(20, weights["cognitiveLoad"]);
        Assert.Equal(15, weights["dataInk"]);
        Assert.Equal(20, weights["visualBestPractices"]);
        Assert.Equal(0, weights["narrative"]);
    }

    [Fact]
    public void ExtractFrameworkWeights_NormalizesFrameworkIds_FromFrameworkArray()
    {
        using var doc = JsonDocument.Parse(
            """
            {
              "frameworks": [
                { "id": "cognitive", "enabled": true, "weight": 40 },
                { "id": "data-ink", "enabled": true, "weight": 15 },
                { "id": "stephen", "enabled": false, "weight": 50 }
              ]
            }
            """);

        var weights = _service.ExtractFrameworkWeights(doc.RootElement);

        Assert.Equal(40, weights["cognitiveLoad"]);
        Assert.Equal(15, weights["dataInk"]);
        Assert.Equal(0, weights["stephenFew"]);
    }

    [Fact]
    public void ExtractNavigationScoringSettings_ClampsWeightAndReadsFlag()
    {
        using var doc = JsonDocument.Parse(
            """
            {
              "navigationScoring": {
                "enabled": false,
                "weight": 125
              }
            }
            """);

        var settings = _service.ExtractNavigationScoringSettings(doc.RootElement);

        Assert.False(settings.Enabled);
        Assert.Equal(100, settings.WeightPercent);
    }

    [Fact]
    public void ExtractGovernanceRules_ReadsLegacyAliases()
    {
        using var doc = JsonDocument.Parse(
            """
            {
              "governance": [
                { "id": "maxVisuals", "value": 12 },
                { "id": "allowPie", "value": true },
                { "id": "requireTitle", "value": false }
              ]
            }
            """);

        var rules = _service.ExtractGovernanceRules(doc.RootElement);

        Assert.Equal(12, rules.MaxVisualsPerPage);
        Assert.True(rules.AllowPieCharts);
        Assert.False(rules.RequirePageTitle);
    }
}
