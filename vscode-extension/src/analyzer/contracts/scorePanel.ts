import type { DesignAnalyzerConfig } from '../config/types';

export interface FrameworkFeedbackItem {
  ok: boolean;
  text: string;
  affectedVisuals?: AffectedVisualReference[];
  earnedPoints?: number;
  possiblePoints?: number;
}

export interface AffectedVisualReference {
  pageName: string;
  visualId: string;
  visualType: string;
}

export interface PageScore {
  pageName: string;
  gestaltScore: number;
  cognitiveLoadScore: number;
  dataInkScore: number;
  accessibilityScore: number;
  visualBestPracticesScore: number;
  stephenFewScore: number;
  enterpriseGovernanceScore: number;
  tufteScore: number;
  graphicalPerceptionScore: number;
  densityScore: number;
  narrativeScore: number;
  dataVisualCount?: number;
  navigationVisualCount?: number;
  hiddenVisualCount?: number;
  compositeScore: number;
  feedback: Record<string, FrameworkFeedbackItem[]>;
  recommendations: string[];
  scoringError?: string;
  frameworkWeights?: Record<string, number>;
}

export interface ScoreResult {
  gestaltScore: number;
  cognitiveLoadScore: number;
  dataInkScore: number;
  accessibilityScore: number;
  visualBestPracticesScore: number;
  stephenFewScore: number;
  enterpriseGovernanceScore: number;
  tufteScore: number;
  graphicalPerceptionScore: number;
  densityScore: number;
  narrativeScore: number;
  compositeScore: number;
  feedback: Record<string, FrameworkFeedbackItem[]>;
  pageCount: number;
  recommendations: string[];
  reportPath: string;
  scoredAt: string;
  dataVisualCount?: number;
  navigationVisualCount?: number;
  hiddenVisualCount?: number;
  pageScores?: PageScore[];
  scoredPageName?: string;
  scoringErrors?: Record<string, string>;
  layoutScore?: number;
  themeScore?: number;
  governanceScore?: number;
  frameworkWeights?: Record<string, number>;
}

export interface ScoreRequestPayload {
  reportPath: string;
  config: DesignAnalyzerConfig;
  pageName?: string;
}

export interface ScorePanelState {
  config: DesignAnalyzerConfig;
  result: ScoreResult;
  selectedPageIndex: number;
}

export type ScorePanelWebviewToHostMessage =
  | { type: 'webviewReady' }
  | { type: 'refresh' }
  | { type: 'selectTab'; pageIndex: number }
  | { type: 'revealVisual'; pageName: string; visualId: string };

export type ScorePanelHostToWebviewMessage =
  | { type: 'loading' }
  | { type: 'scoreState'; state: ScorePanelState }
  | { type: 'error'; message: string };
