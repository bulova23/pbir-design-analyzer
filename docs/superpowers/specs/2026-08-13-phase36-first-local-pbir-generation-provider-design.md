# Phase 36 — First Local PBIR Generation Provider

## Goal

Prove a complete backend-only local PBIR generation loop:

```text
LocalPbirGenerationRequest
  -> LocalPbirGenerationProviderService
  -> Phase 29 IR and deployable serializer
  -> Phase 31 local materialization
  -> existing PBIR analyzer
  -> round-trip verification result
```

The provider is deliberately a small product slice, not a general report-authoring engine.

## Approved boundary

Phase 36 adds no VS Code command, RPC route, extension state, hosted execution, Windows execution, provider security redesign, credentials, remote workers, semantic-model generation, or advanced report authoring. The service is internal to the backend assembly and is exercised through xUnit tests. A future RPC/VS Code contract may consume this service only after the request, artifact, determinism, validation, and round-trip contracts are stable.

Existing unrelated Phase 35 worktree changes remain out of scope.

## Design

### Request

`LocalPbirGenerationRequest` uses `local-pbir-generation-request/v1` and contains only deterministic, supported inputs:

- a safe request id;
- a report name and one page id/display name;
- one visual id with visual type `card`;
- one explicit semantic model measure (`entity`, `property`, and token);
- a safe relative dataset path;
- a caller-supplied UTC generation timestamp;
- an existing absolute output base and safe target leaf for materialization.

The provider rejects blank or unsafe identifiers, rooted/traversal dataset paths, unsupported visual types, extra pages/visuals, and missing semantic fields before producing an artifact. Unsupported constructs fail closed; there is no silent fallback visual or synthetic semantic binding.

### Provider and pipeline

`LocalPbirGenerationProviderService` is the only new generation implementation. It creates the smallest valid Phase 29 intermediate representation directly from the request, computes the existing IR integrity hash, creates the existing serializer requests with `PbirDeployableExecutionPolicy.NoAuthority`, and delegates artifact construction to `PbirDeployableSerializerService`.

The provider does not duplicate canonical JSON, schema validation, artifact hashing, lineage, layout identity, or materialization logic. Phase 29 remains authoritative for the emitted PBIR bytes and artifact hashes. Phase 31 remains authoritative for filesystem persistence, preview/apply safety, transactions, and rollback. The provider requests local mutation only through the existing Phase 31 orchestration service and only for the caller-supplied destination.

### Round trip

Generation is successful only if all of the following are true:

1. Phase 29 returns a serialized artifact and manifest.
2. Phase 29 postflight validation is valid and has no failure diagnostics or warnings.
3. Phase 31 applies the artifact to the requested destination.
4. `PbirProjectService` resolves the resulting PBIP/PBIR report.
5. `PbirScoringService.ScoreAsync` analyzes the materialized report.
6. The result contains exactly one page and one visual, matching the request identity and type.

The returned result preserves the artifact, manifest, Phase 31 hashes/lineage, analyzer `ScoreResult`, and typed diagnostics. Analyzer output remains authoritative; the provider does not calculate or alter scores.

### Determinism

The same request, including the same request id, timestamp, output target, and semantic inputs, must produce byte-identical Phase 29 artifact files and equal artifact/manifest hashes. IDs are derived from the request and canonical IR identities; no random IDs or current-time reads are allowed. The timestamp is explicit in the request because the existing IR contract requires it. Destination transaction identifiers used for Phase 31 are derived deterministically from the request id and artifact hash, subject to the existing safe identifier contract.

No network calls or external schema resolution are permitted. Any future nondeterministic field must be added to the request contract and documented before being emitted.

### Failure behavior

Failures are returned as a typed rejected result with no partial artifact result. Serializer, schema, materialization, and analyzer failures are preserved as bounded diagnostics and never converted into a successful round-trip. Existing Phase 31 failure classification and safety rules remain authoritative.

## Supported feature set

- modern PBIR format `4.0` through the existing Phase 29 serializer;
- one report;
- one page with deterministic 1280x720 layout;
- one `card` visual;
- one direct measure binding;
- one relative dataset reference;
- deterministic artifact, manifest, file-set, lineage, and output hashes;
- local materialization through the existing preview/apply/rollback pipeline;
- immediate analyzer scoring after materialization.

## Explicit limitations

This phase does not support multiple pages or visuals, tables, charts, filters, bookmarks, navigation, formatting objects, themes, calculated measures, semantic-model files, PBIP project generation, Desktop validation, hosted execution, RPC, or extension UI. The provider also does not infer business semantics or invent missing fields.

## Testing strategy

Tests are backend-only and cover:

- a valid request produces the expected nine-file Phase 29 inventory and round-trip score;
- malformed identifiers, paths, fields, and unsupported visual types fail closed;
- serializer/schema diagnostics prevent partial success;
- repeated generation produces equal canonical file bytes and hashes;
- materialized artifacts resolve through `PbirProjectService` and score through the existing analyzer;
- existing Phase 29 serializer and analyzer regression suites remain unchanged and passing.

## Phase 37 handoff

Phase 37 should extend the request and IR mapping incrementally for additional visuals, pages, formatting, and report constructs. It should not add execution infrastructure or widen the RPC/VS Code boundary until those constructs have their own deterministic serializer and analyzer round-trip coverage.
