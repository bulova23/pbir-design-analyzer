# Report Design Studio Trust Boundary

Date: 2026-06-13

## Purpose

This note documents the ownership and workflow restrictions that Report Design Studio must preserve after Tasks 1-10.

The goal is stability, not capability expansion.

## Workflow Ownership

- Design Brief, Concept Studio, Draft Studio, Refinement Studio, and closed-loop comparison are Design Studio workflow concerns.
- Materialization is the explicit boundary between Design Studio artifacts and analyzable candidates.
- Analyzer Workspace owns evaluation of materialized candidates.
- Deterministic preview, apply, and rollback remains the only path for real report mutation outside this workflow.

## Approval Ownership

- Design approval is limited to Design Studio artifacts.
- Refinement approval is limited to advisory refinement proposals.
- Materialization approval is limited to candidate derivation eligibility.
- Validation approval is separate from all three and must never be inferred from them.

Approval implications that are explicitly forbidden:

- design approval does not imply validation approval
- refinement approval does not imply validation approval
- materialization approval does not imply validation approval
- validation approval does not imply deployment approval

## Validation Ownership

- Analyzer Workspace remains authoritative for Story Assessment, Guided Story Improvements, Issues, Fix Plan, Cross-Page Narrative, and validation approval.
- Design Studio must not validate its own outputs.
- Design Studio must not issue validation approval directly.
- Validation approval requires analyzer-owned provenance, analyzer result identity, source candidate identity, source artifact/version fingerprint, and validation result status.

## Provider Restrictions

- Providers are optional and must never be required for the core workflow.
- Provider outputs remain advisory-only and non-production.
- Provider outputs must not self-approve.
- Provider outputs must not bypass lineage or validation.
- Provider outputs must not create analyzable surfaces directly.
- Provider integrations must not mutate reports, generate PBIR assets, or deploy outputs.

## Materialization Restrictions

- Materialization creates candidates only.
- Materialization must not create production assets.
- Materialization must not create PBIR files.
- Materialization must not mutate reports.
- Materialization must not deploy assets.
- Materialization must always preserve exact source lineage and emit diagnostics.
- Analyzer ownership remains downstream from materialization; materialization must not execute analyzers automatically.

## Protocol Restrictions

- Design Studio host and webview messages are a versioned trust boundary.
- Unsupported protocol versions must be rejected.
- Unsupported message types must be rejected.
- Nested payload validation is required before state is consumed.
- Cross-thread or malformed lineage inside protocol payloads must be rejected.

## Regression Guardrails

Future contributors should expect tests to fail if they introduce:

- approval collapse across workflow stages
- validation without analyzer-owned evidence
- lineage drift or stale analyzer lineage
- direct provider-to-surface shortcuts
- materialization side effects beyond candidate creation
- Design Studio owned analyzer authority
- hidden automation inside the closed loop
