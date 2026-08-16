# Phase 35F macOS Containment — Session Record

## Decision

Evaluated App Sandbox, Hardened Runtime/code signing, signed sandbox helpers, XPC/helper launch constraints, Virtualization.framework, container runtimes, and remote isolated execution. On macOS 27.0 / Darwin 27.0.0 / arm64 with the packaged .NET backend, no acceptable local mechanism is proven for all required Phase35C controls. Selected `none-local-macos/v1`; admission is `NotAdmitted`.

## Evidence

- `sandbox-exec` exists, but prior Phase35E custom deny-default probes aborted with exit 134/137.
- A current direct deny-default probe returned exit 71 with `Operation not permitted` and was not treated as enforcement.
- App Sandbox requires a signed app/helper bundle and static entitlements; the current VS Code/.NET backend is not dynamically sandboxable, and child inheritance does not prove child denial.
- Hardened Runtime/code signing protects integrity and injection surfaces but does not enforce the required filesystem/network/resource policy.
- Virtualization.framework is a viable future boundary only with a native signed host, guest image, entitlement, and artifact/teardown design; it was not introduced here.

## Delivered

- `Phase35FContainmentSelector` with platform evidence, explicit per-control states, fail-closed decision, and deterministic evidence hash.
- Representative current-target evidence hash: `3c9ea5bb116357d99456971a81772b8390d897bdc1795989ccecbc27b7aca7ca`.
- Focused tests proving no local admission and that signing/identity is not mislabeled as containment.
- Removed unused unrestricted `Phase35EProcessBoundary` fallback.
- Phase35F design, plan, current-state, threat model, roadmap, architecture-gap, provider-framework, repo-map, and memory updates.

## Validation

- Phase35E/35F focused suite: 11 passed, 0 failed, 0 skipped.
- Full backend: 838 passed, 0 failed, 0 skipped; Phase35A–F focused regression: 65 passed; extension: 494 passed; webview: 68 passed; backend target verification: 5/5; extension build and VSIX package passed; diff/document/boundary scans passed. `npm run lint` remains the documented pre-existing 43-error baseline.
- No provider, fixture, secrets, MCP, Skills, shell, Desktop, PBIR generation, publication, or Fabric mutation was introduced or executed.

## Next step

Keep provider activation blocked. Run the full validation matrix, then perform a design-only comparison of Virtualization.framework guest execution and controlled Windows/Linux remote execution.
