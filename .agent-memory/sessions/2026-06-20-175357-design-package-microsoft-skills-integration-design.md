# 2026-06-20 17:53:57 ET - Design Package Microsoft Skills Integration Design

## Objective

- create a design specification for converting Discovery Wizard Design Packages into Microsoft Power BI Skills / CLI consumable artifacts
- create a phased implementation plan for that integration path
- define contracts, trust boundaries, analyzer handoff, provenance, lifecycle, and failure handling
- stop after design and planning with no implementation work

## Constraints

- no product-code implementation
- no provider execution
- no Microsoft skills implementation
- no CLI implementation
- no changes to Design Studio ownership
- no changes to Analyzer Workspace ownership

## Planned Evidence

- Discovery Wizard MVP readiness assessment
- Discovery Wizard design specification
- Design Studio design and trust-boundary docs
- Analyzer Workspace architecture docs
- current Design Package backend contract and tests
- Microsoft public guidance for:
  - Power BI agentic capabilities
  - report planner and management skills
  - report authoring skill
  - report design skill
  - Power BI Desktop Bridge
  - PBIR / PBIP
  - Fabric report definitions
  - Fabric Apps and data app template

## Progress

- session opened
- repo guidance and memory intake completed
- Design Package contract, Design Studio ownership, analyzer handoff patterns, and contract-ownership guidance inspected
- current Microsoft public guidance reviewed to avoid inventing stale or unsupported capabilities
- identified the main architectural need as a new versioned Generation Request boundary between Design Package and Microsoft-specific adapters

## Delivered

- wrote `docs/superpowers/specs/2026-06-20-design-package-microsoft-skills-integration.md`
- wrote `docs/superpowers/plans/2026-06-20-design-package-microsoft-skills-integration-plan.md`
- updated repo memory for this design/planning session

## Key Findings

- the existing Design Package is strong enough to act as the upstream planning artifact, but it should not be exposed directly as the provider-facing execution contract
- a provider-neutral `generation-request/v1` boundary is the safest way to preserve backward compatibility and avoid Microsoft-specific leakage into Discovery Wizard output semantics
- PBIR Report is the correct first generated artifact target because Microsoft now has explicit first-party guidance for report authoring, validation, and Desktop verification
- Fabric App terminology now risks collision with Microsoft's new Fabric Apps preview terminology, so that mapping should be locked before Fabric-oriented generation is implemented
- generated artifacts must always remain review-required and analyzer-validated before they gain any trusted status

## Outcome

- design and plan documents created
- trust boundaries, lifecycle, provenance, failure handling, and analyzer integration are now defined
- recommended next step:
  - implement Phase 1 contract work only after the Fabric App terminology mapping decision is explicitly locked
- no product-code changes were made
