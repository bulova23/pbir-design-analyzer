# Phase 35D — Pre-Production Provider Certification

## Scope

Implemented the additive Phase 35D certification layer over the existing uncommitted Phase 35C foundation. No real provider was registered, probed, executed, or activated.

## Delivered

- deterministic package identity from approved metadata and SHA-256 hashes
- in-process RSA/SHA-256 signed-attestation verification with signer approval, expiration, revocation, algorithm, signature, hash, and identity checks
- versioned certification candidate/profile/evidence/record contracts
- non-executing provider-specific conformance runner using Phase 35C declarations/evidence and output corpus checks
- pure certification evaluator and immutable lifecycle with expiry, revocation, supersession, invalidation, and illegal-transition rejection
- exact Phase 35C activation binding returning only PreProductionEligible
- bounded atomic audit/replay persistence with outcome-hash-only audit projection, restart validation, sequence continuity, hash integrity, and duplicate replay rejection
- current-state, threat-model, roadmap, architecture-gap, provider-framework, plan/spec, and memory updates

## Validation

- `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter 'FullyQualifiedName~Phase35A|FullyQualifiedName~Phase35B|FullyQualifiedName~Phase35C|FullyQualifiedName~Phase35D'`: 54 passed, 0 failed
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`: 827 passed, 0 failed, 0 skipped
- `cd vscode-extension && npm test -- --runInBand`: 97 extension suites / 494 tests and 11 webview suites / 68 tests passed
- `cd vscode-extension && npm run compile`: passed
- `cd vscode-extension && npm run lint`: unchanged repository baseline, 43 pre-existing errors outside Phase35D
- extension bundle: passed

## Safety and Git state

No process, shell, HTTP, MCP, Skills, Desktop, dynamic loading, publication, PBIR generation, or unrestricted credentials were introduced. Phase 35A/B remain committed at HEAD; Phase 35C and Phase 35D remain uncommitted and unstaged. No staging, commit, reset, clean, or restore was performed. Unrelated dirty files were preserved.

## Next step

The narrowest evidence-based Phase 35E prerequisite is OS sandbox enforcement. Credential issuance, real artifact scanning, an executable provider adapter, and pre-production execution should remain subsequent phases.
