# Repo Map

## Purpose

- VS Code extension and .NET backend for PBIR design analysis.

## Primary Stack

- TypeScript, React, .NET 8, Jest, and xUnit.

## Important Paths

- `.agent-memory/`: repo-local memory, session state, postmortems, and session history
- `AGENTS.md`: canonical repo-level agent operating contract
- `README.md`: baseline product or project description
- `CLAUDE.md`: tool-specific adapter if present

## Key Commands

- `cd vscode-extension && npm run build`: Build the extension and bundled backend output.
- `cd vscode-extension && npm test`: Run extension and webview Jest suites.
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`: Run backend tests.

## Validation Entry Points

- `cd vscode-extension && npm run build`: Build the extension and bundled backend output.
- `cd vscode-extension && npm test`: Run extension and webview Jest suites.
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`: Run backend tests.

## Startup Docs

- `AGENTS.md`
- `README.md`

## Major Constraints

- Keep local fixture-based tests opt-in.
- Preserve packaged backend behavior and release assets.
