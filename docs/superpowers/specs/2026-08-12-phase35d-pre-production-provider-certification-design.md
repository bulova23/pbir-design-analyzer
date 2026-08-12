# Phase 35D — Pre-Production Provider Certification and Signed Attestation Foundation

## Goal

Identify one exact provider package, verify a signed offline attestation, compose Phase 35C assurance decisions and provider-specific conformance evidence, and issue an immutable record that can establish pre-production eligibility without granting production execution authority.

## Design

`Phase35DPackageIdentityResolver` derives a stable package identity hash from provider/version/implementation/package metadata, package SHA-256, manifest hash, implementation hash, signer identity, signature algorithm, certificate/key identifier, and build provenance. The resolver consumes approved metadata only; it does not inspect or execute a package.

`Phase35DSignedAttestationVerifier` is the cryptographic boundary. It verifies canonical Phase 35A JSON bytes using platform RSA/SHA-256 APIs, checks the package hash and exact candidate identity, expected signer, signer validity interval, algorithm, and explicit revocation set. Missing, malformed, unknown, unsupported, invalid, expired, revoked, or mismatched evidence fails closed.

`Phase35DCertificationEvaluator` is a pure composition point. It combines the verifier, Phase 35C trust/sandbox/credential/resource evaluators, Phase 35C conformance and corpus results, and explicit audit/artifact/replay readiness. It emits structured reasons and a deterministic evidence bundle hash. The bundle is bound into an immutable `Certified` record with an expiration and profile/policy versions.

`Phase35DCertificationLifecycle` permits only Candidate → EvidenceCollected → Verified → Certified, with terminal Rejected, Expired, Revoked, Superseded, and Invalidated records. Records are append-only; revocation and supersession create new records. An injected clock makes expiration deterministic.

`Phase35DCertificationActivationBinding` wraps the existing Phase 35C activation decision and requires exact provider/version/implementation/package/profile/policy/evidence matches plus a live Certified record. It can return `PreProductionEligible`; production remains denied by the existing Phase 35C production gate.

`Phase35DProtectedAuditReplayStore` is a bounded local JSON store with atomic replacement, an integrity hash, sequence continuity, and explicit load validation. It persists only Phase 35C audit records and replay identities, and refuses malformed, mutated, deleted, gapped, duplicated, or partially written state. It has no network or provider execution authority.

## Testing and boundary

Tests cover deterministic identity, RSA signature success/failure cases, policy and identity mismatches, full and failed certification, lifecycle transitions, expiry/revocation/supersession, exact activation binding, persistence restart/tamper cases, passing and deliberately broken adapter candidates, and static boundary checks for process, HTTP, shell, MCP, Skills, Desktop, dynamic loading, and provider-controlled execution surfaces. No real provider is registered or executed.

## Non-goals

OS sandbox enforcement, credential issuance, real artifact scanning, executable provider adapters, PBIR generation/materialization, publication, Desktop automation, network calls, and production eligibility remain deferred.
