# PBIR Reader Specification

The PBIR reader scans schema-admitted definition files, captures the Phase 43 authoring envelope, and projects supported query-state roles through the shared descriptor catalog. It emits typed IR bindings only after descriptor resolution and field-shape/kind validation.

Unknown or unsupported roles produce structured `PreservedButUntyped` diagnostics and remain in the envelope. Invalid field shapes, ambiguous mappings, and descriptor kind conflicts produce `Invalid` diagnostics and block the imported IR readiness state. The reader never falls back to Value and never creates a parallel import model.
