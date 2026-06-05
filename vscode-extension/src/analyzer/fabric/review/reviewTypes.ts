import type { NormalizedFinding } from '../../contracts/scorePanel';

export interface FabricAppEvidenceItem {
  kind: 'typescriptLayout' | 'navigation' | 'designToken';
  label: string;
  summary: string;
  filePath: string;
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

export interface FabricAppReviewResult {
  qualityScore: number;
  summary: string;
  remediationGuidance: string[];
  evidence: FabricAppEvidenceItem[];
  normalizedFindings: NormalizedFinding[];
}
