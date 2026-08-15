# PBIR Engineering Remediation Release Candidate Validation

Date: 2026-06-15

## Scope

- Validate remediation Buckets A-D for release-candidate readiness only
- No feature work
- No refactors

## Workstream 9 Confirmation

- Confirmed complete from repo memory, prior implementation notes, and passing validation already recorded for the Workstream 9 cleanup.

## Commands Run

From `vscode-extension/`:

- `npm test`
- `npm run compile`
- `npm run verify:backend:targets`
- `npm run package:all`

From repo root:

- `dotnet test service-dotnet/tests/Tests.csproj -c Release`

## Command Results

- `cd vscode-extension && npm test`
  - Passed
  - Extension Jest: 91 suites, 420 tests
  - Webview Jest: 9 suites, 58 tests
- `cd vscode-extension && npm run compile`
  - Passed
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - Passed
  - 288 tests passed, 0 failed
- `cd vscode-extension && npm run verify:backend:targets`
  - Passed
  - Verified 5 packaged backend targets under `vscode-extension/backend/targets`
- `cd vscode-extension && npm run package:all`
  - Passed
  - Generated 5 VSIX artifacts

## VSIX Artifacts

Generated:

- `vscode-extension/pbir-design-analyzer-0.6.0-darwin-arm64.vsix`
- `vscode-extension/pbir-design-analyzer-0.6.0-darwin-x64.vsix`
- `vscode-extension/pbir-design-analyzer-0.6.0-linux-x64.vsix`
- `vscode-extension/pbir-design-analyzer-0.6.0-win32-arm64.vsix`
- `vscode-extension/pbir-design-analyzer-0.6.0-win32-x64.vsix`

Installed for validation:

- `pbir-design-analyzer-0.6.0-darwin-arm64.vsix`

Installed extension version:

- `bcrowell.pbir-design-analyzer@0.6.0`

## Install Environment

- VS Code version: `1.124.2`
- Platform: macOS arm64
- Clean user data dir: `/tmp/pbir-rc-vscode-user`
- Clean extensions dir: `/tmp/pbir-rc-vscode-ext`
- Disposable PBIR copy:
  - `/tmp/pbir-rc-validation/SalesAndProduction`

## Manual Smoke Results

### Passed

- Extension installed into a clean host successfully.
- Extension activated from the clean host when the PBIR Design Analyzer activity view opened.
- Packaged backend launched successfully from the installed VSIX path:
  - `/tmp/pbir-rc-vscode-ext/bcrowell.pbir-design-analyzer-0.6.0/backend/rpc/ModelingLanguageServer`
- Backend startup diagnostics were understandable and pointed at packaged assets only.
- Score report command worked against the disposable PBIR workspace.
- Score panel opened and rendered the Optimization Report workspace.
- Export workflow worked.
  - Exported `review-workflow-summary.json` to:
    - `/tmp/pbir-rc-validation/SalesAndProduction/review-workflow-summary.json`
- Screenshot upload flow is available.
  - The command opened the native file-picker dialog in the score-panel context.

### Failed

- Design Studio did not render correctly from the packaged VSIX.
  - The clean-host session opened a `Report Design Studio` tab with blank content.
  - VS Code `main.log` recorded:
    - `Blocked vscode-webview request ... extensionId=bcrowell.pbir-design-analyzer ...`

### Partially Verified

- Score panel rendering
  - Confirmed panel launch and top-level Optimization Report workspace rendering.
  - Did not complete a full visual walkthrough of every score workspace section after the Design Studio blocker was found.
- Output channel hygiene
  - Startup diagnostics were understandable.
  - Backend method logging was compact in the backend channel.
  - However, default score diagnostics logging still persisted a large scored payload to the output log.

### Not Completed Because Of Release Blockers

- Full Story Assessment visual walkthrough
- Guided Story Improvements verification
- Issues and Fix Plan workflow walkthrough
- Navigation and deep-link validation
- Deterministic fix preview/apply/rollback on the disposable report copy
- Design Studio shell and workflow rail verification
- Prepare For Review / Review Design verification
- Analyzer Handoff shell verification
- Compare Iterations verification

Those checks were blocked by the packaged Design Studio webview failure and the resulting inability to validate downstream Design Studio workflow surfaces safely in the release-candidate build.

## Runtime Behavior Confirmation

### Packaged Targets Only

- Confirmed from extension activation logs that the backend resolved to the installed VSIX payload under:
  - `/tmp/pbir-rc-vscode-ext/bcrowell.pbir-design-analyzer-0.6.0/backend/rpc/ModelingLanguageServer`
- No repo-local `Debug` or `Release` fallback path was used in the clean-host session.

### No Double Backend Launch In The Clean Host

- Clean-host extension log showed one backend startup sequence.
- Clean-host backend log showed one `Ready for requests` startup event.
- A separate `ModelingLanguageServer` process existed for a different local VS Code Insiders environment, but it was unrelated to the clean validation host and had a different parent process and extension root.

## Output Channel Findings

### Startup Diagnostics

- Acceptable.
- The extension output channel clearly reported:
  - extension id
  - extension path
  - selected VSIX target
  - resolved backend path
  - runtime packaging mode
  - backend startup result

### Logging Hygiene Failure

- Not acceptable for release-candidate signoff.
- `PBIR Score Diagnostics` persisted a large scored payload by default in:
  - `/tmp/pbir-rc-vscode-user/logs/20260615T175909/window1/exthost/output_logging_20260615T175910/1-PBIR Score Diagnostics.log`
- Observed issues:
  - 9,568-line diagnostics log
  - serialized findings payloads
  - local report root paths such as `/tmp/pbir-rc-validation/SalesAndProduction/Sales & Production.Report`

This does not meet the requested validation bar of “no full RPC payloads logged by default” and “no sensitive report paths/content dumped unnecessarily”.

## Failures Found

### 1. Packaged Design Studio webview fails to render

- Severity: Release blocker
- Evidence:
  - blank `Report Design Studio` tab in the clean packaged host
  - `main.log` recorded a blocked `vscode-webview` request for `bcrowell.pbir-design-analyzer`
- Impact:
  - prevents validation of the shipped Design Studio experience
  - blocks validation of Prepare For Review, Review Design, Analyzer Handoff shell, and Compare Iterations

### 2. Default score diagnostics logging still dumps large scored payloads

- Severity: Release blocker
- Evidence:
  - `1-PBIR Score Diagnostics.log` persisted a 9,568-line scored payload log by default
  - log included findings and local report paths
- Impact:
  - violates the release-candidate logging-hygiene requirement
  - increases unnecessary local-content exposure in normal operation

## Release Blockers

- Packaged Design Studio webview does not render in the clean installed VSIX host.
- Default score diagnostics logging still records large scored payloads and report paths.

## Non-Blocking Warnings

- `npm run package:all` still emits existing backend nullable warnings during packaging, including warnings in:
  - `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeInputBuilder.cs`
  - `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Packaging still completed successfully and produced all expected VSIX artifacts.

## Recommendation

- Not ready for internal install.

Rationale:

- Automated validation and packaging passed.
- Clean-host install succeeded.
- Core packaged scoring path worked.
- Release-candidate manual validation found two blockers:
  - packaged Design Studio webview failure
  - default diagnostics payload logging failure

These should be resolved and revalidated before internal release-candidate distribution.
