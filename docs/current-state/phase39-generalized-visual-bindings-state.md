# Phase 39 Generalized Visual Bindings Current State

Phase 39 is implemented as a backend-only additive evolution of the local PBIR provider. The v4 request adds explicit binding roles while preserving the v1–v3 request records and generated Card/Table behavior. The shared IR carries normalized role-bearing bindings; the serializer remains the only PBIR mapping boundary.

Supported roles in the typed contract are Value, Category, Series, Axis, Legend, and Tooltip. Phase 39 accepts Value for Card/Table and Category plus Value for Clustered Column Chart. The remaining roles are reserved and rejected in chart validation until a visual family needs them.

The supported visual catalog is Card, Table, and Clustered Column Chart. Charts use deterministic layout, title/axis-label/legend/background/color formatting, schema validation, lineage, hashing, materialization, and analyzer round-trip. The representative catalog scored 92.5 and measured 2 ms generation, 73 ms materialization, and 23 ms analyzer execution in one run.

Phase 40 should add line, bar, pie, and combo chart families, richer axis semantics, legends, tooltips, conditional formatting, and reusable visual templates before any public RPC or VS Code exposure.
