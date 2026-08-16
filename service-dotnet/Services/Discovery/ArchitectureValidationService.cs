using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ArchitectureValidationService
{
    internal ArchitectureValidationContext CreateContext(DesignPackage package, DateTimeOffset createdUtc)
    {
        ArgumentNullException.ThrowIfNull(package);

        var planning = new PlanningOrchestrationService().Orchestrate(package);
        var specificationService = new PbirGenerationSpecificationService();
        var specificationState = specificationService.PrepareForGenerationProvider(specificationService.CreateSpecification(planning));
        var providerState = new GenerationProviderFrameworkService().CreateProviderState(specificationState);
        var executionPlanningState = new GenerationProviderExecutionPlanningService().CreatePlanState(
            providerState.Request!,
            providerState.Provider!,
            specificationState,
            planning.Outcome);

        var runtimeRegistry = new RuntimeProviderRegistry();
        var runtimeService = new RuntimeProviderAbstractionFrameworkService(runtimeRegistry);
        var runtimeRegistration = runtimeService.CreateDefaultRegistration(
            planning.ExecutionProviderState!.ProviderDefinition!,
            planning.ExecutionProviderState.ProviderRequest!);
        runtimeRegistry.Register(runtimeRegistration);
        var runtimeProviderState = runtimeService.CreateRuntimeCandidate(planning, runtimeRegistration.ProviderId);

        var microsoftRuntimeService = new MicrosoftRuntimeProviderContractFrameworkService(runtimeRegistry);
        var microsoftRuntimeDefinition = microsoftRuntimeService.CreateDefaultProviderDefinition();
        runtimeRegistry.Register(microsoftRuntimeService.CreateDefaultRegistration(microsoftRuntimeDefinition, planning));
        var microsoftRuntimeState = microsoftRuntimeService.CreateMicrosoftRuntimeState(planning, microsoftRuntimeDefinition.ProviderId);

        var pbirPrototypeState = new PbirExecutionPrototypeBoundaryService().CreatePrototypeBoundary(
            planning,
            microsoftRuntimeState,
            PbirExecutionPrototypeOptions.DryRunDefault);

        var manifestState = new GenerationManifestService().CreateManifestState(
            planning,
            specificationState,
            providerState,
            executionPlanningState,
            runtimeProviderState,
            microsoftRuntimeState,
            createdUtc);
        var pipelineVerificationState = new GenerationPipelineVerificationService().VerifyPipeline(
            planning,
            specificationState,
            providerState,
            executionPlanningState,
            runtimeProviderState,
            microsoftRuntimeState,
            manifestState);

        return new ArchitectureValidationContext(
            planning,
            pbirPrototypeState,
            specificationState,
            providerState,
            executionPlanningState,
            runtimeProviderState,
            microsoftRuntimeState,
            manifestState,
            pipelineVerificationState);
    }

    internal ArchitectureValidationState Validate(DesignPackage package, DateTimeOffset createdUtc)
    {
        return Validate(CreateContext(package, createdUtc));
    }

    internal ArchitectureValidationState Validate(ArchitectureValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var pipelineVerification = new GenerationPipelineVerificationService().VerifyPipeline(
            context.Planning,
            context.SpecificationState,
            context.ProviderState,
            context.ExecutionPlanningState,
            context.RuntimeProviderState,
            context.MicrosoftRuntimeState,
            context.ManifestState);
        var contextWithPipeline = context with
        {
            PipelineVerificationState = pipelineVerification
        };

        var frameworkParticipation = CreateFrameworkParticipation(contextWithPipeline);
        var trustBoundaryVerification = CreateTrustBoundaryVerification(contextWithPipeline);
        var ownershipVerification = CreateOwnershipVerification(contextWithPipeline);
        var providerNeutralityVerification = CreateProviderNeutralityVerification(contextWithPipeline);
        var diagnostics = CreateDiagnostics(
            contextWithPipeline,
            frameworkParticipation,
            trustBoundaryVerification,
            ownershipVerification,
            providerNeutralityVerification);

        if (diagnostics.HasFailures)
        {
            return new ArchitectureValidationState(
                Validation: null,
                Diagnostics: diagnostics);
        }

        var validation = new ArchitectureValidation(
            SchemaVersion: ArchitectureValidationContract.SchemaVersionV1,
            ValidationId: $"architectureValidation:{context.Planning.Outcome.References.DesignPackageRef}",
            FrameworkParticipation: frameworkParticipation,
            TrustBoundaryVerification: trustBoundaryVerification,
            OwnershipVerification: ownershipVerification,
            ProviderNeutralityVerification: providerNeutralityVerification);

        return new ArchitectureValidationState(validation, diagnostics);
    }

    private static IReadOnlyList<ArchitectureFrameworkParticipation> CreateFrameworkParticipation(ArchitectureValidationContext context)
    {
        return
        [
            CreateFramework("designPackageConsumption", "DesignPackageConsumptionResult", nameof(DesignPackageConsumptionService), "design-package-consumption/internal", context.Planning.ConsumptionResult.IsValid),
            CreateFramework("generationRequest", "GenerationRequest", nameof(GenerationRequestFrameworkService), GenerationRequestContract.SchemaVersionV1, context.Planning.GenerationRequestState.Request is not null),
            CreateFramework("executionPlan", "ExecutionPlan", nameof(ExecutionPlanFrameworkService), ExecutionPlanContract.SchemaVersionV1, context.Planning.ExecutionPlanState.Plan is not null),
            CreateFramework("providerAdapter", "ProviderAdapter", nameof(ProviderAdapterFrameworkService), ProviderAdapterContract.SchemaVersionV1, context.Planning.ProviderAdapterState.AdapterRequest is not null),
            CreateFramework("microsoftAdapterSpecification", "MicrosoftAdapterSpecification", nameof(MicrosoftAdapterSpecificationService), MicrosoftAdapterSpecificationContract.SchemaVersionV1, context.Planning.MicrosoftPlanningState?.Specification is not null),
            CreateFramework("capabilityNegotiation", "CapabilityNegotiationResult", nameof(CapabilityNegotiationService), CapabilityNegotiationContract.SchemaVersionV1, context.Planning.CapabilityNegotiationState?.Result is not null),
            CreateFramework("executionProviderContract", "ExecutionProviderDefinition", nameof(ExecutionProviderContractFrameworkService), ExecutionProviderContract.SchemaVersionV1, context.Planning.ExecutionProviderState?.ProviderDefinition is not null),
            CreateFramework("planningOrchestration", "PlanningOrchestrationResult", nameof(PlanningOrchestrationService), PlanningOrchestrationContract.SchemaVersionV1, context.Planning.OrchestrationState.SchemaVersion == PlanningOrchestrationContract.SchemaVersionV1),
            CreateFramework("runtimeProviderAbstraction", "RuntimeProviderFrameworkState", nameof(RuntimeProviderAbstractionFrameworkService), RuntimeProviderContract.SchemaVersionV1, context.RuntimeProviderState.Request is not null),
            CreateFramework("microsoftRuntimeProviderContract", "MicrosoftRuntimeProviderFrameworkState", nameof(MicrosoftRuntimeProviderContractFrameworkService), MicrosoftRuntimeProviderContract.SchemaVersionV1, context.MicrosoftRuntimeState.Request is not null),
            CreateFramework("microsoftSkillsCapabilityCatalog", "MicrosoftSkillPlanningState", nameof(MicrosoftSkillsCapabilityCatalogFrameworkService), MicrosoftSkillsCatalogContract.SchemaVersionV1, context.Planning.MicrosoftSkillState?.Catalog is not null),
            CreateFramework("microsoftSkillProviderAdapter", "MicrosoftSkillProviderPlanningState", nameof(MicrosoftSkillProviderAdapterFrameworkService), MicrosoftSkillProviderAdapterContract.SchemaVersionV1, context.Planning.MicrosoftSkillProviderState?.Selection is not null),
            CreateFramework("pbirExecutionPrototypeBoundary", "PbirExecutionPrototypeState", nameof(PbirExecutionPrototypeBoundaryService), PbirExecutionPrototypeContract.SchemaVersionV1, context.PbirPrototypeState.SchemaVersion == PbirExecutionPrototypeContract.SchemaVersionV1),
            CreateFramework("pbirGenerationSpecification", "PbirGenerationSpecificationState", nameof(PbirGenerationSpecificationService), PbirGenerationSpecificationContract.SchemaVersionV1, context.SpecificationState.Specification is not null),
            CreateFramework("generationProvider", "GenerationProviderFrameworkState", nameof(GenerationProviderFrameworkService), GenerationProviderContract.SchemaVersionV1, context.ProviderState.Request is not null),
            CreateFramework("generationProviderExecutionPlanning", "GenerationProviderExecutionPlanningState", nameof(GenerationProviderExecutionPlanningService), GenerationProviderExecutionPlanContract.SchemaVersionV1, context.ExecutionPlanningState.Plan is not null),
            CreateFramework("generationManifest", "GenerationManifestState", nameof(GenerationManifestService), GenerationManifestContract.SchemaVersionV1, context.ManifestState.Manifest is not null),
            CreateFramework("generationPipelineVerification", "GenerationPipelineVerificationState", nameof(GenerationPipelineVerificationService), GenerationPipelineVerificationContract.SchemaVersionV1, context.PipelineVerificationState.IsVerified),
        ];
    }

    private static ArchitectureFrameworkParticipation CreateFramework(
        string frameworkId,
        string contract,
        string service,
        string schemaVersion,
        bool participates)
    {
        return new ArchitectureFrameworkParticipation(frameworkId, contract, service, schemaVersion, participates);
    }

    private static IReadOnlyList<ArchitectureBoundaryVerification> CreateTrustBoundaryVerification(ArchitectureValidationContext context)
    {
        var constraints = context.ManifestState.Manifest?.ExecutionConstraints;
        var pbirPrototypeIsDryRunOnly =
            context.PbirPrototypeState.AcceptsExecutionPrototype &&
            context.PbirPrototypeState.Request?.DryRun == true &&
            context.PbirPrototypeState.MockResult is null;

        return
        [
            CreateBoundary("noMicrosoftSkillsExecution", "Microsoft Skills remain selected metadata only.", context.MicrosoftRuntimeState.Context?.MicrosoftSkillSummary.RequiredSkillIds.Count > 0, "microsoftRuntimeContext.skillSummary"),
            CreateBoundary("noProviderInvocation", "Provider invocation remains prohibited.", constraints is not null && !constraints.ProviderInvocationAllowed, "generationManifest.executionConstraints.providerInvocationAllowed=false"),
            CreateBoundary("noMicrosoftApiInvocation", "Microsoft API invocation remains prohibited.", constraints is not null && !constraints.ApiInvocationAllowed, "generationManifest.executionConstraints.apiInvocationAllowed=false"),
            CreateBoundary("noCliInvocation", "CLI invocation remains prohibited.", constraints is not null && !constraints.CliInvocationAllowed, "generationManifest.executionConstraints.cliInvocationAllowed=false"),
            CreateBoundary("noPbirGeneration", "PBIR generation remains outside this architecture phase.", constraints is not null && constraints.DryRunOnly && pbirPrototypeIsDryRunOnly, "pbirExecutionPrototype.dryRunOnly"),
            CreateBoundary("noDeployment", "Deployment remains prohibited.", constraints is not null && !constraints.DeploymentAllowed, "generationManifest.executionConstraints.deploymentAllowed=false"),
            CreateBoundary("noAnalyzerWorkspaceAutomation", "Analyzer Workspace remains a downstream validation authority with no automation in this phase.", true, "architectureCertification.boundary"),
        ];
    }

    private static IReadOnlyList<ArchitectureBoundaryVerification> CreateOwnershipVerification(ArchitectureValidationContext context)
    {
        var approval = context.ManifestState.Manifest?.ApprovalSummary.DesignApproval;

        return
        [
            CreateBoundary("discoveryWizardRecommends", "Discovery Wizard recommendations remain upstream advisory input.", context.Planning.Outcome.References.DesignPackageRef.StartsWith("designPackage:", StringComparison.Ordinal), "planningOutcome.references.designPackageRef"),
            CreateBoundary("designStudioOwnsDesign", "Design Studio owns design and generation approval state.", approval is not null && approval.DesignApprovalRequired && approval.DesignApproved && approval.GenerationApprovalRequired && approval.GenerationApproved, "planningApprovalStatus"),
            CreateBoundary("planningFrameworkOwnsOrchestration", "Planning Framework owns orchestration stage transitions.", context.Planning.OrchestrationState.CurrentStage == PlanningStage.PlanningOutcome, "planningOrchestration.currentStage"),
            CreateBoundary("runtimeFrameworkOwnsExecutionPreparation", "Runtime Framework owns execution preparation metadata only.", context.RuntimeProviderState.Readiness == RuntimeProviderReadinessState.ReadyForRuntimeProvider, "runtimeProvider.readiness"),
            CreateBoundary("analyzerWorkspaceValidationAuthority", "Analyzer Workspace remains validation authority.", approval is not null && approval.AnalyzerValidationRequired, "planningApprovalStatus.analyzerValidationRequired"),
        ];
    }

    private static IReadOnlyList<ArchitectureBoundaryVerification> CreateProviderNeutralityVerification(ArchitectureValidationContext context)
    {
        var manifest = context.ManifestState.Manifest;

        return
        [
            CreateBoundary("microsoftSpecificBehaviorIsolated", "Microsoft-specific behavior remains isolated to Microsoft adapter/runtime/skills layers.", context.Planning.MicrosoftPlanningState is not null && context.MicrosoftRuntimeState.Definition is not null, "microsoft adapter and runtime states"),
            CreateBoundary("generationProvidersInterchangeable", "Generation providers remain selected through provider-neutral generation-provider contracts.", manifest?.CapabilitySummary.SelectedGenerationProvider.ProviderId == context.ProviderState.Provider?.ProviderId, "generationManifest.selectedGenerationProvider"),
            CreateBoundary("runtimeProvidersInterchangeable", "Runtime providers remain selected through runtime-provider abstraction contracts.", manifest?.SourceReferences.RuntimeProviderRef == context.RuntimeProviderState.Request?.RequestId, "generationManifest.sourceReferences.runtimeProviderRef"),
            CreateBoundary("planningContractsProviderNeutral", "Planning contracts remain provider-neutral before Microsoft-specific adapter translation.", context.Planning.GenerationRequestState.Request?.SchemaVersion == GenerationRequestContract.SchemaVersionV1, "generationRequest.schemaVersion"),
        ];
    }

    private static ArchitectureBoundaryVerification CreateBoundary(string boundaryId, string assertion, bool verified, string evidence)
    {
        return new ArchitectureBoundaryVerification(boundaryId, assertion, verified, [evidence]);
    }

    private static ArchitectureValidationDiagnostics CreateDiagnostics(
        ArchitectureValidationContext context,
        IReadOnlyList<ArchitectureFrameworkParticipation> frameworkParticipation,
        IReadOnlyList<ArchitectureBoundaryVerification> trustBoundaryVerification,
        IReadOnlyList<ArchitectureBoundaryVerification> ownershipVerification,
        IReadOnlyList<ArchitectureBoundaryVerification> providerNeutralityVerification)
    {
        var schemaVersionViolations = new List<string>();
        ValidateSchemaVersions(context, schemaVersionViolations);

        var readinessTransitionViolations = new List<string>();
        ValidateReadinessTransitions(context, readinessTransitionViolations);

        var approvalTransitionViolations = new List<string>();
        ValidateApprovalTransitions(context, approvalTransitionViolations);

        var determinismViolations = context.PipelineVerificationState.IsVerified
            ? Array.Empty<string>()
            : new[] { "generationPipelineVerification" };
        var lineageViolations = context.ManifestState.Manifest is not null &&
                                context.ManifestState.Manifest.Lineage.ImmutableUpstreamLineage.Contains(context.Planning.Outcome.References.DesignPackageRef, StringComparer.Ordinal)
            ? Array.Empty<string>()
            : new[] { "generationManifest" };

        return new ArchitectureValidationDiagnostics(
            LayerSeparationViolations: DistinctAndOrder(frameworkParticipation.Where(framework => !framework.Participates).Select(framework => framework.FrameworkId)),
            TrustBoundaryViolations: DistinctAndOrder(trustBoundaryVerification.Where(boundary => !boundary.Verified).Select(boundary => boundary.BoundaryId)),
            OwnershipBoundaryViolations: DistinctAndOrder(ownershipVerification.Where(boundary => !boundary.Verified).Select(boundary => boundary.BoundaryId)),
            ProviderNeutralityViolations: DistinctAndOrder(providerNeutralityVerification.Where(boundary => !boundary.Verified).Select(boundary => boundary.BoundaryId)),
            DeterminismViolations: DistinctAndOrder(determinismViolations),
            LineageViolations: DistinctAndOrder(lineageViolations),
            SchemaVersionViolations: DistinctAndOrder(schemaVersionViolations),
            ReadinessTransitionViolations: DistinctAndOrder(readinessTransitionViolations),
            ApprovalTransitionViolations: DistinctAndOrder(approvalTransitionViolations),
            DeferredArchitectureGaps: []);
    }

    private static void ValidateSchemaVersions(ArchitectureValidationContext context, ICollection<string> violations)
    {
        if (context.Planning.Outcome.Metadata.SchemaVersion != PlanningOutcomeContract.SchemaVersionV1)
        {
            violations.Add("planningOutcome");
        }

        if (context.Planning.OrchestrationState.SchemaVersion != PlanningOrchestrationContract.SchemaVersionV1)
        {
            violations.Add("planningOrchestration");
        }

        if (context.SpecificationState.Specification?.SchemaVersion != PbirGenerationSpecificationContract.SchemaVersionV1)
        {
            violations.Add("pbirGenerationSpecification");
        }

        if (context.ManifestState.Manifest?.Metadata.SchemaVersion != GenerationManifestContract.SchemaVersionV1)
        {
            violations.Add("generationManifest");
        }

        if (context.PipelineVerificationState.Verification?.SchemaVersion != GenerationPipelineVerificationContract.SchemaVersionV1)
        {
            violations.Add("generationPipelineVerification");
        }
    }

    private static void ValidateReadinessTransitions(ArchitectureValidationContext context, ICollection<string> violations)
    {
        if (context.Planning.Outcome.ReadinessSummary.Status != PlanningReadinessStatus.ApprovedForExecutionProvider)
        {
            violations.Add("planningOutcome");
        }

        if (context.RuntimeProviderState.Readiness != RuntimeProviderReadinessState.ReadyForRuntimeProvider)
        {
            violations.Add("runtimeProvider");
        }

        if (context.MicrosoftRuntimeState.Readiness != MicrosoftRuntimeReadinessState.ReadyForMicrosoftRuntimeProvider)
        {
            violations.Add("microsoftRuntimeProvider");
        }

        if (context.ProviderState.Readiness != GenerationProviderReadinessState.ReadyForGenerationProvider)
        {
            violations.Add("generationProvider");
        }

        if (context.ExecutionPlanningState.Readiness != GenerationProviderExecutionPlanReadinessState.ReadyForExecutionProvider)
        {
            violations.Add("generationProviderExecutionPlanning");
        }

        if (context.ManifestState.Readiness != GenerationManifestReadinessState.ReadyForGenerator)
        {
            violations.Add("generationManifest");
        }
    }

    private static void ValidateApprovalTransitions(ArchitectureValidationContext context, ICollection<string> violations)
    {
        var approval = context.ManifestState.Manifest?.ApprovalSummary.DesignApproval;
        if (approval is null)
        {
            violations.Add("designApproval");
            return;
        }

        if (approval.DesignApprovalRequired && !approval.DesignApproved)
        {
            violations.Add("designApproval");
        }

        if (approval.GenerationApprovalRequired && !approval.GenerationApproved)
        {
            violations.Add("generationApproval");
        }

        if (!approval.AnalyzerValidationRequired)
        {
            violations.Add("analyzerValidation");
        }
    }

    private static string[] DistinctAndOrder(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }
}
