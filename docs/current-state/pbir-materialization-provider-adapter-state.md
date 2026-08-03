# PBIR Materialization Provider Adapter Current State

## Status

No Repository Phase 32 provider-facing materialization adapter is implemented or roadmap-mapped.

Repository Phase 31 is the last explicitly mapped local PBIR slice. It exposes Phase 29 serialization and Phase 30 preview, apply, and recovery inspection through PbirMaterializationOrchestrationService. The original Phase 4 roadmap then identifies the first concrete Microsoft PBIR adapter and broader external execution work, but it does not define an intermediate provider-facing transport adapter or assign that work to Repository Phase 32.

## Mapping Discrepancy

The requested adapter cannot be treated as an existing-roadmap implementation detail for three reasons:

- Provider Adapter Framework, Execution Provider Contract Framework, and Runtime Provider Framework are planning or pre-execution contract seams. Their current-state documents explicitly exclude provider invocation and runtime-provider implementations.
- The original Phase 4 scope describes a Microsoft PBIR adapter that maps Generation Request intent, checks PBIP/PBIR prerequisites, and participates in structural validation. That is broader and semantically different from a transport wrapper over local Phase 31 materialization.
- RpcHost is the only shipped backend transport, but it has no per-request cancellation registry or cancellation notification handling. It processes requests serially and uses permissive JSON deserialization. Therefore cancellation, concurrent request handling, strict unknown-field rejection, bounded payload validation, interrupted responses, and provider-disconnect behavior are not existing lifecycle guarantees that a narrow adapter can merely reuse.

Because these seams do not align, naming the requested work Repository Phase 32 would invent a roadmap mapping and conceal transport work inside a materialization adapter.

## Smallest Roadmap-Aligned Alternative

Authorize a design-only roadmap decision before implementation. The decision should choose and name exactly one boundary:

1. a local materialization transport adapter over PbirMaterializationOrchestrationService, with an explicit prerequisite slice for RpcHost request-lifecycle hardening; or
2. the first runtime-provider implementation downstream from runtime-provider/v1 and execution-provider/v1, which is broader original Phase 4 provider work and must not be represented as a transport-only wrapper.

The smaller option is the first one. Its design must explicitly map the new repository phase, distinguish transport contracts from provider-planning contracts, define strict JSON and size policy, add cancellable concurrent request lifecycle semantics, and preserve Phase 31 as the only callable materialization application service. Only after that mapping is approved should a Phase 32 implementation plan or production code be created.

## Preserved State

- No provider, runtime-provider, Microsoft Skills, API, CLI, deployment, publishing, Desktop, Analyzer, extension command, dialog, notification, tree view, or webview behavior was added.
- No Phase 29, Phase 30, or Phase 31 production contract or service was changed.
- No commit, push, pull request, merge, discard, or working-tree cleanup was performed.
