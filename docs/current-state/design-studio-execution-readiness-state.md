# Design Studio Execution Readiness Current State

## Status

Phase 28 adds the Design Studio Execution Readiness Dashboard.

The dashboard contract is design-studio-execution-readiness/v1.

## Purpose

The dashboard aggregates the planning, generation-preparation, preview-package, and review-handoff pipeline into one consultant-friendly readiness view inside Design Studio.

This phase is informational only. It does not generate deployable PBIR files, create report.json, create definition.pbir, execute Microsoft Skills, invoke providers, call Microsoft APIs, invoke CLI commands, deploy assets, publish assets, mutate reports, launch Analyzer Workspace automatically, or run Analyzer validation.

## Current Product Position

The dashboard sits downstream from:

- architecture-readiness-report/v1
- generation-manifest/v1
- generation-pipeline-verification/v1
- pbir-generation-specification/v1
- pbir-ir/v1
- pbir-preview-package/v1
- pbir-review-handoff/v1
- design-studio-preview-review/v1

It is represented as:

- backend DesignStudioExecutionReadinessService
- backend DesignStudioExecutionReadinessSafetyGate
- backend design-studio-execution-readiness/v1 dashboard records
- extension-side Design Studio execution readiness view model
- extension-side DesignStudioExecutionReadinessSafetyGate
- Design Studio protocol request and update messages
- webview rendering under Preview Review

## Dashboard Sections

The dashboard surfaces deterministic stage summaries for:

- Architecture
- Planning
- Generation
- Runtime
- Skills
- Review

Architecture includes certification status and readiness classification.

Planning includes planning outcome status, Generation Manifest status, and pipeline verification status.

Generation includes PBIR Generation Specification readiness, PBIR IR readiness, Preview Package readiness, and Preview Review status.

Runtime includes Runtime Provider readiness, Microsoft Runtime Provider readiness, and Generation Provider readiness.

Skills includes skill readiness, selected provider, selected skills, and capability coverage summary.

Review includes design approval status, preview review status, and Analyzer handoff readiness.

Warnings include blocking issues, missing approvals, unsupported capabilities, and remaining architecture gaps.

## Readiness Aggregation Model

The deterministic readiness summary can be:

- Not Ready
- Ready for Design Review
- Ready for Analyzer Review
- Ready for Generation Provider
- Blocked

The backend service classifies readiness from existing planning and review states. It preserves fixed stage ordering, immutable lineage references, architecture certification references, warning summaries, reviewer actions available, and a trust boundary summary.

The extension derives the visible dashboard from the active design-studio-preview-review/v1 state so Design Studio can render the same readiness posture without creating execution authority.

## Protocol Validation

Design Studio protocol validation now covers:

- requestExecutionReadiness webview messages
- executionReadinessUpdated host messages
- design-studio-execution-readiness/v1 schema version
- deterministic stage-summary ordering
- warning summary shape
- reviewer-action shape
- lineage reference shape
- architecture certification reference shape
- trust boundary booleans

Unsupported protocol versions are rejected before state is consumed.

Malformed execution readiness payloads are rejected before rendering.

## Safety Gate

DesignStudioExecutionReadinessSafetyGate rejects:

- execution requests
- provider invocation requests
- Microsoft Skills execution requests
- API invocation requests
- CLI invocation requests
- deployment requests
- automatic Analyzer validation requests
- automatic Analyzer launch requests
- malformed readiness payloads
- readiness dashboards that claim execution, provider, API, CLI, deployment, or Analyzer automation authority

Rejected backend requests produce no dashboard record.

Rejected extension payloads are not accepted as valid protocol or dashboard state.

## Trust Boundary Preservation

The dashboard records these as false:

- execution allowed
- provider invocation allowed
- Microsoft Skills execution allowed
- API invocation allowed
- CLI invocation allowed
- deployment allowed
- automatic Analyzer validation allowed
- automatic Analyzer launch allowed

The dashboard is not an execution planner, generator, provider adapter, deployment workflow, Analyzer automation workflow, or validation substitute.

## Remaining Execution Implementation Gaps

Deployable PBIR generation remains unimplemented.

report.json generation remains unimplemented.

definition.pbir generation remains unimplemented.

Microsoft Skills execution remains unimplemented.

Provider, API, and CLI invocation remain unimplemented.

Deployment remains unimplemented.

Analyzer Workspace automation remains unimplemented.

Analyzer Workspace validation remains a separate downstream manual workflow.
