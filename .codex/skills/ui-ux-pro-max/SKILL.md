---
name: ui-ux-pro-max
description: UI/UX design intelligence with searchable database
---
# ui-ux-pro-max

Searchable UI/UX design system skill for web and mobile application work in this repo. Use it when you need stronger visual direction, searchable design references, or implementation guidance from the bundled dataset and scripts.

## Goal

Generate a design direction or design system from the bundled search tooling without forcing the full reference catalog to load by default.

## When To Use

Use this skill when:

- the user requests UI/UX design, implementation, redesign, critique, or visual improvement
- you need a design system recommendation from the bundled search dataset
- you need stack-specific UI guidance from the packaged search tooling

## When Not To Use

Do not use this skill when:

- the task is backend-only or non-visual
- `frontend-design` is sufficient and no searchable design-system guidance is needed
- the user only needs one isolated styling fix with no broader design direction

## Workflow

1. Read `references/prerequisites.md` if Python availability is unknown.
2. Extract the product type, style keywords, industry, and stack.
3. Start with `scripts/search.py` using `--design-system`.
4. Read `references/search-workflow.md` for persistence or domain-specific follow-up searches when needed.
5. Read `references/search-reference.md` for domain and stack options when the query needs refinement.
6. Read `references/professional-ui-rules.md` before implementation when polish or quality issues are part of the task.
7. Use `templates/design-system-output.md` or `examples/scenario-example.md` when you need a response shape.

## Supporting Resources

Read these only when needed:

- `references/prerequisites.md`
- `references/search-workflow.md`
- `references/search-reference.md`
- `references/professional-ui-rules.md`
- `templates/design-system-output.md`
- `examples/scenario-example.md`

## Output Expectations

- Start from a concrete design-system recommendation, not vague style commentary.
- Default to `html-tailwind` stack guidance when the user does not specify a stack.
- Prefer search-driven decisions over improvised design language when using this skill.
- Keep the final response concise: recommended direction, commands used, and implementation implications.

## Safety Rules

- Do not install or recommend OS-level dependencies unless Python is actually missing.
- Do not skip the initial `--design-system` pass when a broader UI direction is needed.
- Do not treat the search dataset as a substitute for repo constraints; pair it with local product boundaries.

| Rule | Do | Don't |
|------|----|----- |
| **Glass card light mode** | Use `bg-white/80` or higher opacity | Use `bg-white/10` (too transparent) |
| **Text contrast light** | Use `#0F172A` (slate-900) for text | Use `#94A3B8` (slate-400) for body text |
| **Muted text light** | Use `#475569` (slate-600) minimum | Use gray-400 or lighter |
| **Border visibility** | Use `border-gray-200` in light mode | Use `border-white/10` (invisible) |

### Layout & Spacing

| Rule | Do | Don't |
|------|----|----- |
| **Floating navbar** | Add `top-4 left-4 right-4` spacing | Stick navbar to `top-0 left-0 right-0` |
| **Content padding** | Account for fixed navbar height | Let content hide behind fixed elements |
| **Consistent max-width** | Use same `max-w-6xl` or `max-w-7xl` | Mix different container widths |

---

## Pre-Delivery Checklist

Before delivering UI code, verify these items:

### Visual Quality
- [ ] No emojis used as icons (use SVG instead)
- [ ] All icons from consistent icon set (Heroicons/Lucide)
- [ ] Brand logos are correct (verified from Simple Icons)
- [ ] Hover states don't cause layout shift
- [ ] Use theme colors directly (bg-primary) not var() wrapper

### Interaction
- [ ] All clickable elements have `cursor-pointer`
- [ ] Hover states provide clear visual feedback
- [ ] Transitions are smooth (150-300ms)
- [ ] Focus states visible for keyboard navigation

### Light/Dark Mode
- [ ] Light mode text has sufficient contrast (4.5:1 minimum)
- [ ] Glass/transparent elements visible in light mode
- [ ] Borders visible in both modes
- [ ] Test both modes before delivery

### Layout
- [ ] Floating elements have proper spacing from edges
- [ ] No content hidden behind fixed navbars
- [ ] Responsive at 375px, 768px, 1024px, 1440px
- [ ] No horizontal scroll on mobile

### Accessibility
- [ ] All images have alt text
- [ ] Form inputs have labels
- [ ] Color is not the only indicator
- [ ] `prefers-reduced-motion` respected
