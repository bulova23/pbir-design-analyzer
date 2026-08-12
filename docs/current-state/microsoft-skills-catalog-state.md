# Microsoft Skills Catalog Current State

Phase 35A treats this catalog as research and planning metadata only. It does not execute Microsoft Skills or infer runtime generation readiness; **No runtime generation provider is available**.

## Summary

Microsoft Skills Catalog is now implemented as the descriptive Microsoft skill metadata layer downstream from Capability Negotiation Framework and upstream from Microsoft Runtime Provider Contract.

Its role is:

- define `microsoft-skills-catalog/v1` as the authoritative Microsoft skill catalog artifact
- define `microsoft-skill-definition/v1` as the authoritative Microsoft skill metadata artifact
- register descriptive Microsoft skill definitions without loading or invoking anything
- discover skills by capability, target profile, and execution mode
- resolve negotiated capabilities into required and optional Microsoft skill candidates
- validate skill coverage, prerequisite satisfaction, catalog integrity, and version compatibility
- classify Microsoft skill readiness for a future skill-provider seam without executing anything

It is not a Microsoft Skills runtime, not a Microsoft API surface, not a CLI runner, not a provider invocation path, and not an artifact-generation surface.

## Current Product Position

Microsoft Skills Catalog currently sits after:

- Design Package Consumption Layer
- Generation Request Framework
- Execution Plan Framework
- Provider Adapter Framework
- Microsoft Adapter Specification
- Capability Negotiation Framework

It currently hands off to:

- Microsoft Skill Provider Adapter

It currently informs:

- Planning Orchestration Framework
- Microsoft Runtime Provider Contract

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs and approves
- Planning Frameworks create planning outcomes
- Microsoft Adapter Specification defines Microsoft capability mappings
- Capability Negotiation Framework resolves provider-neutral capability requirements
- Microsoft Skills Catalog maps those requirements to descriptive Microsoft skills
- Microsoft Skill Provider Adapter maps descriptive Microsoft skills to descriptive providers
- Microsoft Runtime Provider Contract defines Microsoft runtime compatibility only
- Analyzer Workspace remains the downstream validation owner for future generated artifacts

## What Exists Today

The implemented Microsoft skill layer currently includes:

- `microsoft-skills-catalog/v1`
- `microsoft-skill-definition/v1`
- `MicrosoftSkillsCatalog`
- `MicrosoftSkillCompatibilityValidator`
- `MicrosoftSkillResolutionService`
- `MicrosoftSkillReadinessService`
- `MicrosoftSkillsCapabilityCatalogFrameworkService`
- explicit Microsoft skill readiness states:
  - `unsupported`
  - `partiallySatisfied`
  - `satisfied`
  - `readyForSkillProvider`
- boundary tests proving the layer stays metadata-only

## Catalog Model

The authoritative catalog artifact is `microsoft-skills-catalog/v1`.

Its required sections are:

- schema version
- catalog id
- catalog version
- provider category
- skills

The authoritative skill artifact is `microsoft-skill-definition/v1`.

Each skill definition currently requires:

- schema version
- skill id
- skill name
- skill version
- skill category
- provided capabilities
- supported target profiles
- supported execution modes
- unsupported capabilities
- unsupported profiles
- prerequisite capabilities
- status

## Default Microsoft Skill Inventory

The default catalog currently describes:

- Power BI Report Authoring
  - available
  - supports PBIR report planning capabilities for layout, page, and semantic generation
- Power BI Report Design
  - available
  - supports PBIR report navigation generation
- Power BI Validate Report
  - available
  - supports optional validation support metadata for PBIR planning
- Fabric Data App Template
  - planned
  - supports future-facing Fabric Data App deployment and structural planning metadata

No skill definition loads a provider, opens a runtime, or performs execution.

## Skill Resolution Lifecycle

The current deterministic lifecycle is:

Capability Negotiation  
↓  
Microsoft Skills Catalog Resolution  
↓  
Microsoft Skill Provider Selection  
↓  
readyForSkillProvider or fail-closed state

Resolution currently produces:

- candidate skill set
- required skill set
- optional skill set
- capability coverage summary
- unresolved capability summary

Resolution is descriptive only.

It does not invoke a skill.

## Capability Coverage Model

The current coverage model separates:

- required capabilities requested
- required capabilities covered
- optional capabilities requested
- optional capabilities covered
- unresolved required capabilities
- unresolved optional capabilities
- unsupported capabilities

The current PBIR-ready path maps:

- required capabilities
  - layout generation
  - navigation generation
  - page generation
  - semantic generation
- optional capabilities
  - validation support

## Compatibility Validation Model

`MicrosoftSkillCompatibilityValidator` currently validates:

- catalog shape
- missing required fields
- duplicate skill ids
- target-profile support
- capability coverage
- prerequisite satisfaction
- skill-definition and catalog version compatibility
- selected-skill integrity against the catalog

Validation fails closed.

## Current Readiness Model

Microsoft skill readiness currently means descriptive mapping state only.

The current meanings are:

- unsupported
  - the target profile itself is unsupported by the catalog
- partiallySatisfied
  - at least one required capability, prerequisite, or integrity rule is still unresolved
- satisfied
  - all required capabilities are covered and the catalog is valid
- readyForSkillProvider
  - the required skills are known and mapped for a future skill-provider seam

`readyForSkillProvider` does not imply execution.

It only means the planning stack has a coherent Microsoft skill mapping.

Provider mapping is now handled by `docs/current-state/microsoft-skill-provider-adapter-state.md`.

## Current Trust Boundaries

The current Microsoft skill catalog does not:

- execute Microsoft Skills
- invoke Microsoft APIs
- invoke CLI commands
- invoke providers
- generate artifacts
- generate PBIR outputs
- generate Fabric App outputs
- generate Fabric Data App outputs
- deploy assets
- validate generated outputs
- automate Analyzer Workspace

## Remaining Execution Gap

The current repo state still excludes:

- Microsoft Skills execution
- skill invocation
- CLI-backed Microsoft execution
- provider invocation
- PBIR generation
- Fabric App generation
- Fabric Data App generation
- artifact intake and quarantine
- deployment workflows
- Analyzer Workspace automation
