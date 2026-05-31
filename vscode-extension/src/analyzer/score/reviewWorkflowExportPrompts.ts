import * as vscode from 'vscode';
import type {
  ReviewWorkflowExportProfile,
  ReviewWorkflowMarkdownRenderOptions,
  ReviewWorkflowMarkdownTemplateVariant,
} from '../contracts/scorePanel';

export type ReviewWorkflowDocumentFormat = 'markdown' | 'html' | 'pdf';

export interface ProfiledDocumentExportSelection extends ReviewWorkflowMarkdownRenderOptions {
  profile: ReviewWorkflowExportProfile;
}

export interface ReviewWorkflowExportPromptDefaults {
  profile: ReviewWorkflowExportProfile;
  templateVariant: ReviewWorkflowMarkdownTemplateVariant;
}

function prioritizeCurrentPreviewOption<T>(
  options: T[],
  isCurrentPreview: (option: T) => boolean,
): T[] {
  const currentIndex = options.findIndex(isCurrentPreview);
  if (currentIndex <= 0) {
    return options;
  }

  return [options[currentIndex], ...options.slice(0, currentIndex), ...options.slice(currentIndex + 1)];
}

function withCurrentPreviewDescription(
  description: string | undefined,
  isCurrentPreview: boolean,
): string | undefined {
  if (!isCurrentPreview) {
    return description;
  }

  return description ? `${description} · Current preview` : 'Current preview';
}

async function promptForBrandingValue(
  prompt: string,
  placeHolder: string,
): Promise<string | undefined> {
  const value = await vscode.window.showInputBox({
    prompt,
    placeHolder,
    ignoreFocusOut: true,
  });

  if (value === undefined) {
    return undefined;
  }

  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : '';
}

async function promptForConsultantTemplateVariant(
  format: ReviewWorkflowDocumentFormat,
  defaults?: ReviewWorkflowExportPromptDefaults,
): Promise<ReviewWorkflowMarkdownTemplateVariant | undefined> {
  const options = prioritizeCurrentPreviewOption([
    {
      label: 'Standard consultant template',
      description: withCurrentPreviewDescription(
        undefined,
        defaults?.profile === 'consultant' && defaults.templateVariant === 'standard',
      ),
      value: 'standard' as const,
    },
    {
      label: 'Branded consultant template',
      description: withCurrentPreviewDescription(
        `Best for client-facing ${format.toUpperCase()} output`,
        defaults?.profile === 'consultant' && defaults.templateVariant === 'brandedConsultant',
      ),
      value: 'brandedConsultant' as const,
    },
  ], (option) => option.description?.includes('Current preview') ?? false);
  const choice = await vscode.window.showQuickPick(
    options,
    {
      placeHolder: defaults?.profile === 'consultant'
        ? `Choose consultant packet template for ${format.toUpperCase()} export (current preview: ${defaults.templateVariant === 'brandedConsultant' ? 'Branded consultant template' : 'Standard consultant template'})`
        : `Choose consultant packet template for ${format.toUpperCase()} export`,
    },
  );

  return choice?.value;
}

export async function chooseProfiledDocumentExportOptions(
  format: ReviewWorkflowDocumentFormat,
  defaults?: ReviewWorkflowExportPromptDefaults,
): Promise<ProfiledDocumentExportSelection | undefined> {
  const profileOptions = prioritizeCurrentPreviewOption([
    {
      label: 'Consultant',
      description: withCurrentPreviewDescription(
        `Full ${format.toUpperCase()} review packet`,
        defaults?.profile === 'consultant',
      ),
      value: 'consultant' as const,
    },
    {
      label: 'Executive',
      description: withCurrentPreviewDescription(
        `Short ${format.toUpperCase()} review brief`,
        defaults?.profile === 'executive',
      ),
      value: 'executive' as const,
    },
    {
      label: 'Governance',
      description: withCurrentPreviewDescription(
        `Consistency and governance-focused ${format.toUpperCase()} packet`,
        defaults?.profile === 'governance',
      ),
      value: 'governance' as const,
    },
  ], (option) => option.description?.includes('Current preview') ?? false);
  const profileChoice = await vscode.window.showQuickPick(
    profileOptions,
    {
      placeHolder: defaults
        ? `Choose ${format.toUpperCase()} export profile (current preview: ${defaults.profile[0].toUpperCase()}${defaults.profile.slice(1)})`
        : `Choose ${format.toUpperCase()} export profile`,
    },
  );

  if (!profileChoice) {
    return undefined;
  }

  if (profileChoice.value !== 'consultant') {
    return {
      profile: profileChoice.value,
      templateVariant: 'standard',
    };
  }

  const templateVariant = await promptForConsultantTemplateVariant(format, defaults);
  if (!templateVariant) {
    return undefined;
  }

  if (templateVariant !== 'brandedConsultant') {
    return {
      profile: profileChoice.value,
      templateVariant,
    };
  }

  const clientName = await promptForBrandingValue(
    'Client or organization name for the packet cover',
    'Contoso Finance',
  );
  if (clientName === undefined) return undefined;

  const reviewerName = await promptForBrandingValue(
    'Reviewer, consultant, or team name',
    'Northwind BI Advisory',
  );
  if (reviewerName === undefined) return undefined;

  const engagementName = await promptForBrandingValue(
    'Engagement or review title',
    'FY26 Executive Dashboard Review',
  );
  if (engagementName === undefined) return undefined;

  const confidentiality = await promptForBrandingValue(
    'Confidentiality or classification label',
    'Client Confidential',
  );
  if (confidentiality === undefined) return undefined;

  return {
    profile: profileChoice.value,
    templateVariant,
    branding: {
      clientName: clientName || undefined,
      reviewerName: reviewerName || undefined,
      engagementName: engagementName || undefined,
      confidentiality: confidentiality || undefined,
    },
  };
}

export async function chooseMarkdownExportOptions(): Promise<ProfiledDocumentExportSelection | undefined> {
  return chooseProfiledDocumentExportOptions('markdown');
}
