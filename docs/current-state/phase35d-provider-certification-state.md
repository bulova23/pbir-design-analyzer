# Phase 35D Provider Certification — Current State

Phase 35D adds an offline pre-production certification layer over Phase 35C. It identifies an exact package, verifies a signed RSA/SHA-256 attestation, composes Phase 35C assurance decisions, evaluates a provider-specific adapter without calling execution, and issues an immutable certification record. No production provider is registered or activated.

## Architecture

```text
PackageIdentityResolver → SignedAttestationVerifier → ConformanceRunner
  → CertificationEvaluator → CertificationLifecycle → ActivationBinding
                                      ↘ Phase35C assurance evaluators
```

The bounded `Phase35DProtectedAuditReplayStore` persists an explicit audit projection containing outcome hashes (never redacted outcome values) and replay identities with atomic replacement and state-integrity hashing.

## Identity, evidence, and lifecycle

A candidate binds provider ID/version, implementation ID, package ID, package/manifest/implementation SHA-256 values, signer identity, algorithm, key identifier, build provenance, capabilities, execution class, sandbox profile, policy versions, corpus version, and conformance profile version. Package ID and identity hash use Phase 35A canonical JSON/SHA-256 conventions.

Candidate → EvidenceCollected → Verified → Certified is the only certification path. Rejected, Expired, Revoked, Superseded, and Invalidated are fail-closed alternate states. Activation compares exact candidate identity, provider/version/implementation, profile, evidence hash, policy versions, live Certified status, and the Phase 35C activation decision. Success is `PreProductionEligible`; `ProductionEligible` is not issued.

The focused fixture proves valid RSA/SHA-256, invalid signature, wrong signer, expired signer, revoked signer, unsupported algorithm, missing signature, package-hash mismatch, conformance failure, lifecycle invalid transition, persistence restart/tamper/duplicate replay, exact binding, version mismatch, and expiration. Its sanitized passing evidence hash is `82d8e8431e6180ab010a9c194ae802f7d3ac9265a712058c0ac03eec68249298`.

## Provider/runtime state and next prerequisite

`powerbi-report-author@0.1.4` remains local PBIR validation/metadata inspection only. No runtime generation provider is available. The narrowest Phase 35E prerequisite is OS sandbox enforcement: Phase 35C evaluates containment but does not enforce it. Credential issuance, production artifact scanning, executable adapter work, and pre-production execution remain later gates.
