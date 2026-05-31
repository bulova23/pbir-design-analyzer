# How To Use PBIR Design Analyzer

PBIR Design Analyzer is a local review tool for Power BI PBIP/PBIR reports. In `0.2.0`, the main experience is a review workspace:

1. Overview
2. Issues
3. Fix Plan
4. Evidence
5. Export

## Prerequisites

- VS Code 1.93+
- .NET 8 on the machine
- a local PBIP project or `.Report` folder

## Run The Analyzer

1. Open the PBIP project or report folder in VS Code.
2. Use **PBIR Design Analyzer: Score Report**.
3. Wait for the **PBIR Optimization Report** webview to open.
4. Start in **Overview**.

## Overview

Overview is the landing summary.

Use it to answer:

- how healthy is the report overall
- what is wrong first
- what should be fixed first
- which pages look weak by dimension

Overview includes:

- overall score
- maturity/risk language
- top strengths and weaknesses
- top issues
- top actions
- cross-page summary
- cross-page matrix

## Issues

Issues is the primary review surface.

Each normalized finding carries:

- severity
- confidence
- scope
- detection type
- affected pages
- impact area
- framework impact
- recommendation
- evidence references

Use filters to narrow by:

- severity
- page
- dimension
- impact area
- scope
- detection type

## Fix Plan

Fix Plan is the remediation queue.

Use it when you want:

- prioritized next steps
- severity/effort framing
- affected-page context
- consultant-friendly action sequencing

## Evidence

Evidence is the secondary drilldown layer.

It contains:

- Design Framework Analysis
- metadata explorer/detail
- AI Screenshot Audit
- scoring internals
- review packet preview

The point of Evidence is to preserve transparency without forcing every user through dense detail first.

## Review Modes

The workspace review modes are:

- Default
- Executive
- Consultant
- Governance
- Accessibility

These modes change prioritization and emphasis only. They do **not** change:

- score values
- finding severity
- finding confidence
- framework outputs

## Cross-Page Matrix Navigation

The Overview matrix shows:

- page rows
- dimension columns
- status per cell
- finding count
- high-severity count

Click a matrix cell to:

1. set the page filter
2. set the dimension filter
3. jump your attention back into Issues

You can then clear filters or reset back to the current review-mode defaults.

## Intent Confirmation And Review Feedback

When story/intent inference is available, the score panel lets you confirm whether the inferred page story matches your intent.

Use this workflow to:

- confirm page story
- mark mismatches or partial alignment
- add reviewer notes

This workflow improves review usability and export context. It does not mutate scores.

## Export

Export remains downstream from review in `0.2.0`.

Current behavior:

- review packet preview remains available
- review workflow export remains available
- Export is intentionally secondary to Overview, Issues, Fix Plan, and Evidence

Future versions will expand export/deliverables further, but that work is not part of `0.2.0`.

## Current Limitations

- persona defaults are heuristic, not a second scoring system
- matrix dimension filters map to grouped impact areas in the UI
- Export remains downstream rather than a first-class workspace
- visual overlays and advanced enterprise-governance workflows are planned, not shipped

## What Is Planned Next

See [ROADMAP.md](./ROADMAP.md) for the next deferred epics:

1. Consultant Deliverables & Export Platform
2. Visual Intelligence & Screenshot Analysis
3. Enterprise Governance & Advanced Review
