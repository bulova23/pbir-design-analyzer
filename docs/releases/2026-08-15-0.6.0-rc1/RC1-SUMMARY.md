# PBIR Design Analyzer 0.6.0 Release Candidate 1

Date: 2026-08-15
Status: Prepared for controlled UAT; not committed
Feature boundary: Phase 48 complete; no Phase 49 work included

## Release candidate

RC1 is the frozen Phase 36–48 PBIR authoring platform. It packages the .NET 8
backend, VS Code extension, three webviews, configuration assets, and target-
specific backend payloads into five VSIX packages:

- Windows x64
- Windows ARM64
- Linux x64
- macOS x64
- macOS ARM64

The source version is 0.6.0. The RC package is intentionally uncommitted until
manual UAT completes and any release-blocking defects are corrected.

## Recommendation

**Ready for UAT. Not yet recommended for limited release.**

The automated gates are strong: 996 backend tests passed with 11 expected
Windows integration skips, 523 extension tests passed, 68 webview tests
passed, the production build passed, .NET RpcHost build passed, and all five
VSIX packages were produced. Limited release should wait for manual UAT and a
decision on the existing 43-error ESLint baseline.

## Included capability

- Versioned local generation requests v1 through v7.
- Typed PBIR generation for pages, layouts, bindings, Card, Table, and six
  chart/slicer visual families.
- Typed themes, filters, interactions, formatting, metadata, templates,
  sections, slots, navigation, slicers, and slicer interactions.
- Supported PBIR import with opaque snapshot handles, metadata projection,
  identity preservation, and bounded lossless authoring.
- Analyzer verification, fidelity evidence, diagnostics, and deterministic
  hashes.
- Public Generate, Import, Analyze, Rename Page, Add Page, Remove Page, Move
  Page, Move Visual, and Resize Visual workflows through the existing authoring
  boundary. The six mutation names are single-operation curated workflows;
  public batching is not included.
- Existing report scoring, governance, review workspace, evidence, export,
  Design Studio preview/materialization, and recovery workflows.

## Explicitly excluded

Bookmarks, drillthrough, shared slicers, semantic-model generation, DAX
generation, Windows execution activation, hosted execution, provider-security
enhancements, public capability discovery, public mutation batching, and new
RPC operations are not part of RC1.

## Evidence

- Implementation notes: `docs/superpowers/implementation-notes/2026-08-15-phase48-curated-mutation-expansion.md`
- Current-state records: `docs/current-state/phase46-vscode-authoring-integration-state.md`, `phase47-interactive-mutation-workflow-state.md`
- Functional inventory: [FUNCTIONAL-INVENTORY.md](FUNCTIONAL-INVENTORY.md)
- Manual test plan: [UAT-GUIDE.md](UAT-GUIDE.md)
- Validation record: [VALIDATION-RESULTS.md](VALIDATION-RESULTS.md)
- Architecture review: [ARCHITECTURE-ASSESSMENT.md](ARCHITECTURE-ASSESSMENT.md)
- Competitive analysis: [COMPETITIVE-ANALYSIS.md](COMPETITIVE-ANALYSIS.md)
