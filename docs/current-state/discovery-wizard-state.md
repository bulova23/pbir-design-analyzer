# Discovery Wizard Current State

## Summary

Discovery Wizard is implemented as an advisory discovery pipeline that starts from a semantic model and produces consultant-style recommendation artifacts for downstream design work.

Its current role is:

- inspect semantic-model structure and signals
- identify credible analytics opportunities
- rank curated recommendations
- attach an Experience Blueprint to each recommendation
- seed downstream Design Studio artifacts
- produce a provider-neutral Design Package
- preserve provenance for future generation planning

As of the current repo state, Discovery Wizard is treated as MVP-complete for its intended advisory scope.

## Current Product Position

Discovery Wizard is upstream from Design Studio and Analyzer Workspace.

Its current ownership is:

- Discovery Wizard recommends
- Design Studio designs
- Analyzer Workspace validates

It is not a report generator, not a provider execution path, and not a validation surface.

## What Exists Today

The implemented backend discovery stack currently includes:

- semantic-model discovery
- opportunity identification
- recommendation ranking
- Experience Blueprint generation
- Design Studio starting-point generation
- Design Package generation
- Design Package consumption normalization
- Generation Request and prompt-segment derivation for downstream provider-facing planning

The discovery implementation currently lives in backend discovery services rather than in a dedicated shipped Discovery Wizard UI workflow.

## Implemented Discovery Flow

The current implemented flow is:

Semantic Model  
↓  
Discovery Profile  
↓  
Opportunity Catalog  
↓  
Recommendation Set  
↓  
Experience Blueprint  
↓  
Selected Recommendation  
↓  
Design Studio Starting Point  
or  
Design Package  
or  
Generation Request planning seam

## Current Outputs

### Recommendation Set

Discovery Wizard currently produces a curated recommendation output rather than a broad catalog.

The current design target and validation framing are:

- Top 3 primary recommendations
- 2 alternate recommendations

Each recommendation is expected to carry:

- recommendation identity
- experience type
- confidence
- business value
- implementation complexity
- audience and business outcome framing
- supporting rationale
- limiting factors
- attached Experience Blueprint

### Experience Blueprint

The Experience Blueprint is the main concrete output that makes recommendations actionable.

It currently captures:

- recommended pages
- primary KPIs
- global and page filter suggestions
- suggested visual types
- analytical flow
- navigation intent
- audience posture
- business outcome posture
- success-criteria seed

### Design Studio Starting Point

Discovery can currently seed downstream Design Studio artifacts for a selected recommendation.

That seed currently includes:

- Design Brief
- Report Concept and alternate concepts
- Draft seed
- Draft page artifacts
- Draft layout artifacts
- Draft navigation artifacts

This is a design seed, not a generated PBIR artifact.

### Design Package

Discovery can currently produce a provider-neutral Design Package from a selected recommendation.

The package currently includes:

- discovery context
- audience
- experience definition
- pages
- KPIs
- filters
- visual recommendations
- navigation
- analytical flow
- success criteria
- recommendation rationale
- provider guidance
- provenance

This package remains upstream and provider-neutral.

### Downstream Generation Planning Seam

The current repo state now also includes downstream planning seams layered on top of the Design Package:

- Design Package consumption
- generation-request/v1
- Generation Request Framework with request readiness and provider-planning preparation
- execution-plan/v1
- Provider Planning Framework with provider-neutral capabilities, work-unit sequencing, dependency validation, and adapter-readiness planning states
- provider-adapter/v1
- Provider Adapter Framework with adapter registry, compatibility evaluation, and execution-provider readiness states
- microsoft-adapter-specification/v1
- Microsoft Adapter Specification with deterministic Microsoft capability translation, compatibility catalogs, and Microsoft-adapter readiness states
- deterministic prompt segments

These are downstream integration artifacts, not Discovery Wizard execution capabilities.

## Supported Experience Postures

The current discovery system evaluates opportunities across these internal experience types:

- PBIR Report
- Fabric App
- Fabric Data App
- Executive Dashboard
- Operational Monitoring Experience
- Analytical Investigation Experience

Important current limitation:

- Fabric App remains intentionally unsupported in the current provider-facing Generation Request validation path

## Current Trust Boundaries

Discovery Wizard currently preserves these boundaries:

- advisory-only ownership
- no self-validation
- no analyzer ownership
- no Microsoft Skills execution
- no CLI execution
- no PBIR generation
- no Fabric App generation
- no artifact deployment
- no hidden mutation path

It may shape planning artifacts, but it does not gain authority to execute, approve, or validate them.

## Current Implementation Shape

In practical repo terms, Discovery Wizard is currently a backend-first capability.

What is true today:

- the implementation is concentrated in backend discovery services and tests
- the repo contains strong validation and readiness-review docs for discovery output quality
- the current extension manifest does not expose a dedicated shipped Discovery Wizard command or webview flow the way Design Studio does

That means the discovery substrate is real and exercised, but it should be described as implemented advisory infrastructure rather than as a finished end-user surface.

## Current Quality Assessment

The latest current-state assessment in repo memory and docs is:

- Discovery Wizard MVP complete
- recommendation quality is consultant-defensible enough for MVP
- Experience Blueprints are useful downstream baselines
- Design Studio seeding is ready
- Design Package quality is planning-grade trustworthy

The current recommended posture is to stop refining discovery in isolation unless downstream consumers or pilot usage reveal concrete new gaps.

## Known Limitations

The current state still excludes:

- dedicated user-facing Discovery Wizard shell
- provider execution
- Microsoft Skills invocation
- CLI invocation
- direct artifact generation
- provider adapters
- Analyzer Workspace handoff automation
- deployment workflows

Discovery remains a planning and recommendation workflow, not an execution workflow.
