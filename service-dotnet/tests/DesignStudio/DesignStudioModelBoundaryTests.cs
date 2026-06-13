using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.DesignStudio;

public sealed class DesignStudioModelBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ModelsNamespace = "PowerBIModelingService.Services.DesignStudio.Models";

    [Fact(DisplayName = "Design Studio substrate types exist as backend-internal models")]
    public void DesignStudio_InternalTypesExist()
    {
        string[] expectedTypeNames =
        [
            "DesignArtifactLifecycleState",
            "DesignArtifactApprovalState",
            "DesignArtifactApprovalKind",
            "DesignArtifactAuthorSource",
            "DesignArtifactProvenance",
            "DesignArtifactValidationLink",
            "DesignArtifactMetadata",
            "DesignBrief",
            "ReportConcept",
            "PageConcept",
            "NavigationConcept",
            "KpiHierarchyConcept",
            "DraftReportArtifact",
            "DraftPageArtifact",
            "DraftLayoutArtifact",
            "DraftNavigationArtifact",
            "DraftNavigationSectionArtifact",
            "DraftArtifactStatus",
            "CrossPageNarrativeGapSummary",
            "CrossPageNarrativeAnalyzerOutput",
            "SourceArtifactLineageEntry",
            "RefinementSourceAnalyzerOutput",
            "RefinementNoMutationGuarantee",
            "DesignArtifactBacklinkRecord",
            "RefinementProposal",
            "MaterializationRequest",
            "MaterializedSurfaceCandidate",
            "DesignIterationRecord",
        ];

        foreach (var typeName in expectedTypeNames)
        {
            var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
            Assert.NotNull(type);
            Assert.True(type!.IsNotPublic, $"{typeName} should remain backend-internal.");
        }
    }

    [Fact(DisplayName = "Design Studio lifecycle vocabulary stays separate from Story Assessment promotion state")]
    public void DesignStudio_LifecycleVocabulary_DoesNotReusePromotionState()
    {
        var metadataType = RequireType("DesignArtifactMetadata");
        var propertyTypes = metadataType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("DesignArtifactLifecycleState", propertyTypes["LifecycleState"]);
        Assert.Equal("DesignArtifactApprovalState", propertyTypes["ApprovalState"]);
        Assert.Equal("DesignArtifactApprovalKind", propertyTypes["ApprovalKind"]);
        Assert.DoesNotContain("PromotionState", propertyTypes.Keys);
        Assert.DoesNotContain("StoryAssessmentPromotionState", propertyTypes.Values);
    }

    [Fact(DisplayName = "Design Studio models do not widen ScoreResult, PageScore, or Story Assessment public contracts")]
    public void DesignStudio_PublicContracts_DoNotExposeStudioModels()
    {
        var publicResultPropertyNames = typeof(ScoreResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var publicPagePropertyNames = typeof(PageScore)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("DesignStudio", publicResultPropertyNames);
        Assert.DoesNotContain("DesignBrief", publicResultPropertyNames);
        Assert.DoesNotContain("ReportConcept", publicResultPropertyNames);
        Assert.DoesNotContain("MaterializationRequest", publicResultPropertyNames);
        Assert.DoesNotContain("MaterializedSurfaceCandidate", publicResultPropertyNames);
        Assert.DoesNotContain("RefinementProposal", publicResultPropertyNames);

        Assert.DoesNotContain("DesignStudio", publicPagePropertyNames);
        Assert.DoesNotContain("DesignBrief", publicPagePropertyNames);
        Assert.DoesNotContain("ReportConcept", publicPagePropertyNames);
    }

    [Fact(DisplayName = "Design Studio models do not introduce direct mutation or deployment methods")]
    public void DesignStudio_InternalModels_DoNotExposeMutationOrDeploymentAuthority()
    {
        var methods = RequireType("MaterializationRequest")
            .Assembly
            .GetTypes()
            .Where(type => type.Namespace == ModelsNamespace)
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Select(method => method.Name)
            .ToArray();

        Assert.DoesNotContain(methods, method => method.Contains("Apply", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("Deploy", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("Publish", StringComparison.Ordinal));
        Assert.Contains("MaterializedSurfaceCandidate", RequireType("MaterializedSurfaceCandidate").Name);
    }

    [Fact(DisplayName = "Materialization lineage models preserve exact source version and approval metadata")]
    public void DesignStudio_MaterializationLineage_UsesExactSourceMetadata()
    {
        var lineageType = RequireType("SourceArtifactLineageEntry");
        var requestType = RequireType("MaterializationRequest");
        var candidateType = RequireType("MaterializedSurfaceCandidate");

        var lineageProperties = lineageType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ArtifactId", lineageProperties);
        Assert.Contains("ArtifactKind", lineageProperties);
        Assert.Contains("ArtifactVersionId", lineageProperties);
        Assert.Contains("SourceRole", lineageProperties);
        Assert.Contains("ApprovalState", lineageProperties);
        Assert.Contains("ApprovalTimestamp", lineageProperties);

        Assert.Contains(requestType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), property => property.Name == "SourceLineage");
        Assert.Contains(candidateType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), property => property.Name == "SourceLineage");
    }

    private static Type RequireType(string typeName)
    {
        var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }
}
