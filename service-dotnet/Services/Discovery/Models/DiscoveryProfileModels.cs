namespace PowerBIModelingService.Services.Discovery.Models;

internal enum DiscoveryConfidenceLevel
{
    Low,
    Medium,
    High,
}

internal enum DiscoveryDateIntelligenceReadiness
{
    Low,
    Medium,
    High,
}

internal sealed record DiscoveryMeasureProfile(
    string Name,
    string? Description,
    string? Folder);

internal sealed record DiscoveryDimensionProfile(
    string Name,
    string CardinalityIndicator,
    string BusinessRole);

internal sealed record DiscoveryHierarchyProfile(
    string Name,
    IReadOnlyList<string> Levels,
    bool IsInferred);

internal sealed record DiscoveryDateIntelligenceProfile(
    IReadOnlyList<string> DateTables,
    IReadOnlyList<string> DateDimensions,
    DiscoveryDateIntelligenceReadiness Readiness);

internal sealed record DiscoveryRelationshipProfile(
    string FromTable,
    string ToTable,
    string Cardinality,
    string Directionality);

internal sealed record DiscoveryDomainSignal(
    string Domain,
    DiscoveryConfidenceLevel Confidence,
    IReadOnlyList<string> Evidence);

internal sealed record DiscoveryKpiCluster(
    string ClusterName,
    IReadOnlyList<string> MeasureNames,
    DiscoveryConfidenceLevel Confidence);

internal sealed record DiscoveryAudienceSignal(
    string Audience,
    DiscoveryConfidenceLevel Confidence,
    IReadOnlyList<string> Evidence);

internal sealed record DiscoveryProfile(
    IReadOnlyList<DiscoveryMeasureProfile> Measures,
    IReadOnlyList<DiscoveryDimensionProfile> Dimensions,
    IReadOnlyList<DiscoveryHierarchyProfile> Hierarchies,
    DiscoveryDateIntelligenceProfile DateIntelligence,
    IReadOnlyList<DiscoveryRelationshipProfile> Relationships,
    IReadOnlyList<DiscoveryDomainSignal> BusinessDomains,
    IReadOnlyList<DiscoveryKpiCluster> KpiClusters,
    IReadOnlyList<DiscoveryAudienceSignal> AudienceSignals,
    IReadOnlyList<string> AmbiguityNotes,
    DiscoveryConfidenceLevel Confidence,
    string SemanticModelReferenceId,
    string DiscoveryProfileReferenceId);
