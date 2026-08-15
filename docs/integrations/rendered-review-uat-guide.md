# Rendered Review UAT Guide

1. Open a PBIR report and run Analyze.
2. Confirm the Optimization Report still shows the authoritative deterministic
   score and existing Issues/Fix Plan content.
3. Confirm Rendered Review Recommended appears when a finding matches a
   supported rendered category.
4. Confirm each checklist item shows guidance and the five review states.
5. Change a state and enter a Reviewer Note; confirm the state is retained in
   the panel.
6. Attach a user-supplied screenshot and confirm the evidence count increases.
7. Confirm the PBI Lens action is disabled when no supported report context is
   detected, with the checklist still available.
8. Apply a deterministic mutation, re-analyze, and confirm the rendered review
   checklist is available for before/after human confirmation.
9. Export the review summary and confirm the Rendered Review table includes
   category, page, status, notes, and screenshot counts.
10. Disable Rendered Review Enabled or Show Rendered Review Checklist and
    confirm deterministic scoring still works without the checklist.

Out of scope for this UAT: automatic screenshots, OCR, image analysis, visual
AI, CLI, MCP, Desktop automation, and report-viewer behavior.
