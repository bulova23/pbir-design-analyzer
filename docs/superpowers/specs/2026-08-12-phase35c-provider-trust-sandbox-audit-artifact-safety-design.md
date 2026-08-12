# Phase 35C Provider Trust, Sandbox, Audit, and Artifact Safety Foundation

## Goal

Establish the smallest deterministic, offline-only assurance layer that can decide whether a future provider is eligible to execute, while keeping every current production provider unavailable.

## Design

Phase 35C adds focused services beside Phase 35A and Phase 35B. Phase 35A remains authoritative for provider profiles, requests, authorization, execution policies, readiness, results, artifacts, hashes, lineage, retry, redaction, and quarantine. Phase 35B remains authoritative for provider resolution, immutable sessions, lifecycle, timeout/cancellation, staged result validation, artifact intake, and in-memory audit projection.

The new package contains:

- `Phase35CProviderTrustEvaluator`: validates provider identity/version, implementation identity, attestation metadata, approved capabilities/mode/sandbox binding, expiration, and policy/configuration binding.
- `Phase35CSandboxPolicyEvaluator`: evaluates explicit process, network, filesystem, environment, credential, resource, output, and dependency requirements without creating a sandbox runtime.
- `Phase35CCredentialBoundaryPolicy`: accepts opaque credential grants only and rejects raw secret material or scope mismatches.
- `Phase35CReplayProtectionService` and `Phase35CResourcePolicyEvaluator`: provide deterministic replay/idempotency and finite quota decisions.
- `Phase35CDurableAuditStore`: an append-only in-memory durable abstraction with deterministic serialization, sequence checks, and a SHA-256 previous/current hash chain.
- `Phase35CArtifactSafetyPipeline`: validates artifact identity, type, size/count, scanner classification, and redaction metadata before accepting, rejecting, or quarantining an artifact.
- `Phase35COutputValidationEvaluator`: evaluates synthetic versioned corpus fixtures against required and forbidden properties.
- `Phase35CConformanceEvaluator`: runs a reusable contract harness against a constrained Phase 35B adapter and reports closed violations.
- `Phase35CActivationGate`: composes decisions into one structured `Eligible`/`NotEligible` result. It is an admission decision only; it has no provider invocation authority.

All decisions bind to explicit policy versions. A clock is injected into trust, replay, audit, and activation evaluation. Unknown, missing, stale, malformed, or unavailable inputs fail closed. Secret material is not represented in Phase 35C records and diagnostics use reason codes rather than values.

## Data flow

`Phase35A request/profile/authorization/policy/readiness` → `Phase35C activation input` → independent deterministic evaluators → `Phase35C activation decision` → future provider admission only.

Provider results, when used by tests, flow through `identity/hash/type/size` validation, offline scanner classification, redaction validation, quarantine decision, and acceptance. The safety pipeline does not read files, invoke scanners, or create output bytes.

## Fail-closed rules

The gate denies when the profile is not explicitly executable, the provider is absent from the exact registry, trust is unknown/expired/invalid, the sandbox is not approved, credential grants are missing or mismatched, audit is unavailable, conformance or corpus approval is absent, scanner/replay/resource policy is unavailable, or any policy version is missing. Registration, readiness, credentials, Desktop installation, or a provider self-report never imply trust.

## Deferred enforcement

Actual process isolation, network/filesystem enforcement, package/binary signature inspection, secret-provider retrieval, antivirus/cloud scanning, external provider execution, real generated PBIR, publication, mutation, and durable external storage remain deferred. The local audit store and fake scanners are deterministic assurance fixtures, not production security controls.

## Testing

Focused xUnit tests cover each evaluator, every required activation denial, hash-chain tamper/gap detection, duplicate versus retry replay behavior, scanner outcomes, corpus pass/fail, passing and deliberately broken adapters, secret-leakage boundaries, policy staleness, production catalog unavailability, and the Phase 35A/35B boundary API scan.

