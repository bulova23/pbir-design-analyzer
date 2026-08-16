# Phase 36 — First Local PBIR Generation Provider

## Executive summary

Phase 36 proves the first complete local PBIR generation loop entirely in the backend. `LocalPbirGenerationProviderService` accepts a deterministic `local-pbir-generation-request/v1`, maps it to the existing Phase 29 intermediate representation and deployable serializer, materializes the result through Phase 31, and immediately re-analyzes the materialized report with `PbirScoringService`.

No VS Code command, RPC endpoint, extension surface, Windows execution, hosted execution, remote worker, credential integration, or provider-security change was added.

## Generation request format

The supported MVP request is:

```json
{
  "schemaVersion": "local-pbir-generation-request/v1",
  "requestId": "phase36-sales-card",
  "reportName": "Sales",
  "pageId": "Overview",
  "pageDisplayName": "Overview",
  "visualId": "RevenueCard",
  "visualType": "card",
  "datasetPath": "Sales.SemanticModel",
  "measureToken": "Revenue",
  "measureEntity": "Sales",
  "measureProperty": "Revenue",
  "generatedUtc": "2026-08-13T00:00:00Z",
  "outputBaseDirectory": "/tmp/pbir-phase36",
  "targetDirectoryName": "sales-card"
}
```

The provider rejects unsupported visual types, unsafe identifiers, rooted or traversal dataset paths, missing measure fields, invalid output bases, and target names containing path separators.

## Generated PBIR example

The request produces the minimum Phase 29 modern PBIR inventory for one page and one visual:

```text
definition.pbir
definition/version.json
definition/report.json
definition/pages/pages.json
definition/pages/<deterministic-page-id>/page.json
definition/pages/<deterministic-page-id>/visuals/<deterministic-visual-id>/visual.json
```

The generated visual is a card with one direct measure projection in the `Fields` role. Layout uses the existing `modern-grid-1280x720/v1` slot profile. The serializer emits pinned schema references and the standard Phase 29 newline/canonical JSON rules.

## Analyzer round trip

The verified path is:

1. Phase 29 serializes the request into a validated deployable artifact and manifest.
2. Phase 31 previews and applies the artifact to the explicit local target.
3. `PbirProjectService` resolves the materialized report.
4. `PbirScoringService` scores the report as a normal PBIR surface.

Observed result for the deterministic fixture request:

- materialization outcome: `Applied`
- analyzer page count: `1`
- generated visual count: `1`
- analyzer composite score: `73.5`
- serializer validation: valid
- provider failure diagnostics: none

The score is analyzer output and is not calculated or modified by the provider.

## Determinism results

Repeated generation of the same request produced byte-identical file content and equal hashes:

- artifact hash: `ddb60d8ffb1ad30bdd2a59404ab5eea1ea48a67e1aabda618a02348736de6bda`
- manifest hash: `853f34739fdaf55f0ebc2df17fe8737ef1f91312df8d3ec133a97a104fbd0c98`
- file-set hash: `2e89ca915498ecc0d4a892ef9d8448480272a2af715f7b0a2f7febf5d3b7f2f3`
- lineage hash: `5021b2353b7281635cf10901cce1d374bc6ec6e8b001db0bd586169b3be6c165`

The timestamp is explicit in the request because the existing IR contract requires generated time metadata. No current time or random identifier is introduced by the provider. Materialization transaction ids are deterministically derived from the request id.

## Tests and validation

Focused Phase 36 coverage includes:

- request contract shape;
- unsafe paths and unsupported visuals;
- missing semantic fields;
- valid six-file one-page/one-card artifact generation;
- Phase 29 schema/postflight validation;
- Phase 31 materialization;
- analyzer round-trip scoring;
- repeated-generation byte and hash comparison.

The full required validation matrix remains the handoff checklist for this phase:

```text
dotnet test service-dotnet/tests/Tests.csproj -c Release
dotnet build service-dotnet/PbirDesignAnalyzer.Core.csproj -c Release
cd vscode-extension && npm run build
git diff --check
```

## Known limitations

This is not a general PBIR authoring system. It supports one page, one card, one direct measure binding, deterministic layout, and an existing relative semantic-model path. It does not support additional visuals or pages, tables, charts, filters, bookmarks, formatting, themes, calculated measures, semantic-model generation, PBIP project generation, Desktop validation, hosted execution, RPC, or extension UI.

## Recommended Phase 37 scope

Extend the stabilized request and IR mapping incrementally with additional visuals, pages, formatting, and report constructs. Each construct should receive dedicated serializer/schema tests, deterministic artifact comparison, and analyzer round-trip coverage before any RPC or VS Code exposure is added.
