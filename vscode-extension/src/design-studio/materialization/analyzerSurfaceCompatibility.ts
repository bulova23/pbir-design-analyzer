import type { AnalyzerProfileId, AnalyzerType } from '../../analyzer/analyzers/types';
import { getSupportedAnalyzersForSurface } from '../../analyzer/analyzers/registry';
import type { AnalyzableSurface } from '../../analyzer/surfaces/types';

export interface AnalyzerSurfaceCompatibilityResult {
  ok: boolean;
  diagnostics: string[];
}

export function evaluateAnalyzerSurfaceCompatibility(
  surface: AnalyzableSurface,
  analyzerType: AnalyzerType,
  analyzerProfile: AnalyzerProfileId,
): AnalyzerSurfaceCompatibilityResult {
  const supportedAnalyzers = getSupportedAnalyzersForSurface(surface);
  const analyzerMatch = supportedAnalyzers.find((entry) => entry.analyzerType === analyzerType);

  if (!analyzerMatch) {
    return {
      ok: false,
      diagnostics: [`Target analyzer ${analyzerType} is not supported for ${surface.surfaceType}.`],
    };
  }

  if (!analyzerMatch.profiles.includes(analyzerProfile) || !surface.availableAnalyzerProfiles.includes(analyzerProfile)) {
    return {
      ok: false,
      diagnostics: [`Target analyzer profile ${analyzerProfile} is not supported for ${analyzerType}.`],
    };
  }

  return {
    ok: true,
    diagnostics: [],
  };
}
