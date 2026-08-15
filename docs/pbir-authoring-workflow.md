# PBIR Authoring Workflow

The curated authoring workflow is available from the command **PBIR Design
Analyzer: Curated Mutation** after importing a supported PBIR report.

Choose one operation:

- Rename page
- Add page
- Remove page
- Move page
- Move visual
- Resize visual

The extension collects intent with Quick Pick and Input Box prompts. The
backend then produces the preview, validates navigation and layout, calculates
the semantic diff, and decides whether execution is admissible. Confirmation
is required before execution. A successful mutation returns a new artifact
handle and analyzer before/after evidence; the imported source snapshot is not
changed.

Add Visual and the other typed backend mutation capabilities are not public.
The workflow does not provide capability discovery, batching, undo/redo,
graphical editing, or raw JSON editing.
