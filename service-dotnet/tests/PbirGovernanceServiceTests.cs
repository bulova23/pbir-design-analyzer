using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

public sealed class PbirGovernanceServiceTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    [Fact]
    public void ReadPolicy_WithoutSettingsFile_ReturnsNotConfiguredDisabledPolicy()
    {
        var workspaceRoot = CreateWorkspace();
        var service = BuildService();

        var policy = service.ReadPolicy(workspaceRoot);

        Assert.False(policy.Enabled);
        Assert.False(policy.IsConfigured);
        Assert.Empty(policy.ApprovedThemes);
        Assert.Empty(policy.DynamicRules);
    }

    [Fact]
    public void ReadPolicy_FlatSettingsEnabledFalse_ReturnsConfiguredDisabledPolicy()
    {
        var workspaceRoot = CreateWorkspace("""
        {
          "powerbi-modeling.governance.enabled": false,
          "powerbi-modeling.governance.minimumCompositeScore": 82,
          "powerbi-modeling.governance.approvedThemeIds": ["CorporateBlue"]
        }
        """);
        var service = BuildService();

        var policy = service.ReadPolicy(workspaceRoot);

        Assert.False(policy.Enabled);
        Assert.True(policy.IsConfigured);
        Assert.Equal(82, policy.MinScoreThreshold);
        Assert.Equal(["CorporateBlue"], policy.ApprovedThemes);
    }

    [Fact]
    public void ReadPolicy_FlatSettingsEnabledTrue_UsesThresholdThemesAndRules()
    {
        var workspaceRoot = CreateWorkspace("""
        {
          "powerbi-modeling.governance.enabled": true,
          "powerbi-modeling.governance.minimumCompositeScore": 85,
          "powerbi-modeling.governance.approvedThemeIds": ["CorporateBlue", "Executive"],
          "powerbi-modeling.governance.notes": "Policy notes",
          "powerbi-modeling.governance.rules": {
            "requirePageTitle": {
              "name": "Require Page Title",
              "value": true,
              "severity": "error",
              "adminOnly": true
            }
          }
        }
        """);
        var service = BuildService();

        var policy = service.ReadPolicy(workspaceRoot);

        Assert.True(policy.Enabled);
        Assert.True(policy.IsConfigured);
        Assert.Equal(85, policy.MinScoreThreshold);
        Assert.Equal(["CorporateBlue", "Executive"], policy.ApprovedThemes);
        Assert.Equal("Policy notes", policy.Notes);
        Assert.Contains("requirePageTitle", policy.DynamicRules.Keys);
    }

    [Fact]
    public void ReadPolicy_LegacyNestedSettingsStillSupported()
    {
        var workspaceRoot = CreateWorkspace("""
        {
          "powerbi-modeling.governance": {
            "enabled": true,
            "minimumCompositeScore": 78,
            "approvedThemeIds": ["LegacyTheme"]
          }
        }
        """);
        var service = BuildService();

        var policy = service.ReadPolicy(workspaceRoot);

        Assert.True(policy.Enabled);
        Assert.True(policy.IsConfigured);
        Assert.Equal(78, policy.MinScoreThreshold);
        Assert.Equal(["LegacyTheme"], policy.ApprovedThemes);
    }

    [Fact]
    public void Evaluate_DisabledPolicy_ReturnsInformationalState()
    {
        var service = BuildService();
        var result = service.Evaluate(
            new GovernancePolicy { Enabled = false, IsConfigured = false },
            CreateScoreResult(74),
            themeId: null);

        Assert.Equal("notConfigured", result.PolicyState);
        Assert.False(result.PolicyConfigured);
        Assert.False(result.PolicyEnabled);
        Assert.False(result.Blocked);
        Assert.Equal(0, result.RequiredThreshold);
        Assert.Contains("No workspace governance policy is enabled", result.StatusMessage);
    }

    [Fact]
    public void Evaluate_EnabledPolicy_WithMissingTheme_BlocksWithThemeReason()
    {
        var service = BuildService();
        var result = service.Evaluate(
            new GovernancePolicy
            {
                Enabled = true,
                IsConfigured = true,
                MinScoreThreshold = 70,
                ApprovedThemes = ["CorporateBlue"],
            },
            CreateScoreResult(88),
            themeId: "");

        Assert.Equal("enabled", result.PolicyState);
        Assert.True(result.PolicyEnabled);
        Assert.True(result.Blocked);
        Assert.Contains(result.Reasons, reason => reason.Contains("no theme name was supplied", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    private static PbirGovernanceService BuildService() =>
        new(NullLogger<PbirGovernanceService>.Instance);

    private string CreateWorkspace(string? settingsJson = null)
    {
        var tempDir = Directory.CreateTempSubdirectory("pbir-governance-test-").FullName;
        _tempDirs.Add(tempDir);

        if (settingsJson is null)
            return tempDir;

        var vscodeDir = Path.Combine(tempDir, ".vscode");
        Directory.CreateDirectory(vscodeDir);
        File.WriteAllText(Path.Combine(vscodeDir, "settings.json"), settingsJson);
        return tempDir;
    }

    private static ScoreResult CreateScoreResult(double compositeScore)
    {
        const double gestaltWeight = 100;
        return new ScoreResult
        {
            GestaltScore = compositeScore,
            FrameworkWeights = new Dictionary<string, double>
            {
                ["gestalt"] = gestaltWeight,
                ["cognitiveLoad"] = 0,
                ["dataInk"] = 0,
                ["accessibility"] = 0,
                ["visualBestPractices"] = 0,
                ["governance"] = 0,
                ["stephenFew"] = 0,
                ["tufte"] = 0,
                ["graphicalPerception"] = 0,
                ["density"] = 0,
                ["narrative"] = 0,
            },
        };
    }
}
