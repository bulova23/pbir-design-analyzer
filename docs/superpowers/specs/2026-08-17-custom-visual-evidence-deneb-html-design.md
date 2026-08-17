# Custom Visual Evidence — Deneb and HTML Content

## Goal

Stop the scoring engine from silently mis-scoring Deneb (Vega/Vega-Lite),
HTML Content, and other non-native visuals as if they were native comparison
charts. Replace that with honest "not analyzed" classification plus real,
statically-extracted evidence — no rendering, no screenshot automation, no
external tool dependency.

## Problem

`ClassifyAnalyticalTask` (`service-dotnet/Services/Pbir/PbirScoringService.cs:7018-7060`)
matches a fixed set of native `visualType` strings and falls through to
`return "comparison";` for anything unmatched. This includes Deneb, HTML
Content, and any other AppSource/certified custom visual — each gets scored
as if it were a native comparison-shaped chart, producing findings that look
authoritative but describe a chart shape the visual doesn't actually have.

A separate, disabled-by-default governance rule (`allowCustomVisuals` in
`service-dotnet/Services/Pbir/PbirGovernanceService.cs:313-328`) already
maintains a `_knownVisualTypes` allow-list of native types, but it is
disconnected from scoring entirely — it only ever blocks publishing when
explicitly turned on.

## Non-goals

- No rendering, screenshot capture, or browser automation of any kind.
- No dependency on PBI Lens or any external tool.
- No change to composite score weighting — this work is advisory-only, zero
  score impact, confirmed with the user.
- No governance/publish-gate interaction for HTML security signals — advisory
  finding only, confirmed with the user.
- No new user-facing setting — on by default, matching how native
  chart-semantics analysis already behaves, confirmed with the user.
- No fix to the separate, pre-existing native-visual-type classification gaps
  found while investigating this (`gauge`, map variants, `tableEx`/`pivotTable`
  not explicitly branched in `ClassifyAnalyticalTask`) — spun off as its own
  follow-up (task `task_e943e37b`), tracked independently.
- No Fabric App / Rayfin scoring — deferred separately in `docs/ROADMAP.md`
  under "Rayfin (Microsoft Fabric) — Deferred Evaluation".

## Architecture and data flow

Two independent, additive layers. Native-visual scoring is untouched.

```text
Backend (per visual, during existing metadata extraction pass)
  visualType --> NativeVisualTypeCatalog.IsNative?
    yes --> existing InferChartIntent path (unchanged) --> chartIntent
    no  --> CustomVisualEvidenceExtractor --> customVisualEvidence
              (Deneb | HtmlContent | GenericCustom record, or null on
               unparseable input)

VisualMetadataItem (existing per-visual contract type)
  chartIntent?: ChartIntentSummary            (unchanged)
  customVisualEvidence?: CustomVisualEvidence (new, mutually exclusive with
                                                chartIntent in practice)

Frontend (extension host)
  normalizedFindings.ts: buildCustomVisualFindings(result)
    walks page.visuals, emits one NormalizedFinding per visual carrying
    customVisualEvidence
  renderedReview/reviewModel.ts: classifyRenderedReviewFinding
    routes by evidence kind (new), not by title/summary keyword matching
```

### Backend: `service-dotnet/Services/Pbir/CustomVisualEvidence/` (new directory)

Follows the existing `Services/Pbir/CrossPageNarrative/` precedent — an
isolated subdirectory for a bounded scoring sub-concern, rather than adding
more to the ~10,000-line `PbirScoringService.cs`.

- **`NativeVisualTypeCatalog.cs`** — the `_knownVisualTypes` set, moved (not
  duplicated) out of `PbirGovernanceService.cs`. `PbirGovernanceService`
  references this shared catalog instead of owning its own copy. One source
  of truth for "is this visual type native."
- **`CustomVisualEvidenceExtractor.cs`** — given a visual's raw PBIR JSON node
  and its `visualType`, returns `null` for native types (per the shared
  catalog), or one of:
  - **Deneb evidence** (visual type matches Deneb's identifier — confirm the
    exact literal string and the JSON path to its embedded spec against a
    real sample during implementation, not assumed here): parses the
    embedded Vega-Lite spec and extracts mark type, `encoding.*` channel →
    field/measure bindings, and axis/legend/tooltip/title presence.
  - **HTML Content evidence** (visual type matches HTML Content's
    identifier): reads the markup string (or flags it as dynamically bound
    to a measure, not statically analyzable) and extracts content length,
    and presence of `<script`, `<style`, inline `on\w+=` handlers, and
    external `src=`/`href=` references.
  - **Generic custom evidence** — any other non-native type: just the
    `visualType` string and an explicit "not analyzed" marker.
- Wired in wherever the backend currently attaches `chartIntent` to a visual.
  `InferChartIntent`'s existing early-return guard
  (`PbirScoringService.cs:3071`, `if (visual.IsHidden || ... ) return null;`)
  gets one more condition: also return null when the visual type isn't
  native. `ClassifyAnalyticalTask`'s internals are not modified.

### Frontend: existing files, following existing per-file conventions

- **`vscode-extension/src/analyzer/contracts/scorePanel.ts`** — add
  `customVisualEvidence?: CustomVisualEvidence` to `VisualMetadataItem`
  (mirrors the existing `chartIntent?: ChartIntentSummary` field), plus the
  `CustomVisualEvidence` discriminated-union type itself. No new RPC message
  type — this rides the existing `scoreState` payload the same way every
  other `VisualMetadataItem` field does.
- **`vscode-extension/src/analyzer/score/normalizedFindings.ts`** (523 lines
  today, already one builder function per finding domain) — add
  `buildCustomVisualFindings(result)` following the existing pattern in this
  file. Each finding: `detectionType: 'deterministic'`, no score-weight
  field, evidence entry with a new `kind: 'customVisual'` value added to
  `NormalizedFindingEvidenceReference['kind']`.
- **`vscode-extension/src/analyzer/renderedReview/reviewModel.ts`** — extend
  `classifyRenderedReviewFinding` to check `finding.evidence.some(e => e.kind
  === 'customVisual')` directly (matching how it already special-cases
  `semanticModel` evidence), routing to a new `'unsupportedVisualType'`
  `RenderedReviewCategory` value (naming consistent with the existing
  category values, e.g. `whitespaceBalance`, `kpiProminence`) rather than
  relying on keyword matching against title/summary text.

## Finding content (representative, not exhaustive — exact wording finalized during implementation)

- "Deneb visual on \<page\> is not semantically analyzed — no tooltip
  encoding found." (and similar per missing axis/legend/title)
- "HTML Content visual on \<page\> contains an inline \<script\> block —
  verify behavior manually."
- "Custom visual type '\<visualType\>' on \<page\> is not analyzed; rendered
  review recommended."

All advisory, `sourceSection: 'issues'`, zero score-weight contribution, all
routed into the Rendered Review checklist so a reviewer can attach a
screenshot as the remaining aesthetic judgment call — same manual flow that
already exists, unchanged.

## Error handling

- Malformed/unparseable Deneb spec JSON → falls back to generic custom
  evidence rather than throwing or emitting partial/misleading structured
  fields.
- HTML Content bound to a dynamic measure rather than a static string →
  flagged as "dynamic, not statically analyzable," not treated as empty.
- Report with zero non-native visuals → no new findings, no behavior change.
  Purely additive.

## Testing and evidence

- Backend unit tests per extractor: valid Deneb spec → correct fields;
  malformed spec → generic fallback; HTML with/without script tags; every
  type in `NativeVisualTypeCatalog` still produces `chartIntent`, never
  `customVisualEvidence`.
- Frontend unit tests: `buildCustomVisualFindings` mirroring the existing
  builder-function test pattern in `normalizedFindings.ts`'s test file;
  `reviewModel.ts` test confirming evidence-kind routing independent of
  finding wording.
- One end-to-end regression test: score a synthetic report containing a
  Deneb visual, an HTML Content visual, and a native chart; assert the
  native chart's score and classification are completely unaffected.

## Open verification items for implementation

- Confirm Deneb's actual `visualType` literal string and the exact JSON path
  to its embedded Vega-Lite spec against a real PBIR sample — not verified
  in this design, no Deneb sample exists in this repo today.
- Confirm HTML Content's actual `visualType` literal string and where its
  markup/measure binding lives in `visual.json`.
