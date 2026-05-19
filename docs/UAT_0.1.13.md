# PBIR Design Analyzer UAT 0.1.13

This UAT pass is for the `0.1.13` VSIX build. The goal is to validate the end-user experience after the Feature 1-5 scoring expansion and the finding-classification update.

## Build Under Test

- Extension version: `0.1.13`
- Package: `vscode-extension/pbir-design-analyzer-0.1.13.vsix`

## Install

1. Open VS Code.
2. Open the Extensions view.
3. Use `Extensions: Install from VSIX...`.
4. Select `pbir-design-analyzer-0.1.13.vsix`.
5. Reload VS Code when prompted.

## Recommended Test Data

Use these in order if available:

1. `~/Documents/GitHub/PBITesting/Sales & Production.pbip`
2. Any PBIP/PBIR project with:
   - multiple report pages
   - at least one overview page
   - some slicers
   - some cards/KPIs
   - at least one chart with a visible title

If you have purpose-built “bad layout” fixtures, include them. This release adds the most value when the report contains both compliant and non-compliant pages.

## UAT Checklist

### 1. Install and load

1. Open the PBIP project.
2. Run `PBIR Design Analyzer: Open PBIP Project`.
3. Confirm the PBIR tree loads without errors.
4. Confirm reports and pages appear in the explorer.

Expected:

- no activation error
- no missing-backend error
- explorer tree is populated

### 2. Full-report scoring

1. Run `PBIR Design Analyzer: Score Report` on a full report.
2. Wait for the score panel to render.
3. Open the overall tab and at least two page tabs.

Expected:

- overall score renders
- per-framework sections render
- per-page tabs render
- no blank or malformed feedback sections

### 3. Finding classification badges

1. Open at least three framework sections with findings.
2. Confirm findings now show badges.
3. Verify you can find all three badge types across the tested report set:
   - `Objective`
   - `Heuristic`
   - `Style`

Expected:

- scored criterion cards show a badge beside the criterion label
- supplemental findings also show a badge
- badges do not replace pass/fail tone or points

### 4. Visual metadata surface

1. In the score panel, review the `Parsed Visual Metadata` overview.
2. Open a page tab and inspect the per-visual metadata list.
3. Use `Reveal` on at least one affected visual from a finding.

Expected:

- page-level metadata summary appears when metadata is available
- visuals show titles, role hints, and formatting tags when present
- reveal jumps to the expected PBIR visual in the explorer

### 5. Storytelling and decision context

Use a page with KPIs or a clear overview layout.

Check for:

- visible page purpose feedback
- headline outcome clarity feedback
- KPI comparison-context feedback
- overview-to-detail flow feedback

Expected:

- pages with visible title intent score better than pages without it
- KPI-only pages without variance/trend/target context are flagged
- recommendations are specific to the page story problem

### 6. Hierarchy, scan path, and page composition

Use pages with clustered visuals, card bands, or scattered controls.

Check for:

- top-band KPI consistency findings
- spacing rhythm findings
- filter placement or primary scan-path findings
- long-page or overview/detail separation findings

Expected:

- findings cite affected visuals where possible
- pages with obvious lower-right or scattered controls are called out
- dense or vertically stretched pages are penalized

### 7. Chart semantics and comparison quality

Use pages with line charts, pie/donut charts, KPI cards, or comparison visuals.

Check for:

- pie/donut warnings
- categorical line-chart warnings
- KPI pages missing comparison visuals
- redundant label warnings

Expected:

- semantic chart-choice findings appear in `Visual Best Practices` and `Graphical Perception`
- messages explain why the chart choice is weak, not just that it exists

### 8. Filter ergonomics and consistency

Use repeated pages or pages with multiple slicers and mixed styling.

Check for:

- scattered filter placement findings
- overview slicer-density findings
- metric-label consistency findings
- page style language findings
- layout convention findings

Expected:

- naming drift such as `YTD Sales` vs `Sales YTD` is flagged
- mixed corners/shadows/fills across peer pages are flagged
- repeated-page title/filter convention shifts are flagged

### 9. Governance visible-title behavior

Find one page with a visible page title and one page without visible title intent.

Expected:

- governance no longer treats page metadata name alone as sufficient
- pages with visible title intent pass more cleanly
- title-policy feedback references visible title examples when available

### 10. Regression checks

Confirm these still work:

- `Configure Scoring`
- `Check Governance`
- single-page scoring from a page node
- score panel refresh
- switching between report-level and page-level tabs

Expected:

- no crashes
- no obviously wrong zero-score regressions
- no missing feedback payload errors

## Sign-Off Criteria

Accept the build if all of these are true:

- the VSIX installs and activates cleanly
- report scoring completes on at least one real PBIP/PBIR project
- the score panel shows classification badges correctly
- metadata, affected-visual reveal, and page tabs still work
- new heuristic findings are understandable and actionable
- no blocking regressions are found in scoring, governance, or navigation

## Known Out Of Scope For This UAT

These backlog items are still not implemented and should not block sign-off for `0.1.13`:

- audience presets
- structured quick-fix operations
