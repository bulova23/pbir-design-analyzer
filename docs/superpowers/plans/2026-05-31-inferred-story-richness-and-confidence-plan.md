# Inferred Story Richness And Confidence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve inferred page-story wording and confidence so richer PBIR field/measure metadata produces more human-readable narratives, stronger semantic evidence, and more defensible confidence levels.

**Architecture:** Keep the implementation inside the deterministic PBIR scoring service. Extend the existing story-inference pipeline to derive semantic agreement and richer business concepts before story synthesis, then expose the improved evidence through the existing score payload and webview without changing the frontend contract shape.

**Tech Stack:** C#, .NET 8, xUnit, TypeScript, React, Jest

---

### Task 1: Add backend regression tests for richer story inference

**Files:**
- Modify: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Add tests that assert:
- executive-overview story text does not repeat `performance performance`
- semantic metadata can raise inferred-story confidence above `low`
- evidence includes semantic/business-language support, not just structural cues

- [ ] **Step 2: Run the focused backend test selection and verify it fails**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirScoringServiceTests`

Expected: FAIL on the new inferred-story assertions.

- [ ] **Step 3: Implement the minimal backend changes to satisfy the tests**

Touch:
- `service-dotnet/Services/Pbir/PbirScoringService.cs`

Expected implementation areas:
- richer semantic concept extraction
- repetition-safe story phrasing
- semantic-aware confidence scoring
- richer semantic evidence lines

- [ ] **Step 4: Re-run the focused backend tests and verify they pass**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirScoringServiceTests`

Expected: PASS

### Task 2: Verify panel-facing rendering expectations remain correct

**Files:**
- Modify if needed: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] **Step 1: Update webview assertions only if changed text contracts require it**

Keep scope narrow. Only adjust tests if the rendered inferred-story copy or evidence expectations need to acknowledge the richer backend output.

- [ ] **Step 2: Run focused webview tests**

Run: `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`

Expected: PASS

### Task 3: Run targeted regression validation

**Files:**
- No additional file edits required

- [ ] **Step 1: Compile the extension**

Run: `cd vscode-extension && npm run compile`

Expected: PASS

- [ ] **Step 2: Run full extension tests**

Run: `cd vscode-extension && npm test`

Expected: PASS

- [ ] **Step 3: Run full backend tests**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release`

Expected: PASS

### Task 4: Record decisions and residual risks

**Files:**
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-05-31-<time>-inferred-story-richness-and-confidence.md`

- [ ] **Step 1: Record what changed and what remains intentionally deferred**

Document:
- no full semantic-model parser yet
- deterministic semantic agreement added
- confidence now reflects both structure and semantic convergence

- [ ] **Step 2: Note residual risks**

Capture:
- quality still depends on PBIR-exposed metadata richness
- no cross-visual semantic ontology yet
- no LLM-based critique/generation layer
