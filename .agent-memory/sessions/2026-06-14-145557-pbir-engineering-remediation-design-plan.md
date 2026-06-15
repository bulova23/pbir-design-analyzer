# 2026-06-14 PBIR Engineering Remediation Design And Plan

## Scope

- documentation and planning only
- no product-code changes
- no source-file modifications outside repo memory and planning docs
- no implementation, refactor, or file removal

## Authoritative Inputs

- principal-architect repository review findings from the current session
- `docs/superpowers/specs/2026-06-06-engineering-hardening-design.md`
- current repository architecture and trust-boundary guidance

## Planned Deliverables

- `docs/superpowers/specs/2026-06-14-pbir-engineering-remediation-design.md`
- `docs/superpowers/plans/2026-06-14-pbir-engineering-remediation-plan.md`

## Intent

- convert the repository review findings into a staged engineering hardening roadmap
- group the findings into executable workstreams
- define sequencing, validation, rollback guidance, and release buckets for future implementation turns

## Notes

- stop after writing the remediation spec, remediation plan, and repo memory updates
- preserve existing architecture boundaries unless the plan explicitly documents a staged decomposition target

## Created

- `docs/superpowers/specs/2026-06-14-pbir-engineering-remediation-design.md`
- `docs/superpowers/plans/2026-06-14-pbir-engineering-remediation-plan.md`

## Outcome Summary

- converted the repository review findings into nine coherent remediation workstreams
- defined:
  - target architecture
  - dependency map
  - release buckets
  - execution order
  - workstream safety boundaries
  - focused and full validation strategy
  - rollback guidance
  - per-workstream definitions of done
- preserved:
  - no implementation work
  - no source refactors
  - no file removals

## Validation

- verified the remediation spec and remediation plan exist on disk
- verified the task-specific working tree changes are limited to planning and repo-memory files

## Next Recommended Step

- begin implementation with Bucket A only:
  - JSON-RPC framing
  - RPC logging redaction
  - score payload validation
  - backend fallback cleanup
  - backend startup preflight cleanup
