---
name: frontend-design
description: Use when building or restyling web UI in this repo, especially VS Code webviews or React panels, and the work needs a strong visual direction without breaking existing product boundaries.
---
# Frontend Design

## Goal
Produce distinctive, production-ready UI for this repo's frontend surfaces while preserving product boundaries, accessibility expectations, and existing architecture.

## When To Use

Use this skill when:

- The user asks for a new webview, panel, page, component, or notable visual refresh.
- The current UI works functionally but feels generic, flat, or under-designed.
- A design change needs stronger typography, layout, color, motion, or hierarchy.

## When Not To Use

Do not use this skill when:

- The task is backend-only, scoring-only, or limited to non-visual docs/copy.
- The user only wants bug triage with no intended UI or styling change.
- The requested change would cross a protected product boundary without explicit approval.

## Inputs To Look For

- `vscode-extension/webview-src/**`
- `vscode-extension/src/views/**`
- existing design docs in `docs/`
- screenshots, mockups, or user wording about tone and audience

## Workflow

1. Read the relevant files and identify the exact surface being changed.
2. Read `references/repo-context.md` and `references/design-rules.md` if the surface or constraints are not already clear.
3. Pick a concrete visual direction in one sentence before editing.
4. Implement the smallest set of structural and style changes that fully express that direction.
5. Use CSS variables or shared tokens when repeating colors, spacing, borders, or shadows.
6. Keep motion subtle enough for a productivity tool unless the user asks for something louder.
7. Use `references/validation-checklist.md` to choose the narrowest useful validation command.

## Supporting Resources

Read these only when needed:

- `references/repo-context.md`
- `references/design-rules.md`
- `references/validation-checklist.md`
- `templates/change-summary.md`
- `examples/good-output.md`
- `../ui-ux-pro-max/SKILL.md` for deeper design-system search workflows

## Output Expectations

- Ship real code, not just design commentary.
- Make visual choices feel intentional and specific to the surface.
- Keep the explanation short: visual direction, main UI changes, and validation run.

## Safety Rules

- Do not rewrite unrelated surfaces for consistency unless the user asks.
- Do not let styling changes alter scoring, mutation, or advisory trust boundaries.
- Prefer improving an existing pattern over introducing a brand-new UI system.
