# PBIR Design Analyzer Concrete Implementation Backlog

Status: recommended implementation backlog

Date: 2026-05-26

Primary inputs:

- [Reddit comment review research](./2026-05-26_reddit_comment_review_research.md)
- Current scorer implementation in [PbirScoringService.cs](../service-dotnet/Services/Pbir/PbirScoringService.cs)
- Screenshot audit scaffolding in [PbirScorePanel.ts](../vscode-extension/src/views/PbirScorePanel.ts)
- Chart selection reference image: [Chart_selector.png](./Chart_selector.png)

## Purpose

Turn the current strategy work into an execution-oriented backlog that reflects:

- what the product already does
- what gaps remain
- what should be built first
- what depends on other work

This backlog is intentionally biased toward features that:

- directly match repeated reviewer behavior from the Reddit research
- can be implemented with high confidence from PBIR/TMDL metadata
- create product differentiation beyond generic linting

## Current Baseline

Already present in meaningful form:

- visible title and text extraction
- legend, axis label, and data label parsing
- field-role hint parsing
- formatting metadata extraction:
  - font color
  - background fill
  - borders
  - corner radius
  - shadow
- narrative scoring
- bookmark-aware scoring
- filter placement and density heuristics
- long-page and overview/detail density heuristics
- raw KPI label cleanup checks
- partial chart-fit checks:
  - pie / donut
  - categorical line misuse
  - weak funnel semantics
- screenshot upload and AI screenshot-audit scaffolding

Because this baseline already exists, the next wave should focus on semantic depth and UX workflow rather than rebuilding fundamentals.

## Prioritization Rules

Features are ranked by:

1. user impact against repeated reviewer complaints
2. implementation leverage on top of existing code
3. determinism and trustworthiness
4. differentiation value
5. dependency order

## Priority List

### P0. Semantic Color Consistency Engine

Why first:

- Semantic color inconsistency is one of the most repeated reviewer complaints.
- The current parser already extracts theme colors and per-visual colors.
- This is high-confidence, deterministic, and enterprise-friendly.

Outcome:

- detect when the same field or semantic concept uses different colors across visuals or pages
- detect when the same color is reused for conflicting meanings

Scope:

- build per-page and cross-page category-to-color maps
- detect severity/status color inconsistency
- detect repeated dimension color inconsistency:
  - region
  - segment
  - product category

Detection mode:

- deterministic first
- screenshot audit optional for rendered verification

Acceptance criteria:

- analyzer flags cross-visual semantic color drift for repeated fields
- analyzer flags contradictory red/green semantics on the same page
- score panel can show a human-readable semantic color map

Dependencies:

- existing formatting metadata only

Suggested implementation areas:

- `PbirScoringService.cs`
- `VisualMetadataSummary.cs`
- score panel page detail UI

### P1. Chart Intent and Chart-Fit Analyzer

Why second:

- The chart selector image makes the gap obvious.
- Current chart-fit logic is still strongest on pie avoidance and a few misuse cases.
- Reddit reviewers judge chart choice semantically, not cosmetically.

Outcome:

- infer chart intent class:
  - comparison
  - relationship
  - distribution
  - composition
  - trend over time
- detect whether the chosen chart fits the inferred task

Scope:

- classify each visual by intended analytical task using:
  - visual type
  - field roles
  - title text
  - page purpose hints
- detect likely misfits:
  - categorical line charts
  - overused donut/pie
  - composition shown with weak comparison encoding
  - trend shown without temporal evidence
  - relationship pages missing scatter-type support
  - distribution tasks missing histogram / density alternatives where relevant

Acceptance criteria:

- every data visual gets an inferred intent tag
- analyzer can explain why a chart may not fit the task
- score report can recommend alternative chart families

Dependencies:

- current field-role metadata
- visible text metadata

Suggested implementation areas:

- `graphicalPerception`
- `visualBestPractices`
- new chart-intent helper module

### P2. Cross-Page Consistency Analyzer

Why third:

- Repeated reviewer complaints include stable filter placement, consistent page conventions, and repeated semantics.
- The code already has some consistency checks; this should unify and extend them into a first-class feature.

Outcome:

- score report-level consistency, not just page-level quality

Scope:

- title alignment consistency
- filter band consistency
- semantic color consistency
- KPI label convention consistency
- page-style language consistency
- page archetype consistency

Acceptance criteria:

- report-level consistency section exists in the score panel
- findings can cite affected pages and visuals
- users can distinguish page-local problems from report-system problems

Dependencies:

- existing consistency heuristics
- semantic color engine

### P3. Inferred Page Story and Intent Confirmation

Why fourth:

- This directly answers the product opportunity you identified.
- It turns metadata into a user-facing hypothesis instead of pretending to know intent with certainty.
- It is a better workflow than pure autonomous scoring.

Outcome:

- analyzer proposes the story the page appears to be telling
- user confirms whether that matches actual intent
- analyzer can detect story mismatch

Output shape:

- `Inferred story`
- `Story archetype`
- `Confidence`
- `Why this was inferred`
- `Does this match your intent?`

Example:

- `Inferred story: Revenue performance over time, with regional comparison as supporting evidence`
- `Story archetype: Executive overview + trend + comparison`
- `Confidence: High`

Detection inputs:

- page title
- prominent KPI cards
- lead visual types
- field wells / role hints
- layout prominence
- supporting visual mix

Acceptance criteria:

- every scored page can emit an inferred story hypothesis when evidence is sufficient
- confidence is explicit
- the report UI can store or surface user confirmation state
- analyzer can show mismatch insight when intent and inferred story differ

Dependencies:

- chart-intent analyzer
- narrative scoring primitives
- likely UI state persistence work

Suggested implementation phases:

1. deterministic story archetype inference
2. user confirmation UI
3. mismatch analysis
4. optional AI refinement

### P4. Actionability and Decision-Support Scoring

Status: implemented on `2026-05-30`

Why fifth:

- The strongest Reddit criticism is still “what decision does this support?”
- Current narrative scoring is good but still not explicit enough about decision support quality.

Outcome:

- score whether a page supports action, not just whether it has structure

Scope:

- target / benchmark presence
- exception visibility
- urgency signaling
- prior-period context
- drill path or supporting evidence path

Acceptance criteria:

- actionability appears as a named subscore or narrative subsection
- KPI pages without context receive specific actionability findings
- executive overview pages receive stronger decision-support expectations than analyst pages

Implementation notes:

- Added deterministic `ActionabilityBreakdown` output with score, strengths, and gaps for:
  - target / benchmark presence
  - exception visibility
  - urgency signaling
  - prior-period context
  - drill / supporting-evidence path
- Surfaced the actionability score and narrative gaps directly in the score panel.

Dependencies:

- page-intent profiles
- story inference

### P5. Page-Intent Profiles

Status: implemented on `2026-05-30`

Why sixth:

- Many scoring disagreements disappear if the analyzer knows whether a page is executive, operational, analytical, or appendix-style.

Outcome:

- different scoring expectations by page type

Initial profiles:

- executive overview
- operational monitoring
- analytical deep-dive
- detail appendix

Acceptance criteria:

- page intent can be inferred or manually overridden
- framework weighting or thresholds can vary by page intent

Implementation notes:

- Added deterministic `PageIntentProfile` output normalized to:
  - `executive`
  - `operational`
  - `analytical`
  - `appendix`
- Score panel now shows inferred and manually selected profile states.
- Manual override is currently a score-panel-local review control; backend scoring remains deterministic on inferred intent until persisted page-level override config is introduced.

Dependencies:

- story inference

### P6. Reviewer Workflow and Comment Generation

Status: implemented on `2026-05-30`

Why seventh:

- The product should not stop at findings; it should help users sound like expert reviewers.

Outcome:

- generated review comments
- review personas
- consultant-style export

Scope:

- tone presets:
  - coach
  - consultant
  - executive reviewer
  - strict design critic
- reviewer persona overlays
- exportable review summary

Acceptance criteria:

- users can generate human-readable review comments from findings
- comments clearly separate objective findings from heuristics and style preferences

Implementation notes:

- Left the consultant-style export flow intact.
- Added a deterministic reviewer comment generator panel with persona overlays for:
  - coach
  - consultant
  - executive reviewer
  - strict design critic
- Comment generation is grounded in current page findings, actionability gaps, and benchmark insight rather than generic prompts.

Dependencies:

- finding classification already exists
- stronger story and actionability model improves quality

### P7. Screenshot Audit Grounding Upgrade

Status: implemented on `2026-05-30`

Why eighth:

- Screenshot audit already exists in scaffold form, but the prompt grounding is still thin.

Outcome:

- screenshot review becomes meaningfully tied to parsed page metadata and score evidence

Scope:

- pass richer page metadata into audit providers
- include chart-intent and inferred-story context in prompts
- distinguish rendered issues from metadata issues

Acceptance criteria:

- screenshot findings can reference page-story context
- screenshot audit and deterministic findings can be shown side-by-side without duplication

Implementation notes:

- Visual audit providers now receive richer grounding context including:
  - page intent profile
  - inferred story
  - actionability summary
  - benchmark/archetype context
  - page chart-intent metadata
- Screenshot findings now distinguish:
  - `renderedLayout`
  - `metadataModel`
- The score panel surfaces that distinction in the page-level audit output.

Dependencies:

- story inference
- chart-intent analyzer

### P8. Quick-Fix Expansion

Status: implemented on `2026-05-30`

Why ninth:

- The current quick-fix list is helpful but narrow.

Outcome:

- richer advisory fixes tied to high-value findings

Scope:

- semantic color normalization
- chart replacement suggestions
- KPI context fixes
- title rewrite suggestions
- overview/detail separation suggestions

Acceptance criteria:

- new fix types are generated for top high-frequency findings
- fixes can reference affected visuals and pages

Implementation notes:

- Expanded score-panel quick fixes for:
  - semantic color normalization
  - chart replacement suggestions
  - KPI context fixes
  - title rewrite suggestions
  - overview/detail separation suggestions
- New fixes are derived from specific findings or actionability gaps rather than vague generic advice.

Dependencies:

- all higher-priority semantic analyzers

### P9. Benchmark and Archetype Comparison

Status: implemented on `2026-05-30`

Why tenth:

- Strong differentiation, but less urgent than core semantic review quality.

Outcome:

- compare reports to executive-ready and other archetypes

Scope:

- archetype matching
- benchmark messaging
- “beautiful but useless” insight

Implementation notes:

- Added deterministic archetype matching and benchmark messaging to the scoring result.
- Added explicit “beautiful but useless” detection when page polish reads stronger than decision support.
- Surfaced comparative insight in the score panel alongside actionability and reviewer comments.

Dependencies:

- page-intent profiles
- story inference
- actionability scoring

## Phase Plan

### Phase 1: Deterministic Semantic Upgrade

Deliver:

- semantic color consistency
- chart intent and chart-fit analyzer
- expanded cross-page consistency section

Target result:

- stronger deterministic coverage of the most repeated Reddit complaints

### Phase 2: Story and Intent Layer

Deliver:

- inferred page story
- story archetype
- confidence scoring
- user intent confirmation workflow
- story mismatch insight

Target result:

- analyzer becomes a collaboration tool, not just a lint engine

### Phase 3: Decision Support Layer

Deliver:

- actionability scoring
- page-intent profiles
- stronger executive readiness output

Target result:

- analyzer can say whether the page is useful, not just tidy

### Phase 4: Reviewer Workflow Layer

Deliver:

- persona-based comments
- exportable review packet
- richer quick fixes
- screenshot audit grounding upgrade

Target result:

- product supports consultants, reviewers, and enterprise governance workflows

## Data Model and API Backlog

Recommended model additions:

- `InferredStorySummary`
- `StoryArchetype`
- `StoryConfidence`
- `IntentMatchState`
- `SemanticColorMap`
- `ChartIntentClassification`
- `PageIntentProfile`
- `ActionabilityBreakdown`

Recommended API evolution:

- extend score payloads with inferred story and intent profile
- persist optional user confirmation state in extension storage first
- keep AI enrichment additive and non-blocking

## UI Backlog

Recommended score panel additions:

- report-level consistency section
- per-page inferred story card
- semantic color map card
- chart-fit card with alternative chart suggestions
- actionability card
- reviewer comment generator panel

## Out of Scope for Near Term

- fully automatic DAX semantic understanding of business meaning
- pixel-perfect screenshot diffing
- direct Power BI mutation or auto-refactoring
- complete mobile layout simulation

## Recommended Start Order

1. semantic color consistency
2. chart intent and chart-fit analyzer
3. cross-page consistency analyzer
4. inferred page story and intent confirmation
5. actionability scoring

## Definition of Success

The next feature wave is successful if the analyzer can reliably answer questions like:

- “What story does this page appear to tell?”
- “Does the visual language support that story?”
- “Do the same semantics stay consistent across the report?”
- “Would an executive know what to do next?”
- “If this is not the story the author intended, where is the mismatch?”
