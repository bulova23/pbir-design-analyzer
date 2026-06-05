# Fabric App Review Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Release Slice 2 Fabric App Review Mode so semantic-model-backed analytical Fabric Apps can be reviewed through the existing shared workspace without introducing a new workspace, a second findings system, or any repo-mutation behavior.

**Architecture:** Reuse the Slice 1 analyzable-surface, analyzer-registry, analyzer-profile, normalized-findings, and shared-workspace architecture. Add a bounded Fabric App surface detector, a FabricAppReviewAnalyzer, analytics-focused repo evidence extraction, additive findings and governance derivation, and presentation-only workspace extensions for Overview, Issues, Fix Plan, and Evidence.

**Tech Stack:** TypeScript, React, VS Code extension host/webview, Jest, existing score-panel contracts, existing screenshot audit session model, existing advisory AI enrichment boundary, existing normalized findings and governance export seams

---

## Planning Intent

This is a planning-only document for Release Slice 2.

Do:

- extend the current analyzable-surface platform
- keep one shared workspace
- keep normalized findings, evidence, remediation, and governance as shared models
- keep Fabric App Review advisory-only
- keep evidence extraction bounded to analytics UX

Do not:

- implement code generation
- implement deterministic mutation for Fabric Apps
- review backend architecture, GraphQL, infrastructure, or CRUD workflows
- introduce a second remediation system
- expand into general software-engineering review

## Minimum Analyzable Fabric App

### Recommendation

Recommend **Option B as the base**, with one explicit scope qualifier:

- **TypeScript + routes/navigation definitions**
- plus **at least one semantic-model-backed analytics indicator**

Practical Version 1 rule:

- A repo qualifies as a supported Fabric App surface only when it contains:
  - analytics-facing TypeScript or TSX source that defines app layout, visual composition, or analytical interactions
  - explicit route or navigation definitions that establish app-level evidence flow
  - at least one bounded semantic-model-related artifact or configuration reference that ties the experience to analytical data consumption

Optional but not required for minimum qualification:

- screenshots
- design token files
- CSS variable definitions

### Option Evaluation

**Option A: TypeScript only**

- Reject as too weak.
- It is too easy to misclassify a generic frontend repo as an analytical Fabric App.
- It does not establish app navigation, executive-to-detail flow, or even that the repo exposes an analyzable analytical experience.

**Option B: TypeScript + routes**

- Closest to the right minimum.
- It establishes an app-shaped analytical surface and unlocks the first high-value review domains:
  - layout/composition
  - navigation
  - accessibility from component structure
  - governance signals around navigation standards
- It still needs one semantic-model-backed signal to stay inside product scope.

**Option C: TypeScript + routes + screenshots**

- Too strict for minimum qualification.
- Screenshots improve review quality, especially for composition and accessibility posture, but they are not necessary to classify a repo as an analyzable analytical app.
- Making screenshots mandatory would block valid repos and couple Slice 2 to Visual Intelligence readiness too early.

**Option D: TypeScript + routes + design tokens**

- Too strict for minimum qualification.
- Design tokens are highly valuable evidence, but absence of tokens is itself a meaningful review finding.
- Requiring tokens up front would exclude real apps that most need governance and standardization review.

### Rationale

This recommendation best preserves the product boundary:

- broad enough to onboard real Fabric App repos early
- narrow enough to fail closed on generic frontend repos
- sufficient to support analytics UX review rather than code review
- compatible with future optional evidence enrichment from screenshots and tokens

### Unsupported And Ambiguous States

Return `unsupported` when:

- the repo lacks TypeScript or TSX analytical UI evidence
- the repo lacks route or navigation definitions
- the repo has no semantic-model-backed analytics indicator
- the repo looks like generic scaffolding, CRUD, or workflow software

Return `ambiguous` when:

- the repo has analytical UI signals but no clear semantic-model indicator
- the repo has routes but navigation appears operational rather than analytical
- the repo contains mixed app patterns and discovery cannot determine the review surface safely

## Release Slice 2 Scope

### In Scope

- semantic-model-backed analytical Fabric Apps
- dashboard-style applications
- visualization-as-code analytical experiences
- Rayfin-based reporting experiences where the review target is analytics UX
- TypeScript review only where it reveals analytics experience structure
- design-token review
- route and navigation review
- accessibility review
- screenshot-backed evidence when available
- semantic-model-usage review from bounded artifacts
- analytics-governance review

### Out Of Scope

- CRUD or workflow app review
- backend architecture review
- GraphQL review
- infrastructure review
- repo mutation
- automatic fixes
- code generation
- general code quality review

## File Map

### Existing Files To Modify

- `vscode-extension/src/analyzer/surfaces/types.ts`
  - widen `SurfaceEvidenceKind` and available profile support for Fabric App review
- `vscode-extension/src/analyzer/surfaces/discovery.ts`
  - delegate to a dedicated Fabric App detector instead of PBIR-only logic
- `vscode-extension/src/analyzer/analyzers/registry.ts`
  - register Release Slice 2 profile availability and default selection behavior
- `vscode-extension/src/analyzer/analyzers/types.ts`
  - extend analyzer-profile typing for `governance` and `accessibility` on `fabricAppReview`
- `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - add additive Fabric App review evidence, analyzer metadata, and governance signal contracts
- `vscode-extension/src/views/scoreResultPayload.ts`
  - thread Fabric App review outputs into the shared payload
- `vscode-extension/src/views/PbirScorePanel.ts`
  - support non-PBIR surface selection, analyzer selection, and advisory-only UI states
- `vscode-extension/src/analyzer/score/normalizedFindings.ts`
  - support richer evidence provenance and Fabric App finding source kinds
- `vscode-extension/src/analyzer/score/overviewSummary.ts`
  - derive app-quality, governance-posture, and accessibility-posture summary content
- `vscode-extension/src/analyzer/score/fixPlan.ts`
  - shape advisory standardization and governance actions for Fabric App findings
- `vscode-extension/src/analyzer/score/presentation.ts`
  - keep result summary logic surface-aware without branching the workspace
- `vscode-extension/src/analyzer/score/personaPresentation.ts`
  - add emphasis rules for `fabricAppQuality`, `governance`, and `accessibility`
- `vscode-extension/src/analyzer/score/governanceExport.ts`
  - export additive analytics-governance signals for Fabric App review
- `vscode-extension/src/analyzer/audit/types.ts`
  - allow screenshot evidence references to be reused by Fabric App findings if current typing is too PBIR-specific
- `vscode-extension/src/analyzer/audit/session.ts`
  - reuse existing screenshot linkage primitives without creating a parallel store
- `vscode-extension/webview-src/analyzer-score/App.tsx`
  - render Fabric App overview cards, issues, evidence groupings, and advisory fix-plan items
- `vscode-extension/webview-src/analyzer-score/styles.css`
  - style Fabric App-specific evidence badges and summary cards inside the current workspace
- `vscode-extension/webview-src/analyzer-score/App.test.tsx`
  - cover Fabric App review rendering and advisory-only invariants

### New Files To Create

- `vscode-extension/src/analyzer/surfaces/fabricAppDiscovery.ts`
  - detect minimum analyzable Fabric App repos and return supported, unsupported, or ambiguous states
- `vscode-extension/src/analyzer/fabric/review/reviewTypes.ts`
  - define Fabric App review domain outputs, evidence kinds, and bounded analyzer result types
- `vscode-extension/src/analyzer/fabric/review/fabricAppReviewAnalyzer.ts`
  - orchestrate Slice 2 review for Fabric App surfaces
- `vscode-extension/src/analyzer/fabric/review/repoEvidence.ts`
  - coordinate artifact discovery and evidence aggregation
- `vscode-extension/src/analyzer/fabric/review/typescriptEvidence.ts`
  - extract layout, KPI grouping, composition, and interaction evidence from TS or TSX
- `vscode-extension/src/analyzer/fabric/review/navigationEvidence.ts`
  - extract routes, executive-to-detail flow, and evidence-flow signals
- `vscode-extension/src/analyzer/fabric/review/designTokenEvidence.ts`
  - extract token definitions, CSS variables, token bypasses, and theme-standard signals
- `vscode-extension/src/analyzer/fabric/review/screenshotEvidence.ts`
  - link optional screenshot evidence into the shared evidence model
- `vscode-extension/src/analyzer/fabric/review/semanticModelEvidence.ts`
  - extract bounded semantic-model-consumption evidence and anti-patterns
- `vscode-extension/src/analyzer/fabric/review/reviewFindings.ts`
  - convert extracted evidence into normalized findings and remediation items
- `vscode-extension/src/analyzer/governance/analyticsGovernanceTypes.ts`
  - define analytics-focused governance categories that work across analyzers
- `vscode-extension/src/analyzer/governance/analyticsGovernanceRules.ts`
  - derive token, navigation, accessibility, and semantic-model governance signals
- `vscode-extension/src/test/fabricAppDiscovery.test.ts`
- `vscode-extension/src/test/fabricAppReviewAnalyzer.test.ts`
- `vscode-extension/src/test/typescriptEvidence.test.ts`
- `vscode-extension/src/test/navigationEvidence.test.ts`
- `vscode-extension/src/test/designTokenEvidence.test.ts`
- `vscode-extension/src/test/screenshotEvidence.test.ts`
- `vscode-extension/src/test/semanticModelEvidence.test.ts`
- `vscode-extension/src/test/analyticsGovernanceRules.test.ts`

## Analyzer Architecture

### Surface Type

- `Fabric App Surface`

### Analyzer

- `FabricAppReviewAnalyzer`

### Profiles

Release Slice 2 minimum profiles:

- `default`
- `fabricAppQuality`
- `governance`
- `accessibility`

### Architecture Rules

- `surface = thing being reviewed`
- `analyzer = review engine operating on that surface`
- `profile = emphasis lens for that analyzer`

Release Slice 2 must preserve the existing flow:

`Analyzable Surface`
`-> Surface Discovery`
`-> Analyzer Selection`
`-> Analyzer Profile Selection`
`-> Analysis`
`-> Normalized Findings / Evidence / Remediation / Governance`
`-> Overview / Issues / Fix Plan / Evidence / Export`

### Analyzer Responsibilities

`FabricAppReviewAnalyzer` should:

- accept only `fabricApp` surfaces
- orchestrate bounded evidence extraction
- derive additive findings and governance signals
- emit analyzer metadata and evidence provenance
- remain advisory-only

`FabricAppReviewAnalyzer` should not:

- mutate files
- run broad repo linting
- inspect backend or infrastructure systems
- become a generic frontend analyzer

### Profile Behavior

`default`

- balanced prioritization across layout, navigation, tokens, accessibility, semantic-model usage, and governance

`fabricAppQuality`

- emphasize app composition, clarity, navigation flow, and analytical experience quality

`governance`

- emphasize standards compliance, token discipline, navigation conventions, and semantic-model consistency

`accessibility`

- emphasize contrast signals, readable sizing, keyboard-friendly navigation structure, semantics, and evidence-backed accessibility gaps

Future expansion should be supported by registration metadata, not by workspace forks.

## Evidence Extraction Architecture

### Extraction Principles

- keep extraction bounded to analytics UX
- inspect only evidence sources that can affect analytical experience quality
- fail closed on missing core repo artifacts
- degrade gracefully on optional evidence sources

### Evidence Sources

**TypeScript layout definitions**

- target component trees, dashboard sections, KPI blocks, visual composition wrappers, and analytical interaction patterns
- derive density, grouping, spacing, hierarchy, and executive-to-detail composition evidence

**Route and navigation definitions**

- target route maps, menu structures, tab shells, breadcrumbs, deep-link patterns, and drill-style navigation mappings
- derive route clarity, evidence flow, and executive-to-detail findings

**CSS variables and design token files**

- target token declarations, theme files, CSS variables, and style usages that bypass the token system
- derive token consistency, theme compliance, and token-bypass findings

**Screenshots**

- optional evidence source
- link current screenshot session artifacts into findings for visual confirmation
- do not make screenshots mandatory for analysis success

**Semantic-model-related artifacts**

- inspect only bounded artifacts that show analytical data consumption
- examples:
  - query-definition files
  - configuration references
  - semantic-model bindings
  - analytical field mapping definitions
- derive query-pattern and business-logic-fragmentation findings without broad data-platform review

**Analytics UX configuration**

- inspect app-shell, theme, routing, and visualization configuration only where it affects the analytical experience

### Review Domains To Support

**Layout & Composition**

- density
- spacing
- grouping
- visual hierarchy

**Navigation**

- route clarity
- evidence flow
- executive-to-detail flow

**Design Tokens**

- token consistency
- token bypass
- theme compliance

**Accessibility**

- contrast indicators
- readability
- sizing
- navigation accessibility

**Semantic Model Usage**

- query patterns
- business logic fragmentation
- analytical consistency

**Governance**

- approved design standards
- token standards
- navigation standards
- accessibility standards

### Extraction Boundaries

Do not extract or reason about:

- generic test coverage
- dependency freshness
- backend service design
- CI or infrastructure layout
- code style unrelated to analytics UX

## Findings Model Extensions

### Additive Only

Preserve:

- normalized findings
- evidence model
- remediation model

Add:

- Fabric App-specific finding source kinds
- analyzer metadata
- evidence provenance
- analytics-governance signal references

### Contract Additions

Extend the shared result contract to support:

- Fabric App review summary
- Fabric App review evidence kinds
- review-domain rollups
- evidence provenance such as file path, route id, token id, screenshot capture id, and semantic-model artifact id
- analyzer-profile metadata already aligned with Slice 1

### Fabric App Finding Categories

Planned normalized finding families:

- token violations
- navigation findings
- accessibility findings
- semantic-model findings
- layout and composition findings
- governance findings

These stay in the existing normalized finding model, with new `sourceKind`, `impactArea`, and evidence references where needed.

### Remediation Behavior

Fix Plan outputs remain advisory:

- standardization actions
- governance actions
- UX improvement actions
- semantic-model consistency actions

No deterministic Fabric App execution path should be introduced.

## Workspace Integration

### Overview Additions

Add analyzer-specific summary content without changing workspace structure:

- App Quality Score
- Governance Posture
- Accessibility Posture

Recommended presentation rule:

- derive these as additive summary cards or callouts from analyzer output
- do not reinterpret underlying findings differently per tab

### Issues Additions

Render Fabric App findings through the existing Issues surface:

- Token Violations
- Navigation Findings
- Accessibility Findings
- Semantic Model Findings

Keep:

- shared severity semantics
- shared filtering model
- shared grouping behavior

### Fix Plan Additions

Render advisory-only action groupings such as:

- Standardization Actions
- Governance Actions
- UX Improvements

Keep:

- existing fix-plan component
- no preview/apply/rollback affordances for Fabric App review items

### Evidence Additions

Render grouped evidence inside the existing Evidence surface:

- Code Evidence
- Screenshot Evidence
- Token Evidence
- Navigation Evidence
- Semantic Model Evidence

Evidence should remain explainable and traceable to bounded artifacts.

### Host Integration

`PbirScorePanel` is still the host seam in Version 1.

Implementation planning assumption:

- keep the existing panel and messaging architecture
- make its title and payload surface-aware where needed
- avoid a second Fabric-only panel unless later product requirements force it

## Governance Integration

### Governance Scope

Release Slice 2 governance remains analytics-focused only.

Supported governance families:

- token standards
- navigation standards
- accessibility standards
- semantic-model consistency standards

### Governance Derivation Model

Recommended flow:

- evidence extractors produce bounded raw signals
- `analyticsGovernanceRules.ts` converts those into typed governance signals
- analyzer output threads governance signals into:
  - Overview posture summaries
  - Issues findings when policy gaps are user-visible
  - Evidence references
  - Export data

### Governance Boundaries

Do not expand governance derivation into:

- infrastructure governance
- source-control governance
- DevOps process review
- generic application architecture governance

## Testing Strategy

### Required Test Areas

Create targeted coverage for:

- surface discovery
- analyzer selection
- evidence extraction
- findings generation
- governance derivation
- workspace rendering
- screenshot linkage
- profile behavior

### Test Matrix

**Surface discovery**

- supported Fabric App repo returns `fabricApp`
- TypeScript-only repo returns `unsupported`
- TypeScript-plus-routes but no semantic-model indicator returns `ambiguous`
- operational or CRUD-shaped repo returns `unsupported`

**Analyzer selection**

- `fabricAppReview` available only for `fabricApp` surfaces
- default selection for Fabric App surfaces resolves to `fabricAppQuality`
- `governance` and `accessibility` profiles are allowed for `fabricAppReview`

**Evidence extraction**

- TypeScript extractor identifies analytical layout and KPI structure
- navigation extractor identifies route clarity and executive-to-detail patterns
- token extractor identifies token usage and bypass
- semantic-model extractor identifies bounded analytical data-consumption patterns
- missing optional screenshots degrade gracefully

**Findings generation**

- each review domain can produce normalized findings
- evidence references carry provenance
- remediation items remain advisory-only

**Governance derivation**

- standards signals derive from token, navigation, accessibility, and semantic-model evidence
- governance output stays analytics-scoped

**Workspace rendering**

- Overview renders Fabric App posture summaries
- Issues renders Fabric App findings in the shared model
- Fix Plan shows advisory actions only
- Evidence groups show code, token, route, screenshot, and semantic-model evidence

**Screenshot linkage**

- existing screenshot session artifacts can be referenced by Fabric App findings
- no parallel screenshot subsystem is created

**Profile behavior**

- `default` remains balanced
- `fabricAppQuality` reprioritizes composition and navigation
- `governance` reprioritizes standards findings
- `accessibility` reprioritizes accessibility findings

### Validation Commands

Planned focused validation:

- `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/fabricAppDiscovery.test.ts src/test/analyzerRegistry.test.ts src/test/fabricAppReviewAnalyzer.test.ts src/test/typescriptEvidence.test.ts src/test/navigationEvidence.test.ts src/test/designTokenEvidence.test.ts src/test/screenshotEvidence.test.ts src/test/semanticModelEvidence.test.ts src/test/analyticsGovernanceRules.test.ts src/test/scoreResultPayload.test.ts`
- `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`
- `cd vscode-extension && npm run compile`
- `cd vscode-extension && npm test`

## Rollout Strategy

### Release Sequence

**Step 1: Discovery and registry hardening**

- complete minimum-surface detection
- add profile availability
- prove unsupported and ambiguous states are safe

**Step 2: Evidence extraction and findings**

- implement bounded extractors
- derive additive normalized findings
- verify advisory-only remediation output

**Step 3: Governance and workspace integration**

- add governance derivation
- surface posture summaries, issues, evidence, and advisory actions through the shared workspace

**Step 4: Hardening**

- run focused tests
- run compile and full test regression
- verify no PBIR deterministic workflow regression

### Feature Gating Recommendation

Prefer a soft launch posture for Slice 2:

- keep Fabric App Review surfaced only for supported repos
- fail closed for ambiguous repos with clear user messaging
- keep screenshots optional
- keep all actions advisory

### Documentation Rollout

When implementation ships, update:

- `docs/ROADMAP.md`
- `docs/CHANGELOG.md`
- any operator-facing workflow documentation that explains supported surfaces and advisory-only behavior

## Risk Assessment

### Architectural Risks

- discovery heuristics may misclassify generic frontend repos as analytical Fabric Apps
- `scorePanel.ts` may accumulate too many cross-surface contract additions if review types are not split cleanly
- evidence extractors may drift into generic code review unless boundaries are enforced in tests

### Product Risks

- users may expect deterministic fixes once Fabric findings appear in Fix Plan
- missing screenshots may be misread as missing support instead of optional evidence
- governance posture may be over-trusted if semantic-model evidence is sparse

### Delivery Risks

- host assumptions may still be PBIR-shaped in more places than Slice 1 exposed
- screenshot reuse may reveal PBIR-specific typing or naming seams
- route detection conventions may vary across Fabric App repos and require one narrow discovery pass before expansion

### Mitigations

- enforce minimum-surface qualification in tests first
- keep optional evidence sources explicitly optional in contracts and UI wording
- keep remediation labels advisory and non-executable
- prefer small dedicated review modules over growing shared files blindly

## Self-Review

### Coverage Check

This plan covers the requested areas:

- file map
- analyzer architecture
- evidence extraction architecture
- findings model extensions
- workspace integration
- governance integration
- testing strategy
- rollout strategy
- minimum analyzable Fabric App recommendation

### Boundary Check

The plan preserves:

- one shared workspace
- one normalized findings model
- one remediation model
- PBIR-only deterministic preview/apply/rollback authority
- advisory-only AI relationship

### Scope Check

The plan excludes:

- code generation
- repo mutation
- automatic fixes
- GraphQL review
- backend review
- infrastructure review
- operational app review
- general software-engineering review

## Recommendation On Implementation Order

Implement **Phase 4 Advanced AI Refactoring before Release Slice 2 Fabric App Review Mode**.

Reason:

- Phase 4 deepens an already-shipped PBIR surface and reuses the existing Phase 3 proposal-enrichment stack
- Slice 2 introduces a new surface, new discovery heuristics, new evidence extraction, and new governance derivation, which is a materially larger platform expansion
- Phase 4 can establish stronger cross-surface advisory proposal contracts first, which Slice 2 can later reuse without reopening trust-boundary decisions

Recommended order:

1. Phase 4 Advanced AI Refactoring
2. Fabric App Review Mode Release Slice 2
