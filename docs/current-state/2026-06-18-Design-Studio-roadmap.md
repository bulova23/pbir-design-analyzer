# Report Discovery Wizard Roadmap

## Vision

Move Design Studio from:

```text
I know what report I want.
```

to:

```text
I have a semantic model.
What should I build?
```

The goal is to transform Design Studio from a report-design workflow into an analytics-experience recommendation platform.

---

# Problem Statement

Most users do not begin with:

- a report concept
- a dashboard idea
- a design brief

They begin with:

- a semantic model
- a dataset
- a business problem

and ask:

> What reports, dashboards, apps, or experiences should I create?

Current Design Studio assumes the answer already exists.

The Report Discovery Wizard helps generate that answer.

---

# Strategic Positioning

Current:

```text
Design Brief
↓
Concept
↓
Draft
↓
Review
```

Future:

```text
Semantic Model
↓
Report Discovery Wizard
↓
Opportunity Recommendations
↓
Design Studio
↓
Review
```

---

# Guiding Principles

1. Recommend experiences, not just reports.
2. Use the semantic model as the source of truth.
3. Stay provider-neutral.
4. Keep recommendations advisory.
5. Preserve Design Studio trust boundaries.
6. Preserve Analyzer Workspace validation ownership.

---

# Phase 1 – Semantic Model Discovery

## Goal

Understand what exists.

### Inputs

- Semantic model
- PBIR model
- Fabric semantic model
- Dataset metadata

### Analysis

Inspect:

- Measures
- Dimensions
- Hierarchies
- Date intelligence
- Relationships
- Cardinality
- Business domains

### Output

Discovery profile.

Example:

```text
Revenue
Margin
Customer
Product
Salesperson
Territory
Forecast
Inventory
```

---

# Phase 2 – Opportunity Identification

## Goal

Determine what questions can be answered.

### Examples

Executive Reporting

Operational Monitoring

Customer Analysis

Profitability Analysis

Inventory Optimization

Sales Performance

Forecast Accuracy

Service Operations

Root Cause Analysis

### Output

Opportunity catalog.

---

# Phase 3 – Experience Recommendation Engine

## Goal

Generate ranked recommendations.

Example:

### Executive Sales Dashboard

Why:

- strong revenue measures
- territory dimensions
- date intelligence

### Customer Profitability Analysis

Why:

- customer dimension
- profitability measures
- product hierarchy

### Inventory Operations App

Why:

- inventory facts
- operational workflows
- warehouse dimensions

---

# Phase 4 – Analytics Experience Recommendations

## Goal

Move beyond reports.

Recommended artifact types:

### PBIR Report

### Fabric App

### Fabric Data App

### Analytics Experience

### Executive Scorecard

### Operational Workspace

---

# Phase 5 – Design Studio Integration

## Goal

Convert recommendations into Design Studio artifacts.

Flow:

```text
Recommendation
↓
Design Brief
↓
Concept
↓
Draft
```

User selects a recommendation.

Wizard generates:

- Design Brief
- Concept candidates
- Initial Draft

---

# Phase 6 – Design Package Generation

## Goal

Create structured implementation packages.

Contents:

- Audience
- KPIs
- Personas
- Pages
- Navigation
- Analytical Flow
- Success Criteria

Output:

```text
Design Package
```

---

# Phase 7 – Microsoft Skills / CLI Integration

## Goal

Generate real assets.

Flow:

```text
Design Package
↓
Microsoft Power BI Skills
↓
PBIR
```

Future:

```text
Design Package
↓
Fabric App Generator
```

---

# Phase 8 – Validation Loop

## Goal

Use PBIR Design Analyzer as the quality gate.

Flow:

```text
Generated Artifact
↓
Analyzer Workspace
↓
Story Assessment
↓
Recommendations
↓
Design Studio
```

---

# Long-Term Vision

```text
Semantic Model
↓
Discovery Wizard
↓
Top 5 Analytics Experiences
↓
User Chooses
↓
Design Studio
↓
Design Package
↓
Microsoft Skills
↓
Generated Solution
↓
PBIR Design Analyzer
↓
Refinement
```

## Strategic Differentiator

The differentiator is not:

```text
Generate a report.
```

The differentiator is:

```text
Recommend the right analytics experience.
Generate the right solution.
Validate the result.
```