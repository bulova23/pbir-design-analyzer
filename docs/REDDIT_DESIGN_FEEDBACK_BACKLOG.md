# Reddit-Informed Design Feedback Backlog

Status: proposed backlog and specification for post-v1 analyzer enhancements

Last researched: May 15, 2026

## Purpose

This document converts repeated Power BI report critique patterns from `r/PowerBI` into a concrete product backlog for `PBIR Design Analyzer`.

The goal is not to mimic every subjective design opinion from Reddit. The goal is to capture the recurring, defensible review heuristics that repeatedly appear in report critique threads and turn them into implementable analyzer behavior.

## Research Basis

The backlog below is grounded in recurring feedback themes observed in these threads:

- [Updated my Power BI report after your feedback. I Would appreciate another review](https://www.reddit.com/r/PowerBI/comments/1tdjzgl/updated_my_power_bi_report_after_your_feedback_i/) on May 13, 2026
- [Feedback on My First Power BI HR Dashboard](https://www.reddit.com/r/PowerBI/comments/1sxzctx/feedback_on_my_first_power_bi_hr_dashboard/) on April 28, 2026
- [Self Project: Power BI Dashboard Feedback + Map Warning Help](https://www.reddit.com/r/PowerBI/comments/1s69ol1/self_project_power_bi_dashboard_feedback_map/) on March 28, 2026
- [Looking for feedback on my first dashboard! Built for a small manufacturing company](https://www.reddit.com/r/PowerBI/comments/1s1ie66/looking_for_feedback_on_my_first_dashboard_built/) on March 23, 2026
- [Report Design Priorities](https://www.reddit.com/r/PowerBI/comments/1rb4d59/report_design_priorities/) on February 21, 2026
- [Looking for brutally honest feedback](https://www.reddit.com/r/PowerBI/comments/1o56j12/looking_for_brutally_honest_feedback/) on October 2025
- [Dashboard feedback](https://www.reddit.com/r/PowerBI/comments/1jl1yt2/dashboard_feedback/) on April 2025
- [Feedback on my Powerbi Report](https://www.reddit.com/r/PowerBI/comments/1bnzz4z/feedback_on_my_powerbi_report/) on March 2024

## Repeated Research Signals

These themes repeated across multiple threads and should be treated as the design-review baseline:

- clarity matters more than decorative styling
- users should understand the page purpose within seconds
- alignment, spacing, and layout consistency strongly affect perceived quality
- too many visuals per page is one of the most common critique points
- chart choice is judged semantically, not just aesthetically
- KPI cards are frequently criticized when they lack comparison, trend, target, or variance context
- filters and navigation controls should feel intentional and predictable
- report pages should tell a story, not just display disconnected facts
- reviewers often distinguish executive overview pages from analyst detail pages

## Storytelling Finding

Yes. "The data should tell a story" was a repeated theme.

It showed up in multiple forms:

- direct story language: users asked "What is the story here?" and "tell a story with the data"
- time-to-understand framing: users should get the overview in seconds, then drill into explanation
- "so what?" framing: KPI values without trend, target, or comparison context were treated as incomplete
- hierarchy framing: the page should guide the eye from primary outcome to supporting evidence

This means narrative should remain an explicit analyzer concern, but it needs to be expanded beyond the current shallow headline-plus-trend-plus-comparison check.

## Current Analyzer Strengths

The current implementation already covers part of the research pattern:

- alignment and grid checks
- density and cognitive load checks
- data-ink scoring
- pie and donut penalties
- palette size checks
- basic narrative scoring
- navigation weighting for bookmark-heavy and control-heavy layouts

Relevant implementation areas:

- [service-dotnet/Services/Pbir/PbirScoringService.cs](../service-dotnet/Services/Pbir/PbirScoringService.cs)
- [vscode-extension/src/analyzer/config/store.ts](../vscode-extension/src/analyzer/config/store.ts)
- [vscode-extension/src/analyzer/score/quickFixes.ts](../vscode-extension/src/analyzer/score/quickFixes.ts)

## Current Gaps

The research also exposed several important gaps in the current analyzer:

- visual parsing is too shallow for semantic design review
- accessibility scoring is mostly theme-color-versus-white, not actual on-canvas readability
- page title governance currently treats page metadata as a page title rather than verifying visible title intent
- chart-choice scoring is still coarse and mostly type-based
- filter ergonomics, comparison context, and visible hierarchy are not first-class signals
- several governance defaults exist in config but are not meaningfully enforced

The main technical constraint is the current `VisualData` shape in `PbirScoringService`, which only captures:

- visual id
- visual type
- x and y position
- width and height
- hidden state

That is not enough for the next wave of design-review features.

## Backlog Principles

The following principles should guide all backlog items in this document:

- classify findings as `objective`, `strong heuristic`, or `style preference`
- prefer actionable, localized feedback over generic design advice
- expose affected visuals whenever possible
- support page-level findings first, then roll up to report-level insights
- keep executive, operational, and analyst use cases distinct where thresholds differ

## Proposed Feature Set

### Feature 1: Rich Visual Metadata Extraction

Priority: `P0`

Type: foundation and dependency for the rest of this backlog

#### Problem

Most Reddit critique is about visible design intent, not just bounding boxes and visual types. The current parser cannot see enough of the report surface to support those critiques reliably.

#### User Value

Without richer metadata, the analyzer cannot tell the difference between:

- a page with a visible title and a page that only has a page name in metadata
- a readable KPI card and an unreadable one
- a chart with contextual labels and a chart with weak or missing labeling
- a semantically well-formed layout and one that is only technically aligned

#### Scope

Extend visual parsing to capture, when available from PBIR:

- visible title text
- subtitle text
- text box text
- legend presence
- axis label presence
- data label presence
- measure and field role hints
- background fill color
- font color
- border visibility
- corner radius
- shadow or elevation indicators
- actual page width and height when available

#### Implementation Notes

Target areas:

- extend `ParseVisualsFromDirectory` in [PbirScoringService.cs](../service-dotnet/Services/Pbir/PbirScoringService.cs)
- replace or expand the current `VisualData` record
- add typed formatting sub-objects instead of flattening everything into booleans
- preserve tolerance for malformed or partial PBIR definitions

#### Acceptance Criteria

- the parser can detect visible title text for common chart, card, and text visuals
- the parser can distinguish visible filters from general decorative visuals
- score feedback can cite title text or formatting facts when those facts are available
- parsing failure of one formatting section does not fail the full report score

#### Out of Scope

- pixel-perfect visual rendering
- OCR from screenshots
- dynamic runtime state from the Power BI service

### Feature 2: Storytelling and Decision Context Analyzer

Priority: `P1`

Type: new scoring dimension or major expansion of `Narrative Design`

#### Problem

Reddit repeatedly critiques reports that show numbers without meaning. Reviewers consistently ask:

- what is the page trying to say
- is this good or bad
- compared to what
- what should the user do next

The current narrative implementation checks only for:

- headline KPI presence
- trend chart presence
- comparison chart presence

That is a good start, but it is too shallow for real report review.

#### User Value

This feature should help authors answer whether a page is:

- decision-led
- context-rich
- understandable in a first scan

#### Scope

Add a `Storytelling and Decision Context` analyzer with signals such as:

- page has a visible purpose statement or title, not just metadata
- KPI cards include supporting context such as:
  - prior period delta
  - target or budget reference
  - trend sparkline or time context
  - status framing like on track, at risk, below target
- page includes at least one explanatory supporting visual for each top-level KPI cluster
- report supports a quick overview path before deep detail
- page title or prominent text avoids vague naming like `Page 1`, `Overview`, or generic metric labels when more specific intent is possible

#### Example Findings

- "Revenue KPI has no target, variance, or prior-period context. Add one comparison so the value can be interpreted."
- "Page title is present in metadata but no visible title or question anchors the page."
- "This page contains detailed breakdowns but no clear primary outcome. Decide what the user should answer in the first 5-10 seconds."

#### Scoring Model Direction

Suggested sub-criteria:

- visible page purpose
- headline outcome clarity
- comparison context on KPI layer
- supporting evidence flow
- overview-to-detail readability

#### Acceptance Criteria

- the analyzer can flag KPI-heavy pages that lack comparison context
- the analyzer can distinguish visible title intent from page metadata
- score feedback includes at least one narrative-level finding when the page has data but no obvious story anchor
- the score panel can show this as a first-class framework or as an expanded narrative subsection

### Feature 3: Hierarchy, Scan Path, and Page Composition Analyzer

Priority: `P1`

Type: new scoring dimension or expansion of `Gestalt`, `Cognitive Load`, and `Density`

#### Problem

Many critique threads focus less on raw count and more on reading order:

- where does the eye go first
- are KPI cards aligned and consistently spaced
- are filters intruding into the primary scan path
- is the page too tall or too crowded

The current analyzer covers grid alignment and weighted count, but not page-level scan path quality.

#### User Value

This feature should tell authors whether the layout feels intentionally composed rather than merely snapped to a grid.

#### Scope

Add page-composition signals such as:

- top-band KPI consistency
- even spacing rhythm across peer visuals
- large dead zones versus intentional white space
- likely reading order from upper-left through primary evidence clusters
- filter placement quality
- page overflow or long-page risk based on actual canvas size and used bounds
- separation between overview visuals and detail visuals

#### Example Findings

- "Top-row cards are misaligned vertically and use inconsistent left and center alignment."
- "Primary filters appear in the lower-right area of the page, which breaks expected reading flow."
- "The page extends beyond a standard one-screen scan pattern and should likely be split."

#### Implementation Notes

This feature should prefer geometric heuristics over stylistic opinion:

- row clustering
- column clustering
- gap variance
- occupied-bounds analysis
- primary-zone versus secondary-zone placement

#### Acceptance Criteria

- the analyzer can identify inconsistent card spacing and alignment among peer visuals
- the analyzer can warn on likely long-page or scrolling layouts when page geometry supports the conclusion
- the analyzer can detect when filters appear in unusual positions relative to primary content
- feedback references the affected visuals rather than only reporting a page-level warning

### Feature 4: Chart Semantics and Comparison Quality Analyzer

Priority: `P2`

Type: expansion of `Visual Best Practices` and `Graphical Perception`

#### Problem

Reddit feedback frequently criticizes charts not just for existing, but for being the wrong chart for the job:

- line charts on categorical data
- donut charts where bars would compare better
- funnel charts with weak meaning
- KPI cards with no comparative frame
- redundant axes or labels

The current analyzer mostly recognizes:

- pie and donut charts
- trend-capable charts
- comparison-capable charts

That is not enough.

#### User Value

This feature should improve the quality of recommendations from:

- "use fewer pies"

to:

- "this encoding is weak for the comparison task the page appears to be asking the user to perform"

#### Scope

Add rules for:

- line charts used without continuous sequence context
- pie and donut chart severity scaling by count and page role
- redundant labels when both direct labels and heavy axis labeling are present
- chart role mismatch against visible title intent where parsable
- missing comparison visuals on pages with multiple KPI cards
- insufficient variance context for executive summary pages

#### Example Findings

- "Age is shown as a line chart, but the visual appears categorical rather than sequential."
- "This page asks the user to compare categories but does not include a strong bar or column comparison visual."
- "Donut charts are present on an overview page where exact comparison appears to be more important than part-to-whole emphasis."

#### Acceptance Criteria

- the analyzer can issue at least three distinct semantic chart-choice findings beyond pie detection
- these findings are backed by explicit evidence in the parsed visual metadata
- `Graphical Perception` and `Visual Best Practices` feedback become more precise than type counting

### Feature 5: Filter Ergonomics and Visual Consistency Analyzer

Priority: `P2`

Type: new scoring dimension or governance-plus-heuristics package

#### Problem

Another repeated critique pattern is that reports feel inconsistent even when they are technically functional:

- filters are scattered
- slicers are too visually loud
- card labels are inconsistent
- corners and shadows vary too much
- pages use different visual language without reason

The current product has some related governance defaults, but they are not yet strong enough and some are not implemented.

#### User Value

This feature should help authors avoid the "messy but works" class of report that reviewers consistently call out.

#### Scope

Add checks for:

- filters concentrated at left or top versus scattered placement
- slicer count and slicer density on overview pages
- inconsistent page-level use of corner radius, shadows, and background fills
- inconsistent metric label patterns such as mixing `YTD Sales`, `Sales YTD`, and generic `Sum of ...`
- repeated page layouts that shift title or filter conventions without clear purpose
- padding and spacing consistency within card bands and grouped visual clusters

#### Example Findings

- "Filters are distributed across multiple page zones. Consolidate them into a single filter band."
- "Card labels use inconsistent naming patterns. Standardize modifier placement such as `YTD`."
- "Rounded corners and elevation treatments vary across peer visuals, making the page feel less cohesive."

#### Acceptance Criteria

- the analyzer can identify scattered filter placement patterns
- the analyzer can flag obvious metric-label inconsistency when visible title text is available
- the analyzer can produce at least one consistency finding on intentionally inconsistent test fixtures

## Cross-Cutting Product Changes

These are not standalone backlog items, but they should accompany the feature work above.

### Finding Type Classification

Every new finding should declare one of:

- `objective`
- `strongHeuristic`
- `stylePreference`

The score panel should visually distinguish these types so the analyzer does not overstate subjective guidance.

### Audience Presets

Add optional analyzer presets for:

- executive
- operational
- analyst

These presets should influence thresholds for:

- visual density
- required comparison context
- acceptable control count
- level of tolerated detail on a page

### Better Quick Fix Surface

The current quick-fix surface is too narrow. It should eventually support suggested operations such as:

- consolidate filters to top or left
- normalize card alignment
- reduce visual count by identifying low-signal visuals
- replace donut charts with comparison charts
- standardize label naming patterns

## Recommended Delivery Order

### Phase 1

- Feature 1: Rich Visual Metadata Extraction
- Feature 2: Storytelling and Decision Context Analyzer

### Phase 2

- Feature 3: Hierarchy, Scan Path, and Page Composition Analyzer
- Feature 4: Chart Semantics and Comparison Quality Analyzer

### Phase 3

- Feature 5: Filter Ergonomics and Visual Consistency Analyzer
- cross-cutting audience presets and finding classification

## Testing Expectations

Each feature in this backlog should ship with:

- unit tests for parsing and scoring logic
- at least one PBIR fixture that intentionally violates the new rules
- at least one PBIR fixture that demonstrates compliant behavior
- score-panel verification that findings remain readable and actionable

For parser-dependent features, malformed or partial PBIR structures must degrade gracefully and must not crash report scoring.

## Non-Goals

This backlog does not attempt to:

- replace human report review
- enforce one visual design style
- claim certainty where only a heuristic is available
- infer business meaning from data values alone without supporting metadata

The product should remain a rigorous assistant, not an aesthetic dictator.
