# AI Proposal Enrichment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an advisory AI proposal-enrichment layer that improves recommendation wording, rationale, prioritization, and expected-outcome explanations without changing deterministic mutation execution.

**Architecture:** Keep `Issues`, `Remediation Queue`, `Fix Opportunity Engine`, and the `Deterministic Mutation Layer` intact. Insert a provider-agnostic `AI Proposal Enrichment` layer between remediation intent and deterministic opportunity presentation, with strong grounding, validation, fallback behavior, and explicit separation between advisory content and executable previews.

**Tech Stack:** TypeScript, React, Jest, VS Code webview UI, existing score-panel payload/state contracts, deterministic PBIR fix-opportunity pipeline, provider abstraction for advisory model calls

---

## File Map

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - add advisory enrichment contracts, provenance, validation, and UI-state shapes
- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentTypes.ts`
  - shared Phase 3 type definitions if `scorePanel.ts` should stay lean
- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentContextBuilder.ts`
  - build grounded deterministic context from findings, remediation items, opportunities, and page/report metadata
- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentOrchestrator.ts`
  - choose enrichers, call providers, run validation, and return advisory output or fallback
- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentProvider.ts`
  - provider abstraction for advisory enrichment only
- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentValidators.ts`
  - reject or downgrade contradictory, unsupported, or hallucinated output
- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentFallbacks.ts`
  - deterministic fallback wording when AI output is unavailable or rejected
- Create: `vscode-extension/src/analyzer/proposalEnrichment/enrichers/layoutEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/enrichers/themeEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/enrichers/navigationEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/enrichers/storytellingEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/enrichers/executiveReadabilityEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/enrichers/accessibilityEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentTelemetry.ts`
  - capture advisory-only timing, refusal, fallback, and validation outcomes
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
  - thread enrichment output into the score-panel payload without changing score semantics
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
  - orchestrate enrichment loading/fallback behavior and keep deterministic fix flows independent
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
  - render enriched proposal copy, priority, expected outcomes, and advisory alternatives
- Create: `vscode-extension/webview-src/analyzer-score/proposalEnrichment.ts`
  - presentation helpers for advisory copy, priority labels, provenance badges, and fallback states
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`
- Create: `vscode-extension/webview-src/analyzer-score/proposalEnrichment.test.ts`
- Create: `vscode-extension/src/test/proposalEnrichmentContextBuilder.test.ts`
- Create: `vscode-extension/src/test/proposalEnrichmentValidators.test.ts`
- Create: `vscode-extension/src/test/proposalEnrichmentOrchestrator.test.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/CHANGELOG.md`
- Modify: `AGENTS.md`
  - add guidance that external Power BI agent skills are research input only and Phase 3 advisory output must map back to deterministic trust-boundary contracts

## Major Workstreams

### Task 1: Lock The Trust Boundary Into Contracts

**Files:**

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Create or Modify: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentTypes.ts`

- [ ] Add advisory enrichment contracts for:
  - `ProposalEnrichment`
  - `ProposalEnricherId`
  - `EnrichedTitleSuggestion`
  - `EnrichedExplanation`
  - `EnrichedImpactSummary`
  - `AdvisoryPriority`
  - `ExpectedOutcomeNarrative`
  - `AdvisoryAlternative`
  - `ProposalEnrichmentValidationResult`
  - `ProposalEnrichmentProvenance`
- [ ] Document contract boundaries explicitly:
  - remediation item = conceptual solution intent
  - proposal enrichment = advisory presentation layer
  - fix opportunity = executable deterministic proposal
  - mutation = actual file edit
- [ ] Add comments stating that Phase 3 advisory output can never contain executable mutation authority.

### Task 2: Add Context-Building Tests First

**Files:**

- Create: `vscode-extension/src/test/proposalEnrichmentContextBuilder.test.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentContextBuilder.ts`

- [ ] Add failing tests proving the context builder can ground enrichment from:
  - normalized findings
  - remediation items
  - affected pages
  - page purpose cues
  - supported deterministic fix categories when available
- [ ] Add failing tests proving it excludes:
  - raw execution internals not meant for prompts
  - score rewrites
  - unsupported mutation claims
  - unrestricted file content dumps
- [ ] Run only the new context-builder tests first and confirm failures are due to missing implementation rather than bad fixtures.

### Task 3: Build The Grounded Context Builder

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentContextBuilder.ts`
- Test: `vscode-extension/src/test/proposalEnrichmentContextBuilder.test.ts`

- [ ] Implement deterministic context shaping for:
  - title suggestion enrichment
  - remediation explanation enrichment
  - why-this-matters enrichment
  - priority/grouping enrichment
  - expected-outcome enrichment
  - advisory-alternative enrichment
- [ ] Keep context bounded, serializable, and explicit about supported versus unsupported execution surfaces.
- [ ] Re-run the focused context-builder tests and confirm green.

### Task 4: Add Provider Abstraction Without Provider Lock-In

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentProvider.ts`
- Create: `vscode-extension/src/test/proposalEnrichmentOrchestrator.test.ts`

- [ ] Define a provider interface for advisory generation only.
- [ ] Ensure the interface carries:
  - grounded input
  - requested enricher scope
  - deterministic metadata for validation
  - refusal/error reporting
- [ ] Keep provider selection outside scoring and outside deterministic fix application.
- [ ] Add failing orchestrator tests that use a mocked provider instead of a real service.

### Task 5: Add Output Validators And Hallucination Guards

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentValidators.ts`
- Create: `vscode-extension/src/test/proposalEnrichmentValidators.test.ts`

- [ ] Add failing tests proving validators reject or downgrade output that:
  - invents visuals, measures, or fields
  - claims deterministic support where none exists
  - contradicts source findings
  - rewrites score, severity, or confidence
  - presents expected outcomes as actual outcomes
- [ ] Implement validation classes such as:
  - `unsupportedSurface`
  - `inventedArtifact`
  - `contradictoryPriority`
  - `executionLeak`
  - `outcomeOverclaim`
- [ ] Re-run validator tests and confirm green.

### Task 6: Add Deterministic Fallback Wording

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentFallbacks.ts`
- Test: `vscode-extension/src/test/proposalEnrichmentOrchestrator.test.ts`

- [ ] Add fallback builders for:
  - title suggestion fallback
  - explanation fallback
  - why-this-matters fallback
  - priority fallback
  - advisory alternatives fallback
- [ ] Ensure fallback output is deterministic, concise, and clearly non-provider-derived.
- [ ] Add tests proving enrichment failure never removes the base remediation workflow.

### Task 7: Build The Orchestrator

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentOrchestrator.ts`
- Test: `vscode-extension/src/test/proposalEnrichmentOrchestrator.test.ts`

- [ ] Implement orchestration flow:
  1. build grounded context
  2. choose enricher scope
  3. invoke provider abstraction
  4. validate output
  5. downgrade or discard invalid sections
  6. merge fallback content where needed
  7. return advisory enrichment with provenance
- [ ] Keep orchestration failure non-blocking for deterministic fix flows.
- [ ] Re-run the orchestrator tests and confirm green.

### Task 8: Add Domain-Specific Enricher Modules

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/enrichers/layoutEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/enrichers/themeEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/enrichers/navigationEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/enrichers/storytellingEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/enrichers/executiveReadabilityEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/enrichers/accessibilityEnricher.ts`

- [ ] Start with one or two enrichers enabled by configuration while defining the shared interface for all six.
- [ ] Keep every enricher bounded to advisory output and shared validation rules.
- [ ] Add focused mocked tests proving enricher routing is deterministic from grounded remediation categories.

### Task 9: Thread Enrichment Into Score Payload

**Files:**

- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`

- [ ] Add payload support for:
  - per-remediation enrichment
  - fallback states
  - provenance badges or flags
  - advisory priority/group labels
  - advisory alternatives
- [ ] Keep score values, severity values, confidence values, and normalized findings unchanged.
- [ ] Add regression tests proving payload shaping omits enrichment cleanly when unavailable.

### Task 10: Add Host Orchestration

**Files:**

- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
- Create or Modify: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentTelemetry.ts`

- [ ] Decide whether enrichment is:
  - eagerly generated during payload build
  - lazily requested on remediation expansion
- [ ] Add orchestration for:
  - loading state
  - fallback state
  - provider failure handling
  - validator downgrade handling
  - telemetry emission
- [ ] Keep deterministic preview/apply/rollback commands independent from Phase 3 advisory loading.

### Task 11: Add Advisory UI Presentation

**Files:**

- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Create: `vscode-extension/webview-src/analyzer-score/proposalEnrichment.ts`

- [ ] Render:
  - enriched explanation copy
  - why-this-matters summaries
  - advisory priority badges
  - expected-outcome narratives
  - advisory alternatives
  - title suggestion candidates where appropriate
- [ ] Clearly differentiate:
  - advisory AI content
  - deterministic preview content
  - deterministic actual outcomes
- [ ] Keep unsupported remediation items honest about execution availability.

### Task 12: Add Webview Tests

**Files:**

- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`
- Create: `vscode-extension/webview-src/analyzer-score/proposalEnrichment.test.ts`

- [ ] Add tests proving:
  - enriched content renders when available
  - fallback content renders when enrichment is rejected or unavailable
  - advisory labels are visible and distinct from deterministic previews
  - expected outcomes are framed as expected, not actual
  - unsupported remediation still shows advisory guidance without executable actions
  - deterministic preview/apply UX remains unchanged when enrichment is toggled on

### Task 13: Preserve Phase 1 And Phase 2 Semantics

**Files:**

- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] Add regression coverage proving Phase 3 does not change:
  - score values
  - severity values
  - confidence values
  - normalized finding semantics
  - deterministic opportunity generation rules
  - preview/apply/rollback/re-analysis requirements
- [ ] Add tests proving enrichment absence or failure does not block deterministic workflows.

### Task 14: Add Telemetry And Debug Evidence

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/proposalEnrichmentTelemetry.ts`

- [ ] Capture:
  - provider latency
  - refusal or failure state
  - fallback usage
  - validator rejection counts
  - enricher IDs used
- [ ] Keep telemetry advisory-only and free of raw sensitive prompt dumps unless explicitly approved by repo policy.

### Task 15: Update Docs

**Files:**

- Modify: `docs/ROADMAP.md`
- Modify: `docs/CHANGELOG.md`
- Modify: `AGENTS.md`

- [ ] Add Phase 3 links and status to `docs/ROADMAP.md`.
- [ ] Keep the roadmap sequence explicit:
  - Phase 1 deterministic engine
  - Phase 2 hardening
  - Phase 3 proposal enrichment
  - Phase 4 advanced AI refactoring
  - Phase 5 report design studio
- [ ] Add AGENTS.md guidance that:
  - external Power BI agent skills are inspiration only
  - no external skill/prompt/code import is allowed
  - Phase 3 advisory output must always preserve the deterministic execution trust boundary
- [ ] Update `docs/CHANGELOG.md` only when implementation actually ships.

## Non-Goals

- no model-generated mutations
- no autonomous editing
- no direct PBIR modifications
- no direct TMDL modifications
- no DAX generation
- no report generation
- no visual creation
- no chart replacement
- no AI execution paths
- no hidden implementation of Phase 4 under Phase 3 wording improvements

## Validation Checklist

- [ ] `cd vscode-extension && npm test`
- [ ] `cd vscode-extension && npm run compile`
- [ ] targeted Jest runs for:
  - `proposalEnrichmentContextBuilder.test.ts`
  - `proposalEnrichmentValidators.test.ts`
  - `proposalEnrichmentOrchestrator.test.ts`
  - `scoreResultPayload.test.ts`
  - `App.test.tsx`
- [ ] targeted lint on changed files if repo-wide lint still has unrelated failures
- [ ] smoke pass proving:
  - enriched advisory content can load
  - enrichment failure falls back cleanly
  - deterministic preview/apply/rollback still works unchanged

## Testing Strategy Notes

- Start with deterministic red/green cycles for context building, validation, fallback handling, and orchestration.
- Use mocked providers for all CI-safe tests.
- Add explicit regression assertions that deterministic execution is unchanged when enrichment is enabled, disabled, or failing.
- Treat UI wording quality as a product contract:
  - grounded
  - concise
  - non-executable
  - clearly labeled

## Rollout Notes

- Ship behind a configuration gate first.
- Enable one or two enrichers before enabling the full domain-specific set.
- Tune validators before broadening rollout.
- Preserve a full fallback experience so the product remains useful with enrichment disabled.

## Execution Notes

- Keep provider logic out of scoring and out of deterministic mutation code.
- Keep enrichment contracts separate from executable fix contracts.
- Preserve narrow file responsibilities:
  - context building
  - provider abstraction
  - validation
  - orchestration
  - UI presentation
- Commit by vertical slice:
  - contracts + context
  - validation + fallback
  - orchestration + payload
  - UI + docs
