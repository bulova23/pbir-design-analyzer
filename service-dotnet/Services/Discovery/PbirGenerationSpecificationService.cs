using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirGenerationSpecificationService
{
    private readonly PbirGenerationSpecificationValidator _validator;
    private readonly PbirGenerationSpecificationReadinessService _readinessService;

    internal PbirGenerationSpecificationService()
        : this(new PbirGenerationSpecificationValidator(), new PbirGenerationSpecificationReadinessService())
    {
    }

    internal PbirGenerationSpecificationService(
        PbirGenerationSpecificationValidator validator,
        PbirGenerationSpecificationReadinessService readinessService)
    {
        _validator = validator;
        _readinessService = readinessService;
    }

    internal PbirGenerationSpecificationState CreateSpecification(PlanningOrchestrationResult planning)
    {
        ArgumentNullException.ThrowIfNull(planning);

        if (planning.ConsumptionResult.ConsumedPackage is null ||
            planning.GenerationRequestState.Request is null)
        {
            return new PbirGenerationSpecificationState(
                Specification: null,
                Diagnostics: new PbirGenerationSpecificationValidationDiagnostics(
                    MissingRequiredSections: ["planningInputs"],
                    MissingRequiredFields: [],
                    MissingDesignIntent: [],
                    InvalidPageDefinitions: [],
                    InvalidVisualDefinitions: [],
                    InvalidSemanticDefinitions: [],
                    InvalidNavigationDefinitions: [],
                    IncompleteSuccessCriteria: [],
                    UnsupportedSchemaVersions: [],
                    BoundaryViolations: []),
                Readiness: PbirGenerationSpecificationReadinessState.Incomplete,
                AcceptsGenerationProvider: false);
        }

        var consumedPackage = planning.ConsumptionResult.ConsumedPackage;
        var request = planning.GenerationRequestState.Request;
        var designReferences = new PbirGenerationSpecificationDesignReferences(
            DesignPackageReference: consumedPackage.SourceDesignPackageRef,
            GenerationRequestReference: request.RequestId,
            PlanningOutcomeReference: planning.Outcome.Metadata.OutcomeId);

        var specification = new PbirGenerationSpecification(
            SchemaVersion: PbirGenerationSpecificationContract.SchemaVersionV1,
            SpecificationId: $"pbirGenerationSpecification:{planning.Outcome.Metadata.OutcomeId}",
            SourceReferences: new PbirGenerationSpecificationSourceReferences(
                DesignPackageRef: consumedPackage.SourceDesignPackageRef,
                GenerationRequestRef: request.RequestId,
                PlanningOutcomeRef: planning.Outcome.Metadata.OutcomeId,
                Lineage: planning.Outcome.Lineage.UpstreamLineage
                    .Concat(planning.Outcome.Lineage.PlanningLineage)
                    .Distinct()
                    .ToArray()),
            DesignReferences: designReferences,
            ArtifactSpecifications:
            [
                CreateArtifactSpecification(consumedPackage, request, planning.Outcome, designReferences)
            ]);

        var validation = _validator.Validate(specification);
        var readiness = _readinessService.Evaluate(validation);

        return new PbirGenerationSpecificationState(
            Specification: specification,
            Diagnostics: validation.Diagnostics,
            Readiness: readiness,
            AcceptsGenerationProvider: readiness == PbirGenerationSpecificationReadinessState.ReadyForGenerationProvider);
    }

    internal PbirGenerationSpecificationState PrepareForGenerationProvider(PbirGenerationSpecificationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Specification is null)
        {
            return state with
            {
                Readiness = PbirGenerationSpecificationReadinessState.Incomplete,
                AcceptsGenerationProvider = false,
            };
        }

        var validation = _validator.Validate(state.Specification);
        var readiness = _readinessService.Evaluate(validation);
        readiness = _readinessService.PrepareForGenerationProvider(
            readiness,
            state.Specification.ArtifactSpecifications.Count > 0);

        return state with
        {
            Diagnostics = validation.Diagnostics,
            Readiness = readiness,
            AcceptsGenerationProvider = readiness == PbirGenerationSpecificationReadinessState.ReadyForGenerationProvider,
        };
    }

    private static PbirArtifactSpecification CreateArtifactSpecification(
        ConsumedDesignPackageView consumedPackage,
        GenerationRequest request,
        PlanningOutcome outcome,
        PbirGenerationSpecificationDesignReferences designReferences)
    {
        var pageSpecifications = consumedPackage.Pages
            .Select(page => new PbirPageSpecification(
                PageId: page.PageName,
                Purpose: page.PagePurpose,
                Audience: consumedPackage.PrimaryAudience,
                NavigationBehavior: page.NavigationIntent))
            .ToArray();
        var firstPageId = pageSpecifications.FirstOrDefault()?.PageId ?? string.Empty;
        var pageFiltersByPage = consumedPackage.Filters.PageFilters?
            .ToDictionary(filter => filter.PageName, filter => filter.Filters ?? [], StringComparer.Ordinal) ??
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var visualSpecifications = consumedPackage.VisualRecommendations
            .Select((visual, index) => new PbirVisualSpecification(
                PageId: visual.PageName,
                VisualType: visual.VisualType,
                Placement: $"page:{visual.PageName}/slot:{index + 1}",
                IntendedKpi: ResolveVisualKpi(visual, consumedPackage.Kpis),
                IntendedDimensions: "auto",
                IntendedInteractions: ResolveVisualInteractions(visual.PageName, pageFiltersByPage)))
            .ToArray();
        var semanticSpecifications = consumedPackage.Kpis
            .Select((kpi, index) =>
            {
                var pageId = index < pageSpecifications.Length ? pageSpecifications[index].PageId : firstPageId;
                return new PbirSemanticSpecification(
                    PageId: pageId,
                    KpiBinding: kpi.Name,
                    FilterBindings: ResolveSemanticFilters(pageId, consumedPackage.Filters),
                    DrillBehavior: consumedPackage.Navigation.WorkflowPath.Count > 0
                        ? consumedPackage.Navigation.WorkflowPath[Math.Min(index, consumedPackage.Navigation.WorkflowPath.Count - 1)]
                        : "summary",
                    IntendedMeasures: [kpi.Name]);
            })
            .ToArray();
        var pageTransitions = pageSpecifications
            .Zip(pageSpecifications.Skip(1), (fromPage, toPage) => $"{fromPage.PageId}->{toPage.PageId}")
            .ToArray();

        return new PbirArtifactSpecification(
            SchemaVersion: PbirArtifactSpecificationContract.SchemaVersionV1,
            ArtifactSpecificationId: $"pbirArtifactSpecification:{request.RequestId}",
            TargetProfileId: request.TargetArtifactProfile.ProfileId,
            DesignReferences: designReferences,
            PageSpecifications: pageSpecifications,
            VisualSpecifications: visualSpecifications,
            SemanticSpecifications: semanticSpecifications,
            NavigationSpecifications: new PbirNavigationSpecification(
                LandingPage: firstPageId,
                PageTransitions: pageTransitions,
                DrillPaths: consumedPackage.Navigation.WorkflowPath),
            SuccessCriteria: new PbirArtifactSuccessCriteria(
                BusinessSuccessCriteria: consumedPackage.SuccessCriteria.BusinessSuccessCriteria,
                AnalyticalSuccessCriteria: consumedPackage.SuccessCriteria.AnalyticalSuccessCriteria,
                PlanningOutcomeRequirements: BuildPlanningOutcomeRequirements(outcome)));
    }

    private static string ResolveVisualKpi(
        DesignPackageVisualRecommendation visual,
        IReadOnlyList<DesignPackageKpi> kpis)
    {
        var matchingKpi = kpis.FirstOrDefault(kpi =>
            visual.VisualPurpose.Contains(kpi.Name, StringComparison.OrdinalIgnoreCase) ||
            visual.VisualPurpose.Contains(kpi.Grouping, StringComparison.OrdinalIgnoreCase));

        return matchingKpi?.Name ?? kpis.FirstOrDefault()?.Name ?? string.Empty;
    }

    private static IReadOnlyList<string> ResolveVisualInteractions(
        string pageName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> pageFiltersByPage)
    {
        var interactions = new List<string> { "crossFilter" };

        if (pageFiltersByPage.TryGetValue(pageName, out var pageFilters) && pageFilters.Count > 0)
        {
            interactions.AddRange(pageFilters.Select(filter => $"pageFilter:{filter}"));
        }

        return interactions
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> ResolveSemanticFilters(
        string pageId,
        DesignPackageFilterSet filters)
    {
        var pageFilters = filters.PageFilters?
            .FirstOrDefault(filter => string.Equals(filter.PageName, pageId, StringComparison.Ordinal))?
            .Filters ?? [];

        return filters.GlobalFilters
            .Concat(pageFilters)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> BuildPlanningOutcomeRequirements(PlanningOutcome outcome)
    {
        var requirements = new List<string>
        {
            $"planningStatus:{outcome.Status}",
            $"planningReadiness:{outcome.ReadinessSummary.Status}",
            $"executionProviderReadiness:{outcome.ReadinessSummary.ExecutionProviderReadiness}",
        };

        requirements.AddRange(outcome.ReadinessSummary.UnresolvedRequirements.Select(requirement => $"unresolved:{requirement}"));

        return requirements
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
