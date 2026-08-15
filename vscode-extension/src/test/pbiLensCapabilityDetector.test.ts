import {
  detectPbiLensCapabilities,
  PBI_LENS_EXTENSION_ID,
} from '../analyzer/renderedEvidence/pbiLensCapabilityDetector';
import { createPbiLensRenderedDesignEvidenceProvider } from '../analyzer/renderedEvidence/renderedEvidenceProvider';

describe('PBI Lens capability detection', () => {
  it('reports the provider as not installed when the extension cannot be found', () => {
    const report = detectPbiLensCapabilities(() => undefined);

    expect(report).toMatchObject({
      extensionId: PBI_LENS_EXTENSION_ID,
      installed: false,
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
    });
  });

  it('recognizes an installed extension without treating activate/deactivate as a public API', () => {
    const report = detectPbiLensCapabilities(() => ({
      id: PBI_LENS_EXTENSION_ID,
      isActive: true,
      packageJSON: { version: '0.4.0' },
      exports: { activate: jest.fn(), deactivate: jest.fn() },
    }));

    expect(report).toMatchObject({
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
    });
    expect(report.diagnostics).toContain('PBI Lens exposes no supported public VS Code API.');
  });

  it('keeps independently unavailable CLI and MCP capabilities false', () => {
    const report = detectPbiLensCapabilities(() => ({
      id: PBI_LENS_EXTENSION_ID,
      isActive: true,
      packageJSON: { version: '0.4.0' },
      exports: undefined,
    }));

    expect(report.capabilities.cliAvailable).toBe(false);
    expect(report.capabilities.mcpAvailable).toBe(false);
    expect(report.status).toBe('InstalledNoProgrammaticSurface');
  });

  it('reports an extension that is installed but inactive as misconfigured', () => {
    const report = detectPbiLensCapabilities(() => ({
      id: PBI_LENS_EXTENSION_ID,
      isActive: false,
      packageJSON: { version: '0.4.0' },
      exports: undefined,
    }));

    expect(report.status).toBe('Misconfigured');
    expect(report.diagnostics).toContain('PBI Lens is installed but not activated.');
  });

  it('contains extension discovery failures in the provider report', () => {
    const report = detectPbiLensCapabilities(() => {
      throw new Error('extension registry unavailable');
    });

    expect(report.status).toBe('Error');
    expect(report.capabilities.extensionDetected).toBe(false);
    expect(report.diagnostics).toEqual(['PBI Lens capability detection failed.']);
  });

  it('returns bounded unavailable evidence instead of inventing a screenshot', async () => {
    const provider = createPbiLensRenderedDesignEvidenceProvider(() => ({
      id: PBI_LENS_EXTENSION_ID,
      isActive: true,
      packageJSON: { version: '0.4.0' },
      exports: { activate: jest.fn(), deactivate: jest.fn() },
    }));

    const result = await provider.getEvidence({
      reportId: 'report-1',
      pageId: 'page-1',
      pageName: 'Overview',
    });

    expect(result.evidence).toEqual([]);
    expect(result.status).toBe('InstalledNoProgrammaticSurface');
    expect(result.diagnostics).toContain('Rendered evidence acquisition is not available.');
  });

  it('contains provider discovery errors without rejecting evidence requests', async () => {
    const provider = createPbiLensRenderedDesignEvidenceProvider(() => {
      throw new Error('extension lookup failed');
    });

    await expect(provider.getEvidence({ reportId: 'report-1' })).resolves.toMatchObject({
      status: 'Error',
      evidence: [],
    });
  });
});
