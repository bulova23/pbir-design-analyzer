# Phase 46 — Minimal VS Code Integration for Generate, Import, and Analyze

## Executive summary

Phase 46 exposes a deliberately small editor workflow over the existing `pbir-authoring-rpc/v1` contract. VS Code contributes Generate Report, Import Report, and Analyze Report. The extension reads a selected typed generation-request JSON file, selects a PBIR folder for import or fallback analysis, retains only opaque session handles, and presents bounded score, diagnostics, fidelity, identity, and timing results in the existing output channel.

Mutation and standalone Validate remain backend-only. No authoring webview, designer, arbitrary PBIR reader, shell authority, process authority, or new generation schema is introduced.

## Architecture

```text
VS Code commands
      ↓
existing stdio JSON-RPC / pbir/authoring adapter
      ↓
pbir-authoring-rpc/v1 dispatcher
      ↓
typed generation/import/analyzer services
```

The adapter performs bounded object validation, typed JSON deserialization, the three-operation allowlist, dispatcher invocation, and response serialization. It contains no authoring logic. The core dispatcher retains the session maps for snapshots and generated artifacts. Analyze accepts an opaque artifact or snapshot handle as an additive correction to the directory-only Phase 45 shape and resolves it internally.

## Commands

| Command | Input | Output |
| --- | --- | --- |
| Generate Report | A selected JSON file with local-pbir-generation-request/v1 through v7 | Artifact identity, analyzer summary, diagnostics, timing; artifact handle retained for the session |
| Import Report | A selected supported PBIR report/project folder | Snapshot identity, diagnostics, timing; snapshot handle retained for the session |
| Analyze Report | Latest generated artifact, latest imported snapshot, or selected report folder | Score, page/visual counts, diagnostics, fidelity when provided, identity when provided, timing |

The extension does not interpret PBIR definition files. It only routes the typed generation request version and passes the selected source path to the backend importer/analyzer.

## Errors and security

Stable RPC categories map to concise labels: Invalid request, Unsupported PBIR construct, Import failed, Validation failed, Analyzer failed, Authoring conflict, and Authoring operation failed. Bounded diagnostics and timing go to the existing output channel; raw stack traces are not shown by default.

Opaque handles are session-oriented and never converted to paths by TypeScript. Mutation and Validate have no host route or command. The existing trusted backend boundary is reused without adding shell, arbitrary process, credential, hosted, Windows, or Desktop authority.

## Testing and Phase 47 gate

Focused backend tests cover route registration, typed request/response mapping, operation allowlisting, handle-aware analysis, and structured errors. Focused extension tests cover request mapping, command payloads, handle retention, and error formatting. Direct-vs-adapter comparisons use meaningful result fields rather than UI text. Phase 47 should be selected from observed workflow friction after this thin path is exercised; mutation UX is not presumed to be next.
