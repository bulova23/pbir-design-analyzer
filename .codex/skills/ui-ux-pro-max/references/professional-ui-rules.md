# Professional UI Rules

These are frequent polish failures that make otherwise functional UI look unprofessional.

## Icons and Visual Elements

| Rule | Do | Avoid |
| --- | --- | --- |
| No emoji icons | Use SVG icons such as Heroicons, Lucide, or Simple Icons | Emoji used as production UI icons |
| Stable hover states | Use color, opacity, border, or shadow transitions | Scale transforms that shift layout unexpectedly |
| Correct brand logos | Use official SVG assets where possible | Guessed or distorted brand marks |
| Consistent icon sizing | Use a shared viewBox and size scale | Mixed arbitrary icon sizes |

## Interaction and Cursor

| Rule | Do | Avoid |
| --- | --- | --- |
| Cursor pointer | Add pointer affordance to clickable cards and controls | Leaving default cursor on interactive surfaces |
| Hover feedback | Show clear feedback with color, border, or shadow change | No indication that an element is interactive |
| Smooth transitions | Keep transitions short and intentional | Instant state jumps or sluggish transitions |

## Quality Habits

- Prefer one strong visual system over many unrelated flourishes.
- Use typography and spacing to create hierarchy before adding decoration.
- Treat accessibility and polish as part of the design system, not as cleanup work.
- If light and dark themes are both present, verify contrast in both modes before shipping.
