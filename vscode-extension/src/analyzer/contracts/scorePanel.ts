import type { DesignAnalyzerConfig } from '../config/types';

export type FindingType = 'objective' | 'strongHeuristic' | 'stylePreference';
export type AuditFindingType = 'objective' | 'strongHeuristic' | 'stylePreference';
export type AuditSeverity = 'critical' | 'warning' | 'info';
export type AuditConfidence = 'high' | 'medium' | 'low';
export type ChartIntentConfidence = 'high' | 'medium' | 'low';
export type StoryConfidence = 'high' | 'medium' | 'low';
export type NormalizedFindingSeverity = 'high' | 'medium' | 'low' | 'info';
export type NormalizedFindingScope = 'visual' | 'page' | 'crossPage' | 'report';
export type NormalizedFindingDetectionType = 'deterministic' | 'aiAssisted' | 'mixed';
export type NormalizedFindingImpactArea =
  | 'layout'
  | 'storytelling'
  | 'accessibility'
  | 'governance'
  | 'density'
  | 'navigation'
  | 'kpiEffectiveness'
  | 'benchmark'
  | 'actionability'
  | 'metadata';
export type IntentFeedbackConfirmation = 'yes' | 'partial' | 'no';
export type PageIntentProfileType = 'executive' | 'operational' | 'analytical' | 'appendix';
export type ReviewerPersona = 'coach' | 'consultant' | 'executiveReviewer' | 'strictDesignCritic';
export type ReviewPresentationPersona = 'default' | 'executive' | 'consultant' | 'governance' | 'accessibility';
export type AuditIssueSource = 'renderedLayout' | 'metadataModel';

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

export interface NormalizedFindingEvidenceReference {
  kind: 'framework' | 'audit' | 'metadata' | 'consistency' | 'quickFix' | 'benchmark' | 'actionability';
  label: string;
  pageName?: string;
  frameworkKey?: string;
  visualId?: string;
  detail?: string;
}

export interface NormalizedFinding {
  id: string;
  title: string;
  summary: string;
  severity: NormalizedFindingSeverity;
  confidence: number;
  scope: NormalizedFindingScope;
  detectionType: NormalizedFindingDetectionType;
  affectedPages: string[];
  impactArea: NormalizedFindingImpactArea;
  frameworkImpact: string[];
  recommendation: string;
  sourceKind: string;
  sourceSection: 'issues' | 'evidence';
  evidence: NormalizedFindingEvidenceReference[];
}

export interface SemanticColorAssignment {
  semanticKey: string;
  displayLabel?: string;
  color: string;
  sourceVisualId: string;
  sourcePageName: string;
}

export interface ChartIntentSummary {
  intent: string;
  confidence?: ChartIntentConfidence;
  evidence: string[];
  fitStatus?: string;
  recommendedAlternatives: string[];
}

export interface ReportConsistencySummary {
  consistentTitleAnchors: boolean;
  consistentFilterBand: boolean;
  consistentMetricLabels: boolean;
  consistentSemanticColors: boolean;
  overallFinding?: string;
  affectedPages: string[];
  issueCount: number;
  issues: ReportConsistencyFinding[];
  findings: string[];
}

export interface ReportConsistencyFinding {
  category: string;
  issueCategory: string;
  overallFinding: string;
  affectedPages: string[];
  severity: 'high' | 'medium' | 'low';
  confidence: 'high' | 'medium' | 'low';
  recommendedRemediation: string;
}

export interface PageStorySummary {
  intentProfile: string;
  storyArchetype: string;
  inferredStory: string;
  confidence: StoryConfidence;
  evidence: string[];
}

export interface PageIntentProfile {
  inferredProfile: PageIntentProfileType;
  actionabilityExpectation: 'high' | 'medium' | 'low';
  reviewGuidance: string[];
  evidence: string[];
}

export interface ActionabilityBreakdown {
  score: number;
  targetBenchmarkPresent: boolean;
  exceptionVisibility: boolean;
  urgencySignaling: boolean;
  priorPeriodContext: boolean;
  drillPathPresent: boolean;
  expectationLevel: 'high' | 'medium' | 'low';
  strengths: string[];
  gaps: string[];
  summary: string;
}

export interface BenchmarkComparisonSummary {
  archetype: string;
  benchmarkLabel: string;
  comparativePosition: 'above' | 'mixed' | 'below';
  beautifulButUseless: boolean;
  insight: string;
  strengths: string[];
  gaps: string[];
}

export interface IntentFeedbackEntry {
  pageId?: string;
  pageName: string;
  inferredIntent: string;
  storyArchetype?: string;
  userConfirmation: IntentFeedbackConfirmation;
  note?: string;
  timestamp: string;
  analyzerVersion: string;
  reportSessionId: string;
  inferenceConfidence?: StoryConfidence;
}

export type ReviewWorkflowStatus = 'confirmed' | 'partial' | 'mismatch' | 'unreviewed';

export interface ReviewWorkflowExportPage {
  pageName: string;
  reviewStatus: ReviewWorkflowStatus;
  inferredIntent?: string;
  storyArchetype?: string;
  inferredStory?: string;
  inferenceConfidence?: StoryConfidence;
  reviewerNote?: string;
  reviewedAt?: string;
  analyzerVersion?: string;
}

export interface ReviewWorkflowExportSummary {
  totalPages: number;
  reviewedPages: number;
  confirmedPages: number;
  partialPages: number;
  mismatchPages: number;
  unreviewedPages: number;
}

export interface ReviewWorkflowExecutiveSummary {
  overallStatus: 'Ready for export' | 'Needs review' | 'In progress';
  headline: string;
  reviewCoveragePercent: number;
  maturityStatement: string;
  topStrengths: string[];
  topRisks: string[];
  topRecommendedActions: string[];
}

export interface ReviewWorkflowIntentValidationSummary {
  confirmedPages: ReviewWorkflowExportPage[];
  partialPages: ReviewWorkflowExportPage[];
  mismatchPages: ReviewWorkflowExportPage[];
  unreviewedPages: ReviewWorkflowExportPage[];
  pagesNeedingReview: ReviewWorkflowExportPage[];
}

export interface ReviewWorkflowRemediationItem {
  pageName: string;
  reviewStatus: Exclude<ReviewWorkflowStatus, 'confirmed' | 'unreviewed'>;
  reason: string;
  suggestedAction: string;
}

export interface ReviewWorkflowCrossPageConsistencyRollup {
  overallFinding?: string;
  issueCount: number;
  affectedPages: string[];
  issuesByCategory: Array<[string, number]>;
  highestSeverity?: 'high' | 'medium' | 'low';
  remediation: string[];
}

export interface ReviewWorkflowPriorityRecommendation {
  title: string;
  severity: 'high' | 'medium' | 'low';
  affectedPages: string[];
  issueCategory: string;
  remediationGuidance: string;
}

export interface ReviewWorkflowAppendix {
  frameworkScores: Array<{ framework: string; score: number }>;
  metadataDerivedFindings: string[];
  methodologyNotes: string[];
}

export interface ReviewWorkflowAnalyzerMetadata {
  analyzerName: string;
  analyzerVersion: string;
  packetVersion: string;
}

export type ReviewWorkflowExportProfile = 'consultant' | 'executive' | 'governance';
export type ReviewWorkflowMarkdownTemplateVariant = 'standard' | 'brandedConsultant';
export type OverviewMaturityBand = 'Emerging' | 'Developing' | 'Mature' | 'Advanced';
export type OverviewRiskBand = 'Low' | 'Moderate' | 'Elevated' | 'High';
export type FixPlanEffort = 'low' | 'medium' | 'high';

export interface ReviewWorkflowMarkdownBranding {
  clientName?: string;
  reviewerName?: string;
  engagementName?: string;
  confidentiality?: string;
}

export interface ReviewWorkflowMarkdownRenderOptions {
  templateVariant?: ReviewWorkflowMarkdownTemplateVariant;
  branding?: ReviewWorkflowMarkdownBranding;
}

export interface ReviewWorkflowExportData {
  reportPath: string;
  scoredAt: string;
  exportedAt: string;
  compositeScore: number;
  pageCount: number;
  analyzerMetadata: ReviewWorkflowAnalyzerMetadata;
  reviewSummary: ReviewWorkflowExportSummary;
  executiveSummary: ReviewWorkflowExecutiveSummary;
  intentValidationSummary: ReviewWorkflowIntentValidationSummary;
  remediationQueue: ReviewWorkflowRemediationItem[];
  topRecommendations: string[];
  priorityRecommendations: ReviewWorkflowPriorityRecommendation[];
  crossPageConsistencyRollup?: ReviewWorkflowCrossPageConsistencyRollup;
  appendix: ReviewWorkflowAppendix;
  pages: ReviewWorkflowExportPage[];
  crossPageConsistency?: {
    overallFinding?: string;
    issueCount: number;
    affectedPages: string[];
    findings: string[];
  };
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
  semanticColors: SemanticColorAssignment[];
  chartIntent?: ChartIntentSummary;
}

export interface PageVisualMetadataSummary {
  pageName: string;
  visiblePageTitle?: string;
  strictVisiblePageTitle?: string;
  canvasWidth?: number;
  canvasHeight?: number;
  semanticColorMap: SemanticColorAssignment[];
  chartIntentSummary?: ChartIntentSummary;
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
  reportConsistencyNotes?: string[];
  inferredStorySummary?: PageStorySummary;
  pageIntentProfile?: PageIntentProfile;
  actionabilityBreakdown?: ActionabilityBreakdown;
  benchmarkComparison?: BenchmarkComparisonSummary;
  scoringError?: string;
  frameworkWeights?: Record<string, number>;
  visualMetadata?: PageVisualMetadataSummary;
}

export interface OverviewInsight {
  id: string;
  title: string;
  detail: string;
  affectedPages: string[];
  severity?: NormalizedFindingSeverity;
  sourceFindingIds: string[];
}

export interface OverviewAction {
  id: string;
  title: string;
  detail: string;
  severity: NormalizedFindingSeverity;
  affectedPages: string[];
  sourceFindingIds: string[];
}

export interface SeverityDistribution {
  high: number;
  medium: number;
  low: number;
  info: number;
}

export interface CrossPageSummary {
  headline: string;
  details: string[];
  consistentPages: number;
  totalPages: number;
}

export type CrossPageMatrixDimension =
  | 'layout'
  | 'story'
  | 'accessibility'
  | 'consistency'
  | 'navigation'
  | 'actionability';

export interface CrossPageMatrixCell {
  pageName: string;
  dimension: CrossPageMatrixDimension;
  score?: number;
  severity?: NormalizedFindingSeverity;
  findingCount: number;
  highSeverityCount: number;
  confidenceAverage?: number;
  status: 'strong' | 'watch' | 'weak' | 'unknown';
  relatedFindingIds: string[];
  summary: string;
}

export interface CrossPageMatrixRow {
  pageName: string;
  cells: CrossPageMatrixCell[];
}

export interface CrossPageMatrixSummary {
  dimensions: CrossPageMatrixDimension[];
  rows: CrossPageMatrixRow[];
}

export interface OverviewSummary {
  overallScore: number;
  maturityBand: OverviewMaturityBand;
  riskBand: OverviewRiskBand;
  benchmarkSummary: string;
  executiveSummary: string;
  severityDistribution: SeverityDistribution;
  topStrengths: OverviewInsight[];
  topWeaknesses: OverviewInsight[];
  topIssues: OverviewInsight[];
  topActions: OverviewAction[];
  crossPageSummary: CrossPageSummary;
}

export interface FixPlanItem {
  id: string;
  title: string;
  detail: string;
  severity: NormalizedFindingSeverity;
  effort: FixPlanEffort;
  scope: NormalizedFindingScope;
  affectedPages: string[];
  recommendedAction: string;
  sourceFindingIds: string[];
}

export interface ReviewPresentationPersonaProfile {
  id: ReviewPresentationPersona;
  label: string;
  description: string;
  emphasizedImpactAreas: NormalizedFindingImpactArea[];
  emphasizedScopes: NormalizedFindingScope[];
  defaultSeverityFilter?: NormalizedFindingSeverity[];
  defaultDetectionTypes?: NormalizedFindingDetectionType[];
  overviewEmphasis: Array<'issues' | 'actions' | 'strengths' | 'weaknesses' | 'benchmark' | 'consistency'>;
  fixPlanEmphasis: Array<'severity' | 'effort' | 'scope' | 'evidence' | 'crossPage'>;
}

export interface PersonaPresentationState {
  activePersona: ReviewPresentationPersona;
  availablePersonas: ReviewPresentationPersonaProfile[];
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
  reportConsistencySummary?: ReportConsistencySummary;
  inferredStorySummary?: PageStorySummary;
  pageIntentProfile?: PageIntentProfile;
  actionabilityBreakdown?: ActionabilityBreakdown;
  benchmarkComparison?: BenchmarkComparisonSummary;
  layoutScore?: number;
  themeScore?: number;
  governanceScore?: number;
  frameworkWeights?: Record<string, number>;
  visualMetadata?: PageVisualMetadataSummary;
  normalizedFindings?: NormalizedFinding[];
  overviewSummary?: OverviewSummary;
  fixPlan?: FixPlanItem[];
  crossPageMatrix?: CrossPageMatrixSummary;
  personaPresentation?: PersonaPresentationState;
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
  issueSource?: AuditIssueSource;
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
  intentFeedback: IntentFeedbackEntry[];
  reviewPacketPreview?: ReviewWorkflowExportData;
  reviewPacketPreviewHtml?: string;
  reviewPacketPreviewProfile?: ReviewWorkflowExportProfile;
  reviewPacketPreviewTemplateVariant?: ReviewWorkflowMarkdownTemplateVariant;
}

export type ScorePanelWebviewToHostMessage =
  | { type: 'webviewReady' }
  | { type: 'refresh' }
  | { type: 'selectTab'; pageIndex: number }
  | {
    type: 'setIntentFeedback';
    pageName: string;
    inferredIntent: string;
    storyArchetype?: string;
    userConfirmation: IntentFeedbackConfirmation;
    inferenceConfidence?: StoryConfidence;
    note?: string;
  }
  | { type: 'revealVisual'; pageName: string; visualId: string }
  | { type: 'uploadScreenshots' }
  | { type: 'attachScreenshot'; pageName: string }
  | { type: 'removeScreenshot'; captureId: string }
  | { type: 'assignCapture'; captureId: string; targetPageName: string }
  | { type: 'analyzeCapture'; captureId: string; pageName: string }
  | { type: 'exportReviewWorkflow' }
  | { type: 'setReviewPacketPreviewProfile'; profile: ReviewWorkflowExportProfile }
  | { type: 'setReviewPacketPreviewTemplateVariant'; templateVariant: ReviewWorkflowMarkdownTemplateVariant }
  | { type: 'openReviewPacketPreview' }
  | { type: 'openSettings' };

export type ScorePanelHostToWebviewMessage =
  | { type: 'loading' }
  | { type: 'scoreState'; state: ScorePanelState }
  | { type: 'error'; message: string }
  | { type: 'auditState'; audit: AuditState }
  | { type: 'auditAnalyzing'; captureId: string };
