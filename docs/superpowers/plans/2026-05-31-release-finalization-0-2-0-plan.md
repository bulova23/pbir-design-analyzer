# PBIR Design Analyzer 0.2.0 Release Finalization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finalize the `0.2.0` release by curating the feature worktree payload, documenting the deferred roadmap epics, updating release docs, merging into `main`, validating, and packaging the VSIX from `main`.

**Architecture:** Treat this as a release-finalization workflow rather than a feature workflow. First curate the feature worktree so only intentional product/docs/release content remains. Then freeze the deferred roadmap in specs/plans and update `0.2.0` docs. Finally validate, merge into `main`, revalidate, and package from `main`.

**Tech Stack:** Git, TypeScript, React, .NET 8, Jest, Markdown docs, VS Code extension packaging

---

## File Structure

- Create: `docs/superpowers/specs/2026-05-31-release-finalization-0-2-0-design.md`
  - Release-finalization design and merge boundary.
- Create: `docs/superpowers/specs/2026-05-31-consultant-deliverables-export-platform-design.md`
  - Deferred Epic 1 design.
- Create: `docs/superpowers/specs/2026-05-31-visual-intelligence-screenshot-analysis-design.md`
  - Deferred Epic 2 design.
- Create: `docs/superpowers/specs/2026-05-31-enterprise-governance-advanced-review-design.md`
  - Deferred Epic 3 design.
- Create: `docs/superpowers/plans/2026-05-31-release-finalization-0-2-0-plan.md`
  - This execution plan.
- Create: `docs/superpowers/plans/2026-05-31-consultant-deliverables-export-platform-plan.md`
  - Deferred Epic 1 plan.
- Create: `docs/superpowers/plans/2026-05-31-visual-intelligence-screenshot-analysis-plan.md`
  - Deferred Epic 2 plan.
- Create: `docs/superpowers/plans/2026-05-31-enterprise-governance-advanced-review-plan.md`
  - Deferred Epic 3 plan.
- Create or update: `docs/ROADMAP.md`
  - Consolidated roadmap order, value, risk, complexity, quick wins, and strategic notes.
- Modify: `README.md`
  - Product overview and `0.2.0` user-facing summary.
- Modify: `vscode-extension/README.md`
  - Extension-specific walkthrough and `0.2.0` feature descriptions.
- Modify: `docs/HOW_TO_USE.md`
  - Detailed analyzer workflow for Overview, Issues, Fix Plan, Evidence, personas, matrix, and Export.
- Modify: `docs/CHANGELOG.md`
  - Full `0.2.0` release notes and limitations.
- Modify: `AGENTS.md`
  - Architecture and durable agent guidance updates.
- Create or update compact durable memory:
  - `.agent-memory/current-focus.md`
  - `.agent-memory/session-summaries.md`
  - `.agent-memory/repo-map.md`
  - `.agent-memory/sessions/2026-05-31-0-2-0-release-summary.md`
  - `.agent-memory/sessions/2026-05-31-roadmap-next-epics-summary.md`

## Task 1: Curate The Feature Worktree Release Payload

**Files:**
- Review: `.worktrees/feat-semantic-color-chart-intent/*`
- Modify or delete as needed: transient `.agent-memory/sessions/*`, duplicate planning docs, generated noise

- [ ] Review `git status --short` in the feature worktree and classify all modified/untracked files into:
  - product code/tests
  - release docs
  - roadmap specs/plans
  - durable repo memory
  - transient session clutter
  - generated/package artifacts
- [ ] Preserve intended product code and tests for `0.2.0`.
- [ ] Preserve final roadmap/spec/plan docs that belong in the long-term roadmap.
- [ ] Consolidate `.agent-memory` into compact durable summaries.
- [ ] Remove or exclude transient session clutter, generated `.vscode-test/` artifacts, and obsolete planning noise.
- [ ] Re-run `git status --short` and confirm the remaining payload looks like a deliberate release, not an audit dump.

## Task 2: Write Deferred Epic Specs

**Files:**
- Create: `docs/superpowers/specs/2026-05-31-consultant-deliverables-export-platform-design.md`
- Create: `docs/superpowers/specs/2026-05-31-visual-intelligence-screenshot-analysis-design.md`
- Create: `docs/superpowers/specs/2026-05-31-enterprise-governance-advanced-review-design.md`

- [ ] Write Epic 1 design covering:
  - export profiles
  - persona-aware export-summary wording
  - smarter executive summary language polish
  - branded consultant-ready PDF/export profiles
  - export workspace redesign
  - AI-generated executive narrative/commentary
  - future DOCX/PDF architecture
- [ ] Write Epic 2 design covering:
  - screenshot overlays
  - visual annotations
  - reading-order visualization
  - density heatmaps
  - alignment overlays
  - focus-area highlighting
  - screenshot-to-finding linkage
  - visual evidence navigation
- [ ] Write Epic 3 design covering:
  - organization-specific governance profiles
  - configuration workspace redesign
  - advanced configuration workspace
  - benchmark intelligence expansion
  - custom standards
  - industry templates
  - bookmark-state analysis
  - mobile/responsive report review enhancements
- [ ] Ensure every spec includes:
  - architecture
  - data flow
  - UX flow
  - test strategy
  - explicit non-goals
  - dependency notes

## Task 3: Write Deferred Epic Implementation Plans

**Files:**
- Create: `docs/superpowers/plans/2026-05-31-consultant-deliverables-export-platform-plan.md`
- Create: `docs/superpowers/plans/2026-05-31-visual-intelligence-screenshot-analysis-plan.md`
- Create: `docs/superpowers/plans/2026-05-31-enterprise-governance-advanced-review-plan.md`

- [ ] Write an implementation plan for Epic 1 with file boundaries, rollout phases, validation strategy, and explicit non-goals.
- [ ] Write an implementation plan for Epic 2 with file boundaries, rollout phases, validation strategy, and explicit non-goals.
- [ ] Write an implementation plan for Epic 3 with file boundaries, rollout phases, validation strategy, and explicit non-goals.
- [ ] Ensure each plan is executable in isolation and does not assume the other epics ship first unless that dependency is explicit.

## Task 4: Freeze The Roadmap

**Files:**
- Create or update: `docs/ROADMAP.md`

- [ ] Add or update a roadmap document that lists:
  - recommended order
  - business value
  - risk
  - complexity
  - quick wins
  - long-term strategic value
- [ ] Keep the recommended order unless repo evidence forces a change:
  1. Consultant Deliverables & Export Platform
  2. Visual Intelligence & Screenshot Analysis
  3. Enterprise Governance & Advanced Review
- [ ] Explain why this order fits the current repo and product maturity.

## Task 5: Update 0.2.0 Release Documentation

**Files:**
- Modify: `README.md`
- Modify: `vscode-extension/README.md`
- Modify: `docs/HOW_TO_USE.md`
- Modify: `docs/CHANGELOG.md`
- Modify: `AGENTS.md`

- [ ] Update `README.md` with:
  - product overview
  - key `0.2.0` features
  - installation / getting started
  - Overview / Issues / Fix Plan / Evidence concepts
  - persona review modes
  - cross-page matrix navigation
  - roadmap summary
- [ ] Update `vscode-extension/README.md` with:
  - extension install/use flow
  - commands and settings
  - score-panel walkthrough
  - review workflow
  - export behavior
  - `0.2.0` feature updates
- [ ] Update `docs/HOW_TO_USE.md` with:
  - how to run the analyzer
  - how to interpret Overview
  - how to use Issues
  - how to use Fix Plan
  - how to use Evidence
  - how personas work
  - how matrix navigation works
  - what Export currently does
  - what is planned later
- [ ] Update `docs/CHANGELOG.md` with:
  - full `0.2.0` release notes
  - major features
  - known limitations
  - roadmap references
- [ ] Update `AGENTS.md` with:
  - workspace modernization summary
  - normalized findings architecture
  - persona architecture
  - cross-page matrix architecture
  - deferred roadmap references
  - release-finalization notes for future agents

## Task 6: Validate The Curated Feature Worktree

**Files:**
- No code changes required.

- [ ] Run:
  - `cd vscode-extension && npm run compile`
  - `cd vscode-extension && npm test`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- [ ] If a validation fails, fix only release-safe issues and re-run the narrowest relevant validation first.
- [ ] Review `git status --short` again to confirm only intentional release payload remains.

## Task 7: Commit The Cleaned Release Payload On The Feature Branch

**Files:**
- All curated release payload files

- [ ] Stage only intentional release files.
- [ ] Commit with a focused release-finalization message, for example:

```bash
git add README.md vscode-extension/README.md docs/HOW_TO_USE.md docs/CHANGELOG.md AGENTS.md docs/ROADMAP.md docs/superpowers .agent-memory service-dotnet vscode-extension
git commit -m "chore(release): finalize 0.2.0 workspace release"
```

- [ ] Record the commit hash in durable repo memory.

## Task 8: Merge Into Main Carefully

**Files:**
- Root checkout on `main`

- [ ] Review the root `main` working tree status before merge.
- [ ] If `main` has unrelated dirty changes that create merge risk, stop and resolve that boundary before proceeding.
- [ ] Merge the cleaned feature branch into `main`.
- [ ] Resolve conflicts carefully without dropping:
  - product code
  - release docs
  - roadmap docs
  - compact durable `.agent-memory`
- [ ] Re-run `git status --short` on `main` and confirm a clean merged state.

## Task 9: Revalidate On Main

**Files:**
- No code changes required.

- [ ] Run on `main`:
  - `cd vscode-extension && npm run compile`
  - `cd vscode-extension && npm test`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- [ ] If practical, perform a short VS Code smoke pass:
  - install the VSIX locally
  - open a PBIR report
  - verify Overview
  - verify Issues
  - verify persona selector
  - verify matrix click-to-filter
  - verify Fix Plan
  - verify Evidence remains secondary
  - verify Export remains available
- [ ] Document any skipped smoke validation explicitly.

## Task 10: Package 0.2.0 From Main

**Files:**
- `vscode-extension/package.json`
- `vscode-extension/package-lock.json`
- packaged VSIX output

- [ ] Verify package metadata still reflects `0.2.0`.
- [ ] Verify extension description/features text is up to date for the modernized workspace.
- [ ] Run:
  - `cd vscode-extension && npm run package`
- [ ] Record the final package path in:
  - durable repo memory
  - release notes if useful

## Task 11: Final Durable Memory Cleanup

**Files:**
- `.agent-memory/current-focus.md`
- `.agent-memory/session-summaries.md`
- `.agent-memory/repo-map.md`
- `.agent-memory/sessions/2026-05-31-0-2-0-release-summary.md`
- `.agent-memory/sessions/2026-05-31-roadmap-next-epics-summary.md`

- [ ] Summarize release decisions, package path, validation, and known limitations in a final `0.2.0` release summary note.
- [ ] Summarize the deferred roadmap in a compact next-epics summary note.
- [ ] Ensure `current-focus.md` points future agents to post-release packaging/UAT or next-epic planning, not stale pre-release work.
- [ ] Confirm raw historical session clutter has been excluded from the release merge payload.

## Explicit Non-Goals

- Do not implement the deferred epics.
- Do not redesign export in this release pass.
- Do not add screenshot overlays in this release pass.
- Do not add AI-generated narrative in this release pass.
- Do not change scoring algorithms.
- Do not change severity/confidence logic.
- Do not add large dependencies.
