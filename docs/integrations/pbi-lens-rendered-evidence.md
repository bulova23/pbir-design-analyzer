# PBI Lens Rendered Design Evidence Integration

## Integration decision

PBIR Design Analyzer uses a provider boundary for optional rendered design
evidence. The first recognized companion is PBI Lens.

The current integration intentionally uses only the supported VS Code extension
discovery surface:

```text
vscode.extensions.getExtension('duckduck-beps.pbi-lens-vscode')
  → package version, activation state, and public export shape
```

The installed PBI Lens 0.4.0 package exports only its lifecycle functions
activate and deactivate. It does not expose a supported public programmatic
rendering API. PBIR Design Analyzer therefore does not invoke bundled modules,
private exports, or interactive PBI Lens commands as if they were APIs.

The PBI Lens documentation describes a CLI and an MCP server, but neither is
installed or connected in the current environment. This release does not add a
CLI or MCP adapter, executable-path setting, generic process runner, or MCP
proxy.

## Architecture

```text
PBIR Design Analyzer
        │
        ├── Deterministic PBIR Scoring
        │       └── authoritative score, findings, policy evaluation
        │
        └── RenderedDesignEvidenceProvider
                    │
                    └── PBI Lens capability-safe descriptor
                            └── no acquisition until a supported API exists
```

The provider contract is independent of PBI Lens and carries provider identity,
report/page identity, evidence kind, capture time, optional hash, capability
state, and bounded diagnostics. Future CLI, MCP, or exported-extension-API
providers can implement the same contract without changing scoring contracts.

## Current capability matrix

| Capability | PBI Lens 0.4.0 | Used now |
| --- | --- | --- |
| Extension detected | Yes when installed | Yes |
| Installed version | 0.4.0 in the development environment | Yes |
| Activated state | Detected independently | Yes |
| Public VS Code API | No supported API exported | No |
| CLI | Not installed | No |
| MCP | Not installed or connected | No |
| Page screenshot | Unavailable through the current provider | No |
| Report context | Unavailable through the current provider | No |
| Visual context | Unavailable through the current provider | No |
| Rendered scoring | Unavailable | No |

Capabilities are independent. The extension being installed does not imply
that screenshots, report context, visual context, CLI, or MCP are available.

## Provider states and fallback

The detector reports NotInstalled, InstalledNoProgrammaticSurface,
CliAvailable, McpAvailable, Available, Misconfigured, or Error. The current
installed PBI Lens is recognized as InstalledNoProgrammaticSurface when active
and Misconfigured when installed but inactive.

When rendered evidence is unavailable:

```text
Rendered evidence unavailable
        → bounded diagnostic
        → deterministic PBIR analysis continues
        → normal authoritative score is returned
```

No score weight, finding severity, confidence, or policy result changes in this
phase. No screenshot evidence is fabricated.

## User experience

When PBI Lens is absent, the first relevant scoring session may show:

> Install PBI Lens for future enhanced rendered-design scoring support.

The recommendation is optional, one-time, dismissible, and controlled by the
enhanced rendered scoring suggestion setting. Installing PBI Lens alone does
not enable enhanced scoring.

When the extension is installed but no supported programmatic surface exists,
the score panel shows:

> PBI Lens detected, but this installed configuration does not expose a supported programmatic rendering interface. Deterministic scoring remains active.

This is informational status, not a scoring error.

## Settings

- Enhanced rendered scoring enabled is false by default and reserves the future
  activation seam without changing current scoring.
- Enhanced rendered scoring provider defaults to auto.
- Suggest PBI Lens installation is true by default and only applies when the
  extension is absent.

No CLI executable path or MCP connection setting is present.

## Evidence contract

Rendered evidence artifacts are session-oriented and may identify a report,
page, evidence kind, capture timestamp, optional SHA-256 hash, and provider.
The current provider returns an empty evidence collection because no supported
acquisition surface is available.

Deterministic evidence and rendered evidence remain separate domains. Existing
design principles and governance policies remain authoritative; a future
provider would supply observation, not judgment or mutation authority.

## Privacy and security

This phase performs no Power BI Service call, report publication, browser
automation, screenshot capture, token access, tenant access, or cloud operation.
PBI Lens remains independently maintained and optional. Any future adapter
that requires authentication, workspace access, publication, or screenshots
must describe and require that behavior before activation.

## Manual integration test for a future provider

Automatic rendered scoring is not testable in the current environment. Once a
supported programmatic surface is available, validate the first adapter with a
real report:

1. Open a supported PBIR or PBIP report.
2. Run PBIR Design Analyzer and confirm the provider capability report.
3. Confirm the provider's screenshot or context capability is independently
   detected.
4. Run the provider-specific rendered evidence request.
5. Confirm evidence identity, timestamp, provider, and bounded diagnostics.
6. Confirm any enhanced finding cites rendered evidence separately from PBIR
   evidence.
7. Disable or disconnect the provider.
8. Re-run scoring and confirm the deterministic score and findings still
   succeed without rendered evidence.

## Activation criteria

Automatic rendered scoring may be implemented only after one of these is proven
with a supported, testable surface:

1. PBI Lens exports a documented public VS Code API.
2. The PBI Lens CLI is installed and its required fixed commands are exercised
   successfully in a manual integration test.
3. The PBI Lens MCP server is installed, connected, and its required screenshot
   or context tools are exercised successfully in a manual integration test.

At that point, implement one concrete provider adapter, add live/manual
validation, and review evidence and privacy behavior before altering scoring.

## Validation for this capability-safe phase

- Extension suite: 523 tests passed across 102 suites; webview suite: 68 tests
  passed across 11 suites.
- Focused rendered-review/provider/recommendation/presentation tests: 21 passed.
- TypeScript compilation, production build, VSIX packaging, changed-file ESLint,
  and `git diff --check` passed.
- Backend regression: 995 passed, 11 expected Windows skips, and one known
  unrelated Phase 35E timeout-test failure where the runner completed instead
  of timing out.
