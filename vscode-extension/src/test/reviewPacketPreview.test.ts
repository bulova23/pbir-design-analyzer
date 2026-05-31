import {
  buildDeterministicPreviewBranding,
  buildReviewPacketPreviewHtml,
  defaultReviewPacketPreviewOptions,
  normalizeReviewPacketPreviewOptions,
} from '../analyzer/score/reviewPacketPreview';
import { buildReviewWorkflowExportData } from '../analyzer/score/reviewWorkflowExport';
import type { ScoreResult } from '../analyzer/contracts/scorePanel';

function makeScoreResult(): ScoreResult {
  return {
    reportPath: '/workspace/FY26 Sales.Report',
    scoredAt: '2026-05-28T16:00:00.000Z',
    compositeScore: 78,
    gestaltScore: 78,
    cognitiveLoadScore: 75,
    dataInkScore: 77,
    accessibilityScore: 74,
    visualBestPracticesScore: 79,
    stephenFewScore: 76,
    enterpriseGovernanceScore: 80,
    tufteScore: 73,
    graphicalPerceptionScore: 72,
    densityScore: 70,
    narrativeScore: 78,
    feedback: {},
    pageCount: 2,
    recommendations: ['[High] Clarify the lead KPI takeaway.'],
    pageScores: [
      {
        pageName: 'Overview',
        gestaltScore: 80,
        cognitiveLoadScore: 77,
        dataInkScore: 78,
        accessibilityScore: 75,
        visualBestPracticesScore: 81,
        stephenFewScore: 79,
        enterpriseGovernanceScore: 82,
        tufteScore: 74,
        graphicalPerceptionScore: 73,
        densityScore: 71,
        narrativeScore: 80,
        compositeScore: 79,
        feedback: {},
        recommendations: [],
        inferredStorySummary: {
          intentProfile: 'executiveOverview',
          storyArchetype: 'executive overview + comparison',
          inferredStory: 'This page appears to summarize revenue performance.',
          confidence: 'high',
          evidence: ['Lead KPI band'],
        },
      },
      {
        pageName: 'Details',
        gestaltScore: 76,
        cognitiveLoadScore: 73,
        dataInkScore: 75,
        accessibilityScore: 73,
        visualBestPracticesScore: 77,
        stephenFewScore: 74,
        enterpriseGovernanceScore: 78,
        tufteScore: 72,
        graphicalPerceptionScore: 71,
        densityScore: 69,
        narrativeScore: 75,
        compositeScore: 75,
        feedback: {},
        recommendations: [],
        inferredStorySummary: {
          intentProfile: 'analyticalDeepDive',
          storyArchetype: 'trend',
          inferredStory: 'This page appears to analyze variance over time.',
          confidence: 'medium',
          evidence: ['Trend line'],
        },
      },
    ],
  };
}

describe('reviewPacketPreview', () => {
  it('defaults to branded consultant preview options', () => {
    expect(defaultReviewPacketPreviewOptions).toEqual({
      profile: 'consultant',
      templateVariant: 'brandedConsultant',
    });
    expect(normalizeReviewPacketPreviewOptions()).toEqual(defaultReviewPacketPreviewOptions);
  });

  it('forces standard template for non-consultant preview profiles', () => {
    expect(
      normalizeReviewPacketPreviewOptions({
        profile: 'executive',
        templateVariant: 'brandedConsultant',
      }),
    ).toEqual({
      profile: 'executive',
      templateVariant: 'standard',
    });
  });

  it('builds deterministic preview branding from the report name', () => {
    expect(buildDeterministicPreviewBranding('/workspace/FY26 Sales.Report')).toEqual({
      clientName: 'FY26 Sales',
      reviewerName: 'PBIR Design Analyzer',
      engagementName: 'FY26 Sales Review Packet Preview',
      confidentiality: 'Internal preview',
    });
  });

  it('renders branded consultant preview html from the shared packet model', () => {
    const exportData = buildReviewWorkflowExportData(makeScoreResult(), []);

    const html = buildReviewPacketPreviewHtml(exportData, '/workspace/FY26 Sales.Report');

    expect(html).toContain('FY26 Sales Review Packet Preview');
    expect(html).toContain('Prepared for');
    expect(html).toContain('PBIR Design Analyzer');
  });

  it('renders executive preview html without consultant branding controls', () => {
    const exportData = buildReviewWorkflowExportData(makeScoreResult(), []);

    const html = buildReviewPacketPreviewHtml(exportData, '/workspace/FY26 Sales.Report', {
      profile: 'executive',
      templateVariant: 'brandedConsultant',
    });

    expect(html).toContain('Executive Summary');
    expect(html).not.toContain('Internal preview');
    expect(html).not.toContain('Prepared for');
  });
});
