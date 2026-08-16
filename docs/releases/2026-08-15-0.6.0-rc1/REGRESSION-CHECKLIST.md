# RC1 Regression Checklist

Use this condensed smoke test for every future release. Run it from a clean
VS Code profile with a disposable PBIR fixture.

- [ ] Install the target-specific VSIX and verify version 0.6.0.
- [ ] Activate the extension; backend reaches Ready or presents documented
  degraded-mode diagnostics.
- [ ] Open a PBIP project, refresh the explorer, and score one report.
- [ ] Verify score panel Overview, Issues, Fix Plan, Evidence, and Export.
- [ ] Generate one v1 report and one v7 composed report.
- [ ] Analyze both generated reports and record score/page/visual counts.
- [ ] Import a supported PBIR report and verify page/visual metadata.
- [ ] Analyze the imported report from its opaque snapshot workflow.
- [ ] Exercise Rename Page preview, cancel, confirm, execute, and before/after.
- [ ] Exercise Add Page, Remove Page, Move Page, Move Visual, and Resize
  Visual with disposable copies.
- [ ] Verify source snapshot immutability and new artifact handle identity.
- [ ] Verify Card, Table, clustered column, line, bar, pie, and slicer output.
- [ ] Verify theme, formatting, filters, interactions, navigation, and layout.
- [ ] Repeat one generation and compare hashes/normalized output.
- [ ] Submit malformed input and confirm structured rejection.
- [ ] Submit stale handle and invalid planner target and confirm fail-closed
  behavior.
- [ ] Copy score diagnostics and export one review/governance report.
- [ ] Inspect VSIX contents for backend, extension bundle, webviews, config,
  resources, and absence of debug/placeholder artifacts.
- [ ] Record OS, VS Code version, VSIX target, fixture, and result.
