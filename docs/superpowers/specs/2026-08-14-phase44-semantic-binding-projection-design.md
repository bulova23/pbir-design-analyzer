# Phase 44 Semantic Binding Projection and Full Round-Trip Fidelity

## Objective

Complete imported semantic binding projection for the existing supported PBIR visual families so imported reports can flow through the same semantic binding model used by generation, mutation, serialization, and analyzer comparison. Phase 44 extends the Phase 43 reader boundary; it does not redesign the authoring envelope or add a public execution surface.

## Approved architecture

```text
Imported PBIR
    -> reader
    -> query-state role extraction
    -> Phase 40/41 descriptor catalog
    -> existing IR binding records
    -> mutation planner/executor
    -> Phase 43 merge service and serializer
    -> analyzer
```

`PbirIntermediateRepresentationBinding` remains the only imported and generated semantic binding representation. The descriptor catalog remains the authoritative vocabulary. The reader does not create import-only descriptors, role records, or raw-query semantic fallbacks.

## Descriptor projection contract

Each supported visual is resolved through the existing catalog before a query-state projection becomes typed IR:

| Visual | Descriptor-supported imported roles | Projection policy |
| --- | --- | --- |
| Card | Value/Fields, plus descriptor-supported tooltip metadata | Project only roles represented by the descriptor; preserve other query JSON in the envelope |
| Table | Value/Values | Project supported value fields with their measure/dimension kind |
| Clustered Column Chart | Category, Value, Axis, Legend, Tooltip where the catalog maps them | Use descriptor role mapping; reject unknown or ambiguous role mappings from typed projection |
| Line Chart | Category, Value, Series, Axis, Legend, Tooltip where mapped | Preserve projection order and source references |
| Bar Chart | Category, Value, Axis, Legend, Tooltip where mapped | Use the same descriptor path as other Cartesian charts |
| Pie Chart | Legend/Category, Value, Tooltip where mapped | Preserve the imported semantic role; do not reinterpret an unsupported role |
| Slicer | Category | Use the existing Phase 41 slicer descriptor and require a dimension category for typed projection |

The catalog may receive narrow metadata needed to express import aliases or serializer role names, but every addition must be justified by repository fixtures or serializer output. No role is synthesized merely because a visual family commonly uses it.

## Diagnostics and fail-closed behavior

The reader emits structured diagnostics for missing fields, unknown roles, unsupported descriptor roles, ambiguous aliases, invalid role/kind combinations, and conflicting projections. Diagnostics distinguish `Projected`, `PreservedButUntyped`, `Unsupported`, and `Invalid` outcomes through stable codes/messages or an equivalent typed diagnostic classification added to the existing diagnostic contract.

For schema-admitted visual documents, unsupported query-state properties remain preserved by the Phase 43 authoring envelope. They are excluded from typed IR, cannot be targeted by typed mutation, and never get silently reinterpreted. A visual with an invalid required typed projection remains blocked rather than producing a misleading binding.

## Semantic equivalence

Analyzer comparison uses normalized shared IR semantics rather than raw imported JSON. Two bindings are equivalent when all of the following match:

- visual identity and visual family;
- descriptor role;
- measure versus dimension kind;
- entity and property reference;
- normalized token/reference identity;
- projection order where order is semantically meaningful for the role.

Role aliases are equivalent only when the descriptor explicitly maps them to one canonical role. JSON property order, whitespace, serializer formatting, and unrelated envelope properties are not semantic differences. A changed title, layout, formatting property, or binding is intentional only when the mutation plan identifies that path; unrelated semantic changes are regressions.

## Fidelity and analyzer evidence

Round-trip evidence keeps the Phase 43 distinctions separate: byte-identical, authoring-identical, semantic-equivalent, intentionally-changed, and unsupported. Imported analyzer-before/after comparison is computed from the shared IR/analyzer pipeline. The evidence reports unchanged semantics, requested mutation deltas, unexpected semantic regressions, and unsupported semantic domains without treating analyzer output as mutation authority.

## Timing

The import/mutation pipeline exposes deterministic stage durations for reader/import, semantic projection, authoring merge, mutation planning, mutation execution, serialization, and analyzer evaluation. Measurements are observations for representative report sizes; Phase 44 does not add a performance threshold or optimization layer.

## Testing and fixtures

Focused tests cover every supported visual family, descriptor-based projection, role/kind validation, unsupported-role envelope preservation, semantic equivalence, analyzer-before/after comparison, mutation isolation, fidelity classification, deterministic timing shape, and Phase 43 regression behavior. Representative fixtures use repository-owned generated PBIR artifacts and schema-admitted JSON; no external skill/prompt or autonomous execution path is imported.

## Reader/serializer asymmetries and remaining limits

The reader projects only the semantic fields currently represented by the descriptor catalog and shared IR. Formatting, themes, filters, navigation metadata, and other Phase 43 opaque fields remain envelope-preserved unless an existing typed mutation contract supports them. Bookmarks, drillthrough, shared slicers, semantic-model/DAX generation, RPC, and new visual families remain out of scope. Any remaining asymmetry is documented as unsupported or intentionally normalized rather than hidden by serializer workarounds.
