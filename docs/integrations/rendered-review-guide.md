# Rendered Review Guide

Rendered Review is an optional human checklist workflow in the Optimization Report.

```text
Analyze
  ↓
Rendered Review Needed
  ↓
Review the rendered page
  ↓
Attach Screenshot (manual, user-supplied)
  ↓
Record Findings
```

PBIR Design Analyzer remains authoritative for deterministic analysis, semantic
analysis, scoring, governance, remediation planning, and deterministic
mutation. The extension does not build a report viewer, automate screenshot
capture, or inspect image pixels — screenshots are attached manually by the
reviewer. Anyone who wants a faster way to capture Power BI report screenshots
can use an external tool such as [PBI Lens](https://github.com/thenguyentrong/pbi-lens)
and attach the resulting images here; PBIR Design Analyzer does not integrate
with it.

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

Rendered evidence and Reviewer Notes remain separate from analyzer findings.
Evidence-backed governance can therefore distinguish deterministic, semantic,
rendered, and reviewer-confirmed support.

## Screenshot evidence

Use Attach Screenshot to add a user-supplied rendered image to a checklist
item. The typed evidence record stores the report, page, timestamp, provider,
file reference, and optional notes. The image is not parsed or compared
automatically.

## Mutation follow-up

After applying a deterministic mutation, re-analyze the report and use the
Rendered Review checklist to confirm the rendered outcome. Rendered review is
advisory and does not approve or apply mutations.
