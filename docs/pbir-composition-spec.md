# PBIR Composition Specification

Phase 41 composes a report through typed records:

```text
Report
→ Page Template
→ Sections
→ Slots
→ Visuals
→ Bindings
```

Composition is a provider-side projection. It resolves a closed page-template catalog into ordinary visual rectangles before the shared PBIR IR and serializer run. The serializer and analyzer do not accept composition concepts.

Supported sections are Header, KPI Row, Primary Analysis, Secondary Analysis, Detail Grid, Filter Rail, and Footer / Navigation. A slot names a rectangle and an allowed visual family. Required slots, duplicate assignments, missing visual references, incompatible visual families, overflow, and explicit-layout conflicts fail closed.

Layout precedence is explicit visual layout, then assigned template slot, then deterministic automatic placement. An explicit rectangle that differs from an assigned slot is an error; it is not silently repositioned.

Navigation metadata contains typed targets for previous, next, home, and named pages. Targets are ordered as supplied after validation, and page references must resolve. External URLs, arbitrary actions, bookmarks, drillthrough, and circular action graphs are not part of this contract.

Static report/page/visual equality filters remain separate from slicer state. Slicer interactions are explicit metadata and do not introduce a generic event system.

## Slicer subset

Slicer is a closed descriptor with exactly one dimension binding in the Category role. The pinned visual-container schema supports the visualType, query, objects, and syncGroup properties; Phase 41 uses visualType, query, and schema-safe title objects only. It does not emit syncGroup, so synchronized/shared slicers remain deferred.

Typed slicer title and label data are retained in the v6 authoring input. Tooltip input continues to be validated by the existing boundary but does not emit arbitrary tooltip objects.
