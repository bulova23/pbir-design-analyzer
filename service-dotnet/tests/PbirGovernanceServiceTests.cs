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

    // ── Dynamic governance rule tests (REC-02) ───────────────────────────────

    [Fact]
    public void Evaluate_MaxVisualsPerPage_BlocksWhenPageExceedsLimit()
    {
        var service = BuildService();
        var policy = BuildEnabledPolicyWithRule(
            "maxVisualsPerPage", new GovernanceRule { Name = "Max Visuals Per Page", Value = 3 });
        var score = CreateScoreResultWithPages(
            ("Overview", BuildPageMetadata(visualCount: 5)));

        var result = service.Evaluate(policy, score, themeId: null);

        Assert.True(result.Blocked);
        Assert.Contains(result.Reasons, r => r.Contains("maxVisualsPerPage", StringComparison.Ordinal)
            && r.Contains("'Overview'", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_MaxVisualsPerPage_PassesWhenAllPagesUnderLimit()
    {
        var service = BuildService();
        var policy = BuildEnabledPolicyWithRule(
            "maxVisualsPerPage", new GovernanceRule { Name = "Max Visuals Per Page", Value = 10 });
        var score = CreateScoreResultWithPages(
            ("Overview", BuildPageMetadata(visualCount: 4)),
            ("Detail", BuildPageMetadata(visualCount: 7)));

        var result = service.Evaluate(policy, score, themeId: null);

        Assert.DoesNotContain(result.Reasons, r => r.Contains("maxVisualsPerPage", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_MaxHiddenVisuals_BlocksWhenAggregateExceeds()
    {
        var service = BuildService();
        var policy = BuildEnabledPolicyWithRule(
            "maxHiddenVisuals", new GovernanceRule { Name = "Max Hidden Visuals", Value = 5 });
        var score = CreateScoreResultWithPages(("Overview", BuildPageMetadata(visualCount: 0)));
        score.HiddenVisualCount = 11;

        var result = service.Evaluate(policy, score, themeId: null);

        Assert.True(result.Blocked);
        Assert.Contains(result.Reasons, r => r.Contains("maxHiddenVisuals", StringComparison.Ordinal)
            && r.Contains("11", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_AllowPieCharts_FalseBlocksWhenPieDetected()
    {
        var service = BuildService();
        var policy = BuildEnabledPolicyWithRule(
            "allowPieCharts", new GovernanceRule { Name = "Allow Pie Charts", Value = false });
        var score = CreateScoreResultWithPages(
            ("Overview", BuildPageMetadata(visuals: new[]
            {
                ("v1", "barChart", false),
                ("v2", "pieChart", false),
            })));

        var result = service.Evaluate(policy, score, themeId: null);

        Assert.True(result.Blocked);
        Assert.Contains(result.Reasons, r => r.Contains("allowPieCharts", StringComparison.Ordinal)
            && r.Contains("'Overview'", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_AllowPieCharts_TrueDoesNotBlock()
    {
        var service = BuildService();
        var policy = BuildEnabledPolicyWithRule(
            "allowPieCharts", new GovernanceRule { Name = "Allow Pie Charts", Value = true });
        var score = CreateScoreResultWithPages(
            ("Overview", BuildPageMetadata(visuals: new[] { ("v1", "donutChart", false) })));

        var result = service.Evaluate(policy, score, themeId: null);

        Assert.DoesNotContain(result.Reasons, r => r.Contains("allowPieCharts", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_AllowCustomVisuals_FalseBlocksWhenUnknownTypePresent()
    {
        var service = BuildService();
        var policy = BuildEnabledPolicyWithRule(
            "allowCustomVisuals", new GovernanceRule { Name = "Allow Custom Visuals", Value = false });
        var score = CreateScoreResultWithPages(
            ("Overview", BuildPageMetadata(visuals: new[]
            {
                ("v1", "barChart", false),
                ("v2", "acmeCustomVisual", false),
            })));

        var result = service.Evaluate(policy, score, themeId: null);

        Assert.True(result.Blocked);
        Assert.Contains(result.Reasons, r => r.Contains("allowCustomVisuals", StringComparison.Ordinal)
            && r.Contains("acmeCustomVisual", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_RequirePageTitle_BlocksWhenStrictTitleMissing()
    {
        var service = BuildService();
        var policy = BuildEnabledPolicyWithRule(
            "requirePageTitle", new GovernanceRule { Name = "Require Page Title", Value = true });
        var score = CreateScoreResultWithPages(
            ("Overview", BuildPageMetadata(visualCount: 1, strictVisibleTitle: null)),
            ("Detail", BuildPageMetadata(visualCount: 1, strictVisibleTitle: "Detail Drill-Down")));

        var result = service.Evaluate(policy, score, themeId: null);

        Assert.True(result.Blocked);
        Assert.Contains(result.Reasons, r => r.Contains("requirePageTitle", StringComparison.Ordinal)
            && r.Contains("'Overview'", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_RequireFilterPanel_BlocksWhenPageHasNoSlicer()
    {
        var service = BuildService();
        var policy = BuildEnabledPolicyWithRule(
            "requireFilterPanel", new GovernanceRule { Name = "Require Filter Panel", Value = true });
        var score = CreateScoreResultWithPages(
            ("Overview", BuildPageMetadata(visualCount: 3, slicerCount: 0)),
            ("Detail", BuildPageMetadata(visualCount: 3, slicerCount: 2)));

        var result = service.Evaluate(policy, score, themeId: null);

        Assert.True(result.Blocked);
        Assert.Contains(result.Reasons, r => r.Contains("requireFilterPanel", StringComparison.Ordinal)
            && r.Contains("'Overview'", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Reasons, r => r.Contains("'Detail'", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_MinWhiteSpaceRatio_BlocksWhenPageIsCrowded()
    {
        var service = BuildService();
        var policy = BuildEnabledPolicyWithRule(
            "minWhiteSpaceRatio", new GovernanceRule { Name = "Min White Space Ratio", Value = 0.25 });
        var crowded = new PageVisualMetadataSummary
        {
            PageName = "Crowded",
            CanvasWidth = 1000,
            CanvasHeight = 1000,
            Visuals =
            [
                new VisualMetadataItem { VisualId = "v1", VisualType = "barChart", X = 0, Y = 0, Width = 900, Height = 900 },
            ],
        };
        var score = CreateScoreResultWithPages(("Crowded", crowded));

        var result = service.Evaluate(policy, score, themeId: null);

        Assert.True(result.Blocked);
        Assert.Contains(result.Reasons, r => r.Contains("minWhiteSpaceRatio", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_ThemeStandard_BlocksWhenThemeDiffers()
    {
        var service = BuildService();
        var policy = BuildEnabledPolicyWithRule(
            "themeStandard", new GovernanceRule { Name = "Standard Theme", Value = "Executive" });
        var score = CreateScoreResultWithPages(("Overview", BuildPageMetadata(visualCount: 1)));

        var result = service.Evaluate(policy, score, themeId: "Marketing");

        Assert.True(result.Blocked);
        Assert.Contains(result.Reasons, r => r.Contains("themeStandard", StringComparison.Ordinal)
            && r.Contains("Marketing", StringComparison.Ordinal)
            && r.Contains("Executive", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_UnknownRule_IsIgnoredWithoutThrowing()
    {
        var service = BuildService();
        var policy = BuildEnabledPolicyWithRule(
            "completelyMadeUpRule", new GovernanceRule { Name = "Made Up", Value = true });
        var score = CreateScoreResultWithPages(("Overview", BuildPageMetadata(visualCount: 1)));

        var result = service.Evaluate(policy, score, themeId: null);

        Assert.DoesNotContain(result.Reasons, r => r.Contains("completelyMadeUpRule", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_DeferredBookmarkRules_AreRecognizedButDoNotBlock()
    {
        var service = BuildService();
        var policy = BuildEnabledPolicyWithRule(
            "maxBookmarksPerPage", new GovernanceRule { Name = "Max Bookmarks Per Page", Value = 5 });
        var score = CreateScoreResultWithPages(("Overview", BuildPageMetadata(visualCount: 1)));

        var result = service.Evaluate(policy, score, themeId: null);

        Assert.DoesNotContain(result.Reasons, r => r.Contains("maxBookmarksPerPage", StringComparison.Ordinal));
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

    private static GovernancePolicy BuildEnabledPolicyWithRule(string ruleId, GovernanceRule rule) => new()
    {
        Enabled = true,
        IsConfigured = true,
        MinScoreThreshold = 0, // not the rule under test
        DynamicRules = new Dictionary<string, GovernanceRule> { [ruleId] = rule },
    };

    private static ScoreResult CreateScoreResultWithPages(params (string PageName, PageVisualMetadataSummary Metadata)[] pages)
    {
        var result = CreateScoreResult(100);
        result.PageScores = pages.Select(p => new PageScore
        {
            PageName = p.PageName,
            VisualMetadata = p.Metadata,
            FrameworkWeights = result.FrameworkWeights,
        }).ToList();
        result.PageCount = pages.Length;
        return result;
    }

    private static PageVisualMetadataSummary BuildPageMetadata(
        int visualCount = 0,
        string? strictVisibleTitle = "Title",
        int slicerCount = 0,
        (string Id, string Type, bool Hidden)[]? visuals = null)
    {
        var visualItems = visuals is null
            ? Enumerable.Range(0, visualCount)
                .Select(i => new VisualMetadataItem
                {
                    VisualId = $"v{i + 1}",
                    VisualType = "barChart",
                    X = i * 100, Y = 0, Width = 100, Height = 100,
                })
                .ToList()
            : visuals.Select(v => new VisualMetadataItem
            {
                VisualId = v.Id,
                VisualType = v.Type,
                IsHidden = v.Hidden,
                X = 0, Y = 0, Width = 100, Height = 100,
            }).ToList();

        return new PageVisualMetadataSummary
        {
            PageName = "Test",
            StrictVisiblePageTitle = strictVisibleTitle,
            VisiblePageTitle = strictVisibleTitle,
            CanvasWidth = 1280,
            CanvasHeight = 720,
            VisualCount = visualItems.Count,
            SlicerCount = slicerCount,
            Visuals = visualItems,
        };
    }
}
