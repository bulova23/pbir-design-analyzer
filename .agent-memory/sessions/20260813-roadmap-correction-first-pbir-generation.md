# Roadmap Correction — First PBIR Generation

Date: 2026-08-13

## Scope

Review whether the current roadmap still reflects the original product objective, with particular attention to the Windows/Desktop execution branch introduced by Phases 35G–35L. No code or runtime architecture changes were authorized.

## Findings

- The canonical v1 specification defines PBIR Analyzer as a local PBIP/PBIR analysis product. Creating PBIR reports, semantic-model authoring, and TMDL workflows are explicitly out of scope for v1.
- The original seven-phase roadmap puts PBIR generation in original Phase 4, after planning, and puts Analyzer handoff/refinement/Fabric App generation later.
- Phase 29 is the first repository component that emits modern PBIR artifact bytes. Phase 30 safely materializes those bytes locally. Phase 31–34 expose the local path, but a complete upstream generation input/provider is still absent.
- The Generation Provider Framework is metadata-only and explicitly not a generator or runtime provider.
- Phase 35G selects remote Windows execution for a future provider described as likely Power BI Desktop-dependent. No provider, Desktop run, Windows run, or measured generation failure proves that dependency.
- Phase 35L was blocked on a certified Windows worker, so the containment suite has no Windows evidence.

## Research evidence

Current Microsoft documentation confirms that PBIR is publicly documented, supports manual/programmatic batch changes from non-Power BI applications, and has public schemas. It also documents Desktop-specific PBIP/PBIR creation/conversion flows and Desktop validation on open. TMDL metadata can be edited outside Desktop, with restart/revalidation caveats.

The evidence supports a provider-specific Desktop/Windows requirement when demonstrated, not a universal requirement for the first narrow local PBIR report-definition artifact.

## Decision

Substantially rebaseline the critical path around the first local PBIR generation provider. Preserve 35G–35L as deferred Windows Validation / Hosted Execution infrastructure. Re-scope existing Phase 36 to the first local provider over Phase 29–31, then perform generated-artifact intake and Analyzer handoff.

No implementation was performed. No provider, Windows containment, scoring, or security infrastructure changed.
