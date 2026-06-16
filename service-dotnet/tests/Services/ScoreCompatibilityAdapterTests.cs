using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

public sealed class ScoreCompatibilityAdapterTests
{
    [Fact]
    public void PopulateLegacyScores_MapsCurrentFrameworkScoresToDeprecatedProperties()
    {
        var adapter = new ScoreCompatibilityAdapter();
        var result = new ScoreResult
        {
            GestaltScore = 61.5,
            VisualBestPracticesScore = 72.25,
            EnterpriseGovernanceScore = 88.75,
        };

        adapter.PopulateLegacyScores(result);

#pragma warning disable CS0618
        Assert.Equal(61.5, result.LayoutScore);
        Assert.Equal(72.25, result.ThemeScore);
        Assert.Equal(88.75, result.GovernanceScore);
#pragma warning restore CS0618
    }
}
