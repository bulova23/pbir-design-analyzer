# Story Assessment 2.1 Guided Story Improvements Design

Date: 2026-06-11

Status: Design approved for planning; implementation deferred

## Goal

Design the first user-facing Story Assessment enhancement using only the six validated Story Gap categories from Level 1 review.

The feature should help users answer:

- what story is this page trying to tell
- what would make that story stronger

without exposing research-stage Story Assessment signals.

## Authoritative Inputs

This design uses the following as authoritative inputs:

- `docs/story-assessment/2026-06-11-level1-promotion-decision-report.md`
- Level 1 validation export results
- Story Assessment 2.0 validation findings and targeted tuning observations

## Planning Boundary

This is a design specification only.

It does not:

- implement code
- change the extension
- change the score-panel contract now
- expose research-stage Story Assessment internals

## Business Objective

Level 1 validation identified one narrow Story Assessment promotion path that is accurate enough to begin productization:

- validated Story Gap guidance

The product goal is not to expose Story Assessment 2.0 internals.

The product goal is to create a consultant-friendly improvement workflow that:

- strengthens the page story
- explains why the story is weaker
- tells the user what would improve it most

## Validated Gap Set

Guided Story Improvements may use only these validated categories:

- Missing Title / Question Anchor
- Missing Benchmark / Target
- Missing Prior-Period Context
- Missing Primary Metric
- Missing Primary Dimension
- Scattered Filters

All other Story Assessment signals remain internal-only.

## Core Product Principle

Story Assessment should not become another Issues list.

Fix Plan should not become another Story Assessment list.

The intended hierarchy is:

Story Assessment  
↓  
Guided Story Improvements  
↓  
Issues  
↓  
Fix Plan

Story Assessment explains the current narrative posture.

Guided Story Improvements explains what would strengthen that narrative.

Issues and Fix Plan consume those validated story recommendations downstream.

## Chosen Architecture

Use Option 3.

Add a small new score-panel subsection:

- `Guided Story Improvements`

This subsection becomes the source of truth for validated Story Assessment recommendations.

It is:

- distinct from Story Assessment
- upstream from Issues
- upstream from Fix Plan
- advisory rather than score-like

It is not:

- a new research surface
- a raw Story Assessment dump
- a replacement for Issues
- a replacement for Fix Plan

## UX Placement

### Panel Order

Recommended order inside the score-panel workspace:

1. Overview summary block
2. Story Assessment
3. Guided Story Improvements
4. Issues
5. Fix Plan
6. Evidence
7. Export

### Why This Placement

This placement preserves a clear narrative progression:

- Story Assessment explains the page’s current story posture
- Guided Story Improvements translates that posture into targeted improvements
- Issues shows concrete surfaced findings
- Fix Plan sequences implementation work

### Relationship To Story Assessment

Story Assessment remains a concise explanation surface:

- Detected Story
- Supported Decision
- Why This Matters
- Decision Risk

Guided Story Improvements should appear immediately below it and read like the next logical question:

- what would make this story stronger

## Guided Story Improvements Content Model

The subsection contains three blocks:

### High Priority Improvements

Shows the most important validated story improvements that materially affect readability, decision support, or narrative clarity.

### Medium Priority Improvements

Shows meaningful but less urgent improvements that sharpen the page story after the highest-value gaps are addressed.

### Story Improvement Rationale

Explains, in one compact narrative paragraph, why the current story is weaker and what class of change would improve it.

This rationale is synthesized only from the validated gap set and existing public Story Assessment fields.

## Recommendation Model

Each Guided Story Improvement recommendation contains:

- title
- user-facing guidance
- why it matters
- expected impact
- priority
- related issue signal category
- fix-plan mapping hints

### User-Facing Recommendation Shape

Example:

- `Add a clearer page question or title`
- `This page does not establish the decision or question early enough.`
- `A clearer title helps readers understand what the page is meant to explain before they interpret the visuals.`
- `Expected impact: stronger scan path and faster narrative comprehension.`

## Priority Model

Guided Story Improvements supports three user-facing priority levels:

- High Priority
- Medium Priority
- Informational

Only High and Medium are shown by default in the initial product slice.

Informational exists in the model for future expansion but should remain hidden in the first release unless testing shows it adds clarity without noise.

### Priority Rules

High Priority:

- missing title/question anchor
- missing benchmark/target
- missing primary metric

These are first because they most directly impair the user’s ability to understand the page’s claim and decision posture.

Medium Priority:

- missing prior-period context
- missing primary dimension
- scattered filters

These matter strongly, but usually after the page has a visible story anchor and decision frame.

### Escalation Rules

Promote a Medium item to High when:

- the page already has multiple story-related issues and the missing element blocks interpretation
- the missing element affects the lead reading path
- the missing element likely causes downstream confusion in Issues or Fix Plan

Keep an item at Medium when:

- the page story is understandable but less efficient
- the missing element sharpens, rather than establishes, the narrative

### Suppression Rules

Recommendations must be suppressed when internal special-page guardrails mark the page as diagnostic-only or non-reviewable, including:

- Tooltip
- Q&A
- What If
- Key Influencers
- Market Basket
- Reference / Legal
- Validation / Sandbox

Those internal rules remain hidden and are not exposed in the UI.

## Explanation Model

Every recommendation should answer three questions:

1. Why is the story weaker now?
2. What change would help?
3. What impact should the user expect?

### Explanation Tone

The tone should be:

- consultant-like
- concise
- specific
- non-technical

Avoid:

- internal signal names
- validation language
- model-centric phrasing
- research-stage diagnostic terms

### Wording Principles

Use phrases like:

- `clarify the page question`
- `add a benchmark or target`
- `show the current result against a prior period`
- `name the primary metric more clearly`
- `make the main comparison dimension easier to identify`
- `consolidate filter entry points`

Avoid phrases like:

- `semantic coherence`
- `promotion state`
- `competing story`
- `archetype mismatch`
- `confidence breakdown`

## Remediation Guidance Model

Each validated gap maps to a consultant-friendly guidance pattern.

### Missing Title / Question Anchor

Guidance:

- clarify the page title or leading question
- make the page promise visible before the user reads the visuals

### Missing Benchmark / Target

Guidance:

- add a visible target, benchmark, or budget comparison
- show what good or bad performance means

### Missing Prior-Period Context

Guidance:

- compare the current result to a prior period
- make change over time visible in the lead reading path

### Missing Primary Metric

Guidance:

- label the main metric clearly and consistently
- ensure the lead visual and surrounding text point to the same measure

### Missing Primary Dimension

Guidance:

- make the main grouping dimension obvious
- reduce ambiguity about what the page is comparing

### Scattered Filters

Guidance:

- consolidate filters into one clear control band or exploration entry point
- reduce narrative fragmentation caused by distributed controls

## Interaction Model

### Default State

Guided Story Improvements should render as a compact subsection with:

- up to three visible recommendations by default
- High Priority first
- Medium Priority second
- a short rationale paragraph beneath the recommendation groups

### Recommendation Cards

Each recommendation should support:

- concise summary
- expandable `Why this helps` detail
- optional trace into related Issues
- optional trace into Fix Plan items

### Navigation Behavior

Guided Story Improvements should not create a new navigation tab.

It should exist inside the score-panel flow as a subsection under Story Assessment.

### Evidence Behavior

The first slice should not expose raw internal evidence references.

If a future Evidence tie-in is needed, it should point only to visible report artifacts or existing findings, not internal Story Assessment traces.

## Relationship To Issues

Issues should consume validated story-improvement signals rather than reinvent them.

### Integration Rule

When Guided Story Improvements emits a validated recommendation:

- Issues may create or enrich a normalized finding in the `storytelling`, `benchmark`, or related impact areas
- the recommendation remains authored by Guided Story Improvements
- Issues becomes a downstream operational view, not the source of truth

### Result

Users can see the story recommendation first, then inspect the issue record if they want a more standard issue-management format.

## Relationship To Fix Plan

Fix Plan should consume Guided Story Improvements as remediation input.

### Integration Rule

Each Guided Story Improvement should map to:

- a remediation family
- a likely impact statement
- a sequencing hint

Examples:

- title/question anchor -> sequence early because it clarifies the whole page
- benchmark/target -> sequence early because it changes decision interpretation
- prior-period context -> sequence after the lead metric and benchmark are clear
- scattered filters -> sequence after the main narrative path is stabilized

### Result

Fix Plan retains its existing sequencing role but becomes better grounded in validated story improvement inputs.

## Contract Boundary

The first product slice may introduce only a narrow public representation of validated Guided Story Improvements.

It must not expose:

- archetypes
- semantic coherence
- confidence breakdown
- competing stories
- promotion states
- signal registry
- surface scopes
- raw validation outputs

### Safe Public Shape

Allowed public fields should be limited to user-facing recommendation data such as:

- improvement id
- title
- summary
- rationale
- expected impact
- priority
- related issue category

This keeps the contract advisory and presentation-safe.

## Future Compatibility

Guided Story Improvements is the promotion gateway for future Story Assessment outputs.

New signals may enter only if they pass the same promotion ladder:

- internal signal
- Level 1 validated
- contract eligible
- productized through Guided Story Improvements

This keeps future growth disciplined and prevents direct exposure of raw research signals.

## Non-Goals

This design does not include:

- public Archetype exposure
- public Confidence Breakdown
- public Coherence scores
- public Competing Story outputs
- Story Assessment 3.0
- Design Studio integration
- Fabric cross-surface rollout

## Risks

- the current corpus is still small, so the first slice must remain narrow
- if wording becomes too generic, Guided Story Improvements will feel redundant with Issues
- if wording becomes too technical, it will leak research-stage internals indirectly
- if too many recommendations are shown at once, the subsection will collapse into another issue list

## Success Criteria

The feature succeeds if users can answer:

- what story improvements would help this page most

without needing to understand internal Story Assessment research signals.

## Recommendation

Ship Guided Story Improvements as a small story-advisory subsection beneath Story Assessment and above Issues.

Use only the six validated Story Gap categories.

Keep all special-page handling, signal provenance, and classification diagnostics internal.
