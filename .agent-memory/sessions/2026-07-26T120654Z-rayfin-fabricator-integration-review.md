---
date: 2026-07-26
time: 12:06 UTC
agent: codex
repo: pbir-design-analyzer
branch: codex/ux-consolidation-remediation-0-2-2
status: complete
next_step: Confirm whether to design the recommended advisory Fabricator instruction-pack handoff.
validation: research-only
---

# Rayfin Fabricator Integration Review

## Objective

- Review `spatney/rayfin-fabricator` for safe ways PBIR Design Analyzer can provide visualization-enhancement guidance to Fabricator.
- Preserve normalized findings, Fabric App Review ownership, advisory-only enrichment, and deterministic mutation boundaries.

## Repository Snapshot Reviewed

- Repository: `https://github.com/spatney/rayfin-fabricator`
- Commit: `4d4609797a92515c5815877ab8675387f997f4de`
- Retrieved: 2026-07-26
- License: MIT

## Main Findings

- Fabricator is a Tauri desktop workbench around GitHub Copilot, Rayfin CLI, project git history, live preview, deployment, and an AI Advisor.
- Its strongest supported interoperability seam is its custom-skill import:
  - accepts a `SKILL.md`, folder, or zip
  - supports optional Markdown references
  - installs the skill under the Rayfin app's `.agents/skills/`
  - commits the installed skill into the project
- Its live preview design mode already produces:
  - screenshots
  - structured change sets
  - Graphein chart-spec before/after state
  - source-oriented instructions for the Copilot authoring session
- Its Data App template uses Graphein chart specs and a headless preview script that emits PNG output plus machine-readable clipping, overlap, contrast, lint, mark, series, and color diagnostics.
- Fabricator does not expose a documented stable external API or CLI integration contract for another desktop application. Its Tauri IPC is internal.

## Recommended Integration Direction

- Keep PBIR Design Analyzer as the analysis and advisory owner.
- Analyze the same Rayfin/Fabric App repository through the existing Fabric App surface and analyzer.
- Export a versioned, evidence-backed Fabricator instruction pack instead of invoking Fabricator directly.
- Let the user either:
  - import the pack as a Fabricator custom skill, or
  - copy a run-specific instruction brief into Fabricator chat.
- Keep dynamic findings in a versioned reference payload; keep the skill itself small and stable.
- Require explicit user action in Fabricator before source edits or deployment.
- Re-run PBIR Design Analyzer after Fabricator changes and compare findings using project fingerprints and lineage.

## Gaps To Address Before Shipping The Handoff

- Current Fabric App Review findings are too narrow for a high-quality Fabricator visualization brief.
- Screenshot evidence currently inventories captures but does not inspect visual content.
- Semantic-model evidence currently relies on bounded text-pattern extraction.
- Add deterministic Graphein-aware evidence before claiming chart-specific guidance:
  - chart type and encoding
  - formatting
  - title and annotation use
  - series and color density
  - hierarchy and tile sizing
  - interaction and filter patterns
  - headless render diagnostics when present
- Define a versioned export schema, source fingerprint, evidence references, acceptance criteria, and expiration/staleness behavior.

## Risks

- Do not embed or invoke Fabricator internals; there is no stable public integration contract.
- Do not import Fabricator prompts, skills, or source code into this repo.
- Do not let exported advisory guidance carry mutation, deployment, or validation authority.
- Fabricator is a fast-moving personal project built on preview Fabric Apps capabilities.
- The reviewed snapshot contains several very large renderer/backend files and floating Graphein minimum-version ranges, increasing upstream drift risk.
- JavaScript type-check passed in the temporary clone.
- The full Vitest run was inconclusive in this environment because Node 25 exposed a non-functional global `localStorage`; 209 tests passed before 57 environment-caused failures.
- `npm audit` reported eight dependency advisories, primarily in the Vite/Vitest development toolchain. This reinforces avoiding a vendored or embedded dependency.

## Product Changes

- None.

