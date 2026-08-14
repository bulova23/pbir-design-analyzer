using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirRoundTripFidelityServiceTests
{
    [Fact]
    public void Compare_ClassifiesByteSemanticExpectedAndUnexpectedDifferences()
    {
        var source = new Dictionary<string, string>
        {
            ["same.json"] = "{\"b\":2,\"a\":1}",
            ["semantic.json"] = "{\"a\":1,\"b\":2}",
            ["changed.json"] = "{\"value\":1}",
            ["unexpected.json"] = "{\"value\":1}"
        };
        var output = new Dictionary<string, string>
        {
            ["same.json"] = source["same.json"],
            ["semantic.json"] = "{\"b\":2,\"a\":1}",
            ["changed.json"] = "{\"value\":2}",
            ["unexpected.json"] = "{\"value\":2}"
        };

        var result = new PbirRoundTripFidelityService().Compare(source, output, new HashSet<string> { "changed.json" });

        Assert.Equal(PbirFidelityClassification.ByteIdentical, result.Files.Single(file => file.RelativePath == "same.json").Classification);
        Assert.Equal(PbirFidelityClassification.SemanticallyIdentical, result.Files.Single(file => file.RelativePath == "semantic.json").Classification);
        Assert.Equal(PbirFidelityClassification.ExpectedNormalizedDifference, result.Files.Single(file => file.RelativePath == "changed.json").Classification);
        Assert.Equal(PbirFidelityClassification.UnexpectedDifference, result.Files.Single(file => file.RelativePath == "unexpected.json").Classification);
        Assert.Contains("same.json", result.AuthoringIdentical);
        Assert.Contains("semantic.json", result.SemanticEquivalent);
        Assert.Contains("changed.json", result.IntentionallyChanged);
        Assert.Contains("unexpected.json", result.UnexpectedPaths);
        Assert.False(result.IsFidelityReady);
    }
}
