# 2026-06-10 0.5.2 Operational Coherence Implementation

## Objective

Implement Recommended `0.5.2` only from the engineering hardening roadmap:

- output channel consolidation
- namespace unification
- workspace capability declarations
- telemetry posture decision
- troubleshooting cleanup

Explicitly out of scope:

- Recommended `0.5.1` deterministic mutation work
- Recommended `0.6.0` performance, protocol, and scalability work
- packaging rebuilds
- backend scoring or service-dotnet behavior changes

## Completed

### Checkpoint 1

- Added a shared output-channel registry for:
  - PBIR Design Analyzer
  - PBIR Design Analyzer Backend
  - PBIR Design Analyzer Backend Trace
  - PBIR Score Diagnostics
- Rewired extension activation, backend transport, bridge logging, command error logging, and score diagnostics to reuse those singleton channels.

### Checkpoint 2

- Promoted `pbirAnalyzer` as the canonical namespace for extension commands, explorer view ID, and active governance settings.
- Added legacy command alias routing from `pbir.*` to the canonical `pbirAnalyzer.*` commands.
- Added canonical `pbirAnalyzer.governance.*` settings while preserving fallback reads from legacy `powerbi-modeling.governance.*` settings in code only.

### Checkpoint 3

- Declared unsupported posture for untrusted workspaces and virtual workspaces in the extension manifest.
- Made telemetry behavior explicit as local-only/no-op for this scope.
- Updated troubleshooting guidance to match the current command names, explorer label, and packaged-backend restart flow.

## Validation

### Focused

- Passed:
  - `cd vscode-extension && npx jest src/test/outputChannels.test.ts src/test/packageManifest.test.ts src/test/pbirGovernanceCommand.test.ts src/test/pbirReviewWorkflowExportCommand.test.ts src/test/telemetryReporter.test.ts --runInBand`

### Full Required Validation

- Passed:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run compile`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run package:all`

### Release Artifact Inspection

- Rebuilt VSIX artifacts verified:
  - `pbir-design-analyzer-0.5.0-win32-x64.vsix`
  - `pbir-design-analyzer-0.5.0-win32-arm64.vsix`
  - `pbir-design-analyzer-0.5.0-linux-x64.vsix`
  - `pbir-design-analyzer-0.5.0-darwin-x64.vsix`
  - `pbir-design-analyzer-0.5.0-darwin-arm64.vsix`
- Confirmed from packaged contents:
  - extension version remained `0.5.0`
  - explorer view ID is `pbirAnalyzer.explorer`
  - untrusted and virtual workspace capabilities are declared `supported: false`
  - backend binaries stayed target-specific:
    - Windows x64: PE32+ x86-64
    - Windows arm64: PE32+ Aarch64
    - Linux x64: ELF x86-64
    - macOS x64: Mach-O x86_64
    - macOS arm64: Mach-O arm64
  - no stale `powerbiModeling.pbirExplorer` or `powerbi-modeling.governance.*` identifiers remained in release-facing metadata

### VS Code Smoke

- Passed in a real VS Code extension host:
  - canonical explorer metadata resolves to `pbirAnalyzer.explorer`
  - legacy command aliases remain registered:
    - `pbir.refreshTree`
    - `pbir.scoreReport`
  - canonical commands remain registered:
    - `pbirAnalyzer.refreshReports`
    - `pbirAnalyzer.scoreReport`
  - invoking canonical and legacy commands after activation created no new output channels, confirming post-activation reuse
- Skipped:
  - actual untrusted-workspace blocked-host smoke
  - actual virtual-workspace blocked-host smoke
- Exact reason:
  - the local VS Code test harness available in this session always opens the file-backed repo workspace as trusted (`vscode.workspace.isTrusted === true`) and does not expose a supported CLI or API to force that same workspace into an actual untrusted state
  - this session also does not have a real virtual filesystem workspace provider to open the extension inside a true virtual workspace
  - blocked posture was therefore validated at packaged manifest level only for those two cases

## Risks / Notes

- Legacy command aliases remain intentionally registered for migration compatibility even though `pbirAnalyzer` is now the canonical command family.
- Legacy `powerbi-modeling.governance.*` settings remain readable for migration compatibility but are no longer exposed in contributed configuration metadata.
- Packaging validation initially revealed stale release-facing legacy config identifiers in the manifest; that was corrected before final completion and the full extension/package validation was rerun.

## Next Step

- If you want runtime proof beyond packaged manifest declarations for blocked posture, run two external manual checks in an interactive VS Code environment that can:
  - open the repo as an actually untrusted local workspace
  - open the extension inside a true virtual workspace provider
- Otherwise the next implementation step remains Recommended `0.6.0` only after explicit approval.
