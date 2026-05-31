# Issues Workspace Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a normalized findings contract and make the Issues workspace the primary review surface in the score panel, while relocating framework, metadata, and audit detail into evidence drilldown.

**Architecture:** Keep the current scoring engine and most host payloads intact for this slice. Add a frontend-facing normalized findings layer in the score-panel contract and payload shaping path, then build a dedicated issue-centric rendering path in the webview that consumes those findings and demotes raw evidence sections behind progressive disclosure.

**Tech Stack:** TypeScript, React, Jest, CSS, VS Code webview UI

---

## File Structure

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - Add the normalized finding enums, interfaces, evidence references, and panel-state fields.
- Create: `vscode-extension/src/analyzer/score/normalizedFindings.ts`
  - Centralize mapping from existing score result structures into normalized finding objects.
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
  - Build normalized findings into the host-to-webview payload.
- Create: `vscode-extension/src/test/normalizedFindings.test.ts`
  - Add focused contract and mapping coverage for the new finding builder.
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
  - Assert the normalized findings are present and correctly shaped in payload output.
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
  - Replace the framework-first middle of the page with an Issues workspace and Evidence section.
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
  - Add issue card, issue group, evidence section, and smart-collapse styling.
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`
  - Cover the new issue-centric rendering order and collapse defaults.

## Implementation Notes

- Do not redesign the full five-zone shell in this plan.
- Do not move scoring logic into the frontend.
- Prefer deriving findings from existing structures over inventing new backend fields unless the UI is blocked.
- Keep packet preview/export behavior working, but demote its prominence rather than redesigning it.

### Task 1: Define The Normalized Finding Contract

**Files:**
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Test: `vscode-extension/src/test/normalizedFindings.test.ts`

- [ ] **Step 1: Write the failing contract test**

```ts
import type { NormalizedFinding, NormalizedFindingSeverity } from '../analyzer/contracts/scorePanel';

describe('normalized finding contract', () => {
  it('supports the issue workspace attributes needed for triage', () => {
    const finding: NormalizedFinding = {
      id: 'consistency-title-anchor',
      title: 'Inconsistent title anchors across pages',
      summary: 'Page titles drift vertically across overview and detail pages.',
      severity: 'high',
      confidence: 92,
      scope: 'crossPage',
      detectionType: 'deterministic',
      affectedPages: ['Intro', 'Net Sales'],
      impactArea: 'governance',
      frameworkImpact: ['Enterprise Governance', 'Gestalt'],
      recommendation: 'Normalize title anchor placement across report pages.',
      sourceKind: 'reportConsistency',
      sourceSection: 'issues',
      evidence: [],
    };

    const severity: NormalizedFindingSeverity = finding.severity;

    expect(severity).toBe('high');
    expect(finding.confidence).toBeGreaterThanOrEqual(0);
    expect(finding.confidence).toBeLessThanOrEqual(100);
  });
});
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/normalizedFindings.test.ts`

Expected: FAIL because `NormalizedFinding` types do not exist yet.

- [ ] **Step 3: Add the contract types**

```ts
export type NormalizedFindingSeverity = 'high' | 'medium' | 'low' | 'info';
export type NormalizedFindingScope = 'visual' | 'page' | 'crossPage' | 'report';
export type NormalizedFindingDetectionType = 'deterministic' | 'aiAssisted' | 'mixed';
export type NormalizedFindingImpactArea =
  | 'layout'
  | 'storytelling'
  | 'accessibility'
  | 'governance'
  | 'density'
  | 'navigation'
  | 'kpiEffectiveness'
  | 'benchmark'
  | 'actionability'
  | 'metadata';

export interface NormalizedFindingEvidenceReference {
  kind: 'framework' | 'audit' | 'metadata' | 'consistency' | 'quickFix';
  label: string;
  pageName?: string;
  frameworkKey?: string;
  visualId?: string;
  detail?: string;
}

export interface NormalizedFinding {
  id: string;
  title: string;
  summary: string;
  severity: NormalizedFindingSeverity;
  confidence: number;
  scope: NormalizedFindingScope;
  detectionType: NormalizedFindingDetectionType;
  affectedPages: string[];
  impactArea: NormalizedFindingImpactArea;
  frameworkImpact: string[];
  recommendation: string;
  sourceKind: string;
  sourceSection: 'issues' | 'evidence';
  evidence: NormalizedFindingEvidenceReference[];
}
```

- [ ] **Step 4: Re-run the focused test**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/normalizedFindings.test.ts`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add vscode-extension/src/analyzer/contracts/scorePanel.ts \
        vscode-extension/src/test/normalizedFindings.test.ts
git commit -m "feat(score-panel): add normalized finding contract"
```

### Task 2: Build A Dedicated Findings Mapper

**Files:**
- Create: `vscode-extension/src/analyzer/score/normalizedFindings.ts`
- Test: `vscode-extension/src/test/normalizedFindings.test.ts`

- [ ] **Step 1: Extend the failing test with real source-shape coverage**

```ts
import { buildNormalizedFindings } from '../analyzer/score/normalizedFindings';

describe('buildNormalizedFindings', () => {
  it('maps report consistency issues into normalized findings', () => {
    const findings = buildNormalizedFindings({
      reportConsistency: {
        issues: [
          {
            category: 'Layout Consistency',
            issueCategory: 'titleAnchors',
            overallFinding: 'Title positions drift across pages.',
            affectedPages: ['Intro', 'Forecast'],
            severity: 'high',
            confidence: 'high',
            recommendedRemediation: 'Align title anchors.',
          },
        ],
      },
    });

    expect(findings).toHaveLength(1);
    expect(findings[0]).toMatchObject({
      severity: 'high',
      scope: 'crossPage',
      detectionType: 'deterministic',
      affectedPages: ['Intro', 'Forecast'],
      impactArea: 'governance',
      recommendation: 'Align title anchors.',
    });
  });
});
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/normalizedFindings.test.ts`

Expected: FAIL because `buildNormalizedFindings()` does not exist.

- [ ] **Step 3: Implement the mapper with the first source adapters**

```ts
import type {
  ActionabilityBreakdown,
  AuditState,
  BenchmarkComparisonSummary,
  NormalizedFinding,
  ReportConsistencySummary,
  ScoreResult,
} from '../contracts/scorePanel';

interface BuildNormalizedFindingsInput {
  result?: ScoreResult;
  reportConsistency?: ReportConsistencySummary;
  audit?: AuditState;
  actionability?: ActionabilityBreakdown;
  benchmark?: BenchmarkComparisonSummary;
}

export function buildNormalizedFindings(input: BuildNormalizedFindingsInput): NormalizedFinding[] {
  const findings: NormalizedFinding[] = [];

  for (const issue of input.reportConsistency?.issues ?? []) {
    findings.push({
      id: `report-consistency-${issue.issueCategory}-${issue.affectedPages.join('-')}`,
      title: issue.category,
      summary: issue.overallFinding,
      severity: issue.severity,
      confidence: issue.confidence === 'high' ? 90 : issue.confidence === 'medium' ? 70 : 50,
      scope: 'crossPage',
      detectionType: 'deterministic',
      affectedPages: issue.affectedPages,
      impactArea: 'governance',
      frameworkImpact: ['Enterprise Governance'],
      recommendation: issue.recommendedRemediation,
      sourceKind: 'reportConsistency',
      sourceSection: 'issues',
      evidence: [
        {
          kind: 'consistency',
          label: issue.issueCategory,
          detail: issue.overallFinding,
        },
      ],
    });
  }

  return findings.sort((left, right) => right.confidence - left.confidence);
}
```

- [ ] **Step 4: Expand the mapper coverage for first-slice sources**

```ts
it('maps actionability and benchmark gaps into issue findings', () => {
  const findings = buildNormalizedFindings({
    actionability: {
      score: 42,
      targetBenchmarkPresent: false,
      exceptionVisibility: false,
      urgencySignaling: true,
      priorPeriodContext: false,
      drillPathPresent: false,
      expectationLevel: 'high',
      strengths: [],
      gaps: ['No clear benchmark target.', 'No visible drill path.'],
      summary: 'The page is visually clear but not actionable enough.',
    },
    benchmark: {
      archetype: 'Executive Scorecard',
      benchmarkLabel: 'Enterprise norms',
      comparativePosition: 'below',
      beautifulButUseless: true,
      insight: 'The page looks polished but does not support decisions quickly.',
      strengths: [],
      gaps: ['Decision context is weak.'],
    },
  });

  expect(findings.map((finding) => finding.impactArea)).toEqual(
    expect.arrayContaining(['actionability', 'benchmark']),
  );
});
```

- [ ] **Step 5: Re-run the focused test**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/normalizedFindings.test.ts`

Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add vscode-extension/src/analyzer/score/normalizedFindings.ts \
        vscode-extension/src/test/normalizedFindings.test.ts
git commit -m "feat(score-panel): derive normalized findings from score data"
```

### Task 3: Add Findings To The Score Panel Payload

**Files:**
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`

- [ ] **Step 1: Write a failing payload test**

```ts
it('includes normalized findings in the webview payload', () => {
  const payload = toScorePanelState({
    result: mockScoreResult,
    audit: mockAuditState,
  });

  expect(payload.normalizedFindings).toBeDefined();
  expect(payload.normalizedFindings.length).toBeGreaterThan(0);
  expect(payload.normalizedFindings[0]).toEqual(
    expect.objectContaining({
      severity: expect.any(String),
      confidence: expect.any(Number),
      scope: expect.any(String),
      detectionType: expect.any(String),
      affectedPages: expect.any(Array),
      impactArea: expect.any(String),
      frameworkImpact: expect.any(Array),
      recommendation: expect.any(String),
    }),
  );
});
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/scoreResultPayload.test.ts`

Expected: FAIL because `normalizedFindings` is not part of the payload yet.

- [ ] **Step 3: Add payload plumbing**

```ts
import { buildNormalizedFindings } from '../analyzer/score/normalizedFindings';

const normalizedFindings = buildNormalizedFindings({
  result,
  reportConsistency: result.reportConsistencySummary,
  audit,
});

return {
  ...existingState,
  normalizedFindings,
};
```

- [ ] **Step 4: Ensure page-level and overall-view payloads both carry findings**

```ts
const pageScopedFindings = normalizedFindings.filter((finding) => {
  return finding.scope === 'report' ||
    finding.affectedPages.length === 0 ||
    finding.affectedPages.includes(selectedPageName);
});
```

- [ ] **Step 5: Re-run the focused test**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/scoreResultPayload.test.ts`

Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add vscode-extension/src/views/scoreResultPayload.ts \
        vscode-extension/src/test/scoreResultPayload.test.ts \
        vscode-extension/src/analyzer/contracts/scorePanel.ts
git commit -m "feat(score-panel): send normalized findings to webview"
```

### Task 4: Build The Issues Workspace UI

**Files:**
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] **Step 1: Write a failing webview test for the new primary surface**

```tsx
it('renders an Issues workspace before framework detail', () => {
  render(<App initialState={mockStateWithNormalizedFindings} />);

  expect(screen.getByRole('heading', { name: /issues/i })).toBeInTheDocument();
  expect(screen.getByText(/inconsistent title anchors across pages/i)).toBeInTheDocument();

  const issuesHeading = screen.getByRole('heading', { name: /issues/i });
  const frameworksHeading = screen.getByRole('heading', { name: /framework/i });

  expect(
    issuesHeading.compareDocumentPosition(frameworksHeading) & Node.DOCUMENT_POSITION_FOLLOWING,
  ).toBeTruthy();
});
```

- [ ] **Step 2: Run the focused webview test to verify it fails**

Run: `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`

Expected: FAIL because no Issues workspace exists yet.

- [ ] **Step 3: Render grouped issue cards**

```tsx
function groupFindingsBySeverity(findings: NormalizedFinding[]): Array<[string, NormalizedFinding[]]> {
  const buckets = new Map<string, NormalizedFinding[]>();

  for (const finding of findings) {
    const bucket = buckets.get(finding.severity) ?? [];
    bucket.push(finding);
    buckets.set(finding.severity, bucket);
  }

  return ['high', 'medium', 'low', 'info']
    .map((severity) => [severity, buckets.get(severity) ?? []] as const)
    .filter(([, items]) => items.length > 0);
}
```

- [ ] **Step 4: Render the required finding attributes on each card**

```tsx
<article className={`issue-card issue-${finding.severity}`} key={finding.id}>
  <div className="issue-card-head">
    <h3>{finding.title}</h3>
    <span className="issue-severity">{finding.severity}</span>
  </div>
  <p>{finding.summary}</p>
  <dl className="issue-meta-grid">
    <div><dt>Confidence</dt><dd>{finding.confidence}</dd></div>
    <div><dt>Scope</dt><dd>{finding.scope}</dd></div>
    <div><dt>Detection</dt><dd>{finding.detectionType}</dd></div>
    <div><dt>Impact</dt><dd>{finding.impactArea}</dd></div>
  </dl>
  <p className="issue-pages">{finding.affectedPages.join(', ')}</p>
  <p className="issue-frameworks">{finding.frameworkImpact.join(', ')}</p>
  <p className="issue-recommendation">{finding.recommendation}</p>
</article>
```

- [ ] **Step 5: Re-run the focused webview test**

Run: `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`

Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add vscode-extension/webview-src/analyzer-score/App.tsx \
        vscode-extension/webview-src/analyzer-score/styles.css \
        vscode-extension/webview-src/analyzer-score/App.test.tsx
git commit -m "feat(score-panel): add issue-centric workspace"
```

### Task 5: Relocate Framework, Metadata, And Audit Detail Into Evidence

**Files:**
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] **Step 1: Write a failing webview test for evidence demotion**

```tsx
it('renders framework, metadata, and audit detail under evidence drilldown', () => {
  render(<App initialState={mockStateWithNormalizedFindings} />);

  expect(screen.getByRole('heading', { name: /evidence/i })).toBeInTheDocument();
  expect(screen.getByText(/framework analysis/i)).toBeInTheDocument();
  expect(screen.getByText(/parsed metadata/i)).toBeInTheDocument();
  expect(screen.getByText(/screenshot audit/i)).toBeInTheDocument();
});
```

- [ ] **Step 2: Run the focused webview test to verify it fails**

Run: `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`

Expected: FAIL because those sections are still primary peers in the page flow.

- [ ] **Step 3: Wrap existing detail sections in an Evidence section**

```tsx
<section className="evidence-section">
  <details open={false}>
    <summary>Framework analysis</summary>
    {renderFrameworkSections(...)}
  </details>
  <details open={false}>
    <summary>Parsed metadata</summary>
    {renderMetadataSections(...)}
  </details>
  <details open={false}>
    <summary>Screenshot audit</summary>
    {renderAuditSections(...)}
  </details>
</section>
```

- [ ] **Step 4: Re-run the focused webview test**

Run: `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add vscode-extension/webview-src/analyzer-score/App.tsx \
        vscode-extension/webview-src/analyzer-score/styles.css \
        vscode-extension/webview-src/analyzer-score/App.test.tsx
git commit -m "refactor(score-panel): demote detail sections into evidence"
```

### Task 6: Add Smart-Collapse Defaults

**Files:**
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] **Step 1: Write a failing webview test for default disclosure behavior**

```tsx
it('keeps issues visible while evidence details stay collapsed by default', () => {
  render(<App initialState={mockStateWithNormalizedFindings} />);

  expect(screen.getByRole('heading', { name: /issues/i })).toBeVisible();
  expect(screen.getByText(/high severity/i)).toBeVisible();
  expect(screen.queryByText(/score breakdown/i)).not.toBeVisible();
});
```

- [ ] **Step 2: Run the focused webview test to verify it fails**

Run: `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`

Expected: FAIL because current defaults expose too many details.

- [ ] **Step 3: Apply the smart-collapse defaults**

```tsx
const defaultExpandedSeverities: NormalizedFindingSeverity[] = ['high'];

<details open={defaultExpandedSeverities.includes(severity)}>
  <summary>{severityLabel}</summary>
  {cards}
</details>
```

- [ ] **Step 4: Keep packet preview and other lower-priority sections collapsed where possible**

```tsx
<details className="secondary-panel" open={false}>
  <summary>Review packet preview</summary>
  {previewContent}
</details>
```

- [ ] **Step 5: Re-run the focused webview test**

Run: `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`

Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add vscode-extension/webview-src/analyzer-score/App.tsx \
        vscode-extension/webview-src/analyzer-score/styles.css \
        vscode-extension/webview-src/analyzer-score/App.test.tsx
git commit -m "feat(score-panel): add smart-collapse defaults"
```

### Task 7: Run Full Validation

**Files:**
- Modify if needed: `vscode-extension/src/test/normalizedFindings.test.ts`
- Modify if needed: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Modify if needed: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] **Step 1: Run the focused extension tests**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/normalizedFindings.test.ts src/test/scoreResultPayload.test.ts`

Expected: PASS

- [ ] **Step 2: Run the focused webview test**

Run: `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`

Expected: PASS

- [ ] **Step 3: Run compile validation**

Run: `cd vscode-extension && npm run compile`

Expected: PASS

- [ ] **Step 4: Run the full extension test suite**

Run: `cd vscode-extension && npm test`

Expected: PASS

- [ ] **Step 5: Record residual risks**

```md
- Finding deduplication remains intentionally light in the first slice.
- Persona-aware ranking, heatmaps, and export workspace cleanup remain deferred.
- If evidence discoverability feels too hidden in smoke testing, adjust labels before widening scope.
```
