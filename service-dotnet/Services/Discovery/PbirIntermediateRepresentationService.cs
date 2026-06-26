using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirIntermediateRepresentationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly PbirIntermediateRepresentationValidator _validator;
    private readonly PbirIntermediateRepresentationReadinessService _readinessService;

    internal PbirIntermediateRepresentationService()
        : this(new PbirIntermediateRepresentationValidator(), new PbirIntermediateRepresentationReadinessService())
    {
    }

    internal PbirIntermediateRepresentationService(
        PbirIntermediateRepresentationValidator validator,
        PbirIntermediateRepresentationReadinessService readinessService)
    {
        _validator = validator;
        _readinessService = readinessService;
    }

    internal PbirIntermediateRepresentationState CreateIntermediateRepresentation(
        GenerationManifestState manifestState,
        PbirGenerationSpecificationState specificationState,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(manifestState);
        ArgumentNullException.ThrowIfNull(specificationState);

        if (manifestState.Manifest is null || specificationState.Specification is null)
        {
            var diagnostics = new PbirIntermediateRepresentationValidationDiagnostics(
                MissingRequiredSections:
                [
                    manifestState.Manifest is null ? "generationManifest" : "pbirGenerationSpecification"
                ],
                MissingRequiredFields: [],
                InvalidReferences: [],
                InvalidNavigationDefinitions: [],
                InvalidSemanticDefinitions: [],
                InvalidLayoutDefinitions: [],
                UnsupportedSchemaVersions: [],
                BoundaryViolations: []);
            var validation = new PbirIntermediateRepresentationValidationResult(diagnostics);

            return new PbirIntermediateRepresentationState(
                Ir: null,
                Validation: validation,
                Readiness: _readinessService.Evaluate(validation, prepareForSerializer: false));
        }

        var manifest = manifestState.Manifest;
        var specification = specificationState.Specification;
        var irId = $"pbirIr:{manifest.Metadata.ManifestId}";
        var metadata = new PbirIntermediateRepresentationMetadata(
            IrId: irId,
            SchemaVersion: PbirIntermediateRepresentationContract.SchemaVersionV1,
            GeneratedUtc: generatedUtc.UtcDateTime);
        var references = new PbirIntermediateRepresentationReferences(
            GenerationManifestRef: manifest.Metadata.ManifestId,
            PbirGenerationSpecificationRef: specification.SpecificationId);
        var pages = CreatePages(specification);
        var visuals = CreateVisuals(specification);
        var semantics = CreateSemantics(specification, visuals);
        var navigation = CreateNavigation(specification, pages);
        var layout = CreateLayout(pages, visuals);
        var successCriteria = CreateSuccessCriteria(specification);
        var lineage = CreateLineage(manifest, specification, irId);
        var inputHash = ComputeSha256(Serialize(new
        {
            manifest,
            specification
        }));
        var lineageHash = ComputeSha256(Serialize(lineage.ImmutableLineage));
        var contentHash = ComputeSha256(Serialize(new
        {
            metadata,
            references,
            pages,
            visuals,
            semantics,
            navigation,
            layout,
            successCriteria,
            lineage
        }));

        var ir = new PbirIntermediateRepresentation(
            Metadata: metadata,
            References: references,
            Pages: pages,
            Visuals: visuals,
            Semantics: semantics,
            Navigation: navigation,
            Layout: layout,
            SuccessCriteria: successCriteria,
            Lineage: lineage,
            Hashes: new PbirIntermediateRepresentationHashes(
                InputHash: inputHash,
                ContentHash: contentHash,
                LineageHash: lineageHash));
        var validationResult = _validator.Validate(ir);

        return new PbirIntermediateRepresentationState(
            Ir: ir,
            Validation: validationResult,
            Readiness: _readinessService.Evaluate(validationResult, prepareForSerializer: true));
    }

    internal PbirSerializerRequest CreateSerializerRequest(PbirIntermediateRepresentationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Ir is null)
        {
            throw new InvalidOperationException("PBIR IR must exist before creating a serializer request contract.");
        }

        return new PbirSerializerRequest(
            SchemaVersion: PbirSerializerRequestContract.SchemaVersionV1,
            RequestId: $"pbirSerializerRequest:{state.Ir.Metadata.IrId}",
            PbirIrRef: state.Ir.Metadata.IrId,
            PbirIrSchemaVersion: state.Ir.Metadata.SchemaVersion,
            PbirIrContentHash: state.Ir.Hashes.ContentHash,
            SerializerImplementationAvailable: false,
            ProviderInvocationAllowed: false,
            DeploymentAllowed: false,
            MicrosoftSkillsExecutionAllowed: false);
    }

    private static IReadOnlyList<PbirIntermediateRepresentationPage> CreatePages(PbirGenerationSpecification specification)
    {
        return specification.ArtifactSpecifications
            .OrderBy(artifact => artifact.ArtifactSpecificationId, StringComparer.Ordinal)
            .SelectMany(artifact => artifact.PageSpecifications)
            .OrderBy(page => page.PageId, StringComparer.Ordinal)
            .Select((page, index) => new PbirIntermediateRepresentationPage(
                PageId: page.PageId,
                PageIdentity: $"page:{page.PageId}",
                NavigationBehavior: page.NavigationBehavior,
                IntendedPurpose: page.Purpose,
                Order: index + 1))
            .ToArray();
    }

    private static IReadOnlyList<PbirIntermediateRepresentationVisual> CreateVisuals(PbirGenerationSpecification specification)
    {
        return specification.ArtifactSpecifications
            .OrderBy(artifact => artifact.ArtifactSpecificationId, StringComparer.Ordinal)
            .SelectMany(artifact => artifact.VisualSpecifications
                .OrderBy(visual => visual.PageId, StringComparer.Ordinal)
                .ThenBy(visual => visual.VisualType, StringComparer.Ordinal)
                .ThenBy(visual => visual.Placement, StringComparer.Ordinal)
                .Select((visual, index) => new PbirIntermediateRepresentationVisual(
                    VisualId: $"visual:{artifact.ArtifactSpecificationId}:{visual.PageId}:{index + 1}",
                    PageId: visual.PageId,
                    VisualType: visual.VisualType,
                    Placement: visual.Placement,
                    SemanticIntent: visual.IntendedKpi,
                    InteractionModel: PreserveOrder(visual.IntendedInteractions),
                    Order: index + 1)))
            .OrderBy(visual => visual.VisualId, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<PbirIntermediateRepresentationSemantic> CreateSemantics(
        PbirGenerationSpecification specification,
        IReadOnlyList<PbirIntermediateRepresentationVisual> visuals)
    {
        var dimensionsByPage = specification.ArtifactSpecifications
            .SelectMany(artifact => artifact.VisualSpecifications)
            .GroupBy(visual => visual.PageId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(visual => visual.IntendedDimensions)
                    .Where(dimension => !string.IsNullOrWhiteSpace(dimension))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(dimension => dimension, StringComparer.Ordinal)
                    .DefaultIfEmpty("auto")
                    .ToArray(),
                StringComparer.Ordinal);

        return specification.ArtifactSpecifications
            .OrderBy(artifact => artifact.ArtifactSpecificationId, StringComparer.Ordinal)
            .SelectMany(artifact => artifact.SemanticSpecifications
                .OrderBy(semantic => semantic.PageId, StringComparer.Ordinal)
                .ThenBy(semantic => semantic.KpiBinding, StringComparer.Ordinal)
                .Select(semantic =>
                {
                    var dimensions = dimensionsByPage.TryGetValue(semantic.PageId, out var pageDimensions)
                        ? pageDimensions
                        : ["auto"];
                    var visualRelationships = visuals
                        .Where(visual => string.Equals(visual.PageId, semantic.PageId, StringComparison.Ordinal))
                        .Select(visual => $"visual:{visual.VisualId}->semantic:{semantic.KpiBinding}");

                    return new PbirIntermediateRepresentationSemantic(
                        SemanticId: $"semantic:{artifact.ArtifactSpecificationId}:{semantic.PageId}:{semantic.KpiBinding}",
                        PageId: semantic.PageId,
                        Measures: PreserveOrder(semantic.IntendedMeasures),
                        Dimensions: dimensions,
                        Kpis: [semantic.KpiBinding],
                        Filters: PreserveOrder(semantic.FilterBindings),
                        DrillBehavior: semantic.DrillBehavior,
                        Relationships: semantic.FilterBindings
                            .Select(filter => $"filter:{filter}->page:{semantic.PageId}")
                            .Concat(visualRelationships)
                            .Distinct(StringComparer.Ordinal)
                            .OrderBy(relationship => relationship, StringComparer.Ordinal)
                            .ToArray());
                }))
            .OrderBy(semantic => semantic.SemanticId, StringComparer.Ordinal)
            .ToArray();
    }

    private static PbirIntermediateRepresentationNavigation CreateNavigation(
        PbirGenerationSpecification specification,
        IReadOnlyList<PbirIntermediateRepresentationPage> pages)
    {
        var artifact = specification.ArtifactSpecifications
            .OrderBy(item => item.ArtifactSpecificationId, StringComparer.Ordinal)
            .First();
        var landingPage = artifact.NavigationSpecifications.LandingPage;
        var transitions = artifact.NavigationSpecifications.PageTransitions.Count > 0
            ? artifact.NavigationSpecifications.PageTransitions
                .Select(ParseTransition)
                .OrderBy(transition => transition.FromPageId, StringComparer.Ordinal)
                .ThenBy(transition => transition.ToPageId, StringComparer.Ordinal)
                .ThenBy(transition => transition.Transition, StringComparer.Ordinal)
                .ToArray()
            : pages
                .Zip(pages.Skip(1), (fromPage, toPage) => new PbirIntermediateRepresentationPageTransition(
                    FromPageId: fromPage.PageId,
                    ToPageId: toPage.PageId,
                    Transition: $"{fromPage.PageId}->{toPage.PageId}"))
                .ToArray();
        var bookmarks = pages
            .Select(page => $"page:{page.PageId}")
            .Append($"landing:{landingPage}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(bookmark => bookmark, StringComparer.Ordinal)
            .ToArray();

        return new PbirIntermediateRepresentationNavigation(
            LandingPage: landingPage,
            PageTransitions: transitions,
            Bookmarks: bookmarks,
            DrillPaths: PreserveOrder(artifact.NavigationSpecifications.DrillPaths));
    }

    private static PbirIntermediateRepresentationLayout CreateLayout(
        IReadOnlyList<PbirIntermediateRepresentationPage> pages,
        IReadOnlyList<PbirIntermediateRepresentationVisual> visuals)
    {
        var containers = pages
            .Select(page =>
            {
                var visualRefs = visuals
                    .Where(visual => string.Equals(visual.PageId, page.PageId, StringComparison.Ordinal))
                    .Select(visual => visual.VisualId)
                    .DefaultIfEmpty($"pageShell:{page.PageId}")
                    .OrderBy(visualRef => visualRef, StringComparer.Ordinal)
                    .ToArray();

                return new PbirIntermediateRepresentationLayoutContainer(
                    ContainerId: $"container:{page.PageId}",
                    PageId: page.PageId,
                    Purpose: page.IntendedPurpose,
                    VisualRefs: visualRefs);
            })
            .OrderBy(container => container.ContainerId, StringComparer.Ordinal)
            .ToArray();

        return new PbirIntermediateRepresentationLayout(
            Containers: containers,
            Spacing: ["standard-8px-grid"],
            Alignment: ["deterministic-grid", "visual-placement-preserved"],
            ResponsiveHints: ["preserve-page-order", "preserve-visual-intent", "allow-future-serializer-layout-adaptation"]);
    }

    private static PbirIntermediateRepresentationSuccessCriteria CreateSuccessCriteria(PbirGenerationSpecification specification)
    {
        var successCriteria = specification.ArtifactSpecifications
            .OrderBy(artifact => artifact.ArtifactSpecificationId, StringComparer.Ordinal)
            .Select(artifact => artifact.SuccessCriteria)
            .ToArray();

        return new PbirIntermediateRepresentationSuccessCriteria(
            BusinessIntent: PreserveOrder(successCriteria
                .SelectMany(criteria => criteria.BusinessSuccessCriteria)
                .ToArray()),
            AnalyticalFlow: PreserveOrder(successCriteria
                .SelectMany(criteria => criteria.AnalyticalSuccessCriteria)
                .ToArray()),
            SuccessCriteria: PreserveOrder(successCriteria
                .SelectMany(criteria => criteria.PlanningOutcomeRequirements)
                .ToArray()));
    }

    private static PbirIntermediateRepresentationLineage CreateLineage(
        GenerationManifest manifest,
        PbirGenerationSpecification specification,
        string irId)
    {
        return new PbirIntermediateRepresentationLineage(
            UpstreamLineage: manifest.Lineage.UpstreamLineage
                .OrderBy(entry => entry.Stage, StringComparer.Ordinal)
                .ThenBy(entry => entry.ReferenceId, StringComparer.Ordinal)
                .ThenBy(entry => entry.Label, StringComparer.Ordinal)
                .ToArray(),
            ImmutableLineage: manifest.Lineage.ImmutableUpstreamLineage
                .Append(manifest.Metadata.ManifestId)
                .Append(specification.SpecificationId)
                .Append(irId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reference => reference, StringComparer.Ordinal)
                .ToArray());
    }

    private static PbirIntermediateRepresentationPageTransition ParseTransition(string transition)
    {
        var parts = transition.Split("->", StringSplitOptions.TrimEntries);

        return parts.Length == 2
            ? new PbirIntermediateRepresentationPageTransition(
                FromPageId: parts[0],
                ToPageId: parts[1],
                Transition: transition)
            : new PbirIntermediateRepresentationPageTransition(
                FromPageId: transition,
                ToPageId: transition,
                Transition: transition);
    }

    private static IReadOnlyList<string> PreserveOrder(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }
}
