# PBIR Design Analyzer Roadmap

This roadmap reflects the post-`0.2.0` product direction after the score-panel workspace modernization shipped.

## Current Release: 0.2.0

`0.2.0` establishes the core review workspace:

- Overview
- Issues
- Fix Plan
- Evidence
- secondary Export
- normalized findings
- smart collapse defaults
- intent confirmation and review feedback
- workspace review modes
- cross-page matrix navigation

The next roadmap epics build on that foundation without reopening the scoring architecture.

## Recommended Order

### 1. Consultant Deliverables & Export Platform

**Business value:** High  
**Risk:** Medium  
**Complexity:** Medium  
**Quick wins:** High  
**Strategic value:** High

Why first:

- export and packet-preview foundations already exist
- the product can turn current review outputs into clearer consultant/client deliverables quickly
- this improves real-world adoption, sharing, and monetizable presentation quality

Quick wins:

- cleaner export workspace
- persona-aware export-summary wording
- stronger executive summary language
- branded consultant-ready packet variants

Longer-term value:

- future DOCX/PDF architecture
- AI-generated executive narrative options
- export as a first-class deliverables platform

## 2. Visual Intelligence & Screenshot Analysis

**Business value:** High  
**Risk:** Medium  
**Complexity:** Medium to High  
**Quick wins:** Medium  
**Strategic value:** High

Why second:

- screenshot audit foundations already exist
- visual overlays and screenshot-to-finding linkage improve explainability and perceived intelligence
- this creates a strong product differentiator after export/deliverable polish

Quick wins:

- screenshot-to-finding linkage
- visual evidence navigation
- annotated evidence panels

Longer-term value:

- reading-order overlays
- density heatmaps
- alignment and focus-area highlighting

## 3. Enterprise Governance & Advanced Review

**Business value:** Medium to High  
**Risk:** High  
**Complexity:** High  
**Quick wins:** Low to Medium  
**Strategic value:** Very High

Why third:

- this touches the broadest set of contracts and workflows
- it depends on stable configuration, benchmark, and review patterns
- it is strategically strong but less release-efficient than deliverables and visual explainability

Quick wins:

- organization profile scaffolding
- better configuration IA
- benchmark summary expansion

Longer-term value:

- custom standards and templates
- industry-specific review modes
- bookmark-state analysis
- mobile/responsive review workflows

## Epic Summary

### Consultant Deliverables & Export Platform

See:

- [Design Spec](./superpowers/specs/2026-05-31-consultant-deliverables-export-platform-design.md)
- [Implementation Plan](./superpowers/plans/2026-05-31-consultant-deliverables-export-platform-plan.md)

### Visual Intelligence & Screenshot Analysis

See:

- [Design Spec](./superpowers/specs/2026-05-31-visual-intelligence-screenshot-analysis-design.md)
- [Implementation Plan](./superpowers/plans/2026-05-31-visual-intelligence-screenshot-analysis-plan.md)

### Enterprise Governance & Advanced Review

See:

- [Design Spec](./superpowers/specs/2026-05-31-enterprise-governance-advanced-review-design.md)
- [Implementation Plan](./superpowers/plans/2026-05-31-enterprise-governance-advanced-review-plan.md)

## Guardrails

These roadmap epics should not:

- rewrite the core scoring engine unnecessarily
- mutate score or finding severity/confidence from presentation modes
- replace normalized findings as the shared issue model
- turn temporary roadmap experiments into hidden scoring logic

The preferred path is:

- stable scoring layer
- stable findings layer
- richer review, evidence, export, and governance workflows built above them
