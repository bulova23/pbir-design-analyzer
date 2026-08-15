# Optional PBI Lens Capability-Safe Provider Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a provider-independent rendered-design evidence seam, PBI Lens capability detection, and accurate fallback UX without invoking an unsupported rendering surface or changing deterministic scoring.

**Architecture:** The extension host owns a typed `RenderedDesignEvidenceProvider` contract and a capability detector. The first provider is a capability-safe PBI Lens descriptor that recognizes the installed extension through `vscode.extensions`, records the version, and reports that its documented VS Code surface has no public programmatic API. Provider status is attached to the score-panel state for transparent UX, while scoring remains entirely PBIR/backend-owned.

**Tech Stack:** VS Code extension API, TypeScript, Jest, React webview, Markdown documentation.

---

### Task 1: Add provider-independent evidence and capability contracts

**Files:**
- Create: `vscode-extension/src/analyzer/renderedEvidence/types.ts`
- Test: `vscode-extension/src/test/renderedEvidenceTypes.test.ts`

- [ ] **Step 1: Write the failing tests** for provider status, independent capability flags, bounded diagnostics, and session-scoped evidence metadata.
- [ ] **Step 2: Run the focused Jest test and verify it fails** because the contracts do not exist.
- [ ] **Step 3: Implement the minimal discriminated unions and interfaces** for provider identity, report/page identity, evidence kind, capture timestamp/hash, capability report, provider status, diagnostics, and `IRenderedDesignEvidenceProvider` with no acquisition implementation.
- [ ] **Step 4: Run the focused Jest test and verify it passes.**

### Task 2: Detect PBI Lens without private internals

**Files:**
- Create: `vscode-extension/src/analyzer/renderedEvidence/pbiLensCapabilityDetector.ts`
- Create: `vscode-extension/src/analyzer/renderedEvidence/renderedEvidenceProvider.ts`
- Create: `vscode-extension/src/test/pbiLensCapabilityDetector.test.ts`
- Test: `vscode-extension/src/test/utils/vscode-mock.ts`

- [ ] **Step 1: Write failing tests** covering absent extension, installed disabled extension, installed activated extension, installed extension with only `activate`/`deactivate`, version `0.4.0`, and independently unavailable CLI/MCP capabilities.
- [ ] **Step 2: Run the focused tests and verify the expected failures.**
- [ ] **Step 3: Implement discovery through an injected `getExtension` function** equivalent to `vscode.extensions.getExtension`, inspect only `isActive`, `packageJSON.version`, and `exports`, and never import or invoke PBI Lens internals or commands.
- [ ] **Step 4: Implement the initial capability-safe provider** returning `NotInstalled`, `InstalledNoProgrammaticSurface`, or `Misconfigured`/`Error` as appropriate, with `PageScreenshot`, `ReportContext`, and `VisualContext` false unless independently proven by a future adapter.
- [ ] **Step 5: Run the focused tests and verify they pass.**

### Task 3: Add settings and recommendation policy

**Files:**
- Modify: `vscode-extension/package.json`
- Modify: `vscode-extension/src/platform/extensionIds.ts`
- Modify: `vscode-extension/src/platform/settings.ts`
- Create: `vscode-extension/src/analyzer/renderedEvidence/recommendation.ts`
- Create: `vscode-extension/src/test/renderedEvidenceRecommendation.test.ts`

- [ ] **Step 1: Write failing tests** for safe defaults, absence-only installation recommendation, dismissal persistence, and no install recommendation when PBI Lens is installed but unusable.
- [ ] **Step 2: Run the focused tests and verify they fail.**
- [ ] **Step 3: Add minimal settings** `pbirAnalyzer.enhancedScoring.enabled`, `pbirAnalyzer.enhancedScoring.provider`, and `pbirAnalyzer.enhancedScoring.suggestPbiLens`; default enhanced scoring to false, provider to `auto`, and suggestions to true.
- [ ] **Step 4: Implement a one-time recommendation policy** backed by `ExtensionContext.globalState`, with “Learn More”, “Install PBI Lens”, and “Not Now” actions only for the absent-extension state. The install action uses VS Code’s supported Marketplace URI navigation and does not install automatically.
- [ ] **Step 5: Run the focused tests and verify they pass.**

### Task 4: Surface provider status in the score panel while preserving the score

**Files:**
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Test: `vscode-extension/src/test/pbirScorePanel.navigation.test.ts`
- Test: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] **Step 1: Write failing tests** asserting that a score payload contains provider status and that the webview renders non-error copy for absent and installed-but-unusable states without changing `compositeScore`.
- [ ] **Step 2: Run the focused tests and verify they fail.**
- [ ] **Step 3: Add a versioned provider-status field** to the score-panel state and build it from the capability-safe provider during refresh for PBIR and Fabric App review paths.
- [ ] **Step 4: Render a compact status card** in the overview: deterministic scoring active; PBI Lens detected but unsupported, or optional install recommendation when absent. Do not label rendered scoring active and do not add rendered findings.
- [ ] **Step 5: Run focused extension and webview tests and verify they pass.**

### Task 5: Wire activation diagnostics and preserve deterministic fallback

**Files:**
- Modify: `vscode-extension/src/extension.ts`
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
- Create: `vscode-extension/src/test/renderedEvidenceProvider.test.ts`

- [ ] **Step 1: Write failing tests** for bounded diagnostics, provider failure isolation, and deterministic score passthrough.
- [ ] **Step 2: Run the focused tests and verify they fail.**
- [ ] **Step 3: Log a concise capability report to the extension output channel** at activation and score refresh; catch provider detection errors, record a bounded diagnostic, and continue scoring.
- [ ] **Step 4: Keep the provider invocation observational only**: no screenshot acquisition, no score adjustment, no AI request, no CLI/MCP process execution, and no mutation authority.
- [ ] **Step 5: Run focused tests and verify they pass.**

### Task 6: Document the current integration decision and activation criteria

**Files:**
- Create: `docs/integrations/pbi-lens-rendered-evidence.md`
- Create: `docs/current-state/pbi-lens-rendered-evidence-provider-state.md`
- Modify: `README.md`
- Modify: `docs/ROADMAP.md`
- Modify: `.agent-memory/current-focus.md`
- Create: `.agent-memory/sessions/2026-08-15-optional-pbi-lens-provider.md`
- Modify: `.agent-memory/session-summaries.md`

- [ ] **Step 1: Write the integration decision** including the architecture diagram, capability matrix for installed PBI Lens 0.4.0, evidence contract, privacy/security findings, fallback proof, and explicit activation criteria.
- [ ] **Step 2: Document the manual test** and state that automatic rendered scoring is deferred because no supported/testable programmatic surface is available.
- [ ] **Step 3: Add the optional-companion messaging** without implying that installing PBI Lens alone enables enhanced scoring.
- [ ] **Step 4: Perform a documentation self-review** for unsupported claims, inline-code violations in user-facing docs, and missing deferred roadmap language.

### Task 7: Verify the complete change without committing

**Files:**
- Test: changed files and repository validation commands

- [ ] **Step 1: Run focused provider, recommendation, score-panel, and webview tests.**
- [ ] **Step 2: Run extension Jest, webview Jest, TypeScript compilation, production build, backend regression, changed-file lint, package validation, and `git diff --check`.**
- [ ] **Step 3: Verify score snapshots and payloads are unchanged except for additive provider-status metadata.**
- [ ] **Step 4: Inspect `git diff`, `git status`, staged-file output, HEAD, and generated artifacts; leave all integration work unstaged and uncommitted.**
