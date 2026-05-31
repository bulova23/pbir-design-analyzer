import type { PageScore } from '../../contracts/scorePanel';

export function buildVisualAuditContextBlock(pageName: string, pageScore: PageScore | undefined): string {
  const lines = [`Page name: ${pageName}`];

  if (!pageScore) {
    lines.push('No page-score context was available for this screenshot.');
    lines.push('Distinguish rendered/layout issues from metadata/model issues whenever possible.');
    return lines.join('\n');
  }

  lines.push(`PBIR composite score: ${pageScore.compositeScore.toFixed(1)}`);

  if (pageScore.pageIntentProfile) {
    lines.push(`Page intent profile: ${pageScore.pageIntentProfile.inferredProfile}`);
    lines.push(`Actionability expectation: ${pageScore.pageIntentProfile.actionabilityExpectation}`);
  }

  if (pageScore.inferredStorySummary) {
    lines.push(`Story archetype: ${pageScore.inferredStorySummary.storyArchetype}`);
    lines.push(`Inferred story: ${pageScore.inferredStorySummary.inferredStory}`);
  }

  if (pageScore.actionabilityBreakdown) {
    lines.push(`Actionability score: ${pageScore.actionabilityBreakdown.score.toFixed(1)}`);
    lines.push(`Actionability summary: ${pageScore.actionabilityBreakdown.summary}`);
    if (pageScore.actionabilityBreakdown.gaps.length > 0) {
      lines.push(`Actionability gaps: ${pageScore.actionabilityBreakdown.gaps.join(' | ')}`);
    }
  }

  if (pageScore.benchmarkComparison) {
    lines.push(`Benchmark comparison: ${pageScore.benchmarkComparison.archetype}`);
    lines.push(`Benchmark insight: ${pageScore.benchmarkComparison.insight}`);
  }

  if (pageScore.visualMetadata?.chartIntentSummary) {
    const summary = pageScore.visualMetadata.chartIntentSummary;
    lines.push(`Lead chart intent: ${summary.intent}`);
    if (summary.fitStatus) {
      lines.push(`Chart fit status: ${summary.fitStatus}`);
    }
  }

  lines.push('Distinguish rendered/layout issues from metadata/model issues. Use `renderedLayout` for visual/rendering defects and `metadataModel` for issues rooted in titles, chart choice, semantics, or missing decision context.');
  return lines.join('\n');
}
