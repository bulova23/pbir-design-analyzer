# Current Focus

## Active Branch

- Branch: `codex/ux-consolidation-remediation-0-2-2`

## Current Objective

- Complete the Recommended `0.5.2` operational-coherence bundle from the engineering hardening roadmap without expanding into `0.6.0`.
- Keep the implementation limited to output-channel consolidation, namespace unification, capability declarations, telemetry posture clarification, and troubleshooting cleanup.

## In Progress

- Recommended `0.5.2` implementation is now complete for the approved scope:
  - shared singleton output-channel registry for extension, backend, backend trace, and score diagnostics
  - canonical `pbirAnalyzer` command/view/config namespace
  - legacy `pbir.*` command alias routing for migration compatibility
  - canonical `pbirAnalyzer.governance.*` settings with fallback reads from legacy `powerbi-modeling.governance.*` kept in code only
  - explicit unsupported posture for untrusted workspaces and virtual workspaces
  - explicit local-only/no-op telemetry posture
  - troubleshooting guide cleanup to match shipped command names and backend restart flow
  - focused regression coverage for output channels, manifest declarations, config fallback, alias routing, and telemetry behavior
- Authoritative roadmap docs remain:
  - `docs/superpowers/specs/2026-06-06-engineering-hardening-design.md`
  - `docs/superpowers/plans/2026-06-06-engineering-hardening-plan.md`

## Blockers

- No active blocker remains inside the Recommended `0.5.2` scope.
- Remaining open hardening work is intentionally deferred to Recommended `0.6.0`.

## Validation Status

- Focused checkpoint validation passed:
  - `cd vscode-extension && npx jest src/test/outputChannels.test.ts src/test/packageManifest.test.ts src/test/pbirGovernanceCommand.test.ts src/test/pbirReviewWorkflowExportCommand.test.ts src/test/telemetryReporter.test.ts --runInBand`
- Full required validation passed:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run compile`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run package:all`
- Trusted-host VS Code smoke passed for:
  - canonical explorer metadata
  - legacy alias routing
  - post-activation output-channel reuse
- Actual blocked-host smoke for untrusted and virtual workspaces remains externally pending because the local VS Code test harness could not force a file workspace into untrusted mode and did not provide a true virtual workspace provider.

## Release Boundaries

- Stop at Recommended `0.5.2`.
- Do not start Recommended `0.6.0` scale/protocol work in this follow-up.
- Do not expand into new feature pillars while the remaining hardening roadmap items are still intentionally deferred.

## Next Recommended Step

- Smoke-test the completed `0.5.2` runtime/platform changes in VS Code against a real PBIR workspace:
  - actual untrusted-workspace blocked posture
  - actual virtual-workspace blocked posture
- If those two external posture checks are clean, move next into Recommended `0.6.0` only after explicit approval.
- Keep shared repo snapshotting, async I/O, protocol versioning, selected state validation, and scale work deferred exactly as scoped in the hardening roadmap.
