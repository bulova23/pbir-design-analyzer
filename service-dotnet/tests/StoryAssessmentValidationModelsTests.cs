using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests;

public sealed class StoryAssessmentValidationModelsTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ModelsNamespace = "PowerBIModelingService.Services.Pbir.Models";

    [Fact(DisplayName = "Story Assessment validation substrate types exist as internal backend models")]
    public void StoryAssessmentValidationSubstrate_InternalTypesExist()
    {
        string[] expectedTypeNames =
        [
            "StorySignalRegistry",
            "StorySignalRegistryEntry",
            "StorySignalCategory",
            "StorySignalContributionIntent",
            "StorySignalRemediability",
            "StorySignalRequirementRole",
            "StorySignalEvidenceRole",
            "StorySignalReliabilityState",
            "StoryAssessmentSurfaceScope",
            "StoryAssessmentExplanationType",
            "StoryAssessmentActionabilityType",
            "StoryAssessmentValidationDimension",
            "StoryAssessmentValidationLevel",
            "StoryAssessmentValidationRating",
            "StoryAssessmentDimensionEvaluation",
            "StoryAssessmentPromotionState",
            "StoryArchetypeId",
            "StoryArchetypeMatchConfidence",
            "StoryArchetypeValidationStatus",
            "StoryAssessmentPromotionEligibilityState",
            "StoryArchetypeMatchResult",
            "StoryAssessmentLevel1ValidationHarness",
            "StoryAssessmentPromotionGateDefinition",
            "StoryAssessmentArchetypeClassification",
            "StorySemanticCoherenceClassification",
            "StorySemanticCoherenceConfidence",
            "StoryCompetingStoryStatus",
            "StorySemanticCoherenceValidationStatus",
            "StorySemanticTermEvidence",
            "StorySemanticTermCluster",
            "StorySemanticCoherenceLevel1ValidationHarness",
            "StorySemanticCoherenceAssessment",
            "StoryFilterScope",
            "StoryFilterTopologySignalClassification",
            "StoryFilterTopologyFilter",
            "StoryFilterTopologySignal",
            "StoryFilterTopologyAssessment",
            "StoryGapRemediationLayer",
            "StoryGapActionabilityAssessment",
            "StoryGapArchetypeRelevance",
            "StoryGapConfidence",
            "StoryGapEvidenceReference",
            "StoryGapRecord",
            "StoryGapAssessment",
            "StoryConfidenceLowCause",
            "StoryConfidenceBreakdownDimension",
            "StoryConfidenceDimensionRecord",
            "StoryConfidenceBreakdownAssessment",
        ];

        foreach (var typeName in expectedTypeNames)
        {
            var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
            Assert.NotNull(type);
            Assert.True(type!.IsNotPublic, $"{typeName} should remain backend-internal.");
        }
    }

    [Fact(DisplayName = "Story Assessment validation dimension model supports the required four dimensions")]
    public void StoryAssessmentValidationDimensions_SupportRequiredValues()
    {
        var values = GetEnumNames("StoryAssessmentValidationDimension");
        var expected = new[] { "Accuracy", "Consistency", "Explainability", "Actionability" };

        Assert.Equal(expected, values);
    }

    [Fact(DisplayName = "Story Assessment promotion lifecycle supports all required states")]
    public void StoryAssessmentPromotionState_SupportsRequiredValues()
    {
        var values = GetEnumNames("StoryAssessmentPromotionState");
        var expected = new[]
        {
            "Internal",
            "Level1Validated",
            "ContractEligible",
            "Production",
            "CrossSurfaceCandidate",
            "Level2Validated",
            "PlatformCritical",
        };

        Assert.Equal(expected, values);
    }

    [Fact(DisplayName = "Story Assessment surface scope supports PBIR-first and future classification values")]
    public void StoryAssessmentSurfaceScope_SupportsRequiredValues()
    {
        var values = GetEnumNames("StoryAssessmentSurfaceScope");
        var expected = new[] { "PbirSpecific", "CrossSurfaceCandidate", "FutureSurfaceSpecific" };

        Assert.Equal(expected, values);
    }

    [Fact(DisplayName = "Story Assessment archetype identifiers support the six planned validation categories")]
    public void StoryArchetypeId_SupportsRequiredValues()
    {
        var values = GetEnumNames("StoryArchetypeId");
        var expected = new[]
        {
            "PerformanceMonitor",
            "TrendException",
            "Ranking",
            "Comparison",
            "Decomposition",
            "NarrativeWalkthrough",
        };

        Assert.Equal(expected, values);
    }

    [Fact(DisplayName = "Story semantic coherence classification supports focused, split, and sparse states")]
    public void StorySemanticCoherenceClassification_SupportsRequiredValues()
    {
        var values = GetEnumNames("StorySemanticCoherenceClassification");
        var expected = new[] { "Focused", "Split", "Sparse" };

        Assert.Equal(expected, values);
    }

    [Fact(DisplayName = "Competing story status supports precision-first promotion-delayed outputs")]
    public void StoryCompetingStoryStatus_SupportsRequiredValues()
    {
        var values = GetEnumNames("StoryCompetingStoryStatus");
        var expected = new[] { "None", "WeakDiagnosticOnly", "StrongCandidatePromotionDelayed" };

        Assert.Equal(expected, values);
    }

    [Fact(DisplayName = "Story signal registry entry exposes validation-first shaping fields")]
    public void StorySignalRegistryEntry_ExposesExpectedProperties()
    {
        var type = RequireType("StorySignalRegistryEntry");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("String", properties["Id"]);
        Assert.Equal("StorySignalCategory", properties["Category"]);
        Assert.Equal("String", properties["RawValue"]);
        Assert.Equal("Boolean", properties["Fired"]);
        Assert.Equal("StorySignalContributionIntent", properties["ContributionIntent"]);
        Assert.Equal("StorySignalRemediability", properties["Remediability"]);
        Assert.Equal("String", properties["ExplanationHook"]);
        Assert.Equal("StorySignalReliabilityState", properties["ReliabilityState"]);
        Assert.Equal("StoryAssessmentSurfaceScope", properties["SurfaceScope"]);
        Assert.Equal("StorySignalRequirementRole", properties["RequirementRole"]);
        Assert.Equal("StorySignalEvidenceRole", properties["EvidenceRole"]);
        Assert.Equal("StoryAssessmentExplanationType", properties["ExplanationType"]);
        Assert.Equal("StoryAssessmentActionabilityType", properties["ActionabilityType"]);
        Assert.Equal("StoryAssessmentPromotionState", properties["PromotionState"]);
        Assert.Equal("IReadOnlyList`1", properties["Evaluations"]);
    }

    [Fact(DisplayName = "Story signal registry exposes an entry collection for internal runtime extraction")]
    public void StorySignalRegistry_ExposesExpectedProperties()
    {
        var type = RequireType("StorySignalRegistry");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("IReadOnlyList`1", properties["Entries"]);
    }

    [Fact(DisplayName = "Story Assessment dimension evaluation supports deterministic level and rating inspection")]
    public void StoryAssessmentDimensionEvaluation_ExposesExpectedProperties()
    {
        var type = RequireType("StoryAssessmentDimensionEvaluation");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("StoryAssessmentValidationDimension", properties["Dimension"]);
        Assert.Equal("StoryAssessmentValidationLevel", properties["Level"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["Rating"]);
        Assert.Equal("String", properties["Notes"]);
    }

    [Fact(DisplayName = "Story archetype match result exposes internal validation and explanation fields")]
    public void StoryArchetypeMatchResult_ExposesExpectedProperties()
    {
        var type = RequireType("StoryArchetypeMatchResult");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("StoryArchetypeId", properties["ArchetypeId"]);
        Assert.Equal("Double", properties["MatchScore"]);
        Assert.Equal("StoryArchetypeMatchConfidence", properties["MatchConfidence"]);
        Assert.Equal("IReadOnlyList`1", properties["MatchedSignals"]);
        Assert.Equal("IReadOnlyList`1", properties["MissedSignals"]);
        Assert.Equal("IReadOnlyList`1", properties["ExplanationHooks"]);
        Assert.Equal("StoryArchetypeValidationStatus", properties["ValidationStatus"]);
        Assert.Equal("StoryAssessmentPromotionEligibilityState", properties["PromotionEligibilityState"]);
        Assert.Equal("StoryAssessmentPromotionState", properties["PromotionState"]);
    }

    [Fact(DisplayName = "Level 1 validation harness exposes reviewer placeholders and rubric dimensions")]
    public void StoryAssessmentLevel1ValidationHarness_ExposesExpectedProperties()
    {
        var type = RequireType("StoryAssessmentLevel1ValidationHarness");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("String", properties["ReviewerChoice"]);
        Assert.Equal("String", properties["SystemChoice"]);
        Assert.Equal("String", properties["DisagreementReason"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["AccuracyRating"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["ConsistencyRating"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["ExplainabilityRating"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["ActionabilityRating"]);
    }

    [Fact(DisplayName = "Promotion gate definition exposes contract-eligibility thresholds")]
    public void StoryAssessmentPromotionGateDefinition_ExposesExpectedProperties()
    {
        var type = RequireType("StoryAssessmentPromotionGateDefinition");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("Double", properties["MinimumClassificationAccuracy"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["MinimumExplanationQuality"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["MinimumGapUsefulnessPotential"]);
        Assert.Equal("Double", properties["MaximumFalsePositiveRate"]);
        Assert.Equal("Double", properties["ReviewerAgreementThresholdPlaceholder"]);
    }

    [Fact(DisplayName = "Story semantic term evidence exposes canonical term extraction fields")]
    public void StorySemanticTermEvidence_ExposesExpectedProperties()
    {
        var type = RequireType("StorySemanticTermEvidence");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("String", properties["CanonicalTerm"]);
        Assert.Equal("String", properties["RawText"]);
        Assert.Equal("String", properties["Source"]);
        Assert.Equal("Double", properties["Weight"]);
    }

    [Fact(DisplayName = "Story semantic term cluster exposes deterministic clustering fields")]
    public void StorySemanticTermCluster_ExposesExpectedProperties()
    {
        var type = RequireType("StorySemanticTermCluster");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("String", properties["ClusterId"]);
        Assert.Equal("Double", properties["Weight"]);
        Assert.Equal("Int32", properties["SupportCount"]);
        Assert.Equal("IReadOnlyList`1", properties["Terms"]);
        Assert.Equal("String", properties["ExplanationHook"]);
    }

    [Fact(DisplayName = "Story semantic coherence assessment exposes internal scoring and validation fields")]
    public void StorySemanticCoherenceAssessment_ExposesExpectedProperties()
    {
        var type = RequireType("StorySemanticCoherenceAssessment");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("Double", properties["CoherenceScore"]);
        Assert.Equal("StorySemanticCoherenceClassification", properties["CoherenceClassification"]);
        Assert.Equal("String", properties["DominantConcept"]);
        Assert.Equal("IReadOnlyList`1", properties["ExtractedTerms"]);
        Assert.Equal("IReadOnlyList`1", properties["TermClusters"]);
        Assert.Equal("StoryCompetingStoryStatus", properties["CompetingStoryStatus"]);
        Assert.Equal("IReadOnlyList`1", properties["WeakDisagreementSignals"]);
        Assert.Equal("IReadOnlyList`1", properties["ExplanationHooks"]);
        Assert.Equal("StorySemanticCoherenceConfidence", properties["Confidence"]);
        Assert.Equal("StorySemanticCoherenceValidationStatus", properties["ValidationStatus"]);
        Assert.Equal("StoryAssessmentPromotionState", properties["PromotionState"]);
        Assert.Equal("StorySemanticCoherenceLevel1ValidationHarness", properties["Level1ValidationHarness"]);
    }

    [Fact(DisplayName = "Story semantic coherence validation harness exposes expert-review placeholders")]
    public void StorySemanticCoherenceLevel1ValidationHarness_ExposesExpectedProperties()
    {
        var type = RequireType("StorySemanticCoherenceLevel1ValidationHarness");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("String", properties["ReviewerCoherenceChoice"]);
        Assert.Equal("String", properties["SystemCoherenceChoice"]);
        Assert.Equal("String", properties["ReviewerDominantConcept"]);
        Assert.Equal("String", properties["SystemDominantConcept"]);
        Assert.Equal("String", properties["DisagreementReason"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["AccuracyRating"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["ConsistencyRating"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["ExplainabilityRating"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["ActionabilityRating"]);
    }

    [Fact(DisplayName = "Story filter topology filter exposes scope and hierarchy extraction fields")]
    public void StoryFilterTopologyFilter_ExposesExpectedProperties()
    {
        var type = RequireType("StoryFilterTopologyFilter");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("String", properties["SourceId"]);
        Assert.Equal("StoryFilterScope", properties["Scope"]);
        Assert.Equal("String", properties["DisplayLabel"]);
        Assert.Equal("IReadOnlyList`1", properties["FieldHints"]);
        Assert.Equal("String", properties["HierarchyPattern"]);
        Assert.Equal("Int32", properties["HierarchyDepth"]);
        Assert.Equal("String", properties["PlacementZone"]);
    }

    [Fact(DisplayName = "Story filter topology signal exposes reinforcement and usefulness fields")]
    public void StoryFilterTopologySignal_ExposesExpectedProperties()
    {
        var type = RequireType("StoryFilterTopologySignal");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("String", properties["Id"]);
        Assert.Equal("StoryFilterTopologySignalClassification", properties["Classification"]);
        Assert.Equal("StoryAssessmentSurfaceScope", properties["SurfaceScope"]);
        Assert.Equal("StoryFilterScope", properties["Scope"]);
        Assert.Equal("Boolean", properties["Fired"]);
        Assert.Equal("Boolean", properties["SupportsArchetypeReinforcement"]);
        Assert.Equal("StoryAssessmentPromotionState", properties["PromotionState"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["AccuracyContribution"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["ExplainabilityContribution"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["ActionabilityContribution"]);
    }

    [Fact(DisplayName = "Story filter topology assessment exposes extraction, classification, and validation fields")]
    public void StoryFilterTopologyAssessment_ExposesExpectedProperties()
    {
        var type = RequireType("StoryFilterTopologyAssessment");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("Int32", properties["SlicerCount"]);
        Assert.Equal("Int32", properties["PageFilterCount"]);
        Assert.Equal("Int32", properties["ReportFilterCount"]);
        Assert.Equal("IReadOnlyList`1", properties["ExtractedFilters"]);
        Assert.Equal("IReadOnlyList`1", properties["HierarchyPatterns"]);
        Assert.Equal("IReadOnlyList`1", properties["TopologyCharacteristics"]);
        Assert.Equal("IReadOnlyList`1", properties["Signals"]);
        Assert.Equal("IReadOnlyList`1", properties["ReinforcedArchetypes"]);
        Assert.Equal("IReadOnlyList`1", properties["DiagnosticNotes"]);
        Assert.Equal("StoryAssessmentPromotionState", properties["PromotionState"]);
        Assert.Equal("StoryAssessmentSurfaceScope", properties["SurfaceScope"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["AccuracyContribution"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["ExplainabilityContribution"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["ActionabilityContribution"]);
    }

    [Fact(DisplayName = "Story gap remediation layer supports report, model, and restructure outputs")]
    public void StoryGapRemediationLayer_SupportsRequiredValues()
    {
        var values = GetEnumNames("StoryGapRemediationLayer");
        var expected = new[] { "Report", "Model", "Restructure" };

        Assert.Equal(expected, values);
    }

    [Fact(DisplayName = "Story gap actionability supports actionable, partial, and downgraded states")]
    public void StoryGapActionabilityAssessment_SupportsRequiredValues()
    {
        var values = GetEnumNames("StoryGapActionabilityAssessment");
        var expected = new[] { "Actionable", "PartlyActionable", "NotActionable" };

        Assert.Equal(expected, values);
    }

    [Fact(DisplayName = "Story gap confidence supports bounded low, medium, and high labels")]
    public void StoryGapConfidence_SupportsRequiredValues()
    {
        var values = GetEnumNames("StoryGapConfidence");
        var expected = new[] { "Low", "Medium", "High" };

        Assert.Equal(expected, values);
    }

    [Fact(DisplayName = "Story gap evidence reference exposes internal evidence linkage fields")]
    public void StoryGapEvidenceReference_ExposesExpectedProperties()
    {
        var type = RequireType("StoryGapEvidenceReference");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("String", properties["SourceType"]);
        Assert.Equal("String", properties["ReferenceId"]);
        Assert.Equal("String", properties["Summary"]);
    }

    [Fact(DisplayName = "Story gap record exposes evidence-backed remediation and confidence fields")]
    public void StoryGapRecord_ExposesExpectedProperties()
    {
        var type = RequireType("StoryGapRecord");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("String", properties["GapId"]);
        Assert.Equal("String", properties["Description"]);
        Assert.Equal("IReadOnlyList`1", properties["EvidenceReferences"]);
        Assert.Equal("StoryGapRemediationLayer", properties["RemediationLayer"]);
        Assert.Equal("StoryGapActionabilityAssessment", properties["ActionabilityAssessment"]);
        Assert.Equal("StoryGapArchetypeRelevance", properties["ArchetypeRelevance"]);
        Assert.Equal("StoryAssessmentPromotionState", properties["PromotionState"]);
        Assert.Equal("StoryGapConfidence", properties["Confidence"]);
    }

    [Fact(DisplayName = "Story gap assessment exposes internal-only gap collection")]
    public void StoryGapAssessment_ExposesExpectedProperties()
    {
        var type = RequireType("StoryGapAssessment");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("StoryAssessmentSurfaceScope", properties["SurfaceScope"]);
        Assert.Equal("StoryAssessmentPromotionState", properties["PromotionState"]);
        Assert.Equal("IReadOnlyList`1", properties["Gaps"]);
    }

    [Fact(DisplayName = "Story confidence low-cause model supports explicit bounded downgrade reasons")]
    public void StoryConfidenceLowCause_SupportsRequiredValues()
    {
        var values = GetEnumNames("StoryConfidenceLowCause");
        var expected = new[]
        {
            "SparseEvidence",
            "ConflictingEvidence",
            "WeakArchetypeMatch",
            "LowSemanticCoherence",
            "MissingContext",
        };

        Assert.Equal(expected, values);
    }

    [Fact(DisplayName = "Story confidence breakdown dimension supports the four validation dimensions")]
    public void StoryConfidenceBreakdownDimension_SupportsRequiredValues()
    {
        var values = GetEnumNames("StoryConfidenceBreakdownDimension");
        var expected = new[] { "Accuracy", "Consistency", "Explainability", "Actionability" };

        Assert.Equal(expected, values);
    }

    [Fact(DisplayName = "Story confidence dimension record exposes internal evidence-linked dimension fields")]
    public void StoryConfidenceDimensionRecord_ExposesExpectedProperties()
    {
        var type = RequireType("StoryConfidenceDimensionRecord");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("StoryConfidenceBreakdownDimension", properties["DimensionId"]);
        Assert.Equal("String", properties["DimensionLabel"]);
        Assert.Equal("StoryAssessmentValidationRating", properties["Rating"]);
        Assert.Equal("IReadOnlyList`1", properties["ConfidenceDrivers"]);
        Assert.Equal("IReadOnlyList`1", properties["ConfidenceReducers"]);
        Assert.Equal("IReadOnlyList`1", properties["MissingSignals"]);
        Assert.Equal("IReadOnlyList`1", properties["EvidenceReferences"]);
        Assert.Equal("String", properties["Explanation"]);
        Assert.Equal("StoryGapActionabilityAssessment", properties["Actionability"]);
        Assert.Equal("StoryAssessmentPromotionState", properties["PromotionState"]);
        Assert.Equal("StoryAssessmentSurfaceScope", properties["SurfaceScope"]);
    }

    [Fact(DisplayName = "Story confidence breakdown assessment exposes strongest weakest and low-cause outputs")]
    public void StoryConfidenceBreakdownAssessment_ExposesExpectedProperties()
    {
        var type = RequireType("StoryConfidenceBreakdownAssessment");
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("StoryAssessmentSurfaceScope", properties["SurfaceScope"]);
        Assert.Equal("StoryAssessmentPromotionState", properties["PromotionState"]);
        Assert.Equal("IReadOnlyList`1", properties["Dimensions"]);
        Assert.Equal("IReadOnlyList`1", properties["StrongestDimensions"]);
        Assert.Equal("IReadOnlyList`1", properties["WeakestDimensions"]);
        Assert.Equal("IReadOnlyList`1", properties["LowConfidenceCauses"]);
    }

    [Fact(DisplayName = "Public ScoreResult contract does not expose Story Assessment validation substrate models")]
    public void ScoreResult_PublicContract_DoesNotExposeValidationSubstrate()
    {
        var disallowedPropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "StorySignalRegistry",
            "InternalStorySignalRegistry",
            "InternalStoryAssessmentArchetypeClassification",
            "InternalStorySemanticCoherenceAssessment",
            "InternalStoryFilterTopologyAssessment",
            "InternalStoryGapAssessment",
            "InternalStoryConfidenceBreakdownAssessment",
            "StoryAssessmentValidation",
            "StoryAssessmentPromotionState",
            "StoryAssessmentDimensionEvaluations",
            "StoryAssessmentArchetypeClassification",
            "StorySemanticCoherenceAssessment",
            "StoryFilterTopologyAssessment",
            "StoryGapAssessment",
            "StoryGapRecord",
            "StoryConfidenceBreakdownAssessment",
            "StoryConfidenceDimensionRecord",
        };

        var publicPropertyNames = typeof(ScoreResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Empty(publicPropertyNames.Intersect(disallowedPropertyNames, StringComparer.Ordinal));
    }

    [Fact(DisplayName = "Public PageScore contract does not expose Story Assessment validation substrate models")]
    public void PageScore_PublicContract_DoesNotExposeValidationSubstrate()
    {
        var disallowedPropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "StorySignalRegistry",
            "InternalStorySignalRegistry",
            "InternalStoryAssessmentArchetypeClassification",
            "InternalStorySemanticCoherenceAssessment",
            "InternalStoryFilterTopologyAssessment",
            "InternalStoryGapAssessment",
            "InternalStoryConfidenceBreakdownAssessment",
            "StoryAssessmentArchetypeClassification",
            "StorySemanticCoherenceAssessment",
            "StoryFilterTopologyAssessment",
            "StoryGapAssessment",
            "StoryGapRecord",
            "StoryConfidenceBreakdownAssessment",
            "StoryConfidenceDimensionRecord",
        };

        var publicPropertyNames = typeof(PageScore)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Empty(publicPropertyNames.Intersect(disallowedPropertyNames, StringComparer.Ordinal));
    }

    private static string[] GetEnumNames(string typeName)
    {
        var type = RequireType(typeName);
        Assert.True(type.IsEnum, $"{typeName} should be an enum.");
        return Enum.GetNames(type);
    }

    private static Type RequireType(string typeName)
    {
        return CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: true)!;
    }
}
