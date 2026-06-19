namespace PowerBIModelingService.Services.Discovery.Models;

internal enum ConsultantDomainFramework
{
    General,
    RevenueSales,
    CustomerProfitability,
    Inventory,
    Forecasting,
    ServiceOperations,
    AnalyticalInvestigation,
}

internal enum ConsultantAudienceFit
{
    Executive,
    Operational,
    Analyst,
    ServiceManager,
    SalesManager,
    Mixed,
}

internal enum ConsultantDecisionCadence
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Episodic,
}

internal enum ConsultantWorkflowOrientation
{
    Monitor,
    Investigate,
    Act,
    Govern,
}

internal enum ConsultantConsumptionPattern
{
    Dashboard,
    App,
    DataApp,
    InvestigativeExperience,
    NarrativeReport,
}

internal enum ConsultantActionability
{
    Informational,
    Operational,
    Strategic,
}

internal enum ConsultantAdoptionLikelihood
{
    Low,
    Medium,
    High,
}

internal enum ConsultantMaintenanceComplexity
{
    Low,
    Medium,
    High,
}

internal sealed record ConsultantDecisionAssessment(
    ConsultantDomainFramework DomainFramework,
    ConsultantAudienceFit AudienceFit,
    ConsultantDecisionCadence DecisionCadence,
    ConsultantWorkflowOrientation WorkflowOrientation,
    ConsultantConsumptionPattern ConsumptionPattern,
    ConsultantActionability Actionability,
    ConsultantAdoptionLikelihood AdoptionLikelihood,
    ConsultantMaintenanceComplexity MaintenanceComplexity,
    double TechnicalFitScore,
    double BusinessFitScore,
    double ConsultantJudgmentScore,
    string WhyThisExperienceWins,
    IReadOnlyList<string> WhyCompetingExperiencesLose,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Assumptions,
    string AdoptionConsiderations,
    string FutureEvolutionPath);
