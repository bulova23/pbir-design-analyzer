Visual Audit Mode Plan: Screenshot Upload First, Chromium Inspection Later

Summary

Add an optional Visual Audit layer to the existing PBIR scoring experience. V1 lets users upload report-page screenshots and receive AI-assisted visual findings aligned to PBIR pages/tabs. V2 adds guided live inspection for Chromium browsers, with first-class support for Chrome and Edge, using the same audit session model and page-mapping flow.

The current PBIR score remains the deterministic baseline. Visual audit findings are additive in V1: they appear in the score panel but do not change the composite score until the signal quality is proven.

Key Changes

1. Product shape

* Keep the existing Score Report flow and PbirScorePanel as the primary surface.
* Add a new report-level visual-audit workflow instead of a separate standalone product.
* Add command entry points:
    * Upload Report Screenshots
    * Attach Screenshot To Page
    * future: Start Browser Visual Audit
* Extend the existing score panel with:
    * Visual Audit Coverage on the overall tab
    * page-level screenshot/audit sections on each report page tab
    * a per-page capture selector when a page has multiple screenshots or states

2. V1 screenshot upload workflow

* Support multi-page reports by treating screenshots as a report-scoped session, not as single images.
* Introduce a VisualAuditSession stored per report.
    Chosen storage: extension globalStorageUri, not the repo.
* On import, copy selected screenshots into the session asset folder and persist a manifest.
* Support two import modes:
    * bulk folder/files import for an entire report
    * single-page attach for fixing gaps
* Auto-match screenshots to PBIR page names using normalized filename matching.
    Examples: 01 Overview.png, Sales Detail.jpg, Net Sales - Default.png
* Any unmatched screenshots go into a review queue for manual assignment.
* Any PBIR pages without screenshots remain visible as missing coverage.
* Allow multiple captures per page using an optional stateName.
    This covers bookmarks, drill states, or alternate filter states without exploding the top-level tab model.

3. V1 analysis model

* Do not send raw image bytes through the current LSP JSON bridge.
* Keep screenshot ingestion and AI-provider orchestration in the VS Code extension host.
    Reason: easier file access, easier secret handling, no backend request-logging risk for credentials.
* Build a provider abstraction in the extension:
    * VisualAuditProvider
    * provider input = screenshot path + page context + PBIR metadata + existing page score context
    * provider output = structured JSON findings
* Feed the provider:
    * page name
    * page-level PBIR visual metadata
    * existing framework findings for that page
    * screenshot image
* Require structured output with:
    * pageName
    * captureId
    * findingType
    * severity
    * confidence
    * text
    * recommendation
    * optional regionHint
* Default classification rules for visual-audit findings:
    * objective only for clearly visible issues such as clipped text, overlap, obvious blank/error states
    * strongHeuristic for hierarchy, scan path, spacing, density, and visual balance
    * stylePreference for polish and consistency observations
* In V1, visual-audit findings do not alter composite score.
    They render as a separate, clearly labeled evidence layer.

4. V1 score panel behavior

* Reuse the existing Overall + page tabs model.
* Add an overall Visual Audit Coverage card showing:
    * total PBIR pages
    * pages with screenshots
    * pages missing screenshots
    * unmatched captures
    * pages with multiple captures/states
* On each page tab, add:
    * screenshot preview
    * capture selector if multiple captures exist
    * visual-audit findings list
    * empty state when no screenshot is attached
* Add actions:
    * Upload screenshots
    * Replace screenshot
    * Remove screenshot
    * Assign unmatched
* Keep current PBIR findings and visual metadata sections intact.
    Visual audit is additive, not a replacement.

5. Future V2: AI-assisted Chrome/Edge inspection

* Build this as a second evidence source, not a different scoring system.
* Introduce a VisualEvidenceSource abstraction with two implementations:
    * UploadedScreenshotsSource
    * future ChromiumInspectionSource
* Chosen browser strategy: guided Chromium inspection, not full login automation.
* User manually signs in to Power BI and opens the report in Chrome or Edge.
* The extension attaches to the chosen browser/tab through a Chromium-compatible inspection layer.
    Default approach: DevTools Protocol-based adapter with browser profiles chrome and edge.
* The live inspector captures:
    * viewport screenshot
    * current URL/title
    * basic DOM/viewport facts for debugging and traceability
* The mapping model still stays PBIR-first:
    * PBIR pages are the expected set
    * the user confirms or corrects page-to-capture mapping
    * optional auto-advance between visible Power BI tabs is best-effort only
    * manual capture fallback is always available
* Browser mode should reuse the same VisualAuditSession, captureId, and rendering model as uploaded screenshots.
* Edge support is included by design because the future browser adapter targets Chromium-compatible inspection, not a Chrome-only website script path.

Important Interfaces and Data

* Extend extension-side contracts with:
    * VisualAuditSession
    * VisualAuditPageCoverage
    * VisualCapture
    * VisualAuditFinding
    * VisualAuditResult
* Session shape should include:
    * reportPath
    * reportKey
    * createdAt
    * updatedAt
    * pages[]
    * unmatchedCaptures[]
* VisualCapture should include:
    * captureId
    * pageName
    * stateName?
    * fileName
    * storedPath
    * source: upload | browser
    * browserFamily?: chrome | edge
    * capturedAt
    * originalPath?
    * viewport?
* VisualAuditFinding should include:
    * findingId
    * pageName
    * captureId
    * findingType
    * severity
    * confidence
    * text
    * recommendation?
    * regionHint?
* Keep model/pbir/scoreReport unchanged.
* If later backend merge is needed, add a new request for normalized audit results rather than extending the existing scoring request first.

Test Plan

* Extension unit tests:
    * filename-to-page matching
    * unmatched screenshot handling
    * session persistence and reload
    * multi-capture selector behavior
    * page coverage summaries
* Score panel tests:
    * overall coverage card rendering
    * page tab with no screenshot
    * page tab with one screenshot
    * page tab with multiple states/captures
    * visual-audit findings rendered alongside existing PBIR findings
* Provider contract tests:
    * valid structured AI response
    * malformed AI response fallback
    * low-confidence findings shown but clearly marked
* Browser-source tests for V2:
    * browser selection and attach flow
    * Chrome and Edge target discovery
    * manual capture fallback when tab automation fails
* Manual UAT:
    * import screenshots for a 5+ page report
    * confirm missing-page detection
    * confirm page mapping corrections persist
    * confirm uploaded screenshots survive VS Code reload
    * future: guided capture against both Chrome and Edge on a Power BI report with several tabs

Assumptions and Defaults

* V1 is screenshot upload plus AI-assisted screenshot review, not browser automation.
* Visual-audit findings are non-scored in V1.
* Screenshot assets are copied into extension storage so the session is stable even if original files move.
* One report page may have multiple captures; top-level score tabs remain page-based, with capture selection inside the page tab.
* Secrets for any future AI provider live in VS Code SecretStorage, not in repo files or backend request payloads.
* Future live browser inspection is guided and opt-in, not silent background browsing or login automation.
* Future Chromium support assumes a DevTools-Protocol-compatible attach model for both Chrome and Edge.
    References:
    * Microsoft Edge DevTools Protocol: https://learn.microsoft.com/en-us/microsoft-edge/devtools/protocol/
    * Puppeteer launch/executable path behavior: https://pptr.dev/api/puppeteer.puppeteernode.launch