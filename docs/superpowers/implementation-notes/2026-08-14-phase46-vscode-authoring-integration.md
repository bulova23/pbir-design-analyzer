# Phase 46 — Minimal VS Code Authoring Integration

## Implemented

- Added the thin `pbir/authoring` JSON-RPC adapter and route.
- Exposed only Generate, Import, and Analyze at the host boundary.
- Added transport deserialization for the existing typed generation request union v1–v7 and preserved typed response diagnostics, identities, fidelity, analyzer summaries, and timing.
- Added additive Analyze support for opaque artifact and snapshot handles, resolved only in the backend dispatcher session.
- Added three VS Code commands backed by the existing bridge and output channel.
- Added session-only handle retention and concise structured error/result presentation.

## Evidence

Focused backend adapter/host/handle tests pass. Focused extension workflow tests pass. Full validation: backend 977 passed with 11 expected Windows skips; extension 499 passed; webview 68 passed; TypeScript, production build, and VSIX packaging passed. Full lint remains the unchanged 43-error baseline; changed files are lint-clean.

## Intentional non-scope

Mutation, standalone Validate, a report designer, webview authoring UI, arbitrary filesystem APIs, shell/process execution, hosted execution, Windows/Desktop execution, semantic-model/DAX generation, and provider-security changes remain absent.
