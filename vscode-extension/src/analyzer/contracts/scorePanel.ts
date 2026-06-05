import type { DesignAnalyzerConfig } from '../config/types';
import type { AnalyzerProfileId, AnalyzerType } from '../analyzers/types';
import type { SurfaceType } from '../surfaces/types';

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
  kind:
    | 'framework'
    | 'audit'
    | 'metadata'
    | 'consistency'
    | 'quickFix'
    | 'benchmark'
    | 'actionability'
    | 'readiness'
    | 'typescriptLayout'
    | 'navigation'
    | 'designToken'
    | 'screenshot'
    | 'semanticModel';
  label: string;
  pageName?: string;
  frameworkKey?: string;
  visualId?: string;
  detail?: string;
  filePath?: string;
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
export type FixPlanImpact = 'low' | 'medium' | 'high';

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
  pagePurposeAnalysis?: PagePurposeAnalysisSummary;
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
  readinessSummary?: FabricAppReadinessOverviewSummary;
}

export interface AnalysisContextMetadata {
  surfaceType: SurfaceType;
  analyzerType: AnalyzerType;
  analyzerProfile: AnalyzerProfileId;
  surfaceDisplayName: string;
  sourceLocation: string;
  availableAnalyzerTypes: AnalyzerType[];
  availableAnalyzerProfiles: AnalyzerProfileId[];
}

export type FabricAppReadinessBand =
  | 'strongCandidate'
  | 'possibleCandidate'
  | 'redesignRequired'
  | 'keepAsReport';

export type FabricAppPageCandidateState = FabricAppReadinessBand;
export type FabricAppRedesignEffort = 'low' | 'medium' | 'high';
export type FabricAppReadinessEvidenceKind =
  | 'pbirMetadata'
  | 'interaction'
  | 'navigation'
  | 'screenshot'
  | 'semanticModel'
  | 'portability';

export interface FabricAppReadinessDimensionScores {
  layoutPortability: number;
  interactionPortability: number;
  narrativePortability: number;
  semanticModelSuitability: number;
  navigationPortability: number;
  governancePortability: number;
  accessibilityPortability: number;
  visualizationAsCodeOpportunity: number;
}

export interface FabricAppReadinessEvidence {
  kind: FabricAppReadinessEvidenceKind;
  label: string;
  detail: string;
  pageName?: string;
}

export interface AnalyticsGovernanceSignal {
  category: 'navigation' | 'accessibility' | 'storytelling' | 'semanticModel';
  severity: 'low' | 'medium' | 'high';
  summary: string;
  pageName?: string;
}

export interface FabricAppPageReadinessAssessment {
  pageName: string;
  readinessScore: number;
  readinessDimensions: FabricAppReadinessDimensionScores;
  candidateState: FabricAppPageCandidateState;
  positiveSignals: string[];
  blockers: string[];
  unsupportedPatterns: string[];
  redesignRequiredAreas: string[];
  migrationNotes: string[];
  evidence: FabricAppReadinessEvidence[];
}

export interface FabricAppReadinessOverviewSummary {
  readinessScore: number;
  readinessBand: FabricAppReadinessBand;
  candidatePageCount: number;
  migrationBlockerCount: number;
  estimatedRedesignEffort: FabricAppRedesignEffort;
}

export interface FabricAppReadinessAssessment {
  overallReadinessScore: number;
  readinessBand: FabricAppReadinessBand;
  migrationSummary: string;
  candidatePages: string[];
  blockers: string[];
  unsupportedPatterns: string[];
  redesignRequiredAreas: string[];
  recommendedNextActions: string[];
  estimatedRedesignEffort: FabricAppRedesignEffort;
  dimensionScores: FabricAppReadinessDimensionScores;
  pageAssessments: FabricAppPageReadinessAssessment[];
  evidence: FabricAppReadinessEvidence[];
  governanceSignals: AnalyticsGovernanceSignal[];
}

export interface FabricAppReviewEvidence {
  kind: 'typescriptLayout' | 'navigation' | 'designToken' | 'screenshot' | 'semanticModel';
  label: string;
  summary: string;
  filePath: string;
  pageName?: string;
  stateName?: string;
}

export interface FabricAppReviewSummary {
  qualityScore: number;
  summary: string;
  remediationGuidance: string[];
  evidence: FabricAppReviewEvidence[];
}

export interface FixPlanItem {
  id: string;
  title: string;
  detail: string;
  severity: NormalizedFindingSeverity;
  effort: FixPlanEffort;
  impact: FixPlanImpact;
  why: string;
  scope: NormalizedFindingScope;
  affectedPages: string[];
  recommendedAction: string;
  resolvedOutcomes: string[];
  sourceFindingIds: string[];
}

// Phase 3 advisory output is presentation-only. It can improve proposal wording,
// rationale, and prioritization, but it must never carry executable mutation authority.
export type ProposalEnricherId =
  | 'layout'
  | 'theme'
  | 'navigation'
  | 'storytelling'
  | 'executiveReadability'
  | 'accessibility';

export interface EnrichedTitleSuggestion {
  title: string;
  confidence: number;
  rationale: string;
}

export interface EnrichedExplanation {
  shortText: string;
  expandedText?: string;
}

export interface EnrichedImpactSummary {
  text: string;
}

export interface AdvisoryPriority {
  tier: 'highLeverage' | 'quickWin' | 'consistencyCleanup' | 'advisoryOnly';
  rationale: string;
}

export interface ExpectedOutcomeNarrative {
  text: string;
  areas: string[];
}

export interface AdvisoryAlternative {
  title: string;
  description: string;
}

export type ProposalEnrichmentValidationCode =
  | 'unsupportedSurface'
  | 'inventedArtifact'
  | 'contradictoryPriority'
  | 'executionLeak'
  | 'outcomeOverclaim'
  | 'semanticRewrite';

export interface ProposalEnrichmentValidationIssue {
  code: ProposalEnrichmentValidationCode;
  message: string;
  section?: 'titleSuggestions' | 'explanation' | 'whyThisMatters' | 'advisoryPriority' | 'expectedOutcome' | 'advisoryAlternatives';
}

export interface ProposalEnrichmentValidationResult {
  status: 'passed' | 'degraded' | 'rejected';
  issues: ProposalEnrichmentValidationIssue[];
}

export interface ProposalEnrichmentProvenance {
  providerName?: string;
  usedFallback: boolean;
  enrichedAt: string;
  sourceFindingIds: string[];
}

export interface ProposalEnrichment {
  remediationItemId: string;
  status: 'available' | 'fallback' | 'rejected' | 'skipped';
  source: 'provider' | 'fallback';
  enrichersApplied: ProposalEnricherId[];
  titleSuggestions?: EnrichedTitleSuggestion[];
  explanation?: EnrichedExplanation;
  whyThisMatters?: EnrichedImpactSummary;
  advisoryPriority?: AdvisoryPriority;
  expectedOutcome?: ExpectedOutcomeNarrative;
  advisoryAlternatives: AdvisoryAlternative[];
  validation: ProposalEnrichmentValidationResult;
  provenance: ProposalEnrichmentProvenance;
}

export type FixOpportunityCategory =
  | 'title'
  | 'semanticColor'
  | 'alignment'
  | 'spacing'
  | 'grid'
  | 'navigation'
  | 'crossPageConsistency';

export type FixMutationType =
  | 'setTitleText'
  | 'setPosition'
  | 'setSize'
  | 'setSemanticColor'
  | 'setThemeRole'
  | 'setNavigationPlacement';

export type FixOpportunityState =
  | 'Previewed'
  | 'Approved'
  | 'Applied'
  | 'RolledBack'
  | 'Stale'
  | 'FailedValidation'
  | 'AppliedWithUnexpectedOutcome';

export type FixOutcomeStatus = 'Resolved' | 'Improved' | 'Unchanged' | 'Unexpected';

export interface FixMutation {
  id: string;
  pageName?: string;
  targetObjectId: string;
  targetFile: string;
  propertyPath: string;
  mutationType: FixMutationType;
  before: unknown;
  after: unknown;
}

export interface RollbackFileBackup {
  targetFile: string;
  beforeContent: string;
}

export interface RollbackPlan {
  id: string;
  fixOpportunityId: string;
  fileBackups: RollbackFileBackup[];
  reverseMutations: FixMutation[];
}

export interface FixPreviewRow {
  pageName?: string;
  objectId: string;
  property: string;
  before: unknown;
  after: unknown;
}

export interface FixOutcomeEntry {
  findingId: string;
  title: string;
  status: FixOutcomeStatus;
}

export interface FixGroupedOutcomeStatusSummary {
  status: FixOutcomeStatus;
  count: number;
  opportunityIds: string[];
}

export interface FixGroupedOutcomeSummary {
  totalEntries: number;
  statuses: FixGroupedOutcomeStatusSummary[];
  appliedWithUnexpectedOutcomeOpportunityIds: string[];
}

export interface FixOutcomeSummary {
  entries: FixOutcomeEntry[];
  groupedSummary?: FixGroupedOutcomeSummary;
}

export type FixConflictCode =
  | 'overlappingMutation'
  | 'incompatibleCategory'
  | 'staleOpportunity'
  | 'targetDrifted'
  | 'missingRollbackCoverage';

export interface FixConflictReason {
  code: FixConflictCode;
  message: string;
  opportunityIds: string[];
  targetObjectId?: string;
  propertyPath?: string;
}

export interface FixCompatibilityResult {
  isCompatible: boolean;
  compatibleOpportunityIds: string[];
  blockingOpportunityIds: string[];
  blockingReasons: FixConflictReason[];
}

export interface FixBatchPreviewPropertyChange {
  opportunityId: string;
  property: string;
  before: unknown;
  after: unknown;
}

export interface FixBatchPreviewObjectGroup {
  pageName?: string;
  objectId: string;
  propertyChanges: FixBatchPreviewPropertyChange[];
}

export interface FixBatchPreviewPageGroup {
  pageName: string;
  objectGroups: FixBatchPreviewObjectGroup[];
}

export interface FixBatchPreviewSummary {
  changedFileCount: number;
  changedObjectCount: number;
  expectedOutcomeCount: number;
  touchedFiles: string[];
  changedObjects: string[];
}

export interface FixBatchPreview {
  opportunityIds: string[];
  summary: FixBatchPreviewSummary;
  pageGroups: FixBatchPreviewPageGroup[];
  mutationFacts: FixPreviewRow[];
  expectedOutcomes: string[];
}

export type FixSelectionApprovalState = 'NeedsPreview' | 'Previewed' | 'Approved';

export interface FixSelectionState {
  selectedOpportunityIds: string[];
  compatibility: FixCompatibilityResult;
  groupedPreview?: FixBatchPreview;
  approvalState: FixSelectionApprovalState;
  message?: string;
}

export interface FixSessionRollbackRecord {
  rolledBackAt: string;
  state: 'RolledBack' | 'RollbackFailed';
}

export interface FixApplySessionRecord {
  id: string;
  appliedAt: string;
  opportunityIds: string[];
  opportunityTitles: string[];
  rollbackAvailable: boolean;
  rollbackHistory: FixSessionRollbackRecord[];
  groupedOutcomeSummary?: FixGroupedOutcomeSummary;
  staleOpportunityIds?: string[];
  regeneratedOpportunityIds?: string[];
}

export interface FixApplyResult {
  opportunityId: string;
  state: FixOpportunityState;
  appliedMutationCount: number;
  validationErrors: string[];
}

export interface FixBatchApplyResult {
  state: FixOpportunityState;
  opportunityIds: string[];
  appliedMutationCount: number;
  validationErrors: string[];
  applyOrder: string[];
  session?: FixApplySessionRecord;
}

export interface FixOpportunity {
  id: string;
  remediationItemId: string;
  title: string;
  category: FixOpportunityCategory;
  summary: string;
  confidence: number;
  safetyClass: 'safe';
  affectedPages: string[];
  targetObjectIds: string[];
  sourceFindingIds: string[];
  expectedResolutions: string[];
  mutations: FixMutation[];
  previewRows: FixPreviewRow[];
  rollbackPlan: RollbackPlan;
  state: FixOpportunityState;
  outcome?: FixOutcomeSummary;
}

export interface PagePurposeAnalysisSummary {
  inferredPurpose: string;
  confidence?: StoryConfidence;
  actionabilityScore?: number;
  benchmarkStatus?: string;
  topGaps: string[];
  whyThisMatters: string;
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
  pagePurposeAnalysis?: PagePurposeAnalysisSummary;
  normalizedFindings?: NormalizedFinding[];
  overviewSummary?: OverviewSummary;
  fixPlan?: FixPlanItem[];
  proposalEnrichments?: ProposalEnrichment[];
  fixOpportunities?: FixOpportunity[];
  crossPageMatrix?: CrossPageMatrixSummary;
  personaPresentation?: PersonaPresentationState;
  analysisContext?: AnalysisContextMetadata;
  readinessAssessment?: FabricAppReadinessAssessment;
  fabricAppReview?: FabricAppReviewSummary;
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
  fixSelection?: FixSelectionState;
  fixApplySessions?: FixApplySessionRecord[];
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
  | { type: 'toggleFixOpportunitySelection'; opportunityId: string }
  | { type: 'previewSelectedFixOpportunities' }
  | { type: 'approveSelectedFixOpportunities' }
  | { type: 'applySelectedFixOpportunities' }
  | { type: 'rollbackFixSession'; sessionId: string }
  | { type: 'regenerateFixOpportunities'; opportunityIds?: string[] }
  | { type: 'approveFixOpportunity'; opportunityId: string }
  | { type: 'applyFixOpportunity'; opportunityId: string }
  | { type: 'rollbackFixOpportunity'; opportunityId: string }
  | { type: 'openSettings' };

export type ScorePanelHostToWebviewMessage =
  | { type: 'loading' }
  | { type: 'scoreState'; state: ScorePanelState }
  | { type: 'error'; message: string }
  | { type: 'auditState'; audit: AuditState }
  | { type: 'auditAnalyzing'; captureId: string };
