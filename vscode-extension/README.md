# PBIR Design Analyzer

PBIR Design Analyzer is an Analytics Experience Review Platform for teams that need to evaluate report quality, dashboard quality, governance alignment, and migration readiness before analytics work is shared more broadly.

It helps consultants, BI architects, analytics teams, Power BI developers, and Fabric developers review analytical experiences with a clearer workflow:

- assess the story each page is trying to tell
- identify design, usability, actionability, navigation, and accessibility issues
- review evidence before making decisions
- build a Fix Plan with remediation guidance and deterministic fix opportunities where supported
- assess Fabric App migration readiness and review analytical Fabric Apps through the same workspace

Documentation: [How To Use PBIR Design Analyzer](../docs/HOW_TO_USE.md)

## Why Teams Use It

Most report-review tools stop at scorecards or metadata checks.

PBIR Design Analyzer is designed for real review work:

- **Story assessment** to show what a page is trying to communicate, whether the message succeeds, and what gets in the way
- **Issues workspace** to surface findings across design quality, usability, accessibility, actionability, consistency, and navigation
- **Fix Plan** to turn findings into remediation steps, advisory recommendations, and deterministic fix opportunities where safe support exists
- **Evidence-driven review** so recommendations stay tied to metadata, navigation, screenshots, semantic-model usage, and other supporting evidence
- **Governance support** for standards, consistency, accessibility, and overall analytics quality
- **Migration readiness** to assess which Power BI assets are strong candidates for Fabric App evolution and which ones need redesign first

## What’s New In 0.4.0

- Fix Plan now combines advisory AI proposal enrichment with deterministic Fix Opportunities for supported remediation items
- advisory recommendations now provide stronger explanation quality, business rationale, prioritization guidance, and expected-outcome framing
- deterministic fix workflows support preview, approval, apply, rollback, and re-analysis for supported remediation areas
- unsupported remediation remains clearly advisory rather than drifting into opaque or unsafe automation
- the review workspace now stays more stable during re-analysis after apply or rollback
- Fabric App Readiness adds migration-candidate scoring, blockers, redesign effort, and next-step guidance for PBIR reports and pages
- Fabric App Review supports analytical Fabric App review with navigation, design-token, screenshot, and semantic-model evidence

## What The Platform Reviews

### Story Assessment

- page purpose and audience fit
- headline-to-evidence flow
- summary-to-detail sequencing
- analytical clarity and actionability

### Issues Workspace

- layout and density problems
- navigation and drill-path friction
- accessibility and readability concerns
- design consistency gaps
- weak benchmark or decision-support framing

### Fix Plan

- remediation guidance tied to findings
- advisory recommendations that explain why a change matters
- deterministic fix opportunities for supported scenarios
- preview, apply, rollback, and re-analysis for the deterministic execution path

### Evidence

- metadata evidence
- navigation evidence
- screenshot evidence
- semantic-model evidence
- framework analysis and supporting review detail

### Fabric App Readiness

- migration candidates
- blockers and unsupported patterns
- redesign effort
- Fabric App suitability

### Fabric App Review

- analytical Fabric App structure and experience quality
- navigation clarity
- design-token evidence
- screenshot-backed review
- semantic-model usage evidence

## Quick Start

1. Open a local PBIP project or .Report folder.
2. Run PBIR Design Analyzer: Score Report.
3. Start in Overview to understand overall quality, top risks, and story health.
4. Use Issues to triage findings by severity, page, dimension, and scope.
5. Use Fix Plan to sequence remediation and apply supported deterministic fixes where available.
6. Use Evidence to inspect proof, supporting signals, and migration-readiness rationale.

## Review Workspace

### Overview

Use Overview to understand:

- overall analytics experience quality
- top strengths and top risks
- story gaps across the report
- which pages need attention first
- whether the asset is a strong Fabric App candidate

### Issues

Use Issues as the primary review surface when you need to inspect:

- design issues
- usability issues
- accessibility concerns
- navigation problems
- actionability gaps

### Fix Plan

Use Fix Plan when you need an action-oriented remediation workflow instead of a raw list of findings.

It brings together:

- remediation guidance
- advisory recommendations
- business rationale
- deterministic fix opportunities for supported cases

### Evidence

Use Evidence when you want to verify why a finding or recommendation exists before you act on it.

### Export

Export stays downstream from review so the product remains focused on evaluation first and deliverables second.

## Deterministic Fix Workflow

PBIR Design Analyzer preserves a strict execution boundary.

Supported deterministic fixes can:

- preview exact changes before apply
- support grouped review and approval
- apply safe metadata and layout edits in supported areas
- roll back changes if the outcome is not acceptable
- trigger re-analysis after apply

The platform does not use advisory recommendations to generate or apply freeform mutations.

## Review Modes

The workspace supports review modes for:

- Default
- Executive
- Consultant
- Governance
- Accessibility

These modes change emphasis and presentation. They do not change scoring outcomes.

## Cross-Page Matrix

The Overview matrix helps reviewers move from high-level concerns to the exact page and review dimension that needs attention.

## Typical Workflow

1. Open a PBIP project or .Report folder.
2. Score the report or page.
3. Review story quality and top risks in Overview.
4. Triage issues and evidence.
5. Sequence remediation in Fix Plan.
6. Apply supported deterministic fixes where appropriate.
7. Re-review the updated result or export findings for downstream use.

## Core Commands

- PBIR Design Analyzer: Open PBIP Project
- PBIR Design Analyzer: Refresh Reports
- PBIR Design Analyzer: Score Report
- PBIR Design Analyzer: Configure Scoring
- PBIR Design Analyzer: Check Governance
- PBIR Design Analyzer: Export Governance Report
- PBIR Design Analyzer: Export Review Workflow Summary
- PBIR Design Analyzer: Upload Report Screenshots
- PBIR Design Analyzer: Configure Visual Audit Provider

## Settings

Current shared settings focus on governance:

- powerbi-modeling.governance.enabled
- powerbi-modeling.governance.minimumCompositeScore
- powerbi-modeling.governance.approvedThemeIds

Most review behavior and scoring emphasis are managed through the extension workspace rather than a large static settings surface.

## Detailed Usage Guide

For setup, workflow details, scoring interpretation, and review guidance, see [How To Use PBIR Design Analyzer](../docs/HOW_TO_USE.md).

## Requirements

- VS Code 1.93+
- .NET 8 available on the machine for the packaged analyzer backend
- a local Power BI project using PBIP or PBIR files

## Scope

PBIR Design Analyzer is built for local analytics experience review, governance review, and migration-readiness assessment. It is not positioned as a general Fabric authoring tool, TMDL editor, or live service-management console.

## Feedback And Issues

Submit bugs, feature requests, support questions, and documentation fixes in the GitHub repo:

- [Issue forms and request tracking](https://github.com/bulova23/pbir-design-analyzer/issues)
- [Project repository](https://github.com/bulova23/pbir-design-analyzer)
