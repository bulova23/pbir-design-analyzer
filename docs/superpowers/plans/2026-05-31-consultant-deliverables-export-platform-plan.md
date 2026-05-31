# Consultant Deliverables & Export Platform Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the current review packet preview/export workflow into a clearer consultant-facing deliverables platform.

**Architecture:** Extend the existing packet builder, preview state, and renderer stack with export-presentation adapters, profile-aware summary wording, and cleaner downstream export UX. Keep all logic downstream from scoring and normalized findings.

**Tech Stack:** TypeScript, React, Markdown/HTML/PDF renderers, existing export pipeline

---

## Major Workstreams

### Task 1: Stabilize export-side contracts
- [ ] Define a profile-aware export presentation adapter contract.
- [ ] Separate base packet data from audience/presentation wording.
- [ ] Preserve compatibility with existing review packet builders.

### Task 2: Add summary-language and profile emphasis
- [ ] Add persona-aware export-summary wording.
- [ ] Add smarter executive summary language polish.
- [ ] Keep wording deterministic unless AI narrative is explicitly enabled.

### Task 3: Redesign export workspace
- [ ] Move export into a clearer downstream workspace.
- [ ] Add previewable format/profile selection.
- [ ] Preserve current Markdown/HTML/PDF flows.

### Task 4: Extend deliverable formats
- [ ] Plan branded consultant-ready profile variants.
- [ ] Add future DOCX/PDF renderer architecture boundaries.
- [ ] Keep format-specific rendering behind a shared packet model.

### Task 5: Validation
- [ ] Add export profile and wording tests.
- [ ] Add preview/export alignment tests.
- [ ] Validate existing export behavior remains intact.

## Non-Goals

- no scoring changes
- no review-workspace rewrite
- no mandatory AI summary generation
