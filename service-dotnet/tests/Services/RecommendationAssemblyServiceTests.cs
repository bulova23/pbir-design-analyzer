using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

public sealed class RecommendationAssemblyServiceTests
{
    [Fact]
    public void AddBookmarkAwareScoringRecommendation_AppendsExpectedPluralizedMessage()
    {
        var service = new RecommendationAssemblyService();
        var recommendations = new List<string>
        {
            "[Medium] Density: simplify the top band."
        };

        service.AddBookmarkAwareScoringRecommendation(recommendations, 3);

        Assert.Equal(2, recommendations.Count);
        Assert.Equal(
            "[Info] Bookmark-aware scoring active: page scored across 3 layout states (Default + 2 bookmark states).",
            recommendations[1]);
    }

    [Fact]
    public void AddBookmarkAwareScoringRecommendation_UsesSingularBookmarkStateLabel()
    {
        var service = new RecommendationAssemblyService();
        var recommendations = new List<string>();

        service.AddBookmarkAwareScoringRecommendation(recommendations, 2);

        Assert.Equal(
            "[Info] Bookmark-aware scoring active: page scored across 2 layout states (Default + 1 bookmark state).",
            Assert.Single(recommendations));
    }
}
