# Report Discovery Wizard Refinement Round 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve Discovery Wizard recommendation quality by preserving end-to-end provenance, making experience-type selection context-aware, and producing materially more specific blueprints and design-package rationale without widening public contracts.

**Architecture:** Extend the existing internal discovery models instead of introducing a new subsystem. Add structured lineage and evidence references at the discovery/profile seam, make recommendation selection score candidate experience types against semantic and audience/workflow signals, and let blueprint/package generators consume that richer context for differentiated outputs.

**Tech Stack:** .NET 8, existing Discovery services and internal models, xUnit, existing VS Code extension validation commands

---

### Task 1: Provenance model refinement
- Add failing tests for end-to-end lineage fidelity in recommendation blueprint, Design Studio seed, and Design Package generation.
- Extend internal discovery/blueprint/package models with stable internal references for semantic model, discovery profile, and semantic evidence.
- Verify downstream lineage uses preserved upstream ids instead of synthesized placeholders.

### Task 2: Context-aware experience selection
- Add failing recommendation-engine tests proving multiple experience types can compete and that audience/workflow/analytical-depth signals influence the winner.
- Replace category-default selection with scored experience-type evaluation over candidate experience types.
- Keep ranking deterministic and bounded.

### Task 3: Blueprint specificity
- Add failing blueprint tests proving executive, operational, and analytical scenarios differ materially.
- Refine page naming, KPI grouping, filters, visuals, navigation, and analytical flow using domain, audience, dimensions, and opportunity signals.
- Preserve provider-neutral and advisory-only behavior.

### Task 4: Design package rationale quality
- Add failing package tests proving rationale includes audience, KPI, page, navigation, analytical-flow, business-outcome, and provenance context.
- Expand package rationale and page/KPI explanations from blueprint and semantic evidence rather than generic strings.
- Keep internal-only boundaries intact.

### Task 5: Validation and memory
- Run targeted discovery tests while iterating.
- Run `dotnet test service-dotnet/tests/Tests.csproj -c Release`.
- Run `cd vscode-extension && npm test`.
- Run `cd vscode-extension && npm run compile`.
- Update `.agent-memory/current-focus.md`, the active session note, and `.agent-memory/session-summaries.md`.
