# Phase 37 — Incremental PBIR Authoring Design

## Goal

Expand the backend-only local PBIR generation provider from the Phase 36 one-page/one-card slice to a small additive authoring contract supporting multiple pages, multiple visuals, deterministic layout, and typed scalar/tabular bindings. The existing Phase 29 serializer, Phase 31 materialization path, and analyzer remain authoritative.

## Scope

Phase 37 supports exactly two visual types:

- `card`, with one or more measure bindings in the existing `Fields` role;
- `table`, with measure and dimension bindings in the existing `Fields` role.

The request supports ordered page and visual collections. Pages have stable caller-supplied identifiers and display metadata. Visuals have stable caller-supplied identifiers, a page reference, an explicit type, and a typed binding collection. Layout uses explicit numeric `x`, `y`, `width`, and `height` values plus a deterministic page grid. The provider validates the complete request before constructing IR or invoking materialization.

Charts, category/series/axis semantics, filters, interactions, themes, formatting, semantic-model generation, DAX authoring, RPC, VS Code commands, hosted execution, and Windows execution remain out of scope.

## Contract compatibility

The existing `local-pbir-generation-request/v1` record and its one-page/one-visual constructor remain valid. The provider keeps that constructor as a compatibility adapter that creates a one-page collection, one card visual, and one measure binding with the Phase 36 layout values and identity derivation. New collection properties are additive to the typed model and do not accept arbitrary JSON or scripts.

The provider contract version remains unchanged only if the existing serialized request shape can be deserialized without new required fields. The new collection form is represented by a new additive request contract version, `local-pbir-generation-request/v2`; v1 is normalized into the same internal authoring model before validation. Result and diagnostic contracts remain backward compatible, with Phase 37-specific diagnostic codes added only for new validation failures.

## Internal mapping

The provider introduces one private normalized authoring model, not a second persistence or serialization layer. It maps:

1. ordered request pages to `PbirIntermediateRepresentationPage` records;
2. ordered request visuals to `PbirIntermediateRepresentationVisual` records;
3. one semantic record per page containing the measure and dimension tokens used on that page;
4. page transitions from the ordered page list, with the first page as the deterministic landing page;
5. one layout container per page referencing visuals in visual order;
6. deployable semantic-model inventory entries and visual bindings using the existing Phase 29 request types.

Visual and binding identities are derived from the request id plus caller identifiers. Duplicate page ids, visual ids, binding ids, cross-page visual references, missing bindings, and unsupported visual/binding combinations fail closed before serialization. Existing canonical JSON, schema locks, hashes, lineage, and artifact ordering remain delegated to Phase 29.

## Layout rules

Layout is deterministic and intentionally limited:

- page canvas is `1280x720`;
- coordinates and sizes are non-negative integers;
- width and height are positive;
- each visual must fit inside the canvas;
- visuals on a page must not overlap;
- all visuals are sorted by `(y, x, visualId)` for automatic placement only when the request omits coordinates;
- explicit coordinates are preserved;
- the default spacing is an 8-pixel grid and alignment metadata is derived from the final coordinates;
- no pixel-perfect designer behavior, responsive behavior, or interaction geometry is inferred.

Layout validation is provider-level because it describes authoring intent; the serializer still validates the emitted PBIR layout representation.

## Bindings

Bindings are typed records with a stable binding id, role, field kind, semantic token, entity, and property. The provider accepts:

- `measure` bindings for `card` and `table`;
- `dimension` bindings for `table`.

Card requests must contain at least one measure and must not contain dimensions. Table requests must contain at least one binding and may contain measures and dimensions. All fields must resolve to the explicit semantic-model inventory created for the request. No calculated expressions, DAX, implicit measures, or inferred model metadata are generated.

## Round trip and determinism

Generation is successful only when Phase 29 returns a valid artifact and Phase 31 materializes it, the existing project loader resolves it, and `PbirScoringService` observes the requested page and visual counts. The provider never calculates or edits analyzer scores.

The same request produces byte-identical artifact files and equal artifact, manifest, file-set, and lineage hashes. Request ordering is meaningful and preserved; no current-time or random identifiers are read. The only intentional compatibility exception is that a v1 request is normalized to the equivalent v2 internal shape before hashing, while the generated artifact identity remains Phase 36-compatible.

## Testing and performance

Focused xUnit coverage will exercise v1 regression, v2 contract normalization, two-page/multi-visual generation, card scalar bindings, table tabular bindings, page/visual ordering, layout success and failure, duplicate identifiers, unsupported visuals, unsupported bindings, schema validation, analyzer round-trip, repeated hashes, and no-partial-artifact rejection. A representative test records generation, materialization, and analyzer elapsed times for documentation; it is observational and imposes no performance threshold.

