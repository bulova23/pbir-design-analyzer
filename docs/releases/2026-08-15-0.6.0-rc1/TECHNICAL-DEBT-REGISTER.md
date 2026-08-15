# RC1 Technical Debt Register

This register separates actual maintainability debt from future product work.

## High

| Item | Why it matters | Recommended disposition |
| --- | --- | --- |
| Public/internal mutation admission drift | A backend operation can become accidentally user-facing or inconsistently documented. | Before the next major version, define one reviewed admission matrix and contract test. Do not widen RC1. |
| Seven-version request duplication | Validation and behavior can diverge across additive records. | Measure actual consumer usage first; then consider a compatibility projection over one canonical internal model. |
| Checked-in generated target payloads | Packaging rewrites binaries and increases merge/review risk. | Decide whether reproducible target payloads belong in source control; if changing policy, do it as a dedicated release-process change. |

## Medium

| Item | Why it matters | Recommended disposition |
| --- | --- | --- |
| 43-error lint baseline | Lint cannot reliably identify regressions. | Burn down by ownership area, starting with changed extension files; do not mix with product epics. |
| Process-local handle lifecycle | Restart invalidation and unbounded dictionaries limit longer sessions. | Add explicit lifecycle/eviction design only if UAT demonstrates a real workflow need. |
| Descriptor and fallback coverage | Imported fields can be preserved but unavailable for typed mutation. | Expand the catalog only with schema-backed fixtures and round-trip tests. |
| Test nullable warnings | Warning noise weakens release review. | Clean warnings in touched test areas or add a warning policy before v2. |

## Low

- Normalize target package size reporting and document self-contained runtime
  trade-offs.
- Add a concise generated-contract inventory test output for release review.
- Improve manual VS Code smoke automation where the environment can support it.

## Future enhancements, not debt

- Bookmarks, drillthrough, shared slicers, richer formatting, and advanced
  interactions.
- Natural-language authoring, layout optimization, and intelligent review.
- Hosted execution, authentication, collaboration, history, governance, SDK,
  APIs, MCP, automation, CI/CD, and Git workflows.

## Deferred features, not defects

Semantic-model generation, DAX generation, public mutation batching, public
capability discovery, Windows execution activation, hosted execution, provider
security enhancements, and additional RPC operations are deliberately outside
RC1.
