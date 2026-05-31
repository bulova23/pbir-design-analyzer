# Enterprise Governance & Advanced Review Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expand PBIR Design Analyzer into a more configurable governance and advanced review platform with organization profiles, stronger benchmark intelligence, and advanced review surfaces.

**Architecture:** Keep scoring authoritative while adding governance profile models, advanced configuration workspace state, and benchmark/review adapters above the existing scoring and findings layers.

**Tech Stack:** TypeScript, React, existing config/governance pipeline

---

## Major Workstreams

### Task 1: Add organization governance profile model
- [ ] Define profile contracts and persistence boundaries.
- [ ] Separate shared standards from local presentation/workflow settings.
- [ ] Preserve current default governance behavior.

### Task 2: Redesign the configuration workspace
- [ ] Add layered configuration surfaces for basic, advanced, and expert review settings.
- [ ] Keep current scoring controls functional during migration.
- [ ] Avoid turning the config UI into a scoring-engine fork.

### Task 3: Expand advanced review surfaces
- [ ] Add benchmark intelligence expansion.
- [ ] Add custom standards and industry templates.
- [ ] Add bookmark-state and responsive/mobile review workflows.

### Task 4: Validation
- [ ] Add governance profile tests.
- [ ] Add advanced configuration tests.
- [ ] Add benchmark and advanced-review workflow tests.

## Non-Goals

- no backend scoring rewrite
- no hidden organization rules affecting scores without explicit configuration
- no forced enterprise mode for all users
