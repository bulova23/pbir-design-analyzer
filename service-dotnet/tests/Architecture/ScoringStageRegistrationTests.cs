using PowerBIModelingService.Services.Pbir.Scoring;
using Xunit;

namespace ServiceDotnet.Tests.Architecture;

public sealed class ScoringStageRegistrationTests
{
    [Fact]
    public void RegistryPreservesExplicitRegistrationOrder()
    {
        var first = new TestStage(ScoringStageId.Accessibility);
        var second = new TestStage(ScoringStageId.VisualBestPractices);

        var registry = new ScoringStageRegistry([first, second]);

        Assert.Equal([first, second], registry.Stages);
    }

    [Fact]
    public void RegistryRejectsDuplicateIds()
    {
        var first = new TestStage(ScoringStageId.Accessibility);
        var second = new TestStage(ScoringStageId.Accessibility);

        var error = Assert.Throws<ArgumentException>(() => new ScoringStageRegistry([first, second]));

        Assert.Contains("duplicate", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegistryDoesNotExposeMutableRegistrationList()
    {
        var stage = new TestStage(ScoringStageId.Accessibility);
        var registry = new ScoringStageRegistry([stage]);

        Assert.IsAssignableFrom<IReadOnlyList<IScoringStage>>(registry.Stages);
        Assert.Single(registry.Stages);
    }

    private sealed class TestStage(ScoringStageId id) : IScoringStage
    {
        public ScoringStageId Id { get; } = id;

        public ScoringStageResult Analyze(ReportAnalysisContext context, ScoringStageInput input) =>
            ScoringStageResult.Empty;
    }
}
