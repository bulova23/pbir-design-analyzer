# Report Design Studio Manual Smoke Test

Date: 2026-06-13

## Scope

This review covered Report Design Studio Tasks 1-10 with a workflow and UX lens only.

Out of scope:

- code changes
- architecture changes
- feature additions

## Method

The current repository does not expose a unified Report Design Studio entrypoint in the shipped extension surface. The review therefore used the narrowest available real implementation paths:

- extension-side Design Studio state, materialization, handoff, refinement, and closed-loop workflow execution through existing stores and coordinators
- isolated webview component behavior for Design Brief, Concept Studio, Draft Studio, and Closed Loop
- extension command and activation inspection to confirm what is or is not currently user-accessible
- focused backend Design Studio boundary tests for trust-boundary confirmation

## Validation

- `cd vscode-extension && npx jest --runTestsByPath src/test/designBriefStore.test.ts src/test/conceptStore.test.ts src/test/draftStore.test.ts src/test/materializationCoordinator.test.ts src/test/analyzerHandoffService.test.ts src/test/refinementStore.test.ts src/test/iterationStore.test.ts`
  - passed: 45 tests
- `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/design-studio/__tests__/DesignBriefView.test.tsx webview-src/design-studio/__tests__/ConceptStudioView.test.tsx webview-src/design-studio/__tests__/ClosedLoopView.test.tsx`
  - passed: 6 tests
- `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~DesignStudio`
  - passed: 19 tests

## Workflow Walkthrough

### 1. Design Brief

Observed behavior:

- The brief supports the required core fields plus optional context and risk fields.
- Concept generation remains blocked until the brief is valid and approved.
- The webview shows a flat form with validation messages and three actions: Save Brief, Request Approval, Generate Concepts.

UX assessment:

- The gating is correct.
- The form is not easy to scan because required and optional fields are mixed together with no grouping, no helper text, and no indication of which fields are mandatory.
- The approval transition is technically present but not explained well enough for first-time users.

### 2. Concept Studio

Observed behavior:

- Concepts generate only after Design Brief approval.
- Alternate concepts, baseline selection, and explicit approval for Draft Studio are present.
- The concept model contains rich internal data: chapter map, page recommendations, KPI hierarchy, navigation structure, analytical flow, and page concepts.
- The visible UI shows only a summary, preferred baseline label, approval status, and concept names.

UX assessment:

- The workflow is coherent.
- The visible output is too thin for consultant review because the detailed concept structure is mostly hidden.
- Baseline comparison exists in contract/state terms more than in a practical review experience.

### 3. Approved Concept -> Draft Studio

Observed behavior:

- Draft generation remains blocked until both the Design Brief and the Concept baseline are approved.
- Draft artifacts preserve lineage and versioning correctly.
- Provider participation is optional and advisory-only.
- The visible Draft Studio UI shows draft counts, one summary line, and provider capability names.

UX assessment:

- The approval gate is correct.
- Drafts do not yet feel reviewable in the UI because there is no artifact preview, no page-by-page inspection, and no visible lineage context.
- Approval is present in workflow state but not surfaced clearly in the current view.

### 4. Approved Draft -> Materialized Candidate

Observed behavior:

- Materialization can be created only from an approved draft version.
- Diagnostics and lineage are preserved.
- Executable eligibility is correctly separated from approval.
- No dedicated Materialization UI view is present in the current webview surface.

UX assessment:

- The trust model is strong.
- The user-facing workflow is incomplete because the user cannot inspect candidate readiness, diagnostics, or executable-vs-preview-only status in a dedicated experience.

### 5. Analyzer Handoff -> Analyzer Workspace

Observed behavior:

- Handoff eligibility rules are clear in implementation.
- Repository-backed candidates can open Analyzer Workspace.
- Snapshot-backed and synthetic preview candidates are blocked correctly.
- The current launch shell reuses the analyzer panel and presents the handoff as an error-style message instructing the user to run Retry.

UX assessment:

- Ownership separation is preserved.
- The handoff does not feel natural. It reads like a technical fallback instead of a designed transition.
- The “Run Retry” instruction is likely to confuse users about whether the workflow succeeded or failed.

### 6. Analyzer Workspace -> Refinement Proposal

Observed behavior:

- Refinement proposals can be ingested from Story Assessment, Guided Story Improvements, Issues, Fix Plan, and Cross-Page Narrative outputs.
- Proposal lineage, affected artifacts, and no-mutation guarantees are preserved.
- Explicit proposal review, approval, and rejection transitions exist.
- No dedicated Refinement Studio UI view is currently present.

UX assessment:

- The backend and extension workflow is coherent.
- The actual refinement experience is not currently understandable from the product surface because proposals are not exposed in a first-class review UI.

### 7. Closed Loop Comparison

Observed behavior:

- Iteration lineage, analyzer linkage, refinement linkage, and validation approval checkpoints are all preserved.
- Comparison can show concept, draft, analyzer-output, recommendation, and validation-status changes.
- The current UI lists lineage items and renders comparison change strings.

UX assessment:

- The workflow model is coherent.
- Before/after understanding is only partial because the UI is mostly textual and does not show side-by-side artifact state.
- Approval checkpoints are present but not prominent enough as separate decisions.

## UX Observations

### Design Brief UX

- The brief is not easy to complete quickly because the form is long, flat, and unlabeled by section.
- `Consumption Context`, `Decision Cadence`, `Narrative Risks Or Constraints`, `Required Evidence Domains`, and `Target Analyzable Surface Family` are useful but currently read like expert-only fields.
- No fields appear obviously redundant in the model, but several feel premature in the current UI because they lack explanation.
- Missing from the current experience:
  - explicit required/optional labeling
  - helper text
  - progress/state explanation
  - clear approval rationale

### Concept Studio UX

- Concept outputs are rich in the model but underrepresented in the UI.
- Consultant usefulness is limited because chapter maps, KPI hierarchy, navigation structure, and analytical flow are not rendered as review artifacts.
- Concept comparison is directionally useful, but only at summary level.
- Concept approval is technically obvious, but the difference between selecting a preferred baseline and approving it for Draft Studio could still be missed.

### Draft Studio UX

- Drafts do not yet feel useful enough for hands-on review because the visible artifacts are counts, not designs.
- Lineage exists, but the UI does not help users understand what draft page, layout, and navigation artifacts actually mean.
- Draft approval semantics are mostly hidden from the visible workflow.

### Materialization UX

- There is no real user-facing Materialization review surface yet.
- As a result, executable vs preview-only status is not understandable in-product even though the underlying contract is correct.
- Diagnostics are implementation-grade rather than consultant-grade.

### Analyzer Handoff UX

- The peer-workflow separation is architecturally clean.
- The handoff message is not user-friendly because it appears as an error-like state inside Analyzer Workspace.
- The next step is not framed as a normal continuation of the workflow.

### Refinement Studio UX

- Proposal generation and provenance are strong in state.
- Proposal clarity, rationale, and expected impact cannot be meaningfully assessed as UX because the current implementation has no dedicated Refinement Studio view.
- The workflow connection back to analyzer results is present structurally but not visible enough to a user.

### Closed Loop UX

- Iteration history is understandable at a data level.
- The current comparison view is adequate for regression proof, not for consultant-grade before/after review.
- Approval checkpoints are present but easy to overlook.

## Confusing Areas

- The repository presents Tasks 1-10 as complete, but the current extension does not expose a unified Report Design Studio workflow entrypoint.
- The distinction between selection and approval is clear in state but only partly clear in UI.
- Materialization approval and validation approval are well separated architecturally, but there is not enough visible product affordance to teach that distinction.
- Analyzer handoff reads like an exception flow instead of a workflow handoff.

## Friction Points

- Long flat Design Brief form with no grouping.
- Concept review lacks visible artifact detail.
- Draft review lacks artifact inspection.
- No dedicated Materialization step UI.
- No dedicated Refinement Studio UI.
- Closed Loop comparison is text-heavy and low-context.

## Trust-Boundary Observations

What is working well:

- Design approval, refinement approval, materialization approval, and validation approval are distinct in the model and tests.
- Materialization remains candidate-only and non-mutating.
- Analyzer Workspace remains the validation owner.
- Provider participation remains optional and advisory-only.

What is not yet visible enough:

- Design approval vs materialization approval
- refinement approval vs validation approval
- executable eligibility vs approval status
- analyzer ownership after handoff

Net assessment:

- The trust model is understandable to someone reading the implementation.
- The trust model is not yet understandable enough from the product UX alone.

## Findings

### High Priority

1. No integrated Report Design Studio product workflow is currently exposed.
   - The shipped extension commands and activation events expose analyzer and governance workflows, but no Design Studio launch surface.
   - Impact: a user cannot actually walk the intended Tasks 1-10 flow in-product.
   - Long-term risk: the architecture may appear complete while UX debt compounds around isolated internal slices.

2. Materialization, Refinement Studio, and Analyzer Handoff are workflow-complete in state but not represented as first-class UX steps.
   - Users cannot inspect candidate readiness, diagnostics, proposal rationale, or trust boundaries in a dedicated workflow UI.
   - Impact: the most sensitive trust-boundary steps are the least visible.
   - Long-term risk: future provider-backed generation would land on a workflow users do not understand.

3. Approval semantics are architecturally correct but not sufficiently legible in the current UI.
   - Users could reasonably assume that baseline approval, draft readiness, materialization readiness, and validation are closer than they really are.
   - Impact: trust-boundary confusion.
   - Long-term risk: accidental approval collapse in future UX layers.

4. Concept and Draft Studio currently under-communicate the actual artifact value.
   - The underlying models are rich, but the views show mostly summaries and counts.
   - Impact: consultants cannot meaningfully review concept quality or draft usefulness from the visible experience.
   - Long-term risk: additional workflow steps get layered on top of an already opaque review surface.

### Medium Priority

1. Design Brief completion has too much friction for a first-pass workflow.
   - Required and optional fields are mixed.
   - There is no sectioning or helper copy.
   - Several advanced fields feel unexplained.

2. Analyzer handoff messaging is technically accurate but UX-hostile.
   - Opening the Analyzer Workspace into an error-style message with “Run Retry” is not a natural workflow handoff.

3. Closed Loop comparison is functional but too text-heavy for design review.
   - Users can see that something changed, but not enough of what changed in a visually reviewable way.

4. Provider capability presentation is too raw.
   - Capability names appear as internal vocabulary rather than helpful workflow guidance.

### Low Priority

1. The current views do not provide enough positive progress framing.
   - Most user-facing copy emphasizes what the workflow does not do rather than what the current step is accomplishing.

2. The Design Brief validation area is serviceable but plain.
   - It works as a test surface, not as a production review workflow.

3. Closed Loop lineage display is informative but dense.
   - It would benefit later from stronger visual hierarchy.

## Recommendations

1. Do not start provider-backed generation work until there is a real integrated Design Studio shell with explicit step navigation.
2. Add first-class UX surfaces for Materialization, Analyzer Handoff, and Refinement Studio before expanding capability.
3. Make approval boundaries visually explicit at every step:
   - Design Approval
   - Refinement Approval
   - Materialization Approval
   - Validation Approval
4. Upgrade Concept Studio to render the actual consultant review artifacts:
   - chapter map
   - page recommendations
   - KPI hierarchy
   - navigation structure
   - analytical flow
5. Upgrade Draft Studio from artifact counts to artifact review.
6. Reframe Analyzer handoff as a successful transition with clear next-step guidance, not as an error-like panel state.
7. Simplify the Design Brief with grouping, required/optional affordances, and helper text before any production rollout.
8. Treat the current implementation as workflow-validating internal scaffolding, not production-ready UX.

## Final Answers

### 1. Is Report Design Studio understandable?

Partially. The implementation is understandable to developers and reviewers reading the contracts and tests. It is not yet understandable enough as an end-user workflow.

### 2. Is the workflow coherent?

Yes at the architecture and state-management level. No as a complete product experience, because key workflow stages are not yet surfaced as integrated UX.

### 3. Is the trust model understandable?

Partially. The trust model is strong in implementation and tests, but not yet visible enough in UX for users to reliably understand the approval boundaries.

### 4. Is the architecture ready for provider-backed generation?

The architecture is close enough for controlled internal experimentation, but the workflow UX is not ready for provider-backed generation. User comprehension should improve first.

### 5. What should be improved before production use?

- expose a real end-to-end Design Studio workflow
- add first-class Materialization, Refinement Studio, and Handoff UX
- make approval boundaries explicit and visible
- make Concept and Draft artifacts genuinely reviewable
- reduce Design Brief friction

## Overall Verdict

Report Design Studio is workflow-coherent as an internal artifact-and-boundary architecture.

It is not yet ready as a user-understandable product workflow.

The next work before production should be UX completion and trust-boundary legibility, not more capability.
