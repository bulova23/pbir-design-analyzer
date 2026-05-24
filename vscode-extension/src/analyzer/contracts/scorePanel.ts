import type { DesignAnalyzerConfig } from '../config/types';

export type FindingType = 'objective' | 'strongHeuristic' | 'stylePreference';
export type AuditFindingType = 'objective' | 'strongHeuristic' | 'stylePreference';
export type AuditSeverity = 'critical' | 'warning' | 'info';
export type AuditConfidence = 'high' | 'medium' | 'low';

export interface FrameworkFeedbackItem {
  ok: boolean;
  text: string;
  findingType: FindingType;
  affectedVisuals?: AffectedVisualReference[];
  earnedPoints?: number;
  possiblePoints?: number;
}

export interface AffectedVisualReference {
  pageName: string;
  visualId: string;
  visualType: string;
}

export interface VisualMetadataItem {
  visualId: string;
  visualType: string;
  x: number;
  y: number;
  width: number;
  height: number;
  isHidden: boolean;
  isNavigationElement: boolean;
  isDecorative: boolean;
  isSlicer: boolean;
  visibleTitleText?: string;
  visibleSubtitleText?: string;
  textBoxText?: string;
  bestVisibleText?: string;
  hasVisibleTitleIntent: boolean;
  hasLegend?: boolean;
  hasAxisLabels?: boolean;
  hasDataLabels?: boolean;
  categoryHints: string[];
  valueHints: string[];
  seriesHints: string[];
  measureHints: string[];
  backgroundFillColor?: string;
  fontColor?: string;
  hasBorder?: boolean;
  cornerRadius?: number;
  hasShadow?: boolean;
}

export interface PageVisualMetadataSummary {
  pageName: string;
  visiblePageTitle?: string;
  canvasWidth?: number;
  canvasHeight?: number;
  visualCount: number;
  visibleTitleVisualCount: number;
  textVisualCount: number;
  slicerCount: number;
  legendVisualCount: number;
  axisLabelVisualCount: number;
  dataLabelVisualCount: number;
  formattedVisualCount: number;
  visuals: VisualMetadataItem[];
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
  visualMetadata?: PageVisualMetadataSummary;
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
  visualMetadata?: PageVisualMetadataSummary;
}

export interface AuditCaptureSummary {
  captureId: string;
  pageName: string;
  stateName?: string;
  fileName: string;
  storedPath: string;
  findingCount: number;
}

export interface AuditFindingDisplay {
  findingId: string;
  captureId: string;
  findingType: AuditFindingType;
  severity: AuditSeverity;
  confidence: AuditConfidence;
  text: string;
  recommendation?: string;
  regionHint?: string;
}

export interface AuditPageState {
  pageName: string;
  captures: AuditCaptureSummary[];
  findings: AuditFindingDisplay[];
}

export interface AuditCoverage {
  totalPages: number;
  pagesWithCaptures: number;
  unmatchedCaptures: number;
  pagesWithFindings: number;
}

export interface AuditState {
  coverage: AuditCoverage;
  pages: AuditPageState[];
  unmatchedCaptures: AuditCaptureSummary[];
  isAnalyzing: boolean;
  providerName?: string;
  providerConfigured: boolean;
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
  | { type: 'revealVisual'; pageName: string; visualId: string }
  | { type: 'uploadScreenshots' }
  | { type: 'attachScreenshot'; pageName: string }
  | { type: 'removeScreenshot'; captureId: string }
  | { type: 'assignCapture'; captureId: string; targetPageName: string }
  | { type: 'analyzeCapture'; captureId: string; pageName: string }
  | { type: 'configureAuditProvider' };

export type ScorePanelHostToWebviewMessage =
  | { type: 'loading' }
  | { type: 'scoreState'; state: ScorePanelState }
  | { type: 'error'; message: string }
  | { type: 'auditState'; audit: AuditState }
  | { type: 'auditAnalyzing'; captureId: string };
