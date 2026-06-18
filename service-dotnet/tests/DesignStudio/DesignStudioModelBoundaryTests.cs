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
            "DesignProviderCapabilityKind",
            "DesignArtifactLifecycleState",
            "DesignArtifactApprovalState",
            "DesignArtifactApprovalKind",
            "DesignStudioWorkflowCompletionState",
            "DesignArtifactAuthorSource",
            "DesignArtifactProvenance",
            "DesignArtifactValidationLink",
            "ValidationResultStatus",
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
            "MaterializationSnapshotReference",
            "MaterializationHandoffContext",
            "MaterializationHandoffEligibility",
            "MaterializationAnalyzerHandoffReference",
            "MaterializedSurfaceCandidate",
            "IterationMaterializedCandidateLink",
            "IterationAnalyzerResultLink",
            "IterationRefinementProposalLink",
            "IterationApprovalCheckpoint",
            "IterationValidationApprovalCheckpoint",
            "IterationApprovalState",
            "IterationConceptSnapshot",
            "IterationDraftSnapshot",
            "IterationAnalyzerOutputSnapshot",
            "IterationRecommendationSnapshot",
            "IterationComparisonSnapshot",
            "IterationGuardrails",
            "IterationCompletionChecklistItem",
            "IterationWorkflowCompletionHistoryEntry",
            "IterationWorkflowCompletion",
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

    [Fact(DisplayName = "Materialization handoff models distinguish readiness metadata from approval semantics")]
    public void DesignStudio_MaterializationHandoff_UsesSeparateReadinessContracts()
    {
        var requestType = RequireType("MaterializationRequest");
        var candidateType = RequireType("MaterializedSurfaceCandidate");
        var metadataType = RequireType("MaterializationAnalyzerHandoffMetadata");

        Assert.Contains(requestType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), property => property.Name == "HandoffContext");
        Assert.Contains(candidateType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), property => property.Name == "HandoffContext");
        Assert.Contains(candidateType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), property => property.Name == "AnalyzerHandoff");

        var metadataProperties = metadataType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ExecutableEligibility", metadataProperties);
        Assert.Contains("WorkspaceOpenState", metadataProperties);
        Assert.DoesNotContain("DeploymentApproval", metadataProperties);
    }

    [Fact(DisplayName = "Validation approval linkage requires analyzer-owned provenance fields")]
    public void DesignStudio_ValidationApprovalLinkage_PreservesAnalyzerOwnedProvenance()
    {
        var validationLinkType = RequireType("DesignArtifactValidationLink");
        var validationLinkProperties = validationLinkType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("AnalyzerRunId", validationLinkProperties);
        Assert.Contains("ResultReference", validationLinkProperties);
        Assert.Contains("SourceCandidateId", validationLinkProperties);
        Assert.Contains("SourceArtifactVersionFingerprint", validationLinkProperties);
        Assert.Contains("ValidationResultStatus", validationLinkProperties);
        Assert.Contains("RefinementIngestionPath", validationLinkProperties);
        Assert.DoesNotContain("DeploymentApproval", validationLinkProperties);
    }

    [Fact(DisplayName = "Closed-loop iteration records preserve explicit linkage and approval separation")]
    public void DesignStudio_IterationRecords_PreserveClosedLoopWorkflowBoundaries()
    {
        var iterationType = RequireType("DesignIterationRecord");
        var approvalStateType = RequireType("IterationApprovalState");
        var validationCheckpointType = RequireType("IterationValidationApprovalCheckpoint");
        var comparisonSnapshotType = RequireType("IterationComparisonSnapshot");
        var guardrailsType = RequireType("IterationGuardrails");

        var iterationProperties = iterationType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("PreviousIterationId", iterationProperties);
        Assert.Contains("SourceArtifactVersionIds", iterationProperties);
        Assert.Contains("MaterializedCandidate", iterationProperties);
        Assert.Contains("AnalyzerResults", iterationProperties);
        Assert.Contains("RefinementProposals", iterationProperties);
        Assert.Contains("ApprovalCheckpoint", iterationProperties);
        Assert.Contains("ComparisonSnapshot", iterationProperties);
        Assert.Contains("Guardrails", iterationProperties);
        Assert.Contains("WorkflowCompletion", iterationProperties);
        Assert.DoesNotContain("DeploymentApproval", iterationProperties);

        var approvalProperties = approvalStateType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("DesignApproval", approvalProperties);
        Assert.Contains("MaterializationApproval", approvalProperties);
        Assert.Contains("RefinementApproval", approvalProperties);
        Assert.Contains("ValidationApproval", approvalProperties);
        Assert.DoesNotContain("DeploymentApproval", approvalProperties);

        var validationProperties = validationCheckpointType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("Owner", validationProperties);
        Assert.Contains("AnalyzerRunId", validationProperties);
        Assert.Contains("ResultReference", validationProperties);
        Assert.Contains("SourceCandidateId", validationProperties);
        Assert.Contains("SourceArtifactVersionFingerprint", validationProperties);
        Assert.Contains("ValidationResultStatus", validationProperties);

        Assert.Contains(comparisonSnapshotType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), property => property.Name == "ValidationStatus");

        var guardrailProperties = guardrailsType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("AutoOptimizationTriggered", guardrailProperties);
        Assert.Contains("AnalyzerExecutionTriggered", guardrailProperties);
        Assert.Contains("ReportMutationTriggered", guardrailProperties);
        Assert.Contains("PbirFilesGenerated", guardrailProperties);
    }

    private static Type RequireType(string typeName)
    {
        var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }
}
