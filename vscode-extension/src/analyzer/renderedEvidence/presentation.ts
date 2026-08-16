import type { RenderedEvidenceCapabilityReport } from './types';

export function getRenderedEvidenceStatusMessage(
  report: RenderedEvidenceCapabilityReport | undefined,
): string | undefined {
  if (!report) {
    return undefined;
  }

  switch (report.status) {
    case 'NotInstalled':
      return 'Install PBI Lens for future enhanced rendered-design scoring support.';
    case 'InstalledNoProgrammaticSurface':
      return 'PBI Lens detected, but this installed configuration does not expose a supported programmatic rendering interface. Deterministic scoring remains active.';
    case 'CliAvailable':
    case 'McpAvailable':
    case 'Available':
      return 'Rendered design evidence is available through an optional provider. Deterministic scoring remains authoritative.';
    case 'Misconfigured':
    case 'Error':
      return 'Rendered design evidence is unavailable. Deterministic scoring remains active.';
    default:
      return undefined;
  }
}

export function isPbiLensOpenActionAvailable(
  report: RenderedEvidenceCapabilityReport | undefined,
): boolean {
  return report?.providerId === 'pbiLens' && report.capabilities.reportContextAvailable === true;
}
