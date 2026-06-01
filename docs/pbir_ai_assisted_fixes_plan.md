# PBIR Design Analyzer – AI-Assisted Fixes / Refactoring Engine Implementation Plan

## Vision
Transform the product from:

Analyze → Recommend

into:

Analyze → Recommend → Fix → Validate

This feature focuses on refactoring existing reports, not generating new reports.

## Strategic Goal

Allow users to:

1. Review findings.
2. Generate fix recommendations.
3. Preview proposed changes.
4. Approve or reject changes.
5. Apply approved changes.
6. Re-analyze the report.

Human approval is always required.

---

## Design Principles

### Human Approval Required
No automatic changes.

### Explainable Changes
Every fix must explain:
- what changes
- why it changes
- findings resolved
- confidence level

### Deterministic First
Start with highly predictable fixes.

### Findings Drive Fixes
Fixes originate from:
- Issues
- Remediation Queue
- Cross-page findings

---

## Phase 1 – Deterministic Fixes

### Title Standardization
Fix:
- missing titles
- weak titles
- inconsistent titles
- title anchor drift

Actions:
- add title
- rename title
- move title
- standardize placement

### Semantic Color Standardization
Fix:
- semantic color misuse
- color drift

Actions:
- apply semantic colors
- normalize colors across pages

### Alignment & Spacing
Fix:
- alignment issues
- spacing issues
- inconsistent grids

Actions:
- align visuals
- equal spacing
- normalize layout

### Navigation Consistency
Fix:
- navigation drift
- button placement inconsistencies

Actions:
- standardize controls
- standardize back buttons

### Cross-Page Consistency
Fix:
- title anchor drift
- filter placement drift
- layout drift

Actions:
- synchronize patterns
- normalize layouts

---

## User Workflow

Analyze
↓
Review Findings
↓
Generate AI Fixes
↓
Preview Changes
↓
Approve / Reject
↓
Apply Changes
↓
Re-Analyze

---

## UI Placement

### Issues Workspace

Issue Card

- Description
- Severity
- Recommendation

Add:

[Generate Fix]

for supported findings.

### Remediation Queue

Add:

AI Fix Opportunities

Example:

Reduce Visual Density

Resolves:
- Visual Density
- Decoration Minimalism

[Preview AI Fix]

---

## Proposed Fix Contract

```ts
export interface ProposedFix {
  id: string;
  title: string;
  description: string;
  confidence: number;
  impact: "Low" | "Medium" | "High";
  effort: "Low" | "Medium" | "High";
  affectedPages: string[];
  sourceFindingIds: string[];
}
```

---

## Change Preview

Every fix should support:

- before state
- after state
- findings resolved
- confidence score
- approval workflow

Example:

Align KPI Cards

Confidence: 95%

Resolves:
- Grid Alignment

Changes:
- Move KPI A
- Move KPI B
- Normalize spacing

[Apply]
[Reject]

---

## Change Application Engine

Responsibilities:

- apply approved changes
- validate changes
- create backups
- support rollback
- trigger re-analysis

Potential targets:

- PBIR
- TMDL
- theme files
- layout metadata

---

## Fix Categories

### Safe
- titles
- colors
- spacing
- alignment

### Moderate
- KPI hierarchy
- grouping

### Advisory
- storytelling
- page sequencing
- executive narrative

---

## Future Enhancements

### Layout Optimization
Automatically improve:
- density
- spacing
- grouping

### Story Optimization
Suggest:
- titles
- sections
- narrative flow

### Executive Upgrade
Convert operational pages into executive-focused layouts through guided recommendations.

---

## Architecture

Current:

ScoreResult
↓
Normalized Findings
↓
Issues
↓
Fix Plan

Future:

ScoreResult
↓
Normalized Findings
↓
Fix Opportunity Builder
↓
Proposed Fixes
↓
Preview
↓
Apply
↓
Re-Analyze

---

## Implementation Phases

### Phase 1
Deterministic Fixes

### Phase 2
Preview Engine

### Phase 3
Apply Engine

### Phase 4
Advanced Refactoring

---

## Testing Strategy

### Unit Tests
- fix generation
- confidence logic
- validation

### Integration Tests
- preview generation
- PBIR updates
- rollback

### UX Tests
- approval workflow
- explainability
- re-analysis flow

---

## Non-Goals

Do not:
- generate entire reports
- silently modify reports
- change scoring logic
- change severity/confidence
- replace human review

---

## Definition of Done

Phase 1 is complete when:

- deterministic fixes can be generated
- fixes can be previewed
- fixes show findings resolved
- users can approve/reject
- changes can be applied safely
- reports can be re-analyzed

---

## Recommended Roadmap Placement

1. UX Consolidation (Complete)
2. AI-Assisted Fixes / Refactoring Engine
3. Consultant Deliverables & Export Platform
4. Visual Intelligence & Screenshot Analysis
5. Enterprise Governance & Advanced Review
6. Report Design Studio
