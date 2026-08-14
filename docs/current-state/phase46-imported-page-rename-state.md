# Phase 46 — Imported Page Rename Current State

Status: **DESIGN COMPLETE; IMPLEMENTATION NOT STARTED** on 2026-08-14.

Phase 45 selected the existing typed backend providers as the authoring
boundary. The demonstrated callers are backend orchestration and backend
tests. The current direct path already supports generation v1–v7 and imported
visual resize through the hybrid envelope, one copy-on-write merge boundary,
pinned-schema validation, deterministic serialization, stable identities,
fidelity evidence, and analyzer separation.

The bounded limitation selected for Phase 46 is that imported page display-name
rename is represented by the existing `LocalPbirMutationRequest/v1` model and
typed executor, but has no page-document merge path. The operation is therefore
currently unsupported for imported authoring. Formatting, theme, filter,
navigation, slicer, binding, and other non-layout domains remain preserved but
not authorable or unsupported according to the existing inventory.

Phase 46 will add only the typed imported `RenamePage` path. It will preserve
the page folder identity and all unrelated admitted content, and will fail
closed when the pinned schema does not expose one unambiguous page display-name
owner. It will not add RPC, VS Code, a façade, a new request version, or a
generic JSON mutation surface.

The implementation plan is:

- `docs/superpowers/specs/2026-08-14-phase46-imported-page-rename-design.md`
- `docs/superpowers/plans/2026-08-14-phase46-imported-page-rename.md`

Phase 46 remains not implemented until a separate execution goal is approved.

