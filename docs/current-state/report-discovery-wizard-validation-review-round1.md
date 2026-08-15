# Report Discovery Wizard Validation Review – Round 1

Date: 2026-06-19

## Scope

This review validates the completed Discovery Wizard workflow in the current branch state before any Microsoft Power BI Skills or CLI integration planning.

In scope:

- Discovery Profile
- Opportunity Catalog
- Recommendation Engine
- Experience Blueprint generation
- Design Studio seeding
- Design Package generation
- trust-boundary and provenance behavior

Out of scope:

- product-code changes
- feature additions
- architecture changes
- provider integration work

## Method

Validation used:

- current discovery implementation in `service-dotnet/Services/Discovery/`
- current discovery and Design Studio tests in `service-dotnet/tests/Discovery/` and `vscode-extension/src/test/discoveryDesignStudioSeed.test.ts`
- current roadmap and implementation-plan documents
- required repo validation commands

Validation passed:

- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- `cd vscode-extension && npm test`
- `cd vscode-extension && npm run compile`

Review posture:

- architecture-first
- output-quality-first
- long-term maintainability-first

## 1. Scenario Walkthroughs

### Scenario A – Revenue / Sales Model

Expected flow:

- Discovery Profile reliably identifies revenue measures, geography, date readiness, and KPI clusters.
- Opportunity Catalog produces executive and sales-performance directions.
- Recommendation Engine tends to favor Executive Dashboard or PBIR Report variants.
- Blueprint output produces a leadership summary plus revenue, territory, and possibly customer-analysis pages.

Assessment:

- This is the strongest scenario in the current implementation.
- The recommendations are credible and aligned with the original vision of “I have a semantic model. What should I build?”
- The weakness is that the blueprint stays generic. “Executive Summary”, “Revenue Performance”, and “Territory Performance” are useful, but they still read like templates rather than consultant-shaped report concepts.

### Scenario B – Customer Profitability Model

Expected flow:

- Discovery identifies customer and profitability signals.
- Opportunity Catalog produces Customer Profitability Analysis.
- Recommendation Engine prefers Analytical Investigation Experience if that category is present.
- Blueprint output uses analytical pages and variance-style KPI emphasis.

Assessment:

- The opportunity is credible.
- The experience-type choice is directionally reasonable, but somewhat rigid. Some customer-profitability models would be better served first as a PBIR report or executive report with drill paths rather than defaulting toward analytical investigation.
- Consultant quality is moderate, not high. The recommendation is useful, but the system currently treats profitability analysis as a fixed pattern more than a design judgment.

### Scenario C – Inventory / Operations Model

Expected flow:

- Discovery identifies inventory, warehouse, product, quantity, and date signals.
- Opportunity Catalog produces Inventory Operations Monitoring.
- Recommendation Engine prefers Operational Monitoring Experience.
- Blueprint produces Overview, Exceptions, and Detail.

Assessment:

- This is another strong scenario.
- The outcome is credible, understandable, and commercially useful.
- The analytical flow is coherent and the experience-type selection is believable.
- The weakness is again genericity: the operational package is sensible but still closer to a reusable template than a consultant-specific operating model.

### Scenario D – Service Operations Model

Expected flow:

- Discovery identifies service domain signals, technician/work-order hints, and operational audience clues.
- Opportunity Catalog produces Service Operations Dashboard.
- Recommendation Engine prefers Operational Monitoring Experience.
- Blueprint again produces Overview, Exceptions, and Detail.

Assessment:

- The recommendation is credible.
- The business outcome is reasonable.
- The system handles service operations as a valid second operational pattern.
- The weakness is insufficient differentiation from inventory. Service operations and inventory operations currently converge too quickly onto the same experience skeleton.

### Scenario E – Analytical Investigation Model

Expected flow:

- Discovery identifies analytical audience signals, relationship depth, hierarchy richness, and variance/root-cause cues.
- Opportunity Catalog produces Root Cause Analysis Experience.
- Recommendation Engine prefers Analytical Investigation Experience.
- Blueprint produces Question, Investigation, Evidence, and Conclusion.

Assessment:

- This scenario proves the workflow can recommend something other than a conventional dashboard.
- The recommended analytical flow is conceptually correct.
- The experience feels the most “designed” of the current shapes.
- The weakness is that the decision logic is still heuristic-heavy and thin on true semantic reasoning, so the confidence can appear stronger than the underlying evidence warrants.

## 2. Discovery Observations

- Discovery Profile captures meaningful basics well: measures, dimensions, hierarchies, date readiness, relationships, domains, KPI clusters, audience signals, ambiguity notes, and a top-level confidence.
- Ambiguity notes are useful and appropriately explicit.
- Confidence is understandable, but not fully believable as a consultant-grade trust signal.

Strengths:

- The profile does not hide ambiguity.
- Sparse models degrade explicitly.
- The output is internally coherent and easy to consume downstream.

Weaknesses:

- Audience inference is optimistic. Executive can be inferred from page labels or simply from revenue/profitability domain presence, and Analytical can be inferred from only relationship or dimension count (`service-dotnet/Services/Discovery/SemanticModelDiscoveryService.cs:312`-`339`).
- Confidence is a simple additive score based on metadata richness and ambiguity count (`service-dotnet/Services/Discovery/SemanticModelDiscoveryService.cs:377`-`430`). That makes it readable, but not deeply trustworthy.
- Domain and role detection are almost entirely name-based. This is useful for MVP speed, but brittle for real semantic models with inconsistent naming.

Overall judgment:

- Discovery Profile is useful.
- Ambiguity notes are better than the confidence signals.
- Confidence is acceptable for advisory use, not yet strong enough for consultant-grade authority.

## 3. Recommendation Observations

- Opportunities are credible for the five required scenarios.
- Business outcomes are reasonable and generally well-phrased.
- Audience inference is directionally right, but sometimes mechanically inferred rather than truly supported.

Strengths:

- The catalog covers the intended scenario set.
- The Top 3 plus 2 Alternates structure is well-bounded.
- Diversity and near-duplicate collapse are a good architectural choice (`service-dotnet/Services/Discovery/RecommendationEngineService.cs:61`-`181`).

Weaknesses:

- Opportunity generation is a single hard-coded heuristic catalog (`service-dotnet/Services/Discovery/OpportunityIdentificationService.cs:16`-`24`, `30`-`258`). That will be difficult to evolve cleanly in 6-12 months as the taxonomy expands.
- Recommended experience type is chosen by category defaults more than by nuanced evidence (`service-dotnet/Services/Discovery/RecommendationEngineService.cs:234`-`257`).
- “Why we recommend it” is mostly templated supporting-signal text, not a real consultant narrative.
- Alternates are bounded and usually different enough, but not consistently strategic alternatives. They are often adjacent template variants rather than meaningfully different routes.

Overall judgment:

- The recommendations are useful and mostly credible.
- They are not yet consistently consultant-quality.
- The engine is good enough to guide exploration, not yet good enough to be treated as a strong design advisor.

## 4. Blueprint Observations

- Suggested pages usually make sense for the chosen experience type.
- KPI, filter, and navigation outputs are coherent.
- Analytical flow is the strongest part of the blueprint layer.

Strengths:

- Each supported experience type emits a stable, understandable shape.
- Operational and analytical templates are especially coherent.
- The blueprint is concrete enough to seed Design Studio.

Weaknesses:

- The page system is heavily template-driven (`service-dotnet/Services/Discovery/ExperienceBlueprintGenerationService.cs:199`-`260`).
- KPI selection is partly real and partly fallback injection such as “Open Exceptions”, “Backlog Trend”, “Resolution Rate”, or “Revenue / Gross Margin / YoY Growth” (`service-dotnet/Services/Discovery/ExperienceBlueprintGenerationService.cs:130`-`159`).
- Global filters are chosen from a fixed preferred list before falling back to the first available dimensions (`service-dotnet/Services/Discovery/ExperienceBlueprintGenerationService.cs:161`-`197`).
- PBIR Report, Executive Dashboard, Fabric App, and Fabric Data App are differentiated, but not yet deeply differentiated. The blueprint layer understands shape better than it understands product semantics.

Overall judgment:

- Blueprints are useful.
- They are not yet rich enough to be called consultant-quality design blueprints across all scenarios.

## 5. Design Studio Seeding Observations

- Design Brief generation makes conceptual sense.
- Concept candidates and Draft seed structure are coherent.
- The seeded artifacts respect Design Studio ownership and approval boundaries.

Strengths:

- The seeded brief uses audience, objective, KPI, dimensions, story, success criteria, report type, navigation expectations, cadence, risks, and evidence domains.
- The seeded concept and draft structures are specific enough to start Design Studio work.
- The trust boundary is preserved: no approval bypass, no validation approval, no deployable asset, no mutation authority.

Weaknesses:

- Backend lineage is not truly preserved. The backend adapter synthesizes semantic-model and discovery-profile references from the first measure name and selected recommendation id instead of preserving real source identifiers (`service-dotnet/Services/Discovery/DiscoveryDesignStudioAdapterService.cs:65`-`83`).
- Extension-side test seeding uses a stronger provenance shape with real input ids, which means the conceptual model is stronger than the backend implementation seam today.

Overall judgment:

- Design Studio seeding works conceptually.
- Provenance preservation is only partially credible in the backend implementation.

## 6. Design Package Observations

- The Design Package is structurally complete and understandable.
- It is provider-neutral and advisory-only.
- It is sufficient as a planning handoff object, not yet sufficient as a high-confidence generation contract.

Strengths:

- The package contains audience, personas, experience definition, pages, KPIs, filters, visuals, navigation, analytical flow, success criteria, rationale, and provenance.
- The package remains cleanly separate from Design Studio artifacts.
- The package does not widen public contracts and does not introduce provider authority.

Weaknesses:

- The package mostly rephrases blueprint content instead of materially enriching it.
- Persona and page-navigation descriptions are generic and formulaic (`service-dotnet/Services/Discovery/DesignPackageGenerationService.cs:109`-`149`, `213`-`220`).
- Provenance again uses synthetic semantic-model and discovery-profile ids rather than preserving true upstream identities (`service-dotnet/Services/Discovery/DesignPackageGenerationService.cs:78`-`97`).

Overall judgment:

- The Design Package is useful.
- It is not yet a fully trustworthy seam for downstream provider execution planning unless provenance and semantic richness improve.

## 7. Trust-Boundary Observations

Strengths:

- Discovery remains advisory-only.
- Design Studio remains the owner of downstream design approvals.
- Analyzer Workspace validation ownership is preserved.
- Provider-neutrality is maintained.

Weaknesses:

- Provenance quality is weaker than the architecture intends because synthetic lineage ids are minted inside backend seeding and packaging.
- Experience-type selection can look more authoritative than the evidence model really supports.
- The workflow is implemented as internal services and tests, but not yet as a clearly surfaced first-class product workflow. That limits “understandability” from a real user posture even though the internal architecture is coherent.

Overall judgment:

- The major trust boundaries are preserved.
- Provenance credibility is not yet strong enough.

## 8. Readiness Assessment

### Comparison to the original vision

Does the workflow answer:

> I have a semantic model. What should I build?

Assessment:

- Yes, directionally.
- It does this better than the old “I already know what report I want” posture.
- The output is most convincing for executive sales, inventory operations, service operations, and analytical investigation.
- It is less convincing where semantic models could support multiple legitimate experience shapes and the engine currently selects a category-default path.

### Final questions

1. Is the Discovery Wizard understandable?
   - Internally yes, externally only partly. The architecture is understandable, but the productized wizard experience is not yet clearly surfaced.

2. Are recommendations consultant-quality?
   - Not consistently. They are useful and credible, but still too template-driven.

3. Are blueprints useful?
   - Yes.

4. Does Design Studio seeding work conceptually?
   - Yes.

5. Is the Design Package useful?
   - Yes, as a provider-neutral planning handoff.

6. Is the experience-type selection credible?
   - Credible for core scenarios, but too rigid for mixed or ambiguous models.

7. What weaknesses remain?
   - heuristic-heavy discovery and audience inference
   - category-default experience selection
   - generic blueprints and package phrasing
   - synthetic provenance ids in backend lineage
   - limited differentiation among some experience types

8. Is the Discovery Wizard MVP complete?
   - Functionally close, but not quality-complete.

9. Is it ready for Microsoft Skills / CLI integration planning?
   - Not yet. Provider integration should not be the next step until output credibility and provenance quality improve.

### Decision Gate

Recommendation:

- **B. Requires Additional Discovery Work**

Rationale:

- The workflow is architecturally sound enough to continue.
- The outputs are useful enough to prove the concept.
- The current quality bar is not yet high enough for consultant-grade recommendations or for a trustworthy provider handoff seam.

## 9. Long-Term Risk Findings

Ranked by long-term risk:

1. Synthetic provenance in backend seeding and packaging weakens trust and future traceability.
   - This conflicts with the repository emphasis on lineage, versioned boundaries, and advisory trust semantics.

2. Opportunity and experience selection logic is concentrated in hard-coded heuristics.
   - This will become difficult to maintain as taxonomy breadth and model diversity increase.

3. Blueprint and package outputs are too template-driven to scale as consultant-quality design guidance.
   - Future improvements will likely require richer semantic interpretation, not more fallback text.

4. Confidence and audience signals overstate certainty relative to the real evidence model.
   - This is a credibility risk when provider integration makes outputs feel more “official”.

5. Some experience types are insufficiently differentiated.
   - Fabric App, PBIR Report, and Executive Dashboard still overlap too much in the current blueprint layer.

## 10. Bottom Line

The Discovery Wizard is a credible internal MVP slice and a sound architectural foundation.

It is not yet ready to be treated as a consultant-grade recommendation system or as the stable upstream handoff for Microsoft Skills or CLI integration.

The next work should improve:

- provenance fidelity
- heuristic maintainability
- experience-type differentiation
- blueprint specificity
- confidence realism

before provider integration begins.
