using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PbirIntermediateRepresentationContract
{
    internal const string SchemaVersionV1 = "pbir-ir/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "Metadata",
        "Metadata.IrId",
        "Metadata.SchemaVersion",
        "Metadata.GeneratedUtc",
        "References",
        "References.GenerationManifestRef",
        "References.PbirGenerationSpecificationRef",
        "Pages",
        "Pages.PageId",
        "Pages.PageIdentity",
        "Pages.NavigationBehavior",
        "Pages.IntendedPurpose",
        "Pages.Order",
        "Visuals",
        "Visuals.VisualId",
        "Visuals.PageId",
        "Visuals.VisualType",
        "Visuals.Placement",
        "Visuals.SemanticIntent",
        "Visuals.InteractionModel",
        "Visuals.Order",
        "Semantics",
        "Semantics.SemanticId",
        "Semantics.PageId",
        "Semantics.Measures",
        "Semantics.Dimensions",
        "Semantics.Kpis",
        "Semantics.Filters",
        "Semantics.DrillBehavior",
        "Semantics.Relationships",
        "Navigation",
        "Navigation.LandingPage",
        "Navigation.PageTransitions",
        "Navigation.Bookmarks",
        "Navigation.DrillPaths",
        "Layout",
        "Layout.Containers",
        "Layout.Spacing",
        "Layout.Alignment",
        "Layout.ResponsiveHints",
        "SuccessCriteria",
        "SuccessCriteria.BusinessIntent",
        "SuccessCriteria.AnalyticalFlow",
        "SuccessCriteria.SuccessCriteria",
        "Lineage",
        "Lineage.ImmutableLineage",
        "Hashes",
        "Hashes.InputHash",
        "Hashes.ContentHash",
        "Hashes.LineageHash",
    ];
}

internal static class PbirSerializerRequestContract
{
    internal const string SchemaVersionV1 = "pbir-serializer-request/v1";
}

internal enum PbirIntermediateRepresentationReadinessState
{
    Incomplete,
    Blocked,
    Canonical,
    ReadyForSerializer,
}

internal sealed record PbirIntermediateRepresentationMetadata(
    [property: JsonPropertyName("irId")] string IrId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("generatedUtc")] DateTime GeneratedUtc);

internal sealed record PbirIntermediateRepresentationReferences(
    [property: JsonPropertyName("generationManifestRef")] string GenerationManifestRef,
    [property: JsonPropertyName("pbirGenerationSpecificationRef")] string PbirGenerationSpecificationRef);

internal sealed record PbirIntermediateRepresentationPage(
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("pageIdentity")] string PageIdentity,
    [property: JsonPropertyName("navigationBehavior")] string NavigationBehavior,
    [property: JsonPropertyName("intendedPurpose")] string IntendedPurpose,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("displayName")] string? DisplayName = null);

internal sealed record PbirIntermediateRepresentationVisual(
    [property: JsonPropertyName("visualId")] string VisualId,
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("visualType")] string VisualType,
    [property: JsonPropertyName("placement")] string Placement,
    [property: JsonPropertyName("semanticIntent")] string SemanticIntent,
    [property: JsonPropertyName("interactionModel")] IReadOnlyList<string> InteractionModel,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("layout")] PbirIntermediateRepresentationVisualLayout? Layout = null,
    [property: JsonPropertyName("bindings")] IReadOnlyList<PbirIntermediateRepresentationBinding>? Bindings = null);

internal enum PbirIntermediateRepresentationBindingRole
{
    Value,
    Category,
    Series,
    Axis,
    Legend,
    Tooltip
}

internal enum PbirIntermediateRepresentationBindingKind
{
    Measure,
    Dimension
}

internal sealed record PbirIntermediateRepresentationBinding(
    [property: JsonPropertyName("bindingId")] string BindingId,
    [property: JsonPropertyName("role")] PbirIntermediateRepresentationBindingRole Role,
    [property: JsonPropertyName("kind")] PbirIntermediateRepresentationBindingKind Kind,
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("entity")] string Entity,
    [property: JsonPropertyName("property")] string Property,
    [property: JsonPropertyName("projectionOrder")] int ProjectionOrder);

internal sealed record PbirIntermediateRepresentationVisualLayout(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

internal sealed record PbirIntermediateRepresentationSemantic(
    [property: JsonPropertyName("semanticId")] string SemanticId,
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("measures")] IReadOnlyList<string> Measures,
    [property: JsonPropertyName("dimensions")] IReadOnlyList<string> Dimensions,
    [property: JsonPropertyName("kpis")] IReadOnlyList<string> Kpis,
    [property: JsonPropertyName("filters")] IReadOnlyList<string> Filters,
    [property: JsonPropertyName("drillBehavior")] string DrillBehavior,
    [property: JsonPropertyName("relationships")] IReadOnlyList<string> Relationships);

internal sealed record PbirIntermediateRepresentationPageTransition(
    [property: JsonPropertyName("fromPageId")] string FromPageId,
    [property: JsonPropertyName("toPageId")] string ToPageId,
    [property: JsonPropertyName("transition")] string Transition);

internal sealed record PbirIntermediateRepresentationNavigation(
    [property: JsonPropertyName("landingPage")] string LandingPage,
    [property: JsonPropertyName("pageTransitions")] IReadOnlyList<PbirIntermediateRepresentationPageTransition> PageTransitions,
    [property: JsonPropertyName("bookmarks")] IReadOnlyList<string> Bookmarks,
    [property: JsonPropertyName("drillPaths")] IReadOnlyList<string> DrillPaths);

internal sealed record PbirIntermediateRepresentationLayoutContainer(
    [property: JsonPropertyName("containerId")] string ContainerId,
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("purpose")] string Purpose,
    [property: JsonPropertyName("visualRefs")] IReadOnlyList<string> VisualRefs);

internal sealed record PbirIntermediateRepresentationLayout(
    [property: JsonPropertyName("containers")] IReadOnlyList<PbirIntermediateRepresentationLayoutContainer> Containers,
    [property: JsonPropertyName("spacing")] IReadOnlyList<string> Spacing,
    [property: JsonPropertyName("alignment")] IReadOnlyList<string> Alignment,
    [property: JsonPropertyName("responsiveHints")] IReadOnlyList<string> ResponsiveHints);

internal sealed record PbirIntermediateRepresentationSuccessCriteria(
    [property: JsonPropertyName("businessIntent")] IReadOnlyList<string> BusinessIntent,
    [property: JsonPropertyName("analyticalFlow")] IReadOnlyList<string> AnalyticalFlow,
    [property: JsonPropertyName("successCriteria")] IReadOnlyList<string> SuccessCriteria);

internal sealed record PbirIntermediateRepresentationLineage(
    [property: JsonPropertyName("upstreamLineage")] IReadOnlyList<PlanningLineageEntry> UpstreamLineage,
    [property: JsonPropertyName("immutableLineage")] IReadOnlyList<string> ImmutableLineage);

internal sealed record PbirIntermediateRepresentationHashes(
    [property: JsonPropertyName("inputHash")] string InputHash,
    [property: JsonPropertyName("contentHash")] string ContentHash,
    [property: JsonPropertyName("lineageHash")] string LineageHash);

internal sealed record PbirIntermediateRepresentation(
    [property: JsonPropertyName("metadata")] PbirIntermediateRepresentationMetadata Metadata,
    [property: JsonPropertyName("references")] PbirIntermediateRepresentationReferences References,
    [property: JsonPropertyName("pages")] IReadOnlyList<PbirIntermediateRepresentationPage> Pages,
    [property: JsonPropertyName("visuals")] IReadOnlyList<PbirIntermediateRepresentationVisual> Visuals,
    [property: JsonPropertyName("semantics")] IReadOnlyList<PbirIntermediateRepresentationSemantic> Semantics,
    [property: JsonPropertyName("navigation")] PbirIntermediateRepresentationNavigation Navigation,
    [property: JsonPropertyName("layout")] PbirIntermediateRepresentationLayout Layout,
    [property: JsonPropertyName("successCriteria")] PbirIntermediateRepresentationSuccessCriteria SuccessCriteria,
    [property: JsonPropertyName("lineage")] PbirIntermediateRepresentationLineage Lineage,
    [property: JsonPropertyName("hashes")] PbirIntermediateRepresentationHashes Hashes,
    [property: JsonPropertyName("visualInteractions")] IReadOnlyList<PbirIntermediateRepresentationVisualInteraction>? VisualInteractions = null,
    [property: JsonPropertyName("authoringEnvelope")] PbirAuthoringEnvelope? AuthoringEnvelope = null);

internal sealed record PbirIntermediateRepresentationValidationDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> InvalidReferences,
    IReadOnlyList<string> InvalidNavigationDefinitions,
    IReadOnlyList<string> InvalidSemanticDefinitions,
    IReadOnlyList<string> InvalidLayoutDefinitions,
    IReadOnlyList<string> UnsupportedSchemaVersions,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static PbirIntermediateRepresentationValidationDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], []);

    internal bool HasIncompleteFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        UnsupportedSchemaVersions.Count > 0;

    internal bool HasBlockingFailures =>
        InvalidReferences.Count > 0 ||
        InvalidNavigationDefinitions.Count > 0 ||
        InvalidSemanticDefinitions.Count > 0 ||
        InvalidLayoutDefinitions.Count > 0 ||
        BoundaryViolations.Count > 0;
}

internal sealed record PbirIntermediateRepresentationValidationResult(
    PbirIntermediateRepresentationValidationDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasIncompleteFailures && !Diagnostics.HasBlockingFailures;
}

internal sealed record PbirIntermediateRepresentationState(
    PbirIntermediateRepresentation? Ir,
    PbirIntermediateRepresentationValidationResult Validation,
    PbirIntermediateRepresentationReadinessState Readiness);

internal sealed record PbirSerializerRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("pbirIrRef")] string PbirIrRef,
    [property: JsonPropertyName("pbirIrSchemaVersion")] string PbirIrSchemaVersion,
    [property: JsonPropertyName("pbirIrContentHash")] string PbirIrContentHash,
    [property: JsonPropertyName("serializerImplementationAvailable")] bool SerializerImplementationAvailable,
    [property: JsonPropertyName("providerInvocationAllowed")] bool ProviderInvocationAllowed,
    [property: JsonPropertyName("deploymentAllowed")] bool DeploymentAllowed,
    [property: JsonPropertyName("microsoftSkillsExecutionAllowed")] bool MicrosoftSkillsExecutionAllowed);
