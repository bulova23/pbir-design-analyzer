# Phase 40 Advanced Chart Authoring Current State

Phase 40 adds the additive `local-pbir-generation-request/v5` backend contract. V1–v4 remain unchanged and continue through their existing provider paths.

The closed typed catalog supports Card, Table, Clustered Column Chart, Line Chart, Bar Chart, and Pie Chart. Visual descriptors own supported roles, required role cardinality, serializer role mappings, and axis, legend, tooltip, chart-formatting, and conditional-formatting capabilities. The provider resolves descriptors and uses common generation logic; it does not maintain chart-family branches.

V5 adds deterministic `default`, `executive`, and `compact` templates; typed axes; legends; tooltip fields; and a bounded conditional-formatting model. Template values project to existing schema-safe title, axis-label, legend, background, and data-color primitives. Typed tooltip fields are validated and retained in the v5 authoring input, but arbitrary tooltip objects are not emitted because the pinned visual-container schema rejects custom object shapes. Dedicated tooltip PBIR role projection remains a future schema-backed increment.

The representative six-visual report passed schema validation, materialization, lineage validation, analyzer round-trip, and byte/hash determinism. One measured run scored 88.45 and took 73 ms generation, 124 ms materialization, and 97 ms analyzer execution.

Phase 41 should prioritize report composition: reusable report sections, page templates, navigation, slicers, and richer interaction models. Public RPC and VS Code exposure remain deferred.

