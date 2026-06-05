---
name: frontend-design
description: Use when building or restyling web UI in this repo, especially VS Code webviews or React panels, and the work needs a strong visual direction without breaking existing product boundaries.
---
# Frontend Design

## Goal
Produce distinctive, production-ready UI for this repo's frontend surfaces while preserving the existing product architecture, trust boundaries, and accessibility expectations.

## When To Use
Use this skill when:
- The user asks for a new webview, panel, page, component, or notable visual refresh.
- The current UI works functionally but feels generic, flat, or under-designed.
- A design change needs stronger typography, layout, color, motion, or hierarchy.

Do not use this skill when:
- The task is backend-only, scoring-only, or limited to non-visual docs/copy.
- The user only wants bug triage with no intended UI or styling change.
- The requested change would cross a protected product boundary without explicit approval.

## Repo Context
- Prefer existing repo patterns before inventing new ones.
- Primary UI code lives in `vscode-extension/webview-src/` and supporting panel wiring lives in `vscode-extension/src/views/`.
- Keep scoring authoritative, findings normalized, and presentation changes presentation-only.
- Do not import external prompt logic or autonomous execution patterns into product code.

## Design Rules
- Choose one clear aesthetic direction before coding. Bold is good; random is not.
- Avoid generic AI defaults such as `Inter`, `Roboto`, plain system stacks, and purple-on-white gradients unless the repo already uses them for a reason.
- Favor expressive typography, intentional spacing, and a background treatment with some atmosphere.
- Use a few meaningful animations or transitions instead of many weak ones.
- Match ambition to context:
  - informational/admin surfaces should feel polished and high-signal
  - analytical/review surfaces should improve clarity before decoration
- Preserve mobile or constrained-width behavior where relevant, even for VS Code webviews.

## Workflow
1. Read the relevant files and identify the exact surface being changed.
2. Note repo constraints from `AGENTS.md` and any nearby docs/tests.
3. Pick a concrete visual direction in one sentence before editing.
4. Implement the smallest set of structural and style changes that fully express that direction.
5. Use CSS variables or shared tokens when repeating colors, spacing, borders, or shadows.
6. Keep motion subtle enough for a productivity tool unless the user asks for something louder.
7. Validate the affected frontend slice with the narrowest useful build or test command.

## Inputs To Look For
- `vscode-extension/webview-src/**`
- `vscode-extension/src/views/**`
- existing design docs in `docs/`
- screenshots, mockups, or user wording about tone and audience

## Supporting Resources
Read these only when needed:
- `../ui-ux-pro-max/SKILL.md` for extra design-system search workflows already checked into this repo
- repo docs under `docs/` for product intent and UI constraints

## Output Expectations
- Ship real code, not just design commentary.
- Make visual choices feel intentional and specific to the surface.
- Keep the explanation short: visual direction, main UI changes, and validation run.

## Safety Rules
- Do not rewrite unrelated surfaces for consistency unless the user asks.
- Do not let styling changes alter scoring, mutation, or advisory trust boundaries.
- Prefer improving an existing pattern over introducing a brand-new UI system.
