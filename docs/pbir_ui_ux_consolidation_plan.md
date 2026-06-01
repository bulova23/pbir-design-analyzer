# PBIR Design Analyzer – UX Consolidation & Context-Aware Navigation Plan

## Purpose
Address three UX improvements:
1. Differentiate Issues and Fix Plan
2. Consolidate Story/Intent/Actionability/Benchmark/Feedback into Page Purpose Analysis
3. Make Cross-Page Matrix context-aware

## Enhancement 1 – Differentiate Issues and Fix Plan

### Issues = Diagnosis
- What is wrong?
- How severe?
- Why does it matter?
- How confident are we?

### Fix Plan = Remediation Queue
- What should I do first?
- Which fixes solve multiple issues?
- What effort is required?
- What pages are impacted?

### Design Changes
Keep Issues atomic.

Redesign Fix Plan into consolidated remediation actions.

Example:

1. Add Page Titles
   Resolves:
   - Visible Page Purpose
   - Overview-to-Detail Readability
   - Story Clarity

2. Reduce Visual Density
   Resolves:
   - One-Screen Rule
   - Cognitive Load

### Tasks
- Add finding-to-remediation mapping
- Add effort estimates (Low/Medium/High)
- Show findings resolved per action
- Add traceability from issue to remediation

---

## Enhancement 2 – Page Purpose Analysis

### Current Problem
These are displayed separately:
- Inferred Page Story
- Page Intent Profile
- Actionability
- Benchmark & Archetype
- Intent Feedback

### New Parent Container
# Page Purpose Analysis

### Section 1 – What We Think This Page Is
- inferred story
- confidence
- evidence summary

### Section 2 – Expected Behavior
- intent profile
- profile expectations

### Section 3 – Decision Support
- actionability score
- present signals
- missing signals
- recommendations

### Section 4 – Benchmark Comparison
- archetype
- benchmark
- comparison summary

### Section 5 – User Validation
- intent feedback
- review status
- override controls

### Collapsed Summary
Show:
- inferred purpose
- confidence
- actionability score
- benchmark status
- top gaps

### Tasks
- Create PagePurposeAnalysis component
- Move all related sections under one parent
- Support collapsed and expanded modes
- Preserve all existing functionality

---

## Enhancement 3 – Context-Aware Cross-Page Matrix

### Current Problem
Matrix always shows all pages.

### Desired Behavior

#### Overall View
Show full report matrix.

Used for:
- navigation
- weak page discovery
- cross-page comparison

#### Page View
Show selected page only.

Example:
- Layout
- Story
- Accessibility
- Consistency
- Navigation
- Actionability

### Tasks
- Add overall/page mode rendering
- Show full matrix in Overview
- Show single-page matrix in page review
- Add “Back to Full Matrix”
- Preserve navigation workflow

---

## Recommended Order

### Phase 1
Page Purpose Analysis Consolidation

### Phase 2
Fix Plan Remediation Queue

### Phase 3
Context-Aware Matrix

---

## Definition of Done

1. Fix Plan is clearly different from Issues.
2. Story/Intent/Actionability/Benchmark/Feedback are grouped.
3. Matrix adapts to report vs page context.
4. Scrolling is reduced.
5. No scoring changes occur.
6. Existing tests pass.
7. New tests validate behavior.
