# PBIR Visual Catalog Specification

Phase 40–41 use a closed typed catalog rather than an extensible registry.

| Visual | Required bindings | Optional bindings | PBIR roles |
| --- | --- | --- | --- |
| Card | Value Measure | typed tooltip fields | Fields ← Value |
| Table | one or more Value fields | typed tooltip fields | Values ← Value |
| Clustered Column Chart | one Category Dimension and one or more Value Measures | typed tooltip fields | Category ← Category; Y ← Value |
| Line Chart | one Category Dimension and one or more Value Measures | Series Dimension and typed tooltip fields | Category ← Category; Y ← Value; Series ← Series |
| Bar Chart | one Category Dimension and one or more Value Measures | typed tooltip fields | Category ← Category; Y ← Value |
| Pie Chart | one Legend Dimension and one or more Value Measures | typed tooltip fields | Category ← Legend; Y ← Value |
| Slicer | exactly one Category Dimension | schema-safe title and label formatting | Category ← Category |

Role names and binding kinds are enums. Expressions, arbitrary role names, and plugin registration are unsupported. Duplicate roles, missing required roles, incompatible kinds, and unsupported authoring capabilities fail closed.

Slicer is a composition-oriented visual added in Phase 41. Its supported subset intentionally excludes synchronized/shared slicer state and arbitrary tooltip emission because the pinned schema evidence does not establish a safe representation for those features.
