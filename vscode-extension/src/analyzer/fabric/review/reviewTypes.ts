import type { NormalizedFinding } from '../../contracts/scorePanel';

export interface FabricAppEvidenceItem {
  kind: 'typescriptLayout' | 'navigation' | 'designToken' | 'screenshot' | 'semanticModel';
  label: string;
  summary: string;
  filePath: string;
  pageName?: string;
  stateName?: string;
}

export interface TypeScriptEvidenceSignal {
  filePath: string;
  summary: string;
}

export interface TypeScriptEvidenceReport {
  layoutPatterns: TypeScriptEvidenceSignal[];
  kpiPatterns: TypeScriptEvidenceSignal[];
  compositionSignals: TypeScriptEvidenceSignal[];
}

export interface NavigationRouteEvidence {
  path: string;
  label: string;
  filePath: string;
}

export interface NavigationEvidenceReport {
  routes: NavigationRouteEvidence[];
  hasExecutiveToDetailFlow: boolean;
  summary: string;
}

export interface DesignTokenEvidenceSignal {
  filePath: string;
  summary: string;
  token?: string;
}

export interface DesignTokenEvidenceReport {
  tokens: DesignTokenEvidenceSignal[];
  bypasses: DesignTokenEvidenceSignal[];
}

export interface ScreenshotEvidenceSignal {
  filePath: string;
  fileName: string;
  pageName: string;
  stateName?: string;
}

export interface ScreenshotEvidenceReport {
  captures: ScreenshotEvidenceSignal[];
  unmatchedCaptures: Array<{
    filePath: string;
    fileName: string;
    stateName?: string;
  }>;
}

export interface SemanticModelEvidenceSignal {
  filePath: string;
  summary: string;
}

export interface SemanticModelEvidenceReport {
  signals: SemanticModelEvidenceSignal[];
}

export interface FabricAppReviewResult {
  qualityScore: number;
  summary: string;
  remediationGuidance: string[];
  evidence: FabricAppEvidenceItem[];
  normalizedFindings: NormalizedFinding[];
}
