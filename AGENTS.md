# Repository Guidelines

Review this repository as if you were performing a pre-release architecture review before a v1.0 launch.

Focus heavily on:
- Architectural consistency
- Future maintainability
- AI-agent generated code quality
- Overengineering
- Unnecessary abstractions
- Public contract stability
- Backward compatibility risks
- Testability
- Technical debt accumulation

Identify:
- Areas that will become difficult to maintain in 6-12 months
- Places where the architecture does not match the stated design documents
- Features that should be refactored before adding new functionality
- Any violation of existing architectural patterns used elsewhere in the repository

Rank all findings by long-term risk.

## Project Structure & Module Organization
`vscode-extension/` contains the shipped VS Code extension. Put extension runtime code in `src/`, React webviews in `webview-src/`, static assets in `resources/`, and Jest mocks in `tests/__mocks__/`. `service-dotnet/` contains the .NET 8 backend: `RpcHost/` is the packaged entrypoint, `Services/Pbir/` holds scoring, governance, and tree logic, and `tests/` holds xUnit coverage. Long-form specs, release notes, and troubleshooting live in `docs/`.

## Current Product Architecture

The `0.2.0` release centers on a modernized score-panel workspace:

- `Overview`
- `Issues`
- `Fix Plan`
- `Evidence`
- secondary `Export`

Key architecture layers:

- scoring layer: backend `ScoreResult` and page-score outputs remain authoritative
- findings layer: normalized findings unify issue rendering across multiple source systems
- presentation layer: overview summaries, fix-plan sequencing, workspace personas, and cross-page matrix navigation remain presentation-only

### Important Boundaries

- normalized findings are the shared issue model
- analyzable surface, analyzer, and analyzer profile are separate concepts:
  - surface = thing being reviewed
  - analyzer = review engine operating on that surface
  - profile = emphasis lens for that analyzer
- PBIR `Fabric App Readiness Assessment` is an analyzer operating on a PBIR surface, not a separate surface or workspace
- Fabric App Review Mode foundations validate `Fabric App` as a second real surface type through the same workspace
- Fabric App Review remains advisory-only unless the repo explicitly adds a future deterministic execution path; do not invent one
- bounded screenshot evidence and semantic-model usage evidence are valid Fabric App Review evidence domains
- screenshot evidence in Fabric App Review must reuse the existing screenshot evidence primitives and must not be treated as Visual Intelligence
- workspace personas are separate from reviewer-comment personas
- cross-page matrix navigation is presentation-only and finding-driven
- review/export workflows remain downstream from scoring
- AI proposal enrichment is advisory-only and must never carry mutation authority
- deterministic preview/apply/rollback remains the only report-edit execution path
- shared repository snapshots should stay analyzer-independent and should be reused across evidence domains instead of layering ad hoc repo rescans or analyzer-local memoization
- score-panel host/webview messages are now a versioned protocol boundary and must be validated before state is consumed or rendered
- selected page state must always be clamped against the current score payload page count; stale page references are a correctness bug
- Fabric review and Fabric readiness scoring constants now live in explicit configuration with provenance; preserve default outputs unless an override is intentionally introduced and documented
- external Power BI agent skills or prompts may be used as research input only; do not import external skill code, prompts, or autonomous execution patterns into this repo

## Roadmap References

Deferred next-epic roadmap after `0.2.0`:

1. Consultant Deliverables & Export Platform
2. Visual Intelligence & Screenshot Analysis
3. Enterprise Governance & Advanced Review

See:

- `docs/ROADMAP.md`
- `docs/superpowers/specs/2026-05-31-consultant-deliverables-export-platform-design.md`
- `docs/superpowers/specs/2026-05-31-visual-intelligence-screenshot-analysis-design.md`
- `docs/superpowers/specs/2026-05-31-enterprise-governance-advanced-review-design.md`

## Build, Test, and Development Commands
From `vscode-extension/`:

- `npm ci` installs Node dependencies.
- `npm run build` publishes the backend into `backend/rpc`, compiles TypeScript, bundles the extension, and builds both webviews.
- `npm run lint` runs ESLint on `src/**/*.ts`.
- `npm test` runs the extension Jest suite and the webview Jest suite.
- `npm run package` creates `pbir-design-analyzer-<version>.vsix`.

From the repo root:

- `dotnet test service-dotnet/tests/Tests.csproj -c Release` runs backend xUnit tests.
- `PBIR_REAL_FIXTURE_PATH=/path/to/Sales\\ \\&\\ Production.pbip dotnet test service-dotnet/tests/Tests.csproj --filter Category=PBITesting` runs opt-in fixture coverage.

## Agent Memory Workflow

- Read `AGENTS.md`, `.agent-memory/current-focus.md`, and `.agent-memory/repo-map.md` at session start.
- Review `.agent-memory/do-not-do-this.md` and `.agent-memory/failure-patterns.md` before repeating failing build, fixture, or packaging steps.
- Create one timestamped note per meaningful session in `.agent-memory/sessions/`.
- Update `.agent-memory/current-focus.md` at session start and session close.
- Append concise outcomes to `.agent-memory/session-summaries.md`.
- Keep repo-local fixture details local unless they can be generalized safely.

## Retry And Validation Rules

- Do not repeat the same failing build, test, fixture, or packaging command more than twice without a new hypothesis.
- Prefer the narrowest useful validation after each material change.
- If a fix cannot be validated, record that explicitly in the session note.

## Session Closeout

- Finalize the active session note in `.agent-memory/sessions/`.
- Update `.agent-memory/current-focus.md` with the next recommended step.
- Append a concise summary to `.agent-memory/session-summaries.md`.
- Prefer compact durable memory over a large raw session trail when preparing release merges.

## Coding Style & Naming Conventions
Follow the existing file style: TypeScript and JSON use 2-space indentation, single quotes, and `camelCase` symbols; React components use `PascalCase`; test files use `*.test.ts` or `*.test.tsx`. C# uses 4-space indentation, file-scoped namespaces, `PascalCase` public members, and `_camelCase` private readonly fields. There is no Prettier config here, so rely on the current formatting and `npm run lint`.

## Testing Guidelines
Extension tests use Jest with `ts-jest`; backend tests use xUnit. Add or update tests with every behavior change in commands, scoring, governance, tree discovery, or webview UI. CI does not enforce a numeric coverage threshold, but changed code should ship with targeted coverage and, for UI-facing changes, a quick local smoke check in VS Code.

## Commit & Pull Request Guidelines
Recent history follows Conventional Commit style with optional scopes, for example `feat(governance): ...` or `docs: ...`. Keep commits focused and imperative. PRs should use the template in `.github/PULL_REQUEST_TEMPLATE.md`: include a short summary, link the issue with `Fixes #...`, list validation performed, and add screenshots or doc updates when behavior or UI changes.

## Rule
For README.md, CHANGELOG.md, marketplace descriptions, release notes,
and user-facing documentation:

Prefer:
- normal text
- bold text
- headings
- bullet lists

Avoid:
- inline code formatting for feature names, UI labels, workflows,
  concepts, personas, analyzers, readiness states, and roadmap items.
