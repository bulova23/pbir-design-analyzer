# Search Workflow

## Step 1: Analyze Requirements

Extract:

- product type: SaaS, e-commerce, portfolio, dashboard, landing page, and similar
- style keywords: minimal, playful, professional, elegant, dark mode, and similar
- industry: healthcare, fintech, gaming, education, and similar
- stack: React, Vue, Next.js, or default to `html-tailwind`

## Step 2: Generate Design System

Always start with `--design-system`:

```bash
python3 .codex/skills/ui-ux-pro-max/scripts/search.py "<product_type> <industry> <keywords>" --design-system [-p "Project Name"]
```

This command:

1. searches five domains in parallel: product, style, color, landing, and typography
2. applies reasoning rules from `data/ui-reasoning.csv`
3. returns a complete design system: pattern, style, colors, typography, and effects
4. includes anti-patterns to avoid

Example:

```bash
python3 .codex/skills/ui-ux-pro-max/scripts/search.py "beauty spa wellness service" --design-system -p "Serenity Spa"
```

## Step 2b: Persist Design System

To save a master design system and page-specific overrides:

```bash
python3 .codex/skills/ui-ux-pro-max/scripts/search.py "<query>" --design-system --persist -p "Project Name"
```

This creates:

- `design-system/MASTER.md`
- `design-system/pages/`

With a page-specific override:

```bash
python3 .codex/skills/ui-ux-pro-max/scripts/search.py "<query>" --design-system --persist -p "Project Name" --page "dashboard"
```

This also creates:

- `design-system/pages/dashboard.md`

Hierarchical retrieval:

1. check `design-system/pages/<page>.md` first
2. page-specific rules override `MASTER.md`
3. if no page file exists, use `MASTER.md` only

## Step 3: Supplement with Detailed Searches

After the design-system pass, run targeted searches as needed:

```bash
python3 .codex/skills/ui-ux-pro-max/scripts/search.py "<keyword>" --domain <domain> [-n <max_results>]
```

Use detailed searches when you need:

- more style options
- chart recommendations
- UX best practices
- alternative typography
- landing-page structure

## Step 4: Stack Guidance

If the user does not specify a stack, default to `html-tailwind`:

```bash
python3 .codex/skills/ui-ux-pro-max/scripts/search.py "<keyword>" --stack html-tailwind
```

Available stacks include:

- `html-tailwind`
- `react`
- `nextjs`
- `vue`
- `svelte`
- `swiftui`
- `react-native`
- `flutter`
- `shadcn`
- `jetpack-compose`

## Output Formats

The `--design-system` flag supports:

```bash
# ASCII box output
python3 .codex/skills/ui-ux-pro-max/scripts/search.py "fintech crypto" --design-system

# Markdown output
python3 .codex/skills/ui-ux-pro-max/scripts/search.py "fintech crypto" --design-system -f markdown
```

## Tips

1. Be specific with keywords.
2. Search multiple times when the first result is too generic.
3. Combine domain searches for style, typography, color, and UX.
4. Check UX guidance for accessibility, animation, and interaction pitfalls.
5. Use stack-specific guidance before implementation.
