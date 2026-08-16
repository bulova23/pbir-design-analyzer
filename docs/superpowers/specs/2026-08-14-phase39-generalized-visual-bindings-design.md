# Phase 39 Generalized Visual Bindings Design

## Executive Summary

Phase 39 adds an additive `local-pbir-generation-request/v4` contract. A binding now has a strongly typed role in addition to its existing measure/dimension kind. The provider carries these role-bearing bindings into the shared intermediate representation and maps them to the existing Phase 29 serializer roles.

The supported visual catalog is Card, Table, and Clustered Column Chart. The chart requires exactly one Category dimension and one or more Value measures. Card and Table remain compatible with the Phase 36–38 records and generated output. The implementation remains backend-only and advisory: no RPC, VS Code, hosted execution, Windows execution, semantic-model generation, DAX generation, or mutation authority is added.

## Binding Matrix

| Binding kind | Binding roles in the contract | Compatible visuals in Phase 39 |
| --- | --- | --- |
| Measure | Value | Card, Table, Clustered Column Chart |
| Dimension | Value | Table, for backward-compatible direct field tables |
| Dimension | Category | Clustered Column Chart |
| Measure | Series, Axis, Legend, Tooltip | Reserved; rejected for the Phase 39 chart |

Role names are enum values, not free-form strings. Projection order is one-based and contiguous within each serializer role. The chart maps Category to `Category` and Value to `Y`; Card maps Value to `Fields`; Table maps its direct fields to `Values`.

## Visual Catalog

| Visual | Required bindings | Supported formatting |
| --- | --- | --- |
| Card | At least one existing direct Measure binding in v1–v3; v4 Value Measure | Existing title, label, number format, box, and alignment support |
| Table | At least one existing direct Measure or Dimension binding in v1–v3; v4 direct fields | Existing title, subtitle, row/header, alternate row, number format, width, and box support |
| Clustered Column Chart | Exactly one Category Dimension and at least one Value Measure | Title, axis-label visibility, legend visibility, background, and deterministic data colors |

The chart uses the existing deterministic layout profile, report/page/visual authoring, schema validation, materialization, lineage, hashing, and analyzer round-trip.

## Generated Example

The representative v4 request creates two pages: Overview contains a Card for Revenue and a Table for Region and Revenue; Detail contains a Clustered Column Chart with Region as Category and Revenue as Value.

```json
{
  "schemaVersion": "local-pbir-generation-request/v4",
  "pages": ["overview", "detail"],
  "visuals": [
    {"visualType": "card", "bindings": [{"role": "Value", "kind": "Measure", "token": "Revenue"}]},
    {"visualType": "table", "bindings": [{"role": "Value", "kind": "Dimension", "token": "Region"}, {"role": "Value", "kind": "Measure", "token": "Revenue"}]},
    {"visualType": "clusteredColumnChart", "bindings": [{"role": "Category", "kind": "Dimension", "token": "Region"}, {"role": "Value", "kind": "Measure", "token": "Revenue"}]}
  ]
}
```

## Validation Boundaries

Invalid role cardinality, duplicate chart Category roles, wrong field kinds, unsupported chart roles, duplicate identifiers, unsafe paths, layout overlap, serializer role mismatch, schema mismatch, hash mismatch, and lineage mismatch fail closed. Phase 36–38 request records and overloads remain unchanged.

## Phase 40 Recommendation

Extend the generalized binding model to line, bar, pie, and combo charts; richer axis configuration; legends and tooltips; conditional formatting; and reusable visual templates. Stabilize those semantics before considering a public RPC or VS Code surface.
