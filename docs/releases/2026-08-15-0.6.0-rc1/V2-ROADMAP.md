# PBIR Authoring Platform — Next Product Generation Roadmap

This roadmap replaces phase numbering after Phase 48. It is a product planning
document, not authorization to implement every recommendation.

## Recommended ordering

1. Platform & contract consolidation
2. Epic 1 — Advanced Authoring
3. Epic 2 — AI Authoring
4. Epic 3 — Enterprise Platform
5. Epic 4 — Platform & Ecosystem

The consolidation step is intentionally small and may be skipped if UAT shows
the current contracts are sufficient. It protects the public boundary before
larger capabilities are added.

## Platform & contract consolidation

Objective: turn the RC1 evidence into a deliberately maintained v2 foundation.

Business value: lower regression risk, clearer partner expectations, and less
AI-generated contract drift.

Dependencies: UAT findings, public/internal admission matrix, descriptor
catalog inventory, and a decision on generated target artifacts.

Estimated complexity: Medium.

Recommended release ordering: before or alongside the first Advanced Authoring
release; no user-facing feature is implied.

Candidate outcomes: contract fixture suite, catalog documentation, handle
lifecycle decision, and lint debt plan.

## Epic 1 — Advanced Authoring

Objective: expand deterministic authoring from the RC1 curated catalog to the
most valuable report-authoring operations.

Business value: consultants can complete more report design work without manual
PBIR editing, while preview/validation/fidelity preserve trust.

Candidate scope: bookmarks, drillthrough, shared slicers, advanced
interactions, richer formatting, additional descriptor-backed mutation types,
and—only if UAT supports it—carefully designed ordered batches.

Dependencies: stable descriptor catalog, schema fixtures, mutation admission
matrix, undo/rollback decision, and explicit public contract versioning.

Estimated complexity: High.

Recommended order: first user-facing v2 epic after contract consolidation.

## Epic 2 — AI Authoring

Objective: let users describe report intent and receive safe, explainable
authoring proposals.

Business value: shorten report creation and remediation time while keeping
deterministic preview/apply/rollback as the only mutation authority.

Candidate scope: natural-language report creation, report modification by
intent, layout optimization, automatic dashboard generation, and intelligent
report review.

Dependencies: stable authoring contracts, richer deterministic operation
catalog, proposal/evidence model, model/provider governance, and strong
confirmation UX. AI remains advisory and never receives direct mutation
authority.

Estimated complexity: Very high.

Recommended order: after Advanced Authoring proves operation semantics and
evidence quality.

## Epic 3 — Enterprise Platform

Objective: make the authoring platform usable across controlled teams and
organizational review processes.

Business value: support deployment, collaboration, governance, and audit
requirements that local single-user workflows cannot satisfy.

Candidate scope: hosted execution, authentication, collaboration, version
history, governance, auditing, organization profiles, and controlled provider
execution.

Dependencies: identity/security architecture, persistence, tenancy and audit
model, provider trust boundaries, hosted operations, and a clear separation of
advisory AI from execution authority.

Estimated complexity: Very high.

Recommended order: after local contract and authoring semantics stabilize; do
not use hosted execution to bypass local safety boundaries.

## Epic 4 — Platform & Ecosystem

Objective: expose stable capabilities to automation and partner tooling.

Business value: enable CI/CD, repeatable governance, integrations, and a wider
authoring ecosystem.

Candidate scope: public SDK, versioned APIs, MCP integration, automation,
CI/CD, Git workflows, and repository-native review tooling.

Dependencies: stable public contracts, authentication and authorization,
versioning/deprecation policy, auditability, deterministic artifacts, and
enterprise operational support.

Estimated complexity: High to Very high.

Recommended order: last, after the platform can promise compatibility and
security to external consumers.

## Decision rules

- Do not start an epic solely because its candidate list is attractive.
- Promote work only when UAT identifies a user problem and the required
  contract boundary is understood.
- Keep scoring authoritative, findings normalized, presentation downstream,
  and deterministic mutation as the only execution authority.
- Prefer one vertical slice with fixtures, preview, validation, analyzer
  evidence, and manual UAT over broad speculative capability discovery.
