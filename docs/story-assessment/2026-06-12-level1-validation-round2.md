# Story Assessment And Cross-Page Narrative Level 1 Validation Round 2

Date: 2026-06-12

Status: Internal Level 1 validation round using the official validation export harness

## 1. Scope

This round used the official Story Assessment validation export harness across the requested real PBIR corpus:

- PBITesting
  - Sales & Production
- PBITest2
  - Sales Analysis
- PBITest3
  - Running Record Dataverse
- PBITest4
  - Sales AWF

Round 2 goals:

- expand beyond the original two-report baseline
- validate the official export path on a broader real corpus
- re-check Story Assessment promotion posture
- re-check Cross-Page Narrative promotion posture
- compare new results against Sales Analysis and Sales & Production

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
  - sales operations workbook with many supporting tools and tooltip pages

Total reviewed:

- 4 reports
- 77 pages

Coverage improved relative to the first cross-page review:

- operational monitoring: improved
- drill-heavy analysis: still strong
- weak or fragmented reports: improved
- appendix or reference-heavy patterns: improved
- tooltip-heavy report families: materially improved
- education or program-tracking patterns: newly covered

Coverage still missing:

- a clean executive-summary-first report family with strong entry-page semantics
- broader cross-team naming conventions
- a larger 12 to 20 report diversity set

## 3. Official Harness Status

The official Story Assessment validation export harness ran successfully on all four reports and wrote JSON and Markdown artifacts for each corpus member.

This is a material improvement from the earlier Cross-Page Narrative review, where the official export path failed on real reports.

However, Round 2 also exposed an important remaining harness limitation:

- Cross-Page Narrative dominant report objective exported successfully
- orphan decisions exported successfully
- report-level narrative gaps exported successfully
- page roles exported as Unavailable placeholders on all four reports
- main narrative path exported as a missing-data placeholder on all four reports
- narrative dimension scores exported as Unavailable placeholders on all four reports

This means the official harness is now reliable enough to complete the workflow, but it is not yet faithful enough to support a full expert review of Cross-Page Narrative role and flow quality through the exported artifacts alone.

That limitation matters for this round:

- Story Assessment review is mostly possible through the official artifacts
- Cross-Page Narrative objective, orphan handling, and gap review are possible
- Cross-Page Narrative page role accuracy and narrative flow accuracy are not directly reviewable through the current official export output

## 4. Story Assessment Round 2 Review

### 4.1 Directly Observable In The Official Harness

Directly observable:

- Story Type output
- internal signal registry
- internal gap output
- internal confidence breakdown
- special-page suppression behavior

Indirectly observable only:

- Missing Signals usefulness
  - inferred from signal registry and gap output
- Top Story Improvements usefulness
  - inferred from repeated high-value future-contract-candidate gaps

Not directly observable in the official harness:

- Story Maturity
- Deep Link target quality
- Diff Mode usefulness

Those three remain extension-owned public workflows, not export-harness artifacts. They cannot be fully validated in a harness-only round without stepping outside the requested scope.

### 4.2 Story Type Accuracy

Overall judgment:

- directionally useful
- strongest on obvious summary, exploratory, and no-public-story cases
- still too generic on many tooltip, drill, and utility pages

Observed strengths:

- Sales Analysis Overview as a monitoring page remains directionally correct
- Sales & Production Legal as no public story remains correct
- Sales & Production Net Sales and Returns as summary-style pages remain plausible
- Running Record Dataverse repeatedly reads as exploratory analysis, which is directionally plausible for class, teacher, and at-risk drill pages
- Sales AWF summary pages such as Sales KPI, Sales, Customer Overview, and Elapsed Time are directionally plausible
- Notes in Sales AWF as no public story is directionally correct

Observed weaknesses:

- tooltips are still often assigned a public story instead of staying closer to no-public-story or bounded contextual support
- many breakdown pages collapse into the same generic comparison language even when they likely serve different operational roles
- exploratory wording overfires on some duplicate, utility, and sandbox-adjacent pages
- the model still depends heavily on generic page-name cues rather than clearer page-position or workflow cues

### 4.3 Story Maturity Usefulness

Round 2 conclusion:

- not directly reviewable through the official harness
- promotion confidence cannot increase from this round on Story Maturity alone

Reason:

- the official export does not emit the public Story Maturity field
- the internal proxies suggest many pages still sit in low-confidence territory
- especially on exploratory and drill-heavy reports, the public maturity message would still need direct UI review rather than export-only inference

### 4.4 Missing Signals Usefulness

Round 2 conclusion:

- useful as an internal and public advisory concept
- still repetitive
- still strongest when kept narrow

Repeated high-value signal misses across the four-report corpus:

- missing prior-period context
- missing benchmark or target
- missing visible title or question anchor
- missing primary dimension
- missing primary metric
- scattered filters

Strengths:

- the same six categories recur across very different reports
- the repeated categories still map to visible authoring changes
- they remain explainable without exposing internal evidence machinery

Weaknesses:

- signal repetition is so strong that some pages begin to look templated rather than individually diagnosed
- benchmark and prior-period gaps fire on many exploratory and utility-heavy pages where the recommendation may be only partly useful
- model-layer metric and dimension gaps remain useful internally, but some instances still depend too much on metadata quality rather than obvious report-author intent

### 4.5 Top Story Improvements Usefulness

Round 2 conclusion:

- the underlying recommendation categories remain stable
- the usefulness ceiling is still bounded by repetition and page-context sensitivity

Evidence:

- the future-contract-candidate set stayed narrow and consistent across the expanded corpus
- no large new category family displaced the validated six-gap slice
- scattered filters remained less frequent than the other five categories, but it still appeared in Running Record Dataverse and Sales AWF, which supports keeping it in the narrow set

Interpretation:

- Guided Story Improvements remains stable enough as the current narrow public slice
- it is not yet strong evidence for broadening Story Assessment promotion beyond the existing six categories

### 4.6 Deep Link Target Quality

Round 2 conclusion:

- not reviewable in the official export harness

Reason:

- the export artifacts do not expose public navigation targets
- no target-resolution evidence appears in the JSON or Markdown output

This round therefore cannot raise or lower promotion confidence for Deep Link Navigation.

### 4.7 Diff Mode Usefulness

Round 2 conclusion:

- not reviewable in the official export harness

Reason:

- Diff Mode is snapshot-driven extension behavior
- the export harness has no What Changed or snapshot comparison output

This round therefore cannot raise or lower promotion confidence for Diff Mode.

## 5. Cross-Page Narrative Round 2 Review

### 5.1 Directly Observable In The Official Harness

Directly observable:

- dominant report objective
- orphan decisions
- report-level gap output

Not directly reviewable from the current official artifacts:

- page role accuracy
- narrative flow accuracy
- narrative dimension scoring quality

### 5.2 Dominant Report Objective Accuracy

Overall judgment:

- directionally useful
- better than the original two-report sample would suggest
- still too coarse for promotion

Per report:

- Sales Analysis
  - comparative business analysis remains directionally correct
- Sales & Production
  - diagnostic investigation remains directionally correct
- Running Record Dataverse
  - diagnostic investigation is plausible and directionally useful
- Sales AWF
  - diagnostic investigation is plausible given the breadth of operational utility, tooltip, and drill pages

Net conclusion:

- report-level objective detection looks more stable than role and gap output
- it is still not promotable because the official harness does not yet let reviewers pair the objective cleanly with exported roles and flow evidence

### 5.3 Page Role Accuracy

Round 2 conclusion:

- not directly reviewable through the official harness

Reason:

- all four reports exported Unavailable role placeholders for every page

This is not a model-quality conclusion. It is an observability limitation in the official validation workflow as currently exported.

### 5.4 Narrative Flow Accuracy

Round 2 conclusion:

- not directly reviewable through the official harness

Reason:

- all four reports exported the main narrative path as a missing-data placeholder
- narrative dimension scores also collapsed to placeholders

This prevents a defensible Round 2 judgment about whether graph pathing has materially improved beyond the earlier Sales Analysis and Sales & Production review.

### 5.5 Orphan Detection Accuracy

Overall judgment:

- still the strongest observable Cross-Page Narrative behavior
- precision on obvious special pages remains good
- escalation beyond advisory disconnection remains weak

Observed strengths:

- Sales & Production continues to mark Legal, validation pages, tooltips, and Q&A pages conservatively
- Running Record Dataverse marks By Class ToolTip and Duplicate of By Class as advisory disconnected special pages, which is directionally plausible
- Sales AWF marks Notes as advisory-safe through special-page handling and keeps the many tooltip pages out of harsher orphan judgments

Observed weaknesses:

- the model still treats many fragmented or utility-heavy report shapes as broadly connected
- it remains reluctant to escalate toward stronger disconnected or fragmented narrative findings
- the larger tooltip-heavy Sales AWF workbook especially shows that conservative orphan handling may now be too forgiving

### 5.6 Report-Level Gap Usefulness

Round 2 conclusion:

- not improving enough
- not contract-eligible

Observed output:

- all four reports exported the same single report-level gap:
  - MissingExecutiveEntryPoint

Usefulness by report:

- Sales Analysis
  - likely false positive, as in the earlier review
- Sales & Production
  - still plausible
- Running Record Dataverse
  - directionally plausible but still blunt
- Sales AWF
  - likely too blunt and possibly false positive because Sales KPI and Sales appear to function as entry surfaces

Interpretation:

- the repeated single-gap behavior reduces trust
- the category is not discriminative enough across very different report families
- no report-level gap category became more contract-eligible in Round 2

## 6. New Findings From The Expanded Corpus

### 6.1 Newly Discovered Page Types

Newly observed in this expanded real corpus:

- Customer Segmentation Diagnostic
  - observed on Accts Receivable in Sales AWF

Reconfirmed across a broader corpus:

- Tooltip
- Validation Sandbox
- Reference Legal
- Key Influencers
- What If
- Q&A
- Market Basket

Promotion posture:

- keep all of these internal
- continue using them as suppression and downgrade guardrails rather than public product semantics

### 6.2 Newly Discovered Report Patterns

New report patterns now covered:

- education at-risk monitoring with class, teacher, grade, and time-slicer exploration
- tooltip-heavy sales operations workbook with numerous support pages
- duplicate or sandbox-adjacent validation pages outside the original sales corpus
- customer and receivables diagnostic pages inside a broader sales operations report

Why this matters:

- these new families are useful for pressure-testing whether Story Assessment stays narrow and actionable outside the original sales-heavy sample
- they also show that Cross-Page Narrative still needs stronger discrimination on utility-heavy and workflow-heavy reports

## 7. True Positives

- The official validation export workflow now succeeds on all four real reports.
- Story Assessment continues to recognize many obvious summary, exploratory, and no-public-story cases directionally correctly.
- The six validated Guided Story Improvements categories remained stable across the broader corpus.
- Dominant report objective remained directionally useful across all four reports.
- Special-page suppression remains the strongest current hidden guardrail.
- Orphan handling remains conservative and generally safe on tooltips, validation pages, notes, legal pages, and Q&A pages.
- Customer Segmentation Diagnostic appeared as a plausible new internal-only page type on Accts Receivable.

## 8. False Positives

- MissingExecutiveEntryPoint continues to overfire and now appears too blunt across the broader corpus.
- Some tooltip pages still receive public story language that is too assertive for bounded support surfaces.
- Notes in Sales AWF appears over-labeled as Reference Legal rather than a broader appendix or notes pattern.
- Duplicate and utility-adjacent pages can still inherit exploratory story language too easily.
- The repeated benchmark and prior-period gaps likely overfire on some exploratory drill pages.

## 9. False Negatives

- Cross-Page Narrative still fails the official workflow as a review surface for page roles and flow because those fields export as placeholders.
- No stronger fragmented-report or disconnected-analysis gap emerged on Sales AWF despite a heavily utility-driven report shape.
- No new report-level gap category emerged even after expanding to four reports and 77 pages.
- Entry-page recognition remains weak at the report level because the only repeated report gap still assumes missing entry structure too often.

## 10. Promotion Candidates

Promotion candidates remain unchanged from the earlier Story Assessment promotion decision:

- missing title or question anchor
- missing benchmark or target
- missing prior-period context
- missing primary metric
- missing primary dimension
- scattered filters

Why they still qualify:

- they recurred across very different report families
- they remain visible and author-actionable
- they can still be expressed safely without exposing internal diagnostics

Why promotion should still stay narrow:

- the categories are stable, but also repetitive
- the broader corpus did not yet prove a larger Story Assessment public surface

## 11. Signals That Should Remain Internal

Keep internal:

- raw signal registry
- special-page labels
- archetype classifications
- semantic coherence details
- competing-story diagnostics
- confidence breakdown details
- evidence references
- Cross-Page Narrative page roles
- Cross-Page Narrative graph and main narrative path
- Cross-Page Narrative report objective
- Cross-Page Narrative report-level gaps

Reasons:

- several remain unstable or overly generic
- some are not exported faithfully enough for reliable expert review
- many are still diagnostic infrastructure rather than product-ready explanations

## 12. Comparison Against Sales Analysis And Sales & Production

### Guided Story Improvements Stability

Round 2 answer:

- yes, the current narrow Guided Story Improvements slice looks stable

Evidence:

- the same six candidate categories persisted across all four reports
- the expanded corpus did not surface a clearly stronger replacement category
- special-page suppression still helps protect the slice from the worst overclaims

Constraint:

- stable does not mean ready to widen
- the slice looks stable enough to keep, not stable enough to broaden

### Cross-Page Narrative Improvement

Round 2 answer:

- improving in workflow reliability
- not yet improving enough in promotion readiness

Improvements:

- the official harness now runs successfully on real reports

Limits:

- page roles are not reviewable in the official export
- flow is not reviewable in the official export
- the only repeated report-level gap remains too blunt

Net judgment:

- reliability improved
- observability and discriminative usefulness remain below promotion quality

### Report-Level Gap Contract Eligibility

Round 2 answer:

- no report-level gap category is becoming contract-eligible

Reason:

- MissingExecutiveEntryPoint remains the only exported report-level gap
- it is still not discriminative enough
- it still produces likely false positives

## 13. Promotion-Readiness Update

### Story Assessment

Promotion posture:

- keep the current Guided Story Improvements slice
- do not broaden Story Assessment public exposure from this round

What this round increases confidence in:

- the narrow six-category Guided Story Improvements slice
- continued use of special-page suppression as a hidden guardrail

What this round does not increase confidence in:

- Story Maturity promotion decisions
- Deep Link target quality
- Diff Mode usefulness
- broader Story Assessment classification exposure

### Cross-Page Narrative

Promotion posture:

- remain fully internal
- no public contract promotion
- no UI exposure

What improved:

- official workflow reliability on real reports

What blocks promotion:

- role and flow outputs are not reviewable through the official export artifacts
- report-level gaps remain too blunt
- orphan handling remains safer than it is discriminative

## 14. Final Judgment

Story Assessment Round 2:

- Guided Story Improvements are stable enough to keep as the current narrow public slice
- Story Assessment should not widen beyond that slice based on this round

Cross-Page Narrative Round 2:

- the official workflow is now reliable enough to run
- Cross-Page Narrative is not yet ready for promotion
- the current official export still does not support a full expert review of page role and flow quality

Report-level gap promotion:

- no report-level gap category became contract-eligible in this round

## 15. Recommended Next Step

Next recommended step:

1. keep Guided Story Improvements constrained to the current six-category public slice
2. keep all Cross-Page Narrative outputs internal
3. fix the remaining official export observability gaps for Cross-Page Narrative roles, pathing, and dimension scoring before the next validation round
4. rerun a broader Level 1 corpus only after the official export can faithfully surface the narrative fields required for expert review
