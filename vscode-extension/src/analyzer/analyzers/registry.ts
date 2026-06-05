import type { AnalyzerProfileId, AnalyzerRegistration, AnalyzerSelection, AnalyzerType } from './types';
import type { AnalyzableSurface } from '../surfaces/types';

const ANALYZER_REGISTRY: AnalyzerRegistration[] = [
  {
    analyzerType: 'pbirDesignReview',
    supportedSurfaceTypes: ['pbirReport'],
    profiles: ['default'],
    defaultProfile: 'default',
  },
  {
    analyzerType: 'fabricAppReadiness',
    supportedSurfaceTypes: ['pbirReport'],
    profiles: ['default', 'migrationReadiness'],
    defaultProfile: 'migrationReadiness',
  },
  {
    analyzerType: 'fabricAppReview',
    supportedSurfaceTypes: ['fabricApp'],
    profiles: ['default', 'fabricAppQuality'],
    defaultProfile: 'fabricAppQuality',
  },
];

export function getSupportedAnalyzersForSurface(surface: AnalyzableSurface): AnalyzerRegistration[] {
  return ANALYZER_REGISTRY.filter((entry) =>
    entry.supportedSurfaceTypes.includes(surface.surfaceType) &&
    surface.availableAnalyzerTypes.includes(entry.analyzerType),
  );
}

function getAnalyzerRegistration(analyzerType: AnalyzerType): AnalyzerRegistration | undefined {
  return ANALYZER_REGISTRY.find((entry) => entry.analyzerType === analyzerType);
}

export function selectAnalyzerProfile(
  surface: AnalyzableSurface,
  analyzerType: AnalyzerType,
  requestedProfile: AnalyzerProfileId,
): AnalyzerProfileId {
  const registration = getAnalyzerRegistration(analyzerType);
  if (!registration) {
    return 'default';
  }

  return registration.profiles.includes(requestedProfile) && surface.availableAnalyzerProfiles.includes(requestedProfile)
    ? requestedProfile
    : registration.defaultProfile;
}

export function getDefaultAnalyzerSelection(surface: AnalyzableSurface): AnalyzerSelection {
  const readinessAnalyzer = getSupportedAnalyzersForSurface(surface).find((entry) => entry.analyzerType === 'fabricAppReadiness');
  if (readinessAnalyzer) {
    return {
      analyzerType: readinessAnalyzer.analyzerType,
      analyzerProfile: readinessAnalyzer.defaultProfile,
    };
  }

  const fallback = getSupportedAnalyzersForSurface(surface)[0];
  return {
    analyzerType: fallback?.analyzerType ?? 'pbirDesignReview',
    analyzerProfile: fallback?.defaultProfile ?? 'default',
  };
}
