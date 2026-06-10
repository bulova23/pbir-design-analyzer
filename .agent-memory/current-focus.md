# Current Focus

## Active Branch

- Branch: `codex/ux-consolidation-remediation-0-2-2`

## Current Objective

- Recommended `0.6.0` is complete on the active branch.
- Keep the next step focused on release integration, remaining external smoke gaps, and the next roadmap epic rather than reopening the `0.6.0` hardening scope.

## In Progress

- Recommended `0.5.1`, `0.5.2`, and `0.6.0` are complete.
- `0.6.0` delivered:
  - shared repository snapshot seam
  - async repository traversal for the local PBIR fallback tree and Fabric evidence extraction
  - shared-snapshot Fabric evidence reuse
  - host/webview protocol versioning and schema guards
  - selected state validation
  - externalized Fabric scoring configuration with provenance
- Authoritative roadmap docs remain:
  - `docs/superpowers/specs/2026-06-06-engineering-hardening-design.md`
  - `docs/superpowers/plans/2026-06-06-engineering-hardening-plan.md`

## Blockers

- No blocker remains inside the implemented `0.6.0` scope.
- External validation gap remains for a true virtual-workspace runtime smoke.
- Attempted untrusted-workspace runtime smoke still reported `vscode.workspace.isTrusted === true` under the local VS Code test host, so this environment could not prove the blocked posture beyond packaged manifest declarations.

## Validation Status

- `0.6.0` validation passed:
  - focused phase-by-phase Jest runs
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package:all`
- VSIX inspection confirmed version, target integrity, backend target specificity, and current release-facing namespace/capability metadata.
- `0.5.2` validation remains on record as previously completed.

## Release Boundaries

- Stop at Recommended `0.6.0`.
- Do not expand into new feature pillars while the remaining hardening roadmap items are still intentionally deferred.

## Next Recommended Step

- Keep the remaining runtime-validation gap explicit:
  - rerun untrusted-workspace blocked-posture smoke in an environment that can actually produce `vscode.workspace.isTrusted === false`
  - run a true virtual-workspace blocked-posture smoke once a virtual workspace provider/session is available
- Start the next roadmap epic only after deciding whether that external smoke proof is required before release integration.
