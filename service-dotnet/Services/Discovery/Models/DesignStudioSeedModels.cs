using PowerBIModelingService.Services.DesignStudio.Models;

namespace PowerBIModelingService.Services.Discovery.Models;

internal sealed record DiscoveryDesignStudioStartingPoint(
    string SelectedRecommendationId,
    DesignBrief DesignBrief,
    ReportConcept Concept,
    DraftReportArtifact Draft,
    IReadOnlyList<DraftPageArtifact> DraftPages,
    IReadOnlyList<DraftLayoutArtifact> DraftLayouts,
    IReadOnlyList<DraftNavigationArtifact> DraftNavigationArtifacts);
