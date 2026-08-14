using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal enum LocalPbirGenerationNavigationTargetKind
{
    Previous,
    Next,
    Home,
    Page
}

internal enum LocalPbirGenerationSectionKind
{
    Header,
    KpiRow,
    PrimaryAnalysis,
    SecondaryAnalysis,
    DetailGrid,
    FilterRail,
    FooterNavigation
}

internal sealed record LocalPbirGenerationSlotDefinition(
    [property: JsonPropertyName("slotId")] string SlotId,
    [property: JsonPropertyName("section")] LocalPbirGenerationSectionKind Section,
    [property: JsonPropertyName("allowedVisualTypes")] IReadOnlyList<string> AllowedVisualTypes,
    [property: JsonPropertyName("required")] bool Required,
    [property: JsonPropertyName("layout")] LocalPbirGenerationVisualLayout Layout);

internal sealed record LocalPbirGenerationSectionDefinition(
    [property: JsonPropertyName("sectionId")] string SectionId,
    [property: JsonPropertyName("kind")] LocalPbirGenerationSectionKind Kind,
    [property: JsonPropertyName("slots")] IReadOnlyList<LocalPbirGenerationSlotDefinition> Slots);

internal sealed record LocalPbirGenerationPageTemplate(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("pageWidth")] int PageWidth,
    [property: JsonPropertyName("pageHeight")] int PageHeight,
    [property: JsonPropertyName("sections")] IReadOnlyList<LocalPbirGenerationSectionDefinition> Sections,
    [property: JsonPropertyName("navigationSlotId")] string? NavigationSlotId,
    [property: JsonPropertyName("slicerSlotId")] string? SlicerSlotId);

internal sealed record LocalPbirGenerationSlotAssignment(
    [property: JsonPropertyName("slotId")] string SlotId,
    [property: JsonPropertyName("visualId")] string VisualId);

internal sealed record LocalPbirGenerationNavigationTarget(
    [property: JsonPropertyName("navigationId")] string NavigationId,
    [property: JsonPropertyName("kind")] LocalPbirGenerationNavigationTargetKind Kind,
    [property: JsonPropertyName("pageId")] string? PageId = null);

internal sealed record LocalPbirGenerationNavigationDefinition(
    [property: JsonPropertyName("navigationId")] string NavigationId,
    [property: JsonPropertyName("targets")] IReadOnlyList<LocalPbirGenerationNavigationTarget> Targets);

internal sealed record LocalPbirGenerationSlicerInteraction(
    [property: JsonPropertyName("targetVisualIds")] IReadOnlyList<string>? TargetVisualIds = null,
    [property: JsonPropertyName("targetPage")] bool TargetPage = false,
    [property: JsonPropertyName("enabled")] bool Enabled = true);

internal sealed record LocalPbirGenerationSlicerDefinition(
    [property: JsonPropertyName("visualId")] string VisualId,
    [property: JsonPropertyName("orientation")] LocalPbirGenerationAxisOrientation Orientation = LocalPbirGenerationAxisOrientation.Vertical,
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("interaction")] LocalPbirGenerationSlicerInteraction? Interaction = null);

internal sealed record LocalPbirGenerationPageComposition(
    [property: JsonPropertyName("template")] string Template,
    [property: JsonPropertyName("slotAssignments")] IReadOnlyList<LocalPbirGenerationSlotAssignment> SlotAssignments,
    [property: JsonPropertyName("navigation")] LocalPbirGenerationNavigationDefinition? Navigation,
    [property: JsonPropertyName("slicer")] LocalPbirGenerationSlicerDefinition? Slicer,
    [property: JsonPropertyName("pageId")] string? PageId = null,
    [property: JsonPropertyName("interactions")] IReadOnlyList<LocalPbirGenerationSlicerInteractionRule>? Interactions = null);

internal sealed record Phase41CompositionProjectionResult(
    IReadOnlyDictionary<string, LocalPbirGenerationVisualLayout> VisualLayouts,
    IReadOnlyList<LocalPbirGenerationDiagnostic> Diagnostics);
