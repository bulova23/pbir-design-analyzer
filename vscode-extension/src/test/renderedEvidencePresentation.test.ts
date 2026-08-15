import { getRenderedEvidenceStatusMessage, isPbiLensOpenActionAvailable } from '../analyzer/renderedEvidence/presentation';

describe('rendered evidence status presentation', () => {
  it('explains that deterministic scoring remains active when PBI Lens is installed without an API', () => {
    expect(getRenderedEvidenceStatusMessage({
      providerId: 'pbiLens',
      displayName: 'PBI Lens',
      extensionId: 'duckduck-beps.pbi-lens-vscode',
      installed: true,
      activated: true,
      version: '0.4.0',
      status: 'InstalledNoProgrammaticSurface',
      capabilities: {
        extensionDetected: true,
        publicApiAvailable: false,
        cliAvailable: false,
        mcpAvailable: false,
        pageScreenshotAvailable: false,
        reportContextAvailable: false,
        visualContextAvailable: false,
      },
      diagnostics: [],
    })).toContain('Deterministic scoring remains active.');
  });

  it('keeps the PBI Lens action unavailable without a supported report context capability', () => {
    expect(isPbiLensOpenActionAvailable(undefined)).toBe(false);
    expect(isPbiLensOpenActionAvailable({
      providerId: 'pbiLens',
      displayName: 'PBI Lens',
      extensionId: 'duckduck-beps.pbi-lens-vscode',
      installed: true,
      activated: true,
      status: 'InstalledNoProgrammaticSurface',
      capabilities: {
        extensionDetected: true,
        publicApiAvailable: false,
        cliAvailable: false,
        mcpAvailable: false,
        pageScreenshotAvailable: false,
        reportContextAvailable: false,
        visualContextAvailable: false,
      },
      diagnostics: [],
    })).toBe(false);
  });
});
