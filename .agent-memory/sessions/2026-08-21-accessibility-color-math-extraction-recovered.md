# Session — 2026-08-21 baseline recovery and AccessibilityColorMath extraction

## Baseline recovery

- Original branch: `codex/hosted-v1-readiness-validation-2026-08-21`.
- Original HEAD: `926523995e8c352e5c551757e238d4bba7c6c563`.
- Original branch was not based on the release: its merge-base with `v1.0.0` was `cf95f81941e636072292761908015b880eed727f`, and it had no commits beyond the release because the release was on a separate descendant line.
- Immutable `v1.0.0` and `origin/main`: `4c56eaf37f4829640051ec121d9f6f5103aa7084`.
- Recovery: created linked worktree `.worktrees/architecture-post-v1-decomposition` on `architecture/post-v1-decomposition` from the immutable release commit.
- Approved design and plan were preserved in one traceable commit, `9913e704`.
- No unrelated historical commits were imported. The original worktree remains with its dirty release-evidence and session-memory files preserved.

## Baseline evidence

- Focused scoring and existing architecture boundary tests: 142 passed.
- Contract freshness/compatibility: passed; 6 fixtures compatible.
- Baseline characterization: passed; read-only goldens and fingerprints recorded below.
- Baseline fingerprints: custom `5fac615e73d5f0349eecb9c767b8e5485182122fe1df153718f0a6154076a28e`; minimal `98fa5e717eeaeada46392584838f05b7e6b50a7e35516151a97917a3f03e3a8e`; minimal corpus `a186399eba356a9946b54036c9a218aa90f3b521ccac0e0f7bd564442522d17f`; multipage `4f0c1e26b0378dc04f3ed5f2aa608381733b7f4aebc4dde1763224c146dadfbe`; hidden navigation `62e8aeecaabe7cab372b8b6af627eb88454b82e17e357775a14a44a165cc2003`; malformed `2cd55512d4305e8716cf3effa0834abcad7825b503bf6a3264c4dd7f432e64e8`.

## Extraction

- Added `service-dotnet/Services/Pbir/AccessibilityColorMath.cs` as an internal stateless pure helper owner.
- Moved exactly: `TryNormalizeHex`, `LooksLikeRedGreenPair`, `IsRedDominant`, `IsGreenDominant`, `SimulatesToSimilarUnderDeuteranopia`, `SimulateDeuteranopia`, and `HexToRgb`.
- Updated existing accessibility, visual metadata, and semantic-color call sites only. WCAG contrast remains in `WcagContrastCalculator`.
- Added focused direct tests and a narrow source-level ownership guard; no interfaces, DI, providers, transport, authoring, mutation, logging, or filesystem dependencies.

## Validation

- Focused extraction/scoring/architecture tests: 160 passed.
- Full backend suite: 1,054 passed, 11 expected skips.
- Characterization Run 1: PASS. Characterization Run 2: PASS.
- Both runs matched the baseline goldens/fingerprints; golden changes: NONE.
- Contract checks: passed. `git diff --check`: passed.
- Behavior status: `NONE`.

## Lineage

- Branch: `architecture/post-v1-decomposition`.
- HEAD before implementation commit: `9913e7049f0987403a0440f580f023a7f3dda2f4`; merge-base with `v1.0.0`: `4c56eaf37f4829640051ec121d9f6f5103aa7084`.
- History after release contains only the approved planning commit before the extraction implementation.

## Next task

Extract webview score-formatting and label projections. Do not start it in this session.
