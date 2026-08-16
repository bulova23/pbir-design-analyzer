# Phase 35E OS Sandbox Enforcement — Session Record

## Scope

Implemented the narrow Phase35E sandbox admission/evidence boundary without activating a production provider, changing Phase35B orchestration, staging, committing, resetting, cleaning, or publishing.

## Design decision

The repository targets macOS `osx-arm64` for its packaged extension. A focused Phase35E contract/policy/evidence layer and a separate `Phase35E.Runtime` assembly isolate OS process references from the Phase35A–D Core boundary. The intended adapter uses `/usr/bin/sandbox-exec`, but direct Darwin 27 probes showed deny-default custom profiles aborting (exit 134/137). The capability report therefore returns unsupported and admission fails closed.

## Delivered

- immutable Phase35E identity, policy, capability, admission, result, evidence, and failure contracts
- exact absolute-path/executable SHA-256 verification
- explicit allowlisted environment builder and scoped lifecycle directory
- macOS profile-generation seam and isolated runtime assembly containing the sole process boundary
- deterministic bounded runner/test seam, evidence hashing, Phase35C audit projection, and closed-mode fixture
- focused runtime and static boundary tests
- design, plan, current-state, threat-model, roadmap, architecture-gap, provider-framework, repo-map, and memory updates

## Validation

- focused Phase35E: 8/8 passed
- first full backend after initial implementation: 832 passed, 3 failed; failures were the expected architecture regression from Process leaking into Core
- after runtime assembly isolation: focused Phase35E 8/8 passed and phase boundary references were restored
- full backend final: 835/835 passed, 0 failed, 0 skipped after increasing the timeout test window to 100 ms
- fixture project builds successfully; real custom Seatbelt launch was not treated as success because direct probes abort on the observed OS
- extension Jest 494/494; webview Jest 68/68; TypeScript compile; backend build; extension build; VSIX package; `git diff --check` all pass
- repository lint reports the unchanged 43-error baseline; no Phase35E TypeScript surface was added

## Remaining highest-risk blocker

The current macOS custom Seatbelt mechanism cannot be safely proven on Darwin 27. Phase35E remains fail-closed until a supported OS boundary (or a separately approved platform mechanism) can enforce process/filesystem/network/child-process controls without abort behavior. This is the evidence-based Phase35F recommendation.
