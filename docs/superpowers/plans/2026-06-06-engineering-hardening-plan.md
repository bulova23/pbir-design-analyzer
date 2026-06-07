# Engineering Hardening & Reliability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Execute a planning-only roadmap for the remaining engineering hardening work across deterministic mutation safety, platform/runtime reliability, and performance/scalability without introducing new product pillars.

**Architecture:** Preserve the current scoring, findings, advisory AI, and deterministic execution boundaries. Use three epics to reduce corruption risk first, then operational inconsistency, then scalability bottlenecks.

**Tech Stack:** TypeScript, React, Jest, VS Code extension host/webview, Node filesystem APIs, existing deterministic PBIR fix pipeline, existing Fabric review analyzers, existing packaging/runtime architecture

---

## Release Guidance

These buckets are recommendations, not commitments.

### Recommended 0.5.1

- Safe Deterministic Fix Engine core hardening
- governance theme verification
- `uploadScreenshots` repair

### Recommended 0.5.2

- output channel consolidation
- namespace unification
- workspace capability declarations
- telemetry decision
- troubleshooting cleanup

### Recommended 0.6.0

- shared repo snapshot
- async filesystem access
- Fabric evidence reuse
- protocol versioning
- selected state validation
- scoring configuration externalization

## Ship-Together Rules

### Must Ship Together

- atomic writes
- rollback-on-failure
- schema-correct PBIR mutation support
- page-name keyed resolution
- post-write validation
- mutation safety regression coverage

Reason:

These form one deterministic trust bundle. Splitting them creates false confidence.

### Should Ship Together

- namespace unification
- command/view/config migration
- docs cleanup tied to renamed identifiers

Reason:

This avoids multiple migrations and repeated user-facing churn.

### Best Bundled Together

- shared repo snapshot
- async filesystem conversion
- Fabric evidence reuse

Reason:

This is one performance architecture pass and should not be partially repeated.

- protocol versioning
- payload/schema guards
- selected state validation

Reason:

These are one message-contract hardening pass.

### Can Ship Independently

- governance theme verification
- `uploadScreenshots` repair
- output channel consolidation
- workspace capability declarations
- telemetry decision
- troubleshooting cleanup

These are operationally useful but do not require the larger architectural bundles.

## File Map

### Epic 1: Safe Deterministic Fix Engine

- `vscode-extension/src/analyzer/fixes/fixApplyEngine.ts`
- `vscode-extension/src/analyzer/fixes/fixMutationPlanner.ts`
- `vscode-extension/src/analyzer/fixes/*` supporting mutation modules
- `vscode-extension/src/test/fixApplyEngine.test.ts`
- `vscode-extension/src/test/fixMutationPlanner.test.ts`
- additional deterministic safety tests to be created alongside the planner/apply/rollback seams

### Epic 2: Platform & Runtime Reliability

- `vscode-extension/src/extension.ts`
- `vscode-extension/src/commands/register.ts`
- `vscode-extension/src/commands/pbirCommands.ts`
- `vscode-extension/src/telemetry/reporter.ts`
- `vscode-extension/package.json`
- `docs/PBIR_TROUBLESHOOTING.md`
- release/runtime docs affected by namespace or capability decisions

### Epic 3: Performance & Scalability

- `vscode-extension/src/analyzer/project/localTree.ts`
- `vscode-extension/src/analyzer/fabric/review/fabricAppReviewAnalyzer.ts`
- Fabric evidence extractor modules under `vscode-extension/src/analyzer/fabric/review/`
- `vscode-extension/src/views/PbirScorePanel.ts`
- `vscode-extension/src/views/scoreResultPayload.ts`
- `vscode-extension/webview-src/analyzer-score/App.tsx`
- analyzer configuration/store seams used for scoring constants

## Workstream 1: Safe Deterministic Fix Engine

**Target bucket:** Recommended `0.5.1`

**Outcome:** Deterministic fixes become structurally trustworthy rather than partially hardened.

### Task 1: Formalize the supported mutation model

- [ ] Define the supported PBIR mutation surface for `0.5.x` explicitly:
  - which properties are valid
  - where those properties live in PBIR JSON
  - which value wrappers and expression forms are required
- [ ] Record which dormant mutation categories remain disabled and why.
- [ ] Confirm that unsupported categories stay blocked instead of falling through to best-effort writes.

### Task 2: Replace ambiguous target resolution

- [ ] Remove planner reliance on ambiguous page display-name matching for mutation targeting.
- [ ] Introduce stable target resolution keyed on PBIR page identity.
- [ ] Add collision fixtures proving duplicate display names do not misroute mutations.

### Task 3: Add schema-correct mutation shaping

- [ ] Refactor the mutation planner so supported non-position edits compile to real PBIR object/property paths.
- [ ] Preserve the boundary that unsupported edits remain rejected, not approximated.
- [ ] Add round-trip fixtures for the supported mutation categories.

### Task 4: Add atomic file-write orchestration

- [ ] Replace direct overwrite writes with temp-file plus rename behavior.
- [ ] Define consistent backup, temp-file naming, cleanup, and failure semantics.
- [ ] Ensure single-opportunity and batch-opportunity paths use the same atomic write contract.

### Task 5: Add rollback-on-failure semantics

- [ ] Define pre-apply backup capture requirements.
- [ ] Make batch apply all-or-nothing when any write or validation step fails.
- [ ] Ensure rollback does not depend on regeneration.

### Task 6: Add post-write validation

- [ ] Validate the structural invariants of the mutated PBIR file after write and before final success reporting.
- [ ] Fail closed when validation does not match the expected mutation intent.
- [ ] Define what constitutes:
  - mutation success
  - mutation partial failure
  - rollback failure

### Task 7: Add format-preserving edit strategy

- [ ] Decide whether supported edits can use surgical patching instead of full JSON reserialization.
- [ ] If full structural formatting preservation is not feasible in `0.5.1`, define an explicit minimal acceptable strategy and document the tradeoff.
- [ ] Add regression checks around diff churn for representative files.

### Task 8: Expand safety tests

- [ ] Add focused planner tests.
- [ ] Add focused apply-engine tests.
- [ ] Add rollback failure-path tests.
- [ ] Add post-write validation tests.
- [ ] Add duplicate-page-name and stale-target fixtures.

### Task 9: Fold in adjacent trust fixes

- [ ] Repair `uploadScreenshots` so it invokes the intended upload workflow rather than rescoring.
- [ ] Replace governance self-reported theme input with PBIR-derived theme verification.

### Validation Strategy

- focused Jest tests for mutation planning, targeting, apply, and rollback
- narrow regression fixtures for duplicate names, stale targets, and unsupported mutations
- compile/typecheck pass
- packaged extension smoke on supported deterministic fix scenarios

### Regression Strategy

- preserve the existing deterministic mutation trust boundary
- prove unsupported mutations still fail closed
- prove score outputs and findings semantics do not change as a side effect of fix-engine hardening

### Rollout Guidance

- ship as one trust-repair release bucket
- do not split atomicity away from schema-correct mutation work
- if format-preserving edits cannot meet the release bar, keep the mutation surface narrower rather than broadening on partial safety

## Workstream 2: Platform & Runtime Reliability

**Target bucket:** Recommended `0.5.2`, with governance theme verification and `uploadScreenshots` already eligible for Recommended `0.5.1`

**Outcome:** Runtime behavior becomes operationally coherent and easier to support.

### Task 1: Consolidate output channels

- [ ] Define the extension’s output-channel model:
  - shared general channel
  - backend-specific channel if needed
- [ ] Remove duplicate channel creation sites.
- [ ] Ensure error paths reuse shared channels instead of creating one-off instances.

### Task 2: Unify identifier namespaces

- [ ] Choose the canonical namespace family for:
  - commands
  - views
  - configuration keys
  - internal IDs where applicable
- [ ] Add compatibility/migration handling for renamed configuration keys.
- [ ] Update references in docs and tests together.

### Task 3: Add workspace capability declarations

- [ ] Decide the supported posture for:
  - untrusted workspaces
  - virtual workspaces
- [ ] Declare those capabilities explicitly in extension metadata.
- [ ] Ensure behavior matches the declared posture.

### Task 4: Make the telemetry posture explicit

- [ ] Decide whether telemetry remains intentionally local/no-op or becomes a real production pipeline in a later milestone.
- [ ] Update the implementation and docs so the runtime behavior matches the product decision.
- [ ] Remove ambiguous partial instrumentation semantics.

### Task 5: Clean up troubleshooting and runtime docs

- [ ] Remove nonexistent command references.
- [ ] Align troubleshooting steps with the actual backend/runtime architecture.
- [ ] Update release/runtime docs after namespace and capability decisions settle.

### Validation Strategy

- targeted command registration tests
- metadata/package validation
- narrow runtime smoke for output-channel behavior and command wiring
- doc review against actual command IDs and supported runtime posture

### Regression Strategy

- preserve command functionality across renamed IDs where migration is promised
- avoid repeated config-key migrations across later releases
- ensure doc cleanup lands in the same change window as namespace cleanup

### Rollout Guidance

- ship namespace, metadata, and doc changes together
- keep this work separate from deeper performance refactors
- if telemetry remains intentionally disabled, document that clearly rather than leaving partial hooks

## Workstream 3: Performance & Scalability

**Target bucket:** Recommended `0.6.0`

**Outcome:** Review flows scale more predictably and the host/webview contract becomes safer to evolve.

### Task 1: Introduce a shared repo snapshot seam

- [ ] Define a shared repository snapshot abstraction for local PBIR/Fabric analysis flows.
- [ ] Ensure the snapshot can be reused across evidence extraction passes instead of rewalking the repo repeatedly.
- [ ] Keep snapshot creation distinct from analyzer logic so multiple analyzers can consume it.

### Task 2: Convert synchronous repo traversal to async access

- [ ] Identify synchronous extension-host filesystem hot paths.
- [ ] Convert the highest-impact repo traversal paths to async equivalents.
- [ ] Preserve graceful degradation behavior on malformed or partial repos.

### Task 3: Reuse Fabric evidence extraction inputs

- [ ] Refactor Fabric review evidence extraction so multiple evidence domains can operate over a shared repository view.
- [ ] Eliminate avoidable repeated repo scans during one analysis run.
- [ ] Keep evidence reuse architectural, not just memoized ad hoc in one analyzer.

### Task 4: Harden the host/webview protocol

- [ ] Add explicit protocol versioning.
- [ ] Add payload/schema guards at the message seam.
- [ ] Fail clearly when host and webview contract versions diverge.

### Task 5: Validate persisted and incoming panel state

- [ ] Clamp or reject stale `selectedPageIndex` values.
- [ ] Validate persisted panel state against the latest score payload shape.
- [ ] Ensure late-arriving state updates do not silently disappear without host knowledge.

### Task 6: Externalize scoring constants

- [ ] Identify hardcoded review/readiness constants that should move into explicit configuration.
- [ ] Define provenance and override boundaries.
- [ ] Preserve deterministic defaults while making the scoring basis inspectable.

### Validation Strategy

- focused tests for snapshot reuse and async traversal behavior
- performance-oriented regression checks on representative repo shapes
- protocol compatibility tests between host payload shaping and webview consumption
- analyzer tests for externalized scoring defaults and overrides

### Regression Strategy

- preserve analyzer output semantics unless a constant externalization change is intentional and documented
- preserve local-tree graceful degradation while async conversion occurs
- ensure protocol guards fail early rather than deep in the render path

### Rollout Guidance

- ship snapshot plus async plus evidence reuse as one performance architecture pass
- ship protocol versioning with state validation together
- avoid interleaving this work with namespace migration or deterministic fix-engine internals

## Cross-Workstream Ordering

### Order 1: Recommended 0.5.1

1. mutation model and target resolution
2. schema-correct mutation shaping
3. atomic writes and rollback-on-failure
4. post-write validation
5. mutation safety regression expansion
6. governance theme verification
7. `uploadScreenshots` repair

### Order 2: Recommended 0.5.2

1. choose canonical namespace family
2. implement namespace migration and metadata updates
3. consolidate output channels around the stable runtime surface
4. add capability declarations
5. make telemetry posture explicit
6. clean up troubleshooting and runtime docs

### Order 3: Recommended 0.6.0

1. shared repo snapshot seam
2. async filesystem conversion
3. Fabric evidence reuse
4. protocol versioning and schema guards
5. selected state validation
6. scoring configuration externalization

## Dependencies And Coordination Notes

### Epic 1 Coordination

- Do not start atomic-write implementation without first stabilizing mutation semantics and target resolution.
- Do not claim deterministic fix trustworthiness until planner, apply, rollback, and validation tests all exist.

### Epic 2 Coordination

- Namespace unification should lead the workstream because docs, metadata, and command IDs all depend on the final naming choice.
- Output-channel consolidation should follow the namespace decision so the runtime surface is cleaned up once.

### Epic 3 Coordination

- Shared snapshot design should precede async conversion, otherwise async work will likely be repeated.
- Protocol versioning should precede state-validation polish so the validation rules attach to a real contract boundary.

## Regression Checklist By Release Bucket

### Recommended 0.5.1

- [ ] deterministic fix tests prove unsupported cases still fail closed
- [ ] batch apply cannot leave partial writes behind
- [ ] rollback path is deterministic from stored state
- [ ] governance theme verification reads PBIR state, not user-entered labels
- [ ] screenshot upload command triggers the intended workflow

### Recommended 0.5.2

- [ ] command/view/config migrations remain backward-safe where promised
- [ ] runtime output routing uses shared channels
- [ ] extension metadata accurately reflects workspace posture
- [ ] docs no longer reference nonexistent commands

### Recommended 0.6.0

- [ ] one analysis run does not rescan the repo unnecessarily
- [ ] extension host avoids blocking synchronous repo traversal on the primary hot paths
- [ ] stale host/webview contract mismatches fail early and clearly
- [ ] selected page state cannot point outside the current payload
- [ ] scoring constants have explicit provenance

## Final Roadmap Guidance

- Treat Recommended `0.5.1` as the trust-restoration release.
- Treat Recommended `0.5.2` as the operational-coherence release.
- Treat Recommended `0.6.0` as the scalability and protocol-maturity release.

If implementation discovery forces reordering later, preserve the bundle logic:

- trust bundle first
- operational bundle second
- architecture-scale bundle third
