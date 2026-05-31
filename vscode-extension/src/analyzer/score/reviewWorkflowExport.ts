import type {
  ReviewWorkflowAnalyzerMetadata,
  ReviewWorkflowAppendix,
  ReviewWorkflowPriorityRecommendation,
  IntentFeedbackConfirmation,
  IntentFeedbackEntry,
  ReviewWorkflowMarkdownRenderOptions,
  PageScore,
  ReviewWorkflowCrossPageConsistencyRollup,
  ReviewWorkflowExecutiveSummary,
  ReviewWorkflowExportData,
  ReviewWorkflowExportProfile,
  ReviewWorkflowExportPage,
  ReviewWorkflowExportSummary,
  ReviewWorkflowIntentValidationSummary,
  ReviewWorkflowRemediationItem,
  ReviewWorkflowStatus,
  ScoreResult,
} from '../contracts/scorePanel';
import { renderReviewWorkflowPacketHtml } from './reviewWorkflowHtmlPacket';
import { renderConsultantReviewPacketMarkdown } from './reviewWorkflowMarkdownPacket';
import { renderReviewWorkflowPacketPdf } from './reviewWorkflowPdfPacket';

type StoryCarrier = Pick<PageScore, 'pageName' | 'inferredStorySummary'>;

function buildFeedbackKey(
  pageName: string,
  inferredIntent: string,
  storyArchetype?: string,
): string {
  return `${pageName}:${inferredIntent}:${storyArchetype ?? 'unknown'}`;
}

function confirmationToStatus(
  confirmation: IntentFeedbackConfirmation | undefined,
): ReviewWorkflowStatus {
  switch (confirmation) {
    case 'yes':
      return 'confirmed';
    case 'partial':
      return 'partial';
    case 'no':
      return 'mismatch';
    default:
      return 'unreviewed';
  }
}

export function statusLabel(status: ReviewWorkflowStatus): string {
  switch (status) {
    case 'confirmed':
      return 'Confirmed';
    case 'partial':
      return 'Partial / Needs clarification';
    case 'mismatch':
      return 'Mismatch / Needs review';
    default:
      return 'Not reviewed';
  }
}

function buildStoryPages(result: ScoreResult): StoryCarrier[] {
  if (result.pageScores && result.pageScores.length > 0) {
    return result.pageScores.map((page) => ({
      pageName: page.pageName,
      inferredStorySummary: page.inferredStorySummary,
    }));
  }

  if (result.inferredStorySummary || result.scoredPageName) {
    return [{
      pageName: result.scoredPageName ?? 'Report',
      inferredStorySummary: result.inferredStorySummary,
    }];
  }

  return [];
}

function removePriorityPrefix(recommendation: string): string {
  return recommendation.replace(/^\[(High|Medium|Low)\]\s*/i, '').trim();
}

function getRecommendationSeverity(recommendation: string): 'high' | 'medium' | 'low' {
  if (/^\[high\]/i.test(recommendation)) return 'high';
  if (/^\[low\]/i.test(recommendation)) return 'low';
  return 'medium';
}

function frameworkScoreEntries(result: ScoreResult): Array<{ framework: string; score: number }> {
  const entries: Array<{ framework: string; score: number }> = [
    { framework: 'Enterprise Governance', score: result.enterpriseGovernanceScore },
    { framework: 'Narrative', score: result.narrativeScore },
    { framework: 'Gestalt', score: result.gestaltScore },
    { framework: 'Visual Best Practices', score: result.visualBestPracticesScore },
    { framework: 'Data-Ink Ratio', score: result.dataInkScore },
    { framework: 'Tufte', score: result.tufteScore },
    { framework: 'Accessibility', score: result.accessibilityScore },
    { framework: 'Cognitive Load', score: result.cognitiveLoadScore },
    { framework: 'Graphical Perception', score: result.graphicalPerceptionScore },
    { framework: 'Density', score: result.densityScore },
    { framework: 'Stephen Few', score: result.stephenFewScore },
  ];

  return entries.sort((a, b) => b.score - a.score);
}

function buildExecutiveSummary(
  summary: ReviewWorkflowExportSummary,
  result: ScoreResult,
  topRecommendations: string[],
  crossPageConsistencyRollup: ReviewWorkflowCrossPageConsistencyRollup | undefined,
  remediationQueue: ReviewWorkflowRemediationItem[],
): ReviewWorkflowExecutiveSummary {
  const reviewCoveragePercent = summary.totalPages > 0
    ? Math.round((summary.reviewedPages / summary.totalPages) * 100)
    : 0;
  const strengths = frameworkScoreEntries(result)
    .slice(0, 3)
    .map((entry) => `${entry.framework} is currently scoring ${Math.round(entry.score)} / 100.`);
  const risks = [
    ...(remediationQueue.slice(0, 2).map((item) => `${item.pageName}: ${item.reason}`)),
    ...(crossPageConsistencyRollup?.overallFinding ? [crossPageConsistencyRollup.overallFinding] : []),
  ].slice(0, 3);
  const maturityStatement = result.compositeScore >= 85
    ? 'Executive-ready foundation with relatively low design risk.'
    : result.compositeScore >= 70
      ? 'Solid foundation with targeted review and cleanup still required before broad distribution.'
      : 'Early-stage design maturity; substantial remediation is recommended before sharing broadly.';
  const topRecommendedActions = topRecommendations.slice(0, 3);

  if (summary.mismatchPages > 0 || summary.partialPages > 0) {
    return {
      overallStatus: 'Needs review',
      headline: 'Some pages still need intent validation before the report is review-ready.',
      reviewCoveragePercent,
      maturityStatement,
      topStrengths: strengths,
      topRisks: risks,
      topRecommendedActions,
    };
  }

  if (summary.unreviewedPages > 0) {
    return {
      overallStatus: 'In progress',
      headline: 'Intent validation is underway, but some pages are still unreviewed.',
      reviewCoveragePercent,
      maturityStatement,
      topStrengths: strengths,
      topRisks: risks,
      topRecommendedActions,
    };
  }

  return {
    overallStatus: 'Ready for export',
    headline: 'All reviewed pages are aligned with the current inferred intent.',
    reviewCoveragePercent,
    maturityStatement,
    topStrengths: strengths,
    topRisks: risks,
    topRecommendedActions,
  };
}

function buildIntentValidationSummary(
  pages: ReviewWorkflowExportPage[],
): ReviewWorkflowIntentValidationSummary {
  const confirmedPages = pages.filter((page) => page.reviewStatus === 'confirmed');
  const partialPages = pages.filter((page) => page.reviewStatus === 'partial');
  const mismatchPages = pages.filter((page) => page.reviewStatus === 'mismatch');
  const unreviewedPages = pages.filter((page) => page.reviewStatus === 'unreviewed');

  return {
    confirmedPages,
    partialPages,
    mismatchPages,
    unreviewedPages,
    pagesNeedingReview: [...mismatchPages, ...partialPages],
  };
}

function buildRemediationQueue(
  pages: ReviewWorkflowExportPage[],
): ReviewWorkflowRemediationItem[] {
  return pages
    .filter(
      (page): page is ReviewWorkflowExportPage & { reviewStatus: 'partial' | 'mismatch' } =>
        page.reviewStatus === 'partial' || page.reviewStatus === 'mismatch',
    )
    .map((page) => ({
      pageName: page.pageName,
      reviewStatus: page.reviewStatus,
      reason: page.reviewerNote ?? page.inferredStory ?? 'Intent alignment needs review.',
      suggestedAction: page.reviewStatus === 'mismatch'
        ? 'Review the page title, lead KPI band, and supporting visuals so the intended story reads clearly.'
        : 'Clarify the intended takeaway with tighter titles, KPI context, or supporting visual evidence.',
    }));
}

function buildCrossPageConsistencyRollup(
  result: ScoreResult,
): ReviewWorkflowCrossPageConsistencyRollup | undefined {
  const summary = result.reportConsistencySummary;
  if (!summary) {
    return undefined;
  }

  const categoryCounts = new Map<string, number>();
  for (const issue of summary.issues) {
    categoryCounts.set(issue.category, (categoryCounts.get(issue.category) ?? 0) + 1);
  }

  const severityRank: Record<'high' | 'medium' | 'low', number> = { high: 3, medium: 2, low: 1 };
  const highestSeverity = summary.issues.reduce<'high' | 'medium' | 'low' | undefined>((highest, issue) => {
    if (!highest || severityRank[issue.severity] > severityRank[highest]) {
      return issue.severity;
    }
    return highest;
  }, undefined);

  return {
    overallFinding: summary.overallFinding,
    issueCount: summary.issueCount,
    affectedPages: summary.affectedPages,
    issuesByCategory: Array.from(categoryCounts.entries()).sort((a, b) => a[0].localeCompare(b[0])),
    highestSeverity,
    remediation: Array.from(new Set(summary.issues.map((issue) => issue.recommendedRemediation))),
  };
}

function buildPriorityRecommendations(
  result: ScoreResult,
  remediationQueue: ReviewWorkflowRemediationItem[],
): ReviewWorkflowPriorityRecommendation[] {
  const issueRecommendations = (result.reportConsistencySummary?.issues ?? []).map((issue) => ({
    title: issue.overallFinding,
    severity: issue.severity,
    affectedPages: issue.affectedPages,
    issueCategory: issue.category,
    remediationGuidance: issue.recommendedRemediation,
  }));

  const intentRecommendations = remediationQueue.map((item) => ({
    title: item.reason,
    severity: item.reviewStatus === 'mismatch' ? 'high' as const : 'medium' as const,
    affectedPages: [item.pageName],
    issueCategory: 'Intent validation',
    remediationGuidance: item.suggestedAction,
  }));

  const genericRecommendations = (result.recommendations ?? []).map((recommendation) => ({
    title: removePriorityPrefix(recommendation),
    severity: getRecommendationSeverity(recommendation),
    affectedPages: [],
    issueCategory: 'General recommendations',
    remediationGuidance: removePriorityPrefix(recommendation),
  }));

  const combined = [...issueRecommendations, ...intentRecommendations, ...genericRecommendations];
  const unique = new Map<string, ReviewWorkflowPriorityRecommendation>();
  for (const recommendation of combined) {
    const key = `${recommendation.issueCategory}:${recommendation.title}:${recommendation.affectedPages.join(',')}`;
    if (!unique.has(key)) {
      unique.set(key, recommendation);
    }
  }

  const severityRank: Record<'high' | 'medium' | 'low', number> = { high: 3, medium: 2, low: 1 };
  return Array.from(unique.values())
    .sort((a, b) => severityRank[b.severity] - severityRank[a.severity])
    .slice(0, 8);
}

function buildAppendix(result: ScoreResult): ReviewWorkflowAppendix {
  const pagesWithMetadata = result.pageScores?.filter((page) => page.visualMetadata) ?? [];
  const metadataFindings: string[] = [];

  if (pagesWithMetadata.length > 0) {
    metadataFindings.push(
      `${pagesWithMetadata.length} page(s) exposed parsed visual metadata for layout, labels, or semantic color analysis.`,
    );
    for (const page of pagesWithMetadata.slice(0, 3)) {
      const metadata = page.visualMetadata!;
      if (metadata.chartIntentSummary) {
        metadataFindings.push(
          `${page.pageName}: page intent reads as ${metadata.chartIntentSummary.intent} with ${metadata.chartIntentSummary.confidence ?? 'unknown'} confidence.`,
        );
      }
    }
  }

  for (const finding of result.reportConsistencySummary?.findings ?? []) {
    metadataFindings.push(finding);
  }

  return {
    frameworkScores: frameworkScoreEntries(result),
    metadataDerivedFindings: Array.from(new Set(metadataFindings)).slice(0, 8),
    methodologyNotes: [
      'Framework scores are deterministic scoring outputs from the current analyzer run.',
      'Intent confirmations are reviewer feedback and do not change scores.',
      'Story summaries and chart intents combine deterministic rules with inferred design signals.',
    ],
  };
}

function buildAnalyzerMetadata(intentFeedback: IntentFeedbackEntry[]): ReviewWorkflowAnalyzerMetadata {
  const latestVersion = [...intentFeedback]
    .map((entry) => entry.analyzerVersion)
    .filter((value): value is string => Boolean(value))
    .at(-1);

  return {
    analyzerName: 'PBIR Design Analyzer',
    analyzerVersion: latestVersion ?? 'unknown',
    packetVersion: 'consultant-review-v1',
  };
}

export function buildReviewWorkflowExportData(
  result: ScoreResult,
  intentFeedback: IntentFeedbackEntry[],
  exportedAt = new Date().toISOString(),
): ReviewWorkflowExportData {
  const feedbackLookup = new Map<string, IntentFeedbackEntry>();
  const feedbackByPageName = new Map<string, IntentFeedbackEntry>();
  for (const entry of intentFeedback) {
    feedbackLookup.set(
      buildFeedbackKey(entry.pageName, entry.inferredIntent, entry.storyArchetype),
      entry,
    );
    feedbackByPageName.set(entry.pageName, entry);
  }

  const pages = buildStoryPages(result).map((page) => {
    const summary = page.inferredStorySummary;
    if (!summary) {
      const feedback = feedbackByPageName.get(page.pageName);
      return {
        pageName: page.pageName,
        reviewStatus: confirmationToStatus(feedback?.userConfirmation),
        inferredIntent: feedback?.inferredIntent ?? 'unknown',
        storyArchetype: feedback?.storyArchetype,
        inferenceConfidence: feedback?.inferenceConfidence,
        reviewerNote: feedback?.note,
        reviewedAt: feedback?.timestamp,
        analyzerVersion: feedback?.analyzerVersion,
      };
    }

    const feedback = feedbackLookup.get(
      buildFeedbackKey(page.pageName, summary.intentProfile, summary.storyArchetype),
    );

    return {
      pageName: page.pageName,
      reviewStatus: confirmationToStatus(feedback?.userConfirmation),
      inferredIntent: summary.intentProfile,
      storyArchetype: summary.storyArchetype,
      inferredStory: summary.inferredStory,
      inferenceConfidence: summary.confidence,
      reviewerNote: feedback?.note,
      reviewedAt: feedback?.timestamp,
      analyzerVersion: feedback?.analyzerVersion,
    };
  });

  const summary: ReviewWorkflowExportSummary = {
    totalPages: pages.length,
    reviewedPages: pages.filter((page) => page.reviewStatus !== 'unreviewed').length,
    confirmedPages: pages.filter((page) => page.reviewStatus === 'confirmed').length,
    partialPages: pages.filter((page) => page.reviewStatus === 'partial').length,
    mismatchPages: pages.filter((page) => page.reviewStatus === 'mismatch').length,
    unreviewedPages: pages.filter((page) => page.reviewStatus === 'unreviewed').length,
  };

  const topRecommendations = Array.from(
    new Set((result.recommendations ?? []).map(removePriorityPrefix).filter(Boolean)),
  ).slice(0, 5);
  const crossPageConsistencyRollup = buildCrossPageConsistencyRollup(result);
  const intentValidationSummary = buildIntentValidationSummary(pages);
  const remediationQueue = buildRemediationQueue(pages);
  const executiveSummary = buildExecutiveSummary(
    summary,
    result,
    topRecommendations,
    crossPageConsistencyRollup,
    remediationQueue,
  );
  const priorityRecommendations = buildPriorityRecommendations(result, remediationQueue);
  const appendix = buildAppendix(result);
  const analyzerMetadata = buildAnalyzerMetadata(intentFeedback);

  return {
    reportPath: result.reportPath,
    scoredAt: result.scoredAt,
    exportedAt,
    compositeScore: Math.round(result.compositeScore * 10) / 10,
    pageCount: result.pageCount,
    analyzerMetadata,
    reviewSummary: summary,
    executiveSummary,
    intentValidationSummary,
    remediationQueue,
    topRecommendations,
    priorityRecommendations,
    crossPageConsistencyRollup,
    appendix,
    pages,
    crossPageConsistency: result.reportConsistencySummary
      ? {
        overallFinding: result.reportConsistencySummary.overallFinding,
        issueCount: result.reportConsistencySummary.issueCount,
        affectedPages: result.reportConsistencySummary.affectedPages,
        findings: result.reportConsistencySummary.findings,
      }
      : undefined,
  };
}

export function exportReviewWorkflowAsJson(data: ReviewWorkflowExportData): string {
  return JSON.stringify(data, null, 2);
}

export function exportReviewWorkflowAsMarkdown(
  data: ReviewWorkflowExportData,
  profile: ReviewWorkflowExportProfile = 'consultant',
  options: ReviewWorkflowMarkdownRenderOptions = {},
): string {
  return renderConsultantReviewPacketMarkdown(data, profile, options);
}

export function exportReviewWorkflowAsHtml(
  data: ReviewWorkflowExportData,
  profile: ReviewWorkflowExportProfile = 'consultant',
  options: ReviewWorkflowMarkdownRenderOptions = {},
): string {
  return renderReviewWorkflowPacketHtml(data, profile, options);
}

export async function exportReviewWorkflowAsPdf(
  data: ReviewWorkflowExportData,
  profile: ReviewWorkflowExportProfile = 'consultant',
  options: ReviewWorkflowMarkdownRenderOptions = {},
): Promise<Buffer> {
  return renderReviewWorkflowPacketPdf(data, profile, options);
}
