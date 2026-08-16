# Phase 35A — Contract-Only Provider Foundation

## Decision

Phase 35A adds a backend-only, deterministic, versioned governance contract package for future generation providers. It defines provider identity, authoritative request projection, authorization, execution policy, readiness, lifecycle, receipts, results, artifacts, capabilities, failures, hashes, lineage, retries, redaction, and quarantine.

It does not add an executable provider interface, provider discovery by probing, process or shell execution, filesystem provider execution, HTTP/network access, MCP invocation, credential access, Desktop automation, Skills execution, PBIR generation, report materialization, or artifact mutation. Phase 35B or later may add an executable adapter behind these contracts.

## Architectural Fit

The package lives beside the existing Discovery contract layers rather than inside `PbirScoringService` or the RPC host. It consumes the already-governed `GenerationProviderRequest` projection produced from `PbirGenerationSpecificationState`; it does not accept raw provider instructions or replace the existing upstream request contracts.

The new package uses small pure components:

- immutable records and closed enums for the contract model;
- a static catalog for deterministic provider classifications;
- a projector for the authoritative provider-facing request;
- a validator for schema, enum, reference, capability, policy, and relationship checks;
- a readiness evaluator that always starts from `Unavailable` and requires every explicit prerequisite;
- a lifecycle transition validator;
- canonical JSON and SHA-256 helpers for stable identities;
- an offline fake boundary represented only by validated metadata and incapable of execution.

Existing planning/runtime frameworks remain authoritative for their existing contracts. Their pre-execution readiness values are not promoted to Phase 35A runtime readiness.

## Provider Classification

The catalog registers these surfaces:

| Surface | Category | Phase 35A status | Runtime generation ready |
|---|---|---|---|
| `powerbi-report-author@0.1.4` | Local PBIR validation and metadata inspection | Non-executable inspection surface | No |
| Power BI Desktop | Later verification/runtime surface | Deferred beyond Phase 35A | No |
| Power BI Modeling MCP | Semantic-model-only | Non-PBIR generation surface | No |
| Existing Microsoft Skills metadata | Planning/catalog metadata only | No invocation authority | No |
| Existing reference generator/materializer | Offline local/reference or deterministic materialization boundary | Not a provider runtime | No |

No catalog entry is runtime-ready. Package presence, metadata, configuration, executables, filesystem state, Desktop presence, MCP availability, credentials, and network access are not readiness evidence.

## Contract Flow

`Authoritative State → Generation Request Projection → Authorization → Execution Policy → Provider Readiness → Lifecycle → Receipt → Result → Artifact → Lineage`

The request projection contains upstream references, intent reference, capability requirements, and a hash of authoritative inputs. It contains no executable command, endpoint, credential, process path, or provider-specific instruction.

Authorization is an explicit record and is invalid unless approved, scoped to the exact request/provider/capability/artifact set, and backed by the expected policy hash. The Phase 35A default authorization is denied.

Execution policy is closed and deterministic: provider, capability, artifact kind, retry policy, redaction policy, quarantine policy, lineage requirement, and result acceptance criteria are all explicit. The default policy prohibits execution and mutation.

Readiness is `Unavailable`, `Blocked`, or `ReadyForExecution`. The evaluator only returns `ReadyForExecution` when the provider is explicitly registered as executable, every requirement is present, authorization is approved, policy matches, the request is valid, and all lineage/hash checks pass. The current catalog therefore evaluates to `Unavailable`.

The lifecycle is an explicit enum state machine. Invalid transitions throw a deterministic contract exception and no implicit string states are accepted. Receipts, results, and artifacts are descriptive records for future adapters; Phase 35A does not create them from provider activity.

## Failure, Retry, Redaction, and Quarantine

Failures use a closed `GenerationFailureClass` enum and stable code/message fields. Retry classification is explicit in the failure and bounded by a policy; no retry loop exists. Redaction is represented as metadata and hash-preserving references, never raw secret content. Quarantine records identify the artifact, reason, and release eligibility; an artifact is not accepted while quarantined or unvalidated.

## Compatibility and Validation

All Phase 35A contracts use `phase35a-generation-provider/*/v1` schema identifiers. Validators reject unsupported schema versions, unknown enum values, empty identities, duplicate capabilities, invalid hashes, broken references, invalid lifecycle transitions, and unsupported provider/capability/artifact combinations. Canonical JSON uses camelCase, relaxed escaping, string enums with integer values disabled, and SHA-256 lowercase hex, matching the repository's existing deterministic hashing conventions.

## Phase 35B Handoff

Phase 35B may add an adapter that:

1. consumes a validated projected request;
2. proves provider identity and capability compatibility;
3. obtains an externally supplied authorization decision;
4. reports lifecycle, receipt, result, and artifact records through the Phase 35A contracts;
5. passes all output through validation, redaction, quarantine, and lineage checks before acceptance.

Before that work, the repository needs an explicit executable-provider threat model, authentication/credential boundary, provider sandbox, output intake and scanning strategy, cancellation and timeout semantics, artifact validation corpus, audit storage, and end-to-end tests proving no unauthorized mutation or publication.

