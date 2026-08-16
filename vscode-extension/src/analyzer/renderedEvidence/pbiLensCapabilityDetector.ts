import type { RenderedEvidenceCapabilityReport } from './types';

export const PBI_LENS_EXTENSION_ID = 'duckduck-beps.pbi-lens-vscode';

export interface PbiLensExtensionLike {
  id?: string;
  isActive?: boolean;
  packageJSON?: { version?: unknown };
  exports?: unknown;
  activate?: () => Thenable<unknown>;
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

  // `.exports` is only safe to read once the extension is actually active — VS Code's own
  // Extension.exports getter throws "Extension '<id>' is not known or not activated" for an
  // installed-but-inactive extension rather than returning undefined. PBI Lens integration is
  // strictly optional (better visual-based scoring, never a scoring requirement), so an extension
  // that's merely installed and not yet activated must degrade to a capability report, not abort
  // scoring for the whole panel.
  const activated = extension.isActive === true;
  let publicApiAvailable = false;
  try {
    publicApiAvailable = activated && hasSupportedPublicApi(extension.exports);
  } catch {
    // Some VS Code versions throw reading `.exports` even when `isActive` reads true transiently
    // (e.g. mid-activation); treat that the same as "no supported public API".
  }

  const diagnostics = !activated
    ? ['PBI Lens is installed but not activated.']
    : publicApiAvailable
      ? ['PBI Lens public exports were detected, but no provider adapter is enabled in this release.']
      : ['PBI Lens exposes no supported public VS Code API.'];

  return {
    providerId: 'pbiLens',
    displayName: 'PBI Lens',
    extensionId: PBI_LENS_EXTENSION_ID,
    installed: true,
    activated,
    version: readVersion(extension),
    status: activated ? 'InstalledNoProgrammaticSurface' : 'Misconfigured',
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
