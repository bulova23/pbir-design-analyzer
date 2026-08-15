import type {
  IRenderedDesignEvidenceProvider,
  RenderedEvidenceCapabilityReport,
  RenderedEvidenceResult,
} from './types';
import {
  detectPbiLensCapabilities,
  type PbiLensExtensionLookup,
} from './pbiLensCapabilityDetector';

export function createPbiLensRenderedDesignEvidenceProvider(
  getExtension: PbiLensExtensionLookup,
): IRenderedDesignEvidenceProvider {
  let capabilityReport: RenderedEvidenceCapabilityReport | undefined;

  const getCapabilityReport = (): RenderedEvidenceCapabilityReport => {
    capabilityReport ??= detectPbiLensCapabilities(getExtension);
    return capabilityReport;
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
