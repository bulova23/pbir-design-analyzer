# PBIR Fidelity Specification

Round-trip fidelity distinguishes byte-identical source documents, semantically identical canonical JSON, expected normalized differences, intentional mutation differences, unexpected differences, missing output, and unsupported content. Semantic comparison operates on shared IR bindings rather than raw query JSON.

Untyped schema-supported content is preserved by the Phase 43 envelope but is not evidence of typed semantic equivalence. Analyzer-before/after results are advisory evidence and report unchanged semantics, intended mutation deltas, and unexpected semantic regressions separately.
