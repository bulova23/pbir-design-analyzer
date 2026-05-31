# PBIR Design Analyzer 0.2.0 Release Finalization Design

Date: 2026-05-31

Status: Approved design direction captured; ready for execution planning and release finalization

## Goal

Finalize the PBIR Design Analyzer `0.2.0` release without adding new product features beyond:

- roadmap specs and plans for deferred epics
- release and roadmap documentation updates
- release-payload curation
- validated merge into `main`
- packaging and short smoke validation

This pass freezes the already-completed score-panel modernization work and prepares the repository for a clean `0.2.0` release plus a durable next-epic roadmap.

## Release Boundary

This release includes:

- the completed workspace modernization:
  - Overview
  - Issues
  - Fix Plan
  - Evidence
  - secondary Export
- normalized findings
- smart collapse behavior
- intent confirmation and review feedback
- review packet preview and current export positioning
- workspace personas as presentation modes
- cross-page matrix navigation
- documentation updates describing `0.2.0`
- roadmap specs/plans for deferred epics

This release does not include implementation of the deferred epics themselves.

## Workstreams

The release should be executed as three coordinated workstreams.

### 1. Release Payload Curation

The feature worktree currently contains a mixture of:

- intended product code and tests
- release documentation
- roadmap/spec/planning artifacts
- durable repo memory
- transient agent session clutter
- generated or tool-owned noise

The release merge must not assume all current files belong in `main`.

#### Curation Rules

Keep:

- product code
- tests
- package metadata
- final release docs
- final roadmap specs/plans
- compact durable `.agent-memory`

Prune or exclude:

- raw historical session logs
- duplicate implementation notes
- obsolete planning drafts
- transient scratch files
- generated `.vscode-test/` artifacts
- non-durable agent noise

### 2. Docs-First Roadmap Freeze

Before merging, document:

- the actual `0.2.0` product state
- the three deferred epics
- the recommended roadmap order and rationale

This ensures the release ships with:

- clear user-facing guidance
- clear extension guidance
- clear future-agent guidance
- explicit next-version roadmap scope

### 3. Validated Release Integration

Only after curation and documentation:

1. validate in the feature worktree
2. commit the cleaned release payload
3. merge into `main`
4. revalidate on `main`
5. package the `.vsix` from `main`

This avoids ambiguous “release built from side branch” provenance.

## Scope And Merge Boundaries

### Feature Worktree

Source of truth for release staging:

- `.worktrees/feat-semantic-color-chart-intent`

This worktree should be cleaned, validated, and committed before merge.

### Main

Target release branch:

- `main`

`main` should receive only curated, intentional, validated release content.

### Packaging

The `0.2.0` VSIX should be built from `main` after merge and revalidation.

## Deferred Roadmap Epics

Three future epics should be fully spec’d and planned in this release pass.

## Epic 1 — Consultant Deliverables & Export Platform

### Purpose

Evolve the current export and review-packet capability into a clearer consultant-facing deliverables platform.

### Include

- export profiles
- persona-aware export-summary wording
- smarter executive summary language polish
- branded consultant-ready PDF/export profiles
- export workspace redesign
- AI-generated executive narrative/commentary
- future DOCX/PDF architecture

### Architecture Boundary

This epic should build on the current review packet / preview / export pipeline rather than replace it wholesale.

### Dependency Notes

- depends on current packet builder and preview flow
- should remain downstream from score/finding state
- must preserve explainability and score immutability

## Epic 2 — Visual Intelligence & Screenshot Analysis

### Purpose

Turn screenshot audit from text findings into a richer visual review surface.

### Include

- screenshot overlays
- visual annotations
- reading-order visualization
- density heatmaps
- alignment overlays
- focus-area highlighting
- screenshot-to-finding linkage
- visual evidence navigation

### Architecture Boundary

This epic should extend the existing audit/evidence workflows instead of introducing a parallel analysis product.

### Dependency Notes

- depends on existing screenshot capture/audit provider flow
- depends on stable finding IDs and evidence linking
- should stay evidence-first rather than score-first

## Epic 3 — Enterprise Governance & Advanced Review

### Purpose

Expand PBIR Design Analyzer into a more configurable, organization-aware governance and review platform.

### Include

- organization-specific governance profiles
- configuration workspace redesign
- advanced configuration workspace
- benchmark intelligence expansion
- custom standards
- industry templates
- bookmark-state analysis
- mobile/responsive report review enhancements

### Architecture Boundary

This epic should build on current scoring/governance/config contracts without rewriting core scoring behavior.

### Dependency Notes

- depends on stable configuration/state management
- may need new persistence patterns for organization profiles
- should separate scoring rules from presentation and workflow configuration

## Recommended Roadmap Order

### 1. Consultant Deliverables & Export Platform

#### Reason

This has the strongest near-term business value because the product already has:

- review packet preview
- export flows
- consultant-style summary/remediation framing

It is the closest next step to a polished deliverable-oriented product surface.

### 2. Visual Intelligence & Screenshot Analysis

#### Reason

The repo already has screenshot audit foundations, so this is the next strongest differentiator after export/deliverables. It also improves explainability and perceived intelligence without requiring governance redesign first.

### 3. Enterprise Governance & Advanced Review

#### Reason

This is strategically strong but broader and riskier. It touches:

- configuration UX
- governance models
- templates
- expanded benchmark behavior

It should come after the more immediately monetizable/demonstrable deliverable and visual-review layers.

## Roadmap Evaluation Dimensions

The roadmap summary should classify each epic by:

- business value
- implementation risk
- complexity
- quick wins
- strategic value

## Documentation Targets

Use existing repo structure rather than creating duplicate docs.

### Root README

Use [README.md](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/README.md) for:

- high-level product overview
- key features
- installation
- getting started
- workspace concepts
- roadmap summary

### Extension README

Use [vscode-extension/README.md](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension/README.md) for:

- extension installation
- commands
- settings
- score-panel walkthrough
- review workflow
- export behavior
- `0.2.0` workspace updates

### Agent Guidance

Use [AGENTS.md](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/AGENTS.md) for:

- architecture updates
- workspace modernization summary
- normalized findings architecture
- persona architecture
- cross-page matrix architecture
- deferred roadmap references
- release-finalization workflow guidance

### Changelog

Use [docs/CHANGELOG.md](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/docs/CHANGELOG.md) for:

- `0.2.0` release notes
- major features
- known limitations
- roadmap references

### Existing How-To Guide

Use [docs/HOW_TO_USE.md](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/docs/HOW_TO_USE.md) as the detailed user workflow guide for:

- how to run the analyzer
- how to interpret Overview
- how to use Issues
- how to use Fix Plan
- how to use Evidence
- how personas work
- how cross-page matrix navigation works
- what Export currently does

## Durable Memory Policy

The merged repo should keep compact durable memory only.

### Keep

- `.agent-memory/current-focus.md`
- `.agent-memory/session-summaries.md`
- `.agent-memory/repo-map.md`
- one `0.2.0` release summary note
- one roadmap-next-epics summary note if needed
- a very small number of milestone session notes only when they capture final architecture or release decisions not already summarized elsewhere

### Prune

- raw timestamped implementation trails
- duplicate session notes
- obsolete planning logs
- scratch/generated memory artifacts

Principle:

- one durable summary is better than many raw logs

## Validation Plan

Run in the feature worktree before merge:

- `cd vscode-extension && npm run compile`
- `cd vscode-extension && npm test`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`

Run again on `main` after merge:

- `cd vscode-extension && npm run compile`
- `cd vscode-extension && npm test`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`

If practical, perform a short smoke pass by installing the VSIX and verifying:

- Overview loads
- Issues loads
- persona selector works
- cross-page matrix filters Issues
- Fix Plan displays
- Evidence remains secondary
- Export remains available

## Non-Goals

This release-finalization pass does not:

- implement the deferred epics
- add screenshot overlays
- redesign export
- add AI-generated narrative
- add organization-specific governance profiles
- rewrite the configuration workspace
- change scoring algorithms
- change severity/confidence logic
- change backend scoring architecture
- add large dependencies

## Risks

### 1. Merge Pollution

If the curation pass is weak, `main` will absorb raw session clutter and generated noise.

### 2. Release Drift

If docs are not updated before merge/package, the shipped `0.2.0` artifact will not match its documented behavior.

### 3. Packaging From The Wrong State

If the package is built from the feature worktree instead of post-merge `main`, release provenance is weaker.

### 4. Dirty Main

If `main` has unrelated local modifications during merge, merge safety must be reviewed before proceeding.

## Definition Of Done

Done means:

1. Three deferred roadmap epics are fully spec’d and planned.
2. Roadmap documentation is updated.
3. `README.md` is updated.
4. `vscode-extension/README.md` is updated.
5. `docs/HOW_TO_USE.md` is updated.
6. `AGENTS.md` is updated.
7. `docs/CHANGELOG.md` is updated for `0.2.0`.
8. Feature worktree is curated and committed cleanly.
9. Feature branch is merged into `main`.
10. Validation passes on feature worktree and `main`, or skips are documented.
11. `0.2.0` package metadata is correct.
12. A `0.2.0` VSIX is created from `main`.
13. Durable repo memory is updated with release decisions, package path, and next-epic roadmap.
