# Phase 46 — VS Code Authoring Integration Current State

Phase 46 is the first cross-process consumer of the Phase 45 authoring contract. The current implementation exposes one JSON-RPC method, `pbir/authoring`, with exactly three admitted operations: Generate, Import, and Analyze.

## Available workflow

- Generate Report selects a typed local generation request v1–v7, delegates it to the existing dispatcher, and retains only the returned artifact handle.
- Import Report selects a report/project folder and delegates import to the existing backend reader, retaining only the returned snapshot handle.
- Analyze Report uses the latest session handle, or asks for a report folder if no handle exists, then displays a concise result through the existing output channel and notification surface.

Analyze handle resolution is backend-owned. The additive request shape accepts an artifact handle, snapshot handle, or explicit report directory, exactly one at a time. The dispatcher stores the source directory only in its in-process session state.

## Boundaries

The host adapter is transport glue only. It does not contain generation, import, mutation, validation, scoring, PBIR parsing, IR access, or filesystem interpretation beyond bounded request framing. Mutation and standalone Validate remain unregistered. The extension does not expose handles as editable values and does not add a persistent authoring workspace model.

## Limitations

Handles expire with the backend process. Generate requests are selected from existing typed JSON artifacts rather than authored graphically. Import support remains bounded by the existing pinned PBIR schemas and semantic projection catalog. There is no mutation UX, report diff/preview, bookmarks, drillthrough, shared slicers, semantic-model/DAX generation, Desktop automation, hosted execution, or provider-security change.

## Next decision

Phase 47 should be driven by observed Generate/Import/Analyze usage. The highest-value next step may be request authoring, persistent session handling, report diff/preview, or a narrow mutation experience; no choice is made until the thin workflow exposes the dominant usability or contract gap.
