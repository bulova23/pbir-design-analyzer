# Phase 39 Generalized Visual Bindings Current State

Phase 39 is implemented as a backend-only additive evolution of the local PBIR provider. The v4 request adds explicit binding roles while preserving the v1–v3 request records and generated Card/Table behavior. The shared IR carries normalized role-bearing bindings; the serializer remains the only PBIR mapping boundary.

Supported roles in the typed contract are Value, Category, Series, Axis, Legend, and Tooltip. Phase 39 accepts Value for Card/Table and Category plus Value for Clustered Column Chart. The remaining roles are reserved and rejected in chart validation until a visual family needs them.

The supported visual catalog is Card, Table, and Clustered Column Chart. Charts use deterministic layout, title/axis-label/legend/background/color formatting, schema validation, lineage, hashing, materialization, and analyzer round-trip. The representative catalog scored 92.5 and measured 2 ms generation, 73 ms materialization, and 23 ms analyzer execution in one run.

Phase 40 is now implemented as the additive v5 chart-authoring layer. It adds Line, Bar, and Pie descriptors, deterministic templates, typed axis/legend/tooltip/conditional-formatting inputs, and schema-safe common projections. Tooltip PBIR role emission remains deferred because the pinned visual-container schema rejects the required custom shape. Phase 41 should focus on report composition rather than additional individual visuals.
