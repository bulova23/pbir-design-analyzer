# Optional PBI Lens Capability-Safe Provider

## Scope

Implement the approved capability-safe rendered-design provider seam only. Do
not invoke private PBI Lens internals, interactive commands, CLI, MCP, or any
automatic screenshot acquisition. Preserve deterministic PBIR scoring and keep
the work unstaged and uncommitted.

## Research

- Installed extension: `duckduck-beps.pbi-lens-vscode` version 0.4.0.
- Package metadata documents commands and a separate CLI/MCP architecture.
- The VS Code bundle exports only `activate` and `deactivate`; no public typed
  programmatic API is exposed.
- `pbi-lens` is not installed on PATH and no local PBI Lens MCP executable/config
  was found.

## Implementation Progress

- Added provider-independent rendered evidence types and capability/status
  contracts.
- Added PBI Lens detection through injected extension discovery only.
- Added no-fabrication provider returning bounded unavailable diagnostics.
- Added safe enhanced-scoring settings and one-time absent-extension
  recommendation policy.
- Added additive score-panel capability metadata and non-error status message.
- Focused tests: 10 passed.
- TypeScript compilation passed before the latest detector hardening; rerun full
  validation after documentation and integration wiring are complete.

## Final Validation

- Focused provider/recommendation/presentation tests: 11 passed.
- Extension Jest: 517 passed; webview Jest: 68 passed.
- TypeScript compilation, production build, VSIX packaging, changed-file ESLint,
  and `git diff --check` passed.
- Backend regression: 995 passed, 11 expected Windows skips, and one known
  unrelated Phase 35E timeout-test flake (`Completed` instead of `TimedOut`).

## Handoff

- Documentation added for the integration decision, capability matrix,
  privacy/security boundary, manual future-provider test, and activation
  criteria.
- No CLI, MCP, private-extension, interactive-command, screenshot, scoring,
  or weighting integration was added.
- All work remains unstaged and uncommitted. Generated macOS arm64 backend
  binaries and the packaged 0.6.0 VSIX are present from validation and are
  reported separately in repository status.
