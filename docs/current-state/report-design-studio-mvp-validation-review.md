# Report Design Studio MVP Validation Review

Date: 2026-06-14

## Scope

This review validates whether the completed Report Design Studio MVP is understandable, usable, and valuable for real consulting and report-design workflows.

Included workflow:

- Design Brief
- Concept Studio
- Draft Studio
- Materialize Candidate
- Analyzer Handoff
- Refinement Studio
- Compare Iterations

Out of scope:

- code changes
- architecture changes
- feature additions
- provider-backed generation
- Fabric skills integration
- AI-assisted draft generation
- advanced automation

## Method

This review used the narrowest current implementation paths that reflect the shipped MVP:

- current Report Design Studio shell, stage copy, approval cards, refinement experience, and iteration comparison surfaces
- seeded design artifacts and current webview test scenarios for stage-specific workflow inspection
- current command entry contributions for report-scoped launch validation
- focused workflow and trust-boundary validation already present in the Design Studio slice

Validation executed:

- `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/designBriefStore.test.ts src/test/conceptStore.test.ts src/test/draftStore.test.ts src/test/materializationCoordinator.test.ts src/test/analyzerHandoffService.test.ts src/test/refinementStore.test.ts src/test/iterationStore.test.ts src/test/designStudioProtocol.test.ts src/test/designStudioContracts.test.ts src/test/pbirDesignStudioCommand.treeItem.test.ts`
  - passed: 69 tests
- `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/design-studio/__tests__/DesignBriefView.test.tsx webview-src/design-studio/__tests__/ConceptStudioView.test.tsx webview-src/design-studio/__tests__/App.test.tsx webview-src/design-studio/__tests__/ClosedLoopView.test.tsx`
  - passed: 10 tests
- `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~DesignStudio`
  - passed: 19 tests

## 1. Scenario Walkthroughs

### Scenario A: Executive Dashboard

Representative example:

- CEO dashboard
- revenue and margin overview

Walkthrough:

- Design Brief is workable for this scenario because audience, business objective, intended story, success criteria, consumption context, and decision cadence all fit naturally.
- Concept Studio is directionally useful because alternate concepts support a genuine executive design choice between a narrative-first and operating-rhythm posture.
- Draft Studio is the first major usability drop. A consultant can see that a draft exists, but cannot meaningfully review whether the executive page actually lands the story because the stage exposes counts more than artifacts.
- Materialize Candidate is understandable only after reading the stage description carefully. The term materialize is not natural consultant language.
- Analyzer Handoff is conceptually coherent, but it still feels like a tooling transition rather than a workflow continuation.
- Refinement Studio is strong for this scenario. Recommendation, rationale, expected impact, and original/current/proposed comparison map well to executive-story consulting work.
- Compare Iterations is useful for summarizing whether the design got better, but it is still more textual than visual.

Assessment:

- valuable: yes
- understandable without internal knowledge: partly
- likely outcome: a consultant can finish the workflow, but only with some interpretation during Draft Studio and Handoff

### Scenario B: Operational Monitoring

Representative example:

- sales operations
- inventory monitoring
- service management

Walkthrough:

- Design Brief is strongest here because cadence, evidence domains, navigation expectations, and target surface framing all matter and feel justified.
- Concept Studio still lacks enough visible structure. Operational workflows depend heavily on KPI hierarchy, drill path, and navigation sequencing, but those details are mostly hidden behind summary-level concept cards.
- Draft Studio again under-communicates the real value. Operational monitoring needs visible page structure and alerting posture review, and the MVP does not yet expose enough artifact detail.
- Materialize Candidate is more valuable here than in Scenario A because readiness and diagnostics are relevant to whether the design is analyzable, but the language is still technical.
- Analyzer Handoff is understandable once the consultant sees that validation belongs to Analyzer Workspace, though the ownership boundary is clearer than the actual transition wording.
- Refinement Studio is useful because grouped navigation, KPI, and structure recommendations map well to operational report critique.
- Compare Iterations gives enough audit trail to understand that the workflow is iterative, but not enough concrete before-and-after design evidence for fast consultant review.

Assessment:

- valuable: yes
- understandable without internal knowledge: partly
- likely outcome: a consultant can work through it, but will want stronger draft and navigation visibility before trusting the design

### Scenario C: Analytical Investigation

Representative example:

- root cause analysis
- diagnostic report
- performance investigation

Walkthrough:

- Design Brief supports this scenario well because key decisions, intended story, evidence domains, and risks or constraints are appropriate inputs.
- Concept Studio is the weakest stage for this scenario because analytical investigations depend on chapter sequence, question decomposition, and branching logic, but the MVP exposes little of that concept structure directly.
- Draft Studio does not make it clear enough what a draft means for a diagnostic workflow. A consultant can approve a draft without seeing enough proof that the page sequence and investigative flow are sound.
- Materialize Candidate and Analyzer Handoff matter more in this scenario because analyzer feedback is central to the loop, which makes the technical wording more noticeable.
- Refinement Studio is quite useful here because rationale and expected impact directly support consulting critique.
- Compare Iterations is useful for understanding recommendation and validation evolution, but not sufficient for assessing whether the analytical path became easier to use.

Assessment:

- valuable: moderately
- understandable without internal knowledge: no, not fully
- likely outcome: a consultant can follow the loop, but the concept and draft stages do not expose enough analytical structure to make confident design decisions

## 2. Workflow Observations

- The overall workflow is coherent. The stages form a believable consultant loop from intent definition through validation and iteration.
- The shell materially improves usability because the workflow rail, stage summaries, and stage-local approval cards keep the process legible.
- The weakest point is still the middle transition from Draft Studio to Materialize Candidate to Analyzer Handoff. This is the point where product language becomes tool language.
- Refinement Studio is the clearest value proof in the MVP. It reads like design consulting work rather than internal system state.
- Compare Iterations proves continuity and trust, but the MVP still explains changes better than it shows them.

## 3. UX Observations

- Design Brief is easy enough to start, but not easy enough to complete quickly. The form is long and flat, and advanced fields are not explained.
- Concept Studio has useful underlying concepts, but the visible review surface is too compressed. Chapter structure, navigation structure, and KPI hierarchy are present in the model but underexposed in the UI.
- Draft Studio does not yet make draft artifacts feel tangible. Users can tell the stage exists, but not why a given draft is good.
- Materialize Candidate benefits from the shell and diagnostics panel, but the terminology is still more implementation-shaped than consultant-shaped.
- Analyzer Handoff preserves the right authority boundary, but the workflow copy does not yet make the transition feel routine and safe.
- Refinement Studio is the strongest UX stage in the MVP because recommendation, rationale, impact, evidence, and comparison all appear together.
- Compare Iterations is understandable, but heavily text-based. It answers what changed better than how the experience changed.

## 4. Trust-Boundary Observations

- The approval distinctions are present and much clearer than they were in the earlier pre-shell state.
- Design Approval, Materialization Approval, Refinement Approval, and Validation Approval are all explicitly labeled with owner, unlock, and non-effects, which is the right teaching pattern.
- Users are still likely to confuse readiness with approval, especially in Materialize Candidate and Analyze Draft.
- Users are also likely to confuse Design Approval with “good enough to validate” because approval cards appear stage-local instead of cumulative.
- Validation Approval is the clearest trust boundary once the user reaches Compare Iterations, but less clear earlier in the flow.
- The trust model is mostly understandable if a consultant reads the cards carefully. It is not yet self-explanatory under quick scanning.

## 5. High-Priority Findings

1. Draft Studio does not expose enough artifact detail for consultants to judge whether a draft is genuinely reviewable.
   Impact: consultants may approve drafts based on workflow progress instead of design quality.

2. Concept Studio hides too much of the concept structure that matters most for consulting work.
   Impact: chapter structure, navigation structure, and KPI hierarchy exist, but the user cannot review them deeply enough to choose a baseline confidently.

3. Materialize Candidate and Analyzer Handoff still use terminology that assumes internal architectural knowledge.
   Impact: the end-to-end workflow is not yet understandable enough for a consultant who only knows report-design work, not the platform model.

4. Approval semantics are present but still easy to conflate at speed.
   Impact: users can confuse readiness, design approval, materialization approval, and validation approval, especially across the middle stages.

5. Analytical-investigation workflows are under-supported by the visible Concept Studio and Draft Studio UX.
   Impact: the MVP is least convincing in the scenario where reasoning structure matters most.

## 6. Medium-Priority Findings

1. Design Brief has avoidable completion friction because required and optional fields are not clearly separated and advanced fields lack helper context.

2. Compare Iterations is useful but too text-heavy for fast consulting review.

3. Refinement Studio is strong, but the path from analyzer results into refinement still feels procedural rather than naturally continuous.

4. Materialization diagnostics are understandable as status signals, but not yet framed in consultant language.

5. Alternate concept comparison is useful in principle, but the visible comparison surface is too shallow to make side-by-side evaluation feel substantial.

## 7. Low-Priority Findings

1. Some stage labels still feel internally derived rather than polished for consulting language.

2. Provider capability presentation in Draft Studio reads as implementation inventory rather than workflow guidance.

3. Approval cards are effective, but repeated card structure across stages makes fast scanning slightly monotonous.

4. The current shell explains the workflow better than it celebrates progress, so the experience can feel more procedural than outcome-driven.

## 8. Recommended Improvements

Before provider-backed generation, improve these areas first:

1. Make Concept Studio artifacts more reviewable.
   Show chapter structure, navigation structure, KPI hierarchy, and analytical flow as first-class consultant review content.

2. Make Draft Studio artifacts tangible.
   Expose page-level and layout-level artifact detail clearly enough that draft approval feels evidence-based.

3. Reframe middle-stage language.
   Explain Materialize Candidate and Analyze Draft in plain workflow terms so consultants do not need internal architecture knowledge to proceed.

4. Strengthen approval teaching.
   Reinforce the difference between readiness, approval, and validation directly inside each stage, not only in the approval cards.

5. Improve iteration readability.
   Keep the current audit-oriented comparison, but add stronger human-readable emphasis on what improved for the report user.

6. Reduce Design Brief friction.
   Clarify field intent, especially for evidence domains, target surface framing, and constraints.

## 9. Readiness Assessment

Readiness for internal consultant usage:

- not ready for broad internal consultant use without guidance
- ready for a narrow internal pilot with product-context support

Readiness for early design reviews:

- yes, with moderate friction

Readiness for design-first report planning:

- yes, especially for executive and operational scenarios
- less convincing for analytical-investigation scenarios until concept and draft visibility improve

Net assessment:

- The MVP is directionally valuable and structurally coherent.
- It is not yet fully understandable end-to-end for a consultant without special knowledge of the internal architecture.
- The biggest blockers are not trust-boundary correctness. They are artifact visibility, stage language, and approval legibility.

## Final Answers

1. Is Report Design Studio understandable?
   - Partially. The shell helps, but the full workflow still assumes too much internal vocabulary and too little visible artifact detail.

2. Is the workflow coherent?
   - Yes. The stage order and handoff logic make sense.

3. Is the approval model understandable?
   - Partially. It is much clearer than before, but users can still confuse readiness, approval, and validation.

4. Is the trust model understandable?
   - Partially. The boundary is well-designed and reasonably explained, but not yet obvious under normal workflow speed.

5. Is the refinement workflow useful?
   - Yes. This is one of the strongest parts of the MVP.

6. Is the iteration workflow useful?
   - Yes, but it is more audit-friendly than design-review-friendly.

7. Is the MVP ready for internal consultant use?
   - Not for broad self-serve use. It is suitable for a guided internal pilot.

8. What must be improved before provider-backed generation?
   - Concept visibility, draft visibility, middle-stage language, approval clarity, and iteration readability.
