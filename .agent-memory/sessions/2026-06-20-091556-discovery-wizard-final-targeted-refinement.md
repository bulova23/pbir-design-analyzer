# 2026-06-20 09:15:56 Discovery Wizard Final Targeted Refinement

## Objective

- implement only the final targeted refinement for:
  - recommendation trust
  - design package fidelity
- fix the consultant-review gaps from:
  - `docs/report-discovery-wizard-validation-review-round8.md`
  - `docs/report-discovery-wizard-consultant-benchmark-review.md`
- stop before:
  - Microsoft Skills integration
  - CLI integration
  - provider-backed generation
  - Design Studio workflow changes
  - Analyzer Workspace changes
  - architecture changes

## Changes

- refined `RecommendationEngineService` with targeted trust shaping for:
  - service command-center versus service workflow routing portfolios
  - investigation-first preservation in forecast-mix root-cause scenarios
  - portfolio-level lead-selection adjustments when the candidate set contains a clearly stronger service workflow or investigation-first option
- removed unsupported KPI fallback injection from `ExperienceBlueprintGenerationService`
- changed blueprint KPI generation to use only semantic-model-supported measures and KPI clusters
- surfaced KPI insufficiency through blueprint provenance ambiguity notes instead of fabricated KPI content
- normalized technical dimension names into consultant-facing filter labels for blueprint and downstream package consumption while keeping technical lineage in provenance
- added focused regression coverage for:
  - service recommendation trust
  - investigation recommendation trust
  - strict KPI fidelity
  - consultant-facing naming
  - rationale and provider-guidance trust

## Validation

- focused:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~RecommendationEngineServiceTests|FullyQualifiedName~ExperienceBlueprintGenerationServiceTests|FullyQualifiedName~DesignPackageGenerationServiceTests"`
- required:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`

## Notes

- full required validation passed
- existing backend nullable warnings outside this scope still appear during `dotnet test`
- one new investigation-trust regression test was tightened to a clearly investigation-dominant mixed forecasting scenario so it asserts the required contract without over-claiming a broader executive-planning edge case that still belongs in a later review if it remains visible in Round 9

## Next Recommended Step

- run **Discovery Wizard Validation Review – Round 9**
- use Round 9 to determine whether:
  - Discovery Wizard MVP is finally complete
  - Design Package quality is now trustworthy enough to start Microsoft Skills / CLI integration design planning
