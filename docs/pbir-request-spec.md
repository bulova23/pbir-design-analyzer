# Local PBIR Generation Request Specification

The local backend generation request is additive by version. V1, v2, v3, v4, and v5 remain unchanged. Phase 41 adds `local-pbir-generation-request/v6`.

V6 retains the v5 report, page, visual, binding, theme, filter, metadata, interaction, and layout records, and adds a nullable composition collection. Each composition identifies a page, template, slot assignments, optional navigation definition, and optional slicer definition. A slicer visual uses the existing generalized binding record with one Dimension / Category binding.

Older versions are not converted through v6 and receive no composition defaults. V6 is projected into the existing v3-compatible authoring structure only after composition validation and layout resolution. This keeps historical request semantics and artifact generation stable.

V6 is backend-only. It is not exposed through RPC, VS Code commands, hosted execution, Windows execution, or a public provider contract.
