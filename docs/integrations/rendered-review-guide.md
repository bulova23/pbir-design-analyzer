# Rendered Review Guide

Rendered Review is an optional companion workflow in the Optimization Report.

```text
Analyze
  ↓
Rendered Review Needed
  ↓
Open in PBI Lens (when a supported interface is available)
  ↓
Review
  ↓
Record Findings
```

PBIR Design Analyzer remains authoritative for deterministic analysis, semantic
analysis, scoring, governance, remediation planning, and deterministic
mutation. PBI Lens provides rendered observation only. The extension does not
build a report viewer, automate screenshot capture, inspect image pixels, or
invoke undocumented PBI Lens commands.

## Checklist categories

The checklist is finding-driven and currently supports:

- Whitespace balance
- Visual hierarchy
- KPI prominence
- Title wrapping
- Clipped labels
- Crowded visuals
- Table readability
- Visual balance
- Color harmony
- Page readability

Each item explains why rendered inspection is recommended, what to look for,
and the expected design outcome. The reviewer can mark an item Not Reviewed,
Reviewed, Confirmed, Rejected, or Deferred and add an optional note.

## Finding and evidence classification

- Deterministic: PBIR and rule-derived findings.
- Semantic: semantic-model and usage findings.
- Rendered Review Recommended: findings whose conclusion benefits from human
  inspection of the rendered page.
- Rendered Evidence Required: reserved for a future phase and not emitted by
  this release.

Rendered evidence and Reviewer Notes remain separate from analyzer findings.
Evidence-backed governance can therefore distinguish deterministic, semantic,
rendered, and reviewer-confirmed support.

## PBI Lens and fallback behavior

When PBI Lens is available through a supported provider interface, the report
can expose an Open in PBI Lens action. In the current capability-safe release,
the installed PBI Lens 0.4.0 extension has no supported programmatic report
context interface, so the action is disabled and the checklist remains usable.
Deterministic scoring continues normally.

## Screenshot evidence

Use Attach Screenshot to add a user-supplied rendered image to a checklist
item. The typed evidence record stores the report, page, timestamp, provider,
file reference, and optional notes. The image is not parsed or compared
automatically.

## Mutation follow-up

After applying a deterministic mutation, re-analyze the report and use the
Rendered Review checklist to confirm the rendered outcome. Rendered review is
advisory and does not approve or apply mutations.
