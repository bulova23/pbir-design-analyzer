# Story Assessment 2.2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver Story Assessment 2.2 as a workflow-acceleration release that adds deep link navigation and Story Assessment diff mode without expanding research-stage Story Assessment promotion or changing scoring authority.

**Architecture:** Keep backend scoring authoritative and unchanged in meaning. Build both features in the extension presentation layer: derive reusable navigation targets from safe public payload plus page visual metadata, persist public Story Assessment snapshots in extension-owned storage, and thread additive host/webview state into the existing score-panel workspace.

**Tech Stack:** .NET 8 backend outputs already promoted in 2.1, TypeScript extension host, score-panel protocol guards, React webview, VS Code explorer reveal APIs, extension global storage JSON, Jest, xUnit only when backend-adjacent helpers change

---

## Rollout Decision

Recommended shipping posture:

- Deep Link Navigation can ship independently in Phase 1 if Diff Mode needs more soak time.
- Story Assessment Diff Mode should not block navigation unless the release is explicitly marketed as one bundled 2.2 workflow launch.
- Combined workflow validation is still required before claiming the full story-edit-feedback loop is complete.

Reason:

- navigation depends mainly on payload shaping and host/webview behavior
- diff mode adds persistence, snapshot lifecycle, and comparison semantics
- these have different regression profiles and should have separate ship gates

## File Map

### Extension Contracts And Protocol

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Modify: `vscode-extension/src/views/scorePanelProtocol.ts`

### Extension Presentation And Payload

- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Create: `vscode-extension/src/analyzer/score/navigationTargets.ts`
- Create: `vscode-extension/src/analyzer/score/storyAssessmentSnapshot.ts`

### Extension Host

- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
- Modify: `vscode-extension/src/views/pbirExplorerReveal.ts`
- Modify: `vscode-extension/src/providers/PbirTreeProvider.ts`
- Create: `vscode-extension/src/analyzer/score/storyAssessmentSnapshotStore.ts`

### Webview UI

- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

### Tests

- Create: `vscode-extension/src/test/navigationTargets.test.ts`
- Create: `vscode-extension/src/test/storyAssessmentSnapshot.test.ts`
- Create: `vscode-extension/src/test/storyAssessmentSnapshotStore.test.ts`
- Modify: `vscode-extension/src/test/scorePanelProtocol.test.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`

### Documentation And Memory

- Spec: `docs/superpowers/specs/2026-06-12-story-assessment-2-2-design.md`
- This plan file
- Update repo memory on completion

## Architecture Decisions

### 1. Navigation Target Derivation Lives In The Extension

Do not add backend Story Assessment target metadata in the first 2.2 slice.

Instead:

- derive navigation targets from:
  - `guidedStoryImprovements`
  - `normalizedFindings`
  - `pageScores`
  - `page visual metadata`
- expose those targets through additive score-panel payload fields

This preserves the current promotion boundary and keeps navigation presentation-only.

### 2. Diff Snapshots Persist In Extension Global Storage

Do not use:

- workspace state
- PBIR repo files
- backend storage

Use the existing extension-session pattern:

- hash report path
- store JSON under `context.globalStorageUri.fsPath`
- persist one latest public snapshot per report

### 3. One Shared Navigation Contract

Do not create one message for Story Assessment and another for Issues.

Use one generic `navigateToTarget` message and keep `revealVisual` as a temporary compatibility path if needed during migration.

## Data Model

### Navigation Contract

Add additive navigation types in `scorePanel.ts`:

- `ScorePanelNavigationTargetKind`
- `ScorePanelNavigationTarget`

Add optional navigation target references to:

- `GuidedStoryImprovement`
- `NormalizedFinding`
- `FixPlanItem` if needed for direct reuse

If attaching to all of those creates too much churn, add a shared lookup collection keyed by stable IDs. The preferred path is direct attachment because it reduces join logic inside React.

### Diff Contract

Add additive Story Assessment diff types in `scorePanel.ts`:

- `StoryAssessmentPageSnapshot`
- `StoryAssessmentReportSnapshot`
- `StoryAssessmentDiffResult`

Add optional state fields in `ScorePanelState`:

- `storyAssessmentCurrentSnapshot?`
- `storyAssessmentDiffByPage?`
- `storyAssessmentLastComparedAt?`

These must remain optional so older payloads parse cleanly.

## Task 1: Define The Shared Navigation Contract

**Files:**

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Modify: `vscode-extension/src/test/scorePanelProtocol.test.ts`

- [ ] Add failing tests for additive navigation target types on safe public story items.
- [ ] Define:
  - `ScorePanelNavigationTargetKind`
  - `ScorePanelNavigationTarget`
- [ ] Decide one attachment strategy:
  - direct optional `navigationTarget` on `GuidedStoryImprovement`
  - direct optional `navigationTarget` on `NormalizedFinding`
  - optional `navigationTarget` on `FixPlanItem` only if reused directly in UI
- [ ] Keep all new fields optional and additive.
- [ ] Re-run focused contract and payload tests until the new types compile and old payload expectations still pass.

## Task 2: Add Protocol Support For Generic Target Navigation

**Files:**

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Modify: `vscode-extension/src/views/scorePanelProtocol.ts`
- Modify: `vscode-extension/src/test/scorePanelProtocol.test.ts`

- [ ] Add a failing protocol test for:
  - valid `navigateToTarget` messages
  - invalid target payload rejection
  - backward compatibility for existing messages
- [ ] Add `navigateToTarget` to `ScorePanelWebviewToHostMessagePayload`.
- [ ] Validate target shape conservatively in `parseScorePanelWebviewMessage`.
- [ ] Preserve `revealVisual` parsing during migration unless the migration is completed in the same change.
- [ ] Re-run focused protocol tests until the new message is guarded correctly.

## Task 3: Build The Navigation Target Derivation Helper

**Files:**

- Create: `vscode-extension/src/analyzer/score/navigationTargets.ts`
- Create: `vscode-extension/src/test/navigationTargets.test.ts`
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`

- [ ] Write failing tests for the six validated story improvement categories:
  - missing title/question anchor -> page target
  - missing benchmark/target -> visual target when a lead KPI/chart is identifiable
  - missing benchmark/target -> page fallback when a stable visual cannot be inferred
  - missing prior-period context -> trend visual target
  - missing primary metric -> lead metric visual target
  - missing primary dimension -> comparison visual target
  - scattered filters -> slicer cluster target or page fallback
- [ ] Implement a pure helper that ranks candidate targets from public visual metadata only.
- [ ] Ensure the helper prefers explicit fallback over speculative visual selection.
- [ ] Thread derived targets into payload shaping for Guided Story Improvements and downstream findings.
- [ ] Re-run focused navigation-target and payload tests until they pass.

## Task 4: Extend Explorer Reveal To Support Page And Report Targets

**Files:**

- Modify: `vscode-extension/src/views/pbirExplorerReveal.ts`
- Modify: `vscode-extension/src/providers/PbirTreeProvider.ts`
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`

- [ ] Add focused host tests if this area already has coverage; otherwise add small helper-level tests around target resolution where feasible.
- [ ] Add reveal helpers for:
  - page item -> open `page.json`
  - report root item -> open report-level file
  - existing visual item reuse
- [ ] Handle unsupported targets with a warning message rather than silent failure.
- [ ] Wire `navigateToTarget` in `PbirScorePanel.handleMessage`.
- [ ] Keep the existing `revealVisual` path functional until all call sites are migrated.

## Task 5: Add Story Assessment Navigation UI

**Files:**

- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] Add failing webview tests proving:
  - Top Story Improvements render a navigation action when a target exists
  - missing-target items render explanatory disabled state instead of a dead click
  - clicking the action posts `navigateToTarget`
  - Story Assessment still renders correctly when no target metadata exists
- [ ] Add a compact secondary action in Story Assessment improvement rows.
- [ ] Reuse the same action model in Issues and Fix Plan only where existing UI already supports drill-in actions cleanly.
- [ ] Keep the section readable; do not turn Story Assessment into a button list.
- [ ] Re-run focused webview tests until they pass.

## Task 6: Define The Public Story Snapshot Builder

**Files:**

- Create: `vscode-extension/src/analyzer/score/storyAssessmentSnapshot.ts`
- Create: `vscode-extension/src/test/storyAssessmentSnapshot.test.ts`
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`

- [ ] Write failing tests for snapshot building from current public story payload:
  - captures Story Maturity
  - captures Strong Signals
  - captures Missing Signals
  - captures top story improvement IDs and safe recommendation fields
  - excludes internal Story Assessment fields
- [ ] Implement a pure snapshot builder that derives a stable `StoryAssessmentPageSnapshot` from the normalized payload already used by the webview.
- [ ] Add a report-level snapshot builder that packages current page snapshots plus report metadata.
- [ ] Keep snapshot derivation independent from React and host storage.
- [ ] Re-run focused snapshot tests until they pass.

## Task 7: Implement Snapshot Persistence

**Files:**

- Create: `vscode-extension/src/analyzer/score/storyAssessmentSnapshotStore.ts`
- Create: `vscode-extension/src/test/storyAssessmentSnapshotStore.test.ts`
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`

- [ ] Write failing store tests for:
  - load empty snapshot state for a new report
  - save latest snapshot
  - reload saved snapshot by report path hash
  - gracefully handle malformed persisted JSON
- [ ] Mirror the existing extension storage pattern used by:
  - `intentFeedback/store.ts`
  - `audit/session.ts`
  - `reviewPacketPreviewStore.ts`
- [ ] Load the prior snapshot during score-panel refresh.
- [ ] Save the new snapshot only after a successful normalized payload is available.
- [ ] Re-run focused store tests until they pass.

## Task 8: Build The Diff Comparator

**Files:**

- Create: `vscode-extension/src/analyzer/score/storyAssessmentSnapshot.ts`
- Create: `vscode-extension/src/test/storyAssessmentSnapshot.test.ts`
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`

- [ ] Extend snapshot tests with failing diff expectations for:
  - maturity improvement
  - maturity regression
  - resolved recommendation
  - newly introduced recommendation
  - unchanged recommendation
  - added and removed signal lists
- [ ] Implement a pure comparator that produces `StoryAssessmentDiffResult` from two public snapshots.
- [ ] Add a plain-language summary builder.
- [ ] Thread the current snapshot and optional diff result into score-panel state.
- [ ] Re-run focused snapshot and payload tests until they pass.

## Task 9: Render Diff Mode In Story Assessment

**Files:**

- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] Add failing webview tests proving:
  - diff block does not render when no prior snapshot exists
  - diff block renders when a prior snapshot exists
  - improved, regressed, and unchanged cases read clearly
  - diff rows can reuse navigation actions for unresolved or new recommendations when targets exist
- [ ] Render a compact `What Changed` block inside Story Assessment.
- [ ] Keep the default state embedded and concise, with optional expansion for detail.
- [ ] Preserve the current 2.1 reading order.
- [ ] Re-run focused webview tests until they pass.

## Task 10: Validate Combined Workflow

**Files:**

- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`

- [ ] Add failing integration-style tests for:
  - recommendation -> navigate to target
  - re-analysis -> prior snapshot loaded
  - diff result displayed from prior vs current snapshot
  - unresolved or new recommendations still expose navigation actions
- [ ] Confirm the workflow remains:
  - Story Assessment
  - Top Story Improvements
  - What Changed
  - Issues
  - Fix Plan
- [ ] Re-run focused payload and webview tests until the flow is stable.

## Validation Strategy

Run validation in narrow layers after each material change.

### Focused Validation

- `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/navigationTargets.test.ts src/test/storyAssessmentSnapshot.test.ts src/test/storyAssessmentSnapshotStore.test.ts src/test/scorePanelProtocol.test.ts src/test/scoreResultPayload.test.ts`
- `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`

### Broader Extension Validation

- `cd vscode-extension && npm test`
- `cd vscode-extension && npm run compile`

### Backend Validation

Run backend validation only if shared score contract normalization changes reveal backend-facing assumptions:

- `dotnet test service-dotnet/tests/Tests.csproj -c Release`

Expected posture:

- no backend scoring behavior change
- no new Story Assessment public signal promotion

## Regression Strategy

The implementation must protect against these regressions:

- stale page-selection bugs after page-count changes
- malformed navigation payloads consumed by the host
- deep links choosing the wrong visual when multiple candidates exist
- diff snapshots leaking internal Story Assessment fields
- repo pollution from snapshot files
- old score payloads failing to render because navigation or diff data is absent
- Story Assessment collapsing into a second Issues list

Add explicit regression coverage for each of these.

## Rollout Phases

### Phase 1: Deep Link Navigation

Deliver:

- shared navigation target contract
- payload derivation
- host support for page, visual, and report targets
- Story Assessment navigation actions

Ship gate:

- can move from Story Assessment to a correct target with minimal friction
- unsupported cases degrade cleanly

### Phase 2: Story Assessment Diff Mode

Deliver:

- public snapshot builder
- extension-owned snapshot persistence
- public diff comparator
- embedded Story Assessment diff UI

Ship gate:

- users can tell what improved, regressed, and stayed the same without manual comparison

### Phase 3: Combined Workflow Validation

Deliver:

- recommendation-to-target-to-diff loop validation
- final UX tuning to keep Story Assessment readable

Ship gate:

- the full loop feels faster than the current manual workflow

## Future Compatibility Notes

This plan deliberately sets up reusable seams for future work:

- navigation targets should be reusable by Issues and Fix Plan immediately and by Fabric App Review later
- report-level targets should support future report-root advisory findings
- snapshot storage should be generic enough to hold future public analyzer snapshots, but 2.2 must not generalize prematurely
- if future evidence proves backend-assisted targeting is needed, it can be added as an optional hint without replacing the presentation-layer fallback logic

## Explicit Non-Goals

- Do not expose archetypes, coherence, confidence breakdown, competing stories, or raw signal registry data.
- Do not redesign the Story Assessment 2.1 narrative structure.
- Do not redesign Issues or Fix Plan information architecture.
- Do not add new Story Assessment scoring signals.
- Do not write snapshot metadata into the PBIR repo.
