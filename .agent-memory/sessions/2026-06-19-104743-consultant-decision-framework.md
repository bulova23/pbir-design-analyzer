# 2026-06-19 Consultant Decision Framework

## Objective

- implement only the Discovery Wizard Consultant Decision Framework from Validation Review Round 4
- introduce an explicit consultant-style decision layer between Opportunity Catalog and Recommendation Engine
- improve consultant-defensible recommendation ranking and rationale for revenue, customer profitability, forecasting, and service workflow scenarios
- stop before Microsoft Skills integration, CLI integration, provider-backed generation, asset generation, Design Studio workflow changes, and Analyzer Workspace changes

## Notes

- started by reading `AGENTS.md`, repo memory files, Round 4 validation review, the discovery design spec, and the current discovery services/tests
- current recommendation ranking is still dominated by heuristic semantic-fit scoring and only explains the result afterward
- this session will keep all changes inside the backend discovery layer and validate extension commands sequentially to avoid the known `npm test` / `npm run compile` race

## Delivered

- added backend-internal consultant decision models for:
  - domain framework
  - audience fit
  - decision cadence
  - workflow orientation
  - consumption pattern
  - actionability
  - adoption likelihood
  - maintenance complexity
- integrated an explicit consultant decision assessment into `RecommendationEngineService` so recommendation ranking now blends:
  - technical fit
  - business fit
  - consultant judgment
- added domain-aware consultant scoring adjustments for:
  - revenue / sales
  - customer profitability
  - inventory
  - forecasting
  - service operations
  - analytical investigation
- added domain-dilution penalties so generic revenue reporting no longer outranks richer forecasting or customer-profitability recommendations when those signals lead
- rewrote recommendation rationale to include consultant decision sections for:
  - Why This Experience Wins
  - Why Competing Experiences Lose
  - Risks
  - Assumptions
  - Adoption Considerations
  - Future Evolution Path
- preserved existing tradeoff-oriented rationale sections so downstream blueprint and package consumers keep receiving consultant-readable explanation text
- added focused xUnit coverage for:
  - revenue operational workflow beating executive dashboard when follow-through is the real need
  - forecasting recommendations staying distinct from generic revenue reporting
  - customer profitability recommendations staying distinct from generic revenue reporting
  - service workflow orchestration beating monitoring-only recommendations when workflow signals lead
  - consultant decision rationale sections
- updated the broad recommendation ranking baseline test to assert strong candidates surface first without pinning one brittle cross-domain winner
- kept the change inside the discovery backend boundary only; no Microsoft Skills integration, CLI integration, provider-backed generation, asset generation, Design Studio workflow changes, or Analyzer Workspace changes were started

## Validation

- passed `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~BuildRecommendations_RevenueOperationalWorkflowCanBeatExecutiveDashboard|FullyQualifiedName~BuildRecommendations_ForecastingRecommendationsBeatGenericRevenueReportingWhenForecastSignalsLead|FullyQualifiedName~BuildRecommendations_CustomerProfitabilityRecommendationsBeatGenericRevenueReportingWhenProfitabilitySignalsLead|FullyQualifiedName~BuildRecommendations_ServiceWorkflowRecommendationsBeatMonitoringWhenWorkflowSignalsLead|FullyQualifiedName~BuildRecommendations_RationaleIncludesConsultantDecisionFrameworkSections"`
- passed `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~DesignPackageGenerationServiceTests|FullyQualifiedName~ExperienceBlueprintGenerationServiceTests|FullyQualifiedName~DiscoveryDesignStudioAdapterServiceTests|FullyQualifiedName~RecommendationEngineServiceTests|FullyQualifiedName~RecommendationEngineBoundaryTests"`
- passed `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- passed `cd vscode-extension && npm test`
- passed `cd vscode-extension && npm run compile`

## Next Step

- stop here unless a new goal explicitly starts Microsoft Skills integration, CLI integration, provider-backed generation, or downstream discovery-consumer changes
