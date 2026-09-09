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

    [Fact]
    public void ScoringSeamDoesNotReferenceTransportProviderOrMutationInfrastructure()
    {
        var sourceRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "Services", "Pbir", "Scoring"));
        var source = string.Join("\n", Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText));

        foreach (var forbiddenDependency in new[]
        {
            "Microsoft.VisualStudio", "VSCode", "RpcHost", "Transport", "Provider", "Authoring", "Mutation",
            "PbirScorePanel", "ILogger", "Reflection"
        })
        {
            Assert.DoesNotContain(forbiddenDependency, source, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class TestStage(ScoringStageId id) : IScoringStage
    {
        public ScoringStageId Id { get; } = id;

        public ScoringStageResult Analyze(ReportAnalysisContext context, ScoringStageInput input) =>
            ScoringStageResult.Empty;
    }
}
