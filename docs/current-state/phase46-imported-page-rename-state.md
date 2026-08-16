# Phase 46 — Imported Page Rename Current State

Status: **IMPLEMENTED — DIRECT TYPED BACKEND CAPABILITY** on 2026-08-14.

Phase 45 selected the existing typed backend providers as the authoring
boundary. The demonstrated callers are backend orchestration and backend
tests. The current direct path already supports generation v1–v7 and imported
visual resize through the hybrid envelope, one copy-on-write merge boundary,
pinned-schema validation, deterministic serialization, stable identities,
fidelity evidence, and analyzer separation.

The bounded Phase 46 capability is implemented through the existing
`LocalPbirMutationRequest/v1` model, planner, executor, and single
copy-on-write authoring merge boundary. `RenamePage` is typed-and-mergeable for
an imported page when its pinned page document exposes exactly one string
`displayName` owner. The merge changes only that property and preserves the
imported folder/name identity, unrelated page properties, visuals, ordering,
interactions, and other admitted source content. Formatting, theme, filter,
navigation, slicer, binding, and other non-layout domains remain preserved but
not authorable or unsupported according to the existing inventory.

Invalid, missing, unknown, duplicated, or unsupported page targets and empty
display names fail closed with typed diagnostics. Deterministic ordering and
serialization, pinned-schema admission, fidelity evidence, stable identities,
and analyzer separation remain on their existing boundaries. No new request
version, façade, schema, or generic JSON mutation surface was introduced by
this bounded capability. RPC and VS Code integration remain separate
concurrent work and are not part of the direct typed caller boundary.

The implementation plan is:

- `docs/superpowers/specs/2026-08-14-phase46-imported-page-rename-design.md`
- `docs/superpowers/plans/2026-08-14-phase46-imported-page-rename.md`

Focused Phase 46 coverage passes for successful rename, target/name validation,
stable identity, unrelated content preservation, deterministic repeated output,
fidelity, and unsupported page-owner rejection. The intentionally deferred
cases remain page-folder rename, cross-process transport, snapshot/concurrency
contracts, and all other mutation domains.
