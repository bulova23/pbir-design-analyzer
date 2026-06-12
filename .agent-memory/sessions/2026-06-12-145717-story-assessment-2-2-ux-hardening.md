# Story Assessment 2.2 UX Hardening

Date: 2026-06-12 14:57:17 -0400

## Objective

Fix the release-blocking Story Assessment 2.2 UX defects:

- broken Open target deep-link navigation
- overly punitive Story Maturity calibration
- generic Story Improvement Rationale wording

## Scope

- extension host and webview navigation path
- PBIR tree target resolution for page and visual targets
- Story Assessment public maturity and rationale presentation
- focused tests, full extension validation, and repo-memory updates

## Constraints

- do not add new Story Assessment signals
- do not expose internal Story Assessment diagnostics
- preserve the current Story Assessment layout unless required by the fix
- keep scoring authoritative and treat Story Assessment UX changes as downstream presentation or safe public-output shaping

## Notes

- Existing Story Assessment 2.2 implementation already added `navigateToTarget`; this session is validating and correcting the end-to-end wiring rather than redesigning the feature.
- Early root-cause hypothesis: visual-level target resolution is brittle because score payload targets carry PBIR visual IDs while the local explorer tree may match on visual display names.

## Outcome

- Fixed the release-blocking Story Assessment UX defects:
  - corrected PBIR explorer visual matching so Open target can resolve stable PBIR visual ids even when the tree label uses a display name
  - added focused reveal coverage for page and visual targets plus host warning behavior for unresolved targets
  - centralized Story Maturity calibration into one shared helper used by both the webview and Story Assessment diff snapshots
  - softened Draft classification so recognizable but incomplete pages stay Developing more often
  - replaced the generic Story Improvement Rationale fallback with page-specific public wording derived from the actual promoted improvement set

## Validation

- Passed:
  - `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/pbirTreeProvider.localFallback.test.ts src/test/pbirScorePanel.navigation.test.ts src/test/storyAssessmentPresentation.test.ts src/test/pbirExplorerReveal.test.ts src/test/storyAssessmentSnapshot.test.ts`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- Not run:
  - manual VSIX smoke
  - reason: this session completed automated validation and documented the required smoke workflow, but did not perform a local install-and-click run

## Next Recommended Step

- If a final release gate requires live runtime confirmation, install the local VSIX and run the documented Story Assessment Open target smoke against a real PBIR report.
