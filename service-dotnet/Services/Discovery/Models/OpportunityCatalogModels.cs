namespace PowerBIModelingService.Services.Discovery.Models;

internal enum OpportunityCategory
{
    ExecutiveReporting,
    OperationalMonitoring,
    ProfitabilityAnalysis,
    CustomerAnalysis,
    SalesPerformance,
    ForecastAccuracy,
    InventoryOptimization,
    ServiceOperations,
    RootCauseInvestigation,
    ComparativePerformanceManagement,
}

internal enum OpportunityExperienceType
{
    PbirReport,
    FabricApp,
    FabricDataApp,
    ExecutiveDashboard,
    OperationalMonitoringExperience,
    AnalyticalInvestigationExperience,
}

internal sealed record OpportunitySemanticSignal(
    string SignalType,
    string Value);

internal sealed record OpportunityCandidate(
    string OpportunityId,
    string Name,
    OpportunityCategory Category,
    string InferredAudience,
    string BusinessOutcome,
    IReadOnlyList<OpportunityExperienceType> CandidateExperienceTypes,
    IReadOnlyList<OpportunitySemanticSignal> SupportingSemanticSignals,
    IReadOnlyList<string> LimitingFactors,
    DiscoveryConfidenceLevel Confidence);

internal sealed record OpportunityCatalog(
    IReadOnlyList<OpportunityCandidate> Opportunities);
