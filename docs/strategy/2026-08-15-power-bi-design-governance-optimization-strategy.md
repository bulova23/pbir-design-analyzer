# Power BI Design Governance & Optimization Strategy

Date: 2026-08-15
Status: Strategy proposal after 0.6.0 RC1
Implementation status: No implementation authorized by this document

## Executive summary

PBIR Design Analyzer should stop presenting itself as a report-authoring
platform. Microsoft now provides an increasingly complete first-party path for
Power BI report design, planning, PBIR authoring, semantic-model editing,
Desktop verification, report management, and Fabric delivery. Its current
Power BI Agentic materials explicitly describe a design → plan → author →
validate → publish flow, and the Power BI Report Authoring skill already covers
pages, visuals, filters, slicers, formatting, themes, and PBIR validation.

The durable opportunity is the control plane above that flow:

> **Power BI Design Governance & Optimization is the independent quality,
> policy, evidence, and remediation layer for every Power BI report—whether
> authored by a person, Microsoft AI, a consultant, or a CI/CD pipeline.**

The product should accept a report and its available semantic/evidence context,
evaluate it against reusable organizational standards, explain violations with
stable evidence, produce a deterministic review packet, and optionally offer
bounded remediation. It should not own the general-purpose report-authoring
conversation, semantic-model generation, DAX generation, or Fabric publishing
loop.

Recommended product identity:

- Product: **Power BI Design Governance & Optimization**
- Short name: **Design Governance**
- Analyzer: **Design Governance Engine**
- Policy language: **Power BI Design Policy (PDP)**
- Remediation layer: **Deterministic Remediation Engine**

The 0.6.0 RC is a credible technical foundation for this direction, but it is
not yet Version 1.0 of the repositioned product. Its public messaging should
change before launch, and the first product work should turn current scoring,
findings, design principles, evidence, and deterministic mutation into a
policy-backed governance workflow. No new authoring feature should be added
merely to preserve the old positioning.

## Strategic evidence

Microsoft’s current public materials establish the following competitive
context:

- [Power BI Agentic overview](https://learn.microsoft.com/en-us/power-bi/developer/agentic/power-bi-agentic-overview)
  describes agent skills and tools for semantic models, reports, PBIR, live
  Desktop verification, and natural-language development.
- [Power BI Report Design](https://learn.microsoft.com/en-us/power-bi/developer/agentic/power-bi-report-design-skill-overview)
  supplies archetype routing, chart selection, color, typography, layout,
  accessibility, anti-pattern detection, and a design brief.
- [Power BI Report Authoring](https://learn.microsoft.com/en-us/power-bi/developer/agentic/power-bi-report-authoring-skill-overview)
  handles PBIR/PBIP report creation and modification, validation, and Desktop
  verification, including pages, visuals, filters, slicers, themes, and
  formatting.
- [Report Planner and Management](https://learn.microsoft.com/en-us/power-bi/developer/agentic/power-bi-planner-fabric-skill-overview)
  covers requirements, approval, implementation planning, and Fabric report
  item management.
- [Power BI MCP servers](https://learn.microsoft.com/en-us/power-bi/developer/mcp/mcp-servers-overview)
  provide local semantic-model editing and remote report metadata/query
  capabilities for agent workflows.
- [Fabric CLI](https://learn.microsoft.com/en-us/rest/api/fabric/articles/fabric-command-line-interface)
  and [Fabric CI/CD options](https://learn.microsoft.com/en-us/fabric/cicd/manage-deployment)
  provide authenticated automation, Git, item APIs, deployment pipelines, and
  build-environment scripting.
- [PBIR/PBIP documentation](https://learn.microsoft.com/en-us/power-bi/developer/embedded/projects-enhanced-report-format)
  makes report definitions source-control friendly and explicitly supports
  programmatic changes and quality gates through project workflows.
- [Power BI accessibility guidance](https://learn.microsoft.com/en-us/power-bi/create-reports/desktop-accessibility-creating-reports)
  confirms accessibility is an important report-design concern, including
  keyboard navigation, screen readers, high contrast, and alternative text.
- [Fabric governance guidance](https://learn.microsoft.com/en-us/power-bi/guidance/fabric-adoption-roadmap-governance)
  identifies certification, lifecycle management, standards, ownership, and
  organization-specific requirements as governance concerns.

The strategic inference is not that Microsoft has no quality guidance. It does.
The opportunity is to make organizational standards operational and
independent: versioned policy, deterministic compliance results, stable
finding IDs, cross-report benchmarks, approval evidence, and remediation
controls that can sit in front of any authoring provider.

## Product vision

### Mission

Help organizations make every Power BI report understandable, consistent,
accessible, trustworthy, and ready for governed delivery—regardless of how it
was created.

### Vision

Every AI- or human-authored Power BI report passes through a transparent design
quality gate before it becomes an organizational asset.

### Elevator pitch

**Power BI Design Governance is the quality gate for Power BI reports. It
checks reports against your organization’s design, accessibility, semantic,
and delivery standards, explains every issue with evidence, and guides safe,
deterministic remediation—whether the report came from Power BI, Fabric AI, a
consultant, or a developer pipeline.**

### Product promise

1. Independent: authoring tools may change; the organization’s quality bar
   remains stable.
2. Deterministic: the same report, policy, and evidence produce the same
   compliance result.
3. Explainable: every finding has a rule, evidence reference, severity, and
   remediation rationale.
4. Safe: AI may propose; deterministic planning and explicit approval control
   changes.
5. Composable: works locally in VS Code, in review workflows, and eventually
   in CI/CD and Fabric automation.

## Market position

### Primary beachhead

The first commercial beachhead should be **enterprise BI teams and Power BI
consulting firms that need repeatable design quality across many reports**.

These buyers already have report creation tools. Their pain is inconsistency,
review cost, rework, branding drift, accessibility risk, and uncertainty about
whether AI-generated output is ready to approve. They need a standard that can
be applied to reports created by different people, agents, and delivery paths.

### Target audiences

| Audience | Job to be done | Product value |
| --- | --- | --- |
| Enterprise BI center of excellence | Define and enforce report standards | Versioned policies, profiles, compliance dashboards, certification evidence |
| Governance and platform teams | Gate content before promotion | CI/CD checks, severity thresholds, audit trails, ownership, exceptions |
| Consulting firms | Deliver consistent client reports at scale | Reusable standards, review packets, remediation plans, client-ready evidence |
| Report reviewers and design system owners | Review quality without hand-inspecting JSON | Findings, evidence, cross-page analysis, comparison, guided fixes |
| Accessibility reviewers | Find repeatable usability failures | Accessibility policy rules, evidence capture, explainable findings |
| AI-assisted report developers | Validate AI output before acceptance | Independent review, semantic checks, design compliance, safe remediation |
| Release engineers | Automate design quality gates | Deterministic CLI/API contract and non-zero policy outcomes |

### Secondary audiences

Microsoft partners and managed analytics providers can embed the governance
layer in delivery services. Individual authors and small teams are useful for
adoption and feedback, but should not define the architecture or the first
enterprise product promise.

### Positioning statement

For organizations that create or receive many Power BI reports, Power BI Design
Governance is the independent quality and standards platform that verifies
design, semantic clarity, accessibility, and delivery readiness. Unlike a
report authoring agent, it does not compete to create the report; it supplies
the evidence-backed gate that any authoring path must pass.

## Competitive analysis

| Category | Microsoft/Fabric strengths | Governance opportunity |
| --- | --- | --- |
| AI report authoring | First-party skills, PBIR mechanics, Desktop verification, semantic-model tools | Evaluate the output independently and enforce organization-specific rules |
| Report design guidance | Archetypes, chart selection, color, typography, accessibility, anti-pattern advice | Convert guidance into versioned policy with pass/fail/waiver outcomes |
| Semantic-model authoring | Local MCP and agent workflows for model objects and DAX | Validate report usage, semantic clarity, KPI context, and report/model alignment |
| Fabric delivery | CLI, REST APIs, Git integration, deployment pipelines | Add a design-quality gate to the existing delivery path |
| PBIR source control | Public schema, file-level diffs, programmatic editing | Make diffs meaningful through semantic classification and policy evidence |
| Visual review | Desktop reload and screenshots | Combine rendered evidence with metadata, semantics, and stable findings |
| Governance | Enterprise guidance, certification, sensitivity, lifecycle concepts | Operationalize design governance as a repeatable engineering artifact |

The product must avoid claiming that Microsoft lacks design guidance, validation,
or CI/CD. Its claim is narrower and stronger: **organizational design policy and
independent evidence are not the same thing as authoring guidance or file
validation.**

## Differentiation matrix

| Capability | Commodity / partner capability | Durable product differentiation |
| --- | --- | --- |
| Create pages and visuals | Commodity; Microsoft authoring skill already covers it | Not a primary product promise |
| PBIR file mechanics | Commodity infrastructure | Preserve as an adapter and fidelity boundary |
| Schema validation | Necessary baseline | Combine with organization policy and semantic findings |
| Design advice | Increasingly available from Microsoft AI | Policy versioning, exceptions, evidence, and auditability |
| Composite score | Easy to imitate if it is only a number | Multi-dimensional score backed by stable findings and evidence |
| Semantic quality | Less visible than file correctness | Explain report intent, field usage, context, and analytical flow |
| Accessibility | Guidance exists; enforcement is inconsistent | Repeatable checks, evidence, severity, and remediation ownership |
| Review packet | Common consulting artifact | Deterministic, finding-linked, reproducible review record |
| Mutation | General authoring feature | Bounded remediation tied to a verified finding and policy rule |
| CI/CD | Delivery plumbing exists | Organization-specific design quality gate with policy exit status |
| AI integration | Agents can create and edit | Provider-neutral reviewer and constrained remediation authority |

## Capability map

The product should be organized around user outcomes rather than historical
phases.

### Report Analysis

- PBIR/PBIP discovery, import, and source snapshotting.
- Page, visual, binding, filter, interaction, navigation, and layout inventory.
- Semantic-model usage evidence where available.
- Screenshot and bounded visual evidence through existing evidence primitives.
- Stable report fingerprint, deterministic hashes, and analysis lineage.

### Design Review

- Executive, operational, analytical, financial, and consultant review lenses.
- Visual hierarchy, density, alignment, spacing, typography, color, and
  navigation findings.
- Cross-page consistency and design-system drift.
- Review packet, evidence links, reviewer comments, and approval state.

### Design Governance

- Versioned policies and profiles.
- Required, recommended, and advisory rules.
- Exceptions, waivers, ownership, effective dates, and change history.
- Compliance status by report, page, visual, policy, and release.

### Semantic Validation

- Measure/dimension role usage and semantic binding integrity.
- KPI context, titles, units, targets, time context, and filter clarity.
- Report-to-semantic-model usage evidence.
- Unsupported or ambiguous semantics reported rather than guessed.

### Report Optimization

- Density and layout optimization recommendations.
- Duplicate or redundant visual detection.
- Navigation and interaction simplification.
- Readability, accessibility, and executive scan-path improvements.
- Before/after score and evidence comparison.

### Remediation

- Finding-linked deterministic preview.
- Typed, bounded mutation planning.
- Explicit confirmation and immutable source snapshot.
- New artifact identity, fidelity evidence, and analyzer before/after.
- AI proposal enrichment remains advisory-only.

### Enterprise Standards

- Organization and workspace profiles.
- Standard packs for executive, financial, operational, and accessibility
  reporting.
- Policy distribution and version pinning.
- Certification and approval evidence.
- Audit export and CI/CD integration.

### CI/CD validation

- Local command or API evaluation of a PBIP/PBIR change.
- Pull request annotations with finding IDs and policy impact.
- Build failure thresholds by severity or policy scope.
- Baseline comparison to distinguish new from accepted findings.
- Artifact hash, policy hash, and report fingerprint in the result.

### AI Companion

- Review an AI-generated design brief or PBIR result.
- Explain policy failures in natural language.
- Produce a remediation plan grounded in findings.
- Generate bounded proposals for deterministic execution.
- Never bypass policy, validation, approval, or mutation authority.

## Design governance architecture

The governance architecture should add a policy layer around the existing
analysis pipeline, not replace the shared IR, analyzer, or authoring envelope.

```text
Authoring source
  Microsoft Report Design/Planner/Authoring, Power BI Desktop, human editor,
  consultant tool, CI/CD, or another agent
        |
        v
Surface adapter and shared repository snapshot
        |
        v
PBIR/PBIP analysis + semantic projection + bounded visual evidence
        |
        +--> Design Governance Engine
        |      policy compiler
        |      rule evaluation
        |      profile selection
        |      exception/waiver resolution
        |
        +--> Authoritative scoring + normalized findings + evidence
        |
        +--> Optimization planner
        |      recommendations and deterministic remediation proposals
        |
        +--> Review packet / CI result / certification record
        |
        +--> Deterministic Remediation Engine
               preview -> approve -> validate -> apply -> re-analyze
```

### Architectural responsibilities

- **Surface adapter:** identifies the thing being reviewed: PBIR/PBIP first;
  future surfaces remain possible without redefining analyzer/profile concepts.
- **Analyzer:** evaluates the surface and emits authoritative findings and
  evidence. It is not the policy itself.
- **Analyzer profile:** selects emphasis, such as Executive, Accessibility,
  or Financial. It does not change the underlying report identity.
- **Design policy:** states organizational rules and thresholds.
- **Policy evaluator:** maps policy rules to analyzer observations and produces
  compliance results.
- **Finding model:** remains the shared issue contract across review surfaces,
  policy rules, evidence, and remediation.
- **Optimization layer:** derives ranked recommendations from findings and
  should remain presentation/planning logic until a deterministic mutation is
  explicitly admitted.
- **Remediation engine:** reuses the immutable snapshot, typed planner,
  semantic diff, serializer, validator, and analyzer-before/after path.
- **Provider adapters:** integrate Microsoft or other authoring outputs as
  inputs; they never receive governance bypass authority.

### Required boundary decisions

1. A policy evaluation must not mutate a report.
2. A recommendation must not be mistaken for a compliance result.
3. A score must not replace the finding/evidence record.
4. A policy profile must not fork the analyzer into duplicated logic.
5. An AI proposal must not carry mutation authority.
6. Shared repository snapshots must remain analyzer-independent and reusable.
7. Policy and analyzer schema versions must be recorded in every result.

## Design policy language proposal

The proposed **Power BI Design Policy (PDP)** is a versioned, declarative,
machine-readable policy model. It should be readable in code review, stable in
CI/CD, and renderable into a human governance workspace. YAML is the preferred
authoring syntax; a canonical JSON representation and content hash are the
runtime form.

### Policy object model

```yaml
schemaVersion: power-bi-design-policy/v1
policyId: contoso-executive-standard
displayName: Contoso Executive Reporting Standard
version: 1.2.0
status: active
effectiveFrom: 2026-09-01
owner:
  team: BI Governance
  contact: bi-governance@example.com
appliesTo:
  surfaces: [pbir]
  profiles: [executive]
  workspaces: [executive-reporting]
  tags: [external-facing, certified]
rules:
  - ruleId: layout.page-count
    category: composition
    severity: high
    scope: report
    requirement:
      max: 6
    message: Executive reports must have no more than six visible pages.
    remediation: review-page-count
  - ruleId: layout.visual-density
    category: layout
    severity: medium
    scope: page
    requirement:
      maxVisuals: 12
      maxDensityRatio: 0.72
    message: Reduce visual density to preserve executive scanability.
    remediation: reduce-visual-density
  - ruleId: style.typography
    category: branding
    severity: high
    scope: report
    requirement:
      fontFamily: Aptos
      titleMinSize: 16
    message: Use the approved corporate typography system.
    remediation: apply-brand-style
  - ruleId: accessibility.alt-text
    category: accessibility
    severity: high
    scope: visual
    requirement:
      requiredFor: [chart, card, image, shape]
    message: Important visuals must have meaningful alternative text.
  - ruleId: semantics.kpi-context
    category: semantic
    severity: high
    scope: visual
    requirement:
      visualTypes: [card]
      requires: [title, unit-or-format, comparison-or-target]
    message: KPI cards must communicate measure context and interpretation.
exceptions:
  - exceptionId: legacy-finance-pack
    ruleId: layout.page-count
    expires: 2026-12-31
    approvedBy: finance-governance
    reason: Migration wave 1 legacy reports
outputs:
  failOn: [high]
  warnOn: [medium]
  includeEvidence: true
  includeRemediation: true
```

### PDP design principles

- **Closed vocabulary:** rule categories, severities, scopes, evidence kinds,
  and outcomes are enumerated and validated.
- **Composability:** policies inherit or compose from approved base standards,
  with explicit conflict resolution.
- **No hidden defaults:** every threshold has provenance and an effective
  policy version.
- **Explainability:** each rule maps to a stable finding code, evidence
  selector, and remediation eligibility.
- **Safe evolution:** policy schema versions are additive where possible;
  incompatible policy changes require a new version.
- **Temporal governance:** effective dates, expiry, ownership, and waivers are
  first-class.
- **Separation of concern:** PDP describes required quality; analyzer code
  describes how evidence is measured.

### Initial rule families

1. Composition: page count, page order, landing page, hidden/visible structure.
2. Density: visual count, occupied area, whitespace, overlap, alignment,
   repeated patterns.
3. Typography: family, size, hierarchy, contrast, title and label consistency.
4. Branding: theme, palette, logo, corporate colors, approved visual styles.
5. KPI design: placement, context, units, targets, comparisons, semantic role.
6. Navigation: discoverability, consistent destinations, back paths, filters.
7. Interaction: slicer placement, filter scope, cross-highlighting, ambiguity.
8. Accessibility: alt text, tab order evidence where available, contrast,
   screen-reader clarity, non-color cues, keyboard usability.
9. Semantics: valid bindings, meaningful labels, measure/dimension roles,
   ambiguity, unused or duplicated analytical content.
10. Delivery: schema validity, unsupported constructs, lineage, reproducible
    hashes, review approval, and policy compliance.

## Analyzer evolution

The analyzer should evolve from a single composite score into a governance
engine with multiple explicit outputs.

### Proposed result model

- `policyCompliance`: pass, fail, warn, exempt, or unevaluated.
- `violations`: stable finding ID, rule ID, scope, severity, evidence, and
  current/proposed state.
- `recommendations`: advisory optimization actions, ranked by value and risk.
- `remediation`: eligible, ineligible, proposal-only, or requires approval.
- `optimizationScore`: how much measurable quality opportunity remains.
- `qualityScore`: existing authoritative analytical/design score, preserved for
  compatibility and interpreted as one signal rather than the whole product.
- `fidelity`: preserved, changed, unexpected, unsupported, and schema status.
- `provenance`: report fingerprint, policy hash/version, analyzer version,
  profile, evidence snapshot, and timestamp.

### Scoring principle

Do not collapse governance into one opaque score. Use a scorecard:

```text
Compliance gate = policy result and severity thresholds
Quality score   = measured current state
Optimization    = ranked opportunity to improve
Fidelity        = trust in the change boundary
```

A report can have a high quality score but fail a mandatory corporate rule. A
report can pass mandatory policy while still having optimization opportunities.
The UI and CI contract must preserve that distinction.

## AI integration strategy

### Partner position

Microsoft AI becomes an upstream authoring provider and downstream delivery
ecosystem. The product is the independent review and governance checkpoint.

### Integration pattern

1. Microsoft Report Planner or Report Design produces a brief or planned report.
2. Microsoft Report Authoring, Power BI Desktop, Fabric CLI, or another agent
   produces PBIR/PBIP changes.
3. Design Governance imports the resulting surface and available semantic
   context.
4. The engine evaluates policy, analyzer profiles, accessibility, semantics,
   evidence, and fidelity.
5. Findings produce a review packet and CI/approval outcome.
6. AI may summarize findings or draft a remediation plan.
7. The Deterministic Remediation Engine previews and applies only explicitly
   supported, approved, validated changes.
8. The report is re-analyzed before it can pass the gate.

### Integration surfaces

- **PBIR/PBIP files:** primary provider-neutral inspection boundary.
- **Fabric CLI:** use for authenticated workspace discovery, retrieval,
  pipeline orchestration, and eventual result publication; do not duplicate
  Fabric item-management semantics.
- **Power BI MCP:** consume semantic-model and report metadata context where
  authorized; keep governance results independent of a specific model agent.
- **Report Design brief:** validate the brief before authoring where practical,
  then validate the emitted report against the same policy.
- **CI/CD:** run on pull requests or build environments already supported by
  Git, Fabric item APIs, or deployment pipelines.
- **AI clients/MCP:** expose read-only review and recommendation tools first;
  expose remediation only through preview/approval contracts.

### AI safety rules

- AI may explain a violation but cannot mark it compliant.
- AI may propose a fix but cannot bypass deterministic validation.
- AI must cite the finding and evidence that motivated each proposal.
- AI output must be distinguishable from analyzer facts.
- Policy changes require human ownership and versioned review.
- The system must work in a deterministic, non-AI mode for CI/CD.

## Mutation repositioning

Mutation is no longer a general editing catalog. It is a remediation mechanism
attached to a finding, rule, or approved optimization plan.

### Remediation lifecycle

```text
Finding -> eligibility check -> proposed change -> semantic preview
        -> human/CI approval -> deterministic apply -> schema validation
        -> fidelity/analyzer re-run -> compliance comparison
```

### Candidate remediation language

- “Fix all alignment violations on this page.”
- “Apply the approved corporate design standard.”
- “Normalize KPI placement across executive pages.”
- “Reduce visual density while preserving required KPIs.”
- “Add missing KPI context where the typed contract can prove the fields.”

These are product intents, not permission to add arbitrary JSON mutation. Each
must compile to a bounded set of typed operations with an explainable diff.

### Current RC mapping

- Rename Page → metadata normalization remediation.
- Add/Remove/Move Page → composition remediation.
- Move Visual → alignment/order remediation.
- Resize Visual → layout/density remediation.
- Existing analyzer-before/after → remediation acceptance evidence.
- Existing immutable snapshot/new artifact handle → safe change boundary.

Formatting, bindings, filters, navigation, slicers, and advanced interaction
mutations remain deferred until each has a closed typed ownership model and a
finding-linked acceptance contract.

## Enterprise scenarios

### Pull request validation

A PBIP pull request runs the analyzer against changed reports and the selected
policy. The check reports new high-severity violations, semantic diff, policy
version, evidence links, and whether the change is safe to merge.

### Build pipeline gate

An Azure DevOps, GitHub, or Fabric build invokes the deterministic evaluator.
The gate fails only on configured mandatory rules and emits machine-readable
results for the pipeline summary.

### Governance certification

A report owner requests certification. The product records the report
fingerprint, semantic-model context, policy version, scorecard, evidence,
approvals, waivers, and expiry. Certification is a governance record, not a
claim that the report is universally correct.

### Design approval

A consultant or BI review board receives a deterministic review packet with
findings grouped by page, standard, severity, and remediation state.

### Accessibility review

An accessibility profile runs independently or as a mandatory gate. It reports
evidence-backed findings and distinguishes metadata checks from rendered
visual evidence.

### AI output acceptance

An AI-generated report is treated like any other external change. The product
does not trust the authoring provider’s own validation as the organization’s
approval; it runs the selected policy and produces an independent result.

### Brand enforcement

An organization distributes a versioned brand policy for typography, themes,
colors, logos, and approved visual patterns. Exceptions are explicit, owned,
and expiring.

## Product editions

Licensing is not being implemented or decided here. A future packaging model
could be:

| Edition | Intended value | Boundary |
| --- | --- | --- |
| Community | Local report inspection, baseline analyzer, personal review | No organization policy distribution or hosted governance |
| Professional | Consultant and team review packets, custom profiles, remediation | Team policy and repeatable client delivery |
| Enterprise | CI/CD gates, policy registry, certification, audit, SSO/RBAC | Organizational governance and operational controls |
| Hosted | Central policy/evidence service and fleet reporting | Requires security, tenancy, retention, and execution architecture |
| Consulting | Branded standards, reusable delivery packs, client workspaces | Service-led packaging; not a separate technical core |

Recommendation: keep one policy/evidence core and vary distribution, workflow,
administration, and support. Do not fork analyzer logic by edition.

## Product roadmap by epics

Historical phase numbering ends with Phase 48. Future work should be promoted
only as a product epic with a user problem, policy boundary, evidence model,
and release acceptance criteria.

### Epic 1 — Design Governance Foundation

Objective: establish PDP, policy profiles, rule evaluation, stable compliance
results, findings linkage, exceptions, and a governance workspace.

Business value: turn the existing analyzer into an organizational quality gate.

Dependencies: current normalized findings, scoring configuration, analyzer
profiles, shared snapshot, current evidence primitives, and RC1 UAT.

Complexity: High.

Recommended order: first.

### Epic 2 — Design Review and Optimization

Objective: expand review from score reporting into cross-page consistency,
density, hierarchy, accessibility, semantic clarity, and ranked optimization.

Business value: reduce manual review time and make recommendations actionable.

Dependencies: stable rule/evidence IDs, policy profiles, visual evidence, and
clear distinction between objective violations and style preferences.

Complexity: High.

Recommended order: second, with only policy-backed slices promoted.

### Epic 3 — Deterministic Remediation

Objective: connect eligible findings to previewable, bounded remediation plans.

Business value: shorten the path from review to an approved, explainable fix.

Dependencies: Epic 1 policy IDs, current mutation planner, typed merge paths,
fidelity evidence, rollback/approval semantics, and fixture coverage.

Complexity: High.

Recommended order: parallel with late Epic 2 slices; never before policy
identity and finding linkage are stable.

### Epic 4 — AI Companion and Provider Adapters

Objective: integrate Microsoft design/planning/authoring outputs and other AI
providers as review inputs and advisory proposal sources.

Business value: make AI-generated reports safe and organization-ready without
competing with Microsoft’s authoring experience.

Dependencies: policy engine, provider-neutral PBIR boundary, read-only
integration contracts, prompt/proposal provenance, and deterministic fallback.

Complexity: High.

Recommended order: after the governance result contract is stable.

### Epic 5 — Enterprise Governance Operations

Objective: deliver pull-request checks, CI/CD gates, certification, approvals,
branding distribution, audit, waivers, and organization administration.

Business value: make report quality enforceable at enterprise scale.

Dependencies: policy registry, identity/RBAC, persistent evidence, Git/Fabric
integration, retention model, and operational security.

Complexity: Very high.

Recommended order: after a local governance workflow has repeatable adoption.

### Epic 6 — Ecosystem Platform

Objective: expose stable SDK/API/MCP and automation contracts for partners,
consultants, and developer tooling.

Business value: become the governance layer used by multiple authoring and
delivery ecosystems.

Dependencies: versioned public contracts, authentication, rate/tenant limits,
compatibility policy, and enterprise operations.

Complexity: Very high.

Recommended order: last.

## Existing technology mapping

| Current implementation | Repositioned role | Preserve? |
| --- | --- | --- |
| PbirScoringService and page scores | Quality measurement signal inside the governance scorecard | Yes; retain authority |
| Normalized findings | Shared policy violation and review issue contract | Yes; make rule/evidence linkage explicit |
| Analyzer registry and profiles | Governance analyzer/profile routing | Yes; separate profile from policy |
| Shared repository snapshots | Reusable evidence substrate | Yes; do not add analyzer-local rescans |
| PBIR reader and semantic projection | Surface inventory and policy evidence extraction | Yes; grow through schema-backed descriptors |
| pbir-ir/v1 | Canonical analysis/remediation context | Yes; do not make it a generic external JSON editor |
| Lossless authoring envelope | Fidelity and review trust boundary | Yes; source preservation remains authoritative |
| Deterministic serializer/validator | Compliance and remediation acceptance boundary | Yes |
| Mutation planner/executor | Finding-linked deterministic remediation | Yes; narrow and evidence-link |
| Preview/execute/new artifact handles | Safe remediation lifecycle | Yes |
| Score-panel workspace | Governance review workspace | Reframe labels and workflows |
| Evidence domains | Rule evidence and certification record | Yes; retain bounded screenshot semantics |
| Governance configuration | Policy/profile storage precursor | Simplify and version deliberately |
| AI proposal enrichment | Advisory explanation and remediation proposal layer | Yes; no mutation authority |
| Fabric App review foundations | Reusable multi-surface governance architecture | Potentially; preserve surface/analyzer/profile separation |
| Provider/runtime/security contracts | Future adapter and enterprise execution boundaries | Defer activation; do not let them dictate the product identity |

## Core platform versus legacy infrastructure versus deferred

### Core platform

- PBIR/PBIP surface discovery and bounded semantic projection.
- Shared snapshot and provenance model.
- Authoritative analyzer and normalized findings.
- Evidence collection and deterministic hashes.
- Policy evaluation boundary and profile routing.
- Semantic diff, fidelity, validation, and safe remediation lifecycle.
- Review workspace, review packet, and CI result model.

### Legacy or internal infrastructure

- Historical phase-specific generation request names and authoring-only UI
  language.
- Broad provider-execution scaffolding that is not needed for local governance.
- Checked-in target binaries and packaging-specific source artifacts.
- Any duplicate score or finding models that predate normalized findings.
- Generic authoring abstractions with no policy, evidence, or supported-surface
  consumer.

These should be classified and retired only after dependency analysis; this
strategy authorizes no deletion.

### Deferred

- General-purpose AI authoring.
- Semantic-model generation and DAX generation.
- Hosted execution and provider activation.
- Public SDK/API/MCP before the governance result contract stabilizes.
- Broad mutation batching and arbitrary authoring.
- Enterprise persistence, identity, licensing, and tenant administration.

## Naming recommendations

### Recommended naming system

| Layer | Recommended name | Why |
| --- | --- | --- |
| Product | Power BI Design Governance & Optimization | States the buyer outcome and avoids authoring competition |
| Short product name | Design Governance | Easy to say in enterprise workflows |
| Analyzer | Design Governance Engine | Describes evaluation, not a generic score |
| Policy language | Power BI Design Policy (PDP) | Clear policy-as-code identity; not tied to one vendor authoring tool |
| Review workspace | Governance Review | Directly describes the user task |
| Remediation | Deterministic Remediation Engine | Communicates bounded, explainable execution |
| Evidence record | Design Quality Record | Useful for approval, certification, and CI |

### Alternatives

- Power BI Quality Gate: strong CI message, narrower than the full product.
- Power BI Design Assurance: credible enterprise tone, less explicit about
  optimization and policy.
- Report Standards Engine: clear function, weaker association with Power BI
  design and review.
- Power BI Experience Governance: broader surface strategy, but less concrete
  for current PBIR evidence.

Recommendation: lead with Power BI Design Governance & Optimization and use
Power BI Quality Gate as the CI/CD product surface, not the corporate product
name.

## Marketing positioning

### Consultant message

“Deliver every Power BI report against a repeatable design standard. Review
pages, visuals, semantics, accessibility, and evidence in one deterministic
packet, then guide safe remediation.”

### Enterprise message

“Add an independent design-quality gate to Power BI development. Version your
standards, enforce them in review and CI/CD, track exceptions, and certify the
reports that meet your organization’s bar.”

### Microsoft partner message

“Make Microsoft’s Power BI Agentic authoring safer to adopt at scale. Use
Design Governance as the provider-neutral quality, policy, evidence, and
remediation layer around Fabric AI, PBIR, Desktop, and Fabric delivery.”

### Executive message

“AI can create reports quickly. Design Governance makes them consistent,
accessible, explainable, and ready for the business.”

### Short claims

- Design governance for AI-generated Power BI.
- Quality gates for enterprise Power BI.
- Policy-as-code for Power BI design.
- Independent review for every report authoring path.
- From report creation to report confidence.

Avoid claims such as “the AI Power BI author,” “the Power BI replacement,” or
“automatic perfect report design.”

## Technical debt register for the repositioning

### High priority

1. **No formal policy contract.** Existing scoring configuration is not yet a
   versioned organizational policy language with rule IDs, ownership,
   exceptions, and effective dates.
2. **Score/findings/policy separation is incomplete.** The product has strong
   findings and score infrastructure, but governance outcomes must not be
   inferred from a composite score.
3. **Public/internal mutation drift.** The backend contains broader typed
   mutation capability than the public picker. The new position requires a
   reviewed remediation-admission matrix.
4. **Evidence provenance needs a durable contract.** Policy hash, analyzer
   version, profile, report fingerprint, source identity, and evidence snapshot
   must travel together for CI and certification.

### Medium priority

5. **Generation-first language remains in contracts and UI.** It makes the
   product appear to compete with authoring even when the underlying components
   are useful governance infrastructure.
6. **Seven additive generation request versions are not a governance contract.**
   Preserve compatibility, but avoid adding versions solely to support the
   repositioning.
7. **Visual evidence and metadata evidence need a unified rule selector.**
   Screenshot evidence must remain bounded and distinct from Visual
   Intelligence while still linking to findings.
8. **Cross-surface governance is not complete.** The analyzable surface,
   analyzer, and profile architecture is promising; policy applicability and
   evidence parity across PBIR and Fabric App surfaces need explicit design.
9. **Lint and generated-artifact debt remain.** The RC carries a known lint
   baseline and packaging noise that reduce confidence in release automation.

### Low priority

10. **Terminology and documentation drift.** Historical phase and authoring
    terms should be mapped to the new product vocabulary.
11. **Edition boundaries are undefined.** This is product packaging work, not
    a reason to introduce licensing code now.
12. **Optimization value measurement is absent.** Future recommendations need
    measurable outcome signals, not only a higher score.

## Release recommendation

### Is RC1 Version 1.0 of the new product?

**No. RC1 is a strong technical alpha or design-partner foundation, not the
public Version 1.0 of Power BI Design Governance & Optimization.**

It already contains valuable foundations:

- authoritative scoring,
- normalized findings,
- design/evidence workflows,
- semantic projection,
- fidelity validation,
- deterministic diffs,
- immutable snapshots,
- bounded mutation planning, and
- a reusable analyzer/profile architecture.

It does not yet contain the product contract that makes those foundations a
governance platform:

- versioned PDP policies,
- rule-level compliance outcomes,
- policy ownership and exceptions,
- certification/audit records,
- CI/CD exit contracts,
- stable evidence provenance, and
- clear customer-facing governance terminology.

### Public messaging recommendation

Before public messaging, describe 0.6.0 as:

> **An early local PBIR design analysis and deterministic remediation preview,
> transitioning toward Power BI Design Governance & Optimization.**

Do not market it as a complete enterprise governance product until the first
governance foundation epic has produced a versioned policy, deterministic
compliance result, and repeatable review workflow.

### Strategic go/no-go

- **Go:** reposition the product now; the existing architecture supports the
  direction without being discarded.
- **No-go:** continue expanding generic authoring as the primary roadmap.
- **Gate:** approve the next product epic only after a policy/evidence design
  is accepted and the RC’s UAT confirms the current analyzer and remediation
  boundaries are trustworthy.

## Research limitations and assumptions

This strategy uses public Microsoft documentation and repository evidence
available on 2026-08-15. Microsoft preview features, CLI surfaces, and agent
skills can change. Statements about the absence of a capability in Microsoft’s
products are intentionally avoided; the strategy differentiates on independent
organizational policy, evidence, and governance rather than claiming exclusive
technical primitives.
