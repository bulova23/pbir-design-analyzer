# Story Assessment 3.0 Cross-Page Narrative Level 1 Review

Date: 2026-06-12

Status: Internal Level 1 corpus review

## 1. Corpus Summary

The intended Level 1 target was a curated 12 to 20 report corpus spanning executive summary, operational monitoring, drill-heavy, weak or fragmented, and appendix or reference-heavy report families.

That corpus was not available locally.

This review used the available real PBIR corpus:

- Sales Analysis
  - 12 pages
  - best fit: comparative and operational analysis
- Sales & Production
  - 21 pages
  - best fit: drill-heavy analysis with multiple special, appendix-like, and fragmented pages

Total reviewed:

- 2 reports
- 33 pages

Coverage achieved:

- operational monitoring: partial
- drill-heavy: strong
- weak or fragmented: partial
- appendix or reference-heavy: partial through Legal, validation, tooltip, and Q&A pages

Coverage not achieved:

- clean executive summary report family
- standalone appendix or reference-heavy report family
- broader 12 to 20 report diversity target

Validation tooling limitation:

- the validation export CLI was invoked on both real reports
- both runs failed with a NullReferenceException before writing the Markdown or JSON review artifacts
- backend scoring still produced the internal Cross-Page Narrative assessment
- this review therefore used the same backend internal assessment through a temporary read-only inspector against the compiled assemblies so the corpus review could still complete without changing repository code

This limitation materially weakens promotion confidence because the official internal review workflow is not yet reliable on the available real corpus.

## 2. Per-Report Review

### Sales Analysis

Human read:

- likely report objective: comparative business analysis with some operational monitoring behavior
- expected entry page: Overview
- expected shape: overview into customer and order analysis, then comparative detail pages

System result:

- dominant report objective: comparative business analysis
- main narrative path: Overview -> Customer Analysis -> Order Detail -> Commissions -> P5 -> P6 -> P7 -> P8 -> P9 -> P10 -> P10 - Bonus Extra
- composite score: 80.5
- confidence: Medium
- dimensions:
  - Flow: 85, Medium
  - Consistency: 100, Medium
  - Continuity: 100, Medium
  - Navigation: 55, Medium
  - Actionability: 55, Medium

Role review:

- Overview was classified as DetailDrill rather than Overview or ExecutiveSummary
- Customer Analysis was classified as DetailDrill, which is plausible but still narrow
- Order Detail and Commissions as DetailDrill are plausible
- P5 through P10 and Bonus Extra as ComparativeAnalysis at Low confidence are directionally plausible
- Tooltip as Tooltip with advisory disconnection is correct

Flow review:

- the system found a coherent linear path
- that path broadly matches the page order and likely narrative progression
- the main miss is failure to recognize the opening Overview page as a report entry point rather than another drill page

Orphan and isolation review:

- only Tooltip was treated as an advisory disconnected special page
- this is appropriate
- no meaningful false orphaning occurred

Gap review:

- report-level output only surfaced MissingExecutiveEntryPoint
- this is likely a false positive because the report already has an Overview page and appears to start there intentionally

Overall judgment:

- useful but incomplete
- objective detection and pathing are mostly directionally correct
- role assignment under-detects entry and summary pages
- gap output is too blunt

### Sales & Production

Human read:

- likely report objective: diagnostic investigation with multiple drill branches and special analytical utilities
- expected shape: a mixed report with operational detail, drill pages, supporting tools, and several appendix-like or non-primary pages
- likely fragmented rather than tightly linear

System result:

- dominant report objective: diagnostic investigation
- main narrative path: CathegoryBreackdown -> KeyInfluencers -> StoreBreackdown -> NetSales -> WhatIf -> RetCategory -> RetKeyInf -> RetStoreBre
- composite score: 74.2
- confidence: Medium
- dimensions:
  - Flow: 60, Medium
  - Consistency: 100, Medium
  - Continuity: 100, Medium
  - Navigation: 55, Medium
  - Actionability: 55, Medium

Role review:

- Legal as ReferenceLegal is correct
- Validation Page and Duplicate of Validation Page as ValidationSandbox are correct
- Net Sales Tooltip and Returns Tooltip as Tooltip are correct
- Q&A1 and Q&A2 as Qna are correct
- Market Basket Analysis, KeyInfluencers, and WhatIf as SupportingContext are plausible
- most primary analytical pages were flattened into DetailDrill
- Intro was classified as DetailDrill, which is probably too narrow for an opening page

Flow review:

- the chosen main path captures one analytical branch in the returns and breakdown area
- it does not represent the full report structure well
- it skips the actual opening sequence as the main story and instead promotes a later sub-branch
- this suggests the graph currently overweights adjacency and role-compatible chains while underweighting literal entry pages

Orphan and isolation review:

- advisory disconnection on special pages is mostly correct
- the model did not over-penalize special pages
- the model also did not surface broader fragmentation despite multiple utility pages, duplicate validation pages, and weak naming continuity

Gap review:

- MissingExecutiveEntryPoint is a credible true positive for this report
- no fragmented-report or naming-layer gap was emitted even though the report shows signs that would justify at least one of those gaps

Overall judgment:

- strong precision on special-page handling
- weak recall on fragmentation, entry-page recognition, and naming continuity problems
- broadly useful as an internal structural read, but not ready for promotion

## 3. True Positives

- Sales & Production dominant objective as diagnostic investigation is directionally correct.
- Sales & Production special-page roles are strong:
  - Legal -> ReferenceLegal
  - Validation pages -> ValidationSandbox
  - tooltips -> Tooltip
  - Q&A pages -> Qna
- Advisory orphan handling for special pages is useful and appropriately conservative.
- Sales Analysis dominant objective as comparative business analysis is directionally correct.
- Sales Analysis tooltip handling is correct.
- Sales & Production MissingExecutiveEntryPoint appears to be a useful report-level gap.

## 4. False Positives

- Sales Analysis MissingExecutiveEntryPoint is likely false because the report already contains an Overview page that appears intended as the report entry point.
- Sales & Production Consistency score of 100 appears too high given visible naming drift:
  - CathegoryBreackdown
  - StoreBreackdown
  - Net Sales versus NetSales
  - Duplicate of Validation Page
- Sales & Production Continuity score of 100 appears too high for a report with multiple special-page interruptions and obvious branch fragmentation.
- Intro in Sales & Production as DetailDrill is likely an over-claim.

## 5. False Negatives

- Sales Analysis failed to recognize Overview as an overview or summary role.
- Neither report produced an overview or executive-summary role assignment.
- Sales & Production did not emit a fragmented-report or naming-consistency gap despite evidence that would support one.
- Sales & Production did not surface any stronger disconnected-analysis judgment beyond advisory special-page treatment.

## 6. Indeterminate Cases

- Sales Analysis pages P5 through P10 were classified as ComparativeAnalysis at Low confidence. That may be correct, but the current evidence set does not justify a harder judgment.
- Sales & Production classifications for WhatIf, KeyInfluencers, and Market Basket Analysis as SupportingContext are plausible but not definitive without rendered page review.
- Sales Analysis may also partially function as an operational monitoring report, so the dominant objective label is acceptable but not uniquely proven.

## 7. Role Accuracy Observations

- Special-page role precision is the strongest part of the current output.
- Entry-page and summary-page recall is weak.
- DetailDrill is overused as the default role for non-special analytical pages.
- ComparativeAnalysis appears only on obviously weakly named later pages, which suggests the classifier is relying too heavily on local cues and not enough on report-position semantics.
- The current taxonomy likely needs an explicit stronger bias for literal Overview and Intro pages unless contradictory evidence is strong.

## 8. Flow Accuracy Observations

- The graph builds deterministic paths consistently.
- The pathing is useful when the report is mostly linear.
- The current path selection appears too adjacency-driven.
- Entry pages are underweighted.
- Branch-heavy reports are compressed into one linear story even when the human read is more fragmented or hub-and-spoke.

## 9. Orphan Detection Observations

- The model correctly avoids harsh orphan penalties for tooltips, Q&A, legal, and validation pages.
- That conservative behavior is useful and should remain.
- The model is currently too reluctant to escalate from advisory disconnection to broader fragmentation or isolated-analysis findings.
- On the available corpus, orphan logic is safer than flow logic, but also less discriminative.

## 10. Report-Level Gap Usefulness

- The current report-level gap output is too sparse.
- MissingExecutiveEntryPoint is useful when correct.
- Repeating the same single gap across both reports reduces trust because it looks more like a fallback than a discriminative report diagnosis.
- The absence of fragmented-report, naming-layer, or bridge-related gaps on Sales & Production limits actionability.

## 11. Confidence Calibration Observations

- Overall Medium confidence on both reports is reasonable.
- Low confidence on weaker comparative pages in Sales Analysis is healthy.
- Perfect 100 scores on Consistency and Continuity do not match the visible ambiguity and naming drift in Sales & Production.
- Confidence is therefore better calibrated at the report-composite level than at the individual dimension level.

## 12. Recommended Tuning

1. Fix the validation export CLI before any further corpus review or promotion discussion. The internal review workflow is currently broken on real reports.
2. Bias role classification toward literal Overview and Intro pages as candidate entry roles unless the page has strong contradictory cues.
3. Reduce default fallback into DetailDrill for non-special analytical pages.
4. Penalize naming drift and duplicate sandbox or utility pages more directly in Consistency and Continuity scoring.
5. Require stronger evidence before assigning 100 to Consistency or Continuity on branch-heavy reports.
6. Treat special-page chains as weak support for continuity rather than strong narrative glue.
7. Add a more discriminative fragmented-report or disconnected-analysis gap when the report contains multiple special or utility islands without a clear entry-to-detail bridge.
8. Keep report-level gaps internal-only until the gap set becomes more discriminative than the current single-gap fallback.

## 13. Promotion Recommendation

Recommendation:

- no public contract promotion
- no UI exposure
- keep page roles internal
- keep narrative score internal
- keep graph and main narrative path internal
- keep dominant report objective internal
- keep report-level gaps internal

Rationale:

- the corpus is far smaller than the intended review set
- the official validation export workflow currently fails on both available real reports
- special-page handling is promising, but role, flow, and gap outputs are not yet stable enough for even a narrow promotion slice
- report-level gaps are not yet consistently useful enough to repeat the Story Assessment 2.0 limited-promotion pattern

Promotion stance remains:

- do not promote public fields
- do not expose the outputs in the workspace UI
- revisit only after:
  - the export tooling is reliable on real reports
  - a broader 12 to 20 report corpus is reviewed
  - role recall and gap discrimination improve materially
