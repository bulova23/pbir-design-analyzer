# How To Use PBIR Design Analyzer

PBIR Design Analyzer is a local review tool for Power BI PBIP/PBIR reports. It helps report authors inspect report structure, score design quality, tune scoring rules, and check governance readiness before a report is shared or published.

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

Use **Score Report** from the toolbar or report/page context menu.

The score report shows:

- an overall composite score
- page tabs for report navigation
- per-framework score cards
- drilldown findings and recommendations
- affected visual references where the analyzer can identify exact contributors

Select a page before scoring if you want a page-only analysis instead of a full report score.

### 4. Configure Scoring

Use **Configure Scoring** to:

- enable or disable design principles
- change framework weights
- tune governance defaults
- adjust how much navigation controls influence scoring

Enabled principle weights must total `100` before settings can be saved.

### 5. Check Governance

Use **Check Governance** when you want a publish-readiness decision rather than just a score.

Governance can enforce:

- minimum composite score thresholds
- approved theme names
- enterprise-specific rules such as required page titles, maximum page counts, or custom visual restrictions

## Sidecar Toolbar Guide

- **Folder icon**: Open a PBIP project or `.Report` folder
- **Refresh icon**: Re-scan the local PBIR report files
- **Chart icon**: Run the design analysis for the current report or page
- **Gear icon**: Open scoring and governance configuration
- **Shield icon**: Run the governance evaluation

## How To Read The Score Report

The score report is intentionally layered:

- the top level gives you the overall report score or page score
- each framework card shows a score breakdown by sub-principle
- expanding a framework card reveals detailed findings and recommendations
- some findings include **Show affected visuals** drilldowns with `visual type + short ID`
- clicking a referenced visual reveals that visual in the PBIR explorer sidecar

This lets you move from summary to evidence without forcing every user to read raw visual IDs immediately.

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

Applies organization-specific publishing standards. This principle is intended for corporate or team-driven rules such as approved themes, custom visual restrictions, required titles, page limits, and similar governance controls.

### Stephen Few Principles

Uses dashboard design ideas associated with Stephen Few, such as emphasizing important KPIs, keeping comparisons readable, and avoiding dashboard crowding.

### Tufte Minimalism

Measures how well the report avoids chart junk and unnecessary decoration while preserving precision and clarity in the underlying data presentation.

### Dashboard Density

Evaluates whether a page contains a healthy amount of information without becoming crowded, overwhelming, or visually compressed.

### Narrative Design

Scores how well the report guides the user through a story, including page sequence, emphasis, supporting detail, and the flow from headline insights to supporting analysis.

## Typical Review Pattern

Many authors get the best results from this cycle:

1. Open the PBIP project.
2. Score the full report.
3. Review the lowest-scoring frameworks first.
4. Drill into the specific page tabs and affected visuals.
5. Adjust layout, chart choice, or governance issues.
6. Re-score to confirm the improvement.

## Troubleshooting And Feedback

- For setup or runtime issues, start with [PBIR Troubleshooting](PBIR_TROUBLESHOOTING.md).
- For bugs, feature requests, or support questions, use the [GitHub issue forms](https://github.com/bulova23/pbir-design-analyzer/issues/new/choose).
