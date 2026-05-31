# Visual Intelligence & Screenshot Analysis Design

Date: 2026-05-31

## Goal

Extend screenshot audit into a richer visual-review experience with overlays, annotations, and stronger evidence navigation.

## Scope

Include:

- screenshot overlays
- visual annotations
- reading-order visualization
- density heatmaps
- alignment overlays
- focus-area highlighting
- screenshot-to-finding linkage
- visual evidence navigation

## Architecture

Build on current screenshot audit and evidence workflows instead of creating a parallel analysis product.

Recommended layers:

- visual evidence model
- overlay/annotation renderer
- screenshot-to-finding linkage adapter
- evidence navigation controller

## Data Flow

`AuditState`
`+ normalized findings`
`+ visual metadata`
`+ page layout hints`
`-> visual evidence linker`
`-> overlay annotation model`
`-> evidence navigation UI`

## UX Flow

1. User opens Evidence or a finding.
2. User chooses a screenshot or capture.
3. The UI shows linked findings and visual overlays.
4. User can step between findings, affected regions, and pages.
5. User returns to Issues or Fix Plan with clearer evidence context.

## Test Strategy

- screenshot-to-finding linkage tests
- overlay model derivation tests
- navigation interaction tests
- fallback rendering tests when captures are missing

## Non-Goals

- no new scoring system
- no heavy charting dependency
- no requirement that every finding have a screenshot

## Dependencies

- current visual audit provider flow
- stable capture/page assignment
- stable finding IDs and evidence references
