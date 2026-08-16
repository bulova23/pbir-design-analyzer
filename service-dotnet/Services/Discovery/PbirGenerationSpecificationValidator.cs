using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirGenerationSpecificationValidator
{
    internal PbirGenerationSpecificationValidationResult Validate(PbirGenerationSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var missingRequiredSections = new List<string>();
        var missingRequiredFields = new List<string>();
        var missingDesignIntent = new List<string>();
        var invalidPageDefinitions = new List<string>();
        var invalidVisualDefinitions = new List<string>();
        var invalidSemanticDefinitions = new List<string>();
        var invalidNavigationDefinitions = new List<string>();
        var incompleteSuccessCriteria = new List<string>();
        var unsupportedSchemaVersions = new List<string>();
        var boundaryViolations = new List<string>();

        ValidateNotBlank(specification.SpecificationId, "specificationId", missingRequiredFields);
        ValidateNotBlank(specification.SourceReferences.DesignPackageRef, "sourceReferences.designPackageRef", missingRequiredFields);
        ValidateNotBlank(specification.SourceReferences.GenerationRequestRef, "sourceReferences.generationRequestRef", missingRequiredFields);
        ValidateNotBlank(specification.SourceReferences.PlanningOutcomeRef, "sourceReferences.planningOutcomeRef", missingRequiredFields);
        ValidateNotBlank(specification.DesignReferences.DesignPackageReference, "designReferences.designPackageReference", missingRequiredFields);
        ValidateNotBlank(specification.DesignReferences.GenerationRequestReference, "designReferences.generationRequestReference", missingRequiredFields);
        ValidateNotBlank(specification.DesignReferences.PlanningOutcomeReference, "designReferences.planningOutcomeReference", missingRequiredFields);

        ValidateSchemaVersion(specification.SchemaVersion, PbirGenerationSpecificationContract.SchemaVersionV1, unsupportedSchemaVersions);

        if (specification.ArtifactSpecifications.Count == 0)
        {
            missingRequiredSections.Add("artifactSpecifications");
        }

        foreach (var artifact in specification.ArtifactSpecifications)
        {
            ValidateArtifact(
                artifact,
                specification.DesignReferences,
                missingRequiredSections,
                missingRequiredFields,
                missingDesignIntent,
                invalidPageDefinitions,
                invalidVisualDefinitions,
                invalidSemanticDefinitions,
                invalidNavigationDefinitions,
                incompleteSuccessCriteria,
                unsupportedSchemaVersions,
                boundaryViolations);
        }

        return new PbirGenerationSpecificationValidationResult(
            new PbirGenerationSpecificationValidationDiagnostics(
                MissingRequiredSections: missingRequiredSections.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                MissingRequiredFields: missingRequiredFields.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                MissingDesignIntent: missingDesignIntent.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                InvalidPageDefinitions: invalidPageDefinitions.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                InvalidVisualDefinitions: invalidVisualDefinitions.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                InvalidSemanticDefinitions: invalidSemanticDefinitions.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                InvalidNavigationDefinitions: invalidNavigationDefinitions.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                IncompleteSuccessCriteria: incompleteSuccessCriteria.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                UnsupportedSchemaVersions: unsupportedSchemaVersions.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                BoundaryViolations: boundaryViolations.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray()));
    }

    private static void ValidateArtifact(
        PbirArtifactSpecification artifact,
        PbirGenerationSpecificationDesignReferences designReferences,
        ICollection<string> missingRequiredSections,
        ICollection<string> missingRequiredFields,
        ICollection<string> missingDesignIntent,
        ICollection<string> invalidPageDefinitions,
        ICollection<string> invalidVisualDefinitions,
        ICollection<string> invalidSemanticDefinitions,
        ICollection<string> invalidNavigationDefinitions,
        ICollection<string> incompleteSuccessCriteria,
        ICollection<string> unsupportedSchemaVersions,
        ICollection<string> boundaryViolations)
    {
        ValidateSchemaVersion(artifact.SchemaVersion, PbirArtifactSpecificationContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateNotBlank(artifact.ArtifactSpecificationId, "artifactSpecifications.artifactSpecificationId", missingRequiredFields);
        ValidateNotBlank(artifact.TargetProfileId, "artifactSpecifications.targetProfileId", missingRequiredFields);

        if (!string.Equals(artifact.TargetProfileId, GenerationRequestContract.PbirReportDefaultProfile, StringComparison.Ordinal))
        {
            boundaryViolations.Add("artifactSpecifications.targetProfileId must remain pbirReport/default in Phase 15.");
        }

        if (!string.Equals(artifact.DesignReferences.DesignPackageReference, designReferences.DesignPackageReference, StringComparison.Ordinal) ||
            !string.Equals(artifact.DesignReferences.GenerationRequestReference, designReferences.GenerationRequestReference, StringComparison.Ordinal) ||
            !string.Equals(artifact.DesignReferences.PlanningOutcomeReference, designReferences.PlanningOutcomeReference, StringComparison.Ordinal))
        {
            boundaryViolations.Add("artifactSpecifications.designReferences must match the generation specification design references.");
        }

        if (artifact.PageSpecifications.Count == 0)
        {
            missingRequiredSections.Add("artifactSpecifications.pageSpecifications");
        }

        if (artifact.VisualSpecifications.Count == 0)
        {
            missingRequiredSections.Add("artifactSpecifications.visualSpecifications");
        }

        if (artifact.SemanticSpecifications.Count == 0)
        {
            missingRequiredSections.Add("artifactSpecifications.semanticSpecifications");
        }

        var knownPages = artifact.PageSpecifications
            .Select(page => page.PageId)
            .Where(pageId => !string.IsNullOrWhiteSpace(pageId))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var page in artifact.PageSpecifications)
        {
            ValidateNotBlank(page.PageId, "artifactSpecifications.pageSpecifications.pageId", invalidPageDefinitions);
            ValidateNotBlank(page.Purpose, "artifactSpecifications.pageSpecifications.purpose", missingDesignIntent);
            ValidateNotBlank(page.Audience, "artifactSpecifications.pageSpecifications.audience", missingDesignIntent);
            ValidateNotBlank(page.NavigationBehavior, "artifactSpecifications.pageSpecifications.navigationBehavior", invalidPageDefinitions);
        }

        foreach (var visual in artifact.VisualSpecifications)
        {
            ValidateNotBlank(visual.PageId, "artifactSpecifications.visualSpecifications.pageId", invalidVisualDefinitions);
            ValidateNotBlank(visual.VisualType, "artifactSpecifications.visualSpecifications.visualType", invalidVisualDefinitions);
            ValidateNotBlank(visual.Placement, "artifactSpecifications.visualSpecifications.placement", invalidVisualDefinitions);
            ValidateNotBlank(visual.IntendedKpi, "artifactSpecifications.visualSpecifications.intendedKpi", missingDesignIntent);
            ValidateNotBlank(visual.IntendedDimensions, "artifactSpecifications.visualSpecifications.intendedDimensions", invalidVisualDefinitions);

            if (!knownPages.Contains(visual.PageId))
            {
                invalidVisualDefinitions.Add("visual.pageId must match a declared page.");
            }
        }

        foreach (var semantic in artifact.SemanticSpecifications)
        {
            ValidateNotBlank(semantic.PageId, "artifactSpecifications.semanticSpecifications.pageId", invalidSemanticDefinitions);
            ValidateNotBlank(semantic.KpiBinding, "artifactSpecifications.semanticSpecifications.kpiBinding", missingDesignIntent);
            ValidateNotBlank(semantic.DrillBehavior, "artifactSpecifications.semanticSpecifications.drillBehavior", invalidSemanticDefinitions);

            if (!knownPages.Contains(semantic.PageId))
            {
                invalidSemanticDefinitions.Add("semantic.pageId must match a declared page.");
            }

            if (semantic.FilterBindings.Count == 0)
            {
                invalidSemanticDefinitions.Add("artifactSpecifications.semanticSpecifications.filterBindings");
            }

            if (semantic.IntendedMeasures.Count == 0)
            {
                invalidSemanticDefinitions.Add("artifactSpecifications.semanticSpecifications.intendedMeasures");
            }
        }

        if (string.IsNullOrWhiteSpace(artifact.NavigationSpecifications.LandingPage) ||
            artifact.NavigationSpecifications.PageTransitions.Count == 0 ||
            artifact.NavigationSpecifications.DrillPaths.Count == 0)
        {
            missingRequiredSections.Add("artifactSpecifications.navigationSpecifications");
        }

        if (!string.IsNullOrWhiteSpace(artifact.NavigationSpecifications.LandingPage) &&
            !knownPages.Contains(artifact.NavigationSpecifications.LandingPage))
        {
            invalidNavigationDefinitions.Add("navigationSpecifications.landingPage must match a declared page.");
        }

        if (artifact.SuccessCriteria.BusinessSuccessCriteria.Count == 0 ||
            artifact.SuccessCriteria.AnalyticalSuccessCriteria.Count == 0)
        {
            missingRequiredSections.Add("artifactSpecifications.successCriteria");
            incompleteSuccessCriteria.Add("artifactSpecifications.successCriteria");
        }
    }

    private static void ValidateSchemaVersion(string actual, string expected, ICollection<string> unsupportedSchemaVersions)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            unsupportedSchemaVersions.Add(actual);
        }
    }

    private static void ValidateNotBlank(string value, string fieldPath, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add(fieldPath);
        }
    }
}
