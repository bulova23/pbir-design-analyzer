# Phase 43 Lossless Authoring IR — Current State

Status: **implementation complete; acceptance gate passed on 2026-08-14**.

Task 1 is approved by the repository goal. This document freezes the reconciled contract for Tasks 2–10; it does not create a replacement plan.

## Contract

Phase 43 provides semantic-lossless bounded authoring for valid PBIR imported under the pinned `PbirDeployableSchemaLock`. The imported source document is the preservation authority; typed IR owns only supported semantic fields; a declared mutation overlay is applied by one copy-on-write merge service; the deterministic serializer and existing validators own the final artifact. This is not byte-for-byte fidelity and is not a generic JSON editor.

Generation-only requests v1–v7 remain on the existing rebuild path and do not require an authoring envelope. No public RPC, VS Code UI, Desktop, Windows, hosted execution, scoring redesign, or Phase 44 implementation is part of this phase.

## Frozen operation matrix

| Operation/domain | Classification | Phase 43 boundary |
| --- | --- | --- |
| Imported visual resize/layout | typed-and-mergeable | Replace only the owned `position` fields; preserve the rest of the visual document. |
| Imported visual move/page-order change | typed-and-mergeable only where the existing path has a deterministic owner mapping | Preserve imported folder identity and semantic order; reject ambiguous relocation. |
| Imported page rename | typed-and-mergeable only through the existing page-owned typed field | Change the declared page display field, never the imported folder identity. |
| Page/visual identity, references, bindings, Phase 42 interactions | preserved | Retain imported source content and identity; no implicit regeneration. |
| Formatting, theme, filters, navigation, slicer metadata | preserved-but-not-authorable | Preserve admitted source content; reject typed mutations until a closed merge path exists. |
| Unknown-but-valid admitted properties | preserved-but-not-authorable | Retain in the owned source document; never expose JSON Patch/Pointer or replacement fragments. |
| Invalid JSON, unsupported schema/owner/path, ambiguous identity, missing target, identity collision, schema-invalid result | unsupported/fail closed | Emit existing typed diagnostics and do not produce a ready artifact. |

## Existing implementation reconciliation

The committed Phase 43 envelope, imported reader state, identity provenance, layout merge, serializer hook, and standalone fidelity classifier were reused. Admission now fails closed for unsupported owned schemas, imported identity takes precedence over generated/explicit identity, imported mutation acceptance has one typed merge boundary, serializer preservation conflicts block readiness, and authoring-specific fidelity evidence distinguishes preserved from intended changes.

## Acceptance gate

No-op imported round trips must be semantically equivalent. A bounded typed mutation must change only declared paths while preserving unrelated admitted content, identities, ordering, bindings, and Phase 42 interactions. Repeated equivalent operations must produce equivalent canonical output, and the final artifact must pass the pinned schema, structural, cross-reference, hash, and analyzer boundaries.

## Bounded performance observation

The existing representative Phase 44 pipeline timing harness, run as Phase 43 evidence, observed: reader/envelope `3 ms`, semantic projection `0 ms`, planning `6 ms`, execution `0 ms`, merge `1 ms`, deterministic serialization `1 ms`, and analyzer `84 ms`. These are observations on a focused local fixture, not thresholds. The serializer timing covers the existing serializer path alongside the authoring merge; no optimization or cache was added. The bounded limitation is that Phase 43 supports imported visual resize/layout only as typed mutation; other admitted authoring domains remain preserved-but-not-authorable.

## Protected boundary

Pre-existing Phase 44 semantic-projection work is protected and remains outside this implementation. Phase 43 completion does not authorize Phase 44.
