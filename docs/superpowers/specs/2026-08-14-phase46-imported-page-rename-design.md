# Phase 46 — Imported Page Rename Authoring Design

Status: **DESIGN MILESTONE SELECTED; IMPLEMENTATION NOT STARTED** on 2026-08-14.

## Decision

Phase 46 will add one bounded backend-only authoring capability: **rename the
display name of an existing imported page while preserving its folder
identity, page content, visual identities, ordering, interactions, and other
admitted source properties**.

This is the smallest valuable increment supported by the evidence. The closed
`LocalPbirMutationRequest` v1 model already contains `RenamePage`, a page
target, and `displayName`; `PbirMutationExecutor` already represents the typed
IR change. The missing capability is the imported page-document merge path.
The current inventory intentionally classifies `RenamePage` as unsupported,
while only `ResizeVisual` is typed-and-mergeable. Phase 46 closes that one
gap end to end.

The intended callers remain backend orchestration and backend tests through the
existing direct typed services. No RPC route, transport adapter, VS Code
workflow, façade, or public API is part of this milestone.

## Scope

For a valid pinned-schema imported report, accept exactly one or more typed
`RenamePage` operations when each operation has:

- `Target.PageId` naming an existing imported page;
- a non-empty, normalized `DisplayName`; and
- no request-selected JSON path, JSON fragment, or opaque replacement.

The merge service changes only the page-owned display-name property that the
existing page serializer/schema mapping owns. It retains the imported page
folder/name identity and copies every unrelated admitted property from the
source envelope. Equivalent operations are deterministically ordered by the
existing planner and produce equivalent canonical output.

The existing `ResizeVisual` path remains supported. All other imported
mutation domains remain preserved-but-not-authorable or unsupported according
to the current inventory.

## Non-goals

- No RPC registration, JSON-RPC method, transport contract, or process boundary.
- No VS Code command, UI state, extension change, or cross-process caller.
- No new generation request version; generation v1–v7 remains unchanged.
- No mutation request version change unless implementation evidence proves v1
  cannot express the selected operation.
- No page folder rename, page identity replacement, arbitrary JSON editing,
  JSON Patch/Pointer, or caller-selected source property.
- No formatting, theme, filter, navigation, slicer, binding, bookmark,
  drillthrough, visual replacement, page add/remove, or cross-page interaction
  authoring.
- No scoring, analyzer, schema-lock, materialization, Desktop, Windows,
  hosted-execution, or provider-runtime redesign.

## Typed contract and data flow

The existing typed contract is sufficient:

```text
LocalPbirMutationRequest/v1
  operations[*].kind       = RenamePage
  operations[*].target.pageId = imported page ID
  operations[*].displayName = new page display name
```

The direct backend flow is:

```text
LocalPbirMutationProviderService.Import
  -> PbirLocalReportReader
  -> typed IR + pinned-schema authoring envelope
LocalPbirMutationProviderService.Plan
  -> accept RenamePage only when a page owner and typed merge path exist
LocalPbirMutationProviderService.Execute
  -> copy-on-write typed page display-name overlay
PbirAuthoringMergeService
  -> replace only the owned display-name path in the cloned page document
existing serializer/validator/fidelity/analyzer boundaries
  -> ready artifact plus mutation evidence
```

`LocalPbirGenerationProviderService` is unchanged and remains the direct
generation caller for v1–v7 requests.

## Outputs

The existing `LocalPbirMutationResult` v1 remains authoritative. A ready
result contains the existing artifact, manifest, pinned-schema validation,
lineage/hash evidence, changed page identity, changed authoring path, fidelity
classification, and analyzer-before/after evidence where the current provider
path supplies them. No new wire response is introduced.

The changed-path evidence must identify the concrete page display-name path
used by the pinned page schema mapping. The imported page identity and folder
path must appear in preserved identity evidence, not in changed identity
evidence.

## Failure behavior

Fail closed with the existing typed mutation diagnostics and no ready artifact
when any of these occurs:

- invalid request schema, missing mutation ID, empty display name, or missing
  page target;
- source import is invalid, blocked, outside the pinned schema inventory, or
  lacks an unambiguous page owner;
- page target is unknown, duplicated, or collides with an imported identity;
- the selected page owner has no supported display-name property mapping;
- the merge would alter folder identity or any non-owned source property;
- final serialization, pinned-schema validation, structural/reference/hash
  validation, fidelity, or analyzer admission fails.

The operation must not silently fall back to rebuilding the whole page. An
unsupported imported page shape remains preserved-but-not-authorable and is
reported as a typed diagnostic.

## Boundary invariants

- One copy-on-write merge boundary remains the only imported-source mutation
  authority.
- Typed page identity/display-name intent is separate from opaque preserved
  JSON; opaque content is never a mutation input.
- The pinned schema lock controls source admission and final validation.
- Deterministic serialization and hashes remain the output authority.
- Imported page folder identity and visual identities remain stable.
- Analyzer/scoring consumes the existing semantic model and reports evidence;
  it does not authorize or alter the mutation.
- Phase 42 same-page interaction records remain preserved and semantically
  equivalent.
- Generation v1–v7 and all existing backend, extension, and RPC behavior remain
  compatible.

## Test strategy

Add focused backend tests around the existing discovery test project:

1. A generated/imported fixture with one page, multiple visuals, a slicer
   interaction, and an admitted extra page property accepts `RenamePage`.
2. The output display name changes, while page folder identity, visual paths,
   ordering, interactions, bindings, and the extra property remain unchanged.
3. Repeating the same typed request produces equivalent canonical output and
   stable hashes.
4. Empty/whitespace names, missing/unknown targets, duplicate identity, wrong
   schema, unsupported page owner, and preservation conflicts fail without a
   ready artifact.
5. `UpdateTheme` and another preserved-but-not-authorable operation remain
   rejected, proving the boundary did not become a generic authoring surface.
6. Existing `ResizeVisual`, generation v1–v7, Phase 42, Phase 44, fidelity,
   analyzer, and serializer regressions remain green.

## Acceptance criteria

Phase 46 is complete only when:

1. `RenamePage` is the sole newly classified imported operation and has one
   tested typed merge path.
2. A valid imported page can be renamed through the direct typed backend
   provider without RPC or extension involvement.
3. The output passes pinned-schema, structural, cross-reference, hash, and
   existing analyzer boundaries.
4. The page folder identity, visual identities/order, Phase 42 interactions,
   and unrelated admitted properties are preserved.
5. Fidelity evidence reports only the intended page display-name change.
6. Invalid, ambiguous, unsupported, and preservation-conflict cases fail
   closed with no ready artifact.
7. Equivalent requests are deterministic and generation v1–v7 is unchanged.
8. Documentation and repository validation pass with no new lint baseline.

## Risks and deferred decisions

The main risk is that PBIR page display-name ownership differs across admitted
page document shapes. The implementation must use the pinned schema mapping
and reject shapes without one unambiguous owned path; it must not infer a path
from arbitrary JSON.

Deferred until a demonstrated caller requires them: the first cross-process
authoring operation; RPC method and transport details; snapshot/path and
concurrency contracts; cancellation/timeouts; extension workflow ownership;
page folder rename; multi-operation transaction semantics; richer page
metadata; and all other preserved-but-not-authorable mutation domains.

