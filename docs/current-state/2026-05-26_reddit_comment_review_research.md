# Reddit Comment Review Research for PBIR Design Analyzer

Status: research synthesis and product strategy artifact

Date: 2026-05-26

## Goal

Reverse-engineer how experienced Power BI reviewers critique dashboards in comment threads and convert those behaviors into implementable PBIR Design Analyzer capabilities.

This is intentionally not a Reddit summary. It is a design-intelligence report for building an AI-assisted dashboard review system.

## Research Scope

Primary focus: critique-request threads and comment sections from `r/PowerBIdashboards`.

Supplementary calibration set: closely related critique threads in `r/PowerBI` where the feedback style, reviewer expectations, and dashboard-review language were materially similar. These were used to strengthen frequency estimates where `r/PowerBIdashboards` volume was thin.

## Source Threads Reviewed

Primary `r/PowerBIdashboards` threads:

- [Feedback for the Dashboard that I made](https://www.reddit.com/r/PowerBIdashboards/comments/1qcbwg2/feedback_for_the_dashboard_that_i_made/)
- [Need Honest Feedback on my work](https://www.reddit.com/r/PowerBIdashboards/comments/1pwhofv/need_honest_feedback_on_my_work/)
- [Feedback Request: Global Health Analysis Dashboard (Power BI)](https://www.reddit.com/r/PowerBIdashboards/comments/1q9l86q/feedback_request_global_health_analysis_dashboard/)

Supplementary `r/PowerBI` critique threads:

- [Looking for brutally honest feedback](https://www.reddit.com/r/PowerBI/comments/1o56j12/looking_for_brutally_honest_feedback/)
- [Feedback on my dashboard](https://www.reddit.com/r/PowerBI/comments/1siymb0/feedback_on_my_dashboard/)
- [Feedback on my dashboard](https://www.reddit.com/r/PowerBI/comments/1sgy80r/feedback_on_my_dashboard/)
- [Feedback on my dashboard](https://www.reddit.com/r/PowerBI/comments/1s7xovz/feedback_on_my_dashboard/)
- [Self Project: Power BI Dashboard Feedback + Map Warning Help](https://www.reddit.com/r/PowerBI/comments/1s69ol1/self_project_power_bi_dashboard_feedback_map/)
- [First Power BI Dashboard – Would Love Honest Feedback!](https://www.reddit.com/r/PowerBI/comments/1lzx88p/first_power_bi_dashboard_would_love_honest/)
- [My end-to-end Executive Dashboard in Power BI. Looking for feedback!](https://www.reddit.com/r/PowerBI/comments/1o5qiyw/my_endtoend_executive_dashboard_in_power_bi/)

## Method

1. Read the comment sections, not just the original posts.
2. Extract recurring critique patterns, including blunt or informal phrasing.
3. Normalize the critiques into underlying design principles.
4. Classify each pattern by detectability:
   - PBIR/TMDL metadata
   - layout geometry
   - bookmarks and states
   - DAX/model metadata
   - screenshot/image analysis
   - theme JSON and color semantics
   - cross-visual semantic relationships
5. Convert each pattern into product opportunities:
   - scoring rules
   - warnings
   - recommendations
   - AI insights
   - benchmark or pattern checks
6. Separate high-confidence deterministic rules from AI-assisted interpretation.

## Representative Comment Signals

These comments are representative because they show repeated reviewer instincts, not because each exact phrasing matters.

> "Reduce the noise, simplify the output, and target your audience."  
Source: [Need Honest Feedback on my work](https://www.reddit.com/r/PowerBIdashboards/comments/1pwhofv/need_honest_feedback_on_my_work/)

> "What value does your customer get out of seeing this?"  
Source: [Feedback on my dashboard](https://www.reddit.com/r/PowerBI/comments/1siymb0/feedback_on_my_dashboard/)

> "Where is the symmetry?"  
Source: [Looking for brutally honest feedback](https://www.reddit.com/r/PowerBI/comments/1o56j12/looking_for_brutally_honest_feedback/)

> "It is hard to tell the urgency."  
Source: [Feedback Request: Global Health Analysis Dashboard (Power BI)](https://www.reddit.com/r/PowerBIdashboards/comments/1q9l86q/feedback_request_global_health_analysis_dashboard/)

> "The color contrast seems harsh on the eyes."  
Source: [Feedback on my dashboard](https://www.reddit.com/r/PowerBI/comments/1s7xovz/feedback_on_my_dashboard/)

> "This isn't risk, this is just defect history."  
Source: [Feedback on my dashboard](https://www.reddit.com/r/PowerBI/comments/1sgy80r/feedback_on_my_dashboard/)

## Top Meta-Findings

1. Reviewers punish dashboards that are decorative but non-decisional more than dashboards that are plain but clear.
2. Color errors are the fastest trigger for negative sentiment, but they are rarely the deepest complaint.
3. The most repeated serious critique is not "ugly design." It is "what is the story and what decision does this support?"
4. Reviewers expect semantic consistency:
   - same colors should mean the same thing
   - same severity scale should mean the same thing
   - same metric family should retain naming and context across visuals
5. Many strong critiques combine layout and semantics:
   - clutter is bad because it slows comprehension
   - missing whitespace is bad because it obscures hierarchy
   - raw labels are bad because they signal a report that was not translated for business users
6. Commenters frequently separate executive overview expectations from analyst detail expectations.
7. Reviewers disagree on one-page versus multi-page design, but not on the need for an obvious overview-to-detail structure.

## Frequency Scale

- `Very High`: appears in most threads
- `High`: appears repeatedly across several threads
- `Medium`: appears in multiple threads but not dominant
- `Low`: appears occasionally or indirectly

## Category Findings

### Layout & Composition

Recurring feedback patterns:

- Misalignment, inconsistent spacing, and weak symmetry create an immediate "unprofessional" reaction.
- Visuals are often described as too crowded even when the actual visual count is moderate.
- Tables and matrices are frequent clutter magnets, especially when they force scrolling.
- Reviewers like grouped, grid-aligned sections and predictable filter placement.

Why users react:

- Humans assess polish before content comprehension.
- Alignment and spacing act as proxies for trust and craftsmanship.
- Crowded layouts raise cognitive switching cost and make prioritization unclear.

Underlying principles:

- Gestalt
- visual hierarchy
- alignment and balance
- cognitive load
- whitespace usage

Detectability:

- `Deterministic from layout`: yes
- `PBIR metadata`: partial
- `Screenshot analysis`: useful for validation

Recommended analyzer capabilities:

- Scoring rule: measure grid alignment variance across x/y edges.
- Scoring rule: detect inconsistent gutters, irregular section spacing, and crowding clusters.
- Warning: flag large matrix/table visuals on overview pages when they occupy premium layout zones.
- Layout heuristic: detect whether filters are placed in a consistent band at top or left.
- AI insight: summarize whether the page has a clear first-scan focal area.

Specific scoring heuristics:

- Penalize pages where edge alignment misses exceed configurable tolerance on more than 30% of visible visuals.
- Penalize page sections with density above a threshold when whitespace between neighboring visuals drops below a minimum gutter.
- Penalize mixed border radius or border styling within the same visual tier unless explicitly allowed by org theme.
- Reward structured top-band KPI zones and left-to-right or top-to-bottom grouping consistency.

Implementation approach:

- Extend current visual metadata with border visibility, corner radius, title band presence, and section grouping hints.
- Build a `composition analyzer` that clusters visuals by rows, columns, and reading zones.
- Add screenshot-based overlay mode that highlights misaligned edges and crowding hot spots.

Extension UX ideas:

- "Layout Heatmap" overlay for spacing and collision pressure.
- "Reading Order Preview" that numbers likely scan order.
- "Overview Zone" badge for top-band or top-left primary-summary areas.

Framework mapping:

- Gestalt
- Cognitive Load Theory
- Stephen Few
- Executive Dashboard principles

Frequency: `Very High`

### Storytelling & Business Context

Recurring feedback patterns:

- Reviewers repeatedly ask what the dashboard is trying to say.
- Metrics without decision context are treated as incomplete.
- Pages are criticized when they show historical facts without business meaning or action cues.
- Reviewers want overview-first, then explanation, then detail.

Why users react:

- Business dashboards are judged as decision tools, not gallery pieces.
- Users resent spending effort deriving the "so what" themselves.
- Narrative flow reduces ambiguity and accelerates executive comprehension.

Underlying principles:

- storytelling
- actionability
- executive readability
- KPI clarity
- narrative analytics

Detectability:

- `PBIR metadata`: partial
- `DAX/model metadata`: partial
- `Semantic relationships`: strong
- `Screenshot analysis`: useful
- `Bookmarks/states`: useful for overview/detail flow
- `AI-assisted`: required for strongest results

Recommended analyzer capabilities:

- Storytelling metric: detect whether the page has a visible purpose, a primary outcome, and supporting evidence.
- Recommendation: ask "good or bad compared to what?" for KPI cards missing variance, target, or prior-period context.
- AI insight: infer likely business question from titles, measures, and visual mix; flag mismatch between that question and displayed evidence.
- Design pattern check: identify overview pages that contain too much detailed evidence before summary framing.

Specific scoring heuristics:

- Penalize KPI cards with no target, delta, trend, or benchmark companion.
- Penalize pages with many breakdown visuals but no dominant summary cluster.
- Reward visible titles/subtitles that express question or intent instead of raw topic labels.
- Reward presence of variance measures, prior period measures, or benchmark semantics in DAX/model metadata.

Implementation approach:

- Extract visible titles, subtitles, card labels, dynamic text, and measure names.
- Build a semantic graph linking cards to supporting visuals by shared measures and dimensions.
- Use AI to assess whether evidence supports the apparent page purpose.

Extension UX ideas:

- "What question does this page answer?" AI prompt in the results pane.
- Narrative score with subcomponents:
  - purpose clarity
  - context completeness
  - overview-to-detail flow
  - actionability

Framework mapping:

- Storytelling/Narrative Analytics
- Executive Dashboard principles
- Stephen Few
- Cognitive Load Theory

Frequency: `Very High`

### KPI Effectiveness

Recurring feedback patterns:

- Raw KPI cards without context are criticized as weak.
- Labels like `Sum of`, `Count of`, or technical field names trigger immediate negative reactions.
- Reviewers want targets, variance, status, timeframe, and urgency cues.

Why users react:

- KPI cards consume premium real estate and therefore must carry meaning.
- Technical labels signal "unfinished internal draft" rather than stakeholder-ready reporting.

Underlying principles:

- KPI clarity
- executive readability
- actionability
- redundancy reduction

Detectability:

- `PBIR metadata`: strong
- `DAX/model metadata`: strong
- `Screenshot analysis`: moderate
- `AI-assisted`: moderate

Recommended analyzer capabilities:

- Governance rule: forbid raw aggregation prefixes in visible KPI or chart titles.
- Warning: KPI card lacks explicit period context.
- Recommendation: KPI cluster lacks target, prior period, or threshold context.
- Benchmark comparison: compare page KPI composition to known executive dashboard archetypes.

Specific scoring heuristics:

- Penalize visible text matching patterns like `Sum of`, `Count of`, `Average of`, `Item_ID`.
- Penalize KPI clusters where fewer than half of cards have context visuals or variance indicators.
- Reward KPI sets with explicit date reference, status directionality, and business wording.

Implementation approach:

- Parse visual titles and card labels.
- Parse measure names and formatting.
- Infer whether adjacent cards or microcharts provide context.

Extension UX ideas:

- One-click quick fix suggestions:
  - rename visible titles
  - add timeframe text recommendations
  - suggest KPI context pairings

Framework mapping:

- Executive Dashboard principles
- Stephen Few
- Information Density

Frequency: `High`

### Color & Theme Usage

Recurring feedback patterns:

- Harsh, clashing, or overly saturated palettes trigger immediate negative sentiment.
- Semantic color misuse is more serious than mere aesthetic ugliness.
- Reviewers object when the same color means different things across visuals.
- Monochrome themes are criticized when they flatten urgency or priority.

Why users react:

- Color is one of the fastest pre-attentive cues.
- Semantic inconsistency forces users to re-learn the page visual by visual.
- Overly themed dashboards often look "beautiful but useless."

Underlying principles:

- color semantics
- visual hierarchy
- accessibility
- executive readability

Detectability:

- `Theme JSON`: strong
- `PBIR visual formatting`: strong
- `Screenshot analysis`: strong
- `Semantic relationships`: strong
- `AI-assisted`: useful for tone and urgency

Recommended analyzer capabilities:

- Accessibility check: contrast and legibility.
- Design pattern check: consistent semantic mapping for status colors.
- Warning: same categorical field uses different colors across visuals.
- Warning: visually urgent measures use soft neutral colors with no supporting emphasis.
- Recommendation: palette exceeds semantic budget.

Specific scoring heuristics:

- Penalize pages where semantic fields like severity/status/segment map to different colors across visuals.
- Penalize text/background pairs below WCAG thresholds.
- Penalize pages with too many saturated accent colors unless organization overrides exist.
- Reward separate hues for different metric families when comparison clarity benefits.

Implementation approach:

- Parse theme JSON and per-visual overrides.
- Build category-to-color mappings per field and compare across visuals/pages.
- Add screenshot checks for local contrast issues not visible in theme defaults.

Extension UX ideas:

- "Semantic Color Map" table showing each field, each value, and color consistency by page.
- Theme linting against org color policy.
- Suggested palette normalization preview.

Framework mapping:

- WCAG
- Stephen Few
- Executive Dashboard principles

Frequency: `Very High`

### Chart Selection Problems

Recurring feedback patterns:

- Reviewers question why a visual type was chosen, not just whether it looks good.
- Line vs bar inconsistency is noticed when the semantic role is similar.
- Pie and donut visuals are tolerated but frequently viewed as second-choice.
- Redundant mirrored lines or unnecessary chart duplication are criticized.

Why users react:

- Chart type signals analytical intent.
- Inconsistent visual grammar undermines trust and comparability.
- Reviewers want the simplest chart that answers the question.

Underlying principles:

- chart selection
- redundancy
- information density
- storytelling

Detectability:

- `PBIR metadata`: strong
- `Semantic relationships`: strong
- `DAX/model metadata`: moderate
- `Screenshot analysis`: moderate
- `AI-assisted`: useful for semantic fitness

Recommended analyzer capabilities:

- Deterministic warning: inconsistent chart type for same analytical role on same page.
- Recommendation: line chart used without clear time-series context.
- Warning: donut/pie used where rank or comparison chart would be clearer.
- AI insight: likely redundant visual pair showing the same story twice.

Specific scoring heuristics:

- Penalize line charts whose x-axis is not temporal or ordinal in a trend sense.
- Penalize multiple visuals on the same page using the same measure and same grouping with different forms and little added value.
- Reward chart-type consistency for repeated analytical patterns.

Implementation approach:

- Parse visual type plus bound field roles.
- Infer visual purpose: trend, composition, rank, relationship, detail.
- Compare purpose-to-visual-type fit with deterministic rules and AI fallbacks.

Extension UX ideas:

- "Why this chart?" advisory text per visual.
- Alternate chart suggestions with rationale.

Framework mapping:

- Tufte
- Stephen Few
- Data-Ink Ratio

Frequency: `High`

### Accessibility Problems

Recurring feedback patterns:

- Low-contrast text and dark-on-dark combinations draw fast criticism.
- Legibility is treated as a baseline obligation, not an advanced nice-to-have.
- Reviewers also implicitly care about font size and easy differentiation of lines.

Why users react:

- Accessibility failures block comprehension rather than merely lowering polish.
- In dashboard settings, hard-to-read labels are often interpreted as careless design.

Underlying principles:

- accessibility
- legibility
- inclusive design

Detectability:

- `Theme JSON`: moderate
- `PBIR formatting`: strong
- `Screenshot analysis`: strong
- `AI-assisted`: low need

Recommended analyzer capabilities:

- Deterministic WCAG contrast checks for text/background pairs when extractable.
- Warning: lines or categories too similar in hue/value.
- Warning: small text in dense regions.

Specific scoring heuristics:

- Penalize text below contrast thresholds.
- Penalize same-family series colors with insufficient luminance separation.
- Penalize tiny title font sizes on overview pages.

Implementation approach:

- Extract font color, background fill, line colors, and font size where available.
- Screenshot fallback to estimate local contrast and visual distinction.

Extension UX ideas:

- Accessibility badge per page.
- Preview of simulated contrast issues.

Framework mapping:

- WCAG
- accessibility heuristics

Frequency: `High`

### Executive Dashboard Expectations

Recurring feedback patterns:

- Reviewers expect executive pages to lead with summary, exceptions, and decision cues.
- Large detail tables on first pages are often challenged.
- Users care about date range disclosure, performance versus target, and outlier detection.

Why users react:

- Executive audiences optimize for time-to-answer, not exploration depth.
- Detail-first pages feel like analyst workbench screens rather than executive dashboards.

Underlying principles:

- executive dashboard principles
- actionability
- KPI clarity
- hierarchy

Detectability:

- `PBIR metadata`: strong
- `Semantic relationships`: strong
- `DAX/model metadata`: moderate
- `Bookmarks/states`: moderate
- `AI-assisted`: useful

Recommended analyzer capabilities:

- Design archetype check: executive overview vs analyst detail vs operational monitoring.
- Warning: overview page dominated by matrix/detail visual.
- Recommendation: add date range and target/benchmark context.
- Benchmark comparison: compare page structure to top-performing executive-ready archetypes.

Specific scoring heuristics:

- Penalize overview pages where detail visuals consume more than a configurable percentage of premium zone area.
- Reward top-level exception indicators, variance charts, and drill paths to details.

Implementation approach:

- Let users tag page intent or infer from names/titles.
- Score pages differently by intent profile.

Extension UX ideas:

- Page-type selector with tailored scoring weights.
- "Executive readiness" score and narrative summary.

Framework mapping:

- Executive Dashboard principles
- Stephen Few
- Storytelling/Narrative Analytics

Frequency: `High`

### Interaction & Navigation

Recurring feedback patterns:

- Reviewers want filters in predictable places.
- Slicer styling often gets criticized when it dominates content.
- Some commenters explicitly recommend show/hide slicer panels using bookmarks.
- Reviewers value overview-first with deeper analysis on separate pages or drillthrough.

Why users react:

- Navigation friction feels like cognitive waste.
- Visual controls should support analysis, not become the main event.

Underlying principles:

- cognitive load
- interaction design
- progressive disclosure

Detectability:

- `PBIR metadata`: strong
- `Bookmarks/states`: strong
- `Visual interactions/navigation`: strong
- `Screenshot analysis`: moderate

Recommended analyzer capabilities:

- Warning: slicer/control area consumes excessive space relative to business content.
- Bookmark-state-specific analysis for hidden filter panes and alternate layouts.
- Recommendation: when many slicers are visible, suggest collapsible filter menu pattern.

Specific scoring heuristics:

- Penalize pages where slicers occupy large prime zones without clear necessity.
- Reward consistent slicer placement across pages.
- Reward bookmark-driven progressive disclosure that preserves clean defaults.

Implementation approach:

- Parse bookmark definitions and hidden-state variants.
- Track visible versus hidden controls by state.
- Add cross-page navigation consistency checks.

Extension UX ideas:

- "Navigation Consistency" report section.
- Bookmark audit view with before/after layout comparison.

Framework mapping:

- Cognitive Load Theory
- Executive Dashboard principles
- Enterprise Governance

Frequency: `Medium`

### Visual Noise / Cognitive Load

Recurring feedback patterns:

- "Busy," "crowded," and "too many visuals" appear constantly.
- Reviewers dislike decorative choices that add no interpretive value.
- Overrounded cards, heavy backgrounds, harsh shadows, and loud slicers are common triggers.

Why users react:

- Non-informative surface area competes with data.
- Decorative density often masks missing analytical depth.

Underlying principles:

- cognitive load
- data-ink ratio
- redundancy
- information density

Detectability:

- `PBIR metadata`: moderate
- `Layout geometry`: strong
- `Screenshot analysis`: strong
- `AI-assisted`: moderate

Recommended analyzer capabilities:

- Density score: existing concept should expand to include decorative overhead.
- Warning: excessive chrome relative to data area.
- Screenshot insight: visually noisy background or control treatment.

Specific scoring heuristics:

- Estimate data-to-chrome ratio using borders, shadows, filled containers, and control density.
- Penalize repeated visuals showing the same measure without new insight.

Implementation approach:

- Expand current data-ink and density models with richer formatting metadata.
- Add screenshot-based polish pass for background and decorative overload.

Extension UX ideas:

- "Noise Budget" meter.
- Quick fix recommendations for background simplification and slicer collapse.

Framework mapping:

- Tufte
- Data-Ink Ratio
- Cognitive Load Theory

Frequency: `Very High`

### Mobile / Responsive Concerns

Recurring feedback patterns:

- Explicit mobile feedback was limited, but several critiques imply poor small-screen survivability:
  - crowded layout
  - wide matrices
  - too many controls
  - dense labels

Why users react:

- Layouts that barely survive desktop scan will fail on smaller frames.

Underlying principles:

- responsive design
- information density
- executive readability

Detectability:

- `Layout geometry`: strong
- `Screenshot analysis`: moderate
- `Bookmarks/states`: moderate

Recommended analyzer capabilities:

- Recommendation: mobile risk rating based on density, table width, and control sprawl.
- Design pattern check: overview-first mobile-safe composition.

Frequency: `Low to Medium`

### Beginner Mistakes

Recurring feedback patterns:

- Raw default visual titles
- unrefined color palettes
- no visible page title
- weak alignment
- undefined business goal
- technical labels instead of stakeholder language

Why users react:

- These errors instantly identify the report as early-stage practice work.

Underlying principles:

- business translation
- KPI clarity
- hierarchy
- basic accessibility

Detectability:

- mostly deterministic

Recommended analyzer capabilities:

- Beginner coaching mode with plain-language explanations and one-click examples.
- Confidence-safe "portfolio readiness" checklist.

Frequency: `Very High`

### Advanced Enterprise Design Patterns

Recurring feedback patterns:

- Some comments implicitly prefer structured design systems:
  - consistent spacing
  - reusable backgrounds/themes
  - stable filter placement
  - semantic color standards
  - drillthrough or page separation for detail

Why users react:

- Consistency and predictability matter more in enterprise portfolios than novelty.

Underlying principles:

- enterprise governance
- consistency
- semantic standardization

Detectability:

- `Theme JSON`: strong
- `Cross-page metadata`: strong
- `Bookmarks/states`: strong

Recommended analyzer capabilities:

- Cross-page consistency score.
- Org-standard theme compliance.
- Reusable dashboard archetype checks.

Frequency: `Medium`

## Subjective vs Objective Review Criteria

Mostly objective or high-confidence heuristic:

- raw technical titles
- inconsistent alignment
- missing legends where color semantics are unclear
- inconsistent severity/category colors
- low contrast
- missing date range context
- oversized control areas
- overview page dominated by detail table

More subjective:

- exact palette taste
- dark vs light theme preference
- amount of border radius
- preference for single-page vs multi-page when both support clear overview/detail flow

Design implication:

- PBIR Design Analyzer should label findings as:
  - `Objective`
  - `Strong heuristic`
  - `Style preference`

This will make the tool feel more trustworthy than an undifferentiated rule pile.

## Reviewer Disagreement Patterns

Observed disagreements:

- One-page everything visible vs multi-page overview/detail split
- Dark mode acceptance vs rejection
- Whether decorative modern styling helps professionalism

Stable agreement underneath the disagreement:

- overview must still be clear
- semantic consistency matters
- executives should not have to work hard to understand the page
- decorative styling must not outrank meaning

Product implication:

- configurable framework weighting is necessary
- page-intent profiles are necessary
- deterministic findings should be separated from taste-driven suggestions

## Capability Matrix

### High-Confidence Deterministic Rules

- Detect visible titles or obvious absence of them.
- Flag raw labels like `Sum of`, `Count of`, `Average of`, `Item_ID`.
- Check alignment, gutter consistency, and edge symmetry.
- Detect inconsistent categorical color semantics for shared fields.
- Detect missing legends when multiple colors are present with no explicit semantic cue.
- Detect contrast failures for text/background pairs.
- Detect large tables/matrices occupying overview-priority zones.
- Detect inconsistent severity category sets across visuals.
- Detect excessive slicer/control footprint.
- Detect lack of date-range disclosure when time-based measures are prominent.

### AI-Assisted Insight Opportunities

- Infer the likely business question each page is trying to answer.
- Infer the likely story a page appears to be telling from titles, chart mix, field wells, KPI placement, and supporting visuals.
- Present that inferred story as a hypothesis the user can confirm, partially confirm, or reject.
- Assess whether the displayed visuals actually answer that question.
- Identify when a page is "beautiful but useless."
- Judge whether KPI clusters communicate actionability or just display totals.
- Evaluate whether the narrative flow is overview first, then evidence, then detail.
- Generate executive-ready rewrite suggestions for titles and annotations.
- Emulate reviewer personas:
  - executive sponsor
  - BI lead
  - accessibility reviewer
  - design systems reviewer
  - skeptical stakeholder

### Screenshot / Image-Analysis Opportunities

- Detect harsh contrast, dark-on-dark text, and local legibility problems.
- Detect visual crowding that metadata alone underestimates.
- Detect overlapping or visually competing emphasis zones.
- Detect decorative overload:
  - overly strong backgrounds
  - overrounded containers
  - shadow-heavy chrome
- Estimate likely reading order from salience.
- Detect whether urgency is visually visible, not just semantically present in metadata.

### Bookmark-State-Specific Analysis Opportunities

- Audit default state vs expanded slicer-panel state.
- Compare visual density with filters shown versus hidden.
- Evaluate whether bookmarks create a cleaner progressive-disclosure experience.
- Detect state-specific overlaps or hidden-context issues.

### Cross-Page Consistency Analysis Opportunities

- Semantic color consistency for repeated fields across pages.
- Stable filter placement across report pages.
- Typography and border-radius consistency.
- Consistent KPI naming conventions.
- Consistent page archetype transitions:
  - overview
  - drilldown
  - detail
  - appendix

## Prioritized Backlog

### P0 Foundations

1. Rich visual metadata extraction for titles, legends, labels, colors, borders, font sizes, and formatting overrides.
2. Cross-visual semantic mapping for shared fields and measures.
3. Explicit finding classification: objective, strong heuristic, style preference.
4. Page-intent model: executive overview, operational monitoring, analytical deep-dive, detail table.

### P1 Deterministic Feature Pack

1. Business-language title linting.
2. Semantic color consistency checker.
3. Layout composition analyzer:
   - alignment
   - spacing
   - crowding
   - control footprint
4. KPI context analyzer:
   - timeframe
   - variance
   - target
   - benchmark
5. Cross-page consistency audit.

### P2 AI Review Layer

1. Narrative flow and decision-support assessment.
2. "Beautiful but useless" detector.
3. Expert reviewer personas and natural-language review generation.
4. Design archetype matching and benchmark comparison.

### P3 Visual Audit Layer

1. Screenshot upload and per-page audit.
2. Reading-order and salience analysis.
3. Legibility and contrast verification from actual rendered output.
4. Bookmark-state visual diffing.

## Features Competitors Likely Do Not Have

- Field-level semantic color consistency across visuals and pages.
- Bookmark-aware design review instead of single-state linting.
- Combined metadata + screenshot + semantic-graph review.
- Expert reviewer personas grounded in dashboard-review patterns rather than generic UX critique.
- "Executive readiness" scoring that distinguishes summary pages from analyst pages.
- Detection of visually polished but decision-poor dashboards.
- Natural-language review comments that mimic how experienced Power BI reviewers actually talk.

## Enterprise-Grade Differentiators

- Organization-specific design policy profiles.
- Theme-governance plus semantic-governance in one analyzer.
- Cross-page consistency scoring for large report suites.
- Persona-based review outputs for different audiences:
  - developer
  - analytics lead
  - executive consumer
  - accessibility reviewer
- Audit trails showing why a score was assigned and whether it came from deterministic logic, AI inference, or screenshot evidence.

## Beginner Coaching Opportunities

- Beginner mode that translates findings into simple explanations.
- Example-driven quick fixes:
  - rename titles
  - standardize severity colors
  - move filters to a consistent zone
  - reduce first-page matrix footprint
- "Portfolio readiness" checklist.
- Coaching comments that explain why stakeholders react negatively, not just what failed.

## Consultant / Review Workflow Opportunities

- Generate draft review comments in professional tone or blunt-expert tone.
- Save review presets by client standard.
- Compare a revised PBIR against previous review findings and show what improved.
- Export review packet with:
  - score summary
  - key findings
  - screenshot evidence
  - recommended next actions

## Natural-Language Review Generation Opportunities

- Turn deterministic findings into human-style comments:
  - "The page is visually dense and the matrix is competing with the summary KPIs."
  - "Severity colors are not semantically stable across visuals, so users will misread red/green cues."
  - "The dashboard shows history, but the business decision is still unclear."
- Provide tone presets:
  - coach
  - consultant
  - executive reviewer
  - strict design critic

## Story Inference and Intent Confirmation

Recommended capability:

- infer the story the page appears to be telling from the available PBIR/TMDL evidence
- present the result as a hypothesis, not a fact
- let the user confirm whether that matches page intent

Suggested report output:

- `Inferred story`
- `Story archetype`
- `Confidence`
- `Why this was inferred`
- `Intent match`

Example:

- `Inferred story: Revenue performance over time, with regional comparison as supporting evidence`
- `Story archetype: Executive overview + comparison`
- `Confidence: High`
- `Why this was inferred: page title, KPI band, line trend, clustered bar by region`

Why this matters:

- it creates a bridge between analyzer inference and author intent
- it lets the product detect story mismatch instead of only generic design weakness
- it supports better coaching: "the page reads like X, but you intended Y"

## Expert Reviewer Persona Ideas

- `Executive Reviewer`: values fast comprehension, exceptions, targets, and actionability.
- `BI Design Lead`: values consistency, hierarchy, and semantic color discipline.
- `Accessibility Reviewer`: values contrast, legibility, and line distinction.
- `Operations Manager`: values trend clarity, current status, and drillable exception paths.
- `Data Storytelling Reviewer`: values causal framing, comparison context, and evidence flow.

## Design Archetype Benchmark Ideas

- Executive overview
- Operational scorecard
- Sales performance cockpit
- Portfolio showcase dashboard
- Analyst deep-dive page
- Transaction/detail appendix

Analyzer opportunity:

- compare a page against its nearest archetype and explain the mismatch

## "Beautiful but Useless" Detection

Signals:

- strong formatting consistency
- attractive palette
- many cards and visuals
- weak or absent decision context
- limited variance/target framing
- redundant visuals
- unclear primary question

This should become a named AI insight because it maps directly to repeated reviewer frustration.

## Measuring Actionability and Decision Support

Proposed actionability subscore:

- explicit business question
- status framing
- comparison to target or prior period
- exception visibility
- next-step support through drilldown, decomposition, or supporting evidence

## Detecting Visual Redundancy and Duplicated KPIs

Opportunities:

- find visuals using the same measure and same grouping with only cosmetic variation
- find mirrored metrics that add no decision value
- detect repeated KPI values across cards without differentiated meaning

## Scoring Narrative Flow and Reading Order

Potential model:

- dominant entry point exists
- supporting visuals connected semantically to the primary KPI cluster
- tertiary detail placed lower or on separate pages
- reading order consistent with common left-to-right, top-to-bottom scan expectations

Metadata can estimate this. Screenshot salience analysis can validate it.

## Recommended Product Direction

Short term:

- ship stronger deterministic review for business-language titles, layout composition, color semantics, KPI context, and cross-page consistency

Medium term:

- add AI reviewer layer focused on storytelling, actionability, and archetype fit

Long term:

- combine PBIR metadata with screenshot and bookmark-state analysis into a full design-intelligence workflow

## Final Recommendation

PBIR Design Analyzer should not position itself as a generic dashboard beauty checker.

It should position itself as:

- a semantic design reviewer
- a decision-support quality analyzer
- a consistency and governance engine
- an AI-assisted expert critique system for Power BI authors, consultants, and enterprise teams

That positioning matches the strongest repeated signal from Reddit comments:

reviewers do care about aesthetics, but they care more about whether the dashboard helps someone understand something important and act on it quickly.
