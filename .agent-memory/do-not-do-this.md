# Anti-Patterns

## Initial Entries

- 2026-05-15: Record failed strategies, retry traps, and dangerous refactors here.
- 2026-05-31: Do not wrap long command names or settings keys in Markdown inline code inside `vscode-extension/README.md` when they are meant to read like ordinary list items. In VS Code’s extension README rendering this turns them into dark blue code-pill boxes, which reads as heavy visual highlighting. Use plain text list items for command catalogs and settings lists unless code styling is intentionally desired.
