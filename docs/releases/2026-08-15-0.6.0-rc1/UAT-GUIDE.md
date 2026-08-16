# RC1 User Acceptance Test Guide

Version: 0.6.0 RC1
Audience: consultant, product owner, or release tester
Execution mode: manual in VS Code with a disposable PBIR workspace

Use a fresh copy of each report before every mutation test. Never run mutation
tests against a production report. Record the target OS, VS Code version,
extension VSIX filename, report fixture name, and date in the notes for every
session.

## Common test record

For each test, mark one result and record evidence:

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

## A. Installation and startup

### UAT-A01 — Install the RC1 VSIX

Objective: verify that the target-specific package installs cleanly.

Prerequisites: VS Code 1.93 or later; one matching RC1 VSIX; a disposable
workspace.

Steps:

1. Install the target-specific `.vsix` through Extensions: Install from VSIX.
2. Reload VS Code when prompted.
3. Open the Extensions view and inspect the installed extension version.

Expected results: installation completes without an error; the extension is
listed as PBIR Design Analyzer 0.6.0; no package version mismatch is shown.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-A02 — Activate and start the backend

Objective: verify extension activation and packaged backend startup.

Prerequisites: UAT-A01 passed; open a supported local PBIP workspace.

Steps:

1. Open the PBIR Design Analyzer activity-bar view.
2. Open the extension output channel and backend output channel.
3. Observe the status bar while the backend starts.
4. Run Refresh Reports.

Expected results: the explorer opens; the status changes to Ready, or a clear
degraded-mode message identifies the unavailable backend; startup diagnostics
identify the selected target and backend path; reports appear after refresh.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-A03 — Verify unsupported workspace posture

Objective: confirm the declared trust/workspace compatibility boundary.

Steps:

1. Inspect the extension manifest for unsupported untrusted and virtual
   workspace declarations.
2. If the test environment supports them, open an untrusted and a virtual
   workspace and record the actual activation behavior.

Expected results: unsupported contexts do not claim full support. If runtime
proof is unavailable, mark Blocked and record the environment limitation.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

## B. Generation

### UAT-B01 — Generate a minimal v1 report

Objective: verify the first supported generation request and output artifact.

Prerequisites: a valid v1 JSON request with a disposable output directory.

Steps:

1. Run Generate Report.
2. Select the v1 request file.
3. Inspect the output report directory.
4. Run Analyze Report on the generated artifact.

Expected results: generation succeeds; the output contains a supported PBIR
report; the response identifies artifact/manifest hashes, validation, and
round-trip score; Analyze Report returns a score and page/visual counts.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-B02 — Generate v2 multi-page Card/Table report

Objective: verify pages, Card/Table visuals, bindings, and bounded layout.

Steps: use a v2 request containing at least two pages, one Card, one Table,
measure and dimension bindings, and explicit positions; generate and inspect
the report and analyzer result.

Expected results: page order, visual order, bindings, and layout are preserved;
the analyzer completes and reports the expected counts.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-B03 — Generate rich v3–v5 report

Objective: verify formatting, themes, filters, interactions, and chart
contracts across their additive request versions.

Steps:

1. Generate representative v3, v4, and v5 requests separately.
2. Include metadata, theme, report/page/visual equality filters, Card/Table
   formatting, role-aware bindings, chart axis/legend/tooltips, and
   conditional formatting where each request supports them.
3. Inspect generated metadata and run analyzer verification.

Expected results: each request version is accepted independently; unsupported
fields are rejected or diagnosed rather than silently invented; generated
artifacts validate and round-trip evidence is returned.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-B04 — Generate v6/v7 composed report

Objective: verify templates, sections, slots, navigation, slicers, and explicit
same-page slicer interactions.

Steps: generate a v6 request with pages, a template, sections, slots,
navigation, and a slicer; generate a v7 request adding valid same-page slicer
interaction targets; inspect the result and analyzer evidence.

Expected results: the composition is deterministic and valid; navigation and
slicer targets resolve to known objects; invalid composition is rejected with
diagnostics.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

## C. Import and analysis

### UAT-C01 — Import supported PBIR

Objective: verify supported PBIR import and opaque handle creation.

Prerequisites: a disposable supported PBIR report/project directory.

Steps:

1. Run Import Report and select the report directory.
2. Record the returned success message without copying or editing the handle.
3. Confirm that page and visual metadata are available to the workflow.

Expected results: import succeeds; the backend returns a snapshot identity,
content hash, file count, page metadata, and visual metadata; raw IR and raw
filesystem details are not exposed in the user workflow.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-C02 — Analyze generated and imported reports

Objective: verify analyzer results for both sources.

Steps: run Analyze Report after UAT-B01 and UAT-C01; compare page count, visual
count, score, diagnostics, and output-channel messages.

Expected results: both analyses complete; scores are sourced from the existing
authoritative analyzer; failures are structured and understandable.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-C03 — Verify evidence and diagnostics

Objective: confirm fidelity, identity, and timing evidence is useful without
granting mutation authority to the UI.

Steps: execute one successful mutation and inspect the result; then copy score
diagnostics from the score panel.

Expected results: before/after scores, score delta, fidelity classification,
preserved identities, diagnostics, and timing observations are present; timings
are presented as observations, not guarantees.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

## D. Public mutation acceptance

For each test below, use a fresh imported snapshot and record the snapshot
handle identity before and after. The expected result for every successful
operation is: Preview shows a typed semantic diff; confirmation is required;
Execute re-plans authoritatively; the source snapshot remains unchanged; a new
artifact handle is returned; analyzer before/after and fidelity evidence are
shown; preserved object identities remain stable.

### UAT-D01 — Rename Page

Objective: verify page display-name mutation.

Steps: select Rename Page, choose a page, enter a new non-empty name, review
preview, cancel once, repeat, confirm, execute, analyze the result, and verify
the new page name.

Expected: only the intended page name changes; same-name input is a safe no-op;
empty or invalid names are rejected.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-D02 — Add Page

Objective: verify a new page can be planned, previewed, confirmed, and applied.

Steps: choose Add Page, provide the required display name/position, inspect the
page-added diff, confirm, execute, and verify page order and identity.

Expected: exactly one page is added at the requested valid position; invalid
positions fail in planning; unrelated page/visual identities are preserved.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-D03 — Remove Page

Objective: verify safe page removal.

Steps: choose Remove Page for a removable page, inspect the diff, confirm, and
analyze the result. Repeat against the last remaining page if permitted by the
fixture.

Expected: the requested page is removed; unsafe removal or unknown pages are
rejected; no unrelated page is removed; the source snapshot is unchanged.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-D04 — Move Page

Objective: verify page reordering.

Steps: choose Move Page, select a page and valid destination position, inspect
the order diff, confirm, execute, and verify page ordering.

Expected: only page order changes; invalid or out-of-range positions are
rejected; page identities remain stable.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-D05 — Move Visual

Objective: verify moving a visual between supported page/layout targets.

Steps: choose Move Visual, select a visual and destination page/order/layout,
inspect the visual-moved diff, confirm, execute, and verify source and target
page contents.

Expected: the intended visual moves once; order and page identity are correct;
duplicate targets, unknown visuals, and invalid destinations fail closed.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-D06 — Resize Visual

Objective: verify bounded visual layout mutation.

Steps: choose Resize Visual, enter valid x/y/width/height values, inspect the
layout diff, confirm, execute, and verify the resulting layout.

Expected: the visual receives the requested bounded layout; negative, oversized,
or malformed bounds are rejected; visual identity and content are preserved.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-D07 — Mutation cancellation and immutability

Objective: confirm preview cancellation and source immutability.

Steps: start each mutation, cancel at preview and confirmation, then import or
analyze the original snapshot again.

Expected: cancellation produces no artifact and no source change; the original
snapshot handle is not advanced or mutated.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

## E. Layout and visual acceptance

### UAT-E01 — Pages, templates, sections, slots, and navigation

Objective: verify composed report structure.

Steps: use UAT-B04 output; inspect page names/order, template assignment,
section membership, slot placement, and navigation targets.

Expected results: all known references resolve; order is deterministic; invalid
references are diagnosed before materialization.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-E02 — Exercise every supported visual family

Objective: verify each catalog entry is generated, laid out, bound, analyzed,
and represented in the output.

Steps: create or use a fixture containing one Card, Table, Clustered column,
Line, Bar, Pie, and Slicer visual; generate/import, inspect each visual, and
run analyzer verification.

Expected results: all seven families are accepted where the request contract
allows them; bindings and visual order are correct; unsupported visual types
are rejected or preserved as unsupported diagnostics.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

## F. Formatting, filters, and interactions

### UAT-F01 — Themes and shared formatting

Objective: verify theme and formatting projections.

Steps: apply a theme and Card/Table/slicer formatting from representative
requests; inspect generated PBIR and analyzer/fidelity output.

Expected results: colors, typography, alignment, boxes, padding, titles, and
number formats match the request; unsupported properties do not silently alter
unrelated objects.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-F02 — Filters

Objective: verify report, page, and visual equality filters.

Steps: generate a report with one filter at each supported scope; inspect
metadata and round-trip evidence; submit one invalid filter binding.

Expected results: valid filters are scoped correctly; invalid entities,
properties, or values are rejected with a diagnostic.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-F03 — Interactions and slicer interactions

Objective: verify CrossFilter, CrossHighlight, Disabled, and explicit slicer
interaction behavior.

Steps: generate valid examples for each mode; inspect target visual names and
run analyzer verification; repeat with an unknown target.

Expected results: valid modes and targets round-trip; unknown targets and
unsupported scope are rejected; screenshot evidence is not invoked or labeled
as Visual Intelligence.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

## G. Determinism and compatibility

### UAT-G01 — Repeat generation determinism

Objective: verify stable output for the same request.

Steps: run the same request twice into separate directories with the same
inputs; compare artifact hashes, manifest hashes, file inventories, and
normalized content.

Expected results: deterministic fields and output structure match; any
intentional timestamp or path differences are documented and do not change the
semantic result or score.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-G02 — Version compatibility

Objective: verify v1–v7 additive request compatibility.

Steps: run one valid request for every version; verify older requests do not
require newer fields and newer requests do not change older semantics.

Expected results: all supported versions produce their documented output or a
structured validation error; no silent fallback to another version occurs.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

## H. Error handling and fail-closed behavior

### UAT-H01 — Invalid input

Objective: verify malformed JSON, missing schema version, unsupported version,
missing required fields, and multiple selected generation versions.

Steps: submit each invalid input through Generate Report.

Expected results: the request is rejected with a concise structured diagnostic;
no output report is materialized and the extension remains usable.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-H02 — Unsupported operations

Objective: verify that backend-only mutations and standalone Validate are not
presented as supported public workflows.

Steps: inspect the command palette and curated mutation picker; attempt an
unsupported operation only through a controlled backend test harness if one is
available.

Expected results: no placeholder command is visible; the picker exposes only
the curated catalog; backend rejection uses the documented unsupported category.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-H03 — Stale and invalid handles

Objective: verify opaque handle ownership and expiry behavior.

Steps: reload/restart the backend, then attempt Analyze and Mutate with a stale
handle; also submit a handle for the wrong source/artifact.

Expected results: the operation fails safely with a structured diagnostic; no
filesystem path or raw internal state is revealed; the extension remains
responsive.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-H04 — Planner rejection and validation failure

Objective: verify invalid page positions, duplicate targets, unsafe removal,
layout bounds, unknown references, and schema validation failures.

Steps: submit each invalid mutation against a disposable report and inspect
preview/execution behavior.

Expected results: rejection occurs before materialization; no partial output is
published; diagnostics identify the failed field or target.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

## I. Existing review workflows

### UAT-I01 — Score panel and evidence workflow

Objective: verify Phase 36–48 authoring changes did not break the review
workspace.

Steps: Score Report; inspect Overview, Issues, Fix Plan, Evidence, and Export;
open a finding target; copy diagnostics; upload bounded screenshot evidence if
configured.

Expected results: scoring remains authoritative; findings render consistently;
selected page state stays within current page bounds; screenshot evidence uses
the existing evidence primitives.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

### UAT-I02 — Governance and export

Objective: verify governance checks and review workflow export.

Steps: configure scoring/governance, run Check Governance, export governance
report, and export the review workflow summary in each offered format.

Expected results: defaults and provenance are visible; governance is downstream
from scoring; exports contain the observed findings and do not mutate the PBIR.

`[ ] Pass   [ ] Fail   [ ] Blocked`

Notes: ______________________________________________________________________

## UAT exit criteria

- All installation, generation, import, analysis, mutation, layout, visual,
  formatting, determinism, and error tests are Pass or have an accepted,
  documented Blocked result.
- No release-blocking data-loss, identity, stale-handle, or package-startup
  defect remains open.
- All failures have a reproducible report fixture, target OS, VSIX filename,
  logs, and exact steps.
- Product owner signs the RC1 result before a release commit is created.

Product owner: ____________________  Date: __________  Decision: __________
