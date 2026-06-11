# Current Focus

## Active Branch

- Branch: `codex/ux-consolidation-remediation-0-2-2`

## Current Objective

- Recommended `0.6.0` is complete on the active branch.
- Story Assessment 2.0 validation planning is documented and the first implementation slice is now complete:
  - Workstream 1 Validation Substrate
  - Workstream 2 PBIR Expert Review Validation Framework
- Story Assessment 2.0 Workstream 3 is complete:
  - internal Signal Registry runtime extraction
  - representative signal capture
  - signal classification
  - validation coverage
- Story Assessment 2.0 Workstream 4 is now complete:
  - internal archetype scoring
  - matched and missed signal recording
  - Level 1 validation harness
  - promotion gate definition
- Story Assessment 2.0 Workstream 5 is now complete:
  - internal semantic coherence scoring
  - dominant concept detection
  - deterministic term clustering
  - focused-versus-split coherence classification
  - promotion-delayed competing-story detection
  - expert-review validation support
- Keep Story Assessment outputs internal-only until explicit promotion evidence exists.

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
- New planning-only Story Assessment 2.0 docs now exist:
  - `docs/superpowers/specs/2026-06-10-story-assessment-2-design-validation.md`
  - `docs/superpowers/plans/2026-06-10-story-assessment-2-implementation-plan.md`
- Story Assessment 2.0 foundation implementation now exists:
  - internal-only validation substrate models
  - PBIR validation corpus guidance
  - reviewer rubric
  - reviewer workflow
  - validation observations placeholder
- Story Assessment 2.0 runtime foundation now also exists:
  - internal-only signal registry extraction in backend scoring
  - representative layout, semantic, and context signal capture
  - focused xUnit coverage for runtime capture and graceful degradation
- Story Assessment 2.0 Workstream 4 validation foundation now also exists:
  - internal-only best-fit archetype scoring for six planned archetypes
  - matched/missed signal recording from the Signal Registry
  - Level 1 reviewer-harness placeholders
  - internal promotion gate thresholds for future contract eligibility
  - focused archetype-validation xUnit coverage
- Story Assessment 2.0 Workstream 5 validation foundation now also exists:
  - internal-only semantic coherence scoring
  - deterministic extracted-term normalization and token clustering
  - dominant concept detection
  - focused/split/sparse coherence classification
  - precision-first competing-story detection with promotion-delayed status
  - focused coherence-validation xUnit coverage

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

- Review Workstream 5 outputs before proceeding further.
- If Story Assessment 2.0 implementation continues later, start with Workstream 6 only:
  - filter topology extraction
  - reinforcement-only topology classification
  - continued internal-only boundaries until Level 1 evidence supports promotion
- Do not promote new Story Assessment fields to the score-panel contract until Level 1 validation evidence exists.
- Keep the remaining runtime-validation gap explicit:
  - rerun untrusted-workspace blocked-posture smoke in an environment that can actually produce `vscode.workspace.isTrusted === false`
  - run a true virtual-workspace blocked-posture smoke once a virtual workspace provider/session is available
