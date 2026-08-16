# Phase 35C Provider Trust, Sandbox, Audit, and Artifact Safety — Current State

Phase 35C adds the assurance and containment boundary required before any executable provider can be introduced. It is additive to Phase 35A contracts and Phase 35B composition-only runtime services. No real provider is activated.

## Actual implementation

The package lives in `service-dotnet/Services/Discovery/Phase35C/`:

- `Phase35CContracts` defines versioned immutable records, closed reason enums, opaque credential grants, policy versions, artifact dispositions, corpus results, conformance evidence, and activation decisions.
- `Phase35CProviderTrustEvaluator` validates provider/version/implementation identity, capabilities, execution mode, attestation schema, expiration, sandbox binding, and policy-version binding using an injected clock.
- `Phase35CSandboxPolicyEvaluator` evaluates isolated process, denied/allowlisted network, bounded filesystem/environment/dependency, grant-only credential, child-process, and finite resource requirements.
- `Phase35CCredentialBoundaryPolicy` accepts opaque grants only; provider, capability, scope, and expiration are checked without retrieving secret material.
- `Phase35CReplayProtectionService` distinguishes first execution, duplicate execution, modified-request reuse, and explicitly authorized retry.
- `Phase35CResourcePolicyEvaluator` rejects missing/unbounded duration, attempts, artifact, result, and concurrency limits.
- `Phase35CDurableAuditStore` provides deterministic local append-only records with sequence numbers and a SHA-256 previous/current hash chain. It detects mutation and sequence gaps in the supplied chain.
- `Phase35CArtifactSafetyPipeline` performs identity, type, size, scanner classification, and redaction checks and returns accepted, rejected, or quarantined disposition. `Phase35CFakeArtifactScanner` is offline-only.
- `Phase35COutputValidationEvaluator` evaluates versioned synthetic fixtures with required and forbidden properties.
- `Phase35CConformanceEvaluator` reports closed violations for identity, capabilities, readiness, request validation, cancellation, failure mapping, lineage, artifact classification, audit emission, and secret-free diagnostics.
- `Phase35CActivationGate` is the single typed admission decision. `Evaluate` composes every required assurance decision; `EvaluateProduction` proves the current catalog is unavailable.

## Activation boundary

Future activation requires explicit executable profile, authorization, ready state, fresh valid trust/attestation, approved sandbox policy, matching opaque credential grant, valid finite resource policy, passing conformance, approved output corpus, available durable audit, available artifact scanner, and replay protection. Every policy version is returned in the decision and must be recorded by a future runtime.

The gate has no adapter invocation, process, shell, HTTP, filesystem-provider, MCP, Skills, Desktop, credential-store, dynamic loading, publication, or mutation authority.

## Validation

`Phase35CRuntimeTests` covers fresh/stale/missing trust, policy invalidation, sandbox and credential failure, replay, audit tamper detection, scanner outcomes, corpus pass/fail, conformance pass/fail, activation eligibility, production denial, and audit secret redaction. `Phase35CBoundaryTests` checks the Phase 35C assembly and contract surface for provider-execution escape hatches.

## Remaining gaps

The local audit store is a durable abstraction with deterministic fixture behavior, not protected external persistence. Sandbox policy is not enforced by an OS runtime. Attestation does not inspect signed packages or binaries. Credential grants are not issued by a secret provider. Scanner behavior is synthetic. A provider-specific certification adapter, production conformance execution, and real artifact intake remain deferred.
