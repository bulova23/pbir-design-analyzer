# Report Discovery Wizard Consultant Benchmark Review

Date: 2026-06-20

## Scope

This review benchmarks current Discovery Wizard outputs against human consultant reasoning across six required scenarios:

- Revenue / Sales
- Customer Profitability
- Inventory Operations
- Service Operations
- Forecasting
- Analytical Investigation

In scope:

- Discovery Profile
- Opportunity Catalog
- Recommendation Engine
- Experience Blueprint generation
- Design Studio starting-point quality
- Design Package quality
- consultant-comparison scoring
- readiness assessment

Out of scope:

- product-code changes
- feature additions
- architecture changes
- Microsoft Skills integration work
- CLI integration work

## Method

This benchmark used:

- the current discovery implementation in backend discovery services
- the Discovery Wizard design spec
- the Round 8 validation review
- a temporary out-of-repo reflection harness that exercised the live backend workflow end to end against six representative semantic-model scenarios without modifying product code
- consultant-style human comparison outputs generated against those same scenarios

Each scenario was reviewed in three passes:

1. Discovery Wizard output review
2. human consultant recommendation synthesis
3. side-by-side gap analysis and scoring

Important note:

- The benchmark showed the same core pattern as Round 8 even though the exact synthetic semantic-model mix differs from earlier validation runs.
- The remaining weaknesses are real product gaps, not only stylistic disagreement.

## Executive Summary

Discovery Wizard is now materially stronger than the earlier validation rounds in one important area: it usually finds a credible opportunity set. Across the benchmark scenarios it consistently produced multi-option portfolios with meaningful breadth instead of collapsing into one thin direction.

That said, the benchmark still exposes three real gaps that matter more than stylistic differences:

1. Lead recommendation trustworthiness is still inconsistent.
   - Service Operations still promotes a generic investigation-first path ahead of more consultant-defensible monitoring or workflow recommendations.
   - Analytical Investigation can still collapse into executive forecasting logic when mixed signals are present.

2. Provider-grade downstream quality is still below trust threshold.
   - Inventory still injects unsupported fallback KPIs such as Backlog Trend and Open Exceptions.
   - Service still leaks off-domain KPI choices such as Revenue and Gross Margin.
   - Filter guidance still exposes internal semantic-model names such as DimCustomer, DimDate, and DimProduct.
   - Design Package rationale still contains broken stringification such as System.String[].

3. Consultant credibility is still uneven.
   - In the stronger scenarios, Discovery Wizard looks like a credible consultant accelerator.
   - In the weaker scenarios, it still looks like a heuristic engine selecting from templates rather than a senior consultant defending the right experience for the right operating rhythm.

Decision gate:

- **B. One Final Targeted Refinement**

Why not A:

- Design Package quality is not yet trustworthy enough for downstream consumption.
- Lead recommendation quality is still not stable enough in the hardest scenarios.

Why not C:

- the remaining problems are concentrated in recommendation judgment and package fidelity
- the architecture already has the right boundaries
- the gaps do not justify another architecture change

## Long-Term Risk Ranking

1. **High risk: recommendation trust breaks consultant credibility in ambiguous scenarios**
   - If lead-choice quality remains unstable, every downstream artifact inherits the wrong posture.
   - This is the largest risk to user trust, Design Package consumption, and future Microsoft Skills planning.

2. **High risk: Design Package fidelity is still below provider-trust quality**
   - Unsupported KPIs, internal naming leakage, and malformed rationale fields will compound once a downstream consumer treats the package as authoritative.

3. **Medium risk: blueprint clustering still reduces true portfolio diversity**
   - Even when the Top 3 looks broad, near-duplicate blueprint structures still flatten the practical value of multiple recommendations.

4. **Medium risk: consultant language is improving faster than consultant judgment**
   - The wording increasingly sounds credible, but credibility drops when the selected experience is wrong or the package leaks implementation-shaped details.

## Scenario Comparisons

### Scenario A – Revenue / Sales

**Discovery Wizard output**

- Opportunities:
  - Customer Profitability Analysis
  - Comparative Performance Management
  - Executive Revenue Dashboard
  - Executive Sales Reporting
  - Forecast Accuracy Dashboard
  - Forecast Operations Follow-Through
  - Forecast Planning Review
  - Revenue Performance Management
  - Root Cause Analysis Experience
  - Sales Investigation Experience
  - Sales Performance Dashboard
- Top recommendations:
  - Forecast Accuracy Dashboard → Executive Dashboard
  - Root Cause Analysis Experience → Analytical Investigation Experience
  - Forecast Planning Review → Executive Dashboard
- Lead blueprint:
  - Pages: Planning Summary, Variance Review, Regional Follow-Up
  - KPIs: Revenue, Gross Margin, Sales Growth, Forecast Accuracy, Forecast Variance
  - Filters: DimCustomer, DimDate, DimProduct

**Human consultant output**

- Opportunities:
  - Executive Sales and Forecast Review
  - Sales Performance Management
  - Forecast Follow-Through
  - Customer Profitability Drill Path
  - Sales Variance Investigation
- Recommended ranking:
  - Sales Performance Management → Fabric App
  - Executive Sales and Forecast Review → Executive Dashboard
  - Customer Profitability Drill Path → Fabric Data App
  - Alternate: Forecast Follow-Through → Operational Monitoring Experience
  - Alternate: Sales Variance Investigation → Analytical Investigation Experience
- Preferred blueprint:
  - Pages: Leadership Summary, Territory and Segment Performance, Pipeline and Forecast Risk, Follow-Through Actions
  - KPIs: Revenue, Gross Margin, Forecast Accuracy, Pipeline Coverage, Variance to Target
  - Filters: Date, Region, Territory, Product Category, Customer Segment

**Gap analysis**

- Discovery Wizard found the right breadth.
- It overweights planning-summary logic relative to revenue operating management.
- The consultant would still want a stronger action-oriented commercial path near the top.
- The lead package is useful, but internal filter naming materially reduces provider-grade credibility.

**Scores**

- Opportunity Quality: 5
- Recommendation Quality: 3
- Experience Selection: 3
- Blueprint Quality: 3
- Design Package Quality: 2
- Consultant Similarity: 3

### Scenario B – Customer Profitability

**Discovery Wizard output**

- Opportunities:
  - Customer Profitability Analysis
  - Revenue Performance Management
  - Sales Investigation Experience
  - Comparative Performance Management
  - Executive Revenue Dashboard
  - Executive Sales Reporting
  - Root Cause Analysis Experience
  - Sales Performance Dashboard
- Top recommendations:
  - Customer Profitability Analysis → Fabric Data App
  - Root Cause Analysis Experience → Analytical Investigation Experience
  - Revenue Performance Management → Fabric App
- Lead blueprint:
  - Pages: Data Explorer, Segment Analysis, Record Detail
  - KPIs: Revenue, Gross Margin, Variance, Profit per Customer
  - Filters: DimCustomer, DimDate, DimRegion

**Human consultant output**

- Opportunities:
  - Customer Profitability Explorer
  - Segment Profitability Review
  - Account Action Dashboard
  - Margin Driver Investigation
  - Price and Mix Deep Dive
- Recommended ranking:
  - Customer Profitability Explorer → Fabric Data App
  - Segment Profitability Review → PBIR Report
  - Account Action Dashboard → Fabric App
  - Alternate: Margin Driver Investigation → Analytical Investigation Experience
  - Alternate: Executive Profitability Snapshot → Executive Dashboard
- Preferred blueprint:
  - Pages: Segment Explorer, Account Profitability, Margin Drivers, Action Queue
  - KPIs: Gross Margin, Profit per Customer, Revenue, Margin Rate, Variance
  - Filters: Date, Customer Segment, Industry, Region

**Gap analysis**

- Discovery Wizard’s lead choice is consultant-defensible.
- The portfolio still leans investigation-heavy after the lead recommendation.
- The blueprint is good, but it is still narrower than a consultant would likely make for downstream actionability.
- Design Package wording is directionally good, but internal field naming still weakens handoff quality.

**Scores**

- Opportunity Quality: 4
- Recommendation Quality: 4
- Experience Selection: 4
- Blueprint Quality: 4
- Design Package Quality: 3
- Consultant Similarity: 4

### Scenario C – Inventory Operations

**Discovery Wizard output**

- Opportunities:
  - Inventory Investigation
  - Inventory Operations Monitoring
  - Inventory Planning
  - Root Cause Analysis Experience
  - Warehouse Performance
- Top recommendations:
  - Inventory Operations Monitoring → Operational Monitoring Experience
  - Root Cause Analysis Experience → Analytical Investigation Experience
  - Inventory Planning → Executive Dashboard
- Lead blueprint:
  - Pages: Overview, Exceptions, Detail
  - KPIs: Stock Variance, Backlog Trend, Inventory Quantity, Inventory Value, Open Exceptions
  - Filters: DimDate, DimItem, DimWarehouse

**Human consultant output**

- Opportunities:
  - Inventory Command Center
  - Replenishment Risk Review
  - Warehouse Performance Report
  - Inventory Root Cause Investigation
  - Inventory Planning Review
- Recommended ranking:
  - Inventory Command Center → Operational Monitoring Experience
  - Replenishment Risk Review → PBIR Report
  - Inventory Planning Review → Executive Dashboard
  - Alternate: Warehouse Performance Report → PBIR Report
  - Alternate: Inventory Root Cause Investigation → Analytical Investigation Experience
- Preferred blueprint:
  - Pages: Operations Summary, Exception Queue, Warehouse Detail, Replenishment Risk
  - KPIs: Inventory Quantity, Inventory Value, Stock Variance, Stockout Risk, Aged Inventory
  - Filters: Date, Warehouse, Product Category, Item

**Gap analysis**

- Opportunity depth is now credible.
- Lead recommendation is consultant-defensible.
- The main weakness is fidelity, not selection.
- Backlog Trend and Open Exceptions still look like template fallback KPIs rather than model-grounded inventory measures.
- The planning recommendation is still too generic to count as a true consultant-quality alternate.

**Scores**

- Opportunity Quality: 4
- Recommendation Quality: 4
- Experience Selection: 4
- Blueprint Quality: 3
- Design Package Quality: 2
- Consultant Similarity: 3

### Scenario D – Service Operations

**Discovery Wizard output**

- Opportunities:
  - Service Performance Management
  - Root Cause Analysis Experience
  - Service Investigation
  - Service Operations Dashboard
  - Service Workflow Coordination
- Top recommendations:
  - Root Cause Analysis Experience → Analytical Investigation Experience
  - Service Operations Dashboard → Operational Monitoring Experience
  - Service Workflow Coordination → Fabric App
- Lead blueprint:
  - Pages: Question, Investigation, Evidence, Conclusion
  - KPIs: Revenue, Gross Margin, Variance, Open Work Orders, Resolution Time
  - Filters: DimDate, DimTechnician, DimWorkOrder

**Human consultant output**

- Opportunities:
  - Service Operations Dashboard
  - Service Workflow Coordination
  - Service Performance Review
  - SLA Risk Investigation
  - Technician Load Analysis
- Recommended ranking:
  - Service Operations Dashboard → Operational Monitoring Experience
  - Service Workflow Coordination → Fabric App
  - Service Performance Review → PBIR Report
  - Alternate: SLA Risk Investigation → Analytical Investigation Experience
  - Alternate: Technician Load Analysis → PBIR Report
- Preferred blueprint:
  - Pages: Command Center, SLA and Backlog Risk, Technician Detail, Workflow Follow-Up
  - KPIs: Open Work Orders, Resolution Time, SLA Breach Risk, Technician Utilization, Backlog Aging
  - Filters: Date, Region, Technician, Priority, Service Line

**Gap analysis**

- This is still a real product gap, not a style disagreement.
- A consultant would not normally lead a service operations model with a generic root-cause investigation unless the scenario were explicitly forensic.
- The injected Revenue and Gross Margin KPIs materially damage trust.
- Discovery Wizard already knows the better options; it is choosing the wrong winner.

**Scores**

- Opportunity Quality: 4
- Recommendation Quality: 2
- Experience Selection: 2
- Blueprint Quality: 2
- Design Package Quality: 1
- Consultant Similarity: 1

### Scenario E – Forecasting

**Discovery Wizard output**

- Opportunities:
  - Sales Investigation Experience
  - Comparative Performance Management
  - Executive Revenue Dashboard
  - Executive Sales Reporting
  - Forecast Accuracy Dashboard
  - Forecast Operations Follow-Through
  - Forecast Planning Review
  - Revenue Performance Management
  - Root Cause Analysis Experience
  - Sales Performance Dashboard
- Top recommendations:
  - Forecast Accuracy Dashboard → Executive Dashboard
  - Revenue Performance Management → Fabric App
  - Root Cause Analysis Experience → Analytical Investigation Experience
- Lead blueprint:
  - Pages: Planning Summary, Variance Review, Regional Follow-Up
  - KPIs: Revenue, Forecast Accuracy, Forecast Variance, Variance, Actuals
  - Filters: DimDate, DimProduct, DimRegion

**Human consultant output**

- Opportunities:
  - Forecast Leadership Review
  - Forecast Accuracy Dashboard
  - Forecast Follow-Through
  - Forecast Variance Investigation
  - Regional Planning Review
- Recommended ranking:
  - Forecast Leadership Review → Executive Dashboard
  - Forecast Follow-Through → Operational Monitoring Experience
  - Forecast Variance Investigation → Analytical Investigation Experience
  - Alternate: Forecast Accuracy Dashboard → PBIR Report
  - Alternate: Regional Planning Review → Fabric App
- Preferred blueprint:
  - Pages: Forecast Summary, Variance Drivers, Regional Commitments, Follow-Up Actions
  - KPIs: Forecast Accuracy, Forecast Variance, Actuals, Commit Amount, Risk Exposure
  - Filters: Date, Forecast Period, Scenario, Region, Product Category

**Gap analysis**

- Discovery Wizard is close here.
- The current lead recommendation is credible, but it still leans too heavily on generic planning-summary structure.
- The consultant would likely separate executive review from follow-through more clearly.
- Package fidelity remains limited by generic/internal naming conventions.

**Scores**

- Opportunity Quality: 4
- Recommendation Quality: 4
- Experience Selection: 4
- Blueprint Quality: 3
- Design Package Quality: 3
- Consultant Similarity: 4

### Scenario F – Analytical Investigation

**Discovery Wizard output**

- Opportunities:
  - Customer Profitability Analysis
  - Forecast Accuracy Dashboard
  - Forecast Planning Review
  - Revenue Performance Management
  - Sales Investigation Experience
  - Comparative Performance Management
  - Executive Revenue Dashboard
  - Executive Sales Reporting
  - Forecast Operations Follow-Through
  - Root Cause Analysis Experience
  - Sales Performance Dashboard
- Top recommendations:
  - Forecast Accuracy Dashboard → Executive Dashboard
  - Customer Profitability Analysis → Fabric Data App
  - Forecast Planning Review → Executive Dashboard
- Alternate:
  - Root Cause Analysis Experience → Analytical Investigation Experience
- Lead blueprint:
  - Pages: Planning Summary, Variance Review, Regional Follow-Up
  - KPIs: Revenue, Gross Margin, Forecast Accuracy, Variance, Actuals
  - Filters: DimCustomer, DimDate, DimProduct

**Human consultant output**

- Opportunities:
  - Root Cause Analysis Experience
  - Variance Driver Workspace
  - Customer and Product Drill Path
  - Hypothesis Review Report
  - Executive Escalation Summary
- Recommended ranking:
  - Root Cause Analysis Experience → Analytical Investigation Experience
  - Variance Driver Workspace → PBIR Report
  - Customer and Product Drill Path → Fabric Data App
  - Alternate: Hypothesis Review Report → PBIR Report
  - Alternate: Executive Escalation Summary → Executive Dashboard
- Preferred blueprint:
  - Pages: Question Framing, Driver Branching, Evidence Review, Conclusion and Next Action
  - KPIs: Variance, Revenue, Gross Margin, Forecast Accuracy, Exception Count
  - Filters: Date, Customer Segment, Product Category, Region

**Gap analysis**

- This is the clearest benchmark miss.
- The scenario label and semantic cues should produce an investigation-first lead.
- Discovery Wizard instead collapsed back into executive forecasting logic.
- This is not a wording issue. It is a ranking and intent-preservation issue.

**Scores**

- Opportunity Quality: 3
- Recommendation Quality: 1
- Experience Selection: 1
- Blueprint Quality: 1
- Design Package Quality: 2
- Consultant Similarity: 1

## Cross-Scenario Findings

### Genuine gaps

- Lead recommendation quality is still unstable in service and analytical scenarios.
- Experience selection still over-favors executive dashboard logic when mixed variance and forecasting signals coexist.
- Design Package KPI selection still relies on fallback injection too often.
- Design Package filter guidance still uses semantic-model internals instead of consultant-facing labels.
- Design Package rationale still contains malformed field rendering in some sections.

### Style-only differences

- A consultant might reorder some alternates differently without it being a real defect.
- A consultant might prefer PBIR over Fabric App in some second-choice positions based on delivery culture.
- A consultant might vary page naming and section language without changing the underlying design quality.

### Architecture assessment

- The architecture is not the blocker.
- The current design boundaries still make sense:
  - semantic-model discovery upstream
  - recommendation curation in the middle
  - blueprint and package downstream
- The remaining gaps look like heuristic quality and fidelity problems within the existing architecture, not evidence that the architecture should be widened or replaced.

## Score Summary

| Scenario | Opportunity | Recommendation | Experience | Blueprint | Design Package | Similarity |
| --- | --- | --- | --- | --- | --- | --- |
| Revenue / Sales | 5 | 3 | 3 | 3 | 2 | 3 |
| Customer Profitability | 4 | 4 | 4 | 4 | 3 | 4 |
| Inventory Operations | 4 | 4 | 4 | 3 | 2 | 3 |
| Service Operations | 4 | 2 | 2 | 2 | 1 | 1 |
| Forecasting | 4 | 4 | 4 | 3 | 3 | 4 |
| Analytical Investigation | 3 | 1 | 1 | 1 | 2 | 1 |

Average scores:

- Opportunity Quality: 4.0
- Recommendation Quality: 3.0
- Experience Selection: 3.0
- Blueprint Quality: 2.7
- Design Package Quality: 2.2
- Consultant Similarity: 2.7

## Readiness Assessment

### 1. Are Discovery Wizard outputs materially different from consultant outputs?

Yes.

- In the best scenarios, the difference is moderate and mostly about ranking nuance.
- In Service Operations and Analytical Investigation, the difference is material and directly affects trust.

### 2. Are remaining gaps significant?

Yes.

- The remaining gaps are concentrated, but they are significant because they affect lead recommendation credibility and downstream package trust.

### 3. Are remaining gaps worth additional architecture work?

No.

- The architecture is already good enough.
- Additional architecture work would likely be wasteful.
- The remaining work should stay targeted inside ranking logic, blueprint differentiation, and package fidelity.

### 4. Is Discovery Wizard good enough for Design Package consumption?

No.

- It is close enough for supervised internal review.
- It is not yet strong enough for trusted downstream package consumption.

### 5. Is Discovery Wizard good enough for Microsoft Skills integration planning?

No.

- Integration planning should wait until Design Package fidelity and lead recommendation trust are stable.

## Recommendation

**B. One Final Targeted Refinement**

Target the refinement narrowly at:

- lead recommendation judgment for service and analytical scenarios
- stronger intent preservation when variance and forecasting signals mix
- elimination of unsupported fallback KPI injection
- consultant-facing filter and dimension labels in Design Package output
- Design Package rationale field rendering and provider-grade wording cleanup

Do not broaden the scope into architecture work, provider execution, or new feature work.

## Final Determination

Discovery Wizard is not failing because it lacks architecture. It is failing because the last trust-critical layer still behaves unevenly under ambiguous scenarios and because the Design Package still leaks implementation-shaped artifacts. That means the remaining benchmark gaps are genuine product issues, but they are narrow enough that one final targeted refinement is the right move.

The MVP should not yet be called complete. It is, however, close enough that further architecture work would likely add complexity without solving the actual problem.
