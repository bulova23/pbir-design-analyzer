import {
  buildReviewWorkflowExportData,
  exportReviewWorkflowAsHtml,
  exportReviewWorkflowAsJson,
  exportReviewWorkflowAsMarkdown,
  exportReviewWorkflowAsPdf,
} from '../analyzer/score/reviewWorkflowExport';
import type { IntentFeedbackEntry, ScoreResult } from '../analyzer/contracts/scorePanel';

function makeScoreResult(overrides: Partial<ScoreResult> = {}): ScoreResult {
  return {
    reportPath: '/workspace/FY26 Executive Report.Report',
    scoredAt: '2026-05-27T15:56:16.000Z',
    compositeScore: 81.3,
    gestaltScore: 84,
    cognitiveLoadScore: 78,
    dataInkScore: 80,
    accessibilityScore: 77,
    visualBestPracticesScore: 82,
    stephenFewScore: 83,
    enterpriseGovernanceScore: 88,
    tufteScore: 79,
    graphicalPerceptionScore: 76,
    densityScore: 74,
    narrativeScore: 85,
    feedback: {},
    pageCount: 3,
    recommendations: [
      '[High] Standardize revenue terminology across pages.',
      '[Medium] Align title placement with the dominant report header zone.',
      '[Medium] Clarify variance takeaway on analytical deep-dive pages.',
    ],
    reportConsistencySummary: {
      consistentTitleAnchors: false,
      consistentFilterBand: true,
      consistentMetricLabels: false,
      consistentSemanticColors: true,
      overallFinding: 'The report reads coherently overall, but naming drift remains.',
      affectedPages: ['Details'],
      issueCount: 2,
      issues: [
        {
          category: 'Metric labels',
          issueCategory: 'metricLabelDrift',
          overallFinding: 'Sales and Revenue are used interchangeably.',
          affectedPages: ['Details'],
          severity: 'medium',
          confidence: 'high',
          recommendedRemediation: 'Standardize the business term used for top-line revenue.',
        },
        {
          category: 'Layout',
          issueCategory: 'titleAnchorDrift',
          overallFinding: 'One page breaks the dominant title zone.',
          affectedPages: ['Overview', 'Details'],
          severity: 'low',
          confidence: 'medium',
          recommendedRemediation: 'Align title placement with the report header convention.',
        },
      ],
      findings: [
        'Sales and Revenue are used interchangeably.',
        'One page breaks the dominant title zone.',
      ],
    },
    pageScores: [
      {
        pageName: 'Overview',
        gestaltScore: 86,
        cognitiveLoadScore: 82,
        dataInkScore: 81,
        accessibilityScore: 79,
        visualBestPracticesScore: 84,
        stephenFewScore: 85,
        enterpriseGovernanceScore: 89,
        tufteScore: 80,
        graphicalPerceptionScore: 78,
        densityScore: 76,
        narrativeScore: 88,
        compositeScore: 84,
        feedback: {},
        recommendations: [],
        inferredStorySummary: {
          intentProfile: 'executiveOverview',
          storyArchetype: 'executive overview + trend + comparison',
          inferredStory: 'This page appears to summarize revenue performance over time.',
          confidence: 'high',
          evidence: ['Top KPI band and lead line chart'],
        },
      },
      {
        pageName: 'Details',
        gestaltScore: 79,
        cognitiveLoadScore: 74,
        dataInkScore: 77,
        accessibilityScore: 76,
        visualBestPracticesScore: 79,
        stephenFewScore: 78,
        enterpriseGovernanceScore: 84,
        tufteScore: 77,
        graphicalPerceptionScore: 75,
        densityScore: 73,
        narrativeScore: 71,
        compositeScore: 77,
        feedback: {},
        recommendations: [],
        inferredStorySummary: {
          intentProfile: 'analyticalDeepDive',
          storyArchetype: 'trend',
          inferredStory: 'This page appears to analyze category-level variance over time.',
          confidence: 'medium',
          evidence: ['Category trend line chart'],
        },
      },
      {
        pageName: 'Appendix',
        gestaltScore: 72,
        cognitiveLoadScore: 70,
        dataInkScore: 71,
        accessibilityScore: 73,
        visualBestPracticesScore: 72,
        stephenFewScore: 71,
        enterpriseGovernanceScore: 80,
        tufteScore: 70,
        graphicalPerceptionScore: 68,
        densityScore: 69,
        narrativeScore: 66,
        compositeScore: 71,
        feedback: {},
        recommendations: [],
        inferredStorySummary: {
          intentProfile: 'detailReference',
          storyArchetype: 'comparison',
          inferredStory: 'This page appears to provide supporting detail.',
          confidence: 'medium',
          evidence: ['Dense comparison table'],
        },
      },
    ],
    ...overrides,
  };
}

function makeFeedback(entries: Partial<IntentFeedbackEntry>[] = []): IntentFeedbackEntry[] {
  return entries.map((entry, index) => ({
    pageName: entry.pageName ?? `Page ${index + 1}`,
    inferredIntent: entry.inferredIntent ?? 'executiveOverview',
    storyArchetype: entry.storyArchetype,
    userConfirmation: entry.userConfirmation ?? 'yes',
    note: entry.note,
    timestamp: entry.timestamp ?? `2026-05-27T16:0${index}:00.000Z`,
    analyzerVersion: entry.analyzerVersion ?? '1.2.3',
    reportSessionId: entry.reportSessionId ?? 'abc123:2026-05-27T16:00:00.000Z',
    inferenceConfidence: entry.inferenceConfidence ?? 'high',
    pageId: entry.pageId,
  }));
}

function countPdfPages(pdf: Buffer): number {
  const text = pdf.toString('latin1');
  const explicitCount = text.match(/\/Count\s+(\d+)/);
  if (explicitCount) {
    return Number.parseInt(explicitCount[1], 10);
  }

  return (text.match(/\/Type \/Page\b/g) ?? []).length;
}

function extractPdfText(pdf: Buffer): string {
  const text = pdf.toString('latin1');
  const chunks: string[] = [];
  const hexMatches = text.match(/<([0-9A-Fa-f]+)>/g) ?? [];

  for (const match of hexMatches) {
    const hex = match.slice(1, -1);
    if (hex.length % 2 !== 0) {
      continue;
    }

    const decoded = Buffer.from(hex, 'hex').toString('latin1');
    if (/[A-Za-z]/.test(decoded)) {
      chunks.push(decoded);
    }
  }

  return chunks.join(' ');
}

describe('buildReviewWorkflowExportData', () => {
  it('derives report-level review counts and page statuses from persisted feedback', () => {
    const data = buildReviewWorkflowExportData(
      makeScoreResult(),
      makeFeedback([
        {
          pageName: 'Overview',
          inferredIntent: 'executiveOverview',
          storyArchetype: 'executive overview + trend + comparison',
          userConfirmation: 'yes',
          note: 'Ready for executive playback.',
        },
        {
          pageName: 'Details',
          inferredIntent: 'analyticalDeepDive',
          storyArchetype: 'trend',
          userConfirmation: 'no',
          note: 'Reads as a trend page, not a root-cause page yet.',
          inferenceConfidence: 'medium',
        },
      ]),
    );

    expect(data.reviewSummary.totalPages).toBe(3);
    expect(data.reviewSummary.reviewedPages).toBe(2);
    expect(data.reviewSummary.confirmedPages).toBe(1);
    expect(data.reviewSummary.mismatchPages).toBe(1);
    expect(data.reviewSummary.unreviewedPages).toBe(1);
    expect(data.pages.map((page) => [page.pageName, page.reviewStatus])).toEqual([
      ['Overview', 'confirmed'],
      ['Details', 'mismatch'],
      ['Appendix', 'unreviewed'],
    ]);
    expect(data.pages[1].reviewerNote).toBe('Reads as a trend page, not a root-cause page yet.');
    expect(data.crossPageConsistency?.issueCount).toBe(2);
  });

  it('ignores persisted feedback that does not match the current inferred story signature', () => {
    const data = buildReviewWorkflowExportData(
      makeScoreResult(),
      makeFeedback([
        {
          pageName: 'Overview',
          inferredIntent: 'operationalMonitoring',
          storyArchetype: 'status board',
          userConfirmation: 'no',
        },
      ]),
    );

    expect(data.reviewSummary.reviewedPages).toBe(0);
    expect(data.pages[0].reviewStatus).toBe('unreviewed');
  });

  it('keeps pages without inferred story in the export and reconciles persisted feedback by page name', () => {
    const baseline = makeScoreResult();
    const data = buildReviewWorkflowExportData(
      makeScoreResult({
        pageCount: 3,
        pageScores: [
          {
            ...baseline.pageScores![0],
            pageName: 'Legal',
            inferredStorySummary: undefined,
          },
          baseline.pageScores![1],
          baseline.pageScores![2],
        ],
      }),
      makeFeedback([
        {
          pageName: 'Legal',
          inferredIntent: 'unknown',
          userConfirmation: 'partial',
          note: 'Needs clearer title and purpose framing.',
          inferenceConfidence: 'low',
        },
      ]),
    );

    expect(data.pageCount).toBe(3);
    expect(data.reviewSummary.totalPages).toBe(3);
    expect(data.reviewSummary.reviewedPages).toBe(1);
    expect(data.reviewSummary.partialPages).toBe(1);
    expect(data.reviewSummary.unreviewedPages).toBe(2);
    expect(data.pages.map((page) => page.pageName)).toEqual(['Legal', 'Details', 'Appendix']);
    expect(data.pages[0]).toMatchObject({
      pageName: 'Legal',
      reviewStatus: 'partial',
      inferredIntent: 'unknown',
      reviewerNote: 'Needs clearer title and purpose framing.',
      inferenceConfidence: 'low',
    });
    expect(data.intentValidationSummary.partialPages.map((page) => page.pageName)).toEqual(['Legal']);
    expect(data.intentValidationSummary.pagesNeedingReview.map((page) => page.pageName)).toEqual(['Legal']);
    expect(data.remediationQueue).toEqual([
      expect.objectContaining({
        pageName: 'Legal',
        reviewStatus: 'partial',
        reason: 'Needs clearer title and purpose framing.',
      }),
    ]);
  });

  it('builds richer review packet sections from existing score and feedback signals', () => {
    const data = buildReviewWorkflowExportData(
      makeScoreResult(),
      makeFeedback([
        {
          pageName: 'Overview',
          inferredIntent: 'executiveOverview',
          storyArchetype: 'executive overview + trend + comparison',
          userConfirmation: 'yes',
          note: 'Ready for executive playback.',
        },
        {
          pageName: 'Details',
          inferredIntent: 'analyticalDeepDive',
          storyArchetype: 'trend',
          userConfirmation: 'partial',
          note: 'Needs a clearer variance takeaway.',
          inferenceConfidence: 'medium',
        },
      ]),
    );

    expect(data).toHaveProperty('executiveSummary');
    expect(data).toHaveProperty('intentValidationSummary');
    expect(data).toHaveProperty('remediationQueue');
    expect(data).toHaveProperty('topRecommendations');
    expect(data).toHaveProperty('priorityRecommendations');
    expect(data).toHaveProperty('crossPageConsistencyRollup');
    expect(data).toHaveProperty('appendix');
    expect(data).toHaveProperty('analyzerMetadata');
    expect(data.executiveSummary.overallStatus).toBe('Needs review');
    expect(data.executiveSummary.topStrengths.length).toBeGreaterThan(0);
    expect(data.executiveSummary.topRisks.length).toBeGreaterThan(0);
    expect(data.executiveSummary.topRecommendedActions.length).toBeGreaterThan(0);
    expect(data.intentValidationSummary.confirmedPages).toHaveLength(1);
    expect(data.intentValidationSummary.pagesNeedingReview).toHaveLength(1);
    expect(data.remediationQueue[0].pageName).toBe('Details');
    expect(data.topRecommendations[0]).toContain('Standardize revenue terminology');
    expect(data.priorityRecommendations.some((item) => item.severity === 'medium' && item.issueCategory === 'Metric labels')).toBe(true);
    expect(data.appendix.frameworkScores[0].framework).toBe('Enterprise Governance');
    expect(data.appendix.methodologyNotes).toContain('Intent confirmations are reviewer feedback and do not change scores.');
    expect(data.crossPageConsistencyRollup?.issuesByCategory).toEqual([
      ['Layout', 1],
      ['Metric labels', 1],
    ]);
  });
});

describe('review workflow export serialization', () => {
  it('produces valid JSON with review summary and pages', () => {
    const json = JSON.parse(
      exportReviewWorkflowAsJson(
        buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      ),
    ) as Record<string, unknown>;

    expect(json).toHaveProperty('reportPath');
    expect(json).toHaveProperty('reviewSummary');
    expect(json).toHaveProperty('pages');
    expect(json).toHaveProperty('executiveSummary');
    expect(json).toHaveProperty('intentValidationSummary');
    expect(json).toHaveProperty('remediationQueue');
    expect(json).toHaveProperty('topRecommendations');
    expect(json).toHaveProperty('priorityRecommendations');
    expect(json).toHaveProperty('appendix');
    expect(json).toHaveProperty('analyzerMetadata');
  });

  it('renders consultant-style markdown with executive summary, priority recommendations, consistency summary, and appendix detail', () => {
    const markdown = exportReviewWorkflowAsMarkdown(
      buildReviewWorkflowExportData(
        makeScoreResult(),
        makeFeedback([
          {
            pageName: 'Overview',
            inferredIntent: 'executiveOverview',
            storyArchetype: 'executive overview + trend + comparison',
            userConfirmation: 'yes',
            note: 'Ready for executive playback.',
          },
          {
            pageName: 'Details',
            inferredIntent: 'analyticalDeepDive',
            storyArchetype: 'trend',
            userConfirmation: 'partial',
            note: 'Needs a clearer variance takeaway.',
            inferenceConfidence: 'medium',
          },
        ]),
      ),
    );

    expect(markdown).toContain('# PBIR Design Analyzer Consultant Review Packet');
    expect(markdown).toContain('## Packet Metadata');
    expect(markdown).toContain('## Executive Summary');
    expect(markdown).toContain('### Maturity and Readiness');
    expect(markdown).toContain('### Top Strengths');
    expect(markdown).toContain('### Top Risks');
    expect(markdown).toContain('### Top Recommended Actions');
    expect(markdown).toContain('## Review Status Summary');
    expect(markdown).toContain('Confirmed intents: 1');
    expect(markdown).toContain('Partial intents: 1');
    expect(markdown).toContain('## Page Intent Validation');
    expect(markdown).toContain('| Overview | executiveOverview | Confirmed |');
    expect(markdown).toContain('## Priority Recommendations');
    expect(markdown).toContain('| Severity | Category | Affected Pages |');
    expect(markdown).toContain('## Cross-Page Consistency Summary');
    expect(markdown).toContain('Naming');
    expect(markdown).toContain('Layout');
    expect(markdown).toContain('## Appendix: Technical Detail');
    expect(markdown).toContain('### Framework Scores');
    expect(markdown).toContain('### Metadata-Derived Findings');
    expect(markdown).toContain('### Deterministic vs Inferred Notes');
    expect(markdown).toContain('Ready for executive playback.');
    expect(markdown).toContain('Needs a clearer variance takeaway.');
    expect(markdown).toContain('Sales and Revenue are used interchangeably.');
    expect(markdown).toContain('Standardize revenue terminology across pages.');
    expect(markdown).toContain('PBIR Design Analyzer');
  });

  it('renders executive profile markdown with a concise summary and without the appendix', () => {
    const markdown = exportReviewWorkflowAsMarkdown(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'executive',
    );

    expect(markdown).toContain('# PBIR Design Analyzer Executive Review Brief');
    expect(markdown).toContain('## Executive Summary');
    expect(markdown).toContain('## Review Status Summary');
    expect(markdown).toContain('## Priority Recommendations');
    expect(markdown).toContain('## Cross-Page Consistency Summary');
    expect(markdown).not.toContain('## Appendix: Technical Detail');
    expect(markdown).not.toContain('### Framework Scores');
  });

  it('renders governance profile markdown with governance emphasis and technical appendix', () => {
    const markdown = exportReviewWorkflowAsMarkdown(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'governance',
    );

    expect(markdown).toContain('# PBIR Design Analyzer Governance Review Packet');
    expect(markdown).toContain('## Governance Summary');
    expect(markdown).toContain('## Cross-Page Consistency Summary');
    expect(markdown).toContain('## Priority Recommendations');
    expect(markdown).toContain('## Appendix: Technical Detail');
    expect(markdown).toContain('### Framework Scores');
    expect(markdown).toContain('### Deterministic vs Inferred Notes');
  });

  it('renders a branded consultant packet when branding metadata is supplied', () => {
    const markdown = exportReviewWorkflowAsMarkdown(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'consultant',
      {
        templateVariant: 'brandedConsultant',
        branding: {
          clientName: 'Contoso Finance',
          reviewerName: 'Northwind BI Advisory',
          engagementName: 'FY26 Executive Dashboard Review',
          confidentiality: 'Client Confidential',
        },
      },
    );

    expect(markdown).toContain('# FY26 Executive Dashboard Review');
    expect(markdown).toContain('> Prepared for: **Contoso Finance**');
    expect(markdown).toContain('> Prepared by: **Northwind BI Advisory**');
    expect(markdown).toContain('> Classification: **Client Confidential**');
    expect(markdown).toContain('## Document Control');
    expect(markdown).toContain('## Executive Summary');
    expect(markdown).toContain('## Appendix: Technical Detail');
  });

  it('renders consultant-style HTML with branded metadata and section structure', () => {
    const html = exportReviewWorkflowAsHtml(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'consultant',
      {
        templateVariant: 'brandedConsultant',
        branding: {
          clientName: 'Contoso Finance',
          reviewerName: 'Northwind BI Advisory',
          engagementName: 'FY26 Executive Dashboard Review',
          confidentiality: 'Client Confidential',
        },
      },
    );

    expect(html).toContain('<!DOCTYPE html>');
    expect(html).toContain('<title>FY26 Executive Dashboard Review</title>');
    expect(html).toContain('Prepared for');
    expect(html).toContain('Contoso Finance');
    expect(html).toContain('Northwind BI Advisory');
    expect(html).toContain('Client Confidential');
    expect(html).toContain('Executive Summary');
    expect(html).toContain('Priority Recommendations');
    expect(html).toContain('Appendix: Technical Detail');
  });

  it('renders executive and governance HTML packets with explicit profile chrome', () => {
    const exportData = buildReviewWorkflowExportData(makeScoreResult(), makeFeedback());

    const executiveHtml = exportReviewWorkflowAsHtml(exportData, 'executive');
    expect(executiveHtml).toContain('Packet Overview');
    expect(executiveHtml).toContain('Review profile');
    expect(executiveHtml).toContain('Executive');
    expect(executiveHtml).toContain('Template');
    expect(executiveHtml).toContain('Standard');

    const governanceHtml = exportReviewWorkflowAsHtml(exportData, 'governance');
    expect(governanceHtml).toContain('Packet Overview');
    expect(governanceHtml).toContain('Governance');
    expect(governanceHtml).toContain('Document Control');
  });

  it('adds print-oriented HTML packet rules that preserve section and table readability', () => {
    const html = exportReviewWorkflowAsHtml(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'consultant',
    );

    expect(html).toContain('@media print');
    expect(html).toContain('thead { display: table-header-group; }');
    expect(html).toContain('tr, .summary-card, .stats-grid > div, .consistency-grid > div, .recommendation-summary > div, .consistency-breakdown > div, .remediation-step, .intent-review-summary > div, .appendix-breakdown > div, .table-wrap, .subsection, .appendix-note { page-break-inside: avoid; }');
  });

  it('adds section-intro language to HTML packets so major sections scan like a review narrative', () => {
    const html = exportReviewWorkflowAsHtml(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'consultant',
    );

    expect(html).toContain('class="section-intro"');
    expect(html).toContain('This summary highlights the current maturity, primary strengths, and the most material risks.');
    expect(html).toContain('Use these recommendations as the prioritized remediation queue for the next review cycle.');
  });

  it('adds a stronger cover-to-body transition and subsection wrappers to consultant HTML packets', () => {
    const html = exportReviewWorkflowAsHtml(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'consultant',
    );

    expect(html).toContain('class="packet-lead"');
    expect(html).toContain('This packet opens with report context before moving into findings, remediation priorities, and technical evidence.');
    expect(html).toContain('class="subsection"');
    expect(html).toContain('class="table-wrap"');
    expect(html).toContain('.packet-lead {');
    expect(html).toContain('.subsection {');
    expect(html).toContain('.table-wrap {');
    expect(html).toContain('tbody tr:nth-child(even)');
  });

  it('adds denser scanability helpers for executive HTML packets in recommendations, consistency, and appendix-adjacent sections', () => {
    const html = exportReviewWorkflowAsHtml(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'executive',
    );

    expect(html).toContain('class="packet-lead"');
    expect(html).toContain('This packet opens with report context before moving into findings, remediation priorities, and technical evidence.');
    expect(html).toContain('class="recommendation-summary"');
    expect(html).toContain('High priority');
    expect(html).toContain('Medium priority');
    expect(html).toContain('class="consistency-breakdown"');
    expect(html).toContain('Metric labels');
    expect(html).toContain('class="category-count"');
    expect(html).toContain('.recommendation-summary, .consistency-breakdown, .remediation-queue, .intent-review-summary, .appendix-breakdown {');
    expect(html).toContain('.consistency-breakdown { grid-template-columns: repeat(2, minmax(0, 1fr)); }');
    expect(html).toContain('.category-count {');
  });

  it('adds appendix evidence framing to governance HTML packets for methodology-heavy review sections', () => {
    const html = exportReviewWorkflowAsHtml(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'governance',
    );

    expect(html).toContain('class="appendix-note"');
    expect(html).toContain('Methodology note');
    expect(html).toContain('These notes distinguish deterministic findings from inference-based interpretation.');
    expect(html).toContain('.appendix-note {');
  });

  it('groups cross-page remediation and appendix evidence more cleanly in consultant HTML packets', () => {
    const html = exportReviewWorkflowAsHtml(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'consultant',
    );

    expect(html).toContain('Report-level priorities');
    expect(html).toContain('class="remediation-queue"');
    expect(html).toContain('class="remediation-step"');
    expect(html).toContain('class="appendix-section-lead"');
    expect(html).toContain('Use this appendix as supporting evidence for framework discussions, remediation planning, and governance follow-up.');
    expect(html).toContain('class="appendix-table-wrap table-wrap"');
    expect(html).toContain('.remediation-queue {');
    expect(html).toContain('.appendix-section-lead {');
  });

  it('adds compact profile framing and table evidence captions to executive and governance HTML packets', () => {
    const exportData = buildReviewWorkflowExportData(makeScoreResult(), makeFeedback());

    const executiveHtml = exportReviewWorkflowAsHtml(exportData, 'executive');
    expect(executiveHtml).toContain('class="profile-note"');
    expect(executiveHtml).toContain('This executive brief intentionally compresses supporting detail so decisions, risks, and next actions scan first.');
    expect(executiveHtml).toContain('class="table-caption">Recommendation evidence table</p>');
    expect(executiveHtml).toContain('.profile-executive .section {');
    expect(executiveHtml).toContain('.profile-executive .packet-lead, .profile-governance .packet-lead {');

    const governanceHtml = exportReviewWorkflowAsHtml(exportData, 'governance');
    expect(governanceHtml).toContain('This governance packet emphasizes controls, consistency, and appendix-backed evidence over page-by-page narrative detail.');
    expect(governanceHtml).toContain('class="table-caption">Framework score evidence table</p>');
    expect(governanceHtml).toContain('.table-caption {');
    expect(governanceHtml).toContain('.profile-governance .section {');
  });

  it('adds consultant intent-validation and governance appendix scan helpers to HTML packets', () => {
    const exportData = buildReviewWorkflowExportData(
      makeScoreResult(),
      makeFeedback([
        {
          pageName: 'Overview',
          inferredIntent: 'executiveOverview',
          storyArchetype: 'executive overview + trend + comparison',
          userConfirmation: 'yes',
        },
        {
          pageName: 'Details',
          inferredIntent: 'analyticalDeepDive',
          storyArchetype: 'trend',
          userConfirmation: 'partial',
        },
      ]),
    );

    const consultantHtml = exportReviewWorkflowAsHtml(exportData, 'consultant');
    expect(consultantHtml).toContain('Intent review breakdown');
    expect(consultantHtml).toContain('class="intent-review-summary"');
    expect(consultantHtml).toContain('class="table-caption">Intent validation evidence table</p>');
    expect(consultantHtml).toContain('.intent-review-summary {');

    const governanceHtml = exportReviewWorkflowAsHtml(exportData, 'governance');
    expect(governanceHtml).toContain('Appendix evidence breakdown');
    expect(governanceHtml).toContain('class="appendix-breakdown"');
    expect(governanceHtml).toContain('.appendix-breakdown {');
  });

  it('produces a deterministic PDF byte stream for the consultant packet', async () => {
    const pdf = await exportReviewWorkflowAsPdf(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'consultant',
      {
        templateVariant: 'brandedConsultant',
        branding: {
          clientName: 'Contoso Finance',
          reviewerName: 'Northwind BI Advisory',
          engagementName: 'FY26 Executive Dashboard Review',
          confidentiality: 'Client Confidential',
        },
      },
    );

    expect(Buffer.isBuffer(pdf)).toBe(true);
    expect(pdf.subarray(0, 5).toString('utf8')).toBe('%PDF-');
    expect(pdf.length).toBeGreaterThan(1024);
  });

  it('includes page-level footer metadata so the consultant packet scans like a paged document', async () => {
    const pdf = await exportReviewWorkflowAsPdf(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'consultant',
      {
        templateVariant: 'brandedConsultant',
        branding: {
          clientName: 'Contoso Finance',
          reviewerName: 'Northwind BI Advisory',
          engagementName: 'FY26 Executive Dashboard Review',
          confidentiality: 'Client Confidential',
        },
      },
    );

    const text = extractPdfText(pdf);
    expect(text).toMatch(/P\s*age 2 of/);
    expect(text).toContain('Contoso Finance');
  });

  it('surfaces profile chrome in executive PDF packets even without a consultant cover page', async () => {
    const pdf = await exportReviewWorkflowAsPdf(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'executive',
    );

    const text = extractPdfText(pdf);
    expect(text).toMatch(/P\s*ac\s*ket O\s*ver\s*vie\s*w/i);
    expect(text).toMatch(/Re\s*vie\s*w pr\s*ofile:\s*Ex\s*ecutiv\s*e/i);
    expect(text).toMatch(/T\s*emplate:\s*Standard/i);
  });

  it('surfaces the selected review profile and template in the exported PDF packet', async () => {
    const pdf = await exportReviewWorkflowAsPdf(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'consultant',
      {
        templateVariant: 'brandedConsultant',
        branding: {
          clientName: 'Contoso Finance',
          reviewerName: 'Northwind BI Advisory',
          engagementName: 'FY26 Executive Dashboard Review',
          confidentiality: 'Client Confidential',
        },
      },
    );

    const text = extractPdfText(pdf);
    expect(text).toMatch(/Re\s*vie\s*w profile:\s*Consultant/i);
    expect(text).toMatch(/T\s*emplate:\s*Br\s*anded consultant/i);
  });

  it('adds readable section-intro language to consultant PDFs before dense findings sections', async () => {
    const pdf = await exportReviewWorkflowAsPdf(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'consultant',
    );

    const text = extractPdfText(pdf);
    expect(text).toMatch(/This summar\s*y highlights the current matur\s*it\s*y/i);
    expect(text).toMatch(/the most mater\s*ial r\s*isks/i);
    expect(text).toMatch(/Use these recommendations as the pr\s*ior\s*itiz\s*ed remediation queue f\s*or the ne\s*xt re\s*vie\s*w c\s*ycle/i);
  });

  it('adds a readable packet-lead transition to consultant PDFs after the cover page', async () => {
    const pdf = await exportReviewWorkflowAsPdf(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'consultant',
    );

    const text = extractPdfText(pdf);
    expect(text).toMatch(/This pa\s*c\s*k\s*et opens with repor\s*t conte\s*x\s*t/i);
    expect(text).toMatch(/mo\s*ving into findings\s*,\s*remediation pr\s*ior\s*ities\s*,\s*and\s*technical e\s*vidence/i);
  });

  it('adds recommendation and consistency breakdown cues to executive PDFs for faster scanning', async () => {
    const pdf = await exportReviewWorkflowAsPdf(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'executive',
    );

    const text = extractPdfText(pdf);
    expect(text).toMatch(/Recommendation breakdo\s*wn/i);
    expect(text).toMatch(/High priority:/i);
    expect(text).toMatch(/Medium priority:/i);
    expect(text).toMatch(/Lo\s*w priority:/i);
    expect(text).toMatch(/Consistenc\s*y breakdo\s*wn/i);
    expect(text).toMatch(/Metric labels:/i);
    expect(text).toMatch(/La\s*y out:/i);
  });

  it('adds methodology framing to governance PDFs so appendix evidence reads as review context instead of leftovers', async () => {
    const pdf = await exportReviewWorkflowAsPdf(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'governance',
    );

    const text = extractPdfText(pdf);
    expect(text).toMatch(/Methodology note/i);
    expect(text).toMatch(/distinguish deter\s*ministic findings from inf\s*erence-based\s*inter\s*pretation/i);
  });

  it('adds report-level remediation grouping, appendix evidence lead, and page-header chrome to consultant PDFs', async () => {
    const pdf = await exportReviewWorkflowAsPdf(
      buildReviewWorkflowExportData(makeScoreResult(), makeFeedback()),
      'consultant',
    );

    const text = extractPdfText(pdf);
    expect(text).toMatch(/Repor\s*t-le\s*vel priorities/i);
    expect(text).toMatch(/Appendix e\s*vidence lead/i);
    expect(text).toMatch(/suppor\s*ting e\s*vidence/i);
    expect(text).toMatch(/P\s*ac\s*k\s*et sec\s*tion/i);
  });

  it('adds compact profile framing and evidence labels to executive and governance PDFs', async () => {
    const exportData = buildReviewWorkflowExportData(makeScoreResult(), makeFeedback());

    const executivePdf = await exportReviewWorkflowAsPdf(exportData, 'executive');
    const executiveText = extractPdfText(executivePdf);
    expect(executiveText).toMatch(/Brie\s*fing f\s*ormat/i);
    expect(executiveText).toMatch(/e\s*x\s*ecutiv\s*e br\s*ief intentionally compresses suppor\s*ting detail/i);
    expect(executiveText).toMatch(/Recommendation e\s*vidence/i);

    const governancePdf = await exportReviewWorkflowAsPdf(exportData, 'governance');
    const governanceText = extractPdfText(governancePdf);
    expect(governanceText).toMatch(/E\s*vidence f\s*ormat/i);
    expect(governanceText).toMatch(/page-b\s*y-page narr\s*ativ\s*e detail/i);
    expect(governanceText).toMatch(/Fr\s*ame\s*w\s*ork score e\s*vidence/i);
  });

  it('adds intent-validation and appendix scan cues to consultant and governance PDFs', async () => {
    const exportData = buildReviewWorkflowExportData(
      makeScoreResult(),
      makeFeedback([
        {
          pageName: 'Overview',
          inferredIntent: 'executiveOverview',
          storyArchetype: 'executive overview + trend + comparison',
          userConfirmation: 'yes',
        },
        {
          pageName: 'Details',
          inferredIntent: 'analyticalDeepDive',
          storyArchetype: 'trend',
          userConfirmation: 'partial',
        },
      ]),
    );

    const consultantPdf = await exportReviewWorkflowAsPdf(exportData, 'consultant');
    const consultantText = extractPdfText(consultantPdf);
    expect(consultantText).toMatch(/Intent re\s*vie\s*w breakdo\s*wn/i);
    expect(consultantText).toMatch(/Confirmed:/i);
    expect(consultantText).toMatch(/P\s*ar\s*tial \/ Need\s*s c\s*larification:/i);
    expect(consultantText).toMatch(/Intent v\s*alidation e\s*vidence/i);

    const governancePdf = await exportReviewWorkflowAsPdf(exportData, 'governance');
    const governanceText = extractPdfText(governancePdf);
    expect(governanceText).toMatch(/Appendix e\s*vidence breakdo\s*wn/i);
    expect(governanceText).toMatch(/Fr\s*ame\s*w\s*ork score s\s*ets:/i);
    expect(governanceText).toMatch(/Methodolog\s*y note\s*s:/i);
  });

  it('forces the appendix onto its own page even for a compact consultant packet', async () => {
    const scoreResult = makeScoreResult({
      recommendations: [],
      reportConsistencySummary: {
        consistentTitleAnchors: true,
        consistentFilterBand: true,
        consistentMetricLabels: true,
        consistentSemanticColors: true,
        overallFinding: 'The report is consistent across pages.',
        affectedPages: [],
        issueCount: 0,
        issues: [],
        findings: [],
      },
      pageScores: [
        {
          pageName: 'Overview',
          gestaltScore: 80,
          cognitiveLoadScore: 78,
          dataInkScore: 77,
          accessibilityScore: 76,
          visualBestPracticesScore: 79,
          stephenFewScore: 78,
          enterpriseGovernanceScore: 82,
          tufteScore: 75,
          graphicalPerceptionScore: 74,
          densityScore: 73,
          narrativeScore: 72,
          compositeScore: 77,
          feedback: {},
          recommendations: [],
          inferredStorySummary: {
            intentProfile: 'executiveOverview',
            storyArchetype: 'overview',
            inferredStory: 'This page appears to summarize performance.',
            confidence: 'high',
            evidence: ['Lead KPI band'],
          },
        },
      ],
    });

    const pdf = await exportReviewWorkflowAsPdf(
      buildReviewWorkflowExportData(scoreResult, makeFeedback()),
      'consultant',
    );

    expect(countPdfPages(pdf)).toBeGreaterThanOrEqual(3);
  });

  it('keeps larger recommendation sections readable across multiple pages', async () => {
    const scoreResult = makeScoreResult({
      recommendations: Array.from({ length: 12 }, (_, index) => `[Medium] Recommendation ${index + 1}`),
      pageScores: Array.from({ length: 8 }, (_, index) => ({
        pageName: `Page ${index + 1}`,
        gestaltScore: 80,
        cognitiveLoadScore: 78,
        dataInkScore: 77,
        accessibilityScore: 76,
        visualBestPracticesScore: 79,
        stephenFewScore: 78,
        enterpriseGovernanceScore: 82,
        tufteScore: 75,
        graphicalPerceptionScore: 74,
        densityScore: 73,
        narrativeScore: 72,
        compositeScore: 77,
        feedback: {},
        recommendations: [],
        inferredStorySummary: {
          intentProfile: 'analyticalDeepDive',
          storyArchetype: 'comparison',
          inferredStory: `This page appears to explain variance ${index + 1}.`,
          confidence: 'medium',
          evidence: ['Lead comparison visual'],
        },
      })),
    });

    const pdf = await exportReviewWorkflowAsPdf(
      buildReviewWorkflowExportData(scoreResult, makeFeedback()),
      'consultant',
    );

    expect(countPdfPages(pdf)).toBeGreaterThanOrEqual(3);
  });
});
