# Phase 35B Governed Runtime Provider Architecture — Current State

Phase 35B adds an offline-only composition root beside the authoritative Phase 35A contracts. It proves deterministic provider matching, explicit authorization/readiness gates, immutable sessions, lifecycle coordination, fixed validation, artifact disposition, cancellation/timeout classification, audit projection, and structured diagnostics.

The production provider catalog remains unavailable: no registered provider can execute report generation. Success-path tests use an explicitly constructed in-memory fake adapter and governed records. No RPC route, external process, shell, HTTP/network, filesystem provider execution, MCP, Skills, credential access, Desktop automation, PBIR generation/materialization, publication, or mutation authority exists in Phase 35B.

Runtime-only types are limited to session, lifecycle, validation, timeout, diagnostics, audit, and artifact-disposition projections. Phase 35A remains authoritative for provider profiles, requests, authorization, policies, readiness, results, artifacts, failures, hashes, lineage, retries, redaction, and quarantine.

## Flow

`Phase 35A Request → Gates → Exact Resolution → Immutable Session → Offline Adapter → Validation → Artifact Intake → Audit`

The orchestrator is a composition root. Provider-specific behavior, validation, artifact processing, and lifecycle rules live in focused services.

## Remaining Gaps

Before a real provider is considered, the repository still needs a reviewed sandbox/trust boundary, credential isolation, durable tamper-evident audit, an output scanning corpus, provider conformance tests, and explicit artifact validation and publication safeguards.

