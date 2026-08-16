import type { AnalyzableSurface, SurfaceType } from './types';

type SurfaceCapabilityShape = Omit<AnalyzableSurface, 'surfaceType' | 'displayName' | 'sourceLocation'>;

const SURFACE_CAPABILITIES: Record<SurfaceType, SurfaceCapabilityShape> = {
  pbirReport: {
    availableEvidenceKinds: ['pbirMetadata', 'interaction', 'navigation', 'semanticModel', 'portability'],
    availableAnalyzerTypes: ['pbirDesignReview', 'fabricAppReadiness'],
    availableAnalyzerProfiles: ['default', 'migrationReadiness'],
    analysisCapabilities: ['findings', 'evidence', 'remediation', 'governanceSignals'],
    governanceCapabilities: ['analytics'],
  },
  fabricApp: {
    availableEvidenceKinds: ['typescriptLayout', 'navigation', 'designToken'],
    availableAnalyzerTypes: ['fabricAppReview'],
    availableAnalyzerProfiles: ['default', 'fabricAppQuality'],
    analysisCapabilities: ['findings', 'evidence', 'remediation', 'governanceSignals'],
    governanceCapabilities: ['analytics'],
  },
  screenshotBundle: {
    availableEvidenceKinds: ['screenshot'],
    availableAnalyzerTypes: [],
    availableAnalyzerProfiles: ['default'],
    analysisCapabilities: ['evidence'],
    governanceCapabilities: [],
  },
};

export function buildAnalyzableSurface(
  surfaceType: SurfaceType,
  options: {
    displayName: string;
    sourceLocation: string;
  },
): AnalyzableSurface {
  return {
    surfaceType,
    displayName: options.displayName,
    sourceLocation: options.sourceLocation,
    ...SURFACE_CAPABILITIES[surfaceType],
  };
}
