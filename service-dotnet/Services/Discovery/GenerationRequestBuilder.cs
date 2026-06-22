using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationRequestBuilder
{
    internal GenerationRequestCreationResult Create(
        DesignPackageConsumptionResult consumptionResult,
        string schemaVersion = GenerationRequestContract.SchemaVersionV1)
    {
        ArgumentNullException.ThrowIfNull(consumptionResult);

        var missingInputs = new List<string>();
        var compatibilityFailures = new List<string>();
        var unsupportedTargetProfiles = new List<string>();

        if (!consumptionResult.IsValid)
        {
            missingInputs.Add("designPackageConsumptionResult");
            missingInputs.AddRange(consumptionResult.Diagnostics.MissingRequiredFields.Select(field => $"designPackage.{field}"));
            unsupportedTargetProfiles.AddRange(consumptionResult.Diagnostics.UnsupportedExperienceTypes.Select(MapUnsupportedExperienceType));
            compatibilityFailures.AddRange(consumptionResult.Diagnostics.IncompatiblePackageStates);
        }

        if (consumptionResult.NormalizedGenerationInput is null)
        {
            missingInputs.Add("normalizedGenerationInput");
        }

        if (consumptionResult.ConsumedPackage is null)
        {
            missingInputs.Add("consumedDesignPackageView");
        }

        if (missingInputs.Count > 0 || unsupportedTargetProfiles.Count > 0 || compatibilityFailures.Count > 0)
        {
            return new GenerationRequestCreationResult(
                Request: null,
                Diagnostics: new GenerationRequestValidationDiagnostics(
                    MissingRequiredSections: [],
                    MissingRequiredFields: [],
                    MissingInputs: missingInputs.Distinct(StringComparer.Ordinal).ToArray(),
                    UnsupportedTargetProfiles: unsupportedTargetProfiles.Distinct(StringComparer.Ordinal).ToArray(),
                    UnsupportedSchemaVersions: [],
                    CompatibilityFailures: compatibilityFailures.Distinct(StringComparer.Ordinal).ToArray()));
        }

        var normalized = consumptionResult.NormalizedGenerationInput!;
        var semanticModelBinding = ResolveSemanticBinding(normalized.Lineage);
        var request = new GenerationRequest(
            SchemaVersion: schemaVersion,
            RequestId: BuildRequestId(normalized.SourceDesignPackageRef, normalized.TargetArtifactType),
            SourceDesignPackageRef: normalized.SourceDesignPackageRef,
            TargetArtifactProfile: new GenerationRequestTargetArtifactProfile(
                ArtifactType: MapArtifactType(normalized.TargetArtifactType),
                ProfileId: ResolveProfileId(normalized.TargetArtifactType),
                SourceExperienceType: normalized.SourceExperienceType),
            GenerationMode: new GenerationRequestMode(
                Authority: GenerationRequestContract.AdvisoryConstructionOnlyAuthority,
                ReviewRequired: normalized.SuccessContract.ReviewRequired,
                AllowPartialOutput: true),
            DesignIntent: new GenerationRequestDesignIntent(
                PrimaryAudience: normalized.PrimaryAudience,
                SecondaryAudiences: normalized.SecondaryAudiences,
                BusinessOutcome: normalized.BusinessOutcome,
                AnalyticalFlow: new GenerationRequestAnalyticalFlow(
                    Question: normalized.AnalyticalFlow.Question,
                    Investigation: normalized.AnalyticalFlow.Investigation,
                    Evidence: normalized.AnalyticalFlow.Evidence,
                    Decision: normalized.AnalyticalFlow.Decision)),
            StructuralIntent: new GenerationRequestStructuralIntent(
                Pages: normalized.PagesOrRoutes
                    .Select(page => new GenerationRequestPageIntent(
                        Name: page.Name,
                        Purpose: page.Purpose,
                        NavigationIntent: page.NavigationIntent))
                    .ToArray(),
                Navigation: new GenerationRequestNavigationIntent(
                    Hierarchy: normalized.NavigationHierarchy,
                    WorkflowPath: normalized.WorkflowPath),
                VisualHints: normalized.VisualHints
                    .Select(hint => new GenerationRequestVisualHint(
                        PageName: hint.PageName,
                        VisualType: hint.VisualType,
                        VisualPurpose: hint.VisualPurpose))
                    .ToArray()),
            DataIntent: new GenerationRequestDataIntent(
                Kpis: normalized.Kpis
                    .Select(kpi => new GenerationRequestKpiIntent(
                        Name: kpi.Name,
                        Purpose: kpi.Purpose,
                        Grouping: kpi.Grouping))
                    .ToArray(),
                Filters: new GenerationRequestFilters(
                    GlobalFilters: normalized.Filters.GlobalFilters,
                    PageFilters: normalized.Filters.PageFilters
                        .Select(filter => new GenerationRequestPageFilter(
                            PageName: filter.PageName,
                            Filters: filter.Filters))
                        .ToArray()),
                SemanticBinding: semanticModelBinding),
            SuccessContract: new GenerationRequestSuccessContract(
                BusinessSuccessCriteria: normalized.SuccessContract.BusinessSuccessCriteria,
                AnalyticalSuccessCriteria: normalized.SuccessContract.AnalyticalSuccessCriteria,
                ValidationRequirements: BuildValidationRequirements(normalized.SuccessContract)),
            Provenance: new GenerationRequestProvenance(
                SourceDesignPackageRef: normalized.SourceDesignPackageRef,
                Lineage: normalized.Lineage,
                AdapterMetadata: new GenerationRequestAdapterMetadata(
                    AdapterFamily: GenerationRequestContract.ProviderNeutralAdapterFamily,
                    ExecutionMode: GenerationRequestContract.PromptSegmentsOnlyExecutionMode,
                    ProviderSpecificExecution: false)),
            ReviewPolicy: new GenerationRequestReviewPolicy(
                DesignApprovalRequired: true,
                GenerationApprovalRequired: true,
                AnalyzerReviewRequired: true));

        return new GenerationRequestCreationResult(request, GenerationRequestValidationDiagnostics.Empty);
    }

    private static GenerationRequestArtifactType MapArtifactType(GenerationArtifactType artifactType)
    {
        return artifactType switch
        {
            GenerationArtifactType.PbirReport => GenerationRequestArtifactType.PbirReport,
            GenerationArtifactType.FabricDataApp => GenerationRequestArtifactType.FabricDataApp,
            GenerationArtifactType.FabricApp => GenerationRequestArtifactType.FabricApp,
            _ => throw new ArgumentOutOfRangeException(nameof(artifactType), artifactType, "Unsupported generation artifact type."),
        };
    }

    private static string ResolveProfileId(GenerationArtifactType artifactType)
    {
        return artifactType switch
        {
            GenerationArtifactType.PbirReport => GenerationRequestContract.PbirReportDefaultProfile,
            GenerationArtifactType.FabricDataApp => GenerationRequestContract.FabricDataAppDefaultProfile,
            GenerationArtifactType.FabricApp => GenerationRequestContract.FabricAppDefaultProfile,
            _ => throw new ArgumentOutOfRangeException(nameof(artifactType), artifactType, "Unsupported generation artifact type."),
        };
    }

    private static string MapUnsupportedExperienceType(string experienceType)
    {
        return string.Equals(experienceType, OpportunityExperienceType.FabricApp.ToString(), StringComparison.Ordinal)
            ? GenerationRequestTargetProfileCatalog.ToContractValue(GenerationRequestArtifactType.FabricApp)
            : experienceType;
    }

    private static GenerationRequestSemanticBinding ResolveSemanticBinding(IReadOnlyList<DesignPackageReference> lineage)
    {
        var semanticModel = lineage.FirstOrDefault(reference => string.Equals(reference.Stage, "semanticModel", StringComparison.Ordinal));
        return semanticModel is null
            ? new GenerationRequestSemanticBinding(string.Empty, string.Empty)
            : new GenerationRequestSemanticBinding(semanticModel.ReferenceId, semanticModel.Label);
    }

    private static IReadOnlyList<string> BuildValidationRequirements(NormalizedSuccessContract successContract)
    {
        var requirements = new List<string>();

        if (successContract.ReviewRequired)
        {
            requirements.Add("Analyzer review required.");
        }

        if (successContract.ValidationRequired)
        {
            requirements.Add("Validation required before downstream handoff.");
        }

        return requirements;
    }

    private static string BuildRequestId(string sourceDesignPackageRef, GenerationArtifactType artifactType)
    {
        return $"genreq:{GenerationRequestTargetProfileCatalog.ToContractValue(MapArtifactType(artifactType))}:{sourceDesignPackageRef}";
    }
}
