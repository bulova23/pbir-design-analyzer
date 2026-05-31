# Current Focus

## Active Branch

- Branch: `feat/semantic-color-chart-intent`

## Current Objective

- Finalize the `0.2.0` release by curating the feature worktree, preserving only durable repo memory, validating the curated payload, merging into `main`, and packaging the final VSIX from `main`.

## Release Boundaries

- Keep completed product code, tests, docs, roadmap specs/plans, and compact durable memory.
- Do not implement deferred roadmap epics in this release.
- Keep scoring authoritative and unchanged.
- Keep Evidence and Export secondary in the shipped workspace UX.

## Remaining Steps

1. Prune transient `.agent-memory` session clutter and generated test-host artifacts.
2. Validate the feature worktree with compile and test commands.
3. Commit the curated `0.2.0` payload on the feature branch.
4. Merge into `main` and re-run validation there.
5. Package `pbir-design-analyzer-0.2.0.vsix` from `main`.
6. Record final package path, validation results, and deferred roadmap references.

## Next Recommended Step

- Finish release-payload curation first; do not merge until `.agent-memory`, docs, and generated artifacts have been cleaned.
