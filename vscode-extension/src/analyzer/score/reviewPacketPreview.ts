import * as path from 'path';
import type {
  ReviewWorkflowExportData,
  ReviewWorkflowExportProfile,
  ReviewWorkflowMarkdownBranding,
  ReviewWorkflowMarkdownTemplateVariant,
} from '../contracts/scorePanel';
import { exportReviewWorkflowAsHtml } from './reviewWorkflowExport';

export interface ReviewPacketPreviewOptions {
  profile: ReviewWorkflowExportProfile;
  templateVariant: ReviewWorkflowMarkdownTemplateVariant;
}

export const defaultReviewPacketPreviewOptions: ReviewPacketPreviewOptions = {
  profile: 'consultant',
  templateVariant: 'brandedConsultant',
};

export function normalizeReviewPacketPreviewOptions(
  options?: Partial<ReviewPacketPreviewOptions>,
): ReviewPacketPreviewOptions {
  const profile = options?.profile ?? defaultReviewPacketPreviewOptions.profile;
  const requestedTemplate = options?.templateVariant ?? defaultReviewPacketPreviewOptions.templateVariant;
  const templateVariant = profile === 'consultant'
    ? requestedTemplate
    : 'standard';

  return {
    profile,
    templateVariant,
  };
}

export function buildDeterministicPreviewBranding(reportPath: string): ReviewWorkflowMarkdownBranding {
  const reportName = path.basename(reportPath).replace(/\.Report$/i, '');
  return {
    clientName: reportName,
    reviewerName: 'PBIR Design Analyzer',
    engagementName: `${reportName} Review Packet Preview`,
    confidentiality: 'Internal preview',
  };
}

export function buildReviewPacketPreviewHtml(
  exportData: ReviewWorkflowExportData,
  reportPath: string,
  options?: Partial<ReviewPacketPreviewOptions>,
): string {
  const normalizedOptions = normalizeReviewPacketPreviewOptions(options);
  return exportReviewWorkflowAsHtml(exportData, normalizedOptions.profile, {
    templateVariant: normalizedOptions.templateVariant,
    branding: normalizedOptions.templateVariant === 'brandedConsultant'
      ? buildDeterministicPreviewBranding(reportPath)
      : undefined,
  });
}
