using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PlanningOrchestrationService
{
    private readonly DesignPackageConsumptionService _designPackageConsumptionService;
    private readonly GenerationRequestFrameworkService _generationRequestFrameworkService;
    private readonly ExecutionPlanFrameworkService _executionPlanFrameworkService;
    private readonly ProviderAdapterFrameworkService _providerAdapterFrameworkService;
    private readonly MicrosoftAdapterSpecificationService _microsoftAdapterSpecificationService;
    private readonly CapabilityNegotiationService _capabilityNegotiationService;
    private readonly MicrosoftSkillsCapabilityCatalogFrameworkService _microsoftSkillsCapabilityCatalogFrameworkService;
    private readonly MicrosoftSkillProviderAdapterFrameworkService _microsoftSkillProviderAdapterFrameworkService;
    private readonly ExecutionProviderContractFrameworkService _executionProviderContractFrameworkService;
    private readonly PlanningReadinessAggregator _readinessAggregator;

    internal PlanningOrchestrationService()
        : this(
            new DesignPackageConsumptionService(),
            new GenerationRequestFrameworkService(),
            new ExecutionPlanFrameworkService(),
            new ProviderAdapterFrameworkService(new ProviderAdapterRegistry(), new ProviderAdapterCompatibilityService()),
            new MicrosoftAdapterSpecificationService(),
            new CapabilityNegotiationService(),
            new MicrosoftSkillsCapabilityCatalogFrameworkService(),
            new MicrosoftSkillProviderAdapterFrameworkService(),
            new ExecutionProviderContractFrameworkService(),
            new PlanningReadinessAggregator())
    {
    }

    internal PlanningOrchestrationService(
        DesignPackageConsumptionService designPackageConsumptionService,
        GenerationRequestFrameworkService generationRequestFrameworkService,
        ExecutionPlanFrameworkService executionPlanFrameworkService,
        ProviderAdapterFrameworkService providerAdapterFrameworkService,
        MicrosoftAdapterSpecificationService microsoftAdapterSpecificationService,
        CapabilityNegotiationService capabilityNegotiationService,
        MicrosoftSkillsCapabilityCatalogFrameworkService microsoftSkillsCapabilityCatalogFrameworkService,
        MicrosoftSkillProviderAdapterFrameworkService microsoftSkillProviderAdapterFrameworkService,
        ExecutionProviderContractFrameworkService executionProviderContractFrameworkService,
        PlanningReadinessAggregator readinessAggregator)
    {
        _designPackageConsumptionService = designPackageConsumptionService;
        _generationRequestFrameworkService = generationRequestFrameworkService;
        _executionPlanFrameworkService = executionPlanFrameworkService;
        _providerAdapterFrameworkService = providerAdapterFrameworkService;
        _microsoftAdapterSpecificationService = microsoftAdapterSpecificationService;
        _capabilityNegotiationService = capabilityNegotiationService;
        _microsoftSkillsCapabilityCatalogFrameworkService = microsoftSkillsCapabilityCatalogFrameworkService;
        _microsoftSkillProviderAdapterFrameworkService = microsoftSkillProviderAdapterFrameworkService;
        _executionProviderContractFrameworkService = executionProviderContractFrameworkService;
        _readinessAggregator = readinessAggregator;
    }

    internal PlanningOrchestrationResult Orchestrate(
        DesignPackage package,
        PlanningOrchestrationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(package);

        options ??= PlanningOrchestrationOptions.Default;

        var stageHistory = new List<PlanningStageHistoryEntry>
        {
            new(PlanningStage.DesignPackage, PlanningStageStatus.Completed, package.PackageId)
        };
        var transitionHistory = new List<PlanningTransitionRecord>();
        var failures = new List<PlanningFailure>();
        var orchestrationId = $"planningOrchestration:{package.PackageId}";

        var consumptionResult = _designPackageConsumptionService.Consume(package);
        if (!consumptionResult.IsValid)
        {
            failures.AddRange(ToConsumptionFailures(consumptionResult.Diagnostics));
            return BuildResult(
                package,
                consumptionResult,
                new GenerationRequestFrameworkState(null, GenerationRequestReadinessState.Blocked, GenerationRequestValidationDiagnostics.Empty, []),
                new ExecutionPlanFrameworkState(null, null, ExecutionPlanReadinessState.Blocked, ExecutionPlanValidationDiagnostics.Empty),
                new ProviderAdapterFrameworkState(null, null, null, null, null, ProviderAdapterPlanningReadinessState.Incompatible, ProviderAdapterCompatibilityDiagnostics.Empty),
                null,
                null,
                null,
                null,
                null,
                stageHistory,
                transitionHistory,
                failures,
                orchestrationId);
        }

        var generationDraft = _generationRequestFrameworkService.CreateDraft(consumptionResult);
        var generationState = _generationRequestFrameworkService.PrepareForProviderPlanning(generationDraft);
        AddStageTransition(
            PlanningStage.DesignPackage,
            PlanningStage.GenerationRequest,
            new PlanningTransitionContext(package, generationState.Request, null, null, null, null, null, null, null),
            stageHistory,
            transitionHistory,
            failures);
        if (generationState.Request is null || generationState.Readiness != GenerationRequestReadinessState.ReadyForProviderPlanning)
        {
            failures.AddRange(ToGenerationRequestFailures(generationState.Diagnostics));
            return BuildResult(
                package,
                consumptionResult,
                generationState,
                new ExecutionPlanFrameworkState(generationState.Request, null, ExecutionPlanReadinessState.Blocked, ExecutionPlanValidationDiagnostics.Empty),
                new ProviderAdapterFrameworkState(generationState.Request, null, null, null, null, ProviderAdapterPlanningReadinessState.Incompatible, ProviderAdapterCompatibilityDiagnostics.Empty),
                null,
                null,
                null,
                null,
                null,
                stageHistory,
                transitionHistory,
                failures,
                orchestrationId);
        }

        var executionDraft = _executionPlanFrameworkService.CreateDraft(generationState.Request);
        var executionState = _executionPlanFrameworkService.PrepareForProviderAdapter(executionDraft);
        AddStageTransition(
            PlanningStage.GenerationRequest,
            PlanningStage.ExecutionPlan,
            new PlanningTransitionContext(package, generationState.Request, executionState.Plan, null, null, null, null, null, null),
            stageHistory,
            transitionHistory,
            failures);
        if (executionState.Plan is null || executionState.Readiness != ExecutionPlanReadinessState.ReadyForProviderAdapter)
        {
            failures.AddRange(ToExecutionPlanFailures(executionState.Diagnostics));
            return BuildResult(
                package,
                consumptionResult,
                generationState,
                executionState,
                new ProviderAdapterFrameworkState(generationState.Request, executionState.Plan, null, null, null, ProviderAdapterPlanningReadinessState.Incompatible, ProviderAdapterCompatibilityDiagnostics.Empty),
                null,
                null,
                null,
                null,
                null,
                stageHistory,
                transitionHistory,
                failures,
                orchestrationId);
        }

        var adapterDefinition = options.AdapterDefinition ?? CreateDefaultAdapterDefinition();
        var registry = new ProviderAdapterRegistry();
        registry.Register(adapterDefinition);
        var adapterFramework = new ProviderAdapterFrameworkService(registry, new ProviderAdapterCompatibilityService());
        var adapterState = adapterFramework.EvaluateAdapter(adapterDefinition.AdapterId, generationState.Request, executionState.Plan);
        AddStageTransition(
            PlanningStage.ExecutionPlan,
            PlanningStage.ProviderAdapterEvaluation,
            new PlanningTransitionContext(package, generationState.Request, executionState.Plan, adapterState, null, null, null, null, null),
            stageHistory,
            transitionHistory,
            failures);
        if (adapterState.AdapterRequest is null || adapterState.Readiness == ProviderAdapterPlanningReadinessState.Incompatible || adapterState.Readiness == ProviderAdapterPlanningReadinessState.Unsupported)
        {
            failures.AddRange(ToProviderAdapterFailures(adapterState.Diagnostics));
            return BuildResult(
                package,
                consumptionResult,
                generationState,
                executionState,
                adapterState,
                null,
                null,
                null,
                null,
                null,
                stageHistory,
                transitionHistory,
                failures,
                orchestrationId);
        }

        var specification = options.MicrosoftSpecification ?? _microsoftAdapterSpecificationService.CreateDefaultSpecification();
        var microsoftState = _microsoftAdapterSpecificationService.EvaluatePlanning(specification, adapterState.AdapterRequest, executionState.Plan);
        if (microsoftState.Readiness == MicrosoftAdapterPlanningReadinessState.Supported)
        {
            microsoftState = _microsoftAdapterSpecificationService.PrepareForMicrosoftAdapter(microsoftState);
        }

        AddStageTransition(
            PlanningStage.ProviderAdapterEvaluation,
            PlanningStage.MicrosoftPlanningTranslation,
            new PlanningTransitionContext(package, generationState.Request, executionState.Plan, adapterState, microsoftState, null, null, null, null),
            stageHistory,
            transitionHistory,
            failures);
        if (microsoftState.Translation is null || microsoftState.Readiness == MicrosoftAdapterPlanningReadinessState.Unsupported)
        {
            failures.AddRange(ToMicrosoftPlanningFailures(microsoftState.Diagnostics));
            return BuildResult(
                package,
                consumptionResult,
                generationState,
                executionState,
                adapterState,
                microsoftState,
                null,
                null,
                null,
                null,
                stageHistory,
                transitionHistory,
                failures,
                orchestrationId);
        }

        var negotiationState = _capabilityNegotiationService.Negotiate(
            generationState.Request,
            executionState.Plan,
            adapterState.AdapterRequest,
            adapterDefinition,
            specification);
        if (negotiationState.Result is not null && !negotiationState.Diagnostics.HasFailures)
        {
            negotiationState = _capabilityNegotiationService.PrepareForExecutionProvider(negotiationState);
        }

        AddStageTransition(
            PlanningStage.MicrosoftPlanningTranslation,
            PlanningStage.CapabilityNegotiation,
            new PlanningTransitionContext(package, generationState.Request, executionState.Plan, adapterState, microsoftState, negotiationState.Result, null, null, null),
            stageHistory,
            transitionHistory,
            failures);
        if (negotiationState.Result is null || negotiationState.Readiness == CapabilityNegotiationReadinessState.Blocked)
        {
            failures.AddRange(ToCapabilityNegotiationFailures(negotiationState.Diagnostics));
            return BuildResult(
                package,
                consumptionResult,
                generationState,
                executionState,
                adapterState,
                microsoftState,
                negotiationState,
                null,
                null,
                null,
                stageHistory,
                transitionHistory,
                failures,
                orchestrationId);
        }

        var skillCatalogDocument = options.MicrosoftSkillsCatalog ?? _microsoftSkillsCapabilityCatalogFrameworkService.CreateDefaultCatalogDocument();
        var microsoftSkillState = _microsoftSkillsCapabilityCatalogFrameworkService.EvaluatePlanning(negotiationState, skillCatalogDocument);
        if (microsoftSkillState.Readiness == MicrosoftSkillReadinessState.Satisfied)
        {
            microsoftSkillState = _microsoftSkillsCapabilityCatalogFrameworkService.PrepareForSkillProvider(microsoftSkillState);
        }

        AddStageTransition(
            PlanningStage.CapabilityNegotiation,
            PlanningStage.MicrosoftSkillsCatalogResolution,
            new PlanningTransitionContext(package, generationState.Request, executionState.Plan, adapterState, microsoftState, negotiationState.Result, microsoftSkillState, null, null),
            stageHistory,
            transitionHistory,
            failures);
        if (microsoftSkillState.Resolution is null || microsoftSkillState.Readiness != MicrosoftSkillReadinessState.ReadyForSkillProvider)
        {
            failures.AddRange(ToMicrosoftSkillFailures(microsoftSkillState.Validation.Diagnostics));
            return BuildResult(
                package,
                consumptionResult,
                generationState,
                executionState,
                adapterState,
                microsoftState,
                negotiationState,
                microsoftSkillState,
                null,
                null,
                stageHistory,
                transitionHistory,
                failures,
                orchestrationId);
        }

        var microsoftSkillProviderState = _microsoftSkillProviderAdapterFrameworkService.EvaluatePlanning(
            microsoftSkillState,
            options.MicrosoftSkillProviders);
        if (microsoftSkillProviderState.Readiness == MicrosoftSkillProviderReadinessState.Satisfied)
        {
            microsoftSkillProviderState = _microsoftSkillProviderAdapterFrameworkService.PrepareForSkillProviderAdapter(microsoftSkillProviderState);
        }

        AddStageTransition(
            PlanningStage.MicrosoftSkillsCatalogResolution,
            PlanningStage.MicrosoftSkillProviderSelection,
            new PlanningTransitionContext(package, generationState.Request, executionState.Plan, adapterState, microsoftState, negotiationState.Result, microsoftSkillState, microsoftSkillProviderState, null),
            stageHistory,
            transitionHistory,
            failures);
        if (microsoftSkillProviderState.Selection is null || microsoftSkillProviderState.Readiness != MicrosoftSkillProviderReadinessState.ReadyForSkillProviderAdapter)
        {
            failures.AddRange(ToMicrosoftSkillProviderFailures(microsoftSkillProviderState.Validation.Diagnostics));
            return BuildResult(
                package,
                consumptionResult,
                generationState,
                executionState,
                adapterState,
                microsoftState,
                negotiationState,
                microsoftSkillState,
                microsoftSkillProviderState,
                null,
                stageHistory,
                transitionHistory,
                failures,
                orchestrationId);
        }

        var providerDefinition = options.ExecutionProviderDefinition ?? _executionProviderContractFrameworkService.CreateDefaultProviderDefinition();
        var executionProviderState = _executionProviderContractFrameworkService.EvaluateProvider(
            providerDefinition,
            generationState.Request,
            executionState.Plan,
            negotiationState.Result,
            new ExecutionApprovalPolicy(
                DesignApprovalRequired: true,
                GenerationApprovalRequired: true,
                AnalyzerValidationRequired: true,
                DesignApproved: options.DesignApproved,
                GenerationApproved: options.GenerationApproved),
            options.ExecutionProviderMode);
        if (executionProviderState.Eligibility == ExecutionEligibilityStatus.Eligible)
        {
            executionProviderState = _executionProviderContractFrameworkService.PrepareForExecutionProvider(executionProviderState);
        }

        AddStageTransition(
            PlanningStage.MicrosoftSkillProviderSelection,
            PlanningStage.ExecutionProviderEligibility,
            new PlanningTransitionContext(package, generationState.Request, executionState.Plan, adapterState, microsoftState, negotiationState.Result, microsoftSkillState, microsoftSkillProviderState, executionProviderState),
            stageHistory,
            transitionHistory,
            failures);
        failures.AddRange(ToExecutionProviderFailures(executionProviderState.Diagnostics));

        return BuildResult(
            package,
            consumptionResult,
            generationState,
            executionState,
            adapterState,
            microsoftState,
            negotiationState,
            microsoftSkillState,
            microsoftSkillProviderState,
            executionProviderState,
            stageHistory,
            transitionHistory,
            failures,
            orchestrationId);
    }

    internal PlanningTransitionValidationResult ValidateTransition(
        PlanningStage fromStage,
        PlanningStage toStage,
        PlanningTransitionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var invalidTransitions = new List<string>();
        var missingDependencies = new List<string>();
        var invalidReferences = new List<string>();
        var versionMismatches = new List<string>();
        var readinessConflicts = new List<string>();

        if (!IsAllowedTransition(fromStage, toStage))
        {
            invalidTransitions.Add($"{ToToken(fromStage)} -> {ToToken(toStage)}");
            AddRequiredDependenciesForStage(toStage, context, missingDependencies);
        }

        switch (fromStage, toStage)
        {
            case (PlanningStage.DesignPackage, PlanningStage.GenerationRequest):
                if (context.DesignPackage is null)
                {
                    missingDependencies.Add("designPackage");
                }

                if (context.GenerationRequest is null)
                {
                    missingDependencies.Add("generationRequest");
                }
                else
                {
                    ValidateVersion(context.GenerationRequest.SchemaVersion, GenerationRequestContract.SchemaVersionV1, versionMismatches);
                    if (context.DesignPackage is not null &&
                        !string.Equals(context.GenerationRequest.SourceDesignPackageRef, context.DesignPackage.PackageId, StringComparison.Ordinal))
                    {
                        invalidReferences.Add("generationRequest.sourceDesignPackageRef must match designPackage.packageId.");
                    }
                }
                break;

            case (PlanningStage.GenerationRequest, PlanningStage.ExecutionPlan):
                if (context.GenerationRequest is null)
                {
                    missingDependencies.Add("generationRequest");
                }

                if (context.ExecutionPlan is null)
                {
                    missingDependencies.Add("executionPlan");
                }
                else
                {
                    ValidateVersion(context.GenerationRequest?.SchemaVersion, GenerationRequestContract.SchemaVersionV1, versionMismatches);
                    ValidateVersion(context.ExecutionPlan.SchemaVersion, ExecutionPlanContract.SchemaVersionV1, versionMismatches);
                    if (context.GenerationRequest is not null &&
                        !string.Equals(context.ExecutionPlan.SourceReferences.GenerationRequestRef, context.GenerationRequest.RequestId, StringComparison.Ordinal))
                    {
                        invalidReferences.Add("executionPlan.sourceReferences.generationRequestRef must match generationRequest.requestId.");
                    }

                    if (context.DesignPackage is not null &&
                        !string.Equals(context.ExecutionPlan.SourceReferences.SourceDesignPackageRef, context.DesignPackage.PackageId, StringComparison.Ordinal))
                    {
                        invalidReferences.Add("executionPlan.sourceReferences.sourceDesignPackageRef must match designPackage.packageId.");
                    }
                }
                break;

            case (PlanningStage.ExecutionPlan, PlanningStage.ProviderAdapterEvaluation):
                if (context.ExecutionPlan is null)
                {
                    missingDependencies.Add("executionPlan");
                }

                if (context.ProviderAdapterState?.AdapterRequest is null)
                {
                    missingDependencies.Add("providerAdapterRequest");
                }
                else
                {
                    ValidateVersion(context.ProviderAdapterState.AdapterRequest.SchemaVersion, ProviderAdapterContract.SchemaVersionV1, versionMismatches);
                    if (context.GenerationRequest is not null &&
                        !string.Equals(context.ProviderAdapterState.AdapterRequest.GenerationRequestRef, context.GenerationRequest.RequestId, StringComparison.Ordinal))
                    {
                        invalidReferences.Add("providerAdapterRequest.generationRequestRef must match generationRequest.requestId.");
                    }

                    if (context.ExecutionPlan is not null &&
                        !string.Equals(context.ProviderAdapterState.AdapterRequest.ExecutionPlanRef, context.ExecutionPlan.ExecutionPlanId, StringComparison.Ordinal))
                    {
                        invalidReferences.Add("providerAdapterRequest.executionPlanRef must match executionPlan.executionPlanId.");
                    }
                }
                break;

            case (PlanningStage.ProviderAdapterEvaluation, PlanningStage.MicrosoftPlanningTranslation):
                if (context.ProviderAdapterState?.AdapterRequest is null)
                {
                    missingDependencies.Add("providerAdapterRequest");
                }

                if (context.MicrosoftPlanningState?.Translation is null)
                {
                    missingDependencies.Add("microsoftPlanningTranslation");
                }
                else if (context.MicrosoftPlanningState.Readiness == MicrosoftAdapterPlanningReadinessState.Unsupported)
                {
                    readinessConflicts.Add("microsoft planning must not be unsupported.");
                }
                break;

            case (PlanningStage.MicrosoftPlanningTranslation, PlanningStage.CapabilityNegotiation):
                if (context.MicrosoftPlanningState?.Translation is null)
                {
                    missingDependencies.Add("microsoftPlanningTranslation");
                }

                if (context.CapabilityNegotiationResult is null)
                {
                    missingDependencies.Add("capabilityNegotiation");
                }
                else
                {
                    ValidateVersion(context.CapabilityNegotiationResult.SchemaVersion, CapabilityNegotiationContract.SchemaVersionV1, versionMismatches);
                }
                break;

            case (PlanningStage.CapabilityNegotiation, PlanningStage.MicrosoftSkillsCatalogResolution):
                if (context.CapabilityNegotiationResult is null)
                {
                    missingDependencies.Add("capabilityNegotiation");
                }

                if (context.MicrosoftSkillState?.Resolution is null)
                {
                    missingDependencies.Add("microsoftSkillsCatalogResolution");
                }
                else if (context.MicrosoftSkillState.Readiness == MicrosoftSkillReadinessState.Unsupported)
                {
                    readinessConflicts.Add("microsoft skill planning must not be unsupported.");
                }
                break;

            case (PlanningStage.MicrosoftSkillsCatalogResolution, PlanningStage.MicrosoftSkillProviderSelection):
                if (context.MicrosoftSkillState?.Resolution is null)
                {
                    missingDependencies.Add("microsoftSkillsCatalogResolution");
                }

                if (context.MicrosoftSkillProviderState?.Selection is null)
                {
                    missingDependencies.Add("microsoftSkillProviderSelection");
                }
                else if (context.MicrosoftSkillProviderState.Readiness == MicrosoftSkillProviderReadinessState.Unsupported)
                {
                    readinessConflicts.Add("microsoft skill provider planning must not be unsupported.");
                }
                break;

            case (PlanningStage.MicrosoftSkillProviderSelection, PlanningStage.ExecutionProviderEligibility):
                if (context.MicrosoftSkillProviderState?.Selection is null)
                {
                    missingDependencies.Add("microsoftSkillProviderSelection");
                }

                if (context.ExecutionProviderState?.ProviderRequest is null)
                {
                    missingDependencies.Add("executionProviderRequest");
                }
                else
                {
                    ValidateVersion(context.ExecutionProviderState.ProviderRequest.SchemaVersion, ExecutionProviderContract.SchemaVersionV1, versionMismatches);
                    if (context.GenerationRequest is not null &&
                        !string.Equals(context.ExecutionProviderState.ProviderRequest.GenerationRequestRef, context.GenerationRequest.RequestId, StringComparison.Ordinal))
                    {
                        invalidReferences.Add("executionProviderRequest.generationRequestRef must match generationRequest.requestId.");
                    }

                    if (context.ExecutionPlan is not null &&
                        !string.Equals(context.ExecutionProviderState.ProviderRequest.ExecutionPlanRef, context.ExecutionPlan.ExecutionPlanId, StringComparison.Ordinal))
                    {
                        invalidReferences.Add("executionProviderRequest.executionPlanRef must match executionPlan.executionPlanId.");
                    }

                    if (context.CapabilityNegotiationResult is not null &&
                        !string.Equals(context.ExecutionProviderState.ProviderRequest.NegotiationResultRef, context.CapabilityNegotiationResult.NegotiationId, StringComparison.Ordinal))
                    {
                        invalidReferences.Add("executionProviderRequest.negotiationResultRef must match capabilityNegotiation.negotiationId.");
                    }
                }
                break;
        }

        return new PlanningTransitionValidationResult(
            InvalidTransitions: invalidTransitions,
            MissingDependencies: missingDependencies,
            InvalidReferences: invalidReferences,
            VersionMismatches: versionMismatches.Distinct(StringComparer.Ordinal).ToArray(),
            ReadinessConflicts: readinessConflicts);
    }

    internal ProviderAdapterDefinition CreateDefaultAdapterDefinition()
    {
        return new ProviderAdapterDefinition(
            AdapterId: "provider-neutral/layout",
            AdapterName: "Provider Neutral Layout Adapter",
            AdapterVersion: "1.0.0",
            ProviderCategory: ProviderAdapterContract.ProviderNeutralCategory,
            SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile, GenerationRequestContract.FabricDataAppDefaultProfile],
            SupportedCapabilities: ["layoutGeneration", "semanticGeneration"],
            UnsupportedCapabilities: ["artifactGeneration", "validation"],
            SupportedGenerationRequestSchemaVersions: [GenerationRequestContract.SchemaVersionV1],
            SupportedExecutionPlanSchemaVersions: [ExecutionPlanContract.SchemaVersionV1]);
    }

    private static bool IsAllowedTransition(PlanningStage fromStage, PlanningStage toStage)
    {
        return (fromStage, toStage) switch
        {
            (PlanningStage.DesignPackage, PlanningStage.GenerationRequest) => true,
            (PlanningStage.GenerationRequest, PlanningStage.ExecutionPlan) => true,
            (PlanningStage.ExecutionPlan, PlanningStage.ProviderAdapterEvaluation) => true,
            (PlanningStage.ProviderAdapterEvaluation, PlanningStage.MicrosoftPlanningTranslation) => true,
            (PlanningStage.MicrosoftPlanningTranslation, PlanningStage.CapabilityNegotiation) => true,
            (PlanningStage.CapabilityNegotiation, PlanningStage.MicrosoftSkillsCatalogResolution) => true,
            (PlanningStage.MicrosoftSkillsCatalogResolution, PlanningStage.MicrosoftSkillProviderSelection) => true,
            (PlanningStage.MicrosoftSkillProviderSelection, PlanningStage.ExecutionProviderEligibility) => true,
            _ => false,
        };
    }

    private void AddStageTransition(
        PlanningStage fromStage,
        PlanningStage toStage,
        PlanningTransitionContext context,
        ICollection<PlanningStageHistoryEntry> stageHistory,
        ICollection<PlanningTransitionRecord> transitionHistory,
        ICollection<PlanningFailure> failures)
    {
        var validation = ValidateTransition(fromStage, toStage, context);
        if (!validation.IsValid)
        {
            foreach (var failure in ToTransitionFailures(toStage, validation))
            {
                failures.Add(failure);
            }

            stageHistory.Add(new PlanningStageHistoryEntry(toStage, PlanningStageStatus.Failed, ResolveReferenceId(toStage, context)));
            return;
        }

        transitionHistory.Add(new PlanningTransitionRecord(fromStage, toStage, PlanningOrchestrationContract.TransitionRuleVersionV1));
        stageHistory.Add(new PlanningStageHistoryEntry(
            toStage,
            ResolveStageStatus(toStage, context),
            ResolveReferenceId(toStage, context)));
    }

    private PlanningOrchestrationResult BuildResult(
        DesignPackage package,
        DesignPackageConsumptionResult consumptionResult,
        GenerationRequestFrameworkState generationRequestState,
        ExecutionPlanFrameworkState executionPlanState,
        ProviderAdapterFrameworkState providerAdapterState,
        MicrosoftAdapterPlanningState? microsoftPlanningState,
        CapabilityNegotiationFrameworkState? capabilityNegotiationState,
        MicrosoftSkillPlanningState? microsoftSkillState,
        MicrosoftSkillProviderPlanningState? microsoftSkillProviderState,
        ExecutionProviderFrameworkState? executionProviderState,
        IReadOnlyCollection<PlanningStageHistoryEntry> stageHistory,
        IReadOnlyCollection<PlanningTransitionRecord> transitionHistory,
        IReadOnlyList<PlanningFailure> failures,
        string orchestrationId)
    {
        var readinessSummary = _readinessAggregator.Aggregate(capabilityNegotiationState, microsoftSkillProviderState, executionProviderState, failures);
        var outcome = new PlanningOutcome(
            Metadata: new PlanningOutcomeMetadata(
                SchemaVersion: PlanningOutcomeContract.SchemaVersionV1,
                OutcomeId: $"planningOutcome:{package.PackageId}"),
            References: new PlanningOutcomeReferences(
                DesignPackageRef: package.PackageId,
                GenerationRequestRef: generationRequestState.Request?.RequestId ?? string.Empty,
                ExecutionPlanRef: executionPlanState.Plan?.ExecutionPlanId ?? string.Empty,
                NegotiationRef: capabilityNegotiationState?.Result?.NegotiationId ?? string.Empty,
                ExecutionProviderRef: executionProviderState?.ProviderRequest?.RequestId ?? string.Empty),
            Status: DetermineOutcomeStatus(failures, executionProviderState),
            ReadinessSummary: readinessSummary,
            Lineage: BuildLineage(package, microsoftSkillProviderState, executionProviderState),
            Failures: failures
                .Distinct()
                .OrderBy(failure => failure.Stage)
                .ThenBy(failure => failure.Message, StringComparer.Ordinal)
                .ToArray());
        var orchestrationState = new PlanningOrchestrationState(
            SchemaVersion: PlanningOrchestrationContract.SchemaVersionV1,
            OrchestrationId: orchestrationId,
            CurrentStage: PlanningStage.PlanningOutcome,
            StageHistory: stageHistory.ToArray(),
            TransitionHistory: transitionHistory.ToArray());

        return new PlanningOrchestrationResult(
            ConsumptionResult: consumptionResult,
            GenerationRequestState: generationRequestState,
            ExecutionPlanState: executionPlanState,
            ProviderAdapterState: providerAdapterState,
            MicrosoftPlanningState: microsoftPlanningState,
            CapabilityNegotiationState: capabilityNegotiationState,
            MicrosoftSkillState: microsoftSkillState,
            MicrosoftSkillProviderState: microsoftSkillProviderState,
            ExecutionProviderState: executionProviderState,
            OrchestrationState: orchestrationState,
            Outcome: outcome);
    }

    private static PlanningOutcomeLineage BuildLineage(
        DesignPackage package,
        MicrosoftSkillProviderPlanningState? microsoftSkillProviderState,
        ExecutionProviderFrameworkState? executionProviderState)
    {
        var upstreamLineage = (package.Provenance.Lineage ?? [])
            .Select(reference => new PlanningLineageEntry(reference.Stage, reference.ReferenceId, reference.Label))
            .Concat([new PlanningLineageEntry("designPackage", package.PackageId, "Design package")])
            .Distinct()
            .ToArray();
        var planningLineage = new List<PlanningLineageEntry>();

        if (executionProviderState?.GenerationRequest is not null)
        {
            planningLineage.Add(new PlanningLineageEntry("generationRequest", executionProviderState.GenerationRequest.RequestId, "Generation request"));
        }

        if (executionProviderState?.ExecutionPlan is not null)
        {
            planningLineage.Add(new PlanningLineageEntry("executionPlan", executionProviderState.ExecutionPlan.ExecutionPlanId, "Execution plan"));
        }

        if (executionProviderState?.NegotiationResult is not null)
        {
            planningLineage.Add(new PlanningLineageEntry("capabilityNegotiation", executionProviderState.NegotiationResult.NegotiationId, "Capability negotiation"));
        }

        if (microsoftSkillProviderState?.Selection is not null)
        {
            planningLineage.Add(new PlanningLineageEntry("microsoftSkillProviderSelection", microsoftSkillProviderState.Selection.SelectionId, "Microsoft skill provider selection"));
        }

        if (executionProviderState?.ProviderRequest is not null)
        {
            planningLineage.Add(new PlanningLineageEntry("executionProviderEligibility", executionProviderState.ProviderRequest.RequestId, "Execution provider eligibility"));
        }

        var approvalPolicy = executionProviderState?.ApprovalPolicy ?? new ExecutionApprovalPolicy(
            DesignApprovalRequired: true,
            GenerationApprovalRequired: true,
            AnalyzerValidationRequired: true,
            DesignApproved: false,
            GenerationApproved: false);

        return new PlanningOutcomeLineage(
            UpstreamLineage: upstreamLineage,
            PlanningLineage: planningLineage,
            ApprovalLineage: new PlanningApprovalStatus(
                DesignApprovalRequired: approvalPolicy.DesignApprovalRequired,
                GenerationApprovalRequired: approvalPolicy.GenerationApprovalRequired,
                AnalyzerValidationRequired: approvalPolicy.AnalyzerValidationRequired,
                DesignApproved: approvalPolicy.DesignApproved,
                GenerationApproved: approvalPolicy.GenerationApproved));
    }

    private static PlanningOutcomeStatus DetermineOutcomeStatus(
        IReadOnlyList<PlanningFailure> failures,
        ExecutionProviderFrameworkState? executionProviderState)
    {
        if (executionProviderState?.Readiness == ExecutionProviderReadinessState.ApprovedForExecutionProvider &&
            failures.Count == 0)
        {
            return PlanningOutcomeStatus.ApprovedForExecutionProvider;
        }

        if (failures.Any(failure =>
            failure.FailureType == PlanningFailureType.InvalidInput ||
            failure.FailureType == PlanningFailureType.InvalidReference ||
            failure.FailureType == PlanningFailureType.InvalidTransition ||
            failure.FailureType == PlanningFailureType.InvalidVersion ||
            failure.FailureType == PlanningFailureType.MissingDependency ||
            failure.FailureType == PlanningFailureType.ReadinessConflict))
        {
            return PlanningOutcomeStatus.PlanningFailed;
        }

        if (failures.Count > 0 || executionProviderState?.Eligibility is ExecutionEligibilityStatus.Blocked or ExecutionEligibilityStatus.Ineligible)
        {
            return PlanningOutcomeStatus.PlanningBlocked;
        }

        return PlanningOutcomeStatus.PlanningComplete;
    }

    private static PlanningStageStatus ResolveStageStatus(PlanningStage stage, PlanningTransitionContext context)
    {
        return stage switch
        {
            PlanningStage.GenerationRequest when context.GenerationRequest is null => PlanningStageStatus.Blocked,
            PlanningStage.ExecutionPlan when context.ExecutionPlan is null => PlanningStageStatus.Blocked,
            PlanningStage.ProviderAdapterEvaluation when context.ProviderAdapterState?.AdapterRequest is null => PlanningStageStatus.Blocked,
            PlanningStage.MicrosoftPlanningTranslation when context.MicrosoftPlanningState?.Translation is null => PlanningStageStatus.Blocked,
            PlanningStage.CapabilityNegotiation when context.CapabilityNegotiationResult is null => PlanningStageStatus.Blocked,
            PlanningStage.MicrosoftSkillsCatalogResolution when context.MicrosoftSkillState?.Resolution is null => PlanningStageStatus.Blocked,
            PlanningStage.MicrosoftSkillProviderSelection when context.MicrosoftSkillProviderState?.Selection is null => PlanningStageStatus.Blocked,
            PlanningStage.ExecutionProviderEligibility when context.ExecutionProviderState?.ProviderRequest is null => PlanningStageStatus.Blocked,
            _ => PlanningStageStatus.Completed,
        };
    }

    private static string ResolveReferenceId(PlanningStage stage, PlanningTransitionContext context)
    {
        return stage switch
        {
            PlanningStage.DesignPackage => context.DesignPackage?.PackageId ?? string.Empty,
            PlanningStage.GenerationRequest => context.GenerationRequest?.RequestId ?? string.Empty,
            PlanningStage.ExecutionPlan => context.ExecutionPlan?.ExecutionPlanId ?? string.Empty,
            PlanningStage.ProviderAdapterEvaluation => context.ProviderAdapterState?.AdapterRequest?.ExecutionPlanRef ?? string.Empty,
            PlanningStage.MicrosoftPlanningTranslation => context.MicrosoftPlanningState?.Translation?.TargetProfileId ?? string.Empty,
            PlanningStage.CapabilityNegotiation => context.CapabilityNegotiationResult?.NegotiationId ?? string.Empty,
            PlanningStage.MicrosoftSkillsCatalogResolution => context.MicrosoftSkillState?.Resolution?.ResolutionId ?? string.Empty,
            PlanningStage.MicrosoftSkillProviderSelection => context.MicrosoftSkillProviderState?.Selection?.SelectionId ?? string.Empty,
            PlanningStage.ExecutionProviderEligibility => context.ExecutionProviderState?.ProviderRequest?.RequestId ?? string.Empty,
            _ => string.Empty,
        };
    }

    private static string ToToken(PlanningStage stage)
    {
        return stage switch
        {
            PlanningStage.DesignPackage => "designPackage",
            PlanningStage.GenerationRequest => "generationRequest",
            PlanningStage.ExecutionPlan => "executionPlan",
            PlanningStage.ProviderAdapterEvaluation => "providerAdapterEvaluation",
            PlanningStage.MicrosoftPlanningTranslation => "microsoftPlanningTranslation",
            PlanningStage.CapabilityNegotiation => "capabilityNegotiation",
            PlanningStage.MicrosoftSkillsCatalogResolution => "microsoftSkillsCatalogResolution",
            PlanningStage.MicrosoftSkillProviderSelection => "microsoftSkillProviderSelection",
            PlanningStage.ExecutionProviderEligibility => "executionProviderEligibility",
            _ => "planningOutcome",
        };
    }

    private static void AddRequiredDependenciesForStage(
        PlanningStage stage,
        PlanningTransitionContext context,
        ICollection<string> missingDependencies)
    {
        switch (stage)
        {
            case PlanningStage.GenerationRequest:
                if (context.GenerationRequest is null)
                {
                    missingDependencies.Add("generationRequest");
                }
                break;

            case PlanningStage.ExecutionPlan:
                if (context.GenerationRequest is null)
                {
                    missingDependencies.Add("generationRequest");
                }

                if (context.ExecutionPlan is null)
                {
                    missingDependencies.Add("executionPlan");
                }
                break;

            case PlanningStage.ProviderAdapterEvaluation:
                if (context.ExecutionPlan is null)
                {
                    missingDependencies.Add("executionPlan");
                }

                if (context.ProviderAdapterState?.AdapterRequest is null)
                {
                    missingDependencies.Add("providerAdapterRequest");
                }
                break;

            case PlanningStage.MicrosoftPlanningTranslation:
                if (context.ExecutionPlan is null)
                {
                    missingDependencies.Add("executionPlan");
                }

                if (context.ProviderAdapterState?.AdapterRequest is null)
                {
                    missingDependencies.Add("providerAdapterRequest");
                }

                if (context.MicrosoftPlanningState?.Translation is null)
                {
                    missingDependencies.Add("microsoftPlanningTranslation");
                }
                break;

            case PlanningStage.CapabilityNegotiation:
                if (context.ExecutionPlan is null)
                {
                    missingDependencies.Add("executionPlan");
                }

                if (context.ProviderAdapterState?.AdapterRequest is null)
                {
                    missingDependencies.Add("providerAdapterRequest");
                }

                if (context.MicrosoftPlanningState?.Translation is null)
                {
                    missingDependencies.Add("microsoftPlanningTranslation");
                }

                if (context.CapabilityNegotiationResult is null)
                {
                    missingDependencies.Add("capabilityNegotiation");
                }
                break;

            case PlanningStage.ExecutionProviderEligibility:
                if (context.ExecutionPlan is null)
                {
                    missingDependencies.Add("executionPlan");
                }

                if (context.CapabilityNegotiationResult is null)
                {
                    missingDependencies.Add("capabilityNegotiation");
                }

                if (context.MicrosoftSkillProviderState?.Selection is null)
                {
                    missingDependencies.Add("microsoftSkillProviderSelection");
                }

                if (context.ExecutionProviderState?.ProviderRequest is null)
                {
                    missingDependencies.Add("executionProviderRequest");
                }
                break;

            case PlanningStage.MicrosoftSkillsCatalogResolution:
                if (context.CapabilityNegotiationResult is null)
                {
                    missingDependencies.Add("capabilityNegotiation");
                }

                if (context.MicrosoftSkillState?.Resolution is null)
                {
                    missingDependencies.Add("microsoftSkillsCatalogResolution");
                }
                break;

            case PlanningStage.MicrosoftSkillProviderSelection:
                if (context.MicrosoftSkillState?.Resolution is null)
                {
                    missingDependencies.Add("microsoftSkillsCatalogResolution");
                }

                if (context.MicrosoftSkillProviderState?.Selection is null)
                {
                    missingDependencies.Add("microsoftSkillProviderSelection");
                }
                break;
        }
    }

    private static void ValidateVersion(string? candidate, string expected, ICollection<string> versionMismatches)
    {
        if (!string.Equals(candidate, expected, StringComparison.Ordinal))
        {
            versionMismatches.Add(candidate ?? string.Empty);
        }
    }

    private static IReadOnlyList<PlanningFailure> ToConsumptionFailures(DesignPackageConsumptionDiagnostics diagnostics)
    {
        return diagnostics.MissingRequiredFields
            .Select(field => new PlanningFailure(PlanningFailureType.InvalidInput, PlanningStage.DesignPackage, field))
            .Concat(diagnostics.UnsupportedExperienceTypes.Select(target => new PlanningFailure(PlanningFailureType.UnsupportedTarget, PlanningStage.DesignPackage, target)))
            .Concat(diagnostics.IncompatiblePackageStates.Select(state => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.DesignPackage, state)))
            .ToArray();
    }

    private static IReadOnlyList<PlanningFailure> ToGenerationRequestFailures(GenerationRequestValidationDiagnostics diagnostics)
    {
        return diagnostics.MissingRequiredSections
            .Concat(diagnostics.MissingRequiredFields)
            .Concat(diagnostics.MissingInputs)
            .Select(message => new PlanningFailure(PlanningFailureType.InvalidInput, PlanningStage.GenerationRequest, message))
            .Concat(diagnostics.UnsupportedTargetProfiles.Select(target => new PlanningFailure(PlanningFailureType.UnsupportedTarget, PlanningStage.GenerationRequest, target)))
            .Concat(diagnostics.UnsupportedSchemaVersions.Select(version => new PlanningFailure(PlanningFailureType.InvalidVersion, PlanningStage.GenerationRequest, version)))
            .Concat(diagnostics.CompatibilityFailures.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.GenerationRequest, message)))
            .ToArray();
    }

    private static IReadOnlyList<PlanningFailure> ToExecutionPlanFailures(ExecutionPlanValidationDiagnostics diagnostics)
    {
        return diagnostics.MissingRequiredSections
            .Concat(diagnostics.MissingRequiredFields)
            .Select(message => new PlanningFailure(PlanningFailureType.InvalidInput, PlanningStage.ExecutionPlan, message))
            .Concat(diagnostics.UnsupportedSchemaVersions.Select(version => new PlanningFailure(PlanningFailureType.InvalidVersion, PlanningStage.ExecutionPlan, version)))
            .Concat(diagnostics.UnsupportedTargetProfiles.Select(target => new PlanningFailure(PlanningFailureType.UnsupportedTarget, PlanningStage.ExecutionPlan, target)))
            .Concat(diagnostics.DependencyFailures.Select(message => new PlanningFailure(PlanningFailureType.MissingDependency, PlanningStage.ExecutionPlan, message)))
            .Concat(diagnostics.CapabilityInconsistencies.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.ExecutionPlan, message)))
            .Concat(diagnostics.TargetCompatibilityFailures.Select(message => new PlanningFailure(PlanningFailureType.UnsupportedTarget, PlanningStage.ExecutionPlan, message)))
            .Concat(diagnostics.ReviewRequirementFailures.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.ExecutionPlan, message)))
            .ToArray();
    }

    private static IReadOnlyList<PlanningFailure> ToProviderAdapterFailures(ProviderAdapterCompatibilityDiagnostics diagnostics)
    {
        return diagnostics.MissingRequiredSections
            .Concat(diagnostics.MissingRequiredFields)
            .Select(message => new PlanningFailure(PlanningFailureType.InvalidInput, PlanningStage.ProviderAdapterEvaluation, message))
            .Concat(diagnostics.TargetCompatibilityFailures.Select(message => new PlanningFailure(PlanningFailureType.UnsupportedTarget, PlanningStage.ProviderAdapterEvaluation, message)))
            .Concat(diagnostics.CapabilityCompatibilityFailures.Select(message => new PlanningFailure(PlanningFailureType.IncompatibleProvider, PlanningStage.ProviderAdapterEvaluation, message)))
            .Concat(diagnostics.ExecutionPlanCompatibilityFailures.Select(message => new PlanningFailure(PlanningFailureType.MissingDependency, PlanningStage.ProviderAdapterEvaluation, message)))
            .Concat(diagnostics.VersionCompatibilityFailures.Select(message => new PlanningFailure(PlanningFailureType.InvalidVersion, PlanningStage.ProviderAdapterEvaluation, message)))
            .ToArray();
    }

    private static IReadOnlyList<PlanningFailure> ToMicrosoftPlanningFailures(MicrosoftAdapterSpecificationDiagnostics diagnostics)
    {
        return diagnostics.MissingRequiredSections
            .Concat(diagnostics.MissingRequiredFields)
            .Select(message => new PlanningFailure(PlanningFailureType.InvalidInput, PlanningStage.MicrosoftPlanningTranslation, message))
            .Concat(diagnostics.UnsupportedSchemaVersions.Select(message => new PlanningFailure(PlanningFailureType.InvalidVersion, PlanningStage.MicrosoftPlanningTranslation, message)))
            .Concat(diagnostics.UnsupportedTargetProfiles.Select(message => new PlanningFailure(PlanningFailureType.UnsupportedTarget, PlanningStage.MicrosoftPlanningTranslation, message)))
            .Concat(diagnostics.UnsupportedCapabilityRequirements.Select(message => new PlanningFailure(PlanningFailureType.BlockedCapability, PlanningStage.MicrosoftPlanningTranslation, message)))
            .Concat(diagnostics.FutureTargetProfiles.Select(message => new PlanningFailure(PlanningFailureType.BlockedCapability, PlanningStage.MicrosoftPlanningTranslation, message)))
            .Concat(diagnostics.FutureCapabilityRequirements.Select(message => new PlanningFailure(PlanningFailureType.BlockedCapability, PlanningStage.MicrosoftPlanningTranslation, message)))
            .Concat(diagnostics.ConstraintFailures.Select(message => new PlanningFailure(PlanningFailureType.BlockedCapability, PlanningStage.MicrosoftPlanningTranslation, message)))
            .Concat(diagnostics.ReviewRequirementFailures.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.MicrosoftPlanningTranslation, message)))
            .ToArray();
    }

    private static IReadOnlyList<PlanningFailure> ToCapabilityNegotiationFailures(CapabilityNegotiationDiagnostics diagnostics)
    {
        return diagnostics.MissingRequiredSections
            .Concat(diagnostics.MissingRequiredFields)
            .Select(message => new PlanningFailure(PlanningFailureType.InvalidInput, PlanningStage.CapabilityNegotiation, message))
            .Concat(diagnostics.MissingCapabilityDefinitions.Select(message => new PlanningFailure(PlanningFailureType.MissingDependency, PlanningStage.CapabilityNegotiation, message)))
            .Concat(diagnostics.InvalidSubstitutions.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.CapabilityNegotiation, message)))
            .Concat(diagnostics.CircularSubstitutions.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.CapabilityNegotiation, message)))
            .Concat(diagnostics.UnsupportedRequiredCapabilities.Select(message => new PlanningFailure(PlanningFailureType.BlockedCapability, PlanningStage.CapabilityNegotiation, message)))
            .Concat(diagnostics.VersionMismatches.Select(message => new PlanningFailure(PlanningFailureType.InvalidVersion, PlanningStage.CapabilityNegotiation, message)))
            .Concat(diagnostics.CompatibilityFailures.Select(message => new PlanningFailure(PlanningFailureType.IncompatibleProvider, PlanningStage.CapabilityNegotiation, message)))
            .ToArray();
    }

    private static IReadOnlyList<PlanningFailure> ToMicrosoftSkillFailures(MicrosoftSkillCompatibilityDiagnostics diagnostics)
    {
        return diagnostics.MissingRequiredSections
            .Concat(diagnostics.MissingRequiredFields)
            .Select(message => new PlanningFailure(PlanningFailureType.InvalidInput, PlanningStage.MicrosoftSkillsCatalogResolution, message))
            .Concat(diagnostics.DuplicateSkillIds.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.MicrosoftSkillsCatalogResolution, message)))
            .Concat(diagnostics.UnsupportedTargetProfiles.Select(message => new PlanningFailure(PlanningFailureType.UnsupportedTarget, PlanningStage.MicrosoftSkillsCatalogResolution, message)))
            .Concat(diagnostics.UnsupportedCapabilities.Select(message => new PlanningFailure(PlanningFailureType.BlockedCapability, PlanningStage.MicrosoftSkillsCatalogResolution, message)))
            .Concat(diagnostics.UnsatisfiedPrerequisites.Select(message => new PlanningFailure(PlanningFailureType.MissingDependency, PlanningStage.MicrosoftSkillsCatalogResolution, message)))
            .Concat(diagnostics.VersionMismatches.Select(message => new PlanningFailure(PlanningFailureType.InvalidVersion, PlanningStage.MicrosoftSkillsCatalogResolution, message)))
            .Concat(diagnostics.IntegrityFailures.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.MicrosoftSkillsCatalogResolution, message)))
            .ToArray();
    }

    private static IReadOnlyList<PlanningFailure> ToMicrosoftSkillProviderFailures(MicrosoftSkillProviderCompatibilityDiagnostics diagnostics)
    {
        return diagnostics.MissingRequiredSections
            .Concat(diagnostics.MissingRequiredFields)
            .Select(message => new PlanningFailure(PlanningFailureType.InvalidInput, PlanningStage.MicrosoftSkillProviderSelection, message))
            .Concat(diagnostics.DuplicateProviderIds.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.MicrosoftSkillProviderSelection, message)))
            .Concat(diagnostics.UnsupportedTargetProfiles.Select(message => new PlanningFailure(PlanningFailureType.UnsupportedTarget, PlanningStage.MicrosoftSkillProviderSelection, message)))
            .Concat(diagnostics.UnsupportedSkills.Select(message => new PlanningFailure(PlanningFailureType.MissingDependency, PlanningStage.MicrosoftSkillProviderSelection, message)))
            .Concat(diagnostics.UnsupportedCapabilities.Select(message => new PlanningFailure(PlanningFailureType.BlockedCapability, PlanningStage.MicrosoftSkillProviderSelection, message)))
            .Concat(diagnostics.UnsatisfiedPrerequisites.Select(message => new PlanningFailure(PlanningFailureType.MissingDependency, PlanningStage.MicrosoftSkillProviderSelection, message)))
            .Concat(diagnostics.VersionMismatches.Select(message => new PlanningFailure(PlanningFailureType.InvalidVersion, PlanningStage.MicrosoftSkillProviderSelection, message)))
            .Concat(diagnostics.IntegrityFailures.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.MicrosoftSkillProviderSelection, message)))
            .ToArray();
    }

    private static IReadOnlyList<PlanningFailure> ToExecutionProviderFailures(ExecutionProviderDiagnostics diagnostics)
    {
        return diagnostics.MissingRequiredSections
            .Concat(diagnostics.MissingRequiredFields)
            .Select(message => new PlanningFailure(PlanningFailureType.InvalidInput, PlanningStage.ExecutionProviderEligibility, message))
            .Concat(diagnostics.InvalidLineage.Select(message => new PlanningFailure(PlanningFailureType.InvalidReference, PlanningStage.ExecutionProviderEligibility, message)))
            .Concat(diagnostics.InvalidApprovalChains.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.ExecutionProviderEligibility, message)))
            .Concat(diagnostics.UnsupportedProviderDefinitions.Select(message => new PlanningFailure(PlanningFailureType.IncompatibleProvider, PlanningStage.ExecutionProviderEligibility, message)))
            .Concat(diagnostics.IncompatibleExecutionModes.Select(message => new PlanningFailure(PlanningFailureType.IncompatibleProvider, PlanningStage.ExecutionProviderEligibility, message)))
            .Concat(diagnostics.VersionMismatches.Select(message => new PlanningFailure(PlanningFailureType.InvalidVersion, PlanningStage.ExecutionProviderEligibility, message)))
            .Concat(diagnostics.CapabilityRequirementFailures.Select(message => new PlanningFailure(PlanningFailureType.BlockedCapability, PlanningStage.ExecutionProviderEligibility, message)))
            .Concat(diagnostics.ReadinessRequirementFailures.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.ExecutionProviderEligibility, message)))
            .Concat(diagnostics.ApprovalRequirementFailures.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, PlanningStage.ExecutionProviderEligibility, message)))
            .ToArray();
    }

    private static IReadOnlyList<PlanningFailure> ToTransitionFailures(
        PlanningStage stage,
        PlanningTransitionValidationResult validation)
    {
        return validation.InvalidTransitions
            .Select(message => new PlanningFailure(PlanningFailureType.InvalidTransition, stage, message))
            .Concat(validation.MissingDependencies.Select(message => new PlanningFailure(PlanningFailureType.MissingDependency, stage, message)))
            .Concat(validation.InvalidReferences.Select(message => new PlanningFailure(PlanningFailureType.InvalidReference, stage, message)))
            .Concat(validation.VersionMismatches.Select(message => new PlanningFailure(PlanningFailureType.InvalidVersion, stage, message)))
            .Concat(validation.ReadinessConflicts.Select(message => new PlanningFailure(PlanningFailureType.ReadinessConflict, stage, message)))
            .ToArray();
    }
}
