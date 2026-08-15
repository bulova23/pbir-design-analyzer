# PBIR Design Analyzer 0.6.0 RC1 Known Issues

Only confirmed limitations are listed here. Deferred roadmap items are not
defects and are tracked in [V2-ROADMAP.md](V2-ROADMAP.md).

## Confirmed limitations

1. Full ESLint does not pass. The repository baseline currently reports 43
   errors across existing extension and test files. This RC introduces no new
   lint errors, but the baseline remains a release-quality debt item.
2. Eleven Windows containment integration tests are skipped on the current
   non-Windows host. Windows execution behavior therefore requires a Windows
   UAT pass before any claim of platform-wide containment validation.
3. The local manual environment cannot prove true virtual-workspace runtime
   behavior because no virtual workspace provider/session is available. The
   package declares the unsupported posture.
4. Authoring handles are process-local and expire when the backend restarts.
   They must not be persisted or treated as portable IDs.
5. Imported PBIR support is bounded by the pinned schema and descriptor catalog.
   Unsupported semantic roles are preserved diagnostically but are not typed or
   mutable.
6. Public mutations are single-operation curated workflows. Public batching,
   capability discovery, and backend-only mutation families are not supported
   user workflows.
7. The generated Windows ARM64 package is substantially larger than the other
   target packages because it is self-contained. This is packaging behavior,
   not a functional defect.

## Release handling

The first four items require explicit UAT acknowledgement. A data-loss,
identity-preservation, stale-handle, startup, or package-content failure is a
release blocker even if automated tests pass.
