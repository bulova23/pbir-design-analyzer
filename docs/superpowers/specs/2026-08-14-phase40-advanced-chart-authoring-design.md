# Phase 40 Advanced Chart Authoring and Reusable Visual Templates

## Objective

Add an additive `local-pbir-generation-request/v5` backend contract that validates and generates Card, Table, Clustered Column Chart, Line Chart, Bar Chart, and Pie Chart visuals through the existing generalized Phase 39 binding model. Preserve v1–v4 behavior and keep scoring, serialization, materialization, validation, lineage, hashing, and analyzer round-trip authoritative.

## Architecture

Phase 40 introduces a closed typed visual catalog, not a plugin registry. Each descriptor declares the visual kind, accepted and required binding roles, serializer role mapping, formatting capabilities, and support for axes, legends, tooltips, and conditional formatting. The provider resolves one descriptor for each v5 visual and runs common normalization/projection logic; it does not contain chart-family branches.

V1–v4 requests remain on their existing overloads and projections. V5 is adapted into the same v3-compatible authoring structure plus descriptor-validated bindings and additive authoring data. Existing requests therefore retain their serialized output and do not gain v5 defaults.

## V5 Contract

The v5 request adds a nullable visual-template reference and typed visual authoring sections for chart presentation. Templates are `default`, `executive`, and `compact`; template resolution is deterministic and strongly typed. Axis configuration supports title, visibility, orientation, and format. Legend configuration supports visibility, placement, and title. Tooltip fields reuse typed measure/dimension bindings with the `Tooltip` role and never contain expressions. Conditional formatting supports only threshold, positive/negative, and null/default color rules.

The v5 request reuses page, layout, theme, filter, interaction, metadata, and binding records from Phase 39. No RPC, VS Code, Windows, hosted execution, semantic-model generation, DAX generation, Desktop automation, or provider-security capability is added.

## Descriptor Catalog

| Visual | Required roles | Optional roles | Serializer mapping |
| --- | --- | --- | --- |
| Card | Value Measure | Tooltip | Fields ← Value |
| Table | at least one Value | Tooltip | Values ← Value |
| Clustered Column Chart | one Category Dimension, one or more Value Measures | Tooltip | Category ← Category; Y ← Value |
| Line Chart | one Category Dimension, one or more Value Measures | Series, Tooltip | Category ← Category; Y ← Value; Series ← Series |
| Bar Chart | one Category Dimension, one or more Value Measures | Tooltip | Category ← Category; Y ← Value |
| Pie Chart | one Legend Dimension, one or more Value Measures | Tooltip | Category ← Legend; Y ← Value |

Descriptor validation rejects duplicate roles, missing required roles, wrong binding kinds, unsupported roles, and unsupported authoring capabilities. The catalog is a static, exhaustive collection of six descriptors.

## Determinism and Round-Trip

Template defaults and descriptor projections are immutable and ordered. They contribute to the canonical request and artifact hashes but never to generated identifiers. Repeated equivalent v5 requests must produce byte-identical artifact files, manifests, lineage, and hashes. Representative reports must pass locked schema validation, materialization, analyzer scoring, and lineage validation.

## Testing and Documentation

Focused tests cover descriptors, v5 validation, all six visuals, templates, axis/legend/tooltip/conditional formatting serialization, v1–v4 regression, schema validation, analyzer round-trip, and deterministic hashes. Full backend, .NET build, extension build/type checking, and diff checks remain required. Documentation records the catalog, template matrix, example output, analyzer/performance observations, limitations, and Phase 41 composition recommendation.

