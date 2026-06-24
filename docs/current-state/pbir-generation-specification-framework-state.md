# PBIR Generation Specification Framework Current State

## Summary

PBIR Generation Specification Framework is now implemented as the authoritative specification-only translation layer between Design Studio intent and any future PBIR generation provider.

Its role is:

- define `pbir-generation-specification/v1` as the authoritative generation-specification contract
- define `pbir-artifact-specification/v1` as the authoritative PBIR artifact-specification contract
- translate Design Package, Generation Request, and Planning Outcome intent into PBIR artifact definitions
- validate page, visual, semantic, navigation, and success-criteria completeness
- evaluate specification readiness before any future generation-provider handoff

It is not a PBIR generator, not a Microsoft Skills runtime, not a Microsoft API surface, not a CLI runner, not a deployment path, and not a report-mutation workflow.

## Current Product Position

PBIR Generation Specification Framework now sits after:

- Design Package Consumption Layer
- Generation Request Framework
- Planning Orchestration Framework

It sits before:

- the existing PBIR execution prototype boundary
- any future Microsoft generation provider
- any future PBIR artifact intake or deployment workflow

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs and approves
- Planning frameworks normalize and validate planning intent
- PBIR Generation Specification Framework becomes the authoritative translation of approved PBIR design intent
- future generation providers may consume that specification but do not own it
- Analyzer Workspace remains the downstream validation owner for any future generated artifact

## What Exists Today

The implemented Phase 15 layer currently includes:

- `pbir-generation-specification/v1`
- `pbir-artifact-specification/v1`
- `PbirGenerationSpecificationService`
- `PbirGenerationSpecificationValidator`
- `PbirGenerationSpecificationReadinessService`
- deterministic mapping from:
  - Design Package page intent
  - Generation Request target/profile intent
  - Planning Outcome lineage and readiness intent
- explicit readiness states:
  - `incomplete`
  - `partiallySpecified`
  - `specified`
  - `readyForGenerationProvider`

## Design Studio Mapping Model

The current deterministic mapping flow is:

Design Brief  
↓  
Concept Studio  
↓  
Draft Studio  
↓  
Design Package  
↓  
Generation Request  
↓  
Planning Outcome  
↓  
PBIR Generation Specification

The resulting specification becomes the authoritative representation of PBIR construction intent for future providers.

## PBIR Generation Specification Contract

The authoritative generation-level artifact is `pbir-generation-specification/v1`.

Its required sections are:

- metadata
  - schema version
  - specification id
  - source references
- design references
  - Design Package reference
  - Generation Request reference
  - Planning Outcome reference
- artifact specifications

The contract carries source references and lineage but remains provider-neutral and non-executing.

## PBIR Artifact Specification Contract

The authoritative artifact-level contract is `pbir-artifact-specification/v1`.

Its required sections are:

- metadata
  - schema version
  - artifact specification id
  - target profile id
- design references
  - Design Package reference
  - Generation Request reference
  - Planning Outcome reference
- page specifications
  - page identity
  - page purpose
  - page audience
  - navigation behavior
- visual specifications
  - visual type
  - placement
  - intended KPI
  - intended dimensions
  - intended interactions
- semantic specifications
  - KPI bindings
  - filter bindings
  - drill behavior
  - intended measures
- navigation specifications
  - landing page
  - page transitions
  - drill paths
- success criteria
  - business success criteria
  - analytical success criteria
  - planning-outcome requirements

## Validation Model

`PbirGenerationSpecificationValidator` currently validates:

- required generation and artifact references
- page-definition completeness
- visual-definition completeness and page binding integrity
- semantic-definition completeness and page binding integrity
- navigation-definition completeness and landing-page validity
- success-criteria completeness
- boundary safety for Phase 15 PBIR-only scope

Validation fails closed.

## Readiness Model

`PbirGenerationSpecificationReadinessService` currently evaluates:

- `incomplete`
  - missing required sections or fields
  - unsupported schema versions
  - boundary violations
- `partiallySpecified`
  - missing design intent details
  - invalid page, visual, semantic, or navigation definitions
  - incomplete success criteria
- `specified`
  - the specification is coherent and complete as a contract
- `readyForGenerationProvider`
  - the specification is coherent and complete enough for a future provider handoff

`readyForGenerationProvider` does not mean generation occurred.

## Current Trust Boundaries

The current framework does not:

- generate PBIR artifacts
- invoke Microsoft Skills
- invoke Microsoft APIs
- invoke CLI commands
- deploy assets
- mutate reports
- automate Analyzer Workspace

## Remaining Generation Gap

The current repo state still excludes:

- Microsoft Skills execution
- Microsoft API invocation
- CLI-backed execution
- real PBIR generation
- artifact intake and quarantine
- deployment workflows
- Fabric App generation
- Fabric Data App generation
- Analyzer Workspace automation
