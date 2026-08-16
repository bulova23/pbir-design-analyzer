# How To Use PBIR Design Analyzer

PBIR Design Analyzer is a local review tool for Power BI PBIP/PBIR reports and analytical Fabric Apps. In 0.7.0, the main experience is a review workspace with these sections, in order:

1. Overview
2. Issues
3. Fix Plan
4. Review Summary
5. Story Assessment
6. Rendered Review
7. Evidence
8. Export

Overview stays open by default. Every other section, including Issues and Fix Plan, is collapsed behind a Show and Hide toggle until you open it, so you can scroll straight to the part of the review you need instead of past everything else first.

## Prerequisites

- VS Code 1.93+
- .NET 8 on the machine
- a local PBIP project or `.Report` folder

## Run The Analyzer

1. Open the PBIP project or report folder in VS Code.
2. Use **PBIR Design Analyzer: Score Report**.
3. Wait for the **PBIR Optimization Report** webview to open.
4. Start in **Overview**, then open whichever section you need next.

## Cross-Platform Score Diagnostics

Use **PBIR Design Analyzer: Copy Score Diagnostics** after scoring when you need to compare the same report across machines.

The diagnostic payload includes:

- extension version
- backend version
- platform and architecture
- analyzer type and analyzer profile
- overall score
- issue counts
- readiness score and readiness band
- page processing order
- finding IDs and evidence counts
- report fingerprint

For release validation, matching report fingerprints must produce matching score outputs.

To compare two saved diagnostic payloads locally:

```bash
cd vscode-extension
node scripts/compare-score-diagnostics.mjs /path/to/first.json /path/to/second.json
```

## Overview

Overview is the landing summary, and the only section that stays expanded by default.

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

Issues is the primary review surface, and sits directly beneath Overview so it is one click away.

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

Fix Plan is the remediation queue, and sits directly beneath Issues.

Use it when you want:

- prioritized next steps
- severity/effort framing
- affected-page context
- consultant-friendly action sequencing
- deterministic preview, apply, rollback, and re-analysis for supported fix opportunities

## Review Summary

Review Summary shows intent-confirmation status across every page in a multi-page report: confirmed, partial, mismatch, or not reviewed.

Use it to:

- see how much of the report has been reviewed
- filter pages by review status
- jump straight into a specific page from its review card
- export the review summary once enough pages are confirmed

## Story Assessment

Story Assessment reads a page like a consultant would: what it appears to say, what supports that story, what weakens it, and what should change first.

Each page shows:

- the detected story and the decision it appears to support
- story type and story maturity
- strong signals and missing signals
- Guided Story Improvements: the top prioritized recommendations for closing the biggest story gaps, each with an Open target action that jumps straight to the exact page or visual behind the recommendation when one can be identified
- a What Changed summary comparing the latest review against the previous one, once a prior review exists

Use Show Full Reasoning inside Story Assessment for the deeper detail behind the summary: Inferred Page Story, Page Intent Profile, and Intent Feedback, where you can confirm whether the inferred story matches your intent, mark mismatches or partial alignment, and add reviewer notes. This workflow improves review usability and export context. It does not mutate scores.

## Rendered Review

Rendered Review is an optional human checklist for concerns that are easier to judge from the rendered page than from PBIR metadata alone: whitespace balance, KPI prominence, title wrapping, crowded visuals, table readability, color harmony, and page readability.

For each checklist item you can:

- set a review status
- add a reviewer note
- attach a screenshot as supporting evidence

PBI Lens is an optional companion extension for future rendered observation. PBIR Design Analyzer remains authoritative for design judgment and scoring either way. When PBI Lens has no supported way to open the report programmatically, the Open in PBI Lens action is hidden rather than shown disabled, and the checklist and deterministic scoring both continue to work normally without it.

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

## Export

Export remains downstream from review.

Current behavior:

- review packet preview remains available
- review workflow export remains available
- Export is intentionally secondary to Overview, Issues, Fix Plan, Review Summary, Story Assessment, Rendered Review, and Evidence

## Current Limitations

- persona defaults are heuristic, not a second scoring system
- matrix dimension filters map to grouped impact areas in the UI
- Export remains downstream rather than a first-class workspace
- Guided Story Improvements navigation only reaches a specific visual when one can be stably inferred from public metadata; otherwise it falls back to the page level
- Rendered Review depends on a future supported PBI Lens interface for automatic rendered observation; today it is a manual, user-supplied checklist
- visual overlays and advanced enterprise-governance workflows are planned, not shipped

## What Is Planned Next

See [ROADMAP.md](./ROADMAP.md) for the next deferred epics:

1. Consultant Deliverables & Export Platform
2. Visual Intelligence & Screenshot Analysis
3. Enterprise Governance & Advanced Review
