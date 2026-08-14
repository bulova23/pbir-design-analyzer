# Phase 41 Report Composition Design

## Goal

Add backend-only report composition to the local PBIR generation provider through an additive `local-pbir-generation-request/v6` contract. V1–v5 remain unchanged and continue to produce their historical artifacts.

## Architecture

Phase 41 has three bounded responsibilities. The composition contract describes a closed catalog of page templates, sections, slots, navigation, and slicers. The composition projection resolves those records into ordinary page and visual layouts plus existing authoring records. Composition validation rejects invalid references, assignments, conflicts, and unsupported slicer shapes before the existing shared IR, serializer, materialization, and analyzer path runs.

No composition types enter the serializer or analyzer. No nested layout tree, plugin registry, generic action framework, or public RPC surface is introduced.

## V6 contract

V6 adds nullable composition fields to a new request record while preserving the exact v1–v5 records and overloads:

- page template name per page;
- typed section definitions and named slot assignments;
- typed navigation definitions and page targets;
- typed slicer definitions with one category binding;
- explicit slicer interaction metadata;
- optional page-level composition layout settings.

Older versions are converted into the existing v3-compatible authoring shape without adding defaults. V6 converts into that same shape after composition projection.

## Composition catalog

The page-template catalog is closed and deterministic:

| Template | Sections and slots |
| --- | --- |
| Executive Summary | Header, KPI Row (`Kpi1`, `Kpi2`), Primary Analysis (`PrimaryChart`), Detail Grid (`DetailTable`), Filter Rail (`RegionSlicer`), Footer/Navigation (`Navigation`) |
| Overview | Header, Primary Analysis (`PrimaryChart`), Secondary Analysis (`SecondaryChart`), Filter Rail (`Filter1`), Footer/Navigation (`Navigation`) |
| Detail | Header, Filter Rail (`Filter1`), Detail Grid (`DetailTable`), Footer/Navigation (`Navigation`) |
| Comparison | Header, Primary Analysis (`PrimaryChart`, `SecondaryChart`), Footer/Navigation (`Navigation`) |

Slots have deterministic rectangles derived from page size, margins, section spacing, and slot order. Slot compatibility is typed: KPI slots accept cards, analysis slots accept supported chart visuals, detail slots accept tables, filter slots accept slicers, and navigation slots accept navigation metadata rather than visuals.

## Layout precedence

Composition resolution uses this precedence:

1. explicit visual layout;
2. template slot layout;
3. deterministic automatic placement.

An explicit layout and an assigned slot are a conflict unless the explicit rectangle equals the resolved slot rectangle. The provider fails closed instead of silently moving a visual. Overflow, duplicate assignment, missing required slot, unknown slot, invalid page, and incompatible visual/slot combinations are validation errors.

## Navigation

Navigation is typed metadata containing stable navigation IDs, ordered targets, and valid page identities. Supported target kinds are previous, next, home, and named page. Targets are resolved after page identities are known; duplicate IDs, missing pages, unsupported self-references, and materially circular chains are rejected. URLs, external actions, bookmarks, and drillthrough are out of scope.

## Slicers

Slicer is the seventh closed visual descriptor. It accepts exactly one dimension binding in the Category role. Its input may include existing typed title/label formatting and deterministic orientation/layout settings. Static report/page/visual equality filters remain separate from interactive slicer state. Slicer interactions use explicit target visual IDs or page scope and reuse existing interaction semantics; they do not create a generic event model.

The serializer emits only fields accepted by the pinned PBIR schema. Synchronized/shared slicers remain deferred unless repository evidence proves a deterministic schema-safe shape. Typed tooltip input remains validated but does not cause unsupported tooltip PBIR emission.

## Data flow

```text
V6 Request
  -> Composition Contract Validation
  -> Composition Projection
  -> Existing v3-compatible authoring projection
  -> Shared PBIR IR
  -> Phase 29 Serializer
  -> Schema Validation
  -> Phase 31 Materialization
  -> Analyzer Round-Trip
```

## Testing and compatibility

Focused table-driven tests cover all templates, slot compatibility and placement, precedence, conflicts, navigation, slicer bindings/interactions, v6 determinism, and analyzer round-trip. Existing v1–v5 provider tests remain regression gates. V6 representative output records artifact, manifest, file-set, lineage, and repeated-generation hashes plus generation, materialization, and analyzer timings.

## Explicit limitations

Bookmarks, drillthrough, synchronized/shared slicers, dedicated tooltip emission, semantic-model/DAX generation, Desktop workflows, Windows execution, hosted execution, RPC, and VS Code commands remain deferred.
