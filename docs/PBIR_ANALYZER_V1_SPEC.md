# PBIR Analyzer V1 Specification

Status: canonical v1 product and behavior specification

If this document conflicts with older PBIR, Design Analyzer, Navigation Scoring, or scoring markdown, this document wins for the v1 product.

## Purpose

`PBIR Analyzer` is a focused VS Code extension for local Power BI PBIP/PBIR report analysis.

Its v1 job is to help a report developer:

- open a local PBIP project
- inspect report, page, and visual structure
- score a report or a single page
- tune analyzer scoring weights and rule defaults
- evaluate governance readiness before publish

This is not a general Power BI modeling platform in v1.

## Product Boundary

### In Scope

- local PBIP project selection
- PBIR report discovery from a workspace
- PBIR tree view for report, pages, and visuals
- React score panel
- React analyzer configuration panel
- full-report scoring
- single-page scoring
- navigation scoring for report UI controls and navigation elements
- governance check command
- extension-global analyzer configuration persistence
- opt-in integration coverage against a real PBIR fixture

### Out of Scope

- creating PBIR reports
- Fabric live connection flows
- semantic-model browsing and authoring
- TMDL editing workflows
- AI/copilot features
- translation workflows
- monitoring dashboards
- web extension support

## Public Command Surface

The intended public v1 command surface is:

- `pbirAnalyzer.openProject`
- `pbirAnalyzer.refreshReports`
- `pbirAnalyzer.scoreReport`
- `pbirAnalyzer.configureScoring`
- `pbirAnalyzer.checkGovernance`

Older command names may still exist as compatibility aliases, but they are not the canonical user-facing surface.

## Core Workflows

### 1. Open PBIP Project

The user selects a local `.pbip` project. The extension resolves the report workspace and populates the PBIR tree.

### 2. Inspect PBIR Tree

The tree must show:

- report root
- page nodes
- visual nodes

The tree must resolve correctly from the selected PBIP workspace root, not from assumptions about the current file.

### 3. Score Report

Scoring supports two modes:

- full-report scoring when the report root is selected
- single-page scoring when a page node is selected

Full-report scoring returns:

- top-level composite score
- per-framework report scores
- per-page breakdown
- per-page scoring errors without failing the entire report when partial results are available

Single-page scoring returns:

- one page score only
- no page tab set
- page-specific feedback and recommendations

### 4. Configure Analyzer

The configuration panel allows the user to:

- enable or disable scoring frameworks
- change framework weights
- reset to defaults
- edit governance rule defaults used by the analyzer configuration

The save gate is strict:

- enabled framework weights must total exactly `100`

### 5. Check Governance

Governance check is a separate publish-readiness evaluation. It reads workspace policy, scores the report, and reports pass or fail with reasons.

## Scoring Model

### Framework Set

The analyzer uses an eleven-framework model. Framework identifiers must normalize to the same logical keys across TypeScript and .NET.

In addition to the eleven framework scores, v1 also requires a separate navigation-scoring mechanism so report UI controls are not treated exactly like primary data visuals.

Core/default-enabled frameworks:

- `gestalt`
- `cognitive`
- `dataink`
- `accessibility`
- `visual`
- `governance`

Optional/default-disabled frameworks:

- `graphical`
- `stephen`
- `tufte`
- `density`
- `narrative`

### Default Weights

The default enabled weights are:

- Gestalt Principles: `25`
- Cognitive Load: `20`
- Data-Ink Ratio: `15`
- Accessibility: `15`
- Visual Best Practices: `15`
- Enterprise Governance: `10`

The default disabled frameworks start at `0`.

### Weight Rules

- only enabled frameworks contribute to the composite score
- enabled framework weights must sum to `100`
- disabled frameworks must contribute `0`
- when no valid config is provided, scoring falls back to the default analyzer configuration

### Navigation Scoring

Navigation Scoring is part of v1 scope.

It is a separate scoring mechanism, not a full-weight peer to the design frameworks themselves. Its job is to account for the fact that modern Power BI reports frequently include navigation and filtering UI that increases interaction complexity, but should not be penalized as heavily as a chart, table, or KPI visual.

#### Required Product Intent

- navigation elements must affect analyzer results
- navigation elements must receive reduced default weight relative to data visuals
- navigation treatment must be configurable by the user
- navigation treatment must live in the existing analyzer configuration model, not in a separate per-report config file

#### Canonical Treatment

The v1 model should treat navigation elements as a lighter-weight "functional ink" class.

The default direction is:

- include navigation elements in complexity-oriented scoring
- apply a reduced weight multiplier relative to a full data visual
- exclude navigation elements from Data-Ink Ratio scoring
- expose separate navigation counts in analyzer output when feasible

#### Expected Element Class

The implementation should classify common report UI elements such as:

- action buttons
- explicit navigation buttons
- shapes used as navigation affordances
- images used as navigation affordances

The exact detection rules can be tightened during implementation, but common Power BI navigation patterns must be recognized and treated separately from data visuals.

#### Default Behavior

For v1, navigation scoring should participate in baseline analyzer behavior rather than remain a dormant future feature.

The current default target is:

- enabled by default
- reduced weight default of `25%` relative to a full data visual

#### Framework Interaction

The required v1 interaction model is:

- `Cognitive Load`: include navigation elements at reduced weight
- `Dashboard Density`: include navigation elements at reduced weight
- `Data-Ink Ratio`: exclude navigation elements
- other frameworks: do not let navigation treatment distort scores as if these elements were ordinary data visuals

#### Additional Analyzer Signal

The implementation should also support a navigation-heavy complexity warning for reports that overuse button-driven and hidden-state interaction patterns. The exact threshold can follow the earlier navigation-scoring proposal unless implementation discovers a better default.

### Framework Intent

- `Gestalt Principles`: grouping, alignment, proximity, similarity, continuity
- `Cognitive Load`: visual density, competing signals, mental effort
- `Data-Ink Ratio`: data signal versus decorative or redundant ink
- `Graphical Perception`: chart encoding suitability for quantitative comparison
- `Accessibility`: contrast, readability, and accessibility-oriented choices
- `Visual Best Practices`: chart choice, labeling, consistency, and common dashboard heuristics
- `Enterprise Governance`: report quality against organization rules and publish expectations
- `Stephen Few`: dashboard clarity and KPI-oriented presentation heuristics
- `Tufte Minimalism`: clarity, precision, and chart-junk reduction
- `Dashboard Density`: balance between information richness and crowding
- `Narrative Design`: page sequencing and story guidance

### Scoring Output Requirements

Every score result must include:

- composite score
- per-framework scores
- framework feedback collections
- recommendations
- report path
- score timestamp

Full-report scoring should also include:

- `pageScores`
- `pageCount`
- `scoringErrors` for partial failures

Single-page scoring should also include:

- `scoredPageName`

### Zero-Visual Behavior

If a page has no data visuals, the analyzer must not crash. It must return a zero-score result with explanatory feedback instead of failing outright.

### Partial Failure Behavior

If some pages fail during full-report scoring:

- successful pages should still contribute results
- failed pages should be listed in `scoringErrors`
- the overall workflow should remain usable

### Bookmark and Custom Visual Tolerance

Reports containing bookmarks, hidden visuals, and custom visuals must not crash scoring.

For v1:

- bookmark-heavy reports are part of the required integration fixture
- custom visuals must parse without breaking tree discovery or scoring
- deeper per-state bookmark scoring is not a required user-facing v1 workflow

## Analyzer Configuration

### Persistence Model

The canonical v1 persistence model is:

- storage in VS Code `globalState`
- key: `designAnalyzerConfig`
- one extension-level analyzer configuration per user profile

This is important: v1 does not use a per-report `pbir-config` file for analyzer scoring configuration.

### Config Shape

The config contains:

- `frameworks`
- `governance`
- `navigationScoring`
- `lastUpdated`

Each framework includes:

- `id`
- `name`
- `enabled`
- `weight`
- optional metadata such as description and reference

Each governance rule includes:

- `id`
- `name`
- `value`
- `adminOnly`
- optional description and severity

In v1, the `adminOnly` flag is descriptive metadata only. The current product does not implement user identity or role-based editing restrictions in the analyzer config panel.

Navigation scoring configuration is also part of the analyzer config. The intended shape includes:

- `enabled`
- reduced-weight value or values used for navigation treatment
- any supporting display or warning options needed by the analyzer UI

### Config Validation and Migration

The config layer must:

- normalize legacy framework IDs
- migrate older saved shapes into the canonical framework list
- validate that enabled weights total `100`
- recover safely when config data is missing or malformed

### Default Governance Rules in Analyzer Config

The shipped analyzer defaults currently include rule definitions for:

- `maxVisualsPerPage`
- `maxBookmarksPerPage`
- `maxLayoutStatesPerPage`
- `maxHiddenVisuals`
- `minWhiteSpaceRatio`
- `allowPieCharts`
- `allowCustomVisuals`
- `requirePageTitle`
- `requireFilterPanel`
- `themeStandard`

These defaults come from `vscode-extension/config/governance-defaults.json` and are used as analyzer-side configuration defaults.

## Governance Evaluation

### Separate Concern From Analyzer Config

There are two governance concepts in the repo and they must not be conflated:

1. analyzer-side governance defaults inside `designAnalyzerConfig`
2. workspace publish policy used by the governance check command

The publish policy is the source of truth for pass/fail governance evaluation.

### Workspace Policy Source

The governance check reads workspace policy from:

- `.vscode/settings.json`
- key: `powerbi-modeling.governance`

Supported policy fields are:

- `enabled`
- `minimumCompositeScore`
- `approvedThemeIds`
- `notes`
- `rules`

### Blocking Rules Required in V1

When governance is enabled, v1 must be able to block on:

- composite score below `minimumCompositeScore`
- theme not in `approvedThemeIds` when that list is non-empty

The result should include:

- blocked or not blocked
- evaluated score
- required threshold
- evaluated theme id
- reasons
- policy notes when relevant

### Dynamic Rules

The governance service can read dynamic rules, but v1 only requires reliable threshold and approved-theme blocking. Dynamic rule evaluation remains extensible and partially placeholder.

## UI Requirements

### Score Panel

The score panel is React-backed and is the canonical analyzer results UI for v1.

It must support:

- loading state
- error state with retry
- single-page view without tabs
- multi-page view with an `Overall` tab plus one tab per page
- per-framework collapsible feedback sections
- recommendations grouped by severity
- refresh action
- quick-fix action when applicable

Tab switching in a completed multi-page result should not trigger re-scoring.

### Config Panel

The config panel is React-backed and is the canonical analyzer configuration UI for v1.

It must support:

- loading current config
- editing framework enablement and weights
- editing governance default rules
- save
- reset to defaults
- validation messages
- open governance JSON defaults for inspection

## Required Real-World Fixture

The primary integration fixture for v1 is:

- `/Users/bcrowell/Documents/GitHub/PBITesting/Sales & Production.pbip`

Required characteristics currently validated against that fixture:

- `21` pages
- `172` visuals
- bookmark references present
- custom visuals present

The fixture must be usable for:

- tree discovery
- full-report scoring
- single-page scoring
- governance evaluation

## Acceptance Criteria for V1

The following conditions define v1 readiness:

- the extension can open `Sales & Production.pbip`
- the PBIR tree loads consistently
- full-report scoring works
- single-page scoring works
- analyzer config save, load, and reset work
- React score and config panels render without falling back to legacy inline HTML
- governance check runs and reports results
- governance check runs and reports pass or fail with reasons
- bookmark and custom visual presence do not break scoring
- navigation elements are treated as reduced-weight functional ink rather than full-weight data visuals
- local build, package, and smoke-test instructions match reality

## Explicit Non-Canonical Historical Requirements

The following ideas appear in older docs but are not canonical v1 requirements unless re-approved:

- per-report analyzer config stored in a `pbir-config` file
- admin-role enforcement in the analyzer config panel
- dedicated user-facing bookmark-state scoring UI
- broader model-authoring or Fabric-management workflows under the analyzer product

## Deferred Items

The following are reasonable next steps but are not required to define v1 behavior:

- further narrowing of non-PBIR backend paths
- trimming the shipped VSIX contents further
- deleting stale plans, backup files, and obsolete markdown
- renaming `LSP` and `LspHost` terminology
- richer dynamic governance rule enforcement
- deeper bookmark-state scoring beyond the v1 navigation-treatment baseline

## Source Notes

This spec was consolidated from the current implementation plus these higher-signal source documents:

- `plans/PBIR_ANALYZER_REACT_FIRST_V1_PLAN.md`
- `plans/pbir_design_analyzer_spec.md`
- `specs/002-design-analyzer-config-panel/spec.md`
- `specs/003-per-page-scoring/spec.md`
- `specs/NAVIGATION_SCORING_SPEC.md`
- `docs/PBIR_ANALYZER_V1_TESTING.md`
- `docs/design-analyzer-config-panel.md`
- `docs/pbir-config-specs-new.md`

Those documents remain useful as historical context, but this file is the canonical v1 specification.
