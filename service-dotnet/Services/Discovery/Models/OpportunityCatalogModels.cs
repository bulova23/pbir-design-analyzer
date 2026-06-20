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

internal enum OpportunityFamily
{
    Executive,
    Monitoring,
    Operational,
    Analytical,
    Planning,
    Governance,
    Investigation,
    Workflow,
    Performance,
    Optimization,
}

internal enum OpportunityWorkflowOrientation
{
    Monitor,
    Analyze,
    Investigate,
    Act,
    Govern,
}

internal enum OpportunityDecisionPattern
{
    Summary,
    Comparative,
    Threshold,
    Diagnostic,
    Planning,
    Prioritization,
    Workflow,
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
    DiscoveryConfidenceLevel Confidence)
{
    internal OpportunityFamily Family { get; init; } = OpportunityFamily.Analytical;
    internal OpportunityWorkflowOrientation WorkflowOrientation { get; init; } = OpportunityWorkflowOrientation.Analyze;
    internal OpportunityDecisionPattern DecisionPattern { get; init; } = OpportunityDecisionPattern.Summary;
    internal string WhyThisOpportunityExists { get; init; } = string.Empty;
    internal IReadOnlyList<string> EvidenceNarrative { get; init; } = [];
}

internal sealed record OpportunityCatalog(
    IReadOnlyList<OpportunityCandidate> Opportunities);
