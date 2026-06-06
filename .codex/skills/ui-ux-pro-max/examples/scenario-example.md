# Scenario Example

User request: create a landing page for a professional skincare service.

Recommended extraction:

- product type: beauty or spa service
- style keywords: elegant, professional, soft
- industry: beauty or wellness
- stack: `html-tailwind` by default

Suggested first command:

```bash
python3 .codex/skills/ui-ux-pro-max/scripts/search.py "beauty spa wellness service elegant" --design-system -p "Serenity Spa"
```

Possible follow-up commands:

```bash
python3 .codex/skills/ui-ux-pro-max/scripts/search.py "animation accessibility" --domain ux
python3 .codex/skills/ui-ux-pro-max/scripts/search.py "elegant luxury serif" --domain typography
python3 .codex/skills/ui-ux-pro-max/scripts/search.py "layout responsive form" --stack html-tailwind
```
