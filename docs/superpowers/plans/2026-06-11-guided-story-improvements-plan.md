# Guided Story Improvements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver Story Assessment 2.1 as a narrow user-facing Guided Story Improvements workflow built only from the six validated Story Gap categories, while keeping all research-stage Story Assessment signals internal.

**Architecture:** Keep Story Assessment 2.1 contract-safe and advisory-only. The backend shapes a filtered Guided Story Improvements model from validated Story Gap candidates, the extension payload carries only the safe user-facing subset, and the score-panel renders a small subsection between Story Assessment and Issues that also feeds downstream Issues and Fix Plan behavior.

**Tech Stack:** .NET 8 PBIR scoring backend, existing `ScoreResult` and `PageScore` payload shaping, VS Code score-panel protocol guards, React score-panel webview, existing normalized findings and Fix Plan builders, xUnit, Jest, documentation and repo-memory workflows

---

## Rollout Phases

### Phase 1: Backend Shaping

- introduce an internal-to-public filtered Guided Story Improvements model
- map only the six validated Story Gap categories
- keep special-page handling internal as suppression logic

### Phase 2: Contract And Payload

- add the narrow public contract fields
- shape report-level and page-level payloads
- keep all research-stage Story Assessment internals excluded

### Phase 3: Score-Panel Experience

- render Guided Story Improvements below Story Assessment
- connect recommendations to Issues and Fix Plan
- preserve the existing Story Assessment section

### Phase 4: Validation And Regression

- verify contract safety
- verify score-panel placement and wording
- verify Issues and Fix Plan consume the new inputs correctly

## Contract Change Rules

Allowed contract additions:

- Guided Story Improvements subsection payload
- only user-facing recommendation data

Forbidden contract additions:

- archetypes
- semantic coherence
- confidence breakdown
- competing stories
- promotion states
- signal registry
- surface scopes
- raw evidence ids

## File Map

### Backend

- Modify: `service-dotnet/Services/Pbir/Models/ScoreResult.cs`
- Modify: `service-dotnet/Services/Pbir/Models/PageScore.cs`
- Modify: `service-dotnet/Services/Pbir/Models/StoryAssessmentValidationModels.cs`
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`

### Extension Contract And Payload

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Modify: `vscode-extension/src/views/scorePanelProtocol.ts`
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`

### Score-Panel UI

- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

### Issues And Fix Plan Integration

- Modify: `vscode-extension/src/analyzer/score/normalizedFindings.ts`
- Modify: `vscode-extension/src/analyzer/score/fixPlan.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/remediationQueue.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/remediationQueue.test.ts`

### Tests

- Modify: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`
- Modify: `service-dotnet/tests/StoryAssessmentValidationExportTests.cs`
- Modify: `vscode-extension/src/test/scorePanelProtocol.test.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Modify: `vscode-extension/src/test/fixPlan.test.ts`

### Documentation

- This plan file
- Spec: `docs/superpowers/specs/2026-06-11-guided-story-improvements-design.md`
- Future release note/docs updates only after implementation is approved

## Task 1: Define The Public Guided Story Improvements Model

**Files:**

- Modify: `service-dotnet/Services/Pbir/Models/ScoreResult.cs`
- Modify: `service-dotnet/Services/Pbir/Models/PageScore.cs`
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Test: `vscode-extension/src/test/scorePanelProtocol.test.ts`

- [ ] Add a narrow public model for Guided Story Improvements with:
  - `id`
  - `title`
  - `summary`
  - `rationale`
  - `expectedImpact`
  - `priority`
  - `relatedImpactArea`
- [ ] Expose the model only as a user-facing recommendation set, not as raw Story Assessment diagnostics.
- [ ] Define one container section for:
  - `highPriorityImprovements`
  - `mediumPriorityImprovements`
  - `storyImprovementRationale`
- [ ] Add protocol validation so payloads with extra research-stage fields are rejected.
- [ ] Add a failing contract test first that proves:
  - the new subsection parses when only safe fields exist
  - unsafe fields are still absent

## Task 2: Map Validated Story Gaps To Guided Story Improvements

**Files:**

- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Modify: `service-dotnet/Services/Pbir/Models/StoryAssessmentValidationModels.cs`
- Test: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`

- [ ] Add a mapper from validated Story Gap candidates to Guided Story Improvements.
- [ ] Restrict mapping to:
  - missing title/question anchor
  - missing benchmark/target
  - missing prior-period context
  - missing primary metric
  - missing primary dimension
  - scattered filters
- [ ] Write failing backend tests first proving:
  - supported gap ids map to recommendations
  - unsupported gap ids do not map
  - recommendation wording contains no internal signal terminology
- [ ] Keep special-page suppression internal so diagnostic-only pages do not emit user-facing recommendations.

## Task 3: Implement Priority Rules

**Files:**

- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Test: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`

- [ ] Encode default priority rules:
  - High: title/question anchor, benchmark/target, primary metric
  - Medium: prior-period context, primary dimension, scattered filters
- [ ] Add bounded escalation rules when multiple story weaknesses stack on the same page.
- [ ] Add failing tests first for:
  - default High mapping
  - default Medium mapping
  - escalation behavior
  - suppression on diagnostic-only pages

## Task 4: Shape Story Assessment Integration In The Payload

**Files:**

- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Modify: `vscode-extension/src/views/scorePanelProtocol.ts`
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
- Test: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Test: `vscode-extension/src/test/scorePanelProtocol.test.ts`

- [ ] Add payload shaping so Guided Story Improvements appears next to existing Story Assessment data.
- [ ] Preserve the existing Story Assessment block:
  - Detected Story
  - Supported Decision
  - Why This Matters
  - Decision Risk
- [ ] Ensure the new subsection is additive and optional so old payloads still render safely.
- [ ] Add tests proving:
  - Guided Story Improvements appears when present
  - old payloads without it still parse
  - internal Story Assessment fields remain excluded

## Task 5: Render Guided Story Improvements In The Score Panel

**Files:**

- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Test: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] Add a new subsection below Story Assessment and above Issues.
- [ ] Render:
  - `High Priority Improvements`
  - `Medium Priority Improvements`
  - `Story Improvement Rationale`
- [ ] Keep the section compact and consultant-friendly instead of list-heavy.
- [ ] Add tests proving:
  - section placement is correct
  - recommendation order is correct
  - rationale is present
  - no internal diagnostic labels render

## Task 6: Feed Issues From Guided Story Improvements

**Files:**

- Modify: `vscode-extension/src/analyzer/score/normalizedFindings.ts`
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Test: `vscode-extension/src/test/scoreResultPayload.test.ts`

- [ ] Define how Guided Story Improvements enrich or generate normalized story findings.
- [ ] Ensure the source of truth remains Guided Story Improvements, with Issues acting as the downstream issue view.
- [ ] Preserve existing finding categories and impact-area mapping where possible:
  - `storytelling`
  - `benchmark`
  - related story categories already in use
- [ ] Add tests proving:
  - recommendations produce issue-consumable records
  - duplicate issue inflation does not occur
  - internal diagnostics are not leaked into Issues

## Task 7: Feed Fix Plan From Guided Story Improvements

**Files:**

- Modify: `vscode-extension/src/analyzer/score/fixPlan.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/remediationQueue.ts`
- Test: `vscode-extension/src/test/fixPlan.test.ts`
- Test: `vscode-extension/webview-src/analyzer-score/remediationQueue.test.ts`

- [ ] Map Guided Story Improvements to Fix Plan sequencing hints.
- [ ] Keep Fix Plan downstream and remediation-oriented.
- [ ] Use these sequencing assumptions:
  - title/question anchor and benchmark/target early
  - primary metric and primary dimension next
  - prior-period context and scattered filters after the narrative frame is stable
- [ ] Add tests proving:
  - story improvements influence sequencing
  - non-story Fix Plan behavior still works
  - no duplicate remediation items appear from the same recommendation

## Task 8: Add End-To-End Guardrail Tests

**Files:**

- Modify: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] Add regression tests proving the first slice never exposes:
  - archetypes
  - coherence
  - confidence breakdown
  - competing stories
  - promotion states
  - signal registry
- [ ] Add a story-panel rendering regression proving Guided Story Improvements does not collapse into a second Issues list.
- [ ] Add a backward-compatibility regression proving reports without the new fields still render the current score panel correctly.

## Validation Requirements

### Backend

- `dotnet test service-dotnet/tests/Tests.csproj -c Release`

### Extension

- `cd vscode-extension && npm test`
- `cd vscode-extension && npm run compile`

### Focused UI Validation

- confirm Story Assessment renders first
- confirm Guided Story Improvements renders second
- confirm Issues and Fix Plan consume the new signals
- confirm hidden special-page guardrails suppress inappropriate recommendations

## Regression Strategy

- preserve current Story Assessment wording and structure unless the new subsection is present
- preserve current Issues behavior for non-story findings
- preserve current Fix Plan sequencing for non-story findings
- preserve protocol compatibility for existing score payloads
- preserve internal-only protection for all research-stage Story Assessment fields

## Rollout Recommendation

### Release Slice

- ship Guided Story Improvements only
- do not ship broader Story Assessment 2.0 promotion in the same release

### Guardrail

- treat special-page handling as a hidden filter only
- do not expose page-type labels in the first slice

### Follow-Up

- re-run Level 1 review after shipping the first slice
- do not promote archetype, coherence, or confidence surfaces until a broader corpus passes

## Success Criteria

- users can identify the top story improvements for a page quickly
- the new subsection is visibly distinct from Issues and Fix Plan
- the six validated Story Gap categories are the only promoted Story Assessment inputs
- internal Story Assessment research signals remain protected
