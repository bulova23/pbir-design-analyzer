# PBIR Story Assessment Enhancement Plan

> VS Code Extension · Power BI PBIR Design Analyzer  
> Feature: Data Story Assessment  
> Last Updated: 2026-06-06

---

## Overview

This plan details enhancements to the Story Assessment feature, which infers the narrative a Power BI report page is trying to tell from layout structure, visual intent, and semantic metadata. The goal is to make that inference richer, more actionable, and more trustworthy for report authors.

### Current Capability

- Infers story from: visible page title/question, KPI cards, lead chart type/intent, supporting evidence flow, filters/navigation/context
- Uses semantic metadata (display names, descriptions, synonyms/aliases) as reinforcing signals
- Outputs: Detected Story, Supported Decision, Why This Matters, Decision Risk, Story Gaps, Story Confidence label, Decision Support score, Benchmark rating

### Enhancement Goals

1. Replace the single confidence label with a decomposed, multi-signal breakdown
2. Classify pages against known story archetypes for more targeted gap analysis
3. Detect competing stories and cross-page narrative inconsistencies
4. Make gaps prioritized, effort-tagged, and directly actionable
5. Build a feedback loop through diff mode and a full reasoning trace
6. Mine the semantic model layer (measure descriptions, alias gaps) for deeper signal

---

## Architecture Principles

These constraints apply across every phase:

- **Backend-first, UI-second.** No new VS Code UI contracts until the underlying signal is validated against real PBIR files. Each enhancement stages backend-first, then promotes to the UI contract only when the field proves reliable.
- **Additive score contract only.** All new scoring dimensions extend the existing score object — no breaking changes to the current contract shape.
- **Every new signal carries its own `confidence` field.** The top-level score is only as trustworthy as the weakest signal feeding it.
- **Respect the Option 1 boundary.** The parser is still being validated. Do not commit a structured API contract shape to signals that haven't been proven across a real PBIR corpus.

---

## Phase 1 — Backend Signal Enrichment

> **Goal:** Expand what the parser knows before anything renders in the panel. No UI changes in this phase.

---

### 1A. Signal Registry (Internal)

Replace the single confidence output with an internal **signal registry** — a structured map of every input signal, its raw value, whether it fired, and its weighted contribution to the final score.

```typescript
interface StorySignal {
  id: string;               // e.g. "has_kpi_target", "semantic_alias_match"
  category: "layout" | "semantic" | "context" | "interaction";
  fired: boolean;
  rawValue?: string | number | boolean;
  weight: number;           // 0.0–1.0, normalized
  contribution: number;     // actual score points contributed
  remediable: boolean;      // can the author fix this in the report layer?
  effort: "quick" | "model_change" | "restructure";
}
```

This registry is purely internal in Phase 1. Every subsequent enhancement — competing story detection, gap prioritization, diff mode, the reasoning trace — depends on this structure existing.

**Deliverable:** Internal `SignalRegistry` class in the backend parser with unit tests covering at minimum: layout signals (KPI present, lead chart type, time axis), semantic signals (alias match, display name cluster), and context signals (slicer present, target/benchmark present).

---

### 1B. Archetype Classifier (Internal)

Add a story archetype classification step that runs after the existing layout + semantic inference. Define archetypes as scored templates with required and optional signals.

#### Initial Archetype Set

| Archetype | Required Signals | Strong Optional Signals |
|---|---|---|
| Performance Monitor | KPI card, time axis | Target/benchmark, prior-period delta |
| Trend + Exception | Line/area chart, anomaly highlight | Alert visual, conditional formatting |
| Ranking / Leaderboard | Bar chart sorted descending, category axis | Top-N filter, rank labels |
| Comparison | 2+ series or clustered bar, common axis | Shared legend |
| Decomposition | Hierarchy visual or drill-through | Treemap, waterfall |
| Narrative Walkthrough | Sequential text + visuals | Section dividers, annotations |

Classification is a **best-fit score** against each archetype — not a hard rule match. The winning archetype and its match confidence become internal fields on the analysis result:

```typescript
interface ArchetypeClassification {
  archetype: string;            // e.g. "PerformanceMonitor"
  matchScore: number;           // 0–100
  matchConfidence: "low" | "medium" | "high";
  signalsFired: string[];       // which required/optional signals matched
  signalsMissed: string[];      // which required signals did not match
}
```

Gap analysis in later phases uses `signalsMissed` to generate archetype-specific recommendations rather than generic heuristics.

**Deliverable:** `ArchetypeClassifier` module with scoring logic for all 6 archetypes, tested against at least 3 sample PBIR page structures per archetype.

---

### 1C. Semantic Coherence Scorer (Internal)

Add a pass that clusters all field/measure display names, aliases, and visual titles on the page, then scores how tightly they relate semantically.

**Algorithm:**

1. Extract all `displayName`, `description`, and synonym fields from visual objects on the page
2. Normalize to stems/tokens (strip articles, common suffixes)
3. Group into term clusters using a simple overlap/co-occurrence approach
4. Score coherence: if 70%+ of terms share a root concept (Revenue, Sales, Net Sales → revenue cluster), coherence is HIGH
5. Detect competing stories: if two or more unrelated clusters appear with roughly equal term weight, flag as `competingStories: true`

```typescript
interface CoherenceResult {
  score: number;                        // 0–100
  dominantConcept: string;              // e.g. "Revenue / Sales"
  termClusters: TermCluster[];
  competingStories: boolean;
  competingClusters?: TermCluster[];    // populated when competingStories: true
}

interface TermCluster {
  concept: string;
  terms: string[];
  weight: number;   // proportion of page terms in this cluster
}
```

**Deliverable:** `SemanticCoherenceScorer` module, tested on pages with (a) high coherence, (b) low coherence/noise, (c) genuine competing stories.

---

### 1D. Filter Topology Extractor (Internal)

Parse the page's slicer and filter configuration to classify the filter topology and map it to archetype reinforcement signals.

**Extract per filter/slicer:**
- Type: date range, hierarchy, single-select, multi-select, search
- Scope: page-level, report-level, visual-level
- Cardinality hint: single field vs. multi-field hierarchy

**Topology-to-archetype mapping (examples):**

| Filter Pattern | Archetype Signal |
|---|---|
| Date slicer + geography hierarchy | Reinforces "Performance Monitor" |
| Single product hierarchy | Reinforces "Category Review" (Ranking sub-type) |
| No filters at all | Reduces "Performance Monitor" confidence |
| Report-level only filters | Signals overview/landing page role |

```typescript
interface FilterTopology {
  slicers: FilterDescriptor[];
  pageFilters: FilterDescriptor[];
  reportFilters: FilterDescriptor[];
  archetypeSignals: string[];   // e.g. ["performance_monitor_reinforce"]
}
```

**Deliverable:** `FilterTopologyExtractor` module integrated into the main page analysis pipeline.

---

### Phase 1 Exit Criteria

Before proceeding to Phase 2, validate all Phase 1 signals against a real PBIR corpus:

- [ ] Run signal registry against minimum 20 real PBIR pages across at least 5 different reports
- [ ] Confirm archetype classification matches human-labeled page intent at ≥75% accuracy
- [ ] Confirm semantic coherence score correlates with author intent (high coherence pages should score >70)
- [ ] Confirm filter topology signals add information beyond layout signals alone (i.e., they change the archetype score in at least 30% of pages)
- [ ] Document any signals found to be unreliable or frequently null — exclude from Phase 2 contract

---

## Phase 2 — Score Contract Extension & Core UI

> **Goal:** Promote validated Phase 1 signals into the VS Code UI contract and update the assessment panel.  
> **Prerequisite:** Phase 1 exit criteria met.

---

### 2A. Extended Score Result Contract

Extend the score result payload with new top-level fields alongside existing `score` and `feedback`:

```typescript
interface StoryAssessmentResult {
  // Existing (unchanged)
  score: number;
  confidence: "low" | "medium" | "high";
  feedback: string;

  // New in Phase 2
  detectedArchetype: ArchetypeClassification;
  signalBreakdown: StorySignal[];
  semanticCoherence: CoherenceResult;
  storyGaps: StoryGap[];
  competingStories: CompetingStory[];
  filterTopology: FilterTopology;
}

interface StoryGap {
  id: string;
  description: string;
  effort: "quick" | "model_change" | "restructure";
  archetypeRelevance: number;     // 0–1, how critical to the detected archetype
  remediationHint: string;        // plain-language actionable text
  targetVisualId?: string;        // which visual to fix (enables Phase 3 deep links)
  layer: "report" | "model";      // where the fix lives
}

interface CompetingStory {
  clusterA: TermCluster;
  clusterB: TermCluster;
  recommendation: string;
}
```

**Note on `targetVisualId`:** Include this field in the contract now even if Phase 3 navigation isn't built yet. Surfacing the data early validates that the parser can reliably identify the relevant visual before building the UI action that depends on it.

---

### 2B. Confidence Breakdown Panel (VS Code UI)

Update the Story Assessment panel to expand the confidence label into a multi-row signal scorecard. The top-level badge remains ("Story Confidence: low") but becomes clickable to expand:

```
Signal Breakdown
─────────────────────────────────────────────
Layout signals          ████████░░  4 of 5 fired
Semantic signals        ██████░░░░  3 of 5 fired
Context signals         ████░░░░░░  2 of 5 fired
Interaction signals     ░░░░░░░░░░  0 of 5 fired
─────────────────────────────────────────────
```

Each row expands further to show individual signals in plain language:
- ✓ KPI cards present (2 detected)
- ✓ Lead visual intent: ranked comparison
- ✗ Target or benchmark not detected
- ✗ Prior-period delta not detected

**Implementation note:** The expandable breakdown is a webview panel update — no new command or activation event needed. Render from `signalBreakdown[]` in the result.

---

### 2C. Archetype Badge and Gap Reframing

Add the detected archetype to the panel header area:

```
Detected Story: Performance Monitor
Archetype Match: 68% · Confidence: Medium
```

Reframe the Story Gaps section under the archetype lens:

> *For a Performance Monitor, the following elements are typically expected:*

This anchors the author's mental model to a known pattern. "Your Performance Monitor is missing a target" lands differently than a generic "add a target." It implies the author is close to something recognizable, not failing at something undefined.

---

### 2D. Structured Gap List with Effort Tags

Replace the current flat bullet list with rendered `StoryGap[]` items, each showing effort level and layer:

```
◉ Add a target or benchmark next to the KPI card         [Quick Win · Report layer]
◉ Add prior-period delta so users can judge movement     [Quick Win · Report layer]
◉ Add cross-filter from trend chart to KPI card          [Quick Win · Report layer]
△ Prior-period measure requires new DAX                  [Model Change · Model layer]
```

**Icon key:**
- `◉` = fix is in the report/format layer (author can do it in Power BI Desktop without touching the semantic model)
- `△` = fix requires a semantic model change (new measure, description, alias, or relationship)

Gaps should be sorted: Quick Wins first, then Model Changes, then Restructure-level items. Within each tier, sort by `archetypeRelevance` descending.

---

## Phase 3 — Authoring Feedback Loop

> **Goal:** Make the assessment directly actionable — close the loop between reading a gap and fixing it.  
> **Prerequisite:** Phase 2 shipped and `targetVisualId` confirmed reliable in production.

---

### 3A. Deep Link Navigation

Use `targetVisualId` from `StoryGap` to add a "Go to visual" link next to each gap that navigates the VS Code editor to the relevant visual's JSON definition in the PBIR source file.

**Implementation:**

```typescript
// On gap link click, execute:
const uri = vscode.Uri.file(path.join(reportRoot, 'pages', pageId, 'visuals', visualId, 'visual.json'));
const doc = await vscode.workspace.openTextDocument(uri);
const editor = await vscode.window.showTextDocument(doc);
// Optionally reveal the specific field causing the gap
editor.revealRange(targetRange, vscode.TextEditorRevealType.InCenter);
```

For gaps without a `targetVisualId` (e.g., missing a visual entirely), the link navigates to the page's `page.json` instead.

---

### 3B. Story Assessment Diff Mode

Track score history per page across saves, stored in VS Code workspace state keyed by `pageId`. On each re-analysis, compute a delta and display a "What changed" section:

```
What changed since last save
─────────────────────────────────────────────
Story Confidence     low → medium             ↑
Decision Support     60 → 72                 +12
Gaps resolved        1  (target benchmark added)
New gaps             0
```

This section only appears when a prior snapshot exists. It makes the feedback loop feel rewarding and validates that edits are having the intended effect.

**Storage key pattern:** `pbir.storyHistory.{workspaceId}.{reportPath}.{pageId}`

---

### 3C. Competing Story Callout

Surface competing story detection from Phase 1C as a visually distinct warning — separate from normal gaps since it's a structural problem, not a missing element:

```
⚠ Competing Story Signals Detected

This page shows strong signals for both "Revenue Performance" 
and "Headcount Efficiency." Pages with two dominant story 
clusters are harder for operators to act on. Consider splitting 
into two pages or subordinating one story to supporting context.

  Revenue cluster: Net Sales, Gross Revenue, YTD Revenue (6 terms)
  Headcount cluster: FTE Count, Attrition Rate, HC by Dept (5 terms)
```

Display this above the Gaps section so authors see the structural issue before reading gap-level recommendations.

---

### 3D. Cross-Page Narrative Consistency

Extend analysis beyond the current page to check the page's role in the report's overall narrative flow.

**Per-page narrative role inference:**

| Role | Detection Signals |
|---|---|
| Overview / Landing | No drill-through target, report-level filters, summary KPIs only |
| Detail Drill | Has drill-through source defined, more granular dimensions |
| Exception Analysis | Conditional formatting, alert visuals, anomaly highlights |
| Input / Filter | Predominantly slicers, minimal data visuals |
| Orphaned | No incoming navigation, no drill-through source or target |

**Cross-page checks:**
- Drill-through targets that don't semantically match their source page (e.g., a "Revenue Summary" page drilling through to a "Headcount Detail" page)
- Pages with no assigned narrative role relative to their neighbors
- Orphaned pages with no navigation connection to the rest of the report

**Scope note:** This requires parsing the full report manifest (`report.json` and all `page.json` files), not just the current page. Keep this as a separate "Report Narrative" analysis command rather than running on every page save.

---

## Phase 4 — Full Reasoning Transparency

> **Goal:** Build author trust in the scoring system and support iterative development of the detection logic.

---

### 4A. Expanded "Show Full Reasoning" Panel

Replace the current single button with a structured signal evaluation trace that shows the full scoring pipeline:

```
Signal Evaluation Trace
──────────────────────────────────────────────────────
[LAYOUT]    Lead visual type: Clustered Bar           → +8 pts  (Ranking archetype fit)
[LAYOUT]    KPI cards present: Yes (2 detected)       → +10 pts
[LAYOUT]    Time axis: Not detected                   → 0 pts   (Performance Monitor miss)
[SEMANTIC]  Display names cluster: Revenue/Sales      → +12 pts (coherence: high)
[SEMANTIC]  Alias match to lead visual: Yes           → +5 pts
[CONTEXT]   Date slicer: Present                      → +5 pts  (Performance Monitor reinforce)
[CONTEXT]   Target/benchmark: Not detected            → 0 pts   (gap)
[INTERACT]  Cross-filter topology: None detected      → 0 pts
──────────────────────────────────────────────────────
Raw score:          40 / 80 possible
Archetype penalty:  -5 (incomplete Performance Monitor fit)
Final score:        45 / 100  ·  Confidence: Low
```

Color-code by signal category. Make each row expandable to show the raw value extracted from the PBIR JSON (useful during development and for advanced users who want to understand the source of each signal).

---

### 4B. Measure Description Mining

Add a parser pass over the semantic model metadata (`.tmdl` or `model.bim` if accessible from the PBIR package) to pull measure descriptions and incorporate them into story inference.

**Logic:**

- Measures with descriptions that match the inferred story concept → boost semantic confidence
- Measures with no descriptions that are used as key visuals on the page → reduce semantic confidence and add a gap:

```
△ Key measures used on this page lack descriptions in the semantic model.
  This reduces story confidence and limits Copilot / AI agent effectiveness.
  Affected measures: [Net Sales], [Gross Margin %]
  [Model Change · Model layer]
```

**Alias gap detection:** If story inference needed a synonym to fire but didn't find one, surface it explicitly:

```
ℹ Story confidence could increase if you add aliases to [Net Sales] 
  in the semantic model. Current display name matched, but no synonyms 
  are defined to reinforce the story signal.
```

This closes the loop between the report layer and the semantic model layer in a way that feeds directly into Copilot/AI agent quality as a secondary benefit.

---

## Implementation Sequence

| Phase | Item | Description | UI Change? | Prerequisite |
|---|---|---|---|---|
| 1 | 1A | Signal registry (internal) | No | None |
| 1 | 1B | Archetype classifier (internal) | No | 1A |
| 1 | 1C | Semantic coherence scorer (internal) | No | 1A |
| 1 | 1D | Filter topology extractor (internal) | No | None |
| — | **Gate** | **Validate Phase 1 against real PBIR corpus** | — | 1A–1D |
| 2 | 2A | Extended score result contract | No | Gate passed |
| 2 | 2B | Confidence breakdown panel | Yes | 2A |
| 2 | 2C | Archetype badge + gap reframing | Yes | 2A |
| 2 | 2D | Structured gap list with effort tags | Yes | 2A |
| 3 | 3A | Deep link navigation | Yes | 2A + `targetVisualId` reliable |
| 3 | 3B | Diff mode | Yes | 2A + workspace state storage |
| 3 | 3C | Competing story callout | Yes | 1C validated |
| 3 | 3D | Cross-page narrative consistency | Yes | Full manifest parsing confirmed |
| 4 | 4A | Full reasoning trace panel | Yes | 2A |
| 4 | 4B | Measure description mining | Backend + UI | Semantic model access confirmed |

---

## Key Design Decisions

**Why archetype classification rather than free-form inference?**  
Free-form inference tells an author "your page lacks context." Archetype-anchored gap analysis tells them "your Performance Monitor is missing a target — here's why that matters for this type of page." The archetype library makes implicit design knowledge explicit and gives authors a concrete target state to aim for.

**Why defer the UI contract to Phase 2?**  
The parser is still being validated against potentially malformed or partial PBIR definitions. Committing a structured contract shape before signal reliability is confirmed means every noisy or frequently-null field becomes a breaking change when removed. The Phase 1 gate enforces the same principle applied to Feature 1 in the original backlog: prove the signal before exposing it.

**Why separate `layer: "report" | "model"` on each gap?**  
Authors need to know whether they can fix a gap themselves in Power BI Desktop or whether they need to involve the semantic model owner. Mixing these two categories in a flat list creates friction and potentially sets incorrect expectations about what a single editing session can accomplish.

**Why store diff history in workspace state rather than a file?**  
Workspace state is scoped to the VS Code session and workspace, travels with the user's environment, and doesn't pollute the PBIR repo with tool metadata. If the user needs history to survive across machines (e.g., in a team setting), a `.pbir-analyzer/history.json` in the repo root could be added as an opt-in, but workspace state is the right default.
