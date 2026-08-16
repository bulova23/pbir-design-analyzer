# Roadmap Correction: First PBIR Generation

Date: 2026-08-13

Status: architecture review and roadmap correction only

## Executive summary

The roadmap should be substantially restructured around the shortest evidence-based path to the first production-quality PBIR generation capability.

The repository's canonical v1 product objective is PBIR/PBIP report analysis, not generation. The v1 specification explicitly includes local PBIP selection, PBIR discovery, tree inspection, scoring, and governance checks, and explicitly excludes creating PBIR reports, semantic-model authoring, and TMDL workflows. That is the product already delivered.

The later generation roadmap is legitimate adjacent work, but Phases 35G–35L became the active branch before a real generation provider existed. The decision that likely future provider behavior would require Power BI Desktop, and therefore Windows remote execution, is an architectural assumption. It is not supported by an executed provider, Desktop validation run, PBIR generation failure, or repository requirement.

The shortest path is therefore:

1. Freeze 35G–35L as future execution infrastructure and remove them from the critical path.
2. Use the existing Phase 29 serializer and Phase 30–34 local materialization path as the base for a narrow local generation provider.
3. Prove one supported, deterministic, schema-validated modern PBIR report-definition slice end to end on macOS.
4. Add generated-artifact intake and Analyzer handoff only after an artifact can actually be produced.

This is not a claim that the current serializer is already production-complete. It is the closest existing implementation to that milestone. The largest current blocker is the absence of a real generation provider that supplies complete supported input and returns a deployable PBIR artifact through the existing local path.

## Evidence versus recommendation

### Verified repository facts

- `docs/PBIR_ANALYZER_V1_SPEC.md` defines the v1 product as local PBIP/PBIR report analysis. Its in-scope workflows are discovery, inspection, scoring, and governance; creating PBIR reports is out of scope.
- The original seven-phase roadmap places PBIR generation in original Phase 4, after design-package consumption, prompt generation, and generation-request planning. Analyzer handoff, refinement, and Fabric App generation are later phases.
- Repository Phase 29 emits a validated in-memory modern PBIR inventory containing `definition.pbir` and the `definition/` hierarchy for a deliberately narrow subset.
- Repository Phase 30 writes that validated report-definition inventory locally with preview, exact-byte staging, promotion, rollback, and recovery controls.
- Repository Phases 31–34 expose and consume that local path, but Phase 34 documents that preview is unavailable until an authorized upstream producer supplies the canonical input.
- The Generation Provider Framework is metadata-only. It explicitly is not a PBIR generator, provider runtime, API surface, CLI runner, deployment path, or mutation workflow.
- Phase 35B has no normal executable provider; its fake adapters are test-only. Phase 35C–D are offline assurance and certification foundations.
- Phase 35F rejected the currently evaluated local macOS containment mechanisms. No provider was executed.
- Phase 35G selected `remote-controlled-execution/v1` for a future provider boundary. The ADR describes the likely future provider as Power BI Desktop-dependent; it does not demonstrate that the first provider is Desktop-dependent.
- Phase 35H is an inert in-process protocol proof. Phase 35I is a Windows containment implementation. Phase 35J and Phase 35K are validation planning and test implementation. Phase 35L was blocked because no certified Windows worker was available. No generation provider has run on Windows or anywhere else.

### Architectural assumptions

- A future provider will need Power BI Desktop.
- The first useful generation path will require Windows.
- The first generation path will execute untrusted external code rather than use the existing deterministic local serializer/materializer.
- Windows Job Objects and restricted tokens are prerequisites for the first PBIR artifact.

These assumptions may become valid for a later Microsoft Skills, Desktop automation, or hosted provider. They are not current product requirements and are not proven blockers for a local deterministic PBIR report-definition provider.

## Original product objective

The original v1 objective is analysis. The strongest repository evidence is the canonical v1 specification:

- Purpose: local Power BI PBIP/PBIR report analysis.
- User job: open a local PBIP project, inspect structure, score a report or page, tune scoring, and evaluate governance readiness.
- Explicit exclusions: creating PBIR reports, semantic-model browsing and authoring, and TMDL editing workflows.

The repository also contains a later seven-phase generation roadmap. That roadmap is a planned expansion, not evidence that generation was the original shipped product objective. The current README likewise positions the product around review, findings, remediation planning, evidence, and advisory/deterministic fixes.

## Timeline review

| Point | Repository event | Evidence status | Assessment |
|---|---|---|---|
| v1 | PBIR Analyzer product specification | Direct product evidence | Analysis is the primary objective; generation is out of scope. |
| Original Phases 1–3 | Consumption, prompts, requests, planning/readiness | Implemented planning/review evidence | Supporting foundation for future generation; not an executed generator. |
| Phases 21–28 | Reference generator, IR, preview, local writer boundary, review and readiness | Implemented deterministic/review evidence | Mostly research and supporting infrastructure; reference output is explicitly not a PBIR project. |
| Phase 29 | Modern PBIR serializer | Direct artifact evidence | First real deployable-PBIR-producing engine, but narrow, in-memory, and input-bound. |
| Phase 30 | Safe local materialization | Direct filesystem evidence | First local publication path for the Phase 29 artifact; no provider or PBIP/model generation. |
| Phases 31–34 | Orchestration, RPC adapter, VS Code workflow | Direct local integration evidence | Makes the existing path consumable, but does not create the upstream canonical generation input. |
| 35A–35D | Provider contracts, runtime composition, trust, certification | Offline contract evidence | Future-provider infrastructure; no provider execution. |
| 35E–35F | macOS containment probes/evaluation | Negative local-enforcement evidence | Shows the evaluated macOS process boundary is not authoritative; does not show Windows is needed for PBIR generation. |
| 35G | Remote Windows architecture decision | Design evidence only | The Desktop/Windows premise is explicitly future-facing and probabilistic. |
| 35H | Remote protocol proof | In-process inert proof | Proves typed protocol behavior only; no real worker or provider. |
| 35I–35K | Windows containment implementation and suite | Compile/test-discovery evidence only | No Windows enforcement execution evidence. |
| 35L | Certified Windows execution gate | Blocked environment gate | No Windows run, no failure measurement, and no remediation evidence. |

The roadmap changed direction at Phase 35G. The supporting evidence was Microsoft Desktop's Windows platform requirement plus the repository's anticipation of a future Desktop-dependent provider. The missing evidence was a provider implementation or a local PBIR generation experiment showing that Windows/Desktop was required for the first artifact.

## Was Windows ever a requirement?

Repository evidence does not establish Windows execution, Power BI Desktop execution, Desktop automation, Job Objects, or a remote Windows worker as a mandatory requirement for the existing analyzer or for the first deterministic PBIR report-definition artifact.

Those are requirements only for specific future choices, such as Desktop automation or an untrusted external provider that cannot run safely on the developer workstation. They should be capability- or provider-specific constraints, not global roadmap prerequisites.

The repository does establish one narrower fact: the selected Phase 35G future boundary targets Windows first because a likely future provider may depend on Power BI Desktop. That is a valid future design option, not a proven product requirement.

## PBIR reality check

Current Microsoft documentation supports the following distinctions:

- PBIP stores report and semantic-model items as folders. PBIR is the report format and TMDL is the semantic-model format. The `.pbip` file is a pointer to the report folder and is optional when opening a report through `definition.pbir`.
- PBIR is publicly documented and supports manual or programmatic batch changes from non-Power BI applications. Microsoft provides public JSON schemas, and Power BI Desktop validates changed PBIR files when opening them.
- PBIR and PBIR-Legacy are distinct and mutually exclusive report-definition representations. Modern PBIR uses the `definition/` folder plus `definition.pbir`; PBIR-Legacy uses root-level `report.json`.
- Microsoft currently documents creating or converting a project to PBIR through Power BI Desktop preview features. Converting PBIX to PBIP, or PBIP back to PBIX, is also documented as a Desktop Save As operation and not as a programmatic conversion.
- Microsoft documents external editing of supported project files, including TMDL metadata, without launching Desktop. Changes to files already open in Desktop require a restart before Desktop sees them. External edits can still cause errors or model inconsistencies, so validation remains necessary.

The practical boundary is therefore:

| Capability | Cross-platform file implementation | Desktop required or valuable |
|---|---|---|
| Analyze existing PBIR/PBIP files | Yes; this is the shipped product | No |
| Programmatically modify supported PBIR files | Yes, according to Microsoft's public PBIR documentation | Desktop open/reload is valuable for validation |
| Emit a narrow PBIR report-definition folder | Technically possible and already partially implemented by Phase 29–30 | Desktop validation/open is valuable; not proven as a hard execution prerequisite |
| Create/convert PBIP through Desktop Save As or PBIX conversion | Not established by repository code | Yes, documented Desktop workflow |
| Author a complete semantic model | TMDL is file-based and externally editable, but repository does not generate it | Desktop or another validated model-authoring path may be required for some scenarios |
| Publish/deploy to Fabric | Not present in the repository | Requires a separately authorized Fabric/API/Desktop deployment path |
| Execute an untrusted external provider | Not present and not proven locally | May justify Windows/remote containment later |

Microsoft sources: [PBIR enhanced report format](https://learn.microsoft.com/en-us/power-bi/developer/embedded/projects-enhanced-report-format), [Power BI Project overview](https://learn.microsoft.com/en-us/power-bi/developer/projects/projects-overview), [Power BI project report folder](https://learn.microsoft.com/en-us/power-bi/developer/projects/projects-report), [Fabric report definitions](https://learn.microsoft.com/en-us/rest/api/fabric/articles/item-management/definitions/report-definition), and [TMDL view and PBIP](https://learn.microsoft.com/en-us/power-bi/transform-model/desktop-tmdl-view).

The documentation describes PBIR as preview in the cited pages. That increases the need for pinned schemas, compatibility tests, and an explicit supported subset; it does not convert Desktop into a universal prerequisite for every file-level PBIR operation.

## Architecture assessment

### Did 35I–35L solve a real blocker?

They solved a real future-runtime assurance problem: if the repository eventually executes an untrusted Windows-dependent provider, process containment and evidence will matter. They did not solve the current product blocker because no such provider exists, no provider has failed due to Windows absence, and the Windows suite itself has not executed.

The long-term architecture risk is sequencing. The repository built certification, remote protocol, and Windows containment around a hypothetical provider before proving the provider's smallest useful artifact path. This increases maintenance cost, creates public-contract surface without a live consumer, and makes a blocked environment gate appear to be the next product milestone.

### Phase classification

| Completed work | Category | Rationale |
|---|---|---|
| Original Phases 1–3 and the scoring/review workspace | Core Product Capability | Directly delivers the analyzer's stated v1 job. |
| Phases 15–20 planning/readiness/certification seams | Supporting Infrastructure | Makes future planning and review safer but does not create artifacts. |
| Phase 21 reference generator | Research | Deterministic planning proof; its output is explicitly not a PBIR project. |
| Phase 22 IR | Supporting Infrastructure | Canonical internal representation for future serializers/providers. |
| Phases 23–28 preview, local planning, review handoff, and readiness | Supporting Infrastructure | Review and safety surfaces; intentionally non-deployable. |
| Phase 29 serializer | Core Product Capability for the generation expansion | First engine that emits modern PBIR artifact bytes, though only for a narrow subset. |
| Phase 30 materializer | Supporting Infrastructure | Safely publishes Phase 29 output locally. |
| Phases 31–34 orchestration, transport, adapter, and UI workflow | Supporting Infrastructure | Exposes the local path without supplying generation input. |
| 35A–35D provider governance and certification | Future Infrastructure | No executable provider consumes it. |
| 35E–35F macOS containment evaluation | Research | Evaluates enforcement options and records a negative result. |
| 35G–35H remote decision and inert protocol proof | Future Infrastructure / Research | Future execution boundary proof, not product generation. |
| 35I–35K Windows containment implementation and tests | Future Infrastructure | Provider-specific containment with no live provider and no Windows evidence. |
| 35L | Deferred | Blocked execution gate; no product capability was added. |

## Value assessment

The phases that directly increase the ability to produce a PBIR artifact today are Phase 29 and Phase 30. Phase 31–34 increase consumability and user workflow value, but only after a caller can supply the complete canonical input. The repository itself documents that this upstream producer is missing.

The phases that primarily prepare future execution environments are 35A–35L. They do not increase the current ability to produce a PBIR artifact because they do not serialize PBIR, create semantic-model files, invoke a provider, or run a Desktop validation loop.

The largest missing capability is therefore a real, narrow generation provider or equivalent local generation entry point that feeds complete supported input into Phase 29 and returns its output through Phase 30/31. It is not Windows validation.

## Proven requirements

- Preserve the analyzer's existing scoring and review contracts.
- Keep PBIR generation separate from scoring and presentation.
- Generate only a declared modern PBIR subset with explicit semantic bindings and deterministic layout.
- Validate the complete artifact against pinned schemas and cross-reference rules before publication.
- Preserve exact-byte, preview/apply/rollback, lineage, and hash guarantees already supplied by Phases 29–31.
- Keep unsupported intent fail-closed rather than inventing semantic bindings or layout.
- Re-analyze generated output using the existing analyzer after materialization.
- Keep generated-artifact intake and Analyzer handoff downstream of actual generation.

## Future enhancements

These may be valuable, but none should block the first local generation milestone:

- Power BI Desktop open/load verification.
- PBIP wrapper and complete semantic-model/TMDL generation.
- Microsoft Skills or other external provider execution.
- Windows worker containment and hosted execution.
- network isolation, stronger filesystem isolation, image attestation, credentials, and production artifact scanning.
- Fabric API deployment and publishing.
- broad visual coverage, themes, filters, bookmarks, mobile state, composite models, and advanced semantic-model features.

## Corrected roadmap

Do not rewrite the historical record of Phases 35G–35L. Reclassify them as future execution infrastructure and stop making them the critical path.

Use the next existing roadmap slot, Repository Phase 36, for:

**First Local PBIR Generation Provider**

Scope:

- consume the existing provider-neutral generation request or the smallest explicit local host input that can be validated without widening contracts unnecessarily;
- map only the already supported Phase 29 subset;
- supply complete semantic-model inventory and explicit visual-role bindings;
- invoke the existing deterministic serializer and Phase 30/31 local materialization boundary;
- produce one real PBIR report-definition fixture on macOS;
- run the existing analyzer against the generated output;
- document the exact unsupported subset and the artifact acceptance criteria.

Recommended future sequence, using existing phase numbers rather than adding phases:

1. Repository Phase 36: first local PBIR generation provider and end-to-end generated report-definition proof.
2. Repository Phase 37: generated-artifact intake, quarantine, schema validation, and Analyzer handoff.
3. Repository Phase 38: original Phase 5 Analyzer handoff completion and validation loop.
4. Repository Phases 39–40: original Phase 6 refinement loop.
5. Repository Phases 41–43: original Phase 7 Fabric target mapping, generation, review, deployment, and publishing as separately authorized slices.
6. Repository Phase 44: release hardening and packaging.
7. Deferred milestone after the first artifact: Windows Validation / Hosted Execution for providers that actually demonstrate a Windows or Desktop dependency.

This reordering does not add a phase or authorize implementation. It changes the critical-path interpretation of the existing future slots.

## Deferred Windows work

Keep 35G–35L as a clearly labeled future milestone: **Windows Validation and Hosted Execution**. Resume it when a concrete provider demonstrates at least one of the following:

- it cannot produce or validate the supported artifact on macOS;
- it requires Power BI Desktop automation;
- it executes untrusted external code whose containment cannot be met locally;
- it requires a production worker/image/credential boundary.

At that point, Windows containment becomes evidence-driven provider infrastructure. Until then, Phase 35L's unexecuted suite is not a product blocker.

## Next recommended goal

Implement the first narrow local PBIR generation provider using the existing Phase 29–31 boundaries, after a separate implementation authorization.

This is the smallest step that materially advances the product because it converts an already schema-validated serializer/materializer into a user-observable generation capability, produces the first artifact that can be re-analyzed, and tests the actual provider contract. It also creates concrete evidence for deciding whether Desktop, Windows, remote execution, or additional containment is needed.

No provider should be advanced to execution, no Windows prerequisite should be reinstated, and no new security layer should be added until this local path has established a measured capability gap.

## Review conclusion

The shortest evidence-based path is not “finish Windows containment, then discover the provider.” It is “prove the smallest local PBIR generation capability, then add only the execution environment required by the measured provider.”
