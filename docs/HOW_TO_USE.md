# How To Use PBIR Design Analyzer

PBIR Design Analyzer is a local review tool for Power BI PBIP/PBIR reports. It helps report authors inspect report structure, score design quality, tune scoring rules, and optionally run workspace governance checks before a report is shared or published.

## Prerequisites

- VS Code 1.93 or later
- .NET 8 installed on the machine
- a local Power BI PBIP workspace or direct `.Report` folder

## Core Workflow

### 1. Open A Project

Use the **Open PBIP Project** toolbar button in the PBIR Design Analyzer sidecar.

You can point the extension at:

- a `.pbip` project file
- a report workspace that already contains a `.Report` folder

After selection, the sidecar loads the report tree so you can browse reports, pages, and visuals.

### 2. Refresh The Sidecar

Use **Refresh Reports** after:

- saving the report from Power BI Desktop
- editing PBIR JSON files manually
- switching branches or updating report files outside VS Code

This re-reads the local report metadata and rebuilds the explorer tree.

### 3. Score The Report

Use **Score Report** from the toolbar or from a report or page context menu.

Scoring supports two modes:

- full-report scoring when you run the command on a report
- page-only scoring when you run the command on a specific page

The score panel opens beside the explorer and shows:

- the report name and score timestamp
- the composite score for the selected scope
- visual mix counts for data, navigation, and hidden visuals
- framework score cards for each enabled design principle
- recommendations sorted by severity

## How To Read The Optimization Report

### Full Report vs Page Score

When you score a full multi-page report, the score panel shows:

- an `Overall` tab
- one tab per page

Use `Overall` to review the report-wide composite score and overall recommendations. Use a page tab when you want the score, findings, and recommendations for only that page.

When you score a single page directly, the panel opens in page-only mode and does not show page tabs.

### Score Drill-Down Path

The score drill-down is hierarchical. Use it in this order:

1. Start with the top-level composite score for the report or page.
2. If you scored a full report, switch from `Overall` to a page tab to see how that specific page scored.
3. Expand a framework card to see how that framework contributed to the score.
4. Review the criterion-level findings, points, and improvement guidance inside that framework.
5. Open **Show affected visuals** when you need to locate the exact visuals that contributed to the finding.

The top-level numeric score is a summary indicator. The detailed score explanation is in the page tabs and expanded framework cards, not in a separate clickable score widget.

### Summary Card

The summary card near the top of the panel shows:

- the current composite score
- whether you are looking at the full report or a single page
- the current visual mix
- the active navigation-treatment mode

If navigation scoring is enabled, navigation controls count at the configured reduced weight instead of counting like full data visuals.

### How The Composite Score Is Calculated

The composite score is a weighted average of the enabled scoring frameworks for the current scope.

- each enabled framework contributes according to its configured weight
- disabled frameworks contribute `0`
- enabled framework weights must total `100`

Example:

- if `Gestalt Principles` is weighted at `30%`, it contributes 30 percent of the composite score
- if `Visual Best Practices` is weighted at `20%`, it contributes 20 percent of the composite score
- if `Enterprise Governance` is disabled, it does not affect the composite score at all

To understand why the composite score changed, look at:

- which frameworks are enabled
- the weight shown on each framework card
- the score inside each expanded framework card
- the page tab you are currently viewing

In practice, the fastest way to explain a low composite score is:

1. identify the lowest-scoring framework cards
2. check their weights
3. expand those frameworks to inspect the detailed criteria and recommendations

### Framework Cards

Each enabled framework appears as a collapsible card with:

- the framework name
- its scoring weight
- a score bar
- a short score breakdown when criterion-level points are available

Expand a framework card to drill into that part of the score.

In full-report mode:

- `Overall` helps you identify which frameworks are weakest across the report
- a page tab shows that same framework breakdown for one page only

This is the main way to answer questions like:

- why did this page score lower than the rest of the report
- which framework pulled the score down
- what specific rule or heuristic caused the framework score to drop

### Findings, Points, And Improvements

Inside an expanded framework card, the analyzer shows criterion-level entries such as:

- the criterion name
- whether the page is meeting expectation or needs improvement
- earned points versus possible points
- a `Finding` statement that explains what was detected
- an `Improve` statement when the analyzer has a direct recommendation

This is the main drill-down view for understanding why a framework scored the way it did.

### Recommendations

The **Review Recommendations** button jumps to the recommendations section. Recommendations are grouped by severity using the built-in `[High]`, `[Medium]`, and `[Low]` prefixes.

Many users get the best results by fixing the highest-severity recommendations first, then rescoring.

### Refresh Inside The Score Panel

Use the **Refresh** button in the score panel after making report changes. This re-runs scoring for the same report or page target without closing the panel.

## Drill Down To Affected Visuals

Some findings include a **Show affected visuals** expander.

Use it when you want to move from a framework finding to the actual PBIR visual that contributed to that score.

The drill-down shows:

- the visual type
- a shortened visual ID for readability
- the page name when the evidence spans more than one page

Click a listed visual to reveal that visual in the PBIR explorer sidecar. This lets you move from summary to page to framework to exact visual without searching manually through raw PBIR JSON.

## Configure Scoring

Use **Configure Scoring** to open the Design Analyzer Configuration panel.

### Enabled And Optional Frameworks

The configuration panel separates frameworks into:

- enabled frameworks that currently contribute to the composite score
- disabled frameworks that can be enabled when they add useful signal for your team

Enabled framework weights must total `100` before the configuration can be saved.

### Navigation Treatment

The **Navigation Treatment** section controls how buttons, slicers, and other navigation controls affect complexity-oriented scoring.

When enabled:

- navigation elements count at a reduced percentage of a normal data visual
- navigation elements are excluded from Data-Ink Ratio

Use this when your reports rely heavily on tabs, buttons, or bookmark-driven navigation and you do not want navigation controls to distort the score.

### Analyzer Governance Defaults

The **Analyzer Governance Defaults** section controls the local settings used by the optional **Enterprise Governance** scoring framework.

Important:

- these settings affect scoring only
- they do not turn on corporate publish blocking
- they are stored in VS Code `globalState`, not in your PBIP repo

That means they are local to the current VS Code profile or user environment unless you deliberately copy them elsewhere.

Use **Open Defaults JSON** if you want to inspect the built-in defaults template that seeds new analyzer configurations.

### Save And Reset

- **Save Configuration** stores the current analyzer config in VS Code local state
- **Reset to Defaults** restores the built-in default scoring profile

## No Corporate Governance

Corporate governance is disabled by default.

If your team does not use a shared governance policy:

- leave workspace governance settings unset
- or keep `powerbi-modeling.governance.enabled` set to `false`

In that mode:

- `Score Report` still works normally
- `Configure Scoring` still works normally
- the **Enterprise Governance** scoring framework remains off by default
- **Check Governance** returns an informational result instead of a blocking pass/fail decision

You do not need to upload a JSON policy file just to use the analyzer.

## Enable Workspace Governance

Use workspace governance only when you want a shared, corporate publish-readiness rule set for everyone working in that repo or workspace.

Configure it in `.vscode/settings.json`.

Example:

```json
{
  "powerbi-modeling.governance.enabled": true,
  "powerbi-modeling.governance.minimumCompositeScore": 80,
  "powerbi-modeling.governance.approvedThemeIds": [
    "CorporateBlue",
    "Executive"
  ]
}
```

Current blocking behavior is intentionally narrow:

- score below `minimumCompositeScore` blocks
- theme not in `approvedThemeIds` blocks when the approved list is not empty

If approved themes are configured, **Check Governance** prompts for the report theme name before running the theme validation step.

## Corporate Requirements: Scoring vs Publish Blocking

There are two separate governance paths in the product.

### 1. Local Enterprise Governance Scoring

Use this when you want the analyzer score itself to reflect corporate design expectations.

This is controlled from **Configure Scoring** and is local to the user profile unless shared manually.

Today, the Enterprise Governance scoring framework directly affects score results for:

- maximum visuals per page
- pie and donut chart allowance
- page title requirement

Other governance fields visible in the defaults template are currently template or advisory settings and should not be treated as fully enforced score logic yet.

### 2. Workspace Publish Governance

Use this when you want a repo or workspace to enforce a shared publish gate.

This is controlled from `.vscode/settings.json` and is intended to be shared with the workspace.

Today, the workspace governance check primarily enforces:

- minimum composite score threshold
- approved theme list

Advanced rule metadata can exist in workspace settings, but dynamic publish-rule evaluation is still limited in the current implementation.

## Typical Review Pattern

Many authors get the best results from this cycle:

1. Open the PBIP project.
2. Score the full report.
3. Review the `Overall` tab first.
4. Move to the lowest-scoring page tabs.
5. Expand the weakest framework cards.
6. Use **Show affected visuals** to locate exact contributors.
7. Make layout, chart, theme, or navigation changes.
8. Refresh the score panel and confirm the improvement.
9. Run **Check Governance** only if your workspace actually uses shared governance.

## Sidecar Toolbar Guide

- **Folder icon**: Open a PBIP project or `.Report` folder
- **Refresh icon**: Re-scan local PBIR report files
- **Chart icon**: Run the design analysis for the current report or page
- **Gear icon**: Open scoring and local analyzer configuration
- **Shield icon**: Run the workspace governance evaluation

## Design Principles

### Gestalt Principles

Evaluates whether layout and grouping choices help users understand the page as a coherent visual system. This includes alignment, proximity, similarity, and figure-ground separation.

### Cognitive Load

Measures how much mental effort is required to interpret a page. Pages with too many visuals, too many controls, or too many competing signals generally score lower.

### Data-Ink Ratio

Rewards visuals that maximize information and minimize decorative or redundant elements. Borders, shapes, images, and non-essential labels can reduce this score.

### Graphical Perception

Checks whether the chart type and encoding fit how people compare values accurately. Simple length and position comparisons generally score better than angle, area, or stacked clutter.

### Accessibility (WCAG)

Looks at readability and contrast-related signals that affect accessibility coverage, especially theme color contrast and choices that make the report harder to read.

### Visual Best Practices

Applies practical dashboard guidance around labeling, chart choice, consistency, and reducing avoidable friction in the page design.

### Enterprise Governance

An optional scoring framework for organization-specific standards such as page-title expectations, pie-chart policy, or visual-density limits. This is separate from workspace publish blocking.

### Stephen Few Principles

Uses dashboard design ideas associated with Stephen Few, such as emphasizing important KPIs, keeping comparisons readable, and avoiding dashboard crowding.

### Tufte Minimalism

Measures how well the report avoids chart junk and unnecessary decoration while preserving precision and clarity in the underlying data presentation.

### Dashboard Density

Evaluates whether a page contains a healthy amount of information without becoming crowded, overwhelming, or visually compressed.

### Narrative Design

Scores how well the report guides the user through a story, including page sequence, emphasis, supporting detail, and the flow from headline insights to supporting analysis.

## Troubleshooting And Feedback

- For setup or runtime issues, start with [PBIR Troubleshooting](PBIR_TROUBLESHOOTING.md).
- For bugs, feature requests, or support questions, use the [GitHub issue forms](https://github.com/bulova23/pbir-design-analyzer/issues/new/choose).
