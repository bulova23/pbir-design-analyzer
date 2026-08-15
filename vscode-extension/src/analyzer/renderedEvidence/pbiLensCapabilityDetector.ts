import type { RenderedEvidenceCapabilityReport } from './types';

export const PBI_LENS_EXTENSION_ID = 'duckduck-beps.pbi-lens-vscode';

export interface PbiLensExtensionLike {
  id?: string;
  isActive?: boolean;
  packageJSON?: { version?: unknown };
  exports?: unknown;
}

export type PbiLensExtensionLookup = (extensionId: string) => PbiLensExtensionLike | undefined;

function readVersion(extension: PbiLensExtensionLike): string | undefined {
  return typeof extension.packageJSON?.version === 'string'
    ? extension.packageJSON.version
    : undefined;
}

function hasSupportedPublicApi(exportsValue: unknown): boolean {
  if (!exportsValue || typeof exportsValue !== 'object') {
    return false;
  }

  return Object.keys(exportsValue).some((key) => key !== 'activate' && key !== 'deactivate');
}

export function detectPbiLensCapabilities(
  getExtension: PbiLensExtensionLookup,
): RenderedEvidenceCapabilityReport {
  let extension: PbiLensExtensionLike | undefined;
  try {
    extension = getExtension(PBI_LENS_EXTENSION_ID);
  } catch {
    return {
      providerId: 'pbiLens',
      displayName: 'PBI Lens',
      extensionId: PBI_LENS_EXTENSION_ID,
      installed: false,
      activated: false,
      status: 'Error',
      capabilities: {
        extensionDetected: false,
        publicApiAvailable: false,
        cliAvailable: false,
        mcpAvailable: false,
        pageScreenshotAvailable: false,
        reportContextAvailable: false,
        visualContextAvailable: false,
      },
      diagnostics: ['PBI Lens capability detection failed.'],
    };
  }

  if (!extension) {
    return {
      providerId: 'pbiLens',
      displayName: 'PBI Lens',
      extensionId: PBI_LENS_EXTENSION_ID,
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
      diagnostics: ['PBI Lens extension was not detected.'],
    };
  }

  const publicApiAvailable = hasSupportedPublicApi(extension.exports);
  const diagnostics = publicApiAvailable
    ? ['PBI Lens public exports were detected, but no provider adapter is enabled in this release.']
    : ['PBI Lens exposes no supported public VS Code API.'];

  const status = extension.isActive === false
    ? 'Misconfigured'
    : 'InstalledNoProgrammaticSurface';
  if (status === 'Misconfigured') {
    diagnostics.unshift('PBI Lens is installed but not activated.');
  }

  return {
    providerId: 'pbiLens',
    displayName: 'PBI Lens',
    extensionId: PBI_LENS_EXTENSION_ID,
    installed: true,
    activated: extension.isActive === true,
    version: readVersion(extension),
    status,
    capabilities: {
      extensionDetected: true,
      publicApiAvailable,
      cliAvailable: false,
      mcpAvailable: false,
      pageScreenshotAvailable: false,
      reportContextAvailable: false,
      visualContextAvailable: false,
    },
    diagnostics,
  };
}
