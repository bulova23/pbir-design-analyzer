# PBIR Semantic Binding Specification

The shared semantic binding vocabulary is defined by the existing visual descriptor catalogs and represented in the shared IR. Supported roles are Value, Category, Series, Axis, Legend, and Tooltip; a role is importable only when the descriptor for that visual family maps the imported query-state role to it.

Imported aliases are canonicalized through the descriptor catalog. The Pie visual’s Category query role maps to canonical Legend because the descriptor defines that visual role as Legend. Tooltips maps to Tooltip where the descriptor supports tooltip bindings. Slicer Category delegates to the Phase 41 slicer descriptor.

Semantic equivalence requires matching visual family, canonical role, measure/dimension kind, normalized entity/property/token reference, and meaningful projection order. Unknown roles are preserved but remain untyped and cannot be mutated.
