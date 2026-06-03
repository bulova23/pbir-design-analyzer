# Fabric Apps Analytics Review Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Evolve PBIR Design Analyzer into an analytics experience review platform that can assess PBIR reports and analytical Fabric Apps through the existing shared workspace without introducing code generation, app generation, or a separate Fabric App workspace.

**Architecture:** Preserve the existing `Overview -> Issues -> Fix Plan -> Evidence -> Export` workspace, the normalized findings model, governance patterns, and the AI-fix trust boundary. Introduce an `Analyzable Surface` layer, `Surface Discovery`, an `Analyzer Registry`, `Analyzer Profiles`, and two new analyzer capabilities: `Fabric App Readiness Analyzer` for PBIR report surfaces and `Fabric App Review Analyzer` for Fabric App surfaces.

**Tech Stack:** TypeScript, React, VS Code extension host/webview, Jest, existing PBIR analyzer payload contracts, existing screenshot audit architecture, existing governance and proposal-enrichment layers, .NET 8 backend for current PBIR scoring

---

## Scope Guardrails

Implement only analyzer and workspace review capabilities for analytical Fabric Apps.

Do:

- keep one workspace
- keep multiple analyzers
- keep advisory-first Fabric App review
- keep normalized findings as the shared issue model
- keep deterministic preview/apply/rollback scoped to supported PBIR edits only

Do not:

- generate Fabric App code
- scaffold Rayfin projects
- create autonomous migration flows
- review GraphQL or backend architecture
- review CRUD or transactional workflows
- mutate Fabric App repos automatically
- create a separate top-level Fabric App workspace

## File Map

### Core Contract And Registry Layer

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - extend shared score-panel contracts for surface metadata, analyzer metadata, readiness outputs, evidence source typing, and governance signal typing
- Create: `vscode-extension/src/analyzer/surfaces/types.ts`
  - define `AnalyzableSurface`, `SurfaceType`, `SurfaceDiscoveryResult`, capability metadata, and unsupported-surface states
- Create: `vscode-extension/src/analyzer/surfaces/discovery.ts`
  - implement surface discovery entry points and ambiguity handling
- Create: `vscode-extension/src/analyzer/surfaces/fabricAppDiscovery.ts`
  - implement Fabric App repo heuristics and minimum-artifact checks
- Create: `vscode-extension/src/analyzer/analyzers/types.ts`
  - define `AnalyzerType`, `AnalyzerProfileId`, and registry contracts
- Create: `vscode-extension/src/analyzer/analyzers/registry.ts`
  - register supported analyzers by surface type and expose profile availability

### Phase 1 Readiness Analysis

- Create: `vscode-extension/src/analyzer/fabric/readiness/readinessTypes.ts`
  - define readiness score bands, page states, blockers, unsupported patterns, and output contract helpers if `scorePanel.ts` becomes too large
- Create: `vscode-extension/src/analyzer/fabric/readiness/readinessAnalyzer.ts`
  - derive readiness outputs from PBIR score payloads and normalized findings
- Create: `vscode-extension/src/analyzer/fabric/readiness/readinessScoring.ts`
  - implement deterministic readiness scoring and banding
- Create: `vscode-extension/src/analyzer/fabric/readiness/readinessEvidence.ts`
  - shape PBIR-derived portability evidence
- Create: `vscode-extension/src/analyzer/fabric/readiness/readinessFindings.ts`
  - convert readiness output into normalized findings and remediation guidance

### Workspace Integration Layer

- Modify: `vscode-extension/src/analyzer/score/overviewSummary.ts`
  - allow additive analyzer-specific overview cards and readiness summaries
- Modify: `vscode-extension/src/analyzer/score/fixPlan.ts`
  - incorporate advisory migration-preparation remediation items
- Modify: `vscode-extension/src/analyzer/score/normalizedFindings.ts`
  - widen evidence provenance and analyzer metadata support without replacing the current model
- Modify: `vscode-extension/src/analyzer/score/presentation.ts`
  - thread analyzer/surface-aware summary content into the shared workspace model
- Modify: `vscode-extension/src/analyzer/score/personaPresentation.ts`
  - register new analyzer-profile combinations such as `migrationReadiness` and `fabricAppQuality`
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
  - orchestrate surface metadata, analyzer selection outputs, readiness outputs, and future Fabric App review payload shaping
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
  - host-side flow for discovery, analyzer selection, profile selection, and messaging to the webview
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
  - render analyzer-specific overview, issues, fix-plan, and evidence states inside the existing workspace
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
  - style additive analyzer-specific badges, cards, and evidence groupings without changing the workspace structure

### Phase 2 Fabric App Review Layer

- Create: `vscode-extension/src/analyzer/fabric/review/reviewTypes.ts`
  - define Fabric App review evidence, findings, and governance signal contracts
- Create: `vscode-extension/src/analyzer/fabric/review/fabricAppReviewAnalyzer.ts`
  - orchestrate analytical Fabric App repo review
- Create: `vscode-extension/src/analyzer/fabric/review/repoEvidence.ts`
  - coordinate evidence extraction from repo files
- Create: `vscode-extension/src/analyzer/fabric/review/typescriptEvidence.ts`
  - extract analytics-UX-relevant TypeScript evidence
- Create: `vscode-extension/src/analyzer/fabric/review/navigationEvidence.ts`
  - derive navigation patterns and issues
- Create: `vscode-extension/src/analyzer/fabric/review/designTokenEvidence.ts`
  - extract design token and CSS variable evidence
- Create: `vscode-extension/src/analyzer/fabric/review/screenshotEvidence.ts`
  - reuse current screenshot-audit session structures where possible
- Create: `vscode-extension/src/analyzer/fabric/review/semanticModelEvidence.ts`
  - shape semantic-model-backed interaction evidence from bounded repo artifacts
- Create: `vscode-extension/src/analyzer/fabric/review/reviewFindings.ts`
  - convert extracted evidence into normalized findings, remediation guidance, and governance signals

### Governance Integration Layer

- Create: `vscode-extension/src/analyzer/governance/analyticsGovernanceTypes.ts`
  - define analytics-focused governance signal contracts across surfaces
- Create: `vscode-extension/src/analyzer/governance/analyticsGovernanceRules.ts`
  - implement token, navigation, accessibility, and semantic-model-backed experience standards
- Modify: `vscode-extension/src/analyzer/score/governanceExport.ts`
  - support additive governance signal export where analyzer outputs are available

### Tests

- Create: `vscode-extension/src/test/surfaceDiscovery.test.ts`
- Create: `vscode-extension/src/test/analyzerRegistry.test.ts`
- Create: `vscode-extension/src/test/readinessAnalyzer.test.ts`
- Create: `vscode-extension/src/test/readinessScoring.test.ts`
- Create: `vscode-extension/src/test/readinessFindings.test.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Modify: `vscode-extension/src/test/personaPresentation.test.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`
- Create: `vscode-extension/src/test/fabricAppDiscovery.test.ts`
- Create: `vscode-extension/src/test/fabricAppReviewAnalyzer.test.ts`
- Create: `vscode-extension/src/test/typescriptEvidence.test.ts`
- Create: `vscode-extension/src/test/navigationEvidence.test.ts`
- Create: `vscode-extension/src/test/designTokenEvidence.test.ts`
- Create: `vscode-extension/src/test/screenshotEvidence.test.ts`
- Create: `vscode-extension/src/test/semanticModelEvidence.test.ts`
- Create: `vscode-extension/src/test/analyticsGovernanceRules.test.ts`

### Docs And Durable Memory

- Modify: `docs/ROADMAP.md`
  - place Fabric Apps Analytics Review relative to AI Proposal Enrichment, Visual Intelligence, Enterprise Governance, and Report Design Studio
- Modify: `docs/CHANGELOG.md`
  - add release-facing notes when implementation ships
- Modify: `AGENTS.md`
  - add durable analyzer/surface boundary guidance if implementation introduces new repo conventions
- Update durable memory on implementation sessions:
  - `.agent-memory/current-focus.md`
  - `.agent-memory/session-summaries.md`
  - timestamped `.agent-memory/sessions/*`

## Dependency Map

### Existing Foundations This Initiative Reuses

- `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - current workspace and normalized finding contracts
- `vscode-extension/src/views/scoreResultPayload.ts`
  - current payload shaping seam
- `vscode-extension/src/views/PbirScorePanel.ts`
  - host orchestration seam
- `vscode-extension/webview-src/analyzer-score/App.tsx`
  - current shared workspace UI
- `vscode-extension/src/analyzer/score/normalizedFindings.ts`
  - shared issue model derivation
- `vscode-extension/src/analyzer/score/overviewSummary.ts`
  - shared overview summarization
- `vscode-extension/src/analyzer/score/fixPlan.ts`
  - shared remediation queue
- `vscode-extension/src/analyzer/audit/session.ts`
  - screenshot evidence session model
- `vscode-extension/src/analyzer/audit/types.ts`
  - screenshot audit evidence primitives
- `vscode-extension/src/analyzer/score/personaPresentation.ts`
  - existing review modes / profile-like emphasis
- `vscode-extension/src/analyzer/proposalEnrichment/*`
  - advisory-first AI trust boundary patterns
- `vscode-extension/src/analyzer/fixes/*`
  - deterministic PBIR-only fix boundary

### Adjacent Roadmap Dependencies

- AI Proposal Enrichment
  - reuse advisory-only enrichment patterns for future Fabric App wording, but do not couple initial implementation to provider-backed enrichment
- AI Fixes
  - maintain strict separation: readiness and Fabric App review stay advisory, deterministic mutation remains PBIR-only
- Visual Intelligence
  - reuse screenshot evidence architecture and leave overlay/annotation expansion to the Visual Intelligence roadmap
- Enterprise Governance
  - implement only analytics-focused governance signals now; keep broader organization-governance platform work in its own roadmap item
- Report Design Studio
  - generated artifacts must re-enter through the same surface/analyzer architecture later; generation is not part of this plan

## Major Workstreams

### Phase 1: Surface Discovery Foundation

**Outcome:** The platform can identify `PBIR report`, `Fabric App`, and future surfaces before analyzer selection, expose supported analyzers and profiles, and fail clearly when discovery is ambiguous.

### Task 1: Extend shared contracts for surfaces, analyzers, and profiles

**Files:**
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Create: `vscode-extension/src/analyzer/surfaces/types.ts`
- Create: `vscode-extension/src/analyzer/analyzers/types.ts`
- Test: `vscode-extension/src/test/analyzerRegistry.test.ts`

- [ ] Add failing contract-oriented tests for:
  - `AnalyzableSurface`
  - `SurfaceType`
  - `SurfaceDiscoveryResult`
  - `AnalyzerType`
  - `AnalyzerProfileId`
  - unsupported or ambiguous discovery states
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/analyzerRegistry.test.ts` and confirm failure.
- [ ] Implement the base contract layer with explicit comments stating:
  - surface = thing being reviewed
  - analyzer = review engine operating on a surface
  - profile = emphasis lens on analyzer output
- [ ] Re-run the focused contract test and confirm it passes.

### Task 2: Build the surface discovery service

**Files:**
- Create: `vscode-extension/src/analyzer/surfaces/discovery.ts`
- Modify: `vscode-extension/src/analyzer/project/discovery.ts`
- Create: `vscode-extension/src/test/surfaceDiscovery.test.ts`

- [ ] Add failing tests for:
  - PBIR project resolves to `pbirReport`
  - Fabric App repo resolves to `fabricApp`
  - unknown folder resolves to `unsupported`
  - mixed or ambiguous folders resolve to `ambiguous`
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/surfaceDiscovery.test.ts` and confirm failure.
- [ ] Implement a discovery entry point that:
  - normalizes the selected path
  - delegates to PBIR and Fabric heuristics
  - returns explicit ambiguity and unsupported states
- [ ] Re-run the focused discovery test and confirm it passes.

### Task 3: Add Fabric App repo heuristics and minimum-artifact checks

**Files:**
- Create: `vscode-extension/src/analyzer/surfaces/fabricAppDiscovery.ts`
- Create: `vscode-extension/src/test/fabricAppDiscovery.test.ts`

- [ ] Add failing tests for:
  - repo with expected Fabric App indicators qualifies as `fabricApp`
  - repo missing minimum artifacts returns `unsupported`
  - repo with partial indicators returns `ambiguous`
- [ ] Define minimum analyzable Fabric App requirements in tests first:
  - required repo markers
  - required analytics-UX-relevant source presence
  - optional screenshot or semantic-model evidence sources
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/fabricAppDiscovery.test.ts` and confirm failure.
- [ ] Implement heuristic detection and clear unsupported-state messages.
- [ ] Re-run the focused Fabric App discovery test and confirm it passes.

### Task 4: Add analyzer registry and analyzer selection logic

**Files:**
- Create: `vscode-extension/src/analyzer/analyzers/registry.ts`
- Create: `vscode-extension/src/test/analyzerRegistry.test.ts`
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`

- [ ] Expand failing tests to prove:
  - `PBIR Analyzer` and `Fabric App Readiness Analyzer` are available for PBIR report surfaces
  - `Fabric App Review Analyzer` is available for Fabric App surfaces
  - analyzer profiles differ by analyzer type
  - unsupported surfaces expose no executable analyzer options
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/analyzerRegistry.test.ts` and confirm failure.
- [ ] Implement registry-driven analyzer selection plus explicit profile availability.
- [ ] Re-run the focused registry test and confirm it passes.

### Phase 2: Fabric App Readiness Assessment

**Outcome:** PBIR reports can be analyzed for migration readiness into analytical Fabric Apps, with deterministic readiness scoring, candidate-page identification, blockers, unsupported patterns, and evidence-backed advisory outputs.

### Task 5: Define the readiness output contract and readiness score bands

**Files:**
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Create: `vscode-extension/src/analyzer/fabric/readiness/readinessTypes.ts`
- Create: `vscode-extension/src/test/readinessScoring.test.ts`

- [ ] Add failing tests for:
  - `overallReadinessScore`
  - `readinessBand`
  - page-level `candidateState`
  - `blockers`
  - `unsupportedPatterns`
  - `recommendedNextActions`
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/readinessScoring.test.ts` and confirm failure.
- [ ] Implement the additive readiness contract and score-band helpers.
- [ ] Re-run the focused readiness scoring test and confirm it passes.

### Task 6: Build deterministic readiness scoring

**Files:**
- Create: `vscode-extension/src/analyzer/fabric/readiness/readinessScoring.ts`
- Modify: `vscode-extension/src/test/readinessScoring.test.ts`

- [ ] Expand tests to cover:
  - strong candidate pages
  - redesign-required pages
  - keep-as-report pages
  - blocker-heavy pages
  - unsupported patterns lowering readiness
- [ ] Implement deterministic readiness rules using existing PBIR score state, metadata, and normalized findings.
- [ ] Keep the scoring advisory-only and do not imply conversion guarantees.
- [ ] Re-run the focused readiness scoring tests and confirm they pass.

### Task 7: Build readiness evidence and findings adapters

**Files:**
- Create: `vscode-extension/src/analyzer/fabric/readiness/readinessEvidence.ts`
- Create: `vscode-extension/src/analyzer/fabric/readiness/readinessFindings.ts`
- Create: `vscode-extension/src/test/readinessFindings.test.ts`

- [ ] Add failing tests for:
  - portability evidence from PBIR interactions and navigation
  - migration blockers mapped into normalized findings
  - candidate-page opportunities mapped into remediation guidance
  - unsupported patterns represented as advisory issues, not executable fixes
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/readinessFindings.test.ts` and confirm failure.
- [ ] Implement evidence shaping and normalized finding conversion.
- [ ] Re-run the focused readiness findings test and confirm it passes.

### Task 8: Implement the Fabric App Readiness Analyzer

**Files:**
- Create: `vscode-extension/src/analyzer/fabric/readiness/readinessAnalyzer.ts`
- Create: `vscode-extension/src/test/readinessAnalyzer.test.ts`

- [ ] Add failing tests proving the analyzer:
  - accepts PBIR report surfaces only
  - produces the readiness output contract
  - emits analyzer metadata and supported profiles
  - stays advisory-first
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/readinessAnalyzer.test.ts` and confirm failure.
- [ ] Implement the analyzer orchestrator and registry hookup.
- [ ] Re-run the focused readiness analyzer test and confirm it passes.

### Phase 3: Workspace Integration

**Outcome:** Readiness outputs render through the existing `Overview`, `Issues`, `Fix Plan`, and `Evidence` workspace architecture without introducing a new workspace.

### Task 9: Thread surface and analyzer metadata through the payload

**Files:**
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`

- [ ] Add failing payload tests for:
  - surface metadata
  - analyzer metadata
  - analyzer profile metadata
  - readiness summary payloads
  - backward compatibility when readiness data is absent
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/scoreResultPayload.test.ts` and confirm failure.
- [ ] Implement payload threading without changing existing score semantics.
- [ ] Re-run the focused payload tests and confirm they pass.

### Task 10: Reuse overview, issues, fix plan, and evidence builders

**Files:**
- Modify: `vscode-extension/src/analyzer/score/overviewSummary.ts`
- Modify: `vscode-extension/src/analyzer/score/normalizedFindings.ts`
- Modify: `vscode-extension/src/analyzer/score/fixPlan.ts`
- Modify: `vscode-extension/src/analyzer/score/presentation.ts`
- Modify: `vscode-extension/src/analyzer/score/personaPresentation.ts`

- [ ] Add focused tests where needed for:
  - readiness overview cards
  - readiness issue grouping
  - advisory migration remediation items
  - new analyzer profiles such as `migrationReadiness`
- [ ] Keep all changes additive and presentation-driven.
- [ ] Confirm normalized findings remain the shared issue model.
- [ ] Re-run focused builder tests plus persona tests and confirm they pass.

### Task 11: Add host and webview integration for readiness analysis

**Files:**
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] Add failing webview tests proving:
  - readiness overview appears inside the existing workspace
  - readiness issues render as normal issues
  - readiness fix-plan items remain advisory
  - readiness evidence appears inside `Evidence`
  - no separate Fabric App workspace is introduced
- [ ] Run `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx` and confirm failure.
- [ ] Implement host orchestration and webview rendering for readiness analysis.
- [ ] Re-run the focused webview tests and confirm they pass.

### Phase 4: Fabric App Review Mode

**Outcome:** Fabric App repos can be treated as analyzable surfaces, with analytics-UX-focused repo evidence extraction that converts into normalized findings inside the shared workspace.

### Task 12: Define Fabric App review contracts

**Files:**
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Create: `vscode-extension/src/analyzer/fabric/review/reviewTypes.ts`
- Create: `vscode-extension/src/test/fabricAppReviewAnalyzer.test.ts`

- [ ] Add failing tests for:
  - Fabric App review evidence source types
  - analytics-specific governance signals
  - unsupported-state handling when repo artifacts are incomplete
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/fabricAppReviewAnalyzer.test.ts` and confirm failure.
- [ ] Implement the additive review-contract layer.
- [ ] Re-run the focused review-contract tests and confirm they pass.

### Task 13: Build bounded repo evidence extraction

**Files:**
- Create: `vscode-extension/src/analyzer/fabric/review/repoEvidence.ts`
- Create: `vscode-extension/src/analyzer/fabric/review/typescriptEvidence.ts`
- Create: `vscode-extension/src/analyzer/fabric/review/navigationEvidence.ts`
- Create: `vscode-extension/src/analyzer/fabric/review/designTokenEvidence.ts`
- Create: `vscode-extension/src/analyzer/fabric/review/semanticModelEvidence.ts`
- Create: `vscode-extension/src/test/typescriptEvidence.test.ts`
- Create: `vscode-extension/src/test/navigationEvidence.test.ts`
- Create: `vscode-extension/src/test/designTokenEvidence.test.ts`
- Create: `vscode-extension/src/test/semanticModelEvidence.test.ts`

- [ ] Add failing tests proving extraction stays bounded to analytics UX concerns:
  - TypeScript layout and KPI structure
  - route and navigation flow
  - design-token and CSS variable usage
  - semantic-model-backed interaction evidence
  - unsupported or absent artifact handling
- [ ] Run the focused extraction tests and confirm failure:
  - `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/typescriptEvidence.test.ts src/test/navigationEvidence.test.ts src/test/designTokenEvidence.test.ts src/test/semanticModelEvidence.test.ts`
- [ ] Implement extractors that ignore generic software-engineering concerns outside the approved scope.
- [ ] Re-run the focused extraction tests and confirm they pass.

### Task 14: Reuse screenshot evidence architecture for Fabric App review

**Files:**
- Create: `vscode-extension/src/analyzer/fabric/review/screenshotEvidence.ts`
- Modify if needed: `vscode-extension/src/analyzer/audit/types.ts`
- Modify if needed: `vscode-extension/src/analyzer/audit/session.ts`
- Create: `vscode-extension/src/test/screenshotEvidence.test.ts`

- [ ] Add failing tests proving:
  - existing screenshot session structures can be referenced by Fabric App review findings
  - screenshot evidence can be linked without requiring the future Visual Intelligence overlay architecture
  - missing screenshots degrade gracefully
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/screenshotEvidence.test.ts` and confirm failure.
- [ ] Implement screenshot evidence shaping by reusing the current audit/session model rather than creating a parallel screenshot system.
- [ ] Re-run the focused screenshot evidence test and confirm it passes.

### Task 15: Implement Fabric App review findings and analyzer orchestration

**Files:**
- Create: `vscode-extension/src/analyzer/fabric/review/reviewFindings.ts`
- Create: `vscode-extension/src/analyzer/fabric/review/fabricAppReviewAnalyzer.ts`
- Modify: `vscode-extension/src/analyzer/analyzers/registry.ts`
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`

- [ ] Add failing tests proving the analyzer:
  - accepts Fabric App surfaces only
  - produces normalized findings
  - produces evidence and governance signals
  - exposes analyzer profile support such as `fabricAppQuality`
- [ ] Implement the Fabric App review analyzer and registry integration.
- [ ] Re-run the focused review analyzer tests and confirm they pass.

### Task 16: Render Fabric App review in the existing workspace

**Files:**
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] Add failing webview tests proving:
  - Fabric App findings render inside the existing workspace
  - design-token, navigation, accessibility, semantic-model, and screenshot findings group into the normal `Issues` and `Evidence` surfaces
  - no deterministic repo-mutation actions appear
- [ ] Implement the UI integration without changing the workspace structure.
- [ ] Re-run the focused webview tests and confirm they pass.

### Phase 5: Governance Integration

**Outcome:** Analytics-focused governance signals can be produced across readiness and Fabric App review without drifting into generic app-platform governance.

### Task 17: Define analytics-governance contracts and rule scopes

**Files:**
- Create: `vscode-extension/src/analyzer/governance/analyticsGovernanceTypes.ts`
- Create: `vscode-extension/src/analyzer/governance/analyticsGovernanceRules.ts`
- Create: `vscode-extension/src/test/analyticsGovernanceRules.test.ts`

- [ ] Add failing tests for:
  - design-token standards
  - navigation standards
  - accessibility standards
  - semantic-model-backed experience standards
  - unsupported-state behavior when required evidence is missing
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/analyticsGovernanceRules.test.ts` and confirm failure.
- [ ] Implement the analytics-governance rule layer and keep it bounded away from backend infrastructure or GraphQL concerns.
- [ ] Re-run the focused governance rules test and confirm it passes.

### Task 18: Integrate governance outputs into analyzer results and export seams

**Files:**
- Modify: `vscode-extension/src/analyzer/fabric/readiness/readinessAnalyzer.ts`
- Modify: `vscode-extension/src/analyzer/fabric/review/fabricAppReviewAnalyzer.ts`
- Modify: `vscode-extension/src/analyzer/score/governanceExport.ts`
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`

- [ ] Add failing tests proving governance signals can be attached to readiness and Fabric App review output.
- [ ] Keep the existing governance patterns intact and additive.
- [ ] Re-run the focused payload and governance export tests and confirm they pass.

### Phase 6: Hardening And Validation

**Outcome:** Surface discovery, analyzer selection, readiness derivation, Fabric App review evidence extraction, and shared workspace rendering are stable enough for release planning.

### Task 19: Run focused unit and webview validation

**Files:**
- No code changes required

- [ ] Run:
  - `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/surfaceDiscovery.test.ts src/test/analyzerRegistry.test.ts src/test/readinessAnalyzer.test.ts src/test/readinessScoring.test.ts src/test/readinessFindings.test.ts src/test/fabricAppDiscovery.test.ts src/test/fabricAppReviewAnalyzer.test.ts src/test/typescriptEvidence.test.ts src/test/navigationEvidence.test.ts src/test/designTokenEvidence.test.ts src/test/semanticModelEvidence.test.ts src/test/screenshotEvidence.test.ts src/test/analyticsGovernanceRules.test.ts src/test/scoreResultPayload.test.ts`
  - `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`
- [ ] Confirm all focused tests pass before broader validation.

### Task 20: Run broader regression validation

**Files:**
- No code changes required

- [ ] Run:
  - `cd vscode-extension && npm run compile`
  - `cd vscode-extension && npm test`
- [ ] Run targeted ESLint on changed files if repo-wide lint still has known unrelated failures.
- [ ] Confirm no regressions in:
  - normalized findings behavior
  - persona presentation behavior
  - proposal enrichment behavior
  - PBIR deterministic fix workflow behavior

### Task 21: Validate AI trust-boundary and workflow invariants

**Files:**
- No code changes required

- [ ] Add or verify regression coverage proving:
  - Fabric App readiness remains advisory-only
  - Fabric App review remains advisory-only
  - deterministic preview/apply/rollback remains PBIR-only
  - no Fabric App repo mutation actions appear in the workspace
  - proposal enrichment stays downstream and non-authoritative if later enabled for these analyzers

## Open Planning Questions

### 1. Surface discovery strategy

Plan decision:

- implement automatic detection first
- provide explicit user override when discovery returns `ambiguous`
- fail closed rather than guessing for unsupported or partially matched surfaces

Recommended implementation note:

- keep discovery pure and deterministic
- return user-facing reason codes for ambiguity
- do not hide fallback decisions inside analyzer logic

### 2. Minimum analyzable Fabric App requirements

Plan decision:

- require bounded repo evidence before treating a repo as a supported `Fabric App` surface
- screenshots and semantic-model evidence remain optional but should improve evidence quality

Recommended minimum Version 1 requirements:

- repo indicators consistent with analytical Fabric App structure
- analytics-facing TypeScript or frontend source files
- navigation or layout-related source presence
- at least one analyzable evidence path for design token, navigation, or semantic-model-backed interaction review

Unsupported states should include:

- repo lacks sufficient indicators
- repo has only generic frontend scaffolding with no analytics surface evidence
- repo appears operational or CRUD-oriented rather than analytical

### 3. Governance Analyzer roadmap

Plan decision:

- keep `Governance Analyzer` conceptual in the architecture and partial in implementation
- implement analytics-governance rule derivation as a shared layer first
- do not create a full standalone governance analyzer UI slice in the first implementation wave

Relationship to Fabric App review:

- readiness and Fabric App review analyzers should emit governance signals
- those signals should feed the existing workspace and export surfaces
- a separate dedicated governance analyzer can be revisited later if roadmap pressure warrants it

### 4. Screenshot evidence integration

Plan decision:

- reuse existing screenshot audit architecture now
- do not wait for full Visual Intelligence overlays
- keep screenshot evidence typed and linkable so future Visual Intelligence can enrich it later

Relationship to Visual Intelligence:

- this initiative consumes current screenshot evidence primitives
- the Visual Intelligence roadmap can later add overlays, annotations, and stronger visual navigation on top of the same evidence model

## Risk Assessment

### Architectural Risks

- surface discovery can become too heuristic and produce misleading analyzer choices
- analyzer contracts can bloat `scorePanel.ts` if new types are not split into focused modules
- Fabric App review can drift into generic frontend linting unless evidence extractors stay tightly bounded

### Scope Creep Risks

- migration readiness can be mistaken for automatic conversion planning
- Fabric App review can expand into backend, GraphQL, or operational-app concerns
- governance can widen into generic application governance instead of analytics governance
- screenshot review can overreach into future Visual Intelligence scope

### Product Risks

- users may expect deterministic fix actions for Fabric App findings
- users may expect code generation because Fabric Apps are code-first
- ambiguous repo discovery can confuse users if failure states are unclear

### Mitigations

- keep discovery explicit and reason-coded
- keep analyzer scope encoded in tests
- keep advisory-first wording prominent in payloads and UI
- reuse current workspace rather than adding parallel surfaces
- add regression tests proving PBIR deterministic fixes remain isolated

## Testing Strategy

### Unit Tests

- surface discovery
- Fabric App repo heuristics
- analyzer registry
- readiness scoring
- readiness findings conversion
- TypeScript evidence extraction
- navigation evidence extraction
- design-token evidence extraction
- semantic-model evidence extraction
- screenshot evidence shaping
- analytics-governance rules

### Payload And Integration Tests

- score result payload compatibility
- analyzer metadata threading
- readiness payload threading
- Fabric App review payload threading

### Webview Tests

- shared workspace rendering for readiness analysis
- shared workspace rendering for Fabric App review
- advisory-only behavior
- absence of deterministic repo-edit affordances

### Regression Tests

- normalized findings still drive `Issues`
- `Fix Plan` remains shared and advisory for Fabric App analyzers
- `Evidence` remains the place for code-derived and screenshot-derived support
- proposal enrichment and deterministic PBIR fixes remain behaviorally unchanged

## Rollout Recommendation

### Recommended first implementation slice

Ship a narrow vertical slice:

1. surface discovery foundation
2. analyzer registry and profile metadata
3. Phase 1 Fabric App Readiness Analyzer on PBIR report surfaces
4. readiness workspace integration in `Overview`, `Issues`, `Fix Plan`, and `Evidence`

Why first:

- strongest fit with the current product
- lowest risk of scope drift
- validates the `Analyzable Surface` architecture before direct Fabric App repo review
- answers the most immediate business question: `Should this report become a Fabric App?`

### Recommended second implementation slice

Add Fabric App Review Mode with bounded repo evidence extraction after the readiness architecture is stable.

### Recommended third implementation slice

Expand analytics-governance rules and exports once both readiness and direct app review are in place.

## Suggested Roadmap Placement

Place Fabric Apps Analytics Review as a new analyzer-capability roadmap initiative that sits:

- after the core `0.4.0` workspace and AI proposal-enrichment foundation
- adjacent to Visual Intelligence and Enterprise Governance
- before any Report Design Studio review coupling

Recommended positioning:

1. Fabric Apps Analytics Review Phase 1
2. Fabric Apps Analytics Review Phase 2
3. Visual Intelligence integration follow-up
4. deeper Enterprise Governance follow-up
5. Report Design Studio review integration later

This keeps Fabric Apps work framed as:

- analyzer capability
- workspace extension
- platform maturation

Not:

- code generation
- app building
- separate product surface

## Completion Criteria

The initiative is implementation-complete for Version 1 when:

- the platform can identify PBIR report and Fabric App surfaces
- analyzer selection and analyzer profiles are registry-driven
- PBIR report surfaces can produce Fabric App readiness outputs
- readiness outputs render inside the existing workspace
- Fabric App surfaces can produce bounded analytics-review findings
- analytics-governance signals are available for readiness and Fabric App review
- regression tests confirm AI-fix and proposal-enrichment trust boundaries remain intact
