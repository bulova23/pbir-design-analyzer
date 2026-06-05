import type { AnalyzableSurface } from '../surfaces/types';

export type AnalyzerType = 'pbirDesignReview' | 'fabricAppReadiness' | 'fabricAppReview';
export type AnalyzerProfileId =
  | 'default'
  | 'migrationReadiness'
  | 'executive'
  | 'consultant'
  | 'governance'
  | 'accessibility'
  | 'fabricAppQuality';

export interface AnalyzerRegistration {
  analyzerType: AnalyzerType;
  supportedSurfaceTypes: AnalyzableSurface['surfaceType'][];
  profiles: AnalyzerProfileId[];
  defaultProfile: AnalyzerProfileId;
}

export interface AnalyzerSelection {
  analyzerType: AnalyzerType;
  analyzerProfile: AnalyzerProfileId;
}
