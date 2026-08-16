# RC1 Functional Inventory

This is the baseline for UAT and regression testing. “Public” means reachable
from the shipped VS Code workflow. “Backend-only” means implemented or typed
internally but intentionally not exposed by the extension.

## Generation

- Request schemas: `local-pbir-generation-request/v1` through `v7`.
- v1: one-page Card generation with deterministic output and round-trip score.
- v2: multiple pages, Card/Table visuals, direct measure/dimension bindings,
  and bounded layout.
- v3: themes, report/page/visual equality filters, metadata, interactions,
  layout margins/spacing, and Card/Table formatting.
- v4: generalized visual bindings and role-aware bindings.
- v5: advanced chart authoring and chart-specific axis/legend/tooltip and
  conditional-formatting contracts.
- v6: page templates, sections, slots, navigation, slicers, and composition.
- v7: explicit same-page slicer interactions.
- Result evidence includes artifact and manifest identity, validation,
  materialization, round-trip score, diagnostics, and timing observations.
- Generation is local and deterministic; it is not semantic-model or DAX
  generation.

## Visual catalog

Supported generated visual types are:

- Card
- Table
- Clustered column chart
- Line chart
- Bar chart
- Pie chart
- Slicer

Bindings support measure and dimension values with Value, Category, Series,
Axis, Legend, and Tooltip roles where the visual descriptor permits them.
Imported roles outside the descriptor catalog remain preserved and diagnostic,
but are not typed or mutable.

## Formatting and report properties

- Themes: name, font family, font size, background, accent, and palette.
- Card: title, subtitle, label style, number format, box, alignment.
- Table: title, subtitle, header/row style, alternate row color, number
  formats, per-column alignment/format, width behavior, and box.
- Charts: title, axis labels, legend visibility, colors, and background;
  advanced axis and legend configuration is typed where supported.
- Slicers: title and label style.
- Shared text/box styles: font, size, weight, color, alignment, border,
  padding, and background.
- Equality filters at report, page, and visual scope.
- Basic cross-filter, cross-highlight, and disabled interactions.
- Conditional formatting: threshold, positive/negative, and null-default.
- Deterministic layout margins, spacing, alignment, visual padding, and
  bounded x/y/width/height.
- Report metadata: author, description, and display name.

## Composition

- Typed page templates: Default, Executive, and Compact.
- Sections with ordered pages and bounded slot assignments.
- Navigation between known pages.
- Slicers with valid dimension bindings.
- Explicit same-page slicer interactions targeting known visual names.
- Composition validation rejects duplicate slots, unknown navigation targets,
  measure-bound slicers, and unknown interaction targets.

## Import and authoring

- Import of supported local PBIR report/project directories.
- Opaque snapshot handles owned by the backend process.
- Page and visual metadata projection for selection and planning.
- Pinned-schema validation and fail-closed unsupported-role diagnostics.
- Lossless envelope preserving recognized typed fields and bounded opaque
  fields owned by the pinned schema.
- Stable page/visual identity preservation for unchanged objects.
- New artifact handles for successful mutation results; source snapshots stay
  immutable.

## Mutation

Public single-operation curated workflows:

- Rename Page
- Add Page
- Remove Page
- Move Page
- Move Visual
- Resize Visual

Each uses backend planning, typed semantic preview/diff, confirmation, execute,
fresh planning, materialization, validation, and analyzer-before/after evidence
where the operation is admissible.

Backend-only typed operations include broader visual add/remove, formatting,
binding, filter, navigation, and slicer mutations. They are not a supported
RC1 user workflow. Public mutation batching and capability discovery are also
deferred.

## Analysis and validation

- PBIR score/analyzer remains authoritative for report and page score output.
- Generated reports receive round-trip analyzer verification.
- Imported reports can be analyzed through an opaque snapshot handle or report
  directory.
- Mutation results include score delta, fidelity classification, preserved
  identities, diagnostics, and timing observations.
- Structured error categories cover invalid request, import failure,
  unsupported authoring, mutation conflict, validation failure, analyzer
  failure, execution failure, and internal failure.
- Design Analyzer score panel provides Overview, Issues, Fix Plan, Evidence,
  and secondary Export presentation surfaces.
- Governance and readiness configuration preserve existing defaults with
  explicit provenance.

## VS Code commands and workflows

Canonical commands exposed by the extension include:

- Open Report Design Studio
- Open Local PBIR Materialization
- Open PBIP Project
- Refresh Reports
- Score Report
- Copy Score Diagnostics
- Configure Scoring
- Check Governance
- Export Governance Report
- Export Review Workflow Summary
- Upload Report Screenshots
- Configure Visual Audit Provider
- Generate Report
- Import Report
- Analyze Report
- Rename Page
- Curated Mutation

The extension also retains legacy command aliases for compatible older command
links. Unsupported untrusted and virtual workspace posture remains declared in
the package metadata.

## Public contract inventory

- Authoring RPC schema: `pbir-authoring-rpc/v1`.
- Public request flow stores opaque handles rather than exposing filesystem,
  IR, raw PBIR JSON, or authoring envelopes to the extension.
- Generate, Import, Analyze, and Curated Mutation are wired to VS Code.
- Standalone Validate is implemented in the backend contract but is not a
  shipped VS Code command.
- RPC operations and error values are validated before backend state is used.
