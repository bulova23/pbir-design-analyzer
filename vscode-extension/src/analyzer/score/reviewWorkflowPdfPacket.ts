import PDFDocument from 'pdfkit';
import type {
  ReviewWorkflowExportData,
  ReviewWorkflowExportProfile,
  ReviewWorkflowMarkdownRenderOptions,
  ReviewWorkflowStatus,
} from '../contracts/scorePanel';

const PAGE_MARGIN = 56;
const FOOTER_RESERVE = 28;

function packetLead(): string {
  return 'This packet opens with report context before moving into findings, remediation priorities, and technical evidence.';
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

function templateLabel(options: ReviewWorkflowMarkdownRenderOptions): string {
  return options.templateVariant === 'brandedConsultant'
    ? 'Branded consultant'
    : 'Standard';
}

function profileNote(profile: ReviewWorkflowExportProfile): { label: string; body: string } | null {
  switch (profile) {
    case 'executive':
      return {
        label: 'Briefing format',
        body: 'This executive brief intentionally compresses supporting detail so decisions, risks, and next actions scan first.',
      };
    case 'governance':
      return {
        label: 'Evidence format',
        body: 'This governance packet emphasizes controls, consistency, and appendix-backed evidence over page-by-page narrative detail.',
      };
    default:
      return null;
  }
}

function contentBottom(doc: PDFKit.PDFDocument): number {
  return doc.page.height - doc.page.margins.bottom - FOOTER_RESERVE;
}

function ensureSpace(doc: PDFKit.PDFDocument, requiredHeight: number): void {
  if (doc.y + requiredHeight <= contentBottom(doc)) {
    return;
  }

  doc.addPage();
}

function startSection(doc: PDFKit.PDFDocument, title: string, requiredHeight = 96): void {
  ensureSpace(doc, requiredHeight);
  if (doc.y > doc.page.margins.top + 12) {
    doc.moveDown(0.6);
  }

  doc.font('Helvetica-Bold').fontSize(15).fillColor('#13233a').text(title, {
    width: doc.page.width - (PAGE_MARGIN * 2),
  });
  const lineY = doc.y + 3;
  doc.moveTo(PAGE_MARGIN, lineY)
    .lineTo(doc.page.width - PAGE_MARGIN, lineY)
    .lineWidth(1)
    .strokeColor('#d9c8a3')
    .stroke();
  doc.moveDown(0.75);
}

function startFreshPage(doc: PDFKit.PDFDocument): void {
  if (doc.y > doc.page.margins.top + 8) {
    doc.addPage();
  }
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

function addSectionHeading(doc: PDFKit.PDFDocument, title: string): void {
  startSection(doc, title);
  const intro = sectionIntro(title);
  if (!intro) {
    return;
  }

  ensureSpace(doc, 34);
  doc.font('Helvetica-Oblique').fontSize(10.5).fillColor('#4b5970').text(intro, {
    width: doc.page.width - (PAGE_MARGIN * 2),
    paragraphGap: 8,
  });
  doc.font('Helvetica').fillColor('#1e2430');
}

function addBulletList(doc: PDFKit.PDFDocument, items: string[]): void {
  doc.font('Helvetica').fontSize(11).fillColor('#1e2430');
  for (const item of items) {
    ensureSpace(doc, 24);
    doc.text(`• ${item}`, { indent: 12, paragraphGap: 4 });
  }
}

function addNumberedPriorityList(doc: PDFKit.PDFDocument, items: string[]): void {
  for (const [index, item] of items.entries()) {
    ensureSpace(doc, 44);
    const cardY = doc.y;
    doc.roundedRect(PAGE_MARGIN, cardY - 2, doc.page.width - (PAGE_MARGIN * 2), 36, 6)
      .lineWidth(0.75)
      .strokeColor('#d9c8a3')
      .stroke();
    doc.circle(PAGE_MARGIN + 16, cardY + 14, 10)
      .fillAndStroke('#13233a', '#13233a');
    doc.fillColor('#fffdf9')
      .font('Helvetica-Bold')
      .fontSize(10)
      .text(String(index + 1), PAGE_MARGIN + 12, cardY + 8, {
        width: 8,
        align: 'center',
      });
    doc.fillColor('#1e2430')
      .font('Helvetica-Bold')
      .fontSize(10.5)
      .text(`Priority ${index + 1}`, PAGE_MARGIN + 34, cardY + 4);
    doc.font('Helvetica')
      .fontSize(10.5)
      .text(item, PAGE_MARGIN + 34, cardY + 17, {
        width: doc.page.width - (PAGE_MARGIN * 2) - 46,
      });
    doc.moveDown(1.8);
  }
}

function addSubsectionHeading(doc: PDFKit.PDFDocument, title: string): void {
  ensureSpace(doc, 26);
  doc.moveDown(0.2);
  doc.font('Helvetica-Bold').fontSize(11.5).fillColor('#304968').text(title);
  doc.moveDown(0.2);
  doc.font('Helvetica').fontSize(11).fillColor('#1e2430');
}

function addSubsectionNote(doc: PDFKit.PDFDocument, label: string, body: string): void {
  ensureSpace(doc, 42);
  const noteY = doc.y;
  doc.roundedRect(PAGE_MARGIN, noteY, doc.page.width - (PAGE_MARGIN * 2), 34, 5)
    .fillAndStroke('#f7f2e8', '#d9c8a3');
  doc.fillColor('#304968').font('Helvetica-Bold').fontSize(10.5).text(label, PAGE_MARGIN + 10, noteY + 8);
  doc.font('Helvetica').fontSize(10.5).text(body, PAGE_MARGIN + 120, noteY + 8, {
    width: doc.page.width - (PAGE_MARGIN * 2) - 130,
  });
  doc.moveDown(2);
  doc.font('Helvetica').fontSize(11).fillColor('#1e2430');
}

function addKeyValue(doc: PDFKit.PDFDocument, label: string, value: string): void {
  ensureSpace(doc, 20);
  doc.font('Helvetica-Bold').text(`${label}: `, { continued: true });
  doc.font('Helvetica').text(value);
}

function addCoverPage(
  doc: PDFKit.PDFDocument,
  data: ReviewWorkflowExportData,
  profile: ReviewWorkflowExportProfile,
  options: ReviewWorkflowMarkdownRenderOptions,
): void {
  const engagementName = options.branding?.engagementName ?? 'PBIR Design Analyzer Client Review Packet';
  const clientName = options.branding?.clientName ?? 'Client organization';
  const reviewerName = options.branding?.reviewerName ?? data.analyzerMetadata.analyzerName;
  const confidentiality = options.branding?.confidentiality ?? 'Confidential';

  doc.rect(48, 48, doc.page.width - 96, doc.page.height - 96).lineWidth(2).strokeColor('#c89d4d').stroke();
  doc.moveTo(72, 132).lineTo(doc.page.width - 72, 132).lineWidth(1).strokeColor('#d9c8a3').stroke();
  doc.font('Helvetica-Bold').fontSize(14).fillColor('#8b6e2f').text('PBIR DESIGN ANALYZER REVIEW PACKET', 72, 96);
  doc.font('Helvetica-Bold').fontSize(28).fillColor('#13233a').text(engagementName, 72, 160, {
    width: doc.page.width - 144,
  });
  doc.font('Helvetica').fontSize(13).fillColor('#1e2430');
  doc.text(`Prepared for: ${clientName}`, 72, 280);
  doc.text(`Prepared by: ${reviewerName}`, 72, 304);
  doc.text(`Classification: ${confidentiality}`, 72, 328);
  doc.text(`Review profile: ${profileLabel(profile)}`, 72, 352);
  doc.text(`Template: ${templateLabel(options)}`, 72, 376);
  doc.text(`Export date: ${data.exportedAt}`, 72, 400);
  doc.moveTo(72, 432).lineTo(doc.page.width - 72, 432).lineWidth(1).strokeColor('#d9c8a3').stroke();
  doc.font('Helvetica').fontSize(10).fillColor('#6f5a2a').text(
    `${data.analyzerMetadata.analyzerName} ${data.analyzerMetadata.analyzerVersion}`,
    72,
    448,
  );
}

function addPacketMetadata(
  doc: PDFKit.PDFDocument,
  data: ReviewWorkflowExportData,
  profile: ReviewWorkflowExportProfile,
  options: ReviewWorkflowMarkdownRenderOptions,
): void {
  addSectionHeading(doc, 'Document Control');
  addKeyValue(doc, 'Analyzer', `${data.analyzerMetadata.analyzerName} ${data.analyzerMetadata.analyzerVersion}`);
  addKeyValue(doc, 'Packet version', data.analyzerMetadata.packetVersion);
  addKeyValue(doc, 'Review profile', profileLabel(profile));
  addKeyValue(doc, 'Template', templateLabel(options));
  addKeyValue(doc, 'Source report', data.reportPath);
  addKeyValue(doc, 'Scored', data.scoredAt);
  addKeyValue(doc, 'Exported', data.exportedAt);
}

function addPacketOverview(
  doc: PDFKit.PDFDocument,
  data: ReviewWorkflowExportData,
  profile: ReviewWorkflowExportProfile,
  options: ReviewWorkflowMarkdownRenderOptions,
): void {
  addSectionHeading(doc, 'Packet Overview');
  addKeyValue(doc, 'Review profile', profileLabel(profile));
  addKeyValue(doc, 'Template', templateLabel(options));
  addKeyValue(doc, 'Packet version', data.analyzerMetadata.packetVersion);
  addKeyValue(doc, 'Report', data.reportPath);
}

function addPacketLead(doc: PDFKit.PDFDocument): void {
  ensureSpace(doc, 42);
  doc.roundedRect(PAGE_MARGIN, doc.y, doc.page.width - (PAGE_MARGIN * 2), 36, 6)
    .fillAndStroke('#f7f2e8', '#d9c8a3');
  doc.fillColor('#304968')
    .font('Helvetica-Oblique')
    .fontSize(11)
    .text(packetLead(), PAGE_MARGIN + 12, doc.y + 10, {
      width: doc.page.width - (PAGE_MARGIN * 2) - 24,
    });
  doc.moveDown(2.1);
  doc.font('Helvetica').fillColor('#1e2430');
}

function addProfileNote(doc: PDFKit.PDFDocument, profile: ReviewWorkflowExportProfile): void {
  const note = profileNote(profile);
  if (!note) {
    return;
  }

  addSubsectionNote(doc, note.label, note.body);
}

function addExecutiveSummary(doc: PDFKit.PDFDocument, data: ReviewWorkflowExportData): void {
  addSectionHeading(doc, 'Executive Summary');
  addKeyValue(doc, 'Overall score', `${data.compositeScore} / 100`);
  addKeyValue(doc, 'Overall status', data.executiveSummary.overallStatus);
  doc.moveDown(0.4);
  doc.font('Helvetica').fontSize(11).text(data.executiveSummary.headline);
  doc.moveDown(0.4);
  doc.text(data.executiveSummary.maturityStatement);
  doc.moveDown();
  addSubsectionHeading(doc, 'Top Strengths');
  addBulletList(doc, data.executiveSummary.topStrengths);
  addSubsectionHeading(doc, 'Top Risks');
  addBulletList(doc, data.executiveSummary.topRisks);
  addSubsectionHeading(doc, 'Top Recommended Actions');
  addBulletList(doc, data.executiveSummary.topRecommendedActions);
}

function addReviewStatusSummary(doc: PDFKit.PDFDocument, data: ReviewWorkflowExportData): void {
  addSectionHeading(doc, 'Review Status Summary');
  addKeyValue(doc, 'Total pages', String(data.reviewSummary.totalPages));
  addKeyValue(doc, 'Pages reviewed', String(data.reviewSummary.reviewedPages));
  addKeyValue(doc, 'Confirmed intents', String(data.reviewSummary.confirmedPages));
  addKeyValue(doc, 'Partial intents', String(data.reviewSummary.partialPages));
  addKeyValue(doc, 'Mismatches', String(data.reviewSummary.mismatchPages));
  addKeyValue(doc, 'Unreviewed pages', String(data.reviewSummary.unreviewedPages));
}

function addPageIntentValidation(doc: PDFKit.PDFDocument, data: ReviewWorkflowExportData): void {
  addSectionHeading(doc, 'Page Intent Validation');
  addSubsectionHeading(doc, 'Intent review breakdown');
  addKeyValue(doc, 'Confirmed', String(data.reviewSummary.confirmedPages));
  addKeyValue(doc, 'Partial / Needs clarification', String(data.reviewSummary.partialPages));
  addKeyValue(doc, 'Mismatch / Needs review', String(data.reviewSummary.mismatchPages));
  addKeyValue(doc, 'Not reviewed', String(data.reviewSummary.unreviewedPages));
  addSubsectionNote(
    doc,
    'Intent validation evidence',
    'Page cards below preserve the inferred intent, confirmation status, and current remediation note for each page.',
  );
  doc.font('Helvetica').fontSize(11).fillColor('#1e2430');
  for (const page of data.pages) {
    ensureSpace(doc, 62);
    doc.roundedRect(PAGE_MARGIN, doc.y - 4, doc.page.width - (PAGE_MARGIN * 2), 54, 6)
      .lineWidth(0.75)
      .strokeColor('#d9c8a3')
      .stroke();
    doc.font('Helvetica-Bold').text(page.pageName);
    doc.font('Helvetica').text(`Intent: ${page.inferredIntent ?? '—'}`);
    doc.text(`Status: ${statusLabel(page.reviewStatus)}`);
    doc.text(`Note: ${page.reviewerNote ?? page.inferredStory ?? '—'}`);
    doc.moveDown(0.9);
  }
}

function addPriorityRecommendations(doc: PDFKit.PDFDocument, data: ReviewWorkflowExportData): void {
  addSectionHeading(doc, 'Priority Recommendations');
  const severityCounts = new Map<string, number>([
    ['high', 0],
    ['medium', 0],
    ['low', 0],
  ]);
  for (const recommendation of data.priorityRecommendations) {
    const key = recommendation.severity.toLowerCase();
    severityCounts.set(key, (severityCounts.get(key) ?? 0) + 1);
  }
  addSubsectionHeading(doc, 'Recommendation breakdown');
  addKeyValue(doc, 'High priority', String(severityCounts.get('high') ?? 0));
  addKeyValue(doc, 'Medium priority', String(severityCounts.get('medium') ?? 0));
  addKeyValue(doc, 'Low priority', String(severityCounts.get('low') ?? 0));
  addSubsectionNote(
    doc,
    'Recommendation evidence',
    'Severity, affected pages, and remediation guidance are grouped here for remediation planning.',
  );
  for (const recommendation of data.priorityRecommendations) {
    ensureSpace(doc, 68);
    doc.roundedRect(PAGE_MARGIN, doc.y - 4, doc.page.width - (PAGE_MARGIN * 2), 60, 6)
      .lineWidth(0.75)
      .strokeColor('#d9c8a3')
      .stroke();
    doc.font('Helvetica-Bold').fontSize(11).text(
      `[${recommendation.severity.toUpperCase()}] ${recommendation.issueCategory}`,
    );
    doc.font('Helvetica').text(`Affected pages: ${recommendation.affectedPages.join(', ') || 'Report-wide'}`);
    doc.text(`Guidance: ${recommendation.remediationGuidance}`);
    doc.moveDown(0.9);
  }
}

function addCrossPageConsistencySummary(doc: PDFKit.PDFDocument, data: ReviewWorkflowExportData): void {
  addSectionHeading(doc, 'Cross-Page Consistency Summary');
  const rollup = data.crossPageConsistencyRollup;
  if (!rollup) {
    doc.font('Helvetica').fontSize(11).text('No cross-page consistency summary is available for this export.');
    return;
  }

  addKeyValue(doc, 'Overall finding', rollup.overallFinding ?? 'No report-level finding recorded.');
  addKeyValue(doc, 'Issue groups', String(rollup.issueCount));
  if (rollup.issuesByCategory.length > 0) {
    addSubsectionHeading(doc, 'Consistency breakdown');
    for (const [category, count] of rollup.issuesByCategory) {
      addKeyValue(doc, category, String(count));
    }
  }
  if (rollup.remediation.length > 0) {
    addSubsectionHeading(doc, 'Report-level priorities');
    addNumberedPriorityList(doc, rollup.remediation);
  }
}

function addAppendix(doc: PDFKit.PDFDocument, data: ReviewWorkflowExportData): void {
  startFreshPage(doc);
  addSectionHeading(doc, 'Appendix: Technical Detail');
  addSubsectionNote(
    doc,
    'Appendix evidence lead',
    'Use this appendix as supporting evidence for framework discussions, remediation planning, and governance follow-up.',
  );
  addSubsectionHeading(doc, 'Appendix evidence breakdown');
  addKeyValue(doc, 'Framework score sets', String(data.appendix.frameworkScores.length));
  addKeyValue(doc, 'Metadata findings', String(data.appendix.metadataDerivedFindings.length));
  addKeyValue(doc, 'Methodology notes', String(data.appendix.methodologyNotes.length));
  addSubsectionHeading(doc, 'Framework Scores');
  addSubsectionNote(
    doc,
    'Framework score evidence',
    'Use these framework scores as the compact scoring reference for technical follow-up and governance review.',
  );
  for (const framework of data.appendix.frameworkScores) {
    doc.font('Helvetica').text(`${framework.framework}: ${Math.round(framework.score)}`);
  }
  addSubsectionHeading(doc, 'Metadata-Derived Findings');
  addBulletList(doc, data.appendix.metadataDerivedFindings);
  addSubsectionHeading(doc, 'Deterministic vs Inferred Notes');
  addSubsectionNote(
    doc,
    'Methodology note',
    'These notes distinguish deterministic findings from inference-based interpretation.',
  );
  addBulletList(doc, data.appendix.methodologyNotes);
}

function addPageChrome(
  doc: PDFKit.PDFDocument,
  pageIndex: number,
  totalPages: number,
  title: string,
  profile: ReviewWorkflowExportProfile,
  options: ReviewWorkflowMarkdownRenderOptions,
): void {
  if (pageIndex === 0) {
    return;
  }

  const headerY = doc.page.margins.top - 24;
  const footerY = doc.page.height - doc.page.margins.bottom + 6;
  const headerLabel = ['Packet section', title].join(' • ');
  const footerLabel = [
    options.branding?.clientName ?? title,
    `${profileLabel(profile)} profile`,
    templateLabel(options),
  ].join(' • ');
  const pageLabel = `Page ${pageIndex + 1} of ${totalPages}`;

  doc.save();
  doc.font('Helvetica').fontSize(9).fillColor('#6b7280');
  doc.moveTo(PAGE_MARGIN, headerY + 16)
    .lineTo(doc.page.width - PAGE_MARGIN, headerY + 16)
    .lineWidth(0.5)
    .strokeColor('#e3d8bf')
    .stroke();
  doc.text(headerLabel, PAGE_MARGIN, headerY, {
    width: doc.page.width - (PAGE_MARGIN * 2),
    align: 'left',
  });
  doc.moveTo(PAGE_MARGIN, footerY - 6)
    .lineTo(doc.page.width - PAGE_MARGIN, footerY - 6)
    .lineWidth(0.5)
    .strokeColor('#d9c8a3')
    .stroke();
  doc.text(footerLabel, PAGE_MARGIN, footerY, {
    width: (doc.page.width / 2) - PAGE_MARGIN,
    align: 'left',
  });
  doc.text(pageLabel, doc.page.width / 2, footerY, {
    width: (doc.page.width / 2) - PAGE_MARGIN,
    align: 'right',
  });
  doc.restore();
}

export async function renderReviewWorkflowPacketPdf(
  data: ReviewWorkflowExportData,
  profile: ReviewWorkflowExportProfile = 'consultant',
  options: ReviewWorkflowMarkdownRenderOptions = {},
): Promise<Buffer> {
  const title = options.branding?.engagementName
    ?? (profile === 'executive'
      ? 'PBIR Design Analyzer Executive Review Brief'
      : profile === 'governance'
        ? 'PBIR Design Analyzer Governance Review Packet'
        : 'PBIR Design Analyzer Consultant Review Packet');

  const doc = new PDFDocument({
    margin: PAGE_MARGIN,
    size: 'LETTER',
    compress: false,
    bufferPages: true,
    info: {
      Title: title,
      Author: options.branding?.reviewerName ?? data.analyzerMetadata.analyzerName,
      Subject: 'PBIR Design Analyzer review workflow export',
      Keywords: 'PBIR, Power BI, review packet',
      Producer: data.analyzerMetadata.analyzerName,
      Creator: data.analyzerMetadata.analyzerName,
    },
  });

  const chunks: Buffer[] = [];
  doc.on('data', (chunk) => chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk)));

  const completion = new Promise<Buffer>((resolve, reject) => {
    doc.on('end', () => resolve(Buffer.concat(chunks)));
    doc.on('error', reject);
  });

  if (profile === 'consultant') {
    addCoverPage(doc, data, profile, options);
    doc.addPage();
  }

  addSectionHeading(doc, title);
  addPacketLead(doc);
  addProfileNote(doc, profile);
  addPacketOverview(doc, data, profile, options);
  if (profile === 'governance') {
    addPacketMetadata(doc, data, profile, options);
    addKeyValue(doc, 'Overall score', `${data.compositeScore} / 100`);
    addKeyValue(doc, 'Review status', data.executiveSummary.overallStatus);
    addKeyValue(doc, 'Consistency finding', data.crossPageConsistencyRollup?.overallFinding ?? 'No report-level consistency finding recorded.');
    addReviewStatusSummary(doc, data);
    addPriorityRecommendations(doc, data);
    addCrossPageConsistencySummary(doc, data);
    addAppendix(doc, data);
  } else if (profile === 'executive') {
    addPacketMetadata(doc, data, profile, options);
    addExecutiveSummary(doc, data);
    addReviewStatusSummary(doc, data);
    addPriorityRecommendations(doc, data);
    addCrossPageConsistencySummary(doc, data);
  } else {
    addPacketMetadata(doc, data, profile, options);
    addExecutiveSummary(doc, data);
    addReviewStatusSummary(doc, data);
    addPageIntentValidation(doc, data);
    addPriorityRecommendations(doc, data);
    addCrossPageConsistencySummary(doc, data);
    addAppendix(doc, data);
  }

  const range = doc.bufferedPageRange();
  for (let pageIndex = range.start; pageIndex < range.start + range.count; pageIndex += 1) {
    doc.switchToPage(pageIndex);
    addPageChrome(doc, pageIndex, range.count, title, profile, options);
  }

  doc.end();
  return completion;
}
