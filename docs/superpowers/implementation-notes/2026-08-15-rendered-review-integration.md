# Rendered Review Integration Implementation Notes

## Outcome

Phase 1 adds a presentation-only Rendered Review workflow on top of normalized
findings. Classification and checklist generation are pure TypeScript model
functions. The score-panel protocol carries checklist state, status changes,
reviewer notes, and user-supplied screenshot evidence metadata.

## Boundary decisions

- deterministic and semantic scoring remain unchanged and authoritative
- rendered review is recommendation and evidence presentation, not scoring
- PBI Lens is detected through the existing provider capability boundary
- no undocumented commands, CLI runner, MCP client, screenshot automation, or
  pixel parsing was added
- existing screenshot upload/copy primitives are reused for manual evidence
- reviewer notes are not converted into analyzer findings or mutation authority

## Future activation

An Open in PBI Lens action can become active only when a documented and tested
provider report-context capability is available. CLI, MCP, automated screenshot
retrieval, visual AI, and visual regression belong to later roadmap phases.
