# PBIR release-candidate reconciliation — 2026-08-21

- Confirmed the active checkout is `bulova23/pbir-design-analyzer`, not the unrelated Sales repository context; the supplied `7acee81` SHA is not present here.
- Protected `main` remains `a67af7ad`; PR #6 remains open with no merge commit or approving review. Its remote head is `72e7287698eb50be162b0eb23a76eb047bd405fc`.
- The previous evidence commit `926523995e8c352e5c551757e238d4bba7c6c563` is not on protected `main`. PR head adds a CodeQL-safe package-acceptance change and removes the unsupported hosted `macos-13` acceptance leg from CI/release workflows; this is a functional workflow delta requiring release revalidation after merge.
- Rulesets `main-production-protection` (`21156915`) and `release-tag-protection` (`21156927`) are active with no bypass actors. Required package contexts match the five emitted matrix checks. Main ruleset approval count is `0`, not the expected `1`; this remains an unresolved governance mismatch. Tag deletion and non-fast-forward updates are blocked.
- PR head CI run `32516901572` completed successfully for all emitted build, package, runtime, contract, release-gate, and security checks. Extension version remains `0.7.0`; target is `1.0.0`.
- No rulesets, branches, tags, releases, Marketplace publication, application files, or evidence-history records were changed. Prior dirty memory/evidence files were preserved.
