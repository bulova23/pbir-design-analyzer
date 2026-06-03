# AI Proposal Enrichment Design

Date: 2026-06-02

Status: Approved planning direction captured; ready for implementation planning

## Goal

Add an advisory AI proposal-enrichment layer to PBIR Design Analyzer so the product can move from:

- analyze
- diagnose
- remediate
- propose deterministic fixes

into:

- explain remediation more clearly
- prioritize remediation more intelligently
- generate stronger advisory proposal wording
- preserve the existing deterministic preview/apply/rollback/re-analysis loop

Phase 3 improves recommendation quality.

Phase 3 does not change how report mutations are executed.

## Strategic Positioning

Phase 1 proved the deterministic fix-opportunity engine.

Phase 2 hardened the preview/apply/rollback/re-analysis trust loop.

Phase 3 should now improve how remediation is presented, explained, and prioritized without reopening execution risk.

This phase is intentionally about:

- smarter recommendations
- better proposal wording
- better rationale
- better prioritization
- clearer business impact

This phase is intentionally not about:

- smarter execution
- autonomous editing
- mutation generation
- hidden refactoring

## Canonical Architecture

The Phase 3 architecture should be:

- `Issues`
- `Remediation Queue`
- `AI Proposal Enrichment`
- `Fix Opportunity Engine`
- `Deterministic Mutation Layer`

Each layer has one job:

- `Issues` identifies problems
- `Remediation Queue` identifies solution intent
- `AI Proposal Enrichment` improves advisory proposal quality
- `Fix Opportunity Engine` operationalizes safe deterministic opportunities
- `Deterministic Mutation Layer` applies approved changes in an auditable, reversible way

The new enrichment layer sits above deterministic execution.

It may shape what users see and how they decide.

It may not change how file mutations are planned or applied.

## Architecture Principles

### Proposal Quality, Not Execution Autonomy

Phase 3 exists to improve the quality of:

- proposal wording
- rationale
- summaries
- prioritization
- business-impact framing
- advisory alternatives

Phase 3 must not generate low-level file edits.

### Remediation-Led Workflow Stays Intact

Phase 3 remains downstream from `Issues` and upstream from the `Fix Opportunity Engine`.

It does not:

- move the workflow to raw issue cards
- replace remediation items as the solution-intent layer
- create a separate AI editing workspace

The product remains remediation-led, not freeform prompt-led.

### Scoring And Findings Stay Authoritative

Phase 3 must not modify:

- score values
- severity values
- confidence values
- normalized finding semantics

AI may summarize or prioritize around those signals.

AI may not rewrite them.

### Advisory And Executable Layers Must Stay Separate

The product should separate:

- advisory enrichment
- deterministic executable proposals

The user should be able to tell the difference between:

- an AI-enriched explanation
- a deterministic mutation preview
- a real apply outcome

### Grounding Before Generation

Every enrichment request must be grounded in deterministic local context such as:

- normalized findings
- remediation item text and category
- score metadata
- page and visual metadata
- fix opportunity metadata when available
- explicit supported/unsupported mutation status

Phase 3 should never ask a model to infer beyond the grounded evidence surface without marking the result advisory.

## Execution Trust Boundary

This boundary is permanent.

### AI May

- enrich proposal wording
- explain why a remediation matters
- prioritize remediation items
- summarize expected outcomes
- group related advisory actions
- suggest alternative advisory approaches

### AI May Not

- mutate report files directly
- generate direct PBIR edits for execution
- generate direct TMDL edits for execution
- bypass preview
- bypass approval
- bypass apply
- bypass rollback
- bypass deterministic validation
- bypass re-analysis

### Deterministic Execution Requirement

All report modifications must still flow through:

- explicit deterministic fix opportunities
- explicit preview rows
- explicit approval
- deterministic apply orchestration
- deterministic rollback plans
- deterministic validation
- post-apply re-analysis

If Phase 3 cannot map its recommendation to this trust loop, the recommendation must remain advisory-only.

## Product Problem

The current product can already:

- diagnose issues
- surface remediation intent
- generate deterministic opportunities for supported domains
- preview exact mutations
- apply safely
- roll back safely
- re-analyze outcomes

What it does not yet do well enough:

- generate strong candidate titles and labels
- explain why a remediation matters in business terms
- prioritize remediation items in a consultant-friendly way
- distinguish high-value actions from low-value cleanup
- suggest advisory alternatives when deterministic execution is unavailable
- make the analyzer feel more intelligent without adding execution risk

These are proposal-quality gaps, not mutation-engine gaps.

## Product Scope

### Phase 3 Includes

- title suggestion enrichment
- remediation explanation enrichment
- why-this-matters business-impact enrichment
- advisory prioritization and grouping
- expected-outcome narratives
- advisory alternatives
- domain-specific enrichers
- provider-agnostic enrichment orchestration
- grounding, safety, and consistency checks for advisory output
- UI treatment for enriched versus deterministic content
- telemetry and debug evidence for enrichment quality review

### Phase 3 Excludes

- model-generated mutations
- autonomous editing
- direct PBIR modifications
- direct TMDL modifications
- DAX generation
- model changes
- report generation
- visual creation
- chart replacement
- AI execution paths
- hidden refactoring under advisory copy
- provider-specific lock-in embedded into core scoring logic

## Target Workflow

The intended user flow becomes:

- `Overview`
- `Issues`
- `Remediation Queue`
- `AI-enriched proposal framing`
- `Fix Opportunities`
- `Preview`
- `Apply`
- `Re-Analyze`

Operationally:

- user reviews findings
- product surfaces remediation items
- enrichment layer produces stronger advisory wording and prioritization
- supported remediation still generates deterministic fix opportunities
- user previews exact mutations
- user approves apply
- system applies and re-analyzes deterministically

For unsupported remediation:

- enrichment may still improve advisory guidance
- no executable opportunity appears unless the deterministic engine supports it

## AI Proposal Enrichment Layer

### Responsibilities

The enrichment layer should:

- transform grounded deterministic context into stronger proposal language
- rank or group remediation items without changing underlying findings
- generate short and long rationale variants
- generate expected-outcome summaries tied to supported deterministic results
- generate advisory alternatives for unsupported or low-confidence scenarios
- expose machine-readable provenance for every generated enrichment

### Non-Responsibilities

The enrichment layer should not:

- invent mutation plans
- choose apply order
- validate file edits
- inspect live files for mutation correctness
- claim a deterministic result occurred before re-analysis proves it

### Placement

The preferred placement is above the deterministic fix opportunity builder and alongside presentation shaping, not inside scoring.

That means:

- scoring stays model-free and authoritative
- findings stay normalized and stable
- enrichment consumes stable output contracts
- fix opportunity planning remains deterministic

## Capability Areas

### 1. Title Suggestion Enrichment

Phase 3 should improve generic title recommendations into stronger grounded candidates.

Example:

- current: `Add Page Title`
- enriched:
  - `Executive Sales Overview`
  - `Regional Performance Summary`
  - `Customer Retention Analysis`

Requirements:

- title suggestions must be grounded in existing page purpose, findings, and visible semantic cues
- the UI should surface multiple candidates when confidence is moderate
- unsupported suggestions remain advisory until mapped into a deterministic title mutation

### 2. Remediation Explanation Enrichment

Phase 3 should rewrite terse remediation text into clearer reasoning.

Example:

- current: `Normalize title anchors`
- enriched: `Standardizing title anchors improves cross-page navigation and reduces cognitive switching costs.`

Requirements:

- explanations must stay tied to deterministic remediation intent
- explanations must avoid claiming outcomes not supported by the underlying evidence
- concise and expanded variants should both be available

### 3. Why This Matters Enrichment

Phase 3 should generate concise business-impact explanations.

Example:

- finding: `Missing benchmark`
- enriched: `Without a benchmark, users can see performance but cannot determine whether the result is good or bad.`

Requirements:

- impact statements must be short, concrete, and evidence-grounded
- wording should map design issues to decision-making consequences
- unsupported causal claims should be blocked or softened

### 4. Proposal Prioritization

Phase 3 may assist with:

- ranking remediation items
- identifying highest-value actions
- grouping related actions

Phase 3 must not alter:

- score
- severity
- confidence

The preferred model is:

- preserve source signals
- add a separate advisory priority model and rationale

Suggested advisory outputs:

- `High leverage`
- `Quick clarity win`
- `Consistency cleanup`
- `Executive readability`
- `Accessibility risk`

### 5. Expected Outcome Narratives

Phase 3 should generate advisory statements such as:

- `If applied, this fix is expected to improve consistency, readability, and navigation.`

Requirements:

- expected outcomes must be derived from deterministic findings and supported fix categories
- the UI must distinguish expected outcomes from actual re-analysis outcomes
- post-apply reporting must remain deterministic and not be overwritten by AI language

### 6. Advisory Alternatives

Phase 3 should provide alternative approaches when useful.

Example:

- `Instead of adding another KPI card, consider consolidating the existing KPI section.`

Requirements:

- alternatives are advisory only
- alternatives must not be expressed as executable mutations unless the deterministic layer supports them separately
- alternatives are especially useful when the remediation is unsupported, risky, or ambiguous

### 7. Domain-Specific Enrichers

Phase 3 should support specialized advisory enrichers such as:

- `Layout Enricher`
- `Theme Enricher`
- `Navigation Enricher`
- `Storytelling Enricher`
- `Executive Readability Enricher`
- `Accessibility Enricher`

These should be modular advisory units, not provider-specific plugins.

Each enricher should:

- accept a bounded deterministic input contract
- emit advisory output in a shared schema
- identify its rationale and evidence basis
- remain swappable or disableable without affecting execution

## Reference Review Integration

The `Power BI Agent Skills Reference Review` should influence Phase 3 only at the pattern level.

Adopted ideas:

- specialization patterns for advisory enrichers
- reviewer-style critique concepts for future domain-specific enrichment
- validator and hook concepts as inspiration for deterministic grounding and output checks

Rejected ideas:

- importing external skills
- copying prompts
- vendoring code
- adopting a plugin-execution marketplace
- letting external agents own mutation authority

The external repo remains research input, not embedded product logic.

## Enrichment Architecture

### Core Components

Recommended Phase 3 components:

- `proposalEnrichmentOrchestrator`
  - entry point for enrichment requests
- `proposalEnrichmentContextBuilder`
  - builds grounded input context from findings, remediation items, opportunities, and report metadata
- `proposalEnrichmentProvider`
  - provider abstraction for model invocation
- `proposalEnrichmentValidators`
  - blocks or downgrades unsupported, ungrounded, or contradictory output
- `proposalEnrichmentCache`
  - optional session/report-scoped advisory output cache
- `domain enrichers`
  - layout, theme, navigation, storytelling, executive readability, accessibility
- `proposalEnrichmentTelemetry`
  - captures provenance, latency, refusal, and validation outcomes

### Data Flow

Recommended flow:

1. collect deterministic input context
2. choose enrichment scope
3. build grounded prompt/input package
4. invoke provider through abstraction
5. validate output against deterministic constraints
6. downgrade or discard invalid output
7. thread accepted advisory enrichment into the score-panel payload

### Failure Handling

If enrichment fails, the product should:

- keep deterministic remediation visible
- omit or downgrade the enrichment section
- surface concise fallback wording
- never block deterministic fix preview/apply flows

AI enrichment failure must never prevent:

- issue review
- deterministic proposal generation
- preview
- apply
- rollback
- re-analysis

## Advisory Output Model

Phase 3 should introduce a shared advisory schema separate from fix opportunities.

Recommended shape:

```ts
export interface ProposalEnrichment {
  remediationItemId: string;
  status: 'available' | 'fallback' | 'rejected' | 'skipped';
  source: 'provider' | 'fallback';
  enrichersApplied: ProposalEnricherId[];
  titleSuggestions?: EnrichedTitleSuggestion[];
  explanation?: EnrichedExplanation;
  whyThisMatters?: EnrichedImpactSummary;
  advisoryPriority?: AdvisoryPriority;
  expectedOutcome?: ExpectedOutcomeNarrative;
  advisoryAlternatives: AdvisoryAlternative[];
  validation: ProposalEnrichmentValidationResult;
  provenance: ProposalEnrichmentProvenance;
}
```

Recommended supporting types:

```ts
export type ProposalEnricherId =
  | 'layout'
  | 'theme'
  | 'navigation'
  | 'storytelling'
  | 'executiveReadability'
  | 'accessibility';

export interface AdvisoryPriority {
  tier: 'highLeverage' | 'quickWin' | 'consistencyCleanup' | 'advisoryOnly';
  rationale: string;
}
```

This contract should remain presentation and advisory only.

It must not contain executable mutations.

## Grounding And Hallucination Prevention

Phase 3 should be designed around bounded evidence, not open-ended generation.

Recommended controls:

- only pass deterministic local context, not raw file trees
- identify unsupported remediation categories explicitly
- label unsupported or low-grounding sections as advisory
- require output validators for contradictory claims
- reject content that invents visuals, measures, fields, or outcomes not present in context
- constrain length and tone for UI-safe outputs
- retain fallback deterministic copy if validation fails

The product should prefer omission over plausible-sounding unsupported advice.

## Explainability And Auditability

Every enrichment artifact should retain enough provenance to answer:

- which enricher produced this
- what deterministic inputs grounded it
- whether a validator downgraded it
- whether it influenced only wording, ranking, or grouping

The user-facing UI does not need full raw prompt visibility by default, but the system should preserve developer-facing debug evidence for QA and tuning.

## UX And Presentation Principles

### Clear Labeling

The UI should clearly differentiate:

- deterministic findings
- AI-enriched advisory content
- deterministic fix previews
- deterministic actual outcomes

### Progressive Disclosure

Use short default summaries with optional expansion for:

- explanation detail
- why-this-matters detail
- alternative approaches
- priority rationale

### Advisory Copy Must Not Read Like Execution

Avoid wording that implies the system already:

- changed the report
- validated the outcome
- proved a business effect

### Unsupported Work Should Stay Honest

If a remediation remains unsupported for execution, enrichment should say so directly and avoid presenting the suggestion as one click away from apply.

## Relationship To Phase 4

### Why Phase 3 Comes Before Advanced AI Refactoring

Phase 3 comes first because:

- the deterministic trust loop now exists and should be exploited before broadening mutation scope
- recommendation quality can improve user value without increasing execution risk
- better advisory grounding creates a safer base for any future advanced refactoring work
- product trust improves when AI helps users decide rather than silently changing the system beneath them

### Phase 3 Must Not Become Hidden Phase 4

Phase 3 must not smuggle in:

- mutation generation
- execution planning
- file-edit synthesis
- redesign authority

If a future proposal crosses from advisory into executable redesign, it belongs to Phase 4 or later and requires a separate spec.

## Relationship To Phase 5 Report Design Studio

Report Design Studio should build on Phase 3 specialization patterns, not duplicate them.

Phase 5 should likely reuse:

- domain-specific critique modules
- richer advisory panels
- narrative and executive-readability guidance
- theme and storytelling review modes

But Phase 5 should still preserve the same trust boundary whenever execution enters the picture.

Report Design Studio should not turn Phase 3 advisory enrichers into autonomous editors.

## Roadmap Position

The intended AI-fix and advisory sequence is:

1. Phase 1: Deterministic Fix Opportunity Engine
2. Phase 2: Preview / Apply / Rollback Hardening
3. Phase 3: AI Proposal Enrichment
4. Phase 4: Advanced AI Refactoring
5. Phase 5: Report Design Studio

This sequence keeps intelligence above deterministic execution until the product has stronger evidence that broader redesign workflows can remain explainable and auditable.

## Testing Strategy

Phase 3 needs a testing strategy that treats advisory output as a product surface, not an untestable side effect.

### Deterministic Input Tests

Validate:

- context building from normalized findings
- remediation-item grounding
- opportunity-aware context shaping
- correct domain-enricher routing

### Prompt Grounding Tests

Validate:

- only approved deterministic fields are included
- unsupported categories are labeled correctly
- source signals are preserved without mutation

### Explainability Tests

Validate:

- generated rationale cites the correct problem surface
- advisory priority rationale does not contradict source findings
- expected outcome narratives are framed as expected, not actual

### Hallucination Prevention Tests

Validate:

- invented fields/measures/visuals are rejected
- invented deterministic support is rejected
- contradictory outputs are downgraded or discarded
- missing grounding causes fallback copy, not fabricated confidence

### Consistency Checks

Validate:

- same deterministic inputs produce stable advisory structure
- multiple enrichers do not produce contradictory priority tiers
- advisory alternatives remain consistent with supported/unsupported status

### Advisory Output Validation

Validate:

- enriched content remains clearly labeled as advisory
- fix preview/apply flows continue working when enrichment is absent
- payload contracts support enrichment omission and fallback cleanly
- the webview renders enriched and deterministic content without ambiguity

### Integration And Smoke Strategy

Phase 3 should include:

- focused unit tests for context building, validation, and enricher routing
- contract tests for host-to-webview payload shaping
- mocked provider tests for deterministic UI behavior
- one or more smoke paths that prove enrichment failure does not block deterministic workflows

## Rollout Strategy

Recommended rollout:

1. hidden internal advisory plumbing
2. fallback-first UI contracts
3. one or two enrichers behind configuration
4. broader domain-enricher rollout after quality review
5. telemetry-driven tuning before enabling wider default coverage

This should be a reversible presentation-layer rollout, not a core execution migration.

## Open Design Decisions

These should be resolved during implementation planning, not implementation:

- whether enrichment is triggered eagerly during score payload shaping or lazily from the webview
- whether provider calls are per remediation item or batched by page/report
- whether cached enrichment is session-scoped only or persisted across report rescoring
- whether advisory priority should be shown directly in the queue or only inside expanded remediation details
- whether title suggestion selection needs a dedicated UI affordance before deterministic preview

## Definition Of Done

Phase 3 design is complete when:

- the advisory enrichment layer is explicitly defined
- the execution trust boundary is documented and preserved
- Phase 3 capability areas are defined
- the relationship to Phase 4 and Phase 5 is explicit
- the Power BI Agent Skills review is incorporated as pattern-level guidance only
- a testing strategy exists for grounding, explainability, and hallucination prevention
- an implementation plan can proceed without reopening Phase 1 or Phase 2 architecture
