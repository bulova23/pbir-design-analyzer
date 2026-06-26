using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirIntermediateRepresentationValidator
{
    internal PbirIntermediateRepresentationValidationResult Validate(PbirIntermediateRepresentation ir)
    {
        ArgumentNullException.ThrowIfNull(ir);

        var missingRequiredSections = new List<string>();
        var missingRequiredFields = new List<string>();
        var invalidReferences = new List<string>();
        var invalidNavigationDefinitions = new List<string>();
        var invalidSemanticDefinitions = new List<string>();
        var invalidLayoutDefinitions = new List<string>();
        var unsupportedSchemaVersions = new List<string>();
        var boundaryViolations = new List<string>();

        ValidateMetadata(ir, missingRequiredSections, missingRequiredFields, unsupportedSchemaVersions);
        ValidateReferences(ir, missingRequiredSections, missingRequiredFields);
        ValidateCollections(ir, missingRequiredSections);

        var knownPages = ir.Pages
            .Select(page => page.PageId)
            .Where(pageId => !string.IsNullOrWhiteSpace(pageId))
            .ToHashSet(StringComparer.Ordinal);

        ValidatePages(ir, missingRequiredFields);
        ValidateVisuals(ir, knownPages, missingRequiredFields);
        ValidateSemantics(ir, knownPages, invalidSemanticDefinitions, missingRequiredFields);
        ValidateNavigation(ir, knownPages, invalidNavigationDefinitions);
        ValidateLayout(ir, knownPages, invalidLayoutDefinitions);
        ValidateSuccessCriteria(ir, missingRequiredSections);
        ValidateHashes(ir, missingRequiredFields);

        return new PbirIntermediateRepresentationValidationResult(
            new PbirIntermediateRepresentationValidationDiagnostics(
                MissingRequiredSections: DistinctAndOrder(missingRequiredSections),
                MissingRequiredFields: DistinctAndOrder(missingRequiredFields),
                InvalidReferences: DistinctAndOrder(invalidReferences),
                InvalidNavigationDefinitions: DistinctAndOrder(invalidNavigationDefinitions),
                InvalidSemanticDefinitions: DistinctAndOrder(invalidSemanticDefinitions),
                InvalidLayoutDefinitions: DistinctAndOrder(invalidLayoutDefinitions),
                UnsupportedSchemaVersions: DistinctAndOrder(unsupportedSchemaVersions),
                BoundaryViolations: DistinctAndOrder(boundaryViolations)));
    }

    private static void ValidateMetadata(
        PbirIntermediateRepresentation ir,
        ICollection<string> missingRequiredSections,
        ICollection<string> missingRequiredFields,
        ICollection<string> unsupportedSchemaVersions)
    {
        if (ir.Metadata is null)
        {
            missingRequiredSections.Add("metadata");
            return;
        }

        ValidateNotBlank(ir.Metadata.IrId, "metadata.irId", missingRequiredFields);
        ValidateSchemaVersion(ir.Metadata.SchemaVersion, PbirIntermediateRepresentationContract.SchemaVersionV1, unsupportedSchemaVersions);

        if (ir.Metadata.GeneratedUtc == default)
        {
            missingRequiredFields.Add("metadata.generatedUtc");
        }
    }

    private static void ValidateReferences(
        PbirIntermediateRepresentation ir,
        ICollection<string> missingRequiredSections,
        ICollection<string> missingRequiredFields)
    {
        if (ir.References is null)
        {
            missingRequiredSections.Add("references");
            return;
        }

        ValidateNotBlank(ir.References.GenerationManifestRef, "references.generationManifestRef", missingRequiredFields);
        ValidateNotBlank(ir.References.PbirGenerationSpecificationRef, "references.pbirGenerationSpecificationRef", missingRequiredFields);
    }

    private static void ValidateCollections(PbirIntermediateRepresentation ir, ICollection<string> missingRequiredSections)
    {
        if (ir.Pages.Count == 0)
        {
            missingRequiredSections.Add("pages");
        }

        if (ir.Visuals.Count == 0)
        {
            missingRequiredSections.Add("visuals");
        }

        if (ir.Semantics.Count == 0)
        {
            missingRequiredSections.Add("semantics");
        }

        if (ir.Navigation is null)
        {
            missingRequiredSections.Add("navigation");
        }

        if (ir.Layout is null)
        {
            missingRequiredSections.Add("layout");
        }

        if (ir.SuccessCriteria is null)
        {
            missingRequiredSections.Add("successCriteria");
        }

        if (ir.Lineage is null)
        {
            missingRequiredSections.Add("lineage");
        }

        if (ir.Hashes is null)
        {
            missingRequiredSections.Add("hashes");
        }
    }

    private static void ValidatePages(
        PbirIntermediateRepresentation ir,
        ICollection<string> missingRequiredFields)
    {
        foreach (var page in ir.Pages)
        {
            ValidateNotBlank(page.PageId, "pages.pageId", missingRequiredFields);
            ValidateNotBlank(page.PageIdentity, "pages.pageIdentity", missingRequiredFields);
            ValidateNotBlank(page.IntendedPurpose, "pages.intendedPurpose", missingRequiredFields);
            ValidateNotBlank(page.NavigationBehavior, "pages.navigationBehavior", missingRequiredFields);

            if (page.Order <= 0)
            {
                missingRequiredFields.Add("pages.order");
            }
        }
    }

    private static void ValidateVisuals(
        PbirIntermediateRepresentation ir,
        IReadOnlySet<string> knownPages,
        ICollection<string> missingRequiredFields)
    {
        foreach (var visual in ir.Visuals)
        {
            ValidateNotBlank(visual.VisualId, "visuals.visualId", missingRequiredFields);
            ValidateNotBlank(visual.PageId, "visuals.pageId", missingRequiredFields);
            ValidateNotBlank(visual.VisualType, "visuals.visualType", missingRequiredFields);
            ValidateNotBlank(visual.Placement, "visuals.placement", missingRequiredFields);
            ValidateNotBlank(visual.SemanticIntent, "visuals.semanticIntent", missingRequiredFields);

            if (visual.InteractionModel.Count == 0)
            {
                missingRequiredFields.Add("visuals.interactionModel");
            }

            if (!knownPages.Contains(visual.PageId))
            {
                missingRequiredFields.Add("visuals.pageId must match a declared page.");
            }
        }
    }

    private static void ValidateSemantics(
        PbirIntermediateRepresentation ir,
        IReadOnlySet<string> knownPages,
        ICollection<string> invalidSemanticDefinitions,
        ICollection<string> missingRequiredFields)
    {
        foreach (var semantic in ir.Semantics)
        {
            ValidateNotBlank(semantic.SemanticId, "semantics.semanticId", missingRequiredFields);
            ValidateNotBlank(semantic.PageId, "semantics.pageId", missingRequiredFields);
            ValidateNotBlank(semantic.DrillBehavior, "semantics.drillBehavior", missingRequiredFields);

            if (!knownPages.Contains(semantic.PageId))
            {
                invalidSemanticDefinitions.Add("semantic.pageId must match a declared page.");
            }

            if (semantic.Measures.Count == 0)
            {
                invalidSemanticDefinitions.Add("semantic.measures must not be empty.");
            }

            if (semantic.Dimensions.Count == 0)
            {
                invalidSemanticDefinitions.Add("semantic.dimensions must not be empty.");
            }

            if (semantic.Kpis.Count == 0)
            {
                invalidSemanticDefinitions.Add("semantic.kpis must not be empty.");
            }

            if (semantic.Filters.Count == 0)
            {
                invalidSemanticDefinitions.Add("semantic.filters must not be empty.");
            }

            if (semantic.Relationships.Count == 0)
            {
                invalidSemanticDefinitions.Add("semantic.relationships must not be empty.");
            }
        }
    }

    private static void ValidateNavigation(
        PbirIntermediateRepresentation ir,
        IReadOnlySet<string> knownPages,
        ICollection<string> invalidNavigationDefinitions)
    {
        if (ir.Navigation is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ir.Navigation.LandingPage) || !knownPages.Contains(ir.Navigation.LandingPage))
        {
            invalidNavigationDefinitions.Add("navigation.landingPage must match a declared page.");
        }

        if (ir.Navigation.Bookmarks.Count == 0)
        {
            invalidNavigationDefinitions.Add("navigation.bookmarks must not be empty.");
        }

        if (ir.Navigation.DrillPaths.Count == 0)
        {
            invalidNavigationDefinitions.Add("navigation.drillPaths must not be empty.");
        }

        foreach (var transition in ir.Navigation.PageTransitions)
        {
            if (!knownPages.Contains(transition.FromPageId) ||
                !knownPages.Contains(transition.ToPageId) ||
                string.IsNullOrWhiteSpace(transition.Transition))
            {
                invalidNavigationDefinitions.Add("navigation.pageTransitions must reference declared pages.");
            }
        }
    }

    private static void ValidateLayout(
        PbirIntermediateRepresentation ir,
        IReadOnlySet<string> knownPages,
        ICollection<string> invalidLayoutDefinitions)
    {
        if (ir.Layout is null)
        {
            return;
        }

        var containerPages = ir.Layout.Containers
            .Select(container => container.PageId)
            .Where(pageId => !string.IsNullOrWhiteSpace(pageId))
            .ToHashSet(StringComparer.Ordinal);

        if (!knownPages.IsSubsetOf(containerPages))
        {
            invalidLayoutDefinitions.Add("layout.containers must include every declared page.");
        }

        foreach (var container in ir.Layout.Containers)
        {
            if (!knownPages.Contains(container.PageId))
            {
                invalidLayoutDefinitions.Add("layout.containers must reference declared pages.");
            }

            if (string.IsNullOrWhiteSpace(container.ContainerId) ||
                string.IsNullOrWhiteSpace(container.Purpose) ||
                container.VisualRefs.Count == 0)
            {
                invalidLayoutDefinitions.Add("layout.containers must include identity, purpose, and visual references.");
            }
        }

        if (ir.Layout.Spacing.Count == 0)
        {
            invalidLayoutDefinitions.Add("layout.spacing must not be empty.");
        }

        if (ir.Layout.Alignment.Count == 0)
        {
            invalidLayoutDefinitions.Add("layout.alignment must not be empty.");
        }

        if (ir.Layout.ResponsiveHints.Count == 0)
        {
            invalidLayoutDefinitions.Add("layout.responsiveHints must not be empty.");
        }
    }

    private static void ValidateSuccessCriteria(
        PbirIntermediateRepresentation ir,
        ICollection<string> missingRequiredSections)
    {
        if (ir.SuccessCriteria is null)
        {
            return;
        }

        if (ir.SuccessCriteria.BusinessIntent.Count == 0 ||
            ir.SuccessCriteria.AnalyticalFlow.Count == 0 ||
            ir.SuccessCriteria.SuccessCriteria.Count == 0)
        {
            missingRequiredSections.Add("successCriteria");
        }
    }

    private static void ValidateHashes(
        PbirIntermediateRepresentation ir,
        ICollection<string> missingRequiredFields)
    {
        if (ir.Hashes is null)
        {
            return;
        }

        ValidateNotBlank(ir.Hashes.InputHash, "hashes.inputHash", missingRequiredFields);
        ValidateNotBlank(ir.Hashes.ContentHash, "hashes.contentHash", missingRequiredFields);
        ValidateNotBlank(ir.Hashes.LineageHash, "hashes.lineageHash", missingRequiredFields);
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

    private static IReadOnlyList<string> DistinctAndOrder(IEnumerable<string> values)
    {
        return values
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }
}
