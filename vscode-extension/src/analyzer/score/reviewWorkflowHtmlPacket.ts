import type {
  ReviewWorkflowExportData,
  ReviewWorkflowExportProfile,
  ReviewWorkflowMarkdownRenderOptions,
  ReviewWorkflowStatus,
} from '../contracts/scorePanel';

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
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

function renderList(items: string[]): string {
  return `<ul>${items.map((item) => `<li>${escapeHtml(item)}</li>`).join('')}</ul>`;
}

function renderRemediationQueue(items: string[]): string {
  return `
    <div class="remediation-queue">
      ${items.map((item, index) => `
        <div class="remediation-step">
          <span class="category-count">${index + 1}</span>
          <div><strong>Priority ${index + 1}</strong><p>${escapeHtml(item)}</p></div>
        </div>
      `).join('')}
    </div>
  `;
}

function countBySeverity(
  data: ReviewWorkflowExportData,
): Array<{ label: string; count: number }> {
  const counts = new Map<string, number>([
    ['high', 0],
    ['medium', 0],
    ['low', 0],
  ]);

  for (const recommendation of data.priorityRecommendations) {
    const key = recommendation.severity.toLowerCase();
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }

  return [
    { label: 'High priority', count: counts.get('high') ?? 0 },
    { label: 'Medium priority', count: counts.get('medium') ?? 0 },
    { label: 'Low priority', count: counts.get('low') ?? 0 },
  ];
}

function countByReviewStatus(
  data: ReviewWorkflowExportData,
): Array<{ label: string; count: number }> {
  return [
    { label: 'Confirmed', count: data.reviewSummary.confirmedPages },
    { label: 'Partial / Needs clarification', count: data.reviewSummary.partialPages },
    { label: 'Mismatch / Needs review', count: data.reviewSummary.mismatchPages },
    { label: 'Not reviewed', count: data.reviewSummary.unreviewedPages },
  ];
}

function packetLead(): string {
  return 'This packet opens with report context before moving into findings, remediation priorities, and technical evidence.';
}

function sectionIntro(title: string): string | null {
  switch (title) {
    case 'Executive Summary':
      return 'This summary highlights the current maturity, primary strengths, and the most material risks.';
    case 'Review Status Summary':
      return 'Use this section to gauge review coverage before treating the packet as a final client-ready assessment.';
    case 'Page Intent Validation':
      return 'This section shows whether the inferred page story aligns with reviewer-confirmed intent or still needs clarification.';
    case 'Priority Recommendations':
      return 'Use these recommendations as the prioritized remediation queue for the next review cycle.';
    case 'Cross-Page Consistency Summary':
      return 'These findings indicate whether the report behaves like one coherent product across naming, layout, and semantics.';
    case 'Appendix: Technical Detail':
      return 'The appendix preserves framework-level evidence and methodology notes for technical review and governance follow-up.';
    default:
      return null;
  }
}

function renderSection(title: string, body: string): string {
  const intro = sectionIntro(title);
  return `<section class="section"><h2>${escapeHtml(title)}</h2>${intro ? `<p class="section-intro">${escapeHtml(intro)}</p>` : ''}${body}</section>`;
}

function profileLabel(profile: ReviewWorkflowExportProfile): string {
  switch (profile) {
    case 'executive':
      return 'Executive';
    case 'governance':
      return 'Governance';
    default:
      return 'Consultant';
  }
}

function profileNote(profile: ReviewWorkflowExportProfile): string | null {
  switch (profile) {
    case 'executive':
      return 'This executive brief intentionally compresses supporting detail so decisions, risks, and next actions scan first.';
    case 'governance':
      return 'This governance packet emphasizes controls, consistency, and appendix-backed evidence over page-by-page narrative detail.';
    default:
      return null;
  }
}

function templateLabel(options: ReviewWorkflowMarkdownRenderOptions): string {
  return options.templateVariant === 'brandedConsultant'
    ? 'Branded consultant'
    : 'Standard';
}

function renderPacketOverview(
  data: ReviewWorkflowExportData,
  profile: ReviewWorkflowExportProfile,
  options: ReviewWorkflowMarkdownRenderOptions,
): string {
  return renderSection('Packet Overview', `
    <div class="overview-strip">
      <div><span class="overview-label">Review profile</span><strong>${escapeHtml(profileLabel(profile))}</strong></div>
      <div><span class="overview-label">Template</span><strong>${escapeHtml(templateLabel(options))}</strong></div>
      <div><span class="overview-label">Packet version</span><strong>${escapeHtml(data.analyzerMetadata.packetVersion)}</strong></div>
      <div><span class="overview-label">Report</span><strong>${escapeHtml(data.reportPath)}</strong></div>
    </div>
  `);
}

function renderPacketMetadata(
  data: ReviewWorkflowExportData,
  profile: ReviewWorkflowExportProfile,
  options: ReviewWorkflowMarkdownRenderOptions,
): string {
  return renderSection('Document Control', `
    <dl class="meta-grid">
      <div><dt>Analyzer</dt><dd>${escapeHtml(data.analyzerMetadata.analyzerName)} ${escapeHtml(data.analyzerMetadata.analyzerVersion)}</dd></div>
      <div><dt>Packet version</dt><dd>${escapeHtml(data.analyzerMetadata.packetVersion)}</dd></div>
      <div><dt>Review profile</dt><dd>${escapeHtml(profileLabel(profile))}</dd></div>
      <div><dt>Template</dt><dd>${escapeHtml(templateLabel(options))}</dd></div>
      <div><dt>Source report</dt><dd>${escapeHtml(data.reportPath)}</dd></div>
      <div><dt>Scored</dt><dd>${escapeHtml(data.scoredAt)}</dd></div>
      <div><dt>Exported</dt><dd>${escapeHtml(data.exportedAt)}</dd></div>
    </dl>
  `);
}

function renderExecutiveSummary(data: ReviewWorkflowExportData): string {
  return renderSection('Executive Summary', `
    <div class="summary-card">
      <p><strong>Overall score:</strong> ${data.compositeScore} / 100</p>
      <p><strong>Overall status:</strong> ${escapeHtml(data.executiveSummary.overallStatus)}</p>
      <p class="headline">${escapeHtml(data.executiveSummary.headline)}</p>
      <p>${escapeHtml(data.executiveSummary.maturityStatement)}</p>
    </div>
    <div class="two-col">
      <div class="subsection">
        <h3>Top Strengths</h3>
        ${renderList(data.executiveSummary.topStrengths)}
      </div>
      <div class="subsection">
        <h3>Top Risks</h3>
        ${renderList(data.executiveSummary.topRisks)}
      </div>
    </div>
    <div class="subsection">
      <h3>Top Recommended Actions</h3>
      ${renderList(data.executiveSummary.topRecommendedActions)}
    </div>
  `);
}

function renderReviewStatusSummary(data: ReviewWorkflowExportData): string {
  return renderSection('Review Status Summary', `
    <div class="stats-grid">
      <div><span class="stat-value">${data.reviewSummary.totalPages}</span><span class="stat-label">Total pages</span></div>
      <div><span class="stat-value">${data.reviewSummary.reviewedPages}</span><span class="stat-label">Pages reviewed</span></div>
      <div><span class="stat-value">${data.reviewSummary.confirmedPages}</span><span class="stat-label">Confirmed intents</span></div>
      <div><span class="stat-value">${data.reviewSummary.partialPages}</span><span class="stat-label">Partial intents</span></div>
      <div><span class="stat-value">${data.reviewSummary.mismatchPages}</span><span class="stat-label">Mismatches</span></div>
      <div><span class="stat-value">${data.reviewSummary.unreviewedPages}</span><span class="stat-label">Unreviewed pages</span></div>
    </div>
  `);
}

function renderPageIntentValidation(data: ReviewWorkflowExportData): string {
  const statusSummary = countByReviewStatus(data)
    .map((item) => `<div><span class="category-count">${item.count}</span><span>${escapeHtml(item.label)}</span></div>`)
    .join('');
  const rows = data.pages.map((page) => `
    <tr>
      <td>${escapeHtml(page.pageName)}</td>
      <td>${escapeHtml(page.inferredIntent ?? '—')}</td>
      <td>${escapeHtml(statusLabel(page.reviewStatus))}</td>
      <td>${escapeHtml(page.reviewerNote ?? page.inferredStory ?? '—')}</td>
    </tr>
  `).join('');

  return renderSection('Page Intent Validation', `
    <div class="subsection">
      <h3>Intent review breakdown</h3>
      <div class="intent-review-summary">
        ${statusSummary}
      </div>
    </div>
    <p class="table-caption">Intent validation evidence table</p>
    <div class="table-wrap">
      <table>
        <thead>
          <tr><th>Page</th><th>Inferred Intent</th><th>Confirmation Status</th><th>Remediation Note</th></tr>
        </thead>
        <tbody>${rows}</tbody>
      </table>
    </div>
  `);
}

function renderPriorityRecommendations(data: ReviewWorkflowExportData): string {
  const severitySummary = countBySeverity(data)
    .map((item) => `<div><span class="category-count">${item.count}</span><span>${escapeHtml(item.label)}</span></div>`)
    .join('');
  const rows = data.priorityRecommendations.map((recommendation) => `
    <tr>
      <td>${escapeHtml(recommendation.severity)}</td>
      <td>${escapeHtml(recommendation.issueCategory)}</td>
      <td>${escapeHtml(recommendation.affectedPages.join(', ') || 'Report-wide')}</td>
      <td>${escapeHtml(recommendation.remediationGuidance)}</td>
    </tr>
  `).join('');

  return renderSection('Priority Recommendations', `
    <div class="recommendation-summary">
      ${severitySummary}
    </div>
    <p class="table-caption">Recommendation evidence table</p>
    <div class="table-wrap">
      <table>
        <thead>
          <tr><th>Severity</th><th>Category</th><th>Affected Pages</th><th>Remediation Guidance</th></tr>
        </thead>
        <tbody>${rows}</tbody>
      </table>
    </div>
  `);
}

function renderCrossPageConsistencySummary(data: ReviewWorkflowExportData): string {
  const rollup = data.crossPageConsistencyRollup;
  const issueBreakdown = (rollup?.issuesByCategory ?? [])
    .map(([category, count]) => `<div><strong>${escapeHtml(category)}</strong><span><span class="category-count">${count}</span> issue group(s)</span></div>`)
    .join('');
  const namingCount = rollup?.issuesByCategory.find(([category]) => category.toLowerCase().includes('metric'))?.[1] ?? 0;
  const layoutCount = rollup?.issuesByCategory.find(([category]) => category.toLowerCase().includes('layout'))?.[1] ?? 0;
  const semanticColorCount = rollup?.issuesByCategory.find(([category]) => category.toLowerCase().includes('semantic'))?.[1] ?? 0;
  const navigationCount = rollup?.issuesByCategory.find(([category]) => category.toLowerCase().includes('navigation'))?.[1] ?? 0;

  return renderSection('Cross-Page Consistency Summary', `
    <div class="consistency-grid">
      <div><strong>Naming</strong><span>${namingCount} issue group(s)</span></div>
      <div><strong>Layout</strong><span>${layoutCount} issue group(s)</span></div>
      <div><strong>Semantic color</strong><span>${semanticColorCount} issue group(s)</span></div>
      <div><strong>Navigation/story flow</strong><span>${navigationCount} issue group(s)</span></div>
    </div>
    ${issueBreakdown ? `<div class="subsection"><h3>Consistency breakdown</h3><div class="consistency-breakdown">${issueBreakdown}</div></div>` : ''}
    ${rollup?.overallFinding ? `<p class="headline">${escapeHtml(rollup.overallFinding)}</p>` : ''}
    ${rollup?.remediation.length ? `<div class="subsection"><h3>Report-level priorities</h3>${renderRemediationQueue(rollup.remediation)}</div>` : ''}
  `);
}

function renderAppendix(data: ReviewWorkflowExportData): string {
  const frameworkRows = data.appendix.frameworkScores.map((framework) => `
    <tr><td>${escapeHtml(framework.framework)}</td><td>${Math.round(framework.score)}</td></tr>
  `).join('');
  const appendixBreakdown = [
    { label: 'Framework score sets', count: data.appendix.frameworkScores.length },
    { label: 'Metadata findings', count: data.appendix.metadataDerivedFindings.length },
    { label: 'Methodology notes', count: data.appendix.methodologyNotes.length },
  ].map((item) => `<div><span class="category-count">${item.count}</span><span>${escapeHtml(item.label)}</span></div>`)
    .join('');

  return renderSection('Appendix: Technical Detail', `
    <div class="subsection">
      <p class="appendix-section-lead">Use this appendix as supporting evidence for framework discussions, remediation planning, and governance follow-up.</p>
      <h3>Appendix evidence breakdown</h3>
      <div class="appendix-breakdown">
        ${appendixBreakdown}
      </div>
    </div>
    <div class="subsection">
      <h3>Framework Scores</h3>
      <p class="table-caption">Framework score evidence table</p>
      <div class="appendix-table-wrap table-wrap">
        <table>
          <thead><tr><th>Framework</th><th>Score</th></tr></thead>
          <tbody>${frameworkRows}</tbody>
        </table>
      </div>
    </div>
    <div class="subsection">
      <h3>Metadata-Derived Findings</h3>
      ${renderList(data.appendix.metadataDerivedFindings)}
    </div>
    <div class="subsection">
      <h3>Deterministic vs Inferred Notes</h3>
      <div class="appendix-note">
        <strong>Methodology note</strong>
        <p>These notes distinguish deterministic findings from inference-based interpretation.</p>
      </div>
      ${renderList(data.appendix.methodologyNotes)}
    </div>
  `);
}

function renderConsultantBody(data: ReviewWorkflowExportData): string {
  return [
    renderExecutiveSummary(data),
    renderReviewStatusSummary(data),
    renderPageIntentValidation(data),
    renderPriorityRecommendations(data),
    renderCrossPageConsistencySummary(data),
    renderAppendix(data),
  ].join('');
}

function renderCover(data: ReviewWorkflowExportData, options: ReviewWorkflowMarkdownRenderOptions): string {
  const engagementName = options.branding?.engagementName ?? 'PBIR Design Analyzer Client Review Packet';
  const clientName = options.branding?.clientName ?? 'Client organization';
  const reviewerName = options.branding?.reviewerName ?? data.analyzerMetadata.analyzerName;
  const confidentiality = options.branding?.confidentiality ?? 'Confidential';

  return `
    <section class="cover">
      <p class="eyebrow">PBIR Design Analyzer Review Packet</p>
      <h1>${escapeHtml(engagementName)}</h1>
      <p class="cover-line"><strong>Prepared for:</strong> ${escapeHtml(clientName)}</p>
      <p class="cover-line"><strong>Prepared by:</strong> ${escapeHtml(reviewerName)}</p>
      <p class="cover-line"><strong>Classification:</strong> ${escapeHtml(confidentiality)}</p>
      <p class="cover-line"><strong>Export date:</strong> ${escapeHtml(data.exportedAt)}</p>
    </section>
  `;
}

function renderProfileBody(
  data: ReviewWorkflowExportData,
  profile: ReviewWorkflowExportProfile,
  options: ReviewWorkflowMarkdownRenderOptions,
): string {
  if (profile === 'executive') {
    const note = profileNote(profile);
    return [
      `<p class="packet-lead">${escapeHtml(packetLead())}</p>`,
      note ? `<p class="profile-note">${escapeHtml(note)}</p>` : '',
      renderPacketOverview(data, profile, options),
      renderPacketMetadata(data, profile, options),
      renderExecutiveSummary(data),
      renderReviewStatusSummary(data),
      renderPriorityRecommendations(data),
      renderCrossPageConsistencySummary(data),
    ].join('');
  }

  if (profile === 'governance') {
    const note = profileNote(profile);
    return [
      `<p class="packet-lead">${escapeHtml(packetLead())}</p>`,
      note ? `<p class="profile-note">${escapeHtml(note)}</p>` : '',
      renderPacketOverview(data, profile, options),
      renderPacketMetadata(data, profile, options),
      renderSection('Governance Summary', `
        <p><strong>Overall score:</strong> ${data.compositeScore} / 100</p>
        <p><strong>Review status:</strong> ${escapeHtml(data.executiveSummary.overallStatus)}</p>
        <p>${escapeHtml(data.crossPageConsistencyRollup?.overallFinding ?? 'No report-level consistency finding recorded.')}</p>
      `),
      renderReviewStatusSummary(data),
      renderPriorityRecommendations(data),
      renderCrossPageConsistencySummary(data),
      renderAppendix(data),
    ].join('');
  }

  const cover = options.templateVariant === 'brandedConsultant'
    ? renderCover(data, options)
    : `
      <section class="cover standard-cover">
        <p class="eyebrow">PBIR Design Analyzer</p>
        <h1>Consultant Review Packet</h1>
        <p class="cover-line">${escapeHtml(data.reportPath)}</p>
      </section>
    `;

  return `${cover}<p class="packet-lead">${escapeHtml(packetLead())}</p>${renderPacketOverview(data, profile, options)}${renderPacketMetadata(data, profile, options)}${renderConsultantBody(data)}`;
}

export function renderReviewWorkflowPacketHtml(
  data: ReviewWorkflowExportData,
  profile: ReviewWorkflowExportProfile = 'consultant',
  options: ReviewWorkflowMarkdownRenderOptions = {},
): string {
  const title = options.branding?.engagementName
    ?? (profile === 'executive'
      ? 'PBIR Design Analyzer Executive Review Brief'
      : profile === 'governance'
        ? 'PBIR Design Analyzer Governance Review Packet'
        : 'PBIR Design Analyzer Consultant Review Packet');

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>${escapeHtml(title)}</title>
  <style>
    :root { color-scheme: light; }
    body { font-family: Georgia, "Times New Roman", serif; margin: 0; background: #f4f1eb; color: #1e2430; }
    main { max-width: 960px; margin: 0 auto; padding: 32px 40px 64px; background: #fffdf9; }
    h1, h2, h3 { color: #13233a; }
    h1 { font-size: 32px; margin-bottom: 12px; }
    h2 { font-size: 22px; margin: 0 0 16px; padding-bottom: 8px; border-bottom: 2px solid #d9c8a3; }
    h3 { font-size: 16px; margin: 0 0 8px; }
    p, li, td, th, dd, dt { font-size: 14px; line-height: 1.55; }
    .cover { padding: 64px 0 48px; border-bottom: 4px solid #c89d4d; margin-bottom: 32px; }
    .standard-cover { padding-top: 32px; }
    .eyebrow { text-transform: uppercase; letter-spacing: 0.16em; font-size: 12px; color: #8b6e2f; }
    .cover-line { margin: 6px 0; }
    .packet-lead { margin: 0 0 28px; padding: 16px 18px; background: linear-gradient(90deg, rgba(201, 157, 77, 0.14), rgba(201, 157, 77, 0)); border-left: 3px solid #c89d4d; color: #304968; font-size: 15px; line-height: 1.65; }
    .profile-note { margin: -8px 0 20px; padding: 10px 14px; background: #f9f6ef; border: 1px solid #e3d8bf; color: #304968; font-size: 13px; line-height: 1.6; }
    .headline { font-size: 16px; color: #304968; }
    .section { margin-bottom: 32px; }
    .section + .section { margin-top: 10px; }
    .section-intro { margin: -2px 0 16px; color: #4b5970; font-size: 14px; line-height: 1.6; max-width: 760px; }
    .summary-card { padding: 18px 20px; background: #f7f2e8; border-left: 4px solid #c89d4d; }
    .two-col { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
    .subsection { margin-top: 18px; padding-top: 4px; }
    .overview-strip { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
    .overview-strip > div { padding: 14px 16px; background: #13233a; color: #fffdf9; border: 1px solid #d9c8a3; }
    .overview-label { display: block; margin-bottom: 6px; font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; color: #e7d6ae; }
    .stats-grid, .consistency-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
    .stats-grid > div, .consistency-grid > div { padding: 14px 16px; background: #f7f2e8; border: 1px solid #e3d8bf; }
    .recommendation-summary, .consistency-breakdown, .remediation-queue, .intent-review-summary, .appendix-breakdown { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; margin-bottom: 14px; }
    .recommendation-summary > div, .consistency-breakdown > div, .remediation-step, .intent-review-summary > div, .appendix-breakdown > div { padding: 12px 14px; background: #fcf8ef; border: 1px solid #e3d8bf; }
    .consistency-breakdown { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    .remediation-queue { grid-template-columns: 1fr; }
    .intent-review-summary { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    .appendix-breakdown { grid-template-columns: repeat(3, minmax(0, 1fr)); }
    .remediation-step { display: flex; gap: 12px; align-items: flex-start; }
    .remediation-step p { margin: 4px 0 0; }
    .category-count { display: inline-block; min-width: 26px; margin-right: 8px; padding: 2px 7px; border-radius: 999px; background: #13233a; color: #fffdf9; font-size: 12px; font-weight: 700; text-align: center; }
    .stat-value { display: block; font-size: 24px; font-weight: 700; color: #13233a; }
    .stat-label { display: block; margin-top: 4px; color: #4b5970; }
    .table-wrap { margin-top: 10px; border: 1px solid #e3d8bf; background: #fffdfa; overflow: hidden; }
    .table-caption { margin: 0 0 8px; color: #5a6780; font-size: 12px; letter-spacing: 0.04em; text-transform: uppercase; }
    .appendix-table-wrap { margin-top: 12px; }
    .appendix-section-lead { margin: 0 0 12px; color: #4b5970; font-size: 14px; line-height: 1.6; }
    .appendix-note { margin: 0 0 12px; padding: 12px 14px; background: #f7f2e8; border-left: 3px solid #c89d4d; color: #304968; }
    .appendix-note p { margin: 6px 0 0; }
    .profile-executive .section { margin-bottom: 24px; }
    .profile-governance .section { margin-bottom: 26px; }
    .profile-executive .packet-lead, .profile-governance .packet-lead { margin-bottom: 20px; padding: 14px 16px; }
    .profile-executive .meta-grid div, .profile-governance .meta-grid div { padding: 10px 12px; }
    .profile-governance .appendix-table-wrap { margin-top: 8px; }
    table { width: 100%; border-collapse: collapse; margin-top: 0; }
    th, td { border: 1px solid #d8d1c4; padding: 8px 10px; text-align: left; vertical-align: top; }
    th { background: #f1ebdf; }
    tbody tr:nth-child(even) { background: #fbf8f1; }
    .meta-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px 18px; margin: 0; }
    .meta-grid div { background: #f9f6ef; padding: 12px 14px; border: 1px solid #e3d8bf; }
    .meta-grid dt { font-weight: 700; color: #304968; }
    .meta-grid dd { margin: 4px 0 0; }
    @media print {
      body { background: #fff; }
      main { max-width: none; padding: 0; background: #fff; }
      .cover { min-height: 90vh; display: flex; flex-direction: column; justify-content: center; }
      .packet-lead { break-after: avoid; }
      .section { page-break-inside: avoid; }
      .section-intro { page-break-after: avoid; }
      thead { display: table-header-group; }
      tr, .summary-card, .stats-grid > div, .consistency-grid > div, .recommendation-summary > div, .consistency-breakdown > div, .remediation-step, .intent-review-summary > div, .appendix-breakdown > div, .table-wrap, .subsection, .appendix-note { page-break-inside: avoid; }
    }
  </style>
</head>
<body class="profile-${escapeHtml(profile)}">
  <main class="profile-${escapeHtml(profile)}">
    ${renderProfileBody(data, profile, options)}
  </main>
</body>
</html>`;
}
