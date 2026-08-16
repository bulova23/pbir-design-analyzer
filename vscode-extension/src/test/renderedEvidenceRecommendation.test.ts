import {
  getEnhancedScoringSettings,
  getRenderedReviewSettings,
} from '../platform/settings';
import {
  maybeRecommendPbiLens,
  PBI_LENS_RECOMMENDATION_DISMISSED_KEY,
} from '../analyzer/renderedEvidence/recommendation';
import type { RenderedEvidenceCapabilityReport } from '../analyzer/renderedEvidence/types';

const absentReport: RenderedEvidenceCapabilityReport = {
  providerId: 'pbiLens',
  displayName: 'PBI Lens',
  extensionId: 'duckduck-beps.pbi-lens-vscode',
  installed: false,
  activated: false,
  status: 'NotInstalled',
  capabilities: {
    extensionDetected: false,
    publicApiAvailable: false,
    cliAvailable: false,
    mcpAvailable: false,
    pageScreenshotAvailable: false,
    reportContextAvailable: false,
    visualContextAvailable: false,
  },
  diagnostics: [],
};

const installedReport: RenderedEvidenceCapabilityReport = {
  ...absentReport,
  installed: true,
  activated: true,
  version: '0.4.0',
  status: 'InstalledNoProgrammaticSurface',
  capabilities: {
    ...absentReport.capabilities,
    extensionDetected: true,
  },
};

describe('rendered evidence settings and recommendation', () => {
  it('uses backward-compatible safe defaults', () => {
    const settings = getEnhancedScoringSettings({
      get: (_key, defaultValue) => defaultValue,
    });

    expect(settings).toEqual({
      enabled: false,
      provider: 'auto',
      suggestPbiLens: true,
    });
  });

  it('defaults the rendered review checklist on without changing scoring settings', () => {
    expect(getRenderedReviewSettings({ get: (_key, defaultValue) => defaultValue })).toEqual({
      enabled: true,
      suggestPbiLens: true,
      showChecklist: true,
    });
  });

  it('recommends PBI Lens once when it is absent', async () => {
    const globalState = {
      get: jest.fn().mockReturnValue(false),
      update: jest.fn().mockResolvedValue(undefined),
    };
    const showInformationMessage = jest.fn().mockResolvedValue('Not Now');

    await maybeRecommendPbiLens(absentReport, globalState, showInformationMessage);

    expect(showInformationMessage).toHaveBeenCalledWith(
      'Install PBI Lens for future enhanced rendered-design scoring support.',
      'Learn More',
      'Install PBI Lens',
      'Not Now',
    );
    expect(globalState.update).toHaveBeenCalledWith(PBI_LENS_RECOMMENDATION_DISMISSED_KEY, true);
  });

  it('does not recommend installation when PBI Lens is installed but unusable', async () => {
    const globalState = {
      get: jest.fn().mockReturnValue(false),
      update: jest.fn().mockResolvedValue(undefined),
    };
    const showInformationMessage = jest.fn();

    await maybeRecommendPbiLens(installedReport, globalState, showInformationMessage);

    expect(showInformationMessage).not.toHaveBeenCalled();
    expect(globalState.update).not.toHaveBeenCalled();
  });
});
