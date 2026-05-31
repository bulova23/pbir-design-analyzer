# Repo Map

## Purpose

- VS Code extension and .NET backend for PBIR design analysis and review-workspace generation.

## Primary Stack

- TypeScript
- React
- .NET 8
- Jest
- xUnit

## Core Product Areas

- `vscode-extension/src/`: extension host, commands, score payload shaping, review/export helpers
- `vscode-extension/webview-src/analyzer-score/`: score-panel React workspace
- `service-dotnet/Services/Pbir/`: backend scoring, PBIR parsing, semantic/story/governance heuristics
- `service-dotnet/RpcHost/`: packaged backend entrypoint

## Important Docs

- `README.md`: product overview and release-level usage
- `vscode-extension/README.md`: extension install and workflow details
- `docs/HOW_TO_USE.md`: detailed review-workspace walkthrough
- `docs/CHANGELOG.md`: release notes
- `docs/ROADMAP.md`: post-`0.2.0` roadmap ordering and epic links
- `docs/superpowers/specs/` and `docs/superpowers/plans/`: implementation specs and plans

## Important Memory Files

- `AGENTS.md`: repo-level operating contract
- `.agent-memory/current-focus.md`: current branch/release objective
- `.agent-memory/session-summaries.md`: compact durable milestone summary
- `.agent-memory/sessions/2026-05-31-0-2-0-release-summary.md`: final release snapshot
- `.agent-memory/sessions/2026-05-31-roadmap-next-epics-summary.md`: deferred-epic summary

## Key Commands

- `cd vscode-extension && npm run compile`
- `cd vscode-extension && npm test`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- `cd vscode-extension && npm run package`

## Release Constraints

- Keep scoring authoritative and presentation-only derivations separate.
- Keep normalized findings as the shared issue model.
- Keep workspace personas separate from reviewer-comment personas.
- Keep generated test-host artifacts and raw session clutter out of release merges.
