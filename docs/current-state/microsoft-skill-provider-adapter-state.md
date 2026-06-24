# Microsoft Skill Provider Adapter Current State

## Summary

Microsoft Skill Provider Adapter is now implemented as the planning-only metadata layer downstream from Microsoft Skills Catalog and upstream from Execution Provider Contract Framework plus Microsoft Runtime Provider Contract.

Its role is:

- define `microsoft-skill-provider-adapter/v1` as the adapter abstraction for future Microsoft skill providers
- define `microsoft-skill-provider/v1` as the descriptive Microsoft skill-provider definition
- define `skill-provider-selection/v1` as the authoritative provider-selection artifact
- register descriptive Microsoft skill providers without loading or invoking anything
- discover providers by category, capability, skill, and target profile
- resolve required Microsoft skills into candidate providers and selected provider candidates
- validate provider capability coverage, skill coverage, target-profile support, prerequisite satisfaction, catalog integrity, and version compatibility
- classify provider readiness for a future skill-provider adapter handoff without executing anything

It is not a Microsoft Skills runtime, not a Microsoft API surface, not a CLI runner, not a provider invocation path, and not an artifact-generation surface.

The downstream PBIR prototype seam is documented separately in `docs/current-state/pbir-execution-prototype-boundary-state.md`; provider selection remains descriptive input to that boundary rather than an invocation surface.

## Current Product Position

Microsoft Skill Provider Adapter currently sits after:

- Design Package Consumption Layer
- Generation Request Framework
- Execution Plan Framework
- Provider Adapter Framework
- Microsoft Adapter Specification
- Capability Negotiation Framework
- Microsoft Skills Catalog

It currently informs:

- Planning Orchestration Framework
- Execution Provider Contract Framework
- Microsoft Runtime Provider Contract

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs and approves
- Planning Frameworks create planning outcomes and execution candidates
- Microsoft Skills Catalog maps capabilities to descriptive skills
- Microsoft Skill Provider Adapter maps descriptive skills to descriptive providers
- Microsoft Runtime Provider Contract defines Microsoft runtime compatibility only
- Analyzer Workspace remains the downstream validation owner for future generated artifacts

## What Exists Today

The implemented Microsoft skill-provider layer currently includes:

- `microsoft-skill-provider-adapter/v1`
- `microsoft-skill-provider/v1`
- `skill-provider-selection/v1`
- `MicrosoftSkillProviderRegistry`
- `MicrosoftSkillProviderResolutionService`
- `MicrosoftSkillProviderCompatibilityValidator`
- `MicrosoftSkillProviderReadinessService`
- `MicrosoftSkillProviderAdapterFrameworkService`
- explicit Microsoft skill-provider readiness states:
  - `unsupported`
  - `partiallySatisfied`
  - `satisfied`
  - `readyForSkillProviderAdapter`
- boundary tests proving the layer stays metadata-only

## Provider Model

The authoritative provider artifact is `microsoft-skill-provider/v1`.

Each provider definition currently requires:

- schema version
- provider id
- provider name
- provider version
- provider category
- provider status
- supported execution modes
- supported skills
- supported capabilities
- supported target profiles

The default descriptive inventory currently includes:

- Microsoft Power BI Skills Provider
  - available
  - supports PBIR report authoring, design, and validation skill mappings
- Microsoft Fabric Data App Skills Provider
  - planned
  - supports future-facing Fabric Data App template skill mappings

No provider definition loads a provider, opens a runtime, or performs execution.

## Adapter And Selection Contracts

The authoritative adapter artifact is `microsoft-skill-provider-adapter/v1`.

It currently captures:

- adapter identity
- provider category
- provider-schema compatibility
- Microsoft skill-catalog compatibility
- selection-schema compatibility
- Microsoft runtime-provider compatibility
- supported target profiles
- supported execution modes

The authoritative provider-selection artifact is `skill-provider-selection/v1`.

It currently captures:

- required skills
- candidate providers
- selected provider candidates
- unsupported skills
- coverage summary
- readiness summary

Selection is descriptive only.

It does not invoke a provider.

## Provider Resolution Lifecycle

The current deterministic lifecycle is:

Capability Negotiation  
↓  
Microsoft Skills Catalog Resolution  
↓  
Microsoft Skill Provider Selection  
↓  
readyForSkillProviderAdapter or fail-closed state

Resolution currently produces:

- provider candidate set
- selected provider candidates
- skill coverage summary
- capability coverage summary
- unsupported skill summary
- readiness summary

## Capability Coverage Model

The current coverage model separates:

- required skills requested
- required skills covered
- optional skills requested
- optional skills covered
- required capabilities requested
- required capabilities covered
- optional capabilities requested
- optional capabilities covered
- unresolved required capabilities
- unresolved optional capabilities

The current PBIR-ready path maps:

- required skills
  - Power BI Report Authoring
  - Power BI Report Design
- optional skills
  - Power BI Validate Report
- selected provider candidates
  - Microsoft Power BI Skills Provider

## Compatibility Validation Model

`MicrosoftSkillProviderCompatibilityValidator` currently validates:

- provider shape
- missing required fields
- duplicate provider ids
- target-profile support
- skill coverage
- capability coverage
- prerequisite satisfaction
- provider-definition and selection version compatibility
- provider-to-skill catalog integrity

Validation fails closed.

## Current Readiness Model

Microsoft skill-provider readiness currently means descriptive mapping state only.

The current meanings are:

- unsupported
  - the target profile itself is unsupported by the selected provider set
- partiallySatisfied
  - at least one required skill, required capability, prerequisite, or integrity rule is still unresolved
- satisfied
  - all required skills and capabilities are covered and the provider metadata is valid
- readyForSkillProviderAdapter
  - the required skills and provider candidates are known and mapped for a future skill-provider adapter seam

`readyForSkillProviderAdapter` does not imply execution.

It only means the planning stack has a coherent provider mapping.

That mapping is now consumed downstream as selected provider metadata in the PBIR execution request envelope, still without invocation.

## Current Trust Boundaries

The current Microsoft skill-provider layer does not:

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
- provider invocation
- CLI-backed Microsoft execution
- PBIR generation
- Fabric App generation
- Fabric Data App generation
- artifact intake and quarantine
- deployment workflows
- Analyzer Workspace automation
