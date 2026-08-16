export type SurfaceType = 'pbirReport' | 'fabricApp' | 'screenshotBundle';
export type SurfaceEvidenceKind =
  | 'pbirMetadata'
  | 'interaction'
  | 'navigation'
  | 'typescriptLayout'
  | 'designToken'
  | 'semanticModel'
  | 'screenshot'
  | 'portability';
export type SurfaceAnalysisCapability = 'findings' | 'evidence' | 'remediation' | 'governanceSignals';
export type SurfaceGovernanceCapability = 'analytics';

export interface AnalyzableSurface {
  surfaceType: SurfaceType;
  displayName: string;
  sourceLocation: string;
  availableEvidenceKinds: SurfaceEvidenceKind[];
  availableAnalyzerTypes: Array<'pbirDesignReview' | 'fabricAppReadiness' | 'fabricAppReview'>;
  availableAnalyzerProfiles: Array<
    'default' |
    'migrationReadiness' |
    'executive' |
    'consultant' |
    'governance' |
    'accessibility' |
    'fabricAppQuality'
  >;
  analysisCapabilities: SurfaceAnalysisCapability[];
  governanceCapabilities: SurfaceGovernanceCapability[];
}

export type SurfaceDiscoveryReasonCode =
  | 'unsupportedSurface'
  | 'missingFabricAppIndicators'
  | 'missingAnalyticsTypescript'
  | 'missingNavigationArtifacts'
  | 'ambiguousAnalyticsSurface';

export type SurfaceDiscoveryResult =
  | { status: 'supported'; surface: AnalyzableSurface }
  | { status: 'unsupported'; reasonCode: SurfaceDiscoveryReasonCode; reason: string }
  | { status: 'ambiguous'; reasonCode: SurfaceDiscoveryReasonCode; reason: string };
