# Power BI Design Governance & Optimization — Version 2 Roadmap

RC1 freezes report authoring as a product boundary. Version 2 is organized
around governance outcomes, not historical implementation phases. No epic is
authorized by this document; promotion requires UAT evidence and a reviewed
contract decision.

## Epic 1 — Design Policy Engine

Objective: turn the current score, findings, governance, configuration, and
evidence primitives into versioned Power BI Design Policy checks.

Business value: organizations can define a repeatable quality bar and receive
stable, explainable pass/fail/waiver outcomes.

Evidence-supported scope: rule identities, severity, provenance, normalized
findings, scoring profiles, governance export, and policy-backed quality gates.

Dependencies: UAT findings, the public/internal admission matrix, descriptor
catalog inventory, and a decision on generated target artifacts.

Estimated complexity: Medium to High.

## Epic 2 — AI Companion

Objective: provide provider-neutral advisory explanations, review summaries,
and remediation proposals without granting AI mutation authority.

Business value: reduce review and remediation effort while preserving
deterministic preview/apply/rollback as the only execution path.

Evidence-supported scope: current proposal enrichment, reviewer personas,
finding-linked recommendations, and existing provider capability detection.

Dependencies: stable authoring contracts, the policy/evidence model, model and
provider governance, and strong confirmation UX. Do not build a competing
generic authoring agent.

Estimated complexity: High.

## Epic 3 — Enterprise Governance

Objective: make policy, findings, exceptions, approvals, ownership, and audit
usable across controlled teams and delivery processes.

Business value: support governed review and promotion without turning local
single-user workflows into an unbounded hosted execution system.

Candidate scope: organization profiles, policy versions, waivers, approvals,
audit records, ownership, and controlled delivery gates.

Dependencies: identity/security architecture, persistence, tenancy and audit
model, provider trust boundaries, hosted operations, and a clear separation of
advisory AI from execution authority.

Estimated complexity: Very high.

## Epic 4 — Platform

Objective: expose stable governance capabilities to automation and partner
tooling after the public contract is proven.

Business value: enable CI/CD validation, repeatable review packets, Git-based
quality gates, and carefully scoped integrations.

Candidate scope: CLI/API, CI/CD validation, Git workflows, review packets, and
provider-neutral integration surfaces.

Dependencies: stable public contracts, authentication and authorization,
versioning/deprecation policy, auditability, deterministic artifacts, and
enterprise operational support.

Estimated complexity: High to Very high.

## Epic 5 — Rendered Intelligence

Objective: combine bounded rendered evidence with semantic and deterministic
findings to improve review of visual hierarchy, readability, and accessibility.

Evidence-supported scope: existing Rendered Review Mode, typed screenshot
evidence, PBI Lens capability-safe integration, and deterministic fallback.
Visual Intelligence remains a separate future capability and is not implied by
manual screenshot evidence.

Dependencies: UAT of current rendered review, stable evidence provenance,
privacy handling, and a demonstrated supported rendered acquisition path.

Estimated complexity: High.

## Decision rules

- Do not start an epic solely because its candidate list is attractive.
- Promote work only when UAT identifies a user problem and the required
  contract boundary is understood.
- Keep scoring authoritative, findings normalized, presentation downstream,
  and deterministic mutation as the only execution authority.
- Prefer one vertical slice with fixtures, preview, validation, analyzer
  evidence, and manual UAT over broad speculative capability discovery.
- Keep generic authoring, semantic-model generation, and DAX generation out of
  the governance product promise.
