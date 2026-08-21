Bottom line

Recommendation: pause net-new feature development and run a pre-v1.0 consolidation release.

The repository has substantial functionality and unusually broad automated test coverage, but it is accumulating architectural and release-process debt faster than the product surface warrants. The most immediate blocker is simpler: the current main commit has failing CI on all three operating systems, so it is not presently release-ready.

The next engineering investment should be:

1. Restore a green, reproducible release pipeline.
2. Break apart the scoring and score-workspace monoliths.
3. Separate shipped product code from experimental/provider architecture.
4. Introduce one generated, versioned contract between C# and TypeScript.
5. Add real-fixture and packaged-extension acceptance tests.
6. Only then resume roadmap epics.

A new screenshot-intelligence feature will not improve the situation if the release pipeline cannot ship it. Software remains stubbornly literal about these things.

Review boundary

Current repository evidence

- Repository: bulova23/pbir-design-analyzer
- Branch: main
- Reviewed commit: a67af7ad88bf2e7d6fd4bc162be84731d5ae1390
- Repository was inspected read-only from a fresh shallow clone.
- Approximate source composition:
  - C#: 411 files, approximately 96,600 lines
  - TypeScript: 252 files, approximately 56,400 lines
  - TSX: 21 files, approximately 13,800 lines
  - Markdown: 634 files, approximately 98,300 lines
  - Test files identified: approximately 125 C# and 114 TypeScript/TSX files
- Consulting-AI-Memory was queried for repository.pbir-design-analyzer; it returned no admissible historical evidence. Conclusions below therefore rely on current repository and public GitHub evidence.

Scope status

- No signed scope or formal v1.0 acceptance baseline was found.
- All items below are classified as recommended improvements, not contracted requirements.
- Product roadmap items are proposed/deferred work unless explicitly implemented and verified.

Ranked findings

1. HIGH — main is currently failing CI

Confidence: Confirmed

The latest GitHub Actions run for the reviewed commit failed:

- Run: 32268663692
- Commit: a67af7ad88bf2e7d6fd4bc162be84731d5ae1390
- Ubuntu backend tests: failed
- Windows backend tests: failed
- macOS backend tests: failed
- Extension builds were skipped after the backend failures.
- The packaging job was consequently skipped.

The workflow runs backend tests before building the extension:

- .github/workflows/ci.yml:36-41

Impact

- Current branch readiness is NOT READY.
- Cross-platform claims are not supported by the latest execution evidence.
- The same failure on three platforms suggests a shared test/code/configuration problem rather than an isolated runner issue.

Recommended enhancement

Create a short stabilization workstream:

1. Retrieve and classify the exact failing backend tests.
2. Reproduce them locally in a supported .NET 8 environment.
3. Fix or explicitly quarantine only demonstrably environment-dependent tests.
4. Require green CI before merging further feature work.
5. Add branch protection requiring all matrix jobs and packaging verification.

I could not reproduce the backend suite in this sandbox because dotnet is not installed. That is a local review-environment limitation; the GitHub failure is independent evidence.

---

2. HIGH — Core scoring and UI components have become monoliths

Confidence: Confirmed

Largest production source files include:

- service-dotnet/Services/Pbir/PbirScoringService.cs: approximately 9,997 lines
- vscode-extension/webview-src/analyzer-score/App.tsx: approximately 4,573 lines
- vscode-extension/webview-src/analyzer-score/styles.css: approximately 2,453 lines
- service-dotnet/Services/Discovery/RecommendationEngineService.cs: approximately 2,567 lines
- service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs: approximately 1,973 lines
- vscode-extension/src/analyzer/contracts/scorePanel.ts: approximately 1,287 lines
- vscode-extension/src/views/scoreResultPayload.ts: approximately 1,118 lines
- vscode-extension/src/views/PbirScorePanel.ts: approximately 920 lines

App.tsx contains scoring-format helpers, issue filtering, readiness rendering, story assessment, evidence, fix-plan behavior, rendered review, and workspace composition in one module. PbirScorePanel similarly coordinates scoring, audit, export, fixes, persistence, navigation, protocol routing, and presentation assembly.

Impact

- Small changes have a large regression radius.
- Merge conflicts and AI-generated duplication will increase.
- Reviewers cannot easily distinguish domain rules from presentation rules.
- Tests can pass while the component architecture continues deteriorating.
- These files will become materially harder to maintain over the next 6–12 months.

Recommended enhancement

Refactor before adding another workspace section.

C# target decomposition

Turn PbirScoringService into orchestration over explicit stages:

- report loading and normalization
- page structural analysis
- visual analysis
- accessibility analysis
- storytelling analysis
- cross-page analysis
- score calculation
- finding creation
- result assembly and diagnostics

Each stage should consume and return immutable typed models. Preserve scoring outputs through golden characterization tests before moving logic.

React target decomposition

Split App.tsx into bounded feature modules:

- OverviewWorkspace
- IssuesWorkspace
- FixPlanWorkspace
- StoryAssessmentWorkspace
- RenderedReviewWorkspace
- EvidenceWorkspace
- ReadinessWorkspace
- shared formatting and label utilities
- hooks/reducers for filters, expansion, navigation, and selected-page state

Do not merely move 4,000 lines into 40 arbitrary files. Decompose by domain ownership and state boundary.

---

3. HIGH — The shipped backend contains extensive architecture that is not part of the current product’s primary execution path

Confidence: High

The public product is positioned around PBIR/Fabric review, but the core project also compiles a very large Services/Discovery architecture containing:

- provider registries and compatibility services
- Microsoft Skills abstractions
- runtime providers
- execution planning
- generation manifests
- preview serializers and writers
- deployable materialization
- Phase 35A–35I trust, sandbox, certification, and remote-worker concepts
- report generation phases
- mutation and authoring contracts

PbirDesignAnalyzer.Core.csproj:18-26 excludes selected host, test, tool, and platform files, but otherwise compiles the broad service tree into one core assembly.

The roadmap itself describes many components as backend-only, deferred, inert, not enabled, or requiring separate authorization. See docs/ROADMAP.md:79-133.

Impact

- The shipped binary has a much larger maintenance and security review surface than the current user-facing product.
- Experimental architecture can become an accidental public contract.
- Phase-number naming preserves implementation history rather than communicating domain ownership.
- New engineers must understand a platform-sized architecture to change a report analyzer.
- Dead or dormant abstractions can create a false sense of future-proofing while increasing present complexity.

Recommended enhancement

Establish explicit product assemblies:

- PbirAnalyzer.Domain
- PbirAnalyzer.Scoring
- PbirAnalyzer.Authoring
- PbirAnalyzer.Transport
- PbirAnalyzer.Experimental or a separate research repository

Then:

1. Produce a dependency graph from the actual composition root.
2. Identify code unreachable from shipped commands and RPC routes.
3. Move non-shipped provider/sandbox research out of the runtime assembly. 
4. Replace phase-number namespaces with durable domain names where the code remains active.
5. Publish a small “supported runtime surface” manifest.
6. Add architecture tests preventing scoring from depending on authoring, provider, or experimental layers.

The goal is not deletion for sport. It is ensuring the shipping analyzer does not carry an architectural museum in its backpack.

---

4. HIGH — Release documentation, packaging, and automation disagree

Confidence: Confirmed

There are several current contradictions:

1. The README says the release contains five packages, including Windows ARM64:
   - README.md:208-216

2. The release workflow matrix contains only four targets:
   - .github/workflows/release.yml:28-34
   - Missing win32-arm64

3. The README says not to rely on repository publication automation:
   - README.md:226-235

4. The repository has an automated tag release and Marketplace publication workflow:
   - .github/workflows/release.yml:1-22
   - .github/workflows/release.yml:149-163

5. The README links docs/RELEASING.md:
   - README.md:243
   - That path does not exist. The apparent release guide is docs/current-state/RELEASING.md.

6. GitHub currently reports no published GitHub releases.

7. Open issue #4 says Marketplace publisher/PAT setup remains unfinished.

Impact

- A tag release may omit a supported platform.
- Operators cannot determine whether manual or automated publishing is authoritative.
- Release instructions lead to a nonexistent file.
- Marketplace and GitHub artifact sets can diverge.
- This is exactly the sort of problem that is uneventful until release day, when it becomes very eventful.

Recommended enhancement

Create one authoritative release contract:

- Exact supported target matrix
- Framework-dependent versus self-contained policy
- Required artifact names
- Version synchronization rules
- Test and smoke-test gates
- Signing/checksum policy
- GitHub Release procedure
- Marketplace procedure
- Rollback procedure

Then make both CI and documentation consume that shared target definition. Add Windows ARM64 to the release matrix or explicitly remove it from the supported release set. Do not maintain five-target claims and four-target automation simultaneously.

---

5. HIGH — There is no verified end-to-end acceptance path using representative PBIR reports

Confidence: High

The repository has extensive unit and component tests, but current evidence shows several acceptance gaps:

- AGENTS.md:124-125 says UI changes should receive a quick local VS Code smoke check, but CI does not perform one.
- CI runs backend tests, build, extension Jest tests, and packaging; it does not install a VSIX and score a representative report.
- Historical project notes repeatedly state that no real PBIR fixture was configured for automated UI verification.
- docs/CHANGELOG.md:94-116 acknowledges that true virtual-workspace runtime behavior was not proven.
- Real fixture testing is opt-in through PBIR_REAL_FIXTURE_PATH, not part of the standard release gate.
- Current GitHub CI is already failing before any packaged acceptance test can run.

Impact

The test suite can prove internal functions while missing:

- extension/backend handshake failures
- packaged backend resolution problems
- real Power BI Desktop schema variation
- report size and traversal performance
- actual VSIX asset omissions
- webview message-routing failures
- preview/apply/rollback behavior against realistic projects
- cross-platform determinism on identical input

The repository history itself documents bugs that escaped unit coverage at precisely these seams.

Recommended enhancement

Build a sanitized, versioned fixture corpus:

- small standard PBIR report
- multi-page report
- hidden pages and navigation
- bookmarks and interactions
- custom visual/Deneb/HTML cases
- multiple Power BI Desktop schema versions
- malformed and partially unsupported report
- large report for performance
- mutation-safe copy for preview/apply/rollback
- Fabric App sample repository

Release acceptance should include:

1. Install each generated VSIX on its matching platform.
2. Start the packaged backend without repository build artifacts.
3. Score the same fixture.
4. Compare fingerprint, score, findings, evidence count, and readiness output.
5. Exercise one supported mutation and rollback.
6. Export a review packet.
7. Archive diagnostics and checksums as release evidence.

---

6. MEDIUM-HIGH — Cross-language contracts are broad and manually synchronized

Confidence: High

The score contract is spread across:

- C# result models, including service-dotnet/Services/Pbir/Models/ScoreResult.cs
- vscode-extension/src/analyzer/contracts/scorePanel.ts
- vscode-extension/src/views/scoreResultPayload.ts
- vscode-extension/src/views/scorePanelProtocol.ts
- React rendering code
- authoring RPC contract models

The TypeScript ScoreResult concept appears throughout the extension. The repository has added version metadata and runtime payload guards, which is good, but broad manually duplicated contracts remain.

Impact

- Additive backend changes can silently drift from TypeScript assumptions.
- Optional-field accumulation makes it difficult to know what is required by each analyzer or surface.
- One “universal” result object risks becoming a bag of every feature ever added.
- Compatibility becomes test-based rather than mechanically enforced.

Recommended enhancement

Adopt schema-first contracts:

- JSON Schema or an equivalent language-neutral IDL
- generated C# and TypeScript DTOs
- checked-in compatibility fixtures
- explicit protocol versioning policy
- additive/minor versus breaking/major rules
- unknown-field tolerance tests
- supported-version negotiation during initialization

Prefer a stable envelope with analyzer-specific payloads rather than continuously expanding one universal ScoreResult.

---

7. MEDIUM-HIGH — Tracked generated backend artifacts increase repository and supply-chain risk

Confidence: Confirmed

The repository tracks approximately 261 files under vscode-extension/backend/targets/, including DLLs and platform executables. The shallow clone’s Git pack is approximately 34.67 MiB.

The README calls these generated snapshots and says they should not be manually edited:

- README.md:280-307

However, they remain checked in and are also rebuilt during packaging.

Impact

- Source review is mixed with opaque binary review.
- Diffs can contain large generated changes.
- It is harder to prove which source produced each committed binary.
- Dependency and malware scanning become less straightforward.
- Developers may accidentally package stale artifacts.

Recommended enhancement

Move generated targets to CI artifacts or versioned release assets.

For reproducibility:

- pin .NET SDK through global.json
- pin Node through .nvmrc, .node-version, or Volta
- record package-lock and NuGet lock information
- generate SBOMs
- produce SHA-256 checksums
- attest build provenance
- verify the VSIX contains binaries created in the same workflow
- prohibit packaging from pre-existing target directories

If checked-in binaries must remain temporarily, add a manifest mapping each artifact to source commit, target RID, SDK version, and checksum.

---

8. MEDIUM — Roadmap and current-state documentation are stale or internally inconsistent

Confidence: Confirmed

Examples:

- Root README says current release is 0.7.0.
- docs/ROADMAP.md:1-37 still frames the current state around 0.6.0.
- The roadmap repeatedly describes already implemented phases alongside deferred phases in one long historical sequence.
- docs/CHANGELOG.md:96-107 says the 0.6.0 validation produced packages that still had version 0.5.0.
- .agent-memory/repo-map.md:53 still describes the roadmap as post-0.2.0.
- The current-focus file is over 4,000 lines and nearly 300 KB, despite the repository guidance preferring compact durable memory.

Impact

- Planning documents no longer reliably describe current implementation.
- New contributors must infer authority from dates and context.
- Historical phase documentation overwhelms the current product architecture.
- AI agents are especially likely to treat stale prose as current design authority.

Recommended enhancement

Split documentation into:

- docs/architecture/current.md — authoritative current runtime architecture
- docs/product/scope.md — supported user-facing surface
- docs/roadmap.md — only active and next approved epics
- docs/history/ — completed phase narratives
- docs/releasing.md — authoritative release procedure
- generated command/configuration reference from package.json

Add a docs validation test for broken internal links, inconsistent current version references, supported platform matrix, and missing referenced files.

---

9. MEDIUM — Quality gates do not include lint, coverage policy, dependency review, or security scanning

Confidence: Confirmed

Available scripts include lint:

- vscode-extension/package.json:270-293

But .github/workflows/ci.yml does not run npm run lint.

No evidence was found in the current workflows for:

- coverage thresholds
- dependency review
- CodeQL or equivalent static security analysis
- secret scanning configuration
- SBOM generation
- signed release provenance
- VSIX vulnerability scanning

The repository does correctly declare untrusted and virtual workspaces unsupported:

- vscode-extension/package.json:19-27

That is a good trust-boundary decision, but it does not replace release security controls.

Recommended enhancement

Add gates in this order:

1. ESLint with zero-new-warning policy.
2. TypeScript compile as a standalone named check.
3. Coverage reporting with ratcheted thresholds for core scoring, mutation, and protocol code.
4. npm audit/dependency review with an explicit severity policy.
5. NuGet vulnerability audit.
6. CodeQL for C# and TypeScript.
7. SBOM and checksums for each VSIX.
8. Artifact provenance/attestation.
9. Secret-scanning and release-token governance.

Avoid adopting a dozen badges and calling it security. The useful part is enforced failure policy.

---

10. MEDIUM — Product scope is broadening faster than its primary workflow is consolidating

Confidence: High

The product currently includes or plans:

- PBIR scoring
- story assessment
- issue triage
- fix planning
- deterministic mutations
- rendered review
- screenshot evidence
- Fabric App readiness
- Fabric App review
- AI proposal enrichment
- Report Design Studio
- local PBIR generation and materialization
- consultant exports
- visual intelligence
- enterprise governance

The roadmap itself identifies “UX Architecture Consolidation” as strategically important and notes duplication, fragmented reasoning, and excess scroll depth:

- docs/ROADMAP.md:257-283

Impact

- Users may struggle to understand the primary job-to-be-done.
- Every new capability expands the shared result, protocol, UI, test, and documentation surfaces.
- The product risks becoming several partially integrated tools rather than one strong review workflow.

Recommended enhancement

For v1.0, define one primary workflow:

Discover → Score → Review evidence → Prioritize → Preview safe fixes → Apply/rollback → Export

Classify everything else:

- Core
- Optional module
- Experimental
- Deferred
- Unsupported

I would prioritize workflow consolidation and consultant deliverables over advanced screenshot intelligence or additional authoring capabilities. Deliverables build directly on existing review output and offer clearer adoption value with less architectural risk.

---

11. LOW-MEDIUM — Telemetry abstraction is currently a no-op

Confidence: Confirmed
vscode-extension/src/telemetry/reporter.ts:14-53 describes privacy-respecting telemetry but sendEvent intentionally performs no transmission or storage.

Impact

- Call sites imply observability that does not exist.
- The abstraction adds conceptual surface without operational value.
- Product decisions cannot actually use the events.

Recommended enhancement

Choose explicitly:

- Remove event instrumentation until a real privacy-reviewed telemetry design exists, or
- Implement a documented opt-in/VS Code-compliant telemetry pipeline with a published data dictionary.

Do not leave a no-op reporter looking like production observability.

Recommended implementation sequence

Phase 0 — Release stabilization

Gate: Green CI and one reproducible package set.

- Resolve current cross-platform backend test failures.
- Correct release target mismatch.
- Fix broken release-guide links.
- Decide manual versus automated Marketplace publication.
- Add branch protections.
- Publish checksums for generated artifacts.

Phase 1 — Characterization and architecture boundaries

Gate: Existing scoring outputs remain unchanged for the fixture corpus.

- Establish sanitized real-report fixtures.
- Capture golden score diagnostics.
- Document supported RPC and result contracts.
- Map actual runtime dependencies.
- Identify dormant and experimental code.

Phase 2 — Refactor high-risk monoliths

Gate: No score, finding, or mutation behavior changes without explicit approval.

- Decompose PbirScoringService.
- Decompose score-panel React App.tsx.
- Reduce PbirScorePanel to orchestration.
- Generate C#/TypeScript contracts from a shared schema.
- Add architecture tests.

Phase 3 — Packaged acceptance and determinism

Gate: Every supported target passes packaged smoke tests.

- Install target VSIX.
- Start packaged backend.
- Score identical fixture.
- Compare deterministic diagnostics.
- Execute preview/apply/rollback.
- Export review output.
- Archive test evidence.

Phase 4 — Product enhancement

Recommended first enhancement: Consultant Deliverables and Export Platform, but only after Phases 0–3.

Why:

- Existing export foundations are already present.
- It provides visible user and consulting value.
- It does not require expanding mutation authority.
- It is less risky than visual-intelligence or enterprise-governance expansion.

Readiness assessment

Current status: NOT READY for a v1.0 release

Supporting evidence

- Current main CI fails on Ubuntu, Windows, and macOS.
- Release target automation omits a platform claimed in product documentation.
- Release guidance is contradictory and includes a broken path.
- No current packaged end-to-end acceptance evidence was found.
- Real-fixture validation remains optional or historical.
- Major production modules are already beyond maintainable single-file scale.

Positive evidence

- Strong unit/component test investment.
- Explicit advisory-versus-deterministic mutation boundary.
- Versioned score-panel protocol and payload validation.
- Unsupported untrusted/virtual workspace posture is declared.
- Cross-platform determinism is treated as a design requirement.
- Mutation preview/apply/rollback safety receives significant attention.

The foundation is credible. The issue is not lack of engineering effort; it is that the repository now needs consolidation, runtime proof, and release discipline more than another layer of capability.