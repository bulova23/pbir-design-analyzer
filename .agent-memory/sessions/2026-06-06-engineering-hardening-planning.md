# 2026-06-06 Engineering Hardening Planning

## Objective

Create a planning-only Engineering Hardening & Reliability Design Spec and Implementation Plan for the post-`0.5.0` reliability roadmap.

## Context

The latest repo review against the earlier `0.4.0` code-review findings confirmed that the biggest release blockers have already improved in `0.5.0`:

- cross-platform packaging/runtime support is now real
- backend startup readiness is handshake-based
- the fix-outcome severity inversion is corrected
- Windows-hostile packaging scripts were removed
- the riskiest dormant mutation categories remain disabled and covered by regression tests

The main remaining concerns are structural:

- deterministic fix-write trustworthiness
- runtime/operational coherence
- performance and protocol scalability

## Deliverables Added

- `docs/superpowers/specs/2026-06-06-engineering-hardening-design.md`
- `docs/superpowers/plans/2026-06-06-engineering-hardening-plan.md`

## Planning Outcome

The remaining work was grouped into three epics:

1. `Safe Deterministic Fix Engine`
2. `Platform & Runtime Reliability`
3. `Performance & Scalability`

The roadmap was organized into recommended release buckets, explicitly documented as guidance rather than commitments:

- Recommended `0.5.1`
- Recommended `0.5.2`
- Recommended `0.6.0`

## Key Design Decisions Captured

### Recommended `0.5.1`

Treat deterministic mutation hardening as one integrity bundle:

- schema-correct PBIR mutation support
- stable page-name keyed resolution
- atomic writes
- rollback-on-failure
- post-write validation
- mutation safety regression coverage

Also include the adjacent trust fixes:

- governance theme verification from PBIR
- `uploadScreenshots` repair

### Recommended `0.5.2`

Treat runtime coherence as one operational bundle:

- output channel consolidation
- namespace unification
- workspace capability declarations
- telemetry decision
- troubleshooting cleanup

### Recommended `0.6.0`

Treat scale/performance/protocol work as one architecture bundle:

- shared repo snapshot
- async filesystem access
- Fabric evidence reuse
- protocol versioning
- payload/schema guards
- selected state validation
- scoring configuration externalization

## Dependency Guidance Captured

- Deterministic fix trust should not be claimed until mutation semantics, target resolution, atomic writes, rollback, validation, and safety tests all exist together.
- Namespace unification should lead the runtime-coherence work because docs, config migration, command IDs, and metadata all depend on that decision.
- Shared repo snapshot design should precede async conversion so the repo-analysis seam is introduced once, not repeatedly.
- Protocol versioning should precede state-validation cleanup so state rules attach to an explicit contract boundary.

## Validation / Risk Guidance Captured

- Use focused deterministic tests first for mutation safety work.
- Validate runtime-coherence work with targeted command/metadata/doc checks rather than broad performance passes.
- Validate scalability work with snapshot reuse, async traversal, protocol compatibility, and analyzer-configuration tests.
- Keep packaged-extension smoke checks for the buckets that materially affect runtime orchestration or end-to-end review flows.

## Remaining Risks

- The deterministic fix engine is still only partially hardened until the Recommended `0.5.1` integrity bundle is implemented.
- Namespace migration will need careful execution sequencing to avoid repeated churn.
- Snapshot/protocol refactors should not interleave with mutation-engine hardening or namespace migration.

## Next Recommended Step

Review the new hardening spec and plan as the canonical roadmap input for post-`0.5.0` work.

If implementation begins, start with the Recommended `0.5.1` deterministic fix trust bundle and treat it as a single design-reviewed execution stream rather than a set of isolated bug fixes.
