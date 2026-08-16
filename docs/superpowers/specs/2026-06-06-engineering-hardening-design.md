# Engineering Hardening & Reliability Design

Date: 2026-06-06

Status: Approved planning direction captured; implementation deferred

## Goal

Create a structured engineering hardening roadmap for the next reliability phase of PBIR Design Analyzer without expanding feature scope.

This design focuses on the remaining open issues after the `0.5.0` release hardening work:

- deterministic fix safety
- platform/runtime reliability
- performance and scalability

The purpose is roadmap guidance, dependency mapping, and release sequencing.

It is not a release commitment.

## Business Context

The product has already expanded into:

- PBIR Review
- Fabric App Readiness
- Fabric App Review
- AI Proposal Enrichment
- Advanced AI Refactoring foundations
- cross-platform packaging

The main remaining risks are no longer isolated defects.

They are structural engineering concerns:

- deterministic mutation trustworthiness
- operational consistency
- extension-host/runtime maturity
- review scalability on larger repos

Further feature expansion should not outrun these foundations.

## Non-Goals

This hardening phase does not:

- redesign AI systems
- redesign Fabric App Review
- redesign Story Assessment
- broaden deterministic mutation scope to new visual-edit categories
- add new user-facing feature pillars

This is engineering hardening only.

## Current State Summary

Resolved since the earlier `0.4.0` review:

- target-specific cross-platform packaging now exists
- Windows ARM64 support now has a working self-contained package
- backend readiness no longer relies on a fake timer
- fix outcome severity evaluation was corrected
- Windows-hostile packaging scripts were removed
- risky title and semantic-color mutations were disabled and regression-tested

Remaining open issues cluster into three coherent epics:

- **Epic 1:** Safe Deterministic Fix Engine
- **Epic 2:** Platform & Runtime Reliability
- **Epic 3:** Performance & Scalability

## Architectural Boundaries

### Permanent Trust Boundary

The deterministic preview/apply/rollback path remains the only execution authority for PBIR mutations.

Hardening work may improve:

- mutation correctness
- write safety
- rollback safety
- validation
- performance
- operational clarity

Hardening work must not introduce:

- AI mutation authority
- best-effort file rewriting without deterministic validation
- non-reversible report edits

### Layer Boundaries To Preserve

- scoring remains authoritative for score outputs and findings
- normalized findings remain the shared issue model
- proposal enrichment remains advisory
- deterministic fix execution remains separate from scoring and AI commentary
- Fabric App Review remains advisory-only
- webviews remain presentation/state consumers, not mutation authorities

### Hardening Principle

Every change in this roadmap should reduce one of these classes of risk:

- corruption risk
- partial-write risk
- runtime ambiguity
- operational inconsistency
- scalability bottleneck

If a proposed change does not reduce one of those risks, it is out of scope for this phase.

## Epic 1: Safe Deterministic Fix Engine

### Problem Statement

The current deterministic fix engine is materially better than the original `0.4.0` state, but it is not yet trustworthy enough to be treated as a hardened mutation system.

The main remaining risks are:

- non-atomic writes
- rollback gaps on partial failure
- format-destructive whole-file rewrites
- dormant schema-incorrect mutation paths
- unstable target resolution when display names collide
- incomplete post-write validation
- insufficient dedicated safety tests around planner/apply interactions

### Scope

Epic 1 includes:

- atomic writes
- temp-file plus rename
- rollback-on-failure
- schema-correct PBIR mutations
- stable page-name keyed resolution
- format-preserving edits
- post-write validation
- `fixMutationPlanner` hardening
- mutation safety testing

Epic 1 excludes:

- new mutation categories beyond the current supported hardening scope
- AI-generated mutation paths
- best-effort silent repair behavior

### Deliverable

A trustworthy deterministic mutation system that can preserve the product’s trust boundary under single-fix and batch-fix execution paths.

### Why These Items Must Ship Together

The fix engine is the strongest coupling point in the roadmap.

The following items should be treated as one integrity bundle:

- schema-correct mutation support
- stable target resolution
- atomic write path
- rollback-on-failure
- post-write validation
- mutation safety tests

Shipping them separately would create misleading confidence:

- atomic writes alone do not help if the mutation path is semantically wrong
- schema-correct mutation support alone does not help if writes can still partially fail
- rollback alone does not help if the validation boundary is weak

### Independent Items Within Epic 1

Two items can ship independently if necessary, but are still best included in the same release bucket:

- `uploadScreenshots` repair
- governance theme verification from PBIR

They do not depend on deterministic mutation internals, but they fit the same `0.5.1` risk-reduction story because they improve user trust in deterministic/product behavior.

## Epic 2: Platform & Runtime Reliability

### Problem Statement

The extension now packages and starts correctly across targets, but several operational seams remain inconsistent or ambiguous:

- duplicate output channels
- split identifier namespaces
- self-reported governance theme input
- stale troubleshooting instructions
- missing workspace capability declarations
- unresolved telemetry strategy

These issues are not corruption risks, but they reduce platform clarity, diagnosability, and maintainability.

### Scope

Epic 2 includes:

- output channel consolidation
- namespace unification
- governance theme verification from PBIR
- `uploadScreenshots` repair
- workspace capability declarations
- telemetry decision
- troubleshooting documentation cleanup

Epic 2 excludes:

- telemetry pipeline redesign beyond the minimal product decision
- Marketplace/publishing redesign
- new governance scoring features

### Deliverable

An operationally reliable extension platform with clearer runtime behavior, cleaner identifiers, explicit capability posture, and documentation that matches the shipped product.

### Why Some Items Should Ship Together

The following items should be bundled to avoid repeated migrations or user-facing churn:

- namespace unification
- output channel consolidation
- capability declarations
- telemetry decision
- troubleshooting cleanup

This bundle has one theme: platform-operational coherence.

Namespace migration in particular should not be repeated across multiple releases because:

- command IDs
- view IDs
- configuration keys
- migration code
- docs

all need to move in sync.

### Independent Items Within Epic 2

These can ship independently if needed:

- governance theme verification
- `uploadScreenshots` repair

They are user-visible reliability fixes with narrow dependency surfaces.

## Epic 3: Performance & Scalability

### Problem Statement

The extension still relies too heavily on repeated synchronous filesystem access and repeated repo walks across review flows.

The current architecture is functional for moderate projects but will not scale cleanly for:

- larger PBIR repos
- larger Fabric App repos
- repeated review refreshes
- richer panel state and protocol evolution

The performance issues also overlap with correctness risks:

- stale payload shape assumptions
- weak protocol guards
- unchecked persisted state
- hardcoded scoring constants with low provenance

### Scope

Epic 3 includes:

- shared repo snapshots
- async filesystem access
- Fabric evidence reuse
- scoring configuration externalization
- protocol versioning
- selected page and state validation
- payload/schema guards

Epic 3 excludes:

- redesign of Fabric review methodology
- redesign of Story Assessment methodology
- large UI restructuring unrelated to performance

### Deliverable

A scalable review architecture that reduces repeated I/O, makes protocol evolution safer, and improves confidence in large-project analysis behavior.

### Why These Items Should Ship Together

The following items should be treated as one architectural bundle:

- shared repo snapshot
- async filesystem access
- Fabric evidence reuse

These are the same scalability problem expressed in three layers.

Similarly, the following should ship together:

- protocol versioning
- payload/schema guards
- selected state validation

These are one message-contract hardening bundle.

Externalizing scoring configuration can technically ship independently, but it is best aligned with the same `0.6.0` architecture pass because it changes review engine configuration boundaries and should not be revisited twice.

## Cross-Epic Dependency Map

### Epic 1 Dependencies

Epic 1 depends on:

- current deterministic mutation contracts
- current rollback/session semantics
- PBIR file structure knowledge already used by planner/apply code

Epic 1 should not wait on Epic 2 or Epic 3.

It is the highest-priority trust work and should move first.

### Epic 2 Dependencies

Epic 2 depends on:

- no foundational code changes from Epic 1 except where governance theme verification and `uploadScreenshots` are intentionally grouped into the recommended `0.5.1` bucket

Epic 2 should not be blocked by Epic 3.

### Epic 3 Dependencies

Epic 3 depends on:

- a stable enough runtime surface from Epic 2 for identifiers, channels, and capability posture
- no major mutation-engine redesign from Epic 1 because the snapshot/protocol work should avoid interleaving with mutation safety refactors

Epic 3 is intentionally sequenced last because it benefits from calmer platform boundaries.

## Recommended Release Sequencing

These release buckets are recommendations, not commitments.

### Recommended 0.5.1

Ship the trust-repair bundle first:

- deterministic fix safety
- write atomicity
- rollback
- schema-correct PBIR mutation support
- page-name resolution
- `uploadScreenshots` fix
- governance theme verification

Reason:

- this is the strongest user-trust and corruption-risk reduction release
- the deterministic fix path should not remain half-hardened while lower-risk platform cleanup proceeds
- the bundled items either directly reduce mutation risk or remove visible product behaviors that undermine trust

### Recommended 0.5.2

Ship the operational-coherence bundle next:

- output channel consolidation
- namespace unification
- capabilities declarations
- telemetry decision
- troubleshooting cleanup

Reason:

- these items improve runtime clarity and maintainability
- bundling avoids repeated config-key, command-ID, and doc migration churn
- none of these should delay Epic 1 safety work

### Recommended 0.6.0

Ship the architecture-scale bundle last:

- shared repo snapshot
- async filesystem access
- Fabric review optimization
- protocol versioning
- selected state validation
- scoring configuration externalization

Reason:

- these changes are deeper architectural refactors
- they benefit from stable runtime boundaries established in earlier buckets
- bundling reduces repeated protocol, performance, and analyzer configuration churn

## Prioritization Matrix

### Critical

#### Non-atomic deterministic writes

- **Risk:** partial PBIR mutation and irreversible half-applied state
- **Impact:** direct trust and data-integrity risk
- **Complexity:** high
- **Dependencies:** schema-correct mutation plan, rollback plan, post-write validation
- **Recommended order:** first inside Recommended `0.5.1`

#### Schema-incorrect dormant mutation paths

- **Risk:** invalid PBIR writes if re-enabled or expanded carelessly
- **Impact:** direct mutation correctness risk
- **Complexity:** high
- **Dependencies:** mutation path model, target resolution, validation tests
- **Recommended order:** first inside Recommended `0.5.1`

### High

#### Rollback-on-failure and post-write validation

- **Risk:** incomplete recovery after write failure
- **Impact:** high trust damage during fix application
- **Complexity:** high
- **Dependencies:** atomic file strategy, mutation correctness
- **Recommended order:** pair with atomic write work in Recommended `0.5.1`

#### Page-name keyed resolution

- **Risk:** wrong target file selection when display names collide
- **Impact:** high mutation-targeting risk
- **Complexity:** medium
- **Dependencies:** planner hardening, PBIR identity model
- **Recommended order:** same slice as schema-correct mutation support

#### Governance theme verification from PBIR

- **Risk:** governance enforcement remains self-reported
- **Impact:** high policy credibility problem
- **Complexity:** medium
- **Dependencies:** PBIR metadata read path only
- **Recommended order:** Recommended `0.5.1`

#### `uploadScreenshots` repair

- **Risk:** user action does not do what the UI promises
- **Impact:** high workflow reliability issue
- **Complexity:** low to medium
- **Dependencies:** score-panel host/webview command flow
- **Recommended order:** Recommended `0.5.1`

#### Namespace unification

- **Risk:** long-term drift across commands, views, config, and docs
- **Impact:** high maintainability burden
- **Complexity:** high because migration must be coordinated
- **Dependencies:** package metadata, migration rules, docs
- **Recommended order:** lead item in Recommended `0.5.2`

#### Shared repo snapshot and async I/O foundation

- **Risk:** poor scale and UI freezing on larger repos
- **Impact:** high scalability ceiling
- **Complexity:** high
- **Dependencies:** clear repository-analysis seam, evidence extractor reuse
- **Recommended order:** lead item in Recommended `0.6.0`

### Medium

#### Output channel consolidation

- **Risk:** fragmented diagnostics and resource leakage
- **Impact:** moderate operational friction
- **Complexity:** low to medium
- **Dependencies:** namespace/host wiring review
- **Recommended order:** Recommended `0.5.2`

#### Workspace capability declarations

- **Risk:** unclear behavior in untrusted or virtual workspaces
- **Impact:** moderate policy/runtime clarity issue
- **Complexity:** low
- **Dependencies:** product posture decision only
- **Recommended order:** Recommended `0.5.2`

#### Telemetry decision

- **Risk:** instrumentation remains ambiguous and misleading
- **Impact:** moderate operational blind spot
- **Complexity:** low to medium
- **Dependencies:** product/privacy decision
- **Recommended order:** Recommended `0.5.2`

#### Troubleshooting cleanup

- **Risk:** docs guide users to nonexistent commands
- **Impact:** moderate support burden
- **Complexity:** low
- **Dependencies:** final runtime/namespace decisions
- **Recommended order:** end of Recommended `0.5.2`

#### Protocol versioning and schema guards

- **Risk:** stale host/webview payload mismatches fail deep
- **Impact:** moderate stability issue
- **Complexity:** medium
- **Dependencies:** payload contract review
- **Recommended order:** early in Recommended `0.6.0`

#### Selected state validation

- **Risk:** persisted state points past valid page bounds after re-score
- **Impact:** moderate correctness and UX stability issue
- **Complexity:** low
- **Dependencies:** protocol/state guard work
- **Recommended order:** with protocol versioning in Recommended `0.6.0`

#### Scoring configuration externalization

- **Risk:** hidden scoring constants remain hard to reason about and tune
- **Impact:** moderate maintainability and explainability issue
- **Complexity:** medium
- **Dependencies:** review analyzer configuration model
- **Recommended order:** later slice of Recommended `0.6.0`

### Low

#### Stale doc polish beyond the core troubleshooting fixes

- **Risk:** minor drift remains in non-core guidance
- **Impact:** low
- **Complexity:** low
- **Dependencies:** none
- **Recommended order:** after higher-value runtime/doc corrections

## What Must Happen Before Deterministic Fixes Can Be Trusted

The deterministic fix engine should not be considered fully trustworthy until all of the following are true:

1. Mutation paths are schema-correct for the supported categories.
2. Target resolution is stable and does not rely on ambiguous page display names.
3. File writes are atomic.
4. Batch operations can roll back cleanly on failure.
5. Post-write validation confirms the written PBIR still satisfies the expected structural invariants.
6. Safety tests cover planner, apply, rollback, and validation interactions.
7. Format-preserving edit strategy avoids unnecessary whole-file churn for supported edits.

If any of those remain missing, deterministic fixes are still only partially hardened.

## What Can Safely Ship Independently

These items can ship independently without forcing the larger architectural bundles:

- governance theme verification
- `uploadScreenshots` repair
- output channel consolidation
- capability declarations
- troubleshooting cleanup

These items are useful, but they should still follow the recommended release buckets unless priorities change.

## What Should Be Bundled To Avoid Repeated Refactors

Bundle together:

- atomic writes, rollback, schema-correct mutation support, stable target resolution, and post-write validation
- namespace unification with command/view/config migration and docs
- shared repo snapshot with async I/O conversion and Fabric evidence reuse
- protocol versioning with payload/schema guards and selected state validation

These bundles reduce repeated migrations and keep architectural boundaries stable between releases.

## Validation Principles

Each epic should be validated at the narrowest useful layer first, then at the extension-integration layer.

Hardening validation should prefer:

- focused deterministic tests
- regression tests on existing behavior
- packaged-extension smoke checks only where the hardening changes runtime wiring or end-to-end review behavior

## Roadmap Guidance Summary

The recommended order is:

1. restore trust in deterministic writes
2. clean up platform/runtime inconsistencies
3. refactor for scale and protocol maturity

That sequence maximizes risk reduction while minimizing repeated migration churn.
