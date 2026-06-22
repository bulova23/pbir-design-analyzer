using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ExecutionPlanBuilder
{
    private static readonly ExecutionPlanProviderCapabilityModel CapabilityModel = new(
        SupportsLayoutGeneration: true,
        SupportsSemanticGeneration: true,
        SupportsArtifactGeneration: false,
        SupportsValidation: false);

    internal ExecutionPlanCreationResult Create(
        GenerationRequest request,
        string schemaVersion = ExecutionPlanContract.SchemaVersionV1)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestValidation = new GenerationRequestValidator().Validate(request);
        if (!requestValidation.IsValid)
        {
            return new ExecutionPlanCreationResult(
                Plan: null,
                Diagnostics: new ExecutionPlanValidationDiagnostics(
                    MissingRequiredSections: requestValidation.Diagnostics.MissingRequiredSections,
                    MissingRequiredFields: requestValidation.Diagnostics.MissingRequiredFields,
                    UnsupportedTargetProfiles: requestValidation.Diagnostics.UnsupportedTargetProfiles,
                    UnsupportedSchemaVersions: requestValidation.Diagnostics.UnsupportedSchemaVersions,
                    DependencyFailures: [],
                    CapabilityInconsistencies: [],
                    TargetCompatibilityFailures: requestValidation.Diagnostics.CompatibilityFailures,
                    ReviewRequirementFailures: []));
        }

        var supportedCapabilities = BuildSupportedCapabilities(CapabilityModel);
        var unsupportedCapabilities = BuildUnsupportedCapabilities(CapabilityModel);
        var plan = new ExecutionPlan(
            SchemaVersion: schemaVersion,
            ExecutionPlanId: $"execplan:{GenerationRequestTargetProfileCatalog.ToContractValue(request.TargetArtifactProfile.ArtifactType)}:{request.RequestId}",
            SourceReferences: new ExecutionPlanSourceReferences(
                GenerationRequestRef: request.RequestId,
                SourceDesignPackageRef: request.SourceDesignPackageRef),
            TargetDefinition: new ExecutionPlanTargetDefinition(
                TargetArtifactProfile: request.TargetArtifactProfile,
                ExperienceType: request.TargetArtifactProfile.SourceExperienceType),
            ProviderPlanningMetadata: new ExecutionPlanProviderPlanningMetadata(
                ProviderCategory: ExecutionPlanContract.ProviderNeutralPlanningCategory,
                CapabilityModel: CapabilityModel,
                SupportedCapabilities: supportedCapabilities,
                UnsupportedCapabilities: unsupportedCapabilities),
            PlannedWorkUnits:
            [
                new ExecutionPlanWorkUnit("schema-analysis", "Schema Analysis", "Inspect the Generation Request contract and lock provider-safe inputs."),
                new ExecutionPlanWorkUnit("artifact-design", "Artifact Design", "Translate target intent into provider-neutral artifact structure guidance."),
                new ExecutionPlanWorkUnit("layout-planning", "Layout Planning", "Describe future layout-generation work without generating layouts."),
                new ExecutionPlanWorkUnit("semantic-planning", "Semantic Planning", "Describe future semantic-binding work without mutating the semantic model."),
                new ExecutionPlanWorkUnit("validation-planning", "Validation Planning", "Describe future validation sequencing without validating generated outputs."),
            ],
            DependencyGraph: new ExecutionPlanDependencyGraph(
                ExecutionOrder: ["schema-analysis", "artifact-design", "layout-planning", "semantic-planning", "validation-planning"],
                Dependencies:
                [
                    new ExecutionPlanDependency("schema-analysis", []),
                    new ExecutionPlanDependency("artifact-design", ["schema-analysis"]),
                    new ExecutionPlanDependency("layout-planning", ["artifact-design"]),
                    new ExecutionPlanDependency("semantic-planning", ["schema-analysis"]),
                    new ExecutionPlanDependency("validation-planning", ["layout-planning", "semantic-planning"]),
                ]),
            PlanningConstraints: new ExecutionPlanPlanningConstraints(
                UnsupportedTargets: [],
                UnsupportedCapabilities: unsupportedCapabilities,
                ReviewRequirements: BuildReviewRequirementConstraints(request.ReviewPolicy),
                ValidationRequirements: request.SuccessContract.ValidationRequirements),
            ReviewRequirements: new ExecutionPlanReviewRequirements(
                DesignApprovalRequired: request.ReviewPolicy.DesignApprovalRequired,
                GenerationApprovalRequired: request.ReviewPolicy.GenerationApprovalRequired,
                AnalyzerReviewRequired: request.ReviewPolicy.AnalyzerReviewRequired),
            SuccessContract: request.SuccessContract);

        return new ExecutionPlanCreationResult(plan, ExecutionPlanValidationDiagnostics.Empty);
    }

    internal static IReadOnlyList<string> BuildSupportedCapabilities(ExecutionPlanProviderCapabilityModel capabilityModel)
    {
        var capabilities = new List<string>();

        if (capabilityModel.SupportsLayoutGeneration)
        {
            capabilities.Add("layoutGeneration");
        }

        if (capabilityModel.SupportsSemanticGeneration)
        {
            capabilities.Add("semanticGeneration");
        }

        if (capabilityModel.SupportsArtifactGeneration)
        {
            capabilities.Add("artifactGeneration");
        }

        if (capabilityModel.SupportsValidation)
        {
            capabilities.Add("validation");
        }

        return capabilities;
    }

    internal static IReadOnlyList<string> BuildUnsupportedCapabilities(ExecutionPlanProviderCapabilityModel capabilityModel)
    {
        var capabilities = new List<string>();

        if (!capabilityModel.SupportsLayoutGeneration)
        {
            capabilities.Add("layoutGeneration");
        }

        if (!capabilityModel.SupportsSemanticGeneration)
        {
            capabilities.Add("semanticGeneration");
        }

        if (!capabilityModel.SupportsArtifactGeneration)
        {
            capabilities.Add("artifactGeneration");
        }

        if (!capabilityModel.SupportsValidation)
        {
            capabilities.Add("validation");
        }

        return capabilities;
    }

    private static IReadOnlyList<string> BuildReviewRequirementConstraints(GenerationRequestReviewPolicy reviewPolicy)
    {
        var constraints = new List<string>();

        if (reviewPolicy.DesignApprovalRequired)
        {
            constraints.Add("Design approval required.");
        }

        if (reviewPolicy.GenerationApprovalRequired)
        {
            constraints.Add("Generation approval required.");
        }

        if (reviewPolicy.AnalyzerReviewRequired)
        {
            constraints.Add("Analyzer review required.");
        }

        return constraints;
    }
}
