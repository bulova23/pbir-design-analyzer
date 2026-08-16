import type { AnalyzableSurface } from '../analyzer/surfaces/types';
import {
  getDefaultAnalyzerSelection,
  getSupportedAnalyzersForSurface,
  selectAnalyzerProfile,
} from '../analyzer/analyzers/registry';

function buildPbirSurface(): AnalyzableSurface {
  return {
    surfaceType: 'pbirReport',
    displayName: 'Sales & Production.Report',
    sourceLocation: '/tmp/Sales & Production.Report',
    availableEvidenceKinds: ['pbirMetadata', 'interaction', 'navigation', 'semanticModel', 'portability'],
    availableAnalyzerTypes: ['pbirDesignReview', 'fabricAppReadiness'],
    availableAnalyzerProfiles: ['default', 'migrationReadiness'],
    analysisCapabilities: ['findings', 'evidence', 'remediation', 'governanceSignals'],
    governanceCapabilities: ['analytics'],
  };
}

function buildFabricSurface(): AnalyzableSurface {
  return {
    surfaceType: 'fabricApp',
    displayName: 'Executive Fabric App',
    sourceLocation: '/tmp/executive-fabric-app',
    availableEvidenceKinds: ['navigation', 'semanticModel', 'screenshot'],
    availableAnalyzerTypes: ['fabricAppReview'],
    availableAnalyzerProfiles: ['default', 'fabricAppQuality'],
    analysisCapabilities: ['findings', 'evidence', 'remediation', 'governanceSignals'],
    governanceCapabilities: ['analytics'],
  };
}

describe('analyzerRegistry', () => {
  it('returns the supported analyzers and profiles for PBIR surfaces', () => {
    const supported = getSupportedAnalyzersForSurface(buildPbirSurface());

    expect(supported.map((entry) => entry.analyzerType)).toEqual([
      'pbirDesignReview',
      'fabricAppReadiness',
    ]);
    expect(supported.find((entry) => entry.analyzerType === 'fabricAppReadiness')).toMatchObject({
      defaultProfile: 'migrationReadiness',
      profiles: ['default', 'migrationReadiness'],
    });
  });

  it('defaults PBIR surfaces to the Fabric App readiness analyzer for this release slice', () => {
    expect(getDefaultAnalyzerSelection(buildPbirSurface())).toEqual({
      analyzerType: 'fabricAppReadiness',
      analyzerProfile: 'migrationReadiness',
    });
  });

  it('falls back to the analyzer default profile when a requested profile is unsupported', () => {
    expect(selectAnalyzerProfile(buildPbirSurface(), 'fabricAppReadiness', 'executive')).toBe('migrationReadiness');
    expect(selectAnalyzerProfile(buildPbirSurface(), 'pbirDesignReview', 'migrationReadiness')).toBe('default');
  });

  it('returns the supported analyzer and default profile for Fabric App surfaces', () => {
    const supported = getSupportedAnalyzersForSurface(buildFabricSurface());

    expect(supported).toEqual([
      expect.objectContaining({
        analyzerType: 'fabricAppReview',
        defaultProfile: 'fabricAppQuality',
        profiles: ['default', 'fabricAppQuality'],
      }),
    ]);
    expect(getDefaultAnalyzerSelection(buildFabricSurface())).toEqual({
      analyzerType: 'fabricAppReview',
      analyzerProfile: 'fabricAppQuality',
    });
  });
});
