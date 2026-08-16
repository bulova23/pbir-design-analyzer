import type {
  ReviewWorkflowExportData,
  ReviewWorkflowMarkdownRenderOptions,
  ReviewWorkflowExportProfile,
  ReviewWorkflowStatus,
} from '../contracts/scorePanel';

function pushSection(lines: string[], title: string): void {
  lines.push(title);
  lines.push('');
}

function formatList(lines: string[], items: string[]): void {
  for (const item of items) {
    lines.push(`- ${item}`);
  }
  lines.push('');
}

function statusLabel(status: ReviewWorkflowStatus): string {
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

function renderPacketMetadata(lines: string[], data: ReviewWorkflowExportData): void {
  pushSection(lines, '## Packet Metadata');
  lines.push(`- Analyzer: ${data.analyzerMetadata.analyzerName}`);
  lines.push(`- Analyzer version: ${data.analyzerMetadata.analyzerVersion}`);
  lines.push(`- Packet version: ${data.analyzerMetadata.packetVersion}`);
  lines.push(`- Report: \`${data.reportPath}\``);
  lines.push(`- Scored: ${data.scoredAt}`);
  lines.push(`- Exported: ${data.exportedAt}`);
  lines.push('');
}

function renderExecutiveSummary(lines: string[], data: ReviewWorkflowExportData): void {
  pushSection(lines, '## Executive Summary');
  lines.push(`- Overall score: ${data.compositeScore} / 100`);
  lines.push(`- Overall status: ${data.executiveSummary.overallStatus}`);
  lines.push(`- Headline: ${data.executiveSummary.headline}`);
  lines.push('');
  pushSection(lines, '### Maturity and Readiness');
  lines.push(data.executiveSummary.maturityStatement);
  lines.push('');
  pushSection(lines, '### Top Strengths');
  formatList(lines, data.executiveSummary.topStrengths);
  pushSection(lines, '### Top Risks');
  formatList(lines, data.executiveSummary.topRisks);
  pushSection(lines, '### Top Recommended Actions');
  formatList(lines, data.executiveSummary.topRecommendedActions);
}

function renderReviewStatusSummary(lines: string[], data: ReviewWorkflowExportData): void {
  pushSection(lines, '## Review Status Summary');
  lines.push(`- Total pages: ${data.reviewSummary.totalPages}`);
  lines.push(`- Pages reviewed: ${data.reviewSummary.reviewedPages}`);
  lines.push(`- Confirmed intents: ${data.reviewSummary.confirmedPages}`);
  lines.push(`- Partial intents: ${data.reviewSummary.partialPages}`);
  lines.push(`- Mismatches: ${data.reviewSummary.mismatchPages}`);
  lines.push(`- Unreviewed pages: ${data.reviewSummary.unreviewedPages}`);
  lines.push('');
}

function renderPageIntentValidation(lines: string[], data: ReviewWorkflowExportData): void {
  pushSection(lines, '## Page Intent Validation');
  lines.push('| Page | Inferred Intent | Confirmation Status | Remediation Note |');
  lines.push('|------|------------------|---------------------|------------------|');
  for (const page of data.pages) {
    lines.push(
      `| ${page.pageName} | ${page.inferredIntent ?? '—'} | ${statusLabel(page.reviewStatus)} | ${page.reviewerNote ?? page.inferredStory ?? '—'} |`,
    );
  }
  lines.push('');
}

function renderPriorityRecommendations(lines: string[], data: ReviewWorkflowExportData): void {
  pushSection(lines, '## Priority Recommendations');
  lines.push('| Severity | Category | Affected Pages | Remediation Guidance |');
  lines.push('|----------|----------|----------------|----------------------|');
  for (const recommendation of data.priorityRecommendations) {
    lines.push(
      `| ${recommendation.severity} | ${recommendation.issueCategory} | ${recommendation.affectedPages.join(', ') || 'Report-wide'} | ${recommendation.remediationGuidance} |`,
    );
  }
  lines.push('');
}

function renderRenderedReview(lines: string[], data: ReviewWorkflowExportData): void {
  const review = data.renderedReview;
  if (!review) return;
  pushSection(lines, '## Rendered Review');
  lines.push('PBI Lens provides rendered observation. PBIR Design Analyzer remains authoritative for design judgment and scoring.');
  lines.push('');
  lines.push('| Category | Pages | Status | Reviewer note | Screenshots |');
  lines.push('|----------|-------|--------|---------------|-------------|');
  for (const item of review.checklist) {
    lines.push(`| ${item.label} | ${item.pageNames.join(', ') || 'Report'} | ${item.status} | ${item.reviewerNote ?? '—'} | ${item.screenshotEvidence?.length ?? 0} |`);
  }
  lines.push('');
  lines.push('### Evidence classification');
  lines.push('');
  lines.push('- Deterministic: analyzer-derived metadata and rule results.');
  lines.push('- Semantic: analyzer-derived model and usage evidence.');
  lines.push('- Rendered: user-supplied screenshot records from the rendered review workflow.');
  lines.push('- Reviewer Notes: human observations kept separate from analyzer findings.');
  lines.push('');
}

function renderCrossPageConsistencySummary(lines: string[], data: ReviewWorkflowExportData): void {
  pushSection(lines, '## Cross-Page Consistency Summary');
  lines.push(`- Naming: ${data.crossPageConsistencyRollup?.issuesByCategory.find(([category]) => category.toLowerCase().includes('metric'))?.[1] ?? 0} issue group(s)`);
  lines.push(`- Layout: ${data.crossPageConsistencyRollup?.issuesByCategory.find(([category]) => category.toLowerCase().includes('layout'))?.[1] ?? 0} issue group(s)`);
  lines.push(`- Semantic color: ${data.crossPageConsistencyRollup?.issuesByCategory.find(([category]) => category.toLowerCase().includes('semantic'))?.[1] ?? 0} issue group(s)`);
  lines.push(`- Navigation/story flow: ${data.crossPageConsistencyRollup?.issuesByCategory.find(([category]) => category.toLowerCase().includes('navigation'))?.[1] ?? 0} issue group(s)`);
  if (data.crossPageConsistencyRollup?.overallFinding) {
    lines.push(`- Overall finding: ${data.crossPageConsistencyRollup.overallFinding}`);
  }
  lines.push('');
  if (data.crossPageConsistencyRollup?.remediation.length) {
    formatList(lines, data.crossPageConsistencyRollup.remediation);
  }
}

function renderAppendix(lines: string[], data: ReviewWorkflowExportData): void {
  pushSection(lines, '## Appendix: Technical Detail');
  pushSection(lines, '### Framework Scores');
  lines.push('| Framework | Score |');
  lines.push('|-----------|-------|');
  for (const framework of data.appendix.frameworkScores) {
    lines.push(`| ${framework.framework} | ${Math.round(framework.score)} |`);
  }
  lines.push('');

  pushSection(lines, '### Metadata-Derived Findings');
  formatList(lines, data.appendix.metadataDerivedFindings);
  pushSection(lines, '### Deterministic vs Inferred Notes');
  formatList(lines, data.appendix.methodologyNotes);
}

function renderConsultantPacket(lines: string[], data: ReviewWorkflowExportData): void {
  lines.push('# PBIR Design Analyzer Consultant Review Packet');
  lines.push('');
  renderPacketMetadata(lines, data);
  renderExecutiveSummary(lines, data);
  renderReviewStatusSummary(lines, data);
  renderPageIntentValidation(lines, data);
  renderPriorityRecommendations(lines, data);
  renderRenderedReview(lines, data);
  renderCrossPageConsistencySummary(lines, data);
  renderAppendix(lines, data);
}

function renderBrandedConsultantPacket(
  lines: string[],
  data: ReviewWorkflowExportData,
  options: ReviewWorkflowMarkdownRenderOptions,
): void {
  const engagementName = options.branding?.engagementName ?? 'PBIR Design Analyzer Client Review Packet';
  const clientName = options.branding?.clientName ?? 'Client organization';
  const reviewerName = options.branding?.reviewerName ?? data.analyzerMetadata.analyzerName;
  const confidentiality = options.branding?.confidentiality ?? 'Confidential';

  lines.push(`# ${engagementName}`);
  lines.push('');
  lines.push(`> Prepared for: **${clientName}**`);
  lines.push(`> Prepared by: **${reviewerName}**`);
  lines.push(`> Classification: **${confidentiality}**`);
  lines.push(`> Export date: **${data.exportedAt}**`);
  lines.push('');
  lines.push('---');
  lines.push('');
  pushSection(lines, '## Document Control');
  lines.push(`- Analyzer: ${data.analyzerMetadata.analyzerName} ${data.analyzerMetadata.analyzerVersion}`);
  lines.push(`- Packet version: ${data.analyzerMetadata.packetVersion}`);
  lines.push(`- Source report: \`${data.reportPath}\``);
  lines.push(`- Scored: ${data.scoredAt}`);
  lines.push(`- Exported: ${data.exportedAt}`);
  lines.push('');
  renderExecutiveSummary(lines, data);
  renderReviewStatusSummary(lines, data);
  renderPageIntentValidation(lines, data);
  renderPriorityRecommendations(lines, data);
  renderCrossPageConsistencySummary(lines, data);
  renderAppendix(lines, data);
}

function renderExecutivePacket(lines: string[], data: ReviewWorkflowExportData): void {
  lines.push('# PBIR Design Analyzer Executive Review Brief');
  lines.push('');
  renderPacketMetadata(lines, data);
  renderExecutiveSummary(lines, data);
  renderReviewStatusSummary(lines, data);
  renderPriorityRecommendations(lines, data);
  renderCrossPageConsistencySummary(lines, data);
}

function renderGovernancePacket(lines: string[], data: ReviewWorkflowExportData): void {
  lines.push('# PBIR Design Analyzer Governance Review Packet');
  lines.push('');
  renderPacketMetadata(lines, data);
  pushSection(lines, '## Governance Summary');
  lines.push(`- Overall score: ${data.compositeScore} / 100`);
  lines.push(`- Review status: ${data.executiveSummary.overallStatus}`);
  lines.push(`- Naming and consistency finding: ${data.crossPageConsistencyRollup?.overallFinding ?? 'No report-level consistency finding recorded.'}`);
  lines.push('');
  renderReviewStatusSummary(lines, data);
  renderPriorityRecommendations(lines, data);
  renderCrossPageConsistencySummary(lines, data);
  renderAppendix(lines, data);
}

export function renderConsultantReviewPacketMarkdown(
  data: ReviewWorkflowExportData,
  profile: ReviewWorkflowExportProfile = 'consultant',
  options: ReviewWorkflowMarkdownRenderOptions = {},
): string {
  const lines: string[] = [];

  switch (profile) {
    case 'executive':
      renderExecutivePacket(lines, data);
      break;
    case 'governance':
      renderGovernancePacket(lines, data);
      break;
    default:
      if (options.templateVariant === 'brandedConsultant') {
        renderBrandedConsultantPacket(lines, data, options);
      } else {
        renderConsultantPacket(lines, data);
      }
      break;
  }

  return lines.join('\n');
}
