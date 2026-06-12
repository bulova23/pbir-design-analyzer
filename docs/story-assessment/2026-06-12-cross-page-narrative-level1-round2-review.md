# Cross-Page Narrative Level 1 Round 2 Review After Official Export Fix

Date: 2026-06-12

Status: Internal validation rerun using the fixed official export only

## 1. Scope

This review reran Cross-Page Narrative Level 1 Round 2 against the fixed official validation export output only.

Corpus reviewed:

- Sales & Production
- Sales Analysis
- Running Record Dataverse
- Sales AWF

Evaluation areas:

1. page role accuracy
2. main narrative path accuracy
3. dominant report objective accuracy
4. narrative dimension usefulness
5. orphan or isolation detection
6. report-level gap usefulness
7. false positives
8. false negatives
9. indeterminate cases
10. promotion readiness

This session did not modify product code.

## 2. Corpus Summary

Reports reviewed:

- Sales Analysis
  - 12 pages
  - comparative analysis with some monitoring behavior
- Sales & Production
  - 21 pages
  - drill-heavy diagnostic and utility-heavy report
- Running Record Dataverse
  - 15 pages
  - education monitoring and at-risk exploration workflow
- Sales AWF
  - 29 pages
  - sales operations workbook with many utility, tooltip, and support pages

Total reviewed:

- 4 reports
- 77 pages

Role-shape summary from the fixed official export:

- Sales Analysis
  - 6 ComparativeAnalysis
  - 5 DetailDrill
  - 1 Tooltip
- Sales & Production
  - 11 DetailDrill
  - 3 SupportingContext
  - 2 Tooltip
  - 2 Qna
  - 2 ValidationSandbox
  - 1 ReferenceLegal
- Running Record Dataverse
  - 13 DetailDrill
  - 1 Tooltip
  - 1 ValidationSandbox
- Sales AWF
  - 16 DetailDrill
  - 10 Tooltip
  - 2 SupportingContext
  - 1 ReferenceLegal

## 3. What Improved After The Export Fix

The previous Round 2 limitation is resolved at the workflow level.

Before the fix, the official export completed but left three Cross-Page Narrative areas effectively unreviewable:

- page roles exported as placeholders
- main narrative path exported as a missing-data placeholder
- narrative dimension scores exported as placeholders

After the fix, all four official exports now include concrete values for:

- page roles with confidence
- readable main narrative paths using page names
- narrative dimension ids, scores, and confidence

Conclusion:

- page roles are now reviewable
- narrative path is now reviewable
- narrative dimensions are now reviewable

That is a real improvement in observability. It does not, by itself, establish quality or promotion readiness.

## 4. Per-Report Findings

### Sales Analysis

Dominant report objective:

- comparative business analysis is directionally correct

Role findings:

- Tooltip as Tooltip is correct
- Overview as DetailDrill is likely wrong
- Customer Analysis as DetailDrill is plausible but narrower than expected
- P5, P6, P7, P9, P10, and P10 - Bonus Extra as ComparativeAnalysis at Low confidence are directionally plausible

Flow findings:

- the main path is readable and broadly matches a plausible report progression
- the path still under-recognizes Overview as a real entry page rather than a drill page

Dimension findings:

- Flow 85 is directionally plausible because this is the cleanest of the four visible paths
- Consistency 100 and Continuity 100 still feel too perfect given the weakly named later pages
- Navigation 55 and Actionability 55 are too generic to teach much

Gap and orphan findings:

- Tooltip as AdvisoryDisconnectedSpecialPage is useful
- MissingExecutiveEntryPoint still appears false because the report already starts with Overview

Overall judgment:

- newly reviewable and moderately useful
- still not promotion-ready

### Sales & Production

Dominant report objective:

- diagnostic investigation is directionally correct

Role findings:

- Legal, Validation Page, Duplicate of Validation Page, tooltips, and Q&A pages are strong true positives
- Market Basket Analysis, KeyInfluencers, and WhatIf as SupportingContext are plausible
- Intro as DetailDrill is likely wrong
- primary analytic pages are still flattened too heavily into DetailDrill

Flow findings:

- the main path is now inspectable and confirms the earlier concern
- it selects one later branch as the main story rather than the actual opening sequence
- this makes the path reviewable, but not accurate enough for promotion

Dimension findings:

- Flow 60 is plausible
- Consistency 100 and Continuity 100 remain too high for a report with naming drift, duplicate validation pages, and branch fragmentation
- Navigation 55 and Actionability 55 again read as generic defaults rather than report-specific judgments

Gap and orphan findings:

- advisory disconnected handling for Legal, Validation, tooltips, Q&A, and duplicate validation is conservative and useful
- MissingExecutiveEntryPoint is a credible true positive here
- there is still no stronger fragmented-report or naming-consistency gap despite visible evidence for one

Overall judgment:

- special-page precision remains the strongest behavior
- report-structure recall remains weak

### Running Record Dataverse

Dominant report objective:

- diagnostic investigation is plausible, though the report also has monitoring behavior

Role findings:

- By Class ToolTip as Tooltip is correct
- Duplicate of By Class as ValidationSandbox is directionally acceptable as special handling
- thirteen of fifteen pages as DetailDrill is too collapsed
- By Class, By Teacher, and At-Risk Program Tracker likely deserve more differentiated roles than generic DetailDrill

Flow findings:

- the main path is readable and at least covers the visible core pages
- it feels more like a traversal of similarly weighted analytic pages than a deliberately identified narrative spine

Dimension findings:

- Flow 60 is plausible
- Consistency 100 and Continuity 100 appear overstated because the report spans class, scholar, teacher, growth, and at-risk workflows rather than one seamless story
- Navigation 55 and Actionability 55 are again minimally discriminative

Gap and orphan findings:

- orphan handling is appropriately conservative on the tooltip and duplicate page
- MissingExecutiveEntryPoint is plausible
- no broader fragmentation or workflow-switching gap appears even though the report mixes several exploration modes

Overall judgment:

- reviewability improved materially
- differentiation remains weak

### Sales AWF

Dominant report objective:

- diagnostic investigation is plausible but too coarse for such a utility-heavy workbook

Role findings:

- tooltip handling is strong and consistent
- Accts Receivable as SupportingContext is plausible
- HotList Defined as SupportingContext at Low confidence is acceptable but weak
- Notes as ReferenceLegal appears wrong or at least over-specific
- Sales KPI, Sales, Customer Overview, and Elapsed Time being flattened into DetailDrill misses likely summary or monitoring intent

Flow findings:

- the main path is now readable and clearly reviewable
- it captures one long chain through core workbook pages
- it still looks overly linear for a report family with many side utilities and support pages

Dimension findings:

- Flow 60 is plausible
- Consistency 100 and Continuity 100 are not credible for a workbook with many tooltips, utility pages, notes, and mixed operational tasks
- Navigation 55 and Actionability 55 again do not add much report-specific insight

Gap and orphan findings:

- advisory disconnection on the many tooltip pages is a real strength
- Notes as AdvisoryDisconnectedSpecialPage is only partly useful because the role itself likely overfires
- MissingExecutiveEntryPoint is plausible
- the absence of a stronger disconnected-analysis or utility-heavy fragmentation gap is a notable miss

Overall judgment:

- good special-page handling
- weak high-level narrative discrimination

## 5. Role Accuracy Observations

Strongest role behavior:

- Tooltip classification
- ValidationSandbox classification
- Qna classification
- ReferenceLegal on clearly legal material
- some SupportingContext calls on explicitly auxiliary analytic pages

Weakest role behavior:

- Overview and Intro style entry pages are under-detected
- summary or monitoring pages are repeatedly flattened into DetailDrill
- Running Record Dataverse shows near-total collapse into DetailDrill
- Notes in Sales AWF shows that the special-page taxonomy can still over-specify the wrong kind of special page

Overall role conclusion:

- special-page precision is useful
- primary-page role recall is not strong enough for exposure

## 6. Flow Accuracy Observations

What improved:

- the main narrative path is now directly inspectable in the official export
- this alone removes the biggest prior review blocker

What the paths show:

- Sales Analysis has the most credible main path
- Sales & Production confirms the model overweights one mid-report branch
- Running Record Dataverse reads more like sequence traversal than a strong narrative spine
- Sales AWF is compressed into one long linear chain despite obvious workbook sprawl

Overall flow conclusion:

- path review is now possible
- path quality is still only directionally useful
- entry-page weighting and branch-awareness still look underpowered

## 7. Narrative Dimension Observations

The dimensions are now visible, but their usefulness is limited.

Observed pattern:

- Flow varies by report
  - 85 on Sales Analysis
  - 60 on the other three
- Consistency is 100 on all four reports
- Continuity is 100 on all four reports
- Navigation is 55 on all four reports
- Actionability is 55 on all four reports

Interpretation:

- Flow is somewhat discriminative
- the other four dimensions currently look too templated to support strong reviewer confidence
- identical 100 or 55 outputs across very different report families reduce trust that the dimensions are measuring enough report-specific evidence

Overall dimension conclusion:

- dimension scores are now reviewable
- only Flow currently looks meaningfully informative
- the dimension block is not strong enough for promotion or UI exposure

## 8. Gap Usefulness Observations

Observed pattern:

- every report emitted the same report-level gap:
  - MissingExecutiveEntryPoint

Usefulness judgment:

- credible on Sales & Production
- plausible on Running Record Dataverse
- plausible on Sales AWF
- likely false on Sales Analysis

Interpretation:

- the gap is understandable and sometimes useful
- it is not yet low-noise enough to support promotion
- no second report-level gap type emerged consistently enough to strengthen confidence

Overall gap conclusion:

- report-level gaps should remain internal
- they may become candidates later, but current evidence is not strong enough

## 9. False Positives

- Sales Analysis MissingExecutiveEntryPoint is likely false.
- Sales Analysis Overview as DetailDrill is likely false.
- Sales & Production Intro as DetailDrill is likely false.
- Sales AWF Notes as ReferenceLegal is likely false or too specific.
- Consistency 100 on all four reports appears overstated.
- Continuity 100 on all four reports appears overstated.

## 10. False Negatives

- overview or executive-summary recognition remains weak across the corpus
- summary or monitoring pages are often not promoted above DetailDrill
- Sales & Production still misses a stronger fragmentation or naming-consistency gap
- Running Record Dataverse misses more differentiated workflow-role labeling
- Sales AWF misses a stronger disconnected-analysis or utility-heavy fragmentation signal

## 11. Indeterminate Cases

- Sales Analysis later pages marked ComparativeAnalysis at Low confidence are plausible but not proven
- Sales & Production SupportingContext labels for Market Basket Analysis, KeyInfluencers, and WhatIf are plausible but not definitive
- Running Record Dataverse dominant objective could reasonably be read as mixed monitoring plus diagnostic exploration
- Sales AWF dominant objective is plausible but too coarse to be uniquely convincing

## 12. Promotion Readiness

Promotion recommendation:

- do not promote any Cross-Page Narrative public contract fields

Reasoning:

- the official export fix solved observability, not promotion quality
- page roles are now reviewable, but primary-page role recall is still weak
- main narrative paths are now reviewable, but path quality is only directionally useful
- dimension scores are now reviewable, but four of five dimensions still look too templated
- report-level gaps are understandable, but still too repetitive and not yet low-noise

Boundary recommendation:

- keep page roles internal
- keep graph and path outputs internal
- keep dimension scores internal
- keep composite Cross-Page Narrative scores internal
- keep report-level gaps internal for now

## 13. Remaining Tuning Recommendations

1. Improve entry-page and overview recognition before any promotion discussion.
2. Reduce DetailDrill overuse on primary narrative pages.
3. Make path selection weigh literal entry pages and branch structure more heavily.
4. Recalibrate Consistency and Continuity so they can fall below perfect scores on fragmented or naming-drift-heavy reports.
5. Make Navigation and Actionability more report-specific, or keep them hidden until they are.
6. Add stronger report-level gap recall for fragmentation, disconnected-analysis, or naming-consistency patterns when evidence is obvious.
7. Revisit special-page over-specificity so pages like Notes do not collapse into the wrong special subtype.
8. Re-run the same corpus plus a cleaner executive-summary-first report family before reconsidering any contract exposure.

## 14. Final Recommendation

The fixed official export is now sufficient for real Cross-Page Narrative expert review on this corpus. That is the main success of this rerun.

The substantive product recommendation does not change:

- no public contract promotion
- no UI exposure
- keep Cross-Page Narrative roles, graph, dimensions, scores, and report-level gaps internal

Evidence is now strong enough to say the review surface works.
Evidence is not strong enough to say the underlying Cross-Page Narrative outputs are promotion-ready.
