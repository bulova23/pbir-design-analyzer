# Provider Planning Framework Current State

## Summary

Provider Planning Framework is now implemented as a planning-only layer downstream from Generation Request.

Its role is:

- consume a valid generation-request/v1 contract
- derive a versioned execution-plan/v1 artifact
- preserve source provenance, success criteria, and review policy
- describe future provider work without executing it
- declare provider-neutral capabilities and sequencing constraints

It is not a provider adapter, not a Microsoft Skills execution path, and not an artifact-generation surface.

## Current Product Position

Provider Planning Framework currently sits between Generation Request and Provider Adapter Framework.

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs and approves
- Generation Request Framework creates execution contracts
- Provider Planning Framework creates execution plans
- Provider Adapter Framework evaluates adapter compatibility
- Analyzer Workspace validates generated artifacts

## What Exists Today

The implemented planning framework currently includes:

- execution-plan/v1 contract
- provider-neutral capability model
- deterministic Execution Plan builder
- Execution Plan validator
- explicit readiness states:
  - draft
  - valid
  - blocked
  - readyForProviderAdapter
- dependency graph validation
- review-requirement validation
- boundary tests proving the framework stays execution-free

## Execution Plan Contract

The current authoritative provider-planning artifact is execution-plan/v1.

Its required sections are:

- schema metadata
- source references
- target definition
- provider planning metadata
- planned work units
- dependency graph
- planning constraints
- review requirements
- success contract

The plan is derived from Generation Request and does not replace it.

Generation Request remains the authoritative execution contract.

Prompt segments remain derived from Generation Request rather than from Execution Plan.

## Capability Model

The current provider-neutral capability model declares:

- supports layout generation
- supports semantic generation
- supports artifact generation
- supports validation

In the current repo state, the framework uses those declarations for planning metadata only.

It does not execute any of those capabilities.

## Current Planning Shape

The current deterministic work-unit sequence is:

1. schema analysis
2. artifact design
3. layout planning
4. semantic planning
5. validation planning

The dependency graph is validated before a plan can become ready for a provider adapter.

Unsupported targets, unsupported capabilities, review requirements, and validation requirements remain explicit constraints inside the plan.

Provider Adapter Framework now consumes that ready state and converts it into provider-adapter/v1 compatibility input without gaining execution authority.

Microsoft Adapter Specification now consumes that provider-adapter/v1 output descriptively and translates the same planning requirements into Microsoft capability requirements without gaining execution authority.

## Current Readiness Model

Execution Plan readiness currently means planning state only.

The current meanings are:

- draft: plan created but not yet validated
- valid: plan passes structural validation
- blocked: plan has validation failures and cannot proceed
- readyForProviderAdapter: plan is structurally ready for a future adapter to consume

These states do not imply:

- provider execution
- approval completion
- generated artifact validation
- deployment

## Current Trust Boundaries

The current framework does not:

- execute Microsoft Skills
- invoke CLI commands
- call provider APIs
- generate PBIR artifacts
- generate Fabric App artifacts
- generate Fabric Data App artifacts
- automate Analyzer Workspace
- validate generated outputs
- deploy assets

## Remaining Execution Gaps

The current repo state still excludes:

- Microsoft provider adapters
- provider implementations
- CLI-backed adapter execution
- artifact intake and quarantine
- PBIR generation
- Fabric App generation
- Fabric Data App generation
- deployment workflows
- Analyzer Workspace automation
