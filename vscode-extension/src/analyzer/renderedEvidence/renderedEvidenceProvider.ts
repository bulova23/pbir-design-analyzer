import type {
  IRenderedDesignEvidenceProvider,
  RenderedEvidenceCapabilityReport,
  RenderedEvidenceResult,
} from './types';
import {
  detectPbiLensCapabilities,
  PBI_LENS_EXTENSION_ID,
  type PbiLensExtensionLookup,
} from './pbiLensCapabilityDetector';

export function createPbiLensRenderedDesignEvidenceProvider(
  getExtension: PbiLensExtensionLookup,
): IRenderedDesignEvidenceProvider {
  const getCapabilityReport = (): RenderedEvidenceCapabilityReport => {
    const report = detectPbiLensCapabilities(getExtension);
    if (report.installed && !report.activated) {
      activateInBackground(getExtension);
    }
    return report;
  };

  return {
    getCapabilityReport,
    async getEvidence(): Promise<RenderedEvidenceResult> {
      const report = getCapabilityReport();
      return {
        providerId: 'pbiLens',
        status: report.status,
        evidence: [],
        diagnostics: [
          ...report.diagnostics,
          'Rendered evidence acquisition is not available.',
        ],
      };
    },
  };
}

// PBI Lens declares no activation events of its own (it relies on VS Code's implicit
// per-contribution activation), so simply being installed never makes it active. Rendered
// evidence is an optional scoring enhancement, so activation here is fire-and-forget: if it
// fails or the extension has no activate() to call, the next capability check just reports the
// same "installed but not activated" state rather than blocking anything.
function activateInBackground(getExtension: PbiLensExtensionLookup): void {
  try {
    const extension = getExtension(PBI_LENS_EXTENSION_ID);
    if (typeof extension?.activate === 'function') {
      void Promise.resolve(extension.activate()).catch(() => undefined);
    }
  } catch {
    // Extension lookup itself failing is already handled by detectPbiLensCapabilities.
  }
}
