# Phase 36 — First Local PBIR Generation Provider Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a backend-only deterministic local PBIR generation provider that creates one valid card report, materializes it through Phase 31, and immediately verifies it with the existing analyzer.

**Architecture:** The provider maps a narrow `local-pbir-generation-request/v1` into the existing Phase 29 intermediate representation and deployable serializer inputs. It delegates persistence to Phase 31 and scoring to the existing `PbirScoringService`; no RPC, VS Code, serializer, schema, or security architecture is added.

**Tech Stack:** .NET 8, C# records, existing Phase 29 serializer, Phase 31 materialization orchestration, xUnit, `PbirProjectService`, and `PbirScoringService`.

---

## File map

- Create `service-dotnet/Services/Discovery/Models/LocalPbirGenerationModels.cs` for the v1 request, result, round-trip, diagnostics, and readiness contracts.
- Create `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs` for request validation, IR/request construction, Phase 29 serialization, Phase 31 materialization, and analyzer round-trip orchestration.
- Create `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs` for all Phase 36 behavior and artifact comparison assertions.
- Modify `docs/current-state/generation-provider-framework-state.md` to record the first concrete backend provider without reclassifying the provider-neutral framework as an execution runtime.
- Modify `docs/current-state/reference-generator-state.md` to distinguish the new production-quality local provider from the descriptive reference generator.
- Modify `docs/ROADMAP.md` with the completed Phase 36 entry and Phase 37 incremental authoring recommendation.
- Create `docs/superpowers/implementation-notes/2026-08-13-phase36-first-local-pbir-generation-provider.md` with the generated example, request format, round-trip evidence, deterministic hashes, test commands, and limitations.
- Create `.agent-memory/sessions/20260813-phase36-first-local-pbir-generation-provider.md` and update `.agent-memory/current-focus.md` and `.agent-memory/session-summaries.md` at closeout.

### Task 1: Add the request/result contract and failing contract tests

**Files:**
- Create: `service-dotnet/Services/Discovery/Models/LocalPbirGenerationModels.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] **Step 1: Write failing tests for the supported request contract.**

Add tests asserting the request schema is `local-pbir-generation-request/v1`, valid fixture values round-trip as records, the supported visual type is `card`, and the result exposes artifact, manifest, materialization, score, and diagnostics fields.

```csharp
[Fact]
public void Contract_ExposesBackendOnlyPhase36V1Shape()
{
    Assert.Equal("local-pbir-generation-request/v1", LocalPbirGenerationRequestContract.SchemaVersionV1);
    Assert.Equal("card", LocalPbirGenerationProviderContract.SupportedVisualType);
    Assert.Contains("artifact", LocalPbirGenerationResultContract.RequiredFieldInventory);
    Assert.Contains("roundTrip.score", LocalPbirGenerationResultContract.RequiredFieldInventory);
}
```

- [ ] **Step 2: Run the focused test to verify it fails.**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~LocalPbirGenerationProviderServiceTests`

Expected: FAIL because the Phase 36 contract types do not exist.

- [ ] **Step 3: Implement the minimal records and constants.**

Define the request with safe identifiers, report/page/visual metadata, measure entity/property/token, dataset path, generation timestamp, output base directory, and target directory name. Define typed diagnostics, result readiness, and round-trip records that reference existing `PbirDeployableArtifact`, `PbirDeployableManifest`, `PbirMaterializationOrchestrationResult`, and `ScoreResult` types. Do not add JSON/RPC handlers.

- [ ] **Step 4: Run the focused contract test to verify it passes.**

Run the same focused command. Expected: PASS for the contract tests.

- [ ] **Step 5: Commit the contract slice.**

```bash
git add service-dotnet/Services/Discovery/Models/LocalPbirGenerationModels.cs service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs
git commit -m "feat(pbir): add phase 36 generation contracts"
```

### Task 2: Implement request validation and deterministic Phase 29 input mapping

**Files:**
- Modify: `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] **Step 1: Add failing validation tests.**

Cover blank request, unsafe request/report/page/visual ids, rooted/traversal/separator dataset paths, missing semantic fields, unsupported visual type, and output target names containing separators. Assert `Rejected`, no artifact, no manifest, and stable diagnostic codes.

```csharp
[Theory]
[InlineData("../Sales.SemanticModel")]
[InlineData("/Sales.SemanticModel")]
[InlineData("C:/Sales.SemanticModel")]
public void Generate_UnsafeDatasetPath_FailsClosed(string path)
{
    var result = new LocalPbirGenerationProviderService().Generate(CreateRequest() with { DatasetPath = path });
    Assert.Equal(LocalPbirGenerationReadinessState.Rejected, result.Readiness);
    Assert.Null(result.Artifact);
    Assert.Contains(result.Diagnostics, item => item.Code == "PBIR36-REQUEST-PATH-001");
}
```

- [ ] **Step 2: Run the validation tests to verify they fail.**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~LocalPbirGenerationProviderServiceTests`

Expected: FAIL because provider validation is not implemented.

- [ ] **Step 3: Implement the provider’s validation and mapping helpers.**

Validate the request before creating any IR. Map exactly one page, exactly one card visual at `page:<pageId>/slot:1`, one measure semantic entry, deterministic navigation/layout/success criteria, and lineage references derived from the request. Compute the IR content hash with `PbirIntermediateRepresentationIntegrity.ComputeContentHash`. Build `PbirSerializerRequest` and `PbirDeployableSerializerRequest` using the existing schema locks, `modernPbir`, the caller dataset path, one measure inventory entry, one `Fields` binding, and `PbirDeployableExecutionPolicy.NoAuthority`.

Use stable IDs derived from the request id; never use `Guid.NewGuid()`, `DateTimeOffset.UtcNow`, network calls, or fallback semantic fields.

- [ ] **Step 4: Run validation tests to verify they pass.**

Run the focused test command. Expected: PASS for all malformed and supported-input mapping tests.

- [ ] **Step 5: Commit the mapping slice.**

```bash
git add service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs
git commit -m "feat(pbir): map phase 36 requests to canonical IR"
```

### Task 3: Delegate to Phase 29 and verify artifact/schema output

**Files:**
- Modify: `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] **Step 1: Add failing serialization tests.**

Assert a valid request produces `Serialized`, valid postflight validation, no failure diagnostics or warnings, exactly six files for one page and one visual, required paths including `definition.pbir`, `definition/report.json`, `definition/pages/pages.json`, one page file, and one visual file, plus stable SHA-256 fields.

```csharp
[Fact]
public void Generate_ValidRequest_ProducesOnePageOneCardArtifact()
{
    var result = new LocalPbirGenerationProviderService().Generate(CreateRequest());
    Assert.Equal(LocalPbirGenerationReadinessState.Generated, result.Readiness);
    Assert.NotNull(result.Artifact);
    Assert.True(result.Validation!.IsValid);
    Assert.Equal(9, result.Artifact!.Files.Count);
    Assert.Contains(result.Artifact.Files, file => file.RelativePath == "definition/report.json");
    Assert.Single(result.Artifact.Files.Where(file => file.RelativePath.EndsWith("/visual.json", StringComparison.Ordinal)));
}
```

- [ ] **Step 2: Run the focused serialization test to verify it fails.**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~LocalPbirGenerationProviderServiceTests`

Expected: FAIL because generation does not yet call the Phase 29 serializer.

- [ ] **Step 3: Call `PbirDeployableSerializerService.CreateArtifacts`.**

Return a rejected result when the serializer is not `Serialized`, its validation is invalid, or its diagnostics contain failures/warnings. Preserve the serializer artifact, manifest, validation, hashes, and lineage in the successful result without recomputing them.

- [ ] **Step 4: Run the focused serialization test to verify it passes.**

Run the focused test command. Expected: PASS with the exact Phase 29 inventory.

- [ ] **Step 5: Commit the serializer integration slice.**

```bash
git add service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs
git commit -m "feat(pbir): generate phase 36 deployable artifact"
```

### Task 4: Add Phase 31 persistence and analyzer round-trip

**Files:**
- Modify: `service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] **Step 1: Add failing end-to-end tests.**

Use an isolated temporary output base and a safe target leaf. Assert the provider applies through `PbirMaterializationOrchestrationService`, resolves the report through `PbirProjectService`, returns one page/one visual in the analyzer result, and retains the expected generated artifact hash and materialization outcome.

```csharp
[Fact]
public async Task Generate_ValidRequest_MaterializesAndScoresRoundTrip()
{
    using var temp = new TemporaryDirectory();
    var result = await new LocalPbirGenerationProviderService().GenerateAndVerifyAsync(
        CreateRequest(temp.Path));
    Assert.Equal(LocalPbirGenerationReadinessState.RoundTripVerified, result.Readiness);
    Assert.Equal(PbirMaterializationOrchestrationOutcome.Applied, result.Materialization!.Outcome);
    Assert.NotNull(result.RoundTrip?.Score);
    Assert.Single(result.RoundTrip!.Score!.PageScores!);
    Assert.Single(result.RoundTrip.Score.PageScores[0].Visuals!);
}
```

- [ ] **Step 2: Run the end-to-end test to verify it fails.**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~LocalPbirGenerationProviderServiceTests`

Expected: FAIL because materialization and round-trip analysis are not implemented.

- [ ] **Step 3: Implement explicit Phase 31 apply and analyzer verification.**

Construct the existing orchestration input, call preview, derive its validated preview identity, and call apply with a deterministic transaction id. Reject conflicts, schema failures, cancellation, or non-applied outcomes. Resolve the materialized target using `PbirProjectService.TryGetReportLocation`, then call `PbirScoringService.ScoreAsync` with `NullLogger` only through a provider constructor dependency; avoid static/global analyzer state. Verify exactly one page and one visual of type `card` before returning `RoundTripVerified`.

- [ ] **Step 4: Run the end-to-end test to verify it passes.**

Run the focused test command. Expected: PASS and a real `ScoreResult` from the generated report.

- [ ] **Step 5: Commit the round-trip slice.**

```bash
git add service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs
git commit -m "feat(pbir): verify generated artifact through analyzer"
```

### Task 5: Add determinism, regression, and unsupported-construct coverage

**Files:**
- Modify: `service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs`

- [ ] **Step 1: Add determinism tests.**

Generate the same request twice into separate temporary destinations and compare every Phase 29 file path/content/hash, artifact hash, manifest hash, lineage hash, and score summary. Assert the only destination-specific values are the expected target paths outside artifact bytes.

- [ ] **Step 2: Add malformed and unsupported generation regression tests.**

Cover `table`, `lineChart`, missing measure property, duplicate identity inputs, and a tampered serializer input through the existing serializer safety gate. Assert no partial artifact and stable diagnostic codes.

- [ ] **Step 3: Add analyzer regression assertions.**

Run the existing analyzer against the generated report and a pre-existing fixture in the same test class. Assert the fixture score behavior is unchanged and the generated report is analyzed as a normal PBIR surface, not as a special provider surface.

- [ ] **Step 4: Run focused provider and existing Phase 29/analyzer tests.**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~LocalPbirGenerationProviderServiceTests|FullyQualifiedName~PbirDeployableSerializerServiceTests|FullyQualifiedName~PbirScoringServiceTests"
```

Expected: all selected tests pass.

- [ ] **Step 5: Commit the coverage slice.**

```bash
git add service-dotnet/tests/Discovery/LocalPbirGenerationProviderServiceTests.cs
git commit -m "test(pbir): cover phase 36 determinism and round trip"
```

### Task 6: Update current state, roadmap, implementation notes, and memory

**Files:**
- Modify: `docs/current-state/generation-provider-framework-state.md`
- Modify: `docs/current-state/reference-generator-state.md`
- Modify: `docs/ROADMAP.md`
- Create: `docs/superpowers/implementation-notes/2026-08-13-phase36-first-local-pbir-generation-provider.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/20260813-phase36-first-local-pbir-generation-provider.md`

- [ ] **Step 1: Document the final supported contract and generated example.**

Include the exact request JSON shape, one generated artifact file excerpt, artifact/manifest/file-set/lineage hashes from verification, analyzer score summary, commands run, and explicit limitations. State that the timestamp is caller-supplied and that no RPC/VS Code surface exists yet.

- [ ] **Step 2: Update roadmap/current-state language.**

Record Phase 36 as the first concrete backend local provider and Phase 37 as incremental authoring capability work. Keep the provider-neutral framework, security boundary, and Phase 31 mutation authority descriptions accurate.

- [ ] **Step 3: Finalize memory records.**

Record preserved unrelated Phase 35 changes, implementation decisions, validation results, any environment limitations, and the next recommended Phase 37 step.

- [ ] **Step 4: Run documentation whitespace validation.**

Run: `git diff --check`

Expected: no output and exit code 0.

- [ ] **Step 5: Commit documentation and memory.**

```bash
git add docs/current-state/generation-provider-framework-state.md docs/current-state/reference-generator-state.md docs/ROADMAP.md docs/superpowers/implementation-notes/2026-08-13-phase36-first-local-pbir-generation-provider.md .agent-memory/current-focus.md .agent-memory/session-summaries.md .agent-memory/sessions/20260813-phase36-first-local-pbir-generation-provider.md
git commit -m "docs: record phase 36 local PBIR generation"
```

### Task 7: Run the complete validation matrix

**Files:**
- No source changes; validate the complete Phase 36 diff.

- [ ] **Step 1: Run backend generation and regression tests.**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release`

Expected: exit code 0 with zero failed tests.

- [ ] **Step 2: Run .NET build.**

Run: `dotnet build service-dotnet/PbirDesignAnalyzer.Core.csproj -c Release`

Expected: exit code 0 with zero errors.

- [ ] **Step 3: Run TypeScript compilation and extension build.**

Run: `npm run build` from `vscode-extension/`.

Expected: exit code 0; confirm the existing extension build remains compatible even though Phase 36 adds no extension code.

- [ ] **Step 4: Run schema and whitespace checks.**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirDeployableSerializerSchemaTests|FullyQualifiedName~LocalPbirGenerationProviderServiceTests"` and `git diff --check`.

Expected: exit code 0 for both commands.

- [ ] **Step 5: Review the final diff for scope and unrelated changes.**

Run: `git status --short` and `git diff --stat HEAD~7..HEAD`.

Confirm only Phase 36 commits are attributed to this work and existing Phase 35 worktree changes remain present but uncommitted or in their original commits.
