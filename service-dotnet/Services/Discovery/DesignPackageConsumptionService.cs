using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class DesignPackageConsumptionService
{
    internal static IReadOnlyList<DesignPackageFieldConsumptionMetadata> Inventory { get; } =
    [
        new("AnalyticalFlow", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Narrative chain must become normalized analytical intent."),
        new("AnalyticalFlow.Decision", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Decision intent stays part of normalized analytical flow."),
        new("AnalyticalFlow.Evidence", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Evidence intent stays part of normalized analytical flow."),
        new("AnalyticalFlow.Investigation", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Investigation intent stays part of normalized analytical flow."),
        new("AnalyticalFlow.Question", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Question intent stays part of normalized analytical flow."),
        new("Audience", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Audience remains direct planning input."),
        new("Audience.Personas", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Personas enrich downstream audience context but do not block consumption."),
        new("Audience.Personas.Name", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Persona names remain advisory audience context."),
        new("Audience.Personas.Perspective", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Persona perspectives remain advisory audience context."),
        new("Audience.Personas.Role", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Persona roles remain advisory audience context."),
        new("Audience.PrimaryAudience", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Primary audience anchors every generation request."),
        new("Audience.SecondaryAudiences", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Secondary audiences enrich role-specific shaping."),
        new("DiscoveryContext", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Discovery lineage remains authoritative provenance input."),
        new("DiscoveryContext.DiscoveryProfileReference", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Discovery profile lineage remains authoritative provenance input."),
        new("DiscoveryContext.DiscoveryProfileReference.Label", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Discovery profile lineage labels remain authoritative provenance input."),
        new("DiscoveryContext.DiscoveryProfileReference.ReferenceId", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Discovery profile lineage identifiers remain authoritative provenance input."),
        new("DiscoveryContext.DiscoveryProfileReference.Stage", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Discovery profile lineage stages remain authoritative provenance input."),
        new("DiscoveryContext.ExperienceBlueprintReference", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Blueprint lineage remains authoritative provenance input."),
        new("DiscoveryContext.ExperienceBlueprintReference.Label", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Blueprint lineage labels remain authoritative provenance input."),
        new("DiscoveryContext.ExperienceBlueprintReference.ReferenceId", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Blueprint lineage identifiers remain authoritative provenance input."),
        new("DiscoveryContext.ExperienceBlueprintReference.Stage", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Blueprint lineage stages remain authoritative provenance input."),
        new("DiscoveryContext.OpportunityReference", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Opportunity lineage remains authoritative provenance input."),
        new("DiscoveryContext.OpportunityReference.Label", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Opportunity lineage labels remain authoritative provenance input."),
        new("DiscoveryContext.OpportunityReference.ReferenceId", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Opportunity lineage identifiers remain authoritative provenance input."),
        new("DiscoveryContext.OpportunityReference.Stage", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Opportunity lineage stages remain authoritative provenance input."),
        new("DiscoveryContext.RecommendationReference", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Recommendation lineage remains authoritative provenance input."),
        new("DiscoveryContext.RecommendationReference.Label", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Recommendation lineage labels remain authoritative provenance input."),
        new("DiscoveryContext.RecommendationReference.ReferenceId", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Recommendation lineage identifiers remain authoritative provenance input."),
        new("DiscoveryContext.RecommendationReference.Stage", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Recommendation lineage stages remain authoritative provenance input."),
        new("DiscoveryContext.SemanticModelSource", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Semantic-model lineage remains authoritative provenance input."),
        new("DiscoveryContext.SemanticModelSource.Label", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Semantic-model lineage labels remain authoritative provenance input."),
        new("DiscoveryContext.SemanticModelSource.ReferenceId", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Semantic-model lineage identifiers remain authoritative provenance input."),
        new("DiscoveryContext.SemanticModelSource.Stage", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Semantic-model lineage stages remain authoritative provenance input."),
        new("ExperienceDefinition", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Experience definition remains direct planning input until normalized target resolution occurs."),
        new("ExperienceDefinition.BusinessOutcome", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Business outcome remains a direct intent field."),
        new("ExperienceDefinition.BusinessValue", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Ignored, "Business value is a review signal and not execution input."),
        new("ExperienceDefinition.Complexity", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Ignored, "Complexity is review metadata and not direct execution input."),
        new("ExperienceDefinition.Confidence", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Ignored, "Confidence is a trust signal and not direct execution input."),
        new("ExperienceDefinition.ExperienceType", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Experience type resolves the normalized target artifact type."),
        new("Filters", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Filter scope must be preserved explicitly."),
        new("Filters.GlobalFilters", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Global filter scope remains explicit."),
        new("Filters.PageFilters", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Page filter scope remains explicit."),
        new("Filters.PageFilters.Filters", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Page filter values remain explicit."),
        new("Filters.PageFilters.PageName", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Page filter page bindings remain explicit."),
        new("Kpis", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "KPI bindings are required for every supported target."),
        new("Kpis.Grouping", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "KPI groupings remain explicit input."),
        new("Kpis.Name", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "KPI names remain explicit input."),
        new("Kpis.Purpose", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "KPI purposes remain explicit input."),
        new("Navigation", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Navigation becomes normalized structure intent."),
        new("Navigation.Hierarchy", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Navigation hierarchy becomes normalized structure intent."),
        new("Navigation.WorkflowPath", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Workflow path becomes normalized structure intent."),
        new("PackageId", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct, "Package reference remains the upstream source identifier."),
        new("Pages", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Pages become normalized pages or routes."),
        new("Pages.NavigationIntent", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Page navigation intent becomes normalized structure intent."),
        new("Pages.PageName", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Page names become normalized route names."),
        new("Pages.PagePurpose", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Page purpose becomes normalized route purpose."),
        new("ProviderGuidance", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Provider guidance is preserved as advisory package context only."),
        new("ProviderGuidance.ExperienceToGenerate", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Provider guidance remains advisory package context only."),
        new("ProviderGuidance.SuccessLooksLike", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Provider guidance remains advisory package context only."),
        new("ProviderGuidance.WhyThisPackageExists", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Provider guidance remains advisory package context only."),
        new("Provenance", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Provenance becomes normalized lineage input."),
        new("Provenance.Lineage", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Provenance lineage becomes normalized lineage input."),
        new("Provenance.Lineage.Label", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Provenance lineage labels become normalized lineage input."),
        new("Provenance.Lineage.ReferenceId", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Provenance lineage identifiers become normalized lineage input."),
        new("Provenance.Lineage.Stage", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Provenance lineage stages become normalized lineage input."),
        new("Provenance.PackageReference", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Package provenance becomes normalized lineage input."),
        new("RecommendationRationale", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Recommendation rationale stays advisory and auditable."),
        new("RecommendationRationale.AnalyticalFlowRationale", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Recommendation rationale stays advisory and auditable."),
        new("RecommendationRationale.AudienceRationale", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Recommendation rationale stays advisory and auditable."),
        new("RecommendationRationale.BusinessOutcomeRationale", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Recommendation rationale stays advisory and auditable."),
        new("RecommendationRationale.ExperienceTypeRationale", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Recommendation rationale stays advisory and auditable."),
        new("RecommendationRationale.KpiRationale", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Recommendation rationale stays advisory and auditable."),
        new("RecommendationRationale.LimitingFactors", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Recommendation rationale stays advisory and auditable."),
        new("RecommendationRationale.NavigationRationale", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Recommendation rationale stays advisory and auditable."),
        new("RecommendationRationale.PageRationale", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Recommendation rationale stays advisory and auditable."),
        new("RecommendationRationale.RecommendationExplanation", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct, "Recommendation rationale stays advisory and auditable."),
        new("RecommendationRationale.ProvenanceNotes", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Ignored, "Provenance notes are review metadata and not execution input."),
        new("RecommendationRationale.SupportingSemanticSignals", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Ignored, "Supporting semantic signals are audit data and not direct execution input."),
        new("SuccessCriteria", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Success criteria become the normalized success contract."),
        new("SuccessCriteria.AnalyticalSuccessCriteria", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Success criteria become the normalized success contract."),
        new("SuccessCriteria.BusinessSuccessCriteria", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed, "Success criteria become the normalized success contract."),
        new("VisualRecommendations", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Transformed, "Visuals become optional normalized hints rather than hard requirements."),
        new("VisualRecommendations.PageName", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Transformed, "Visual hints become normalized optional structure hints."),
        new("VisualRecommendations.VisualPurpose", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Transformed, "Visual hints become normalized optional structure hints."),
        new("VisualRecommendations.VisualType", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Transformed, "Visual hints become normalized optional structure hints."),
    ];

    private static readonly IReadOnlyList<string> IgnoredFieldPaths = Inventory
        .Where(entry => entry.Handling == DesignPackageConsumptionHandling.Ignored)
        .Select(entry => entry.FieldPath)
        .ToArray();

    internal DesignPackageConsumptionResult Consume(DesignPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);

        var missingRequiredFields = new List<string>();
        var unsupportedExperienceTypes = new List<string>();
        var incompatiblePackageStates = new List<string>();

        ValidateRequiredFields(package, missingRequiredFields);

        var targetArtifactType = ResolveTargetArtifactType(
            package.ExperienceDefinition.ExperienceType,
            unsupportedExperienceTypes);

        ValidateCompatibility(
            package,
            targetArtifactType,
            incompatiblePackageStates);

        var diagnostics = new DesignPackageConsumptionDiagnostics(
            MissingRequiredFields: missingRequiredFields,
            UnsupportedExperienceTypes: unsupportedExperienceTypes,
            IncompatiblePackageStates: incompatiblePackageStates);

        if (diagnostics.HasFailures || targetArtifactType is null)
        {
            return new DesignPackageConsumptionResult(
                ConsumedPackage: null,
                NormalizedGenerationInput: null,
                Diagnostics: diagnostics);
        }

        var consumedPackage = new ConsumedDesignPackageView(
            SourceDesignPackageRef: package.PackageId,
            DiscoveryContext: package.DiscoveryContext,
            PrimaryAudience: package.Audience.PrimaryAudience,
            SecondaryAudiences: package.Audience.SecondaryAudiences ?? [],
            Personas: package.Audience.Personas ?? [],
            ExperienceType: package.ExperienceDefinition.ExperienceType,
            BusinessOutcome: package.ExperienceDefinition.BusinessOutcome,
            Pages: package.Pages ?? [],
            Kpis: package.Kpis ?? [],
            Filters: package.Filters,
            VisualRecommendations: package.VisualRecommendations ?? [],
            Navigation: package.Navigation,
            AnalyticalFlow: package.AnalyticalFlow,
            SuccessCriteria: package.SuccessCriteria,
            RecommendationRationale: package.RecommendationRationale,
            ProviderGuidance: package.ProviderGuidance,
            Provenance: package.Provenance,
            IgnoredFieldPaths: IgnoredFieldPaths);

        var normalizedInput = new NormalizedGenerationInput(
            SourceDesignPackageRef: package.PackageId,
            TargetArtifactType: targetArtifactType.Value,
            SourceExperienceType: package.ExperienceDefinition.ExperienceType,
            PrimaryAudience: package.Audience.PrimaryAudience,
            SecondaryAudiences: package.Audience.SecondaryAudiences ?? [],
            BusinessOutcome: package.ExperienceDefinition.BusinessOutcome,
            PagesOrRoutes: (package.Pages ?? [])
                .Select(page => new NormalizedGenerationPageOrRoute(
                    Name: page.PageName,
                    Purpose: page.PagePurpose,
                    NavigationIntent: page.NavigationIntent))
                .ToArray(),
            NavigationHierarchy: package.Navigation.Hierarchy ?? [],
            WorkflowPath: package.Navigation.WorkflowPath ?? [],
            Kpis: (package.Kpis ?? [])
                .Select(kpi => new NormalizedGenerationKpi(
                    Name: kpi.Name,
                    Purpose: kpi.Purpose,
                    Grouping: kpi.Grouping))
                .ToArray(),
            Filters: new NormalizedGenerationFilters(
                GlobalFilters: package.Filters.GlobalFilters ?? [],
                PageFilters: (package.Filters.PageFilters ?? [])
                    .Select(filter => new NormalizedGenerationPageFilter(
                        PageName: filter.PageName,
                        Filters: filter.Filters ?? []))
                    .ToArray()),
            VisualHints: (package.VisualRecommendations ?? [])
                .Select(recommendation => new NormalizedGenerationVisualHint(
                    PageName: recommendation.PageName,
                    VisualType: recommendation.VisualType,
                    VisualPurpose: recommendation.VisualPurpose))
                .ToArray(),
            AnalyticalFlow: new NormalizedAnalyticalFlow(
                Question: package.AnalyticalFlow.Question,
                Investigation: package.AnalyticalFlow.Investigation,
                Evidence: package.AnalyticalFlow.Evidence,
                Decision: package.AnalyticalFlow.Decision),
            SuccessContract: new NormalizedSuccessContract(
                BusinessSuccessCriteria: package.SuccessCriteria.BusinessSuccessCriteria,
                AnalyticalSuccessCriteria: package.SuccessCriteria.AnalyticalSuccessCriteria,
                ReviewRequired: true,
                ValidationRequired: true),
            Lineage: package.Provenance.Lineage ?? []);

        return new DesignPackageConsumptionResult(
            ConsumedPackage: consumedPackage,
            NormalizedGenerationInput: normalizedInput,
            Diagnostics: diagnostics);
    }

    private static void ValidateRequiredFields(
        DesignPackage package,
        ICollection<string> missingRequiredFields)
    {
        ValidateNotBlank(package.PackageId, "PackageId", missingRequiredFields);

        if (package.DiscoveryContext is null)
        {
            missingRequiredFields.Add("DiscoveryContext");
        }
        else
        {
            ValidateReference(package.DiscoveryContext.SemanticModelSource, "DiscoveryContext.SemanticModelSource", missingRequiredFields);
            ValidateReference(package.DiscoveryContext.DiscoveryProfileReference, "DiscoveryContext.DiscoveryProfileReference", missingRequiredFields);
            ValidateReference(package.DiscoveryContext.OpportunityReference, "DiscoveryContext.OpportunityReference", missingRequiredFields);
            ValidateReference(package.DiscoveryContext.RecommendationReference, "DiscoveryContext.RecommendationReference", missingRequiredFields);
            ValidateReference(package.DiscoveryContext.ExperienceBlueprintReference, "DiscoveryContext.ExperienceBlueprintReference", missingRequiredFields);
        }

        if (package.Audience is null)
        {
            missingRequiredFields.Add("Audience");
        }
        else
        {
            ValidateNotBlank(package.Audience.PrimaryAudience, "Audience.PrimaryAudience", missingRequiredFields);
        }

        if (package.ExperienceDefinition is null)
        {
            missingRequiredFields.Add("ExperienceDefinition");
        }
        else
        {
            ValidateNotBlank(package.ExperienceDefinition.BusinessOutcome, "ExperienceDefinition.BusinessOutcome", missingRequiredFields);
        }

        if (package.Kpis is null || package.Kpis.Count == 0)
        {
            missingRequiredFields.Add("Kpis");
        }
        else
        {
            foreach (var kpi in package.Kpis)
            {
                ValidateNotBlank(kpi.Name, "Kpis.Name", missingRequiredFields);
                ValidateNotBlank(kpi.Purpose, "Kpis.Purpose", missingRequiredFields);
                ValidateNotBlank(kpi.Grouping, "Kpis.Grouping", missingRequiredFields);
            }
        }

        if (package.Filters is null)
        {
            missingRequiredFields.Add("Filters");
        }

        if (package.Navigation is null)
        {
            missingRequiredFields.Add("Navigation");
        }
        else
        {
            if (package.Navigation.Hierarchy is null || package.Navigation.Hierarchy.Count == 0)
            {
                missingRequiredFields.Add("Navigation.Hierarchy");
            }

            if (package.Navigation.WorkflowPath is null || package.Navigation.WorkflowPath.Count == 0)
            {
                missingRequiredFields.Add("Navigation.WorkflowPath");
            }
            else
            {
                foreach (var workflowStep in package.Navigation.WorkflowPath)
                {
                    ValidateNotBlank(workflowStep, "Navigation.WorkflowPath", missingRequiredFields);
                }
            }

            foreach (var hierarchyPage in package.Navigation.Hierarchy ?? [])
            {
                ValidateNotBlank(hierarchyPage, "Navigation.Hierarchy", missingRequiredFields);
            }
        }

        if (package.AnalyticalFlow is null)
        {
            missingRequiredFields.Add("AnalyticalFlow");
        }
        else
        {
            ValidateNotBlank(package.AnalyticalFlow.Question, "AnalyticalFlow.Question", missingRequiredFields);
            ValidateNotBlank(package.AnalyticalFlow.Investigation, "AnalyticalFlow.Investigation", missingRequiredFields);
            ValidateNotBlank(package.AnalyticalFlow.Evidence, "AnalyticalFlow.Evidence", missingRequiredFields);
            ValidateNotBlank(package.AnalyticalFlow.Decision, "AnalyticalFlow.Decision", missingRequiredFields);
        }

        if (package.SuccessCriteria is null)
        {
            missingRequiredFields.Add("SuccessCriteria");
        }
        else
        {
            if (package.SuccessCriteria.BusinessSuccessCriteria is null || package.SuccessCriteria.BusinessSuccessCriteria.Count == 0)
            {
                missingRequiredFields.Add("SuccessCriteria.BusinessSuccessCriteria");
            }

            if (package.SuccessCriteria.AnalyticalSuccessCriteria is null || package.SuccessCriteria.AnalyticalSuccessCriteria.Count == 0)
            {
                missingRequiredFields.Add("SuccessCriteria.AnalyticalSuccessCriteria");
            }

            foreach (var businessCriterion in package.SuccessCriteria.BusinessSuccessCriteria ?? [])
            {
                ValidateNotBlank(businessCriterion, "SuccessCriteria.BusinessSuccessCriteria", missingRequiredFields);
            }

            foreach (var analyticalCriterion in package.SuccessCriteria.AnalyticalSuccessCriteria ?? [])
            {
                ValidateNotBlank(analyticalCriterion, "SuccessCriteria.AnalyticalSuccessCriteria", missingRequiredFields);
            }
        }

        if (package.Provenance is null)
        {
            missingRequiredFields.Add("Provenance");
        }
        else
        {
            ValidateNotBlank(package.Provenance.PackageReference, "Provenance.PackageReference", missingRequiredFields);

            if (package.Provenance.Lineage is null || package.Provenance.Lineage.Count == 0)
            {
                missingRequiredFields.Add("Provenance.Lineage");
            }
        }

        if (package.Pages is not null)
        {
            foreach (var page in package.Pages)
            {
                ValidateNotBlank(page.PageName, "Pages.PageName", missingRequiredFields);
                ValidateNotBlank(page.PagePurpose, "Pages.PagePurpose", missingRequiredFields);
                ValidateNotBlank(page.NavigationIntent, "Pages.NavigationIntent", missingRequiredFields);
            }
        }
    }

    private static GenerationArtifactType? ResolveTargetArtifactType(
        OpportunityExperienceType experienceType,
        ICollection<string> unsupportedExperienceTypes)
    {
        return experienceType switch
        {
            OpportunityExperienceType.PbirReport => GenerationArtifactType.PbirReport,
            OpportunityExperienceType.ExecutiveDashboard => GenerationArtifactType.PbirReport,
            OpportunityExperienceType.OperationalMonitoringExperience => GenerationArtifactType.PbirReport,
            OpportunityExperienceType.AnalyticalInvestigationExperience => GenerationArtifactType.PbirReport,
            OpportunityExperienceType.FabricDataApp => GenerationArtifactType.FabricDataApp,
            OpportunityExperienceType.FabricApp => AddUnsupportedExperienceType(experienceType, unsupportedExperienceTypes),
            _ => AddUnsupportedExperienceType(experienceType, unsupportedExperienceTypes),
        };
    }

    private static GenerationArtifactType? AddUnsupportedExperienceType(
        OpportunityExperienceType experienceType,
        ICollection<string> unsupportedExperienceTypes)
    {
        unsupportedExperienceTypes.Add(experienceType.ToString());
        return null;
    }

    private static void ValidateCompatibility(
        DesignPackage package,
        GenerationArtifactType? targetArtifactType,
        ICollection<string> incompatiblePackageStates)
    {
        var pageNames = (package.Pages ?? [])
            .Where(page => !string.IsNullOrWhiteSpace(page.PageName))
            .Select(page => page.PageName)
            .ToHashSet(StringComparer.Ordinal);

        if (targetArtifactType == GenerationArtifactType.PbirReport && pageNames.Count == 0)
        {
            incompatiblePackageStates.Add("Pages are required for PBIR report generation input.");
        }

        foreach (var navigationPage in package.Navigation?.Hierarchy ?? [])
        {
            if (!pageNames.Contains(navigationPage))
            {
                incompatiblePackageStates.Add($"Navigation.Hierarchy references page '{navigationPage}' that does not exist in Pages.");
            }
        }

        foreach (var pageFilter in package.Filters?.PageFilters ?? [])
        {
            if (!pageNames.Contains(pageFilter.PageName))
            {
                incompatiblePackageStates.Add($"Filters.PageFilters references page '{pageFilter.PageName}' that does not exist in Pages.");
            }
        }

        foreach (var visualRecommendation in package.VisualRecommendations ?? [])
        {
            if (!pageNames.Contains(visualRecommendation.PageName))
            {
                incompatiblePackageStates.Add($"VisualRecommendations references page '{visualRecommendation.PageName}' that does not exist in Pages.");
            }
        }
    }

    private static void ValidateReference(
        DesignPackageReference reference,
        string fieldPath,
        ICollection<string> missingRequiredFields)
    {
        if (reference is null)
        {
            missingRequiredFields.Add(fieldPath);
            return;
        }

        ValidateNotBlank(reference.Stage, $"{fieldPath}.Stage", missingRequiredFields);
        ValidateNotBlank(reference.ReferenceId, $"{fieldPath}.ReferenceId", missingRequiredFields);
        ValidateNotBlank(reference.Label, $"{fieldPath}.Label", missingRequiredFields);
    }

    private static void ValidateNotBlank(
        string? value,
        string fieldPath,
        ICollection<string> missingRequiredFields)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingRequiredFields.Add(fieldPath);
        }
    }
}
