# Visual Intelligence & Screenshot Analysis Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend screenshot audit into a visual-evidence workflow with overlays, annotations, and finding-linked navigation.

**Architecture:** Build on current screenshot capture and audit state, then add an overlay/evidence model that links captures to normalized findings and page layout hints without changing scoring.

**Tech Stack:** TypeScript, React, existing visual audit pipeline

---

## Major Workstreams

### Task 1: Create visual-evidence linkage model
- [ ] Define screenshot-to-finding linkage structures.
- [ ] Map capture/page/finding relationships deterministically.
- [ ] Add fallback behavior for missing or unmatched captures.

### Task 2: Add overlay and annotation model
- [ ] Define overlay primitives for regions, reading order, density, alignment, and focus areas.
- [ ] Keep overlays renderable without heavy dependencies.
- [ ] Ensure overlays degrade cleanly when confidence is weak.

### Task 3: Build evidence navigation UX
- [ ] Add screenshot selection and linked-finding navigation.
- [ ] Support stepping between findings, overlays, and pages.
- [ ] Keep Evidence secondary to Issues and Fix Plan.

### Task 4: Validation
- [ ] Add linkage tests.
- [ ] Add overlay derivation tests.
- [ ] Add UI navigation tests.

## Non-Goals

- no score mutation
- no requirement that every finding has overlay support
- no large charting/graphics dependency
