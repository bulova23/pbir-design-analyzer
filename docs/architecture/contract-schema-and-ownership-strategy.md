# Contract Schema And Ownership Strategy

Date: 2026-06-15

Status: Active guidance for Workstream 2B hardening

## Purpose

This document defines how PBIR Design Analyzer owns, validates, versions, and gradually consolidates cross-language contracts without changing current runtime behavior.

The immediate goal is drift reduction, not a forced code generation migration.

## Scope

This strategy applies to:

- Score payload contracts
- Design Studio contracts
- Design Studio protocol envelopes
- Extension/backend RPC payloads where practical

## Contract Ownership Model

### Current authoritative owners

- Score payload contracts:
  - C# backend `service-dotnet/Services/Pbir/Models/ScoreResult.cs` is authoritative for serialized score payload shape
  - TypeScript `vscode-extension/src/views/scoreResultPayload.ts` is the consuming validator and normalizer
- Design Studio contracts:
  - TypeScript `vscode-extension/src/design-studio/contracts/designStudioModels.ts` is the product-facing contract owner
  - C# `service-dotnet/Services/DesignStudio/Models/DesignStudioModels.cs` is a mirrored internal backend model set that must stay vocabulary-compatible where duplication exists
- Design Studio protocol envelopes:
  - TypeScript `vscode-extension/src/design-studio/contracts/designStudioProtocol.ts` is authoritative because the current protocol is host/webview-only
- Extension/backend RPC payloads:
  - JSON-RPC framing and envelope compatibility remain jointly owned by the extension transport and RpcHost entrypoint
  - request/response method contracts should be treated as versioned transport contracts, not ad hoc serializer side effects

### Ownership rules

- One side is the schema owner for each boundary.
- The non-owning side may mirror the contract, but it must not silently redefine semantics.
- Cross-language duplication must be backed by drift tests.
- Presentation-only derived models must not be treated as backend-authoritative contracts.

## Required Versus Optional Fields

### Rule set

- Required fields:
  - must be present with the expected primitive/object kind
  - must fail explicitly when absent, renamed, or shape-incompatible
  - must not fall back to synthetic defaults such as `0`, `false`, or empty strings
- Optional fields:
  - may be absent without failing contract validation
  - must preserve backward compatibility for older payload producers
  - when present, must still satisfy their expected shape

### Score payload contract rules

- Required top-level score payload fields:
  - `gestaltScore`
  - `cognitiveLoadScore`
  - `dataInkScore`
  - `accessibilityScore`
  - `visualBestPracticesScore`
  - `stephenFewScore`
  - `enterpriseGovernanceScore`
  - `tufteScore`
  - `graphicalPerceptionScore`
  - `densityScore`
  - `narrativeScore`
  - `compositeScore`
  - `feedback`
  - `pageCount`
  - `recommendations`
  - `reportPath`
  - `scoredAt`
- Optional top-level score payload fields currently preserved for compatibility:
  - `layoutScore`
  - `themeScore`
  - `governanceScore`
  - `dataVisualCount`
  - `navigationVisualCount`
  - `hiddenVisualCount`
  - `frameworkWeights`
  - `pageScores`
  - `scoredPageId`
  - `scoredPageName`
  - `scoringErrors`
  - `visualMetadata`
  - `inferredStorySummary`
  - `pageIntentProfile`
  - `actionabilityBreakdown`
  - `benchmarkComparison`
  - `guidedStoryImprovements`
  - `reportConsistencySummary`

### Design Studio contract rules

- Shared enum vocabularies duplicated in TypeScript and C# are compatibility-critical:
  - lifecycle states
  - approval states
  - approval kinds
  - validation result states
  - materialization modes
  - materialization source roles
- Optional object sections inside Design Studio state or request payloads remain optional only when the protocol already allows absence.
- Protocol envelope version fields are always required.

## Compatibility And Versioning Rules

- Additive optional fields are allowed without a protocol version bump.
- Renaming, removing, or changing the meaning of a required field is a breaking contract change.
- Enum value additions are breaking unless every consumer is explicitly tolerant before the producer ships the new value.
- Host/webview and extension/backend boundaries must reject unsupported protocol or schema versions before consuming payload state.
- Backward compatibility should prefer:
  - preserving old optional fields
  - deprecating before removing
  - documenting fallback windows

## Automated Drift Detection

Current hardening for Workstream 2B uses:

- Jest drift tests that compare duplicated Design Studio enum vocabularies between TypeScript and C#
- Jest contract inventory tests that keep required and optional score payload fields explicit
- Jest score payload tests that prove required-field failure and optional-field compatibility
- Jest protocol envelope tests that reject unsupported protocol and schema versions

Future drift checks should prefer machine-readable snapshots over handwritten prose assertions when schema artifacts exist.

## Schema And Code Generation Migration Path

### Phase 0

- Keep current handwritten contracts.
- Make ownership explicit.
- Add drift tests and required/optional inventories.

### Phase 1

- Introduce repo-local schema artifacts for the highest-risk duplicated boundaries first:
  - score payload contracts
  - Design Studio enum vocabularies
  - Design Studio protocol envelopes
- Preferred artifact direction:
  - JSON Schema stored in a dedicated contracts folder under source control
- During this phase, generated code is optional.

### Phase 2

- Generate TypeScript validators/types and C# DTOs from the schema for the selected boundaries.
- Keep existing handwritten models only as adapters where immediate replacement is too risky.

### Phase 3

- Migrate remaining duplicated contracts.
- Remove redundant mirrored declarations only after generated outputs and drift tests are stable.

## Contracts That Should Remain Hand-Maintained For Now

- Score-panel presentation-only derived models
- local webview state models that do not cross into C#
- analyzer-derived enrichment models that are assembled after backend scoring

## Implementation Guardrails

- Do not change current runtime behavior just to match a future schema tool.
- Do not force full code generation during Workstream 2B.
- Do not treat advisory or presentation-only layers as backend-authoritative contracts.
- Do not widen deterministic mutation authority through contract cleanup work.
