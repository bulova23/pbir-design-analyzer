using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationRequestService
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
                ArtifactType: MapArtifactType(normalized.TargetArtifactType)),
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
                    AdapterFamily: "providerNeutral",
                    ExecutionMode: "promptSegmentsOnly",
                    ProviderSpecificExecution: false)),
            ReviewPolicy: new GenerationRequestReviewPolicy(
                DesignApprovalRequired: true,
                GenerationApprovalRequired: true,
                AnalyzerReviewRequired: true));

        var validation = Validate(request);
        return validation.IsValid
            ? new GenerationRequestCreationResult(request, validation.Diagnostics)
            : new GenerationRequestCreationResult(null, validation.Diagnostics);
    }

    internal GenerationRequestValidationResult Validate(GenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var missingSections = new List<string>();
        var missingFields = new List<string>();
        var missingInputs = new List<string>();
        var unsupportedTargets = new List<string>();
        var unsupportedSchemaVersions = new List<string>();
        var compatibilityFailures = new List<string>();

        ValidateNotBlank(request.SchemaVersion, "schemaVersion", missingFields);
        if (!string.IsNullOrWhiteSpace(request.SchemaVersion) &&
            !string.Equals(request.SchemaVersion, GenerationRequestContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            unsupportedSchemaVersions.Add(request.SchemaVersion);
        }

        ValidateNotBlank(request.RequestId, "requestId", missingFields);
        ValidateNotBlank(request.SourceDesignPackageRef, "sourceDesignPackageRef", missingFields);

        if (request.TargetArtifactProfile is null)
        {
            missingSections.Add("targetArtifactProfile");
        }
        else if (!IsSupportedArtifactType(request.TargetArtifactProfile.ArtifactType))
        {
            unsupportedTargets.Add(ToContractValue(request.TargetArtifactProfile.ArtifactType));
        }

        if (request.GenerationMode is null)
        {
            missingSections.Add("generationMode");
        }
        else
        {
            ValidateNotBlank(request.GenerationMode.Authority, "generationMode.authority", missingFields);
            if (!request.GenerationMode.ReviewRequired)
            {
                compatibilityFailures.Add("generationMode.reviewRequired must stay true because generation remains review-gated.");
            }
        }

        if (request.DesignIntent is null)
        {
            missingSections.Add("designIntent");
        }
        else
        {
            ValidateNotBlank(request.DesignIntent.PrimaryAudience, "designIntent.primaryAudience", missingFields);
            ValidateNotBlank(request.DesignIntent.BusinessOutcome, "designIntent.businessOutcome", missingFields);

            if (request.DesignIntent.AnalyticalFlow is null)
            {
                missingSections.Add("designIntent.analyticalFlow");
            }
            else
            {
                ValidateNotBlank(request.DesignIntent.AnalyticalFlow.Question, "designIntent.analyticalFlow.question", missingFields);
                ValidateNotBlank(request.DesignIntent.AnalyticalFlow.Investigation, "designIntent.analyticalFlow.investigation", missingFields);
                ValidateNotBlank(request.DesignIntent.AnalyticalFlow.Evidence, "designIntent.analyticalFlow.evidence", missingFields);
                ValidateNotBlank(request.DesignIntent.AnalyticalFlow.Decision, "designIntent.analyticalFlow.decision", missingFields);
            }
        }

        if (request.StructuralIntent is null)
        {
            missingSections.Add("structuralIntent");
        }
        else
        {
            if (request.StructuralIntent.Pages is null || request.StructuralIntent.Pages.Count == 0)
            {
                missingSections.Add("structuralIntent.pages");
            }
            else
            {
                foreach (var page in request.StructuralIntent.Pages)
                {
                    ValidateNotBlank(page.Name, "structuralIntent.pages.name", missingFields);
                    ValidateNotBlank(page.Purpose, "structuralIntent.pages.purpose", missingFields);
                    ValidateNotBlank(page.NavigationIntent, "structuralIntent.pages.navigationIntent", missingFields);
                }
            }

            if (request.StructuralIntent.Navigation is null)
            {
                missingSections.Add("structuralIntent.navigation");
            }
            else
            {
                if (request.StructuralIntent.Navigation.Hierarchy is null || request.StructuralIntent.Navigation.Hierarchy.Count == 0)
                {
                    missingSections.Add("structuralIntent.navigation.hierarchy");
                }

                if (request.StructuralIntent.Navigation.WorkflowPath is null || request.StructuralIntent.Navigation.WorkflowPath.Count == 0)
                {
                    missingSections.Add("structuralIntent.navigation.workflowPath");
                }
            }
        }

        if (request.DataIntent is null)
        {
            missingSections.Add("dataIntent");
        }
        else
        {
            if (request.DataIntent.Kpis is null || request.DataIntent.Kpis.Count == 0)
            {
                missingSections.Add("dataIntent.kpis");
            }
            else
            {
                foreach (var kpi in request.DataIntent.Kpis)
                {
                    ValidateNotBlank(kpi.Name, "dataIntent.kpis.name", missingFields);
                    ValidateNotBlank(kpi.Purpose, "dataIntent.kpis.purpose", missingFields);
                    ValidateNotBlank(kpi.Grouping, "dataIntent.kpis.grouping", missingFields);
                }
            }

            if (request.DataIntent.Filters is null)
            {
                missingSections.Add("dataIntent.filters");
            }

            if (request.DataIntent.SemanticBinding is null)
            {
                missingSections.Add("dataIntent.semanticBinding");
            }
            else
            {
                ValidateNotBlank(request.DataIntent.SemanticBinding.SemanticModelRef, "dataIntent.semanticBinding.semanticModelRef", missingFields);
                ValidateNotBlank(request.DataIntent.SemanticBinding.SemanticModelLabel, "dataIntent.semanticBinding.semanticModelLabel", missingFields);
            }
        }

        if (request.SuccessContract is null)
        {
            missingSections.Add("successContract");
        }
        else
        {
            if (request.SuccessContract.BusinessSuccessCriteria is null || request.SuccessContract.BusinessSuccessCriteria.Count == 0)
            {
                missingSections.Add("successContract.businessSuccessCriteria");
            }

            if (request.SuccessContract.AnalyticalSuccessCriteria is null || request.SuccessContract.AnalyticalSuccessCriteria.Count == 0)
            {
                missingSections.Add("successContract.analyticalSuccessCriteria");
            }

            if (request.SuccessContract.ValidationRequirements is null || request.SuccessContract.ValidationRequirements.Count == 0)
            {
                missingSections.Add("successContract.validationRequirements");
            }
        }

        if (request.Provenance is null)
        {
            missingSections.Add("provenance");
        }
        else
        {
            ValidateNotBlank(request.Provenance.SourceDesignPackageRef, "provenance.sourceDesignPackageRef", missingFields);
            if (request.Provenance.Lineage is null || request.Provenance.Lineage.Count == 0)
            {
                missingSections.Add("provenance.lineage");
            }

            if (request.Provenance.AdapterMetadata is null)
            {
                missingSections.Add("provenance.adapterMetadata");
            }
        }

        if (request.ReviewPolicy is null)
        {
            missingSections.Add("reviewPolicy");
        }
        else
        {
            if (!request.ReviewPolicy.DesignApprovalRequired)
            {
                compatibilityFailures.Add("reviewPolicy.designApprovalRequired must stay true.");
            }

            if (!request.ReviewPolicy.GenerationApprovalRequired)
            {
                compatibilityFailures.Add("reviewPolicy.generationApprovalRequired must stay true.");
            }

            if (!request.ReviewPolicy.AnalyzerReviewRequired)
            {
                compatibilityFailures.Add("reviewPolicy.analyzerReviewRequired must stay true.");
            }
        }

        if (request.Provenance is not null &&
            !string.IsNullOrWhiteSpace(request.SourceDesignPackageRef) &&
            !string.IsNullOrWhiteSpace(request.Provenance.SourceDesignPackageRef) &&
            !string.Equals(request.SourceDesignPackageRef, request.Provenance.SourceDesignPackageRef, StringComparison.Ordinal))
        {
            compatibilityFailures.Add("provenance.sourceDesignPackageRef must match sourceDesignPackageRef.");
        }

        if (request.DataIntent?.SemanticBinding is not null &&
            request.Provenance?.Lineage is not null &&
            request.Provenance.Lineage.All(reference => !string.Equals(reference.ReferenceId, request.DataIntent.SemanticBinding.SemanticModelRef, StringComparison.Ordinal)))
        {
            compatibilityFailures.Add("dataIntent.semanticBinding.semanticModelRef must resolve from provenance.lineage.");
        }

        var diagnostics = new GenerationRequestValidationDiagnostics(
            MissingRequiredSections: missingSections.Distinct(StringComparer.Ordinal).ToArray(),
            MissingRequiredFields: missingFields.Distinct(StringComparer.Ordinal).ToArray(),
            MissingInputs: missingInputs.Distinct(StringComparer.Ordinal).ToArray(),
            UnsupportedTargetProfiles: unsupportedTargets.Distinct(StringComparer.Ordinal).ToArray(),
            UnsupportedSchemaVersions: unsupportedSchemaVersions.Distinct(StringComparer.Ordinal).ToArray(),
            CompatibilityFailures: compatibilityFailures.Distinct(StringComparer.Ordinal).ToArray());

        return new GenerationRequestValidationResult(diagnostics);
    }

    internal IReadOnlyList<GenerationRequestPromptSegment> BuildPromptSegments(GenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validate(request);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("Prompt segments can only be generated from a valid generation request.");
        }

        return
        [
            new GenerationRequestPromptSegment(
                1,
                "Target Summary",
                string.Join(Environment.NewLine,
                [
                    $"Target artifact profile: {ToContractValue(request.TargetArtifactProfile.ArtifactType)}",
                    $"Schema version: {request.SchemaVersion}",
                    $"Request id: {request.RequestId}",
                    $"Source design package: {request.SourceDesignPackageRef}",
                ])),
            new GenerationRequestPromptSegment(
                2,
                "Audience Summary",
                string.Join(Environment.NewLine,
                [
                    $"Primary audience: {request.DesignIntent.PrimaryAudience}",
                    $"Secondary audiences: {FormatList(request.DesignIntent.SecondaryAudiences)}",
                ])),
            new GenerationRequestPromptSegment(
                3,
                "Business Outcome",
                string.Join(Environment.NewLine,
                [
                    $"Business outcome: {request.DesignIntent.BusinessOutcome}",
                    $"Question: {request.DesignIntent.AnalyticalFlow.Question}",
                    $"Investigation: {request.DesignIntent.AnalyticalFlow.Investigation}",
                    $"Evidence: {request.DesignIntent.AnalyticalFlow.Evidence}",
                    $"Decision: {request.DesignIntent.AnalyticalFlow.Decision}",
                ])),
            new GenerationRequestPromptSegment(
                4,
                "Structural Intent",
                string.Join(Environment.NewLine,
                [
                    "Pages:",
                    .. request.StructuralIntent.Pages.Select(page => $"- {page.Name}: {page.Purpose} [{page.NavigationIntent}]"),
                    "Visual hints:",
                    .. request.StructuralIntent.VisualHints.Select(hint => $"- {hint.PageName}: {hint.VisualType} ({hint.VisualPurpose})"),
                ])),
            new GenerationRequestPromptSegment(
                5,
                "Data Intent",
                string.Join(Environment.NewLine,
                [
                    "KPIs:",
                    .. request.DataIntent.Kpis.Select(kpi => $"- {kpi.Name}: {kpi.Purpose} [{kpi.Grouping}]"),
                    $"Global filters: {FormatList(request.DataIntent.Filters.GlobalFilters)}",
                    "Page filters:",
                    .. request.DataIntent.Filters.PageFilters.Select(filter => $"- {filter.PageName}: {FormatList(filter.Filters)}"),
                    $"Semantic binding: {request.DataIntent.SemanticBinding.SemanticModelRef} ({request.DataIntent.SemanticBinding.SemanticModelLabel})",
                ])),
            new GenerationRequestPromptSegment(
                6,
                "Navigation Intent",
                string.Join(Environment.NewLine,
                [
                    $"Hierarchy: {FormatArrowList(request.StructuralIntent.Navigation.Hierarchy)}",
                    $"Workflow path: {FormatArrowList(request.StructuralIntent.Navigation.WorkflowPath)}",
                ])),
            new GenerationRequestPromptSegment(
                7,
                "Success Criteria",
                string.Join(Environment.NewLine,
                [
                    "Business success criteria:",
                    .. request.SuccessContract.BusinessSuccessCriteria.Select(item => $"- {item}"),
                    "Analytical success criteria:",
                    .. request.SuccessContract.AnalyticalSuccessCriteria.Select(item => $"- {item}"),
                    "Validation requirements:",
                    .. request.SuccessContract.ValidationRequirements.Select(item => $"- {item}"),
                ])),
            new GenerationRequestPromptSegment(
                8,
                "Constraints",
                string.Join(Environment.NewLine,
                [
                    $"Authority: {request.GenerationMode.Authority}",
                    $"Review required: {request.GenerationMode.ReviewRequired}",
                    $"Allow partial output: {request.GenerationMode.AllowPartialOutput}",
                    $"Design approval required: {request.ReviewPolicy.DesignApprovalRequired}",
                    $"Generation approval required: {request.ReviewPolicy.GenerationApprovalRequired}",
                    $"Analyzer review required: {request.ReviewPolicy.AnalyzerReviewRequired}",
                    "Do not infer unsupported target semantics or introduce provider-specific behavior.",
                ])),
        ];
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

    private static string MapUnsupportedExperienceType(string experienceType)
    {
        return string.Equals(experienceType, OpportunityExperienceType.FabricApp.ToString(), StringComparison.Ordinal)
            ? ToContractValue(GenerationRequestArtifactType.FabricApp)
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
        return $"genreq:{ToContractValue(MapArtifactType(artifactType))}:{sourceDesignPackageRef}";
    }

    private static bool IsSupportedArtifactType(GenerationRequestArtifactType artifactType)
    {
        return artifactType is GenerationRequestArtifactType.PbirReport or GenerationRequestArtifactType.FabricDataApp;
    }

    private static string ToContractValue(GenerationRequestArtifactType artifactType)
    {
        return artifactType switch
        {
            GenerationRequestArtifactType.PbirReport => "pbirReport",
            GenerationRequestArtifactType.FabricDataApp => "fabricDataApp",
            GenerationRequestArtifactType.FabricApp => "fabricApp",
            _ => throw new ArgumentOutOfRangeException(nameof(artifactType), artifactType, "Unsupported generation request artifact type."),
        };
    }

    private static string FormatList(IReadOnlyList<string> values)
    {
        return values.Count == 0 ? "none" : string.Join(", ", values);
    }

    private static string FormatArrowList(IReadOnlyList<string> values)
    {
        return values.Count == 0 ? "none" : string.Join(" -> ", values);
    }

    private static void ValidateNotBlank(string? value, string fieldPath, ICollection<string> missingFields)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingFields.Add(fieldPath);
        }
    }
}
