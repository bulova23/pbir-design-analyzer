# Per-Page/Per-Tab Report Scoring — Troubleshooting Guide

This guide helps resolve common issues encountered when using per-page scoring in PBIR.

---

## Table of Contents

1. ["Page not found" Error](#page-not-found-error)
2. [Tab Bar Missing or Tabs Not Switching](#tab-bar-missing-or-tabs-not-switching)
3. [Scoring Takes Longer Than Expected](#scoring-takes-longer-than-expected)
4. [Recommendations Need Manual Changes](#recommendations-need-manual-changes)
5. [Page Failed to Score (Partial Failure)](#page-failed-to-score-partial-failure)
6. [Scores Look Incorrect or Don't Match Report](#scores-look-incorrect-or-dont-match-report)
7. [Single-Page Report Unexpectedly Shows Tabs](#single-page-report-unexpectedly-shows-tabs)
8. [Can't Select Pages from Tree View](#cant-select-pages-from-tree-view)
9. [Overall Report Score Differs from Individual Page Scores](#overall-report-score-differs-from-individual-page-scores)
10. [Performance Baseline](#performance-baseline)

---

## "Page not found" Error

**Symptom**: 
```
ERROR: Page 'Sales Analysis' not found in report
```

### Diagnosis

1. **Exact spelling**: The page name you entered doesn't match any page in the report
2. **Case sensitivity**: "sales analysis" and "Sales Analysis" are different
3. **Whitespace**: Extra spaces (leading, trailing, or in the middle)
4. **Special characters**: Character encoding mismatch or typo

### Resolution

**Option 1: Use the Tree View (Recommended)**
1. Open the **PBIR Reports** tree in the Activity Bar
2. Expand your report
3. Right-click the page you want to score
4. Select **Score Page**
5. The extension handles the exact name automatically

**Option 2: Copy-Paste the Name**
1. Open the **PBIR Reports** tree
2. Find the page in the tree
3. Open a Command Palette (`⌘⇧P`)
4. Run **PBIR: Score Page**
5. Paste the page name from the tree (exact match guaranteed)

**Option 3: Verify the Page Exists**
1. Open the report in Power BI Desktop
2. Look at the page tabs at the bottom
3. Check the exact spelling and capitalization
4. Return to VS Code and use the exact name

### Prevention

- Always use the tree view instead of manual entry
- Don't rely on memory of page names
- If you must type manually, use copy-paste

---

## Tab Bar Missing or Tabs Not Switching

**Symptom**: 
- You scored a multi-page report but see no tabs
- Tabs exist but clicking them doesn't switch pages
- All pages shown in a single view (pages overlapped)

### Diagnosis

| Cause | Indicator |
|-------|-----------|
| Single-page report | Only 1 page in the report |
| Rendering bug | Multiple pages but no tabs visible |
| Browser/VS Code issue | Tabs visible but unresponsive |
| Cache issue | Outdated content displayed |

### Resolution

**For Single-Page Reports**:
- Single-page reports intentionally show no tabs (simpler UI)
- This is expected behavior, not an error
- The page's score is shown directly

**For Multi-Page Reports (Tabs Missing)**:
1. Reload the extension: `Ctrl+R` or `⌘R` (in VS Code)
2. Open the VS Code Developer Console (`⌘⇧I` on macOS)
3. Check for JavaScript errors (red messages in console)
4. If errors appear, screenshot and contact support

**For Tabs Not Switching**:
1. Click a tab once (wait for it to become active)
2. If unresponsive, reload the extension
3. Try again (should be responsive after reload)

**To Force a Full Re-Render**:
1. Close the Optimization Report panel
2. Run **PBIR: Refresh Reports** from the Command Palette
3. Re-run **PBIR: Score Report Quality**
4. Panel should render correctly with tabs

### Prevention

- Keep VS Code and extensions up-to-date
- Clear cache periodically (rarely needed, but try if tabs act strangely)
- Restart VS Code if UI becomes unresponsive

---

## Scoring Takes Longer Than Expected

**Symptom**: Scoring a 20-page report takes >15 seconds (slower than expected)

### Performance Expectations

| Report Size | Expected Time | Max Time |
|-------------|---------------|----------|
| 1 page | ~0.5 seconds | <1 second |
| 5 pages | ~1.5 seconds | <3 seconds |
| 10 pages | ~3 seconds | <5 seconds |
| 20 pages | ~6-8 seconds | <12 seconds |

If your scoring is **slower than "Expected Time"**, investigate.

### Diagnosis

1. **Network latency**: Backend service is slow to respond
2. **Large report size**: Report has many visuals or complex structures
3. **System resources**: Your computer is running low on CPU/memory
4. **Backend service**: .NET scoring engine is not running optimally

### Resolution

**Step 1: Check Backend Service**
1. Run **Power BI MCP: Restart .NET Daemon** from the Command Palette
2. Wait 10 seconds for the service to restart
3. Try scoring again

**Step 2: Check Network**
1. Ensure your internet connection is stable
2. Try a simple operation (refresh tree view)
3. If other operations are slow, it's a network issue
4. Move closer to your router or switch to wired connection

**Step 3: Check System Resources**
1. Open Activity Monitor (macOS) or Task Manager (Windows)
2. Check CPU and Memory usage
3. If CPU is at 100% or memory is critically low, close other applications
4. Try scoring again

**Step 4: Reduce Report Complexity (if applicable)**
1. If a single report is excessively slow:
   - Open in Power BI Desktop
   - Check for unusual visual counts (>100 visuals on a page is excessive)
   - Move some visuals to separate pages
   - Simplify complex filters or conditional formatting
   - Save and re-score

**Step 5: Try Scoring a Single Page**
1. To isolate whether the issue is report-wide or specific:
   - Run **PBIR: Score Page** on a single page
   - If it's fast (~0.5s), the report is large but functional
   - If it's slow, the page may have issues (complex visuals, etc.)

### Typical Bottlenecks

- Large reports (100+ visuals total) → naturally slower
- Complex visual properties → framework analysis takes longer
- Network latency → usually 1-2 second delay (outside PBIR's control)

---

## Recommendations Need Manual Changes

**Symptom**: 
- The analyzer lists issues, but the report still needs manual changes
- Or: The score does not improve after editing the report

### Diagnosis

1. **The issue is informational**: Some findings describe density or design tradeoffs, not a one-click fix
2. **The wrong visual was changed**: The recommendation may point to a specific page area or visual type
3. **The page still violates the threshold**: A partial edit may not be enough to improve the framework score

### Resolution

1. Review the framework feedback for your page
2. Expand the affected visuals list where available and inspect the cited visuals in the PBIR explorer
3. Adjust the layout, theme, chart choice, or navigation density in Power BI Desktop or the PBIR JSON
4. Re-score after making the manual changes to confirm the result

**If edits were made but you don't see changes**:
1. The changes are applied to the `.pbir` file on disk
2. Power BI Desktop must reload to display changes
3. Close and reopen the report in Power BI Desktop
4. Or: Close Power BI and re-open the file (full reload)

**If the score still looks wrong after edits**:
1. Refresh the PBIR tree and re-run scoring
2. Confirm the report save actually updated the PBIR files on disk
3. Check whether a different page or visual is still driving the same penalty

### Common Reasons Scores Don’t Move

| Reason | Solution |
|--------|----------|
| Page has no visuals | Add visuals to the page |
| Visuals already optimized | Review feedback for remaining issues |
| Wrong visual changed | Use the affected-visual references to locate the actual contributor |
| Issues not detected | Refresh and re-score after the PBIR files change on disk |

---

## Page Failed to Score (Partial Failure)

**Symptom**: 
```
WARNING: 1 page failed to score
Page 'Regional Breakdown': Invalid visual structure
```

### Diagnosis

The page has a structural issue preventing scoring. See [Partial Failure & Recovery Guide](./PBIR_ERROR_HANDLING.md) for detailed resolution.

### Quick Resolution

1. **Open the report in Power BI Desktop**
2. **Click the problematic page** ("Regional Breakdown" in the example)
3. **Check each visual**:
   - Ensure all visuals have data bindings (drag fields to Visualizations pane)
   - Look for visuals with red error icons
   - Ensure no custom visuals that might not be supported
4. **Save the report**
5. **Return to VS Code and re-score**

### Prevention

- Validate reports in Power BI Desktop before scoring
- Ensure all visuals have proper data bindings
- Use built-in Power BI visuals, not custom or marketplace visuals

---

## Scores Look Incorrect or Don't Match Report

**Symptom**: 
- Score seems unfairly low
- Framework feedback doesn't seem to apply to your report
- Per-page scores seem inconsistent with what you see visually

### Diagnosis

1. **Misunderstanding of scoring model**: The 6 frameworks score specific aspects
2. **Unexpected visual counting**: Framework counts visuals differently than you might
3. **Data binding issues**: Visuals with hidden/filtered data don't appear in some frameworks

### Resolution

**Step 1: Review the 6-Framework Model**

See the [PBIR_GUIDE.md — The 6-Framework Scoring Model](../PBIR_GUIDE.md#the-6-framework-scoring-model) section to understand what each framework checks.

**Step 2: Read Framework Feedback**

In the Optimization Report panel, click each framework panel to expand it. Read the specific **⚠ issues** listed:
- Each issue explains what the framework detected
- Recommendations show how to address it

**Step 3: Compare to Your Visual Design**

1. Count your visuals: How many are visible? (Frameworks count non-decorative visuals)
2. Check layout: Are visuals aligned? (Gestalt framework checks this)
3. Review colours: How many unique colours? (Visual Best Practices framework checks palette size)

**Step 4: Re-Read the Frameworks**

If you think a framework's feedback is wrong:
1. Review the framework description again (sometimes the issue is subtle)
2. Check the **✓ items** to see what the framework already approves
3. The **⚠ items** are specific issues the framework identified

### Common Misconceptions

| Misconception | Reality |
|---------------|---------|
| "My report looks great, why is the score low?" | Visual design is subjective; frameworks rate specific measurable properties |
| "I have 6 visuals, why is Cognitive Load saying too many?" | Some frameworks count decorative elements differently |
| "All my colours are from my brand palette, why is it flagged?" | VBP framework checks for consistency and perceptual distinctness, not brand adherence |

---

## Single-Page Report Unexpectedly Shows Tabs

**Symptom**: 
- Report has only 1 page
- Tabs are shown anyway (should be non-tabbed for single-page)
- Only 1 tab visible ("Page 1" or "Overall Report")

### Diagnosis

This is a rare rendering edge case, not typical behavior.

**Likely cause**: UI rendering bug or race condition during load

### Resolution

1. **Reload the extension**:
   - Press `Ctrl+R` or `⌘R` in VS Code
   - Wait for the extension to reload
   - Re-open the Optimization Report panel

2. **Close and re-score**:
   - Close the Optimization Report panel
   - Run **PBIR: Score Report Quality** again
   - Panel should render correctly (non-tabbed)

3. **Check browser console**:
   - Open Developer Tools (`⌘⇧I` on macOS)
   - Look for JavaScript errors in the Console
   - If errors appear, screenshot and contact support

### Prevention

- Keep extensions up-to-date
- Don't have too many panels open simultaneously
- Restart VS Code periodically to prevent UI drift

---

## Can't Select Pages from Tree View

**Symptom**: 
- PBIR Reports tree is empty or not showing pages
- "Right-click page" is not available
- Tree shows "No PBIP model connected"

### Diagnosis

| Indicator | Likely Cause |
|-----------|--------------|
| "No PBIP model connected" message | Need to connect a PBIP project |
| Tree shows report but no pages | Report has no pages or is malformed |
| Tree doesn't exist in Activity Bar | Extension not loaded or disabled |

### Resolution

**If "No PBIP model connected"**:
1. Run **Power BI MCP: Connect to Model** from the Command Palette
2. Select your PBIP project folder
3. The tree should populate with reports and pages

**If tree shows report but no pages**:
1. Check if the report is valid in Power BI Desktop
2. Right-click the report in the tree and select **Refresh**
3. Or: Run **PBIR: Refresh Reports** from the Command Palette
4. If still empty, the report may be malformed; open in Power BI Desktop to diagnose

**If tree section missing**:
1. Check if the Power BI Modeling MCP extension is installed
2. Check if the extension is enabled (not disabled)
3. Reload VS Code (`Ctrl+R` or `⌘R`)
4. The PBIR Reports section should appear in the Activity Bar

### Prevention

- Keep your PBIP project connected
- Ensure the Power BI extension is enabled
- Periodically refresh the tree view for latest changes

---

## Overall Report Score Differs from Individual Page Scores

**Symptom**: 
```
Overall Report Score: 72
But page scores are: Page 1 (75), Page 2 (70), Page 3 (75)
Expected average: (75 + 70 + 75) / 3 = 73, not 72
```

### Diagnosis

**Possible causes**:
1. One page failed during scoring (excluded from average)
2. Rounding: 73.33 rounds down to 73, not up to 74
3. Weighted aggregation: Some pages may have more influence based on size/complexity

### Resolution

**Check for failed pages**:
1. Look at the tab bar
2. Click each page tab and verify all pages show scores (no error messages)
3. If one page has an error, the overall score excludes it

**Understand rounding**:
- PBIR uses integer scoring (0-100)
- Fractional averages round to nearest integer
- (75 + 70 + 75) / 3 = 73.33 → displays as 73

**If still inconsistent**:
1. Screenshot the overall score and all page scores
2. Open the VS Code Developer Console and check for errors
3. Contact support with the information

---

## Performance Baseline

### Expected Performance by Report Size

| Pages | Score Type | Expected Time | Max Time | Notes |
|-------|-----------|---------------|----------|-------|
| 1 | Single-page | ~0.5s | <1s | Very fast, suitable for real-time feedback |
| 1 | Full report | ~1s | <2s | Single page scored as full report (tabs not shown) |
| 5 | Full report | ~1.5s | <3s | Comfortable for typical report |
| 10 | Full report | ~3s | <5s | Still responsive |
| 20 | Full report | ~6-8s | <12s | Acceptable, within budget |
| 50+ | Full report | 15-30s | >30s | Large reports; consider scoring per-page instead |

### Performance Tips

1. **Score single pages** for real-time feedback during editing
2. **Score full reports** periodically to ensure overall quality
3. **Restart the service** if scoring slows down dramatically
4. **Monitor system resources** — ensure other applications aren't competing for CPU/memory
5. **Keep reports under 50 pages** — very large reports may exceed comfortable scoring time

### If Performance Degrades

1. Restart the .NET backend: **Power BI MCP: Restart .NET Daemon**
2. Close and reopen VS Code
3. Check system resources (Activity Monitor or Task Manager)
4. Try scoring a smaller report to isolate the issue

---

## Still Having Issues?

If your issue isn't covered above:

1. **Check the comprehensive guides**:
   - [PBIR Page Matching Guide](./PBIR_PAGE_MATCHING.md) — for page identification issues
   - [Error Handling & Partial Failure Guide](./PBIR_ERROR_HANDLING.md) — for scoring errors
   - [PBIR_GUIDE.md](../PBIR_GUIDE.md) — for general feature documentation

2. **Gather diagnostic information**:
   - Screenshot of the error message
   - Report name and approximate size (page count, visual count)
   - Steps you've already tried
   - Your VS Code version and extension version

3. **Contact support** with this information

---

## Summary Checklist

- [ ] Is the page name spelled correctly? (Use tree view to verify)
- [ ] Have you restarted the .NET backend service?
- [ ] Is the report valid in Power BI Desktop? (no error indicators)
- [ ] Do all visuals have data bindings?
- [ ] Is your system running low on CPU/memory?
- [ ] Have you tried reloading VS Code?
- [ ] Is your network connection stable?

Good luck, and happy scoring!
