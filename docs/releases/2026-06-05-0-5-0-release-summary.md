# PBIR Design Analyzer 0.5.0 Release Summary

PBIR Design Analyzer 0.5.0 is the first cross-platform Analytics Experience Review Platform release.

It expands the product from a PBIR-focused review utility into a broader review workspace for PBIR reports and analytical Fabric Apps, while preserving deterministic execution boundaries for supported fixes.

## New

### Fabric App Readiness Assessment

Determine whether a PBIR report is a strong candidate for migration into a Fabric App, including migration blockers, redesign effort, and next-step guidance.

### Fabric App Review Mode Foundations

Review analytical Fabric Apps through the same workspace used for PBIR analysis, with advisory findings and evidence-driven review paths.

### Screenshot Evidence

Use screenshot-backed review signals as part of the shared Evidence workflow.

### Semantic Model Evidence

Trace findings to semantic-model usage signals alongside other analytical evidence.

### Analyzable Surface Architecture

Use a shared architecture for multiple review surfaces rather than treating PBIR as the only reviewable asset.

### Surface Discovery

Detect supported review targets and route them into the correct review flow.

### Analyzer Registry

Support multiple analyzers through one shared workspace contract.

### Analyzer Profiles

Support analyzer-specific emphasis lenses without splitting the product into separate tools.

## Improved

### Cross-Platform Support

- Windows x64
- Linux x64
- macOS x64
- macOS arm64

Each release now includes platform-targeted VSIX packaging with the correct backend binary for the target operating system and architecture.

### Backend Startup Reliability

Backend readiness now uses a real handshake instead of a fake ready delay.

### Runtime Detection

Backend startup now detects missing or mismatched runtime and backend conditions more clearly.

### Degraded-Mode Messaging

When the backend cannot start, the extension stays usable in degraded mode for local tree browsing and explains what is unavailable.

### Packaging Isolation

Target packaging now uses isolated backend staging and a packaging lock so one target build cannot silently contaminate another VSIX artifact.

## Safety

### Deterministic Fix-Engine Hardening

The deterministic execution path remains the only mutation authority for report edits.

### Safer Mutation Planning

Unsafe title and semantic-color PBIR mutation paths are disabled until schema-correct support is ready.

### Severity Outcome Correction

Fix outcome evaluation now distinguishes improved, unchanged, worsened, resolved, and unexpected results correctly.

## Validation Summary

- `cd vscode-extension && npm test`
- `cd vscode-extension && npm run compile`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- `cd vscode-extension && npm run package:all`

## Packaging Summary

Built release artifacts:

- Windows x64 VSIX
- Linux x64 VSIX
- macOS x64 VSIX
- macOS arm64 VSIX

Confirmed package integrity:

- each VSIX contains the correct backend binary for its platform
- no Windows or Linux package ships a macOS backend executable
- package names reflect the `0.5.0` release

Windows ARM64 status:

- deferred from `0.5.0` publication pending manual backend startup validation on Windows 11 ARM
- backend diagnostics and a self-contained Windows ARM64 test packaging path were added to support follow-up validation
- public `0.5.0` packages remain framework-dependent; the private Windows ARM64 investigation package is intentionally self-contained only for follow-up smoke testing
