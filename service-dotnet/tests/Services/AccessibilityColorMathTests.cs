using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace ServiceDotnet.Tests.Services;

public sealed class AccessibilityColorMathTests
{
    [Theory]
    [InlineData("#abc", "#AABBCC")]
    [InlineData(" abc123 ", "#ABC123")]
    [InlineData("#AbCdEf", "#ABCDEF")]
    [InlineData("000000", "#000000")]
    public void TryNormalizeHex_ReturnsCanonicalSixDigitHex(string input, string expected)
    {
        Assert.Equal(expected, AccessibilityColorMath.TryNormalizeHex(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#12")]
    [InlineData("#1234567")]
    [InlineData("#12GG56")]
    public void TryNormalizeHex_ReturnsNullForMalformedInput(string? input)
    {
        Assert.Null(AccessibilityColorMath.TryNormalizeHex(input));
    }

    [Theory]
    [InlineData("#FF0000", "#00FF00", true)]
    [InlineData("#00FF00", "#FF0000", true)]
    [InlineData("#0000FF", "#FFFF00", false)]
    [InlineData("#FF0000", "#F00010", false)]
    public void LooksLikeRedGreenPair_UsesExistingDominanceRule(string first, string second, bool expected)
    {
        Assert.Equal(expected, AccessibilityColorMath.LooksLikeRedGreenPair(first, second));
    }

    [Theory]
    [InlineData("#FF0000", true, false)]
    [InlineData("#00FF00", false, true)]
    [InlineData("#282828", false, false)]
    [InlineData("#280000", false, false)]
    public void DominanceChecksPreserveChannelBoundaryBehavior(string hex, bool expectedRed, bool expectedGreen)
    {
        Assert.Equal(expectedRed, AccessibilityColorMath.IsRedDominant(hex));
        Assert.Equal(expectedGreen, AccessibilityColorMath.IsGreenDominant(hex));
    }

    [Fact]
    public void DeuteranopiaSimulationPreservesSimilarityThreshold()
    {
        Assert.True(AccessibilityColorMath.SimulatesToSimilarUnderDeuteranopia("#FF0000", "#F00010"));
        Assert.False(AccessibilityColorMath.SimulatesToSimilarUnderDeuteranopia("#000000", "#FFFFFF"));
    }

    [Theory]
    [InlineData("#FF0000", 0.625, 0.7, 0)]
    [InlineData("#00FF00", 0.375, 0.3, 0.3)]
    [InlineData("#0000FF", 0, 0, 0.7)]
    public void DeuteranopiaSimulationPreservesProjectionChannels(string hex, double expectedR, double expectedG, double expectedB)
    {
        var actual = AccessibilityColorMath.SimulateDeuteranopia(hex);

        Assert.Equal(expectedR, actual.R, 10);
        Assert.Equal(expectedG, actual.G, 10);
        Assert.Equal(expectedB, actual.B, 10);
    }

    [Fact]
    public void DeuteranopiaSimulationClampsProjectedChannels()
    {
        var black = AccessibilityColorMath.SimulateDeuteranopia("#000000");
        var white = AccessibilityColorMath.SimulateDeuteranopia("#FFFFFF");

        Assert.Equal((0d, 0d, 0d), black);
        Assert.Equal((1d, 1d, 1d), white);
    }

    [Fact]
    public void HexToRgbPreservesChannels()
    {
        Assert.Equal((255, 128, 0), AccessibilityColorMath.HexToRgb("#FF8000"));
    }
}
