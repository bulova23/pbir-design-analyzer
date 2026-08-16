# AI-Assisted Fix Opportunities Design

Date: 2026-05-31

Status: Approved design direction captured; ready for implementation planning

## Goal

Add a remediation-led fix workflow to PBIR Design Analyzer so users can safely move from:

- analyze
- diagnose
- remediate

into:

- propose fix opportunities
- preview exact changes
- approve
- apply
- re-analyze

Phase 1 is deliberately deterministic. It is not an AI implementation in the execution path.

## Strategic Positioning

This roadmap item should be framed as `AI-Assisted Fix Opportunities`, but the first implementation phase is a `Deterministic Fix Opportunity Engine`.

That distinction matters:

- the long-term roadmap is AI-assisted
- the first implementation is trust-first, deterministic, and safe

The objective is to prove:

- fix discovery
- fix opportunity generation
- preview
- apply
- rollback
- re-analysis

before introducing model-driven proposal enrichment.

## Product Problem

The current workspace is strong at:

- diagnosis (`Issues`)
- remediation intent (`Remediation Queue`)
- evidence and context

What it cannot yet do is operationalize supported remediation into safe report changes.

Today the product stops at:

- analyze
- recommend

The next step is:

- analyze
- recommend
- fix
- validate

## Canonical Architecture

The canonical architecture for this roadmap should be:

- `Issues`
- `Remediation Queue`
- `Fix Opportunity Engine`
- `Deterministic Mutation Layer`

Each layer has one job:

- `Issues` identifies problems
- `Remediation Queue` identifies solution intent
- `Fix Opportunity Engine` operationalizes safe solutions
- `Deterministic Mutation Layer` applies approved changes in an auditable, reversible way

This is the long-term architectural direction. Future AI should improve what gets proposed above this stack, not bypass it.

## Architecture Principles

### Remediation-First Workflow

Phase 1 is not centered around findings.

It is centered around remediation.

Reason:

- findings are diagnostic
- remediation is actionable
- multiple findings often map to one remediation action

Example:

- `Visual Density`
- `Decoration Minimalism`
- `Grid Alignment`

may all contribute to one remediation action:

- `Improve Layout Structure`

The product should not generate three unrelated fix flows for three related findings.

### Deterministic Execution Principle

Intelligence may improve proposal quality.

Intelligence must not replace deterministic execution.

This should remain true across every future phase.

### Execution Trust Boundary

All report modifications must ultimately be expressed as:

- explicit fix opportunities
- explicit mutations
- explicit previews
- explicit rollback plans

Regardless of how intelligent future proposal generation becomes.

AI may influence what is proposed.

AI may not bypass the preview/apply/rollback pipeline.

### Human Approval Required

No automatic report modification.

Every apply action requires an explicit user decision.

### Existing-Object Mutation Only

Phase 1 may modify existing report objects.

Phase 1 may not create, delete, or semantically transform report objects.

## Product Scope

### Phase 1 Includes

- remediation-led fix opportunity generation
- deterministic fix opportunity derivation
- structured preview
- approval workflow
- deterministic apply
- deterministic rollback
- automatic re-analysis
- outcome reporting after apply

### Phase 1 Excludes

- new visual creation
- visual deletion
- chart-type replacement
- visual-type swaps
- DAX changes
- model or TMDL semantic changes
- relationship changes
- page restructuring
- bookmark behavior redesign beyond safe metadata normalization
- KPI hierarchy redesign
- visual regrouping redesign
- storytelling transformation
- page sequencing changes
- executive narrative restructuring
- full report architecture redesign
- model/provider-backed proposal generation

## Phase 1 Supported Domains

Phase 1 should support only safe metadata/layout/theme refactors of existing objects:

- title generation from deterministic page context
- title text standardization
- title anchor placement normalization
- semantic color normalization
- theme-role normalization where already represented in files
- alignment normalization
- spacing normalization
- grid normalization
- navigation/button placement normalization
- cross-page consistency normalization where the relevant objects already exist

These are safe-refactor operations, not redesign operations.

## Target Workflow

The intended user flow is:

- `Overview`
- `Issues`
- `Remediation Queue`
- `Fix Opportunities`
- `Preview`
- `Apply`
- `Re-Analyze`

Operationally:

- user reviews findings
- user selects or expands a remediation item
- system generates supported fix opportunities
- user previews exact mutations
- user approves one opportunity
- system applies the change safely
- system re-analyzes and reports actual outcome

## Data Model

### Remediation Item

This already exists as the conceptual solution layer.

It remains the parent context for fix generation.

### Fix Opportunity

Each fix opportunity is an executable proposal derived from one remediation item.

Recommended shape:

```ts
export interface FixOpportunity {
  id: string;
  remediationItemId: string;
  title: string;
  category:
    | 'title'
    | 'semanticColor'
    | 'alignment'
    | 'spacing'
    | 'grid'
    | 'navigation'
    | 'crossPageConsistency';
  summary: string;
  confidence: number;
  safetyClass: 'safe';
  affectedPages: string[];
  targetObjectIds: string[];
  sourceFindingIds: string[];
  expectedResolutions: string[];
  mutations: FixMutation[];
  rollbackPlan: RollbackPlan;
}
```

### Fix Mutation

Mutations are low-level, typed edits against existing file-backed report objects.

Recommended shape:

```ts
export interface FixMutation {
  id: string;
  pageName?: string;
  targetObjectId: string;
  targetFile: string;
  propertyPath: string;
  mutationType:
    | 'setTitleText'
    | 'setPosition'
    | 'setSize'
    | 'setSemanticColor'
    | 'setThemeRole'
    | 'setNavigationPlacement';
  before: unknown;
  after: unknown;
}
```

### Rollback Plan

Rollback must be generated before apply and must not depend on recomputing intent later.

Recommended shape:

```ts
export interface RollbackPlan {
  id: string;
  fixOpportunityId: string;
  fileBackups: Array<{
    targetFile: string;
    beforeContent: string;
  }>;
  reverseMutations: FixMutation[];
}
```

## Derivation Model

The derivation chain should be:

- `Normalized Findings`
- `Remediation Builder`
- `Fix Opportunity Builder`
- `Preview Model`
- `Apply Engine`
- `Re-Analysis`

Rules:

- fix opportunities sit under remediation items, not under raw findings
- one remediation item may produce zero, one, or multiple fix opportunities
- unsupported remediation items remain advisory
- fix opportunities must carry `sourceFindingIds` and `expectedResolutions`
- rollback plans must exist before apply is allowed

Example:

- remediation: `Normalize cross-page title anchors`
- fix opportunities:
  - `Standardize title x-position`
  - `Standardize title y-position`
  - `Standardize title text value`

That keeps remediation conceptual and execution granular.

## Preview Model

Preview is the trust surface.

It must be structured rather than narrative-heavy.

The source of truth is the mutation list.

Primary preview columns:

- object
- property
- before
- after

Example:

- `Page: Customer Analysis`
- `Object: title-textbox-1`
- `Property: x`
- `Before: 42`
- `After: 24`

Preview should also include:

- remediation context
- affected pages
- findings expected to resolve
- confidence
- rollback availability

## Apply Model

Apply should proceed in this order:

1. validate assumptions
2. create backup
3. apply typed mutations
4. record result
5. trigger re-analysis

Validation is mandatory.

If target objects or expected values no longer match preview assumptions:

- do not partially apply
- do not best-effort repair
- mark the opportunity `Stale`
- require regeneration

## Rollback Model

Rollback is a first-class workflow.

Requirements:

- generated before apply
- deterministic
- based on explicit reverse mutations and file backups
- restores original metadata values
- restores original file content for touched sections
- does not depend on re-running fix logic

## Lifecycle State Model

Fix opportunities should surface explicit lifecycle states:

- `Previewed`
- `Approved`
- `Applied`
- `Rolled Back`
- `Stale`
- `Failed Validation`
- `Applied With Unexpected Outcome`

These states should be visible to users and testable in code.

## Re-Analysis And Outcome Reporting

After apply, the system should not merely report success.

It should automatically re-analyze and show proof of effect.

Recommended outcome buckets:

- `Resolved`
- `Improved`
- `Unchanged`
- `Unexpected`

Example:

- `Resolved: Grid Alignment`
- `Improved: Visual Density`
- `Unchanged: Decoration Minimalism`
- `Unexpected: finding still present`

If the fix applied correctly but expected findings did not clear, the state should be:

- `Applied With Unexpected Outcome`

That is not necessarily a rollback condition, but it is important diagnostic feedback.

## UI Placement

Phase 1 should extend the existing remediation-first workspace rather than creating a new top-level refactor surface.

Recommended placement:

- `Issues` stays diagnostic
- `Remediation Queue` gains fix-opportunity generation affordances
- supported remediation items expose:
  - `Generate Fix Opportunities`
  - `Preview`
  - `Apply`
  - `Rollback` when relevant

Not recommended in Phase 1:

- putting primary fix generation directly on issue cards
- creating a separate Refactor workspace

## Testing And Validation Expectations

Phase 1 validation should prove:

- deterministic proposal generation
- deterministic preview generation
- deterministic apply behavior
- deterministic rollback behavior
- deterministic re-analysis flow
- safe failure on stale assumptions
- no scoring changes
- no severity changes
- no confidence changes
- no model or provider dependency

## Roadmap Phases

### Phase 1

`Deterministic Fix Opportunity Engine`

Primary objective:

- safe execution

### Phase 2

`Preview / Apply / Rollback Hardening`

This is operational maturity, not architectural expansion.

Potential work:

- multi-opportunity sequencing
- stronger conflict detection
- richer history
- better diff visualization
- safer batch handling

### Phase 3

`Hybrid Enhancements`

AI may help with:

- title wording
- explanation wording
- rationale summaries
- expected outcome summaries

AI does not generate or apply mutations.

### Phase 4

`Advanced AI Refactoring`

Only after the deterministic execution layer is trusted:

- advisory redesign options
- storytelling proposals
- restructuring proposals
- broader AI-assisted refactoring

Even here, proposal-first behavior should remain preferred.

## Long-Term Strategy

The strategic story should be:

Today:

- analyze
- recommend

Phase 1:

- analyze
- recommend
- fix
- validate

Future:

- analyze
- recommend
- fix
- validate
- AI assists proposal quality above the execution layer

This is stronger than placing AI directly in the execution path because it preserves auditability, reversibility, and user trust.
