# Phase 35D Provider Certification Threat Model

## Mitigation

- Package substitution is blocked by deterministic metadata/hash identity and exact activation comparison.
- Signature stripping/spoofing is blocked by fail-closed missing, malformed, unsupported, invalid, unapproved, expired, and revoked signer results.
- Downgrade/stale reuse is blocked by provider/version/profile/policy/evidence bindings and injected-clock expiration.
- Evidence, record, audit, and replay tampering is detected by canonical hashes, atomic persistence, sequence continuity, previous-hash continuity, and duplicate execution checks.
- Malicious correctly signed code remains a residual risk: signing proves signer control and package identity, not behavioral safety. Conformance, corpus, policy, and artifact readiness remain required.

## Residual risk and boundary

Phase 35D does not enforce an OS sandbox, issue credentials, scan real provider artifacts, validate a live certificate chain/revocation service, inspect a build pipeline, or execute provider code. A compromised approved signer or build pipeline can sign unsafe code. Local persistence is tamper-detecting, not externally replicated.

The Phase 35D assembly adds no process, shell, HTTP, dynamic assembly loading, Desktop, MCP, Skills, provider-controlled filesystem execution, publication, or unrestricted credential path. The adapter execution member is never called.
