# PBIR Design Analyzer 0.6.0

## Release Candidate 1

PBIR Design Analyzer 0.6.0 RC1 is a local VS Code authoring and review
platform for creating, importing, analyzing, and safely applying a focused set
of PBIR changes.

## Highlights

- Generate local PBIR reports from versioned requests v1 through v7.
- Author pages, layouts, bindings, Card, Table, chart, and slicer visuals.
- Apply themes, formatting, filters, interactions, navigation, and composed
  page structures.
- Import supported PBIR reports while preserving stable identities and bounded
  unsupported content.
- Preview and apply six curated single-operation changes: Rename Page, Add
  Page, Remove Page, Move Page, Move Visual, and Resize Visual.
- Review analyzer scores, fidelity evidence, diagnostics, and before/after
  mutation results.
- Continue using the score-panel review workspace, governance checks, evidence,
  and export workflows.

## Supported workflow

Install the VSIX matching the operating system, open a supported local PBIP
workspace, and use the PBIR Design Analyzer explorer. Generation and import
workflows retain backend-owned handles; users do not edit handles or raw PBIR
JSON. Mutations show a preview and require confirmation before execution.

## Compatibility

- VS Code: 1.93 or later.
- Backend: .NET 8 packaged per target.
- Targets: Windows x64, Windows ARM64, Linux x64, macOS x64, and macOS ARM64.
- Supported workspace: local supported PBIP/PBIR workflows. Untrusted and
  virtual workspaces are not supported.

## Limitations

PBIR import and semantic projection are bounded by the pinned schema and
descriptor catalog. Handles expire with the backend process. Public mutations
are single-operation and curated. Bookmarks, drillthrough, shared slicers,
semantic-model/DAX generation, hosted execution, public batching, capability
discovery, and Windows execution activation are not included.

## UAT status

This is an RC for controlled manual evaluation. Automated validation is
recorded in [VALIDATION-RESULTS.md](VALIDATION-RESULTS.md). Do not treat the
RC as a general release until UAT exit criteria are signed off.
