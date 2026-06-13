import type { AnalyzerProfileId, AnalyzerType } from '../../analyzer/analyzers/types';
import type { AnalyzableSurface, SurfaceType } from '../../analyzer/surfaces/types';

export const DESIGN_STUDIO_ARTIFACT_KINDS = [
  'designBrief',
  'reportConcept',
  'pageConcept',
  'navigationConcept',
  'kpiHierarchyConcept',
  'draftReportArtifact',
  'draftPageArtifact',
  'draftLayoutArtifact',
  'draftNavigationArtifact',
  'refinementProposal',
  'materializationRequest',
  'materializedSurfaceCandidate',
  'designIterationRecord',
] as const;

export type DesignStudioArtifactKind = typeof DESIGN_STUDIO_ARTIFACT_KINDS[number];

export const DESIGN_STUDIO_LIFECYCLE_STATES = [
  'draft',
  'proposed',
  'reviewed',
  'approved',
  'materialized',
  'analyzed',
  'superseded',
  'archived',
] as const;

export type DesignArtifactLifecycleState = typeof DESIGN_STUDIO_LIFECYCLE_STATES[number];

export const DESIGN_STUDIO_APPROVAL_STATES = [
  'notSubmitted',
  'pendingApproval',
  'approved',
  'rejected',
] as const;

export type DesignArtifactApprovalState = typeof DESIGN_STUDIO_APPROVAL_STATES[number];
export const DESIGN_STUDIO_APPROVAL_KINDS = [
  'designApproval',
  'refinementApproval',
  'validationApproval',
  'materializationApproval',
] as const;

export type DesignArtifactApprovalKind = typeof DESIGN_STUDIO_APPROVAL_KINDS[number];
export type DesignArtifactAuthorSource = 'user' | 'provider' | 'system';
export const DESIGN_STUDIO_MATERIALIZATION_MODES = [
  'conceptToStructurePreview',
  'draftToSurfaceCandidate',
  'refinementProposalToCandidateComparison',
] as const;

export type MaterializationMode = typeof DESIGN_STUDIO_MATERIALIZATION_MODES[number];
export const DESIGN_STUDIO_SOURCE_ROLES = [
  'primary',
  'supporting',
  'comparisonBase',
  'comparisonProposal',
] as const;

export type MaterializationSourceRole = typeof DESIGN_STUDIO_SOURCE_ROLES[number];

export const DESIGN_PROVIDER_CAPABILITY_KINDS = [
  'designAssistance',
  'generationAssistance',
  'screenshotIterationAssistance',
  'semanticModelAwareAssistance',
] as const;

export type DesignProviderCapabilityKind = typeof DESIGN_PROVIDER_CAPABILITY_KINDS[number];

export const DESIGN_STUDIO_REPORT_TYPES = [
  'dashboard',
  'scorecard',
  'narrativeBriefing',
  'operationalMonitoring',
] as const;

export type DesignStudioReportType = typeof DESIGN_STUDIO_REPORT_TYPES[number];

export const DESIGN_STUDIO_REQUIRED_BRIEF_FIELDS = [
  'audience',
  'businessObjective',
  'keyDecisions',
  'primaryKpis',
  'dimensions',
  'intendedStory',
  'successCriteria',
  'reportType',
  'navigationExpectations',
] as const;

export type DesignBriefRequiredField = typeof DESIGN_STUDIO_REQUIRED_BRIEF_FIELDS[number];

export interface DesignArtifactAttribution {
  artifactId: string;
  artifactKind: DesignStudioArtifactKind;
}

export interface DesignArtifactProvenance {
  source: string;
  providerId?: string;
  providerDisplayName?: string;
  providerCapabilityId?: string;
  providerCapabilityKind?: DesignProviderCapabilityKind;
  requestId?: string;
  proposalId?: string;
  modelOrEngineName?: string;
  modelOrEngineVersion?: string;
  timestamp?: string;
  artifactAttribution?: DesignArtifactAttribution;
  notes?: string[];
}

export interface DesignArtifactValidationLinkage {
  analyzerRunId?: string;
  resultReference?: string;
  comparedIterationId?: string;
}

export interface SourceArtifactLineageEntry {
  artifactId: string;
  artifactKind: DesignStudioArtifactKind;
  artifactVersionId: string;
  sourceRole: MaterializationSourceRole;
  approvalState: DesignArtifactApprovalState;
  approvalTimestamp: string;
}

export interface MaterializationProvenanceEntry extends SourceArtifactLineageEntry {
  capturedAt: string;
}

export interface MaterializationAnalyzerHandoffMetadata {
  target: 'analyzerWorkspace';
  requestId: string;
  candidateId: string;
  targetSurfaceType: SurfaceType;
  targetAnalyzer: AnalyzerType;
  targetAnalyzerProfile: AnalyzerProfileId;
  executionState: 'notStarted';
}

export interface DesignArtifactMetadata {
  id: string;
  threadId: string;
  kind: DesignStudioArtifactKind;
  version: number;
  lifecycleState: DesignArtifactLifecycleState;
  approvalState: DesignArtifactApprovalState;
  approvalKind: DesignArtifactApprovalKind;
  createdAt: string;
  updatedAt: string;
  authorSource: DesignArtifactAuthorSource;
  provenance: DesignArtifactProvenance;
  validationLinkage?: DesignArtifactValidationLinkage;
}

export interface DesignBrief extends DesignArtifactMetadata {
  kind: 'designBrief';
  audience: string;
  businessObjective: string;
  keyDecisions: string[];
  primaryKpis: string[];
  dimensions: string[];
  intendedStory: string;
  successCriteria: string[];
  reportType: DesignStudioReportType;
  navigationExpectations: string;
  consumptionContext?: string;
  decisionCadence?: string;
  narrativeRisksOrConstraints?: string[];
  requiredEvidenceDomains?: string[];
  targetAnalyzableSurfaceFamily?: string;
}

export interface ReportConcept extends DesignArtifactMetadata {
  kind: 'reportConcept';
  briefId: string;
  sourceBriefId: string;
  sourceBriefVersionId: string;
  summary: string;
  chapterMap: ReportChapterMapConcept;
  pageRecommendations: PageRecommendationConcept[];
  pageConcepts: PageConcept[];
  kpiHierarchy: KpiHierarchyConcept;
  navigationStructure: NavigationConcept;
  analyticalFlow: AnalyticalFlowConcept;
  alternateConcepts: AlternateReportConcept[];
  preferredBaselineConceptId?: string;
  approvedBaselineConceptId?: string;
  comparison?: AlternateConceptComparison;
}

export interface PageConcept extends DesignArtifactMetadata {
  kind: 'pageConcept';
  reportConceptId: string;
  sourceBriefVersionId: string;
  sourceReportConceptVersionId: string;
  title: string;
  intendedPurpose: string;
  targetAudienceOrRole: string;
  primaryKpis: string[];
  supportingDimensions: string[];
  intendedStoryQuestion: string;
  navigationRole: string;
  relatedChapterId: string;
}

export interface NavigationConcept extends DesignArtifactMetadata {
  kind: 'navigationConcept';
  reportConceptId: string;
  sourceBriefVersionId: string;
  sourceReportConceptVersionId: string;
  pattern: string;
  rationale: string;
  sections: NavigationSectionConcept[];
}

export interface KpiHierarchyConcept extends DesignArtifactMetadata {
  kind: 'kpiHierarchyConcept';
  reportConceptId: string;
  sourceBriefVersionId: string;
  sourceReportConceptVersionId: string;
  nodes: KpiHierarchyNodeConcept[];
  supportingDimensions: string[];
}

export interface ChapterConcept {
  id: string;
  title: string;
  objective: string;
  pageRecommendationIds: string[];
}

export interface ReportChapterMapConcept {
  chapters: ChapterConcept[];
}

export interface PageRecommendationConcept {
  id: string;
  title: string;
  objective: string;
  chapterId: string;
  recommendedKpis: string[];
}

export interface KpiHierarchyNodeConcept {
  id: string;
  label: string;
  level: 'primary' | 'supporting' | 'diagnostic';
  childNodeIds: string[];
}

export interface NavigationSectionConcept {
  id: string;
  label: string;
  pageRecommendationIds: string[];
}

export interface AnalyticalFlowStepConcept {
  id: string;
  label: string;
  objective: string;
  pageRecommendationId: string;
}

export interface AnalyticalFlowConcept {
  steps: AnalyticalFlowStepConcept[];
}

export interface AlternateReportConcept {
  id: string;
  label: string;
  summary: string;
  chapterMap: ReportChapterMapConcept;
  pageRecommendations: PageRecommendationConcept[];
  kpiHierarchy: {
    nodes: KpiHierarchyNodeConcept[];
    supportingDimensions: string[];
  };
  navigationStructure: {
    pattern: string;
    rationale: string;
    sections: NavigationSectionConcept[];
  };
  analyticalFlow: AnalyticalFlowConcept;
}

export interface AlternateConceptDecision {
  conceptId: string;
  label: string;
  disposition: 'preferredBaseline' | 'alternative';
}

export interface AlternateConceptComparison {
  preferredConceptId: string;
  summary: string;
  decisions: AlternateConceptDecision[];
}

export interface ConceptDraftReadiness {
  canEnterDraftStudio: boolean;
  reasons: string[];
}

export interface DraftReportArtifact extends DesignArtifactMetadata {
  kind: 'draftReportArtifact';
  briefId: string;
  conceptId?: string;
  sourceBriefVersionId: string;
  sourceConceptVersionId?: string;
  sourceNavigationConceptVersionId?: string;
  pageArtifactIds: string[];
  layoutArtifactIds: string[];
  navigationArtifactIds: string[];
  summary: string;
  draftStatus: DraftArtifactStatus;
}

export interface DraftPageArtifact extends DesignArtifactMetadata {
  kind: 'draftPageArtifact';
  draftReportArtifactId: string;
  pageConceptId?: string;
  sourceBriefVersionId: string;
  sourceConceptVersionId?: string;
  sourcePageConceptVersionId?: string;
  structureSummary: string;
  recommendedVisualRoles: string[];
  draftStatus: DraftArtifactStatus;
}

export interface DraftLayoutArtifact extends DesignArtifactMetadata {
  kind: 'draftLayoutArtifact';
  draftPageArtifactId: string;
  pageConceptId?: string;
  sourceBriefVersionId: string;
  sourceConceptVersionId?: string;
  sourcePageConceptVersionId?: string;
  layoutType: string;
  title: string;
  kpiBindings: string[];
  zones: string[];
  draftStatus: DraftArtifactStatus;
}

export interface DraftNavigationSectionArtifact {
  id: string;
  label: string;
  pageArtifactId: string;
  pageConceptId?: string;
}

export interface DraftNavigationArtifact extends DesignArtifactMetadata {
  kind: 'draftNavigationArtifact';
  draftReportArtifactId: string;
  navigationConceptId?: string;
  sourceBriefVersionId: string;
  sourceConceptVersionId?: string;
  sourceNavigationConceptVersionId?: string;
  frameworkType: string;
  sections: DraftNavigationSectionArtifact[];
  draftStatus: DraftArtifactStatus;
}

export interface DraftArtifactStatus {
  isolation: 'isolated';
  reviewability: 'reviewable';
  productionState: 'nonProduction';
}

export const REFINEMENT_ANALYZER_SOURCES = [
  'storyAssessment',
  'guidedStoryImprovements',
  'issues',
  'fixPlan',
  'crossPageNarrative',
] as const;

export type RefinementAnalyzerSource = typeof REFINEMENT_ANALYZER_SOURCES[number];
export type RefinementBacklinkArtifactKind =
  | 'pageConcept'
  | 'draftPageArtifact'
  | 'draftLayoutArtifact'
  | 'navigationConcept'
  | 'kpiHierarchyConcept';

export interface CrossPageNarrativeGapSummary {
  id: string;
  title: string;
  summary: string;
  affectedPageNames: string[];
}

export interface CrossPageNarrativeAnalyzerOutput {
  scoreSummary: {
    score: number;
    confidence: 'high' | 'medium' | 'low';
    dominantObjective: string;
  };
  gaps: CrossPageNarrativeGapSummary[];
  narrativePath: string[];
  summary: string;
}

export interface RefinementSourceAnalyzerOutput {
  analyzerSource: RefinementAnalyzerSource;
  analyzerRunId: string;
  resultReference: string;
  reportPath: string;
  scoredAt: string;
  sourceArtifactVersionIds: string[];
  payload: unknown;
}

export interface RefinementNoMutationGuarantee {
  directReportMutation: false;
  materializationTriggered: false;
  analyzerHandoffTriggered: false;
  pbirAssetGenerationTriggered: false;
  analyzableSurfaceCreated: false;
  autoApplied: false;
}

export interface DesignArtifactBacklinkRecord {
  analyzerSource: RefinementAnalyzerSource;
  analyzerReferenceId: string;
  artifactId: string;
  artifactKind: RefinementBacklinkArtifactKind;
  artifactVersionId: string;
  stableIdentity: StableArtifactBacklinkIdentity;
  pageName?: string;
  reason: string;
  linkedFindingIds: string[];
}

export interface StableArtifactBacklinkIdentity {
  designArtifactId: string;
  designArtifactVersionId: string;
  draftArtifactId: string;
  draftArtifactVersionId: string;
}

export interface RefinementProposal extends DesignArtifactMetadata {
  kind: 'refinementProposal';
  sourceArtifactId: string;
  sourceLineage: SourceArtifactLineageEntry[];
  sourceAnalyzerOutput: RefinementSourceAnalyzerOutput;
  affectedArtifactIds: string[];
  affectedArtifactVersionIds: string[];
  suggestedDesignChange: string;
  rationale: string;
  expectedImpact: string;
  linkedFindingIds: string[];
  noMutationGuarantee: RefinementNoMutationGuarantee;
}

export interface MaterializationRequest extends DesignArtifactMetadata {
  kind: 'materializationRequest';
  materializationMode: MaterializationMode;
  sourceArtifactIds: string[];
  sourceLineage: SourceArtifactLineageEntry[];
  targetSurfaceType: SurfaceType;
  targetAnalyzer: AnalyzerType;
  targetAnalyzerProfile: AnalyzerProfileId;
}

export interface MaterializedSurfaceCandidate extends DesignArtifactMetadata {
  kind: 'materializedSurfaceCandidate';
  materializationMode: MaterializationMode;
  sourceArtifactIds: string[];
  sourceLineage: SourceArtifactLineageEntry[];
  targetSurfaceType: SurfaceType;
  derivedSurface: AnalyzableSurface;
  materializationDiagnostics: string[];
  provenanceTrace: MaterializationProvenanceEntry[];
  analyzerHandoff: MaterializationAnalyzerHandoffMetadata;
}

export interface DesignIterationRecord extends DesignArtifactMetadata {
  kind: 'designIterationRecord';
  sourceArtifactVersionIds: string[];
  materializedCandidateId?: string;
  refinementProposalIds: string[];
  comparisonSummary: string;
}

export interface DesignBriefValidationError {
  field: DesignBriefRequiredField | 'approvalState';
  message: string;
}

export interface DesignBriefValidationResult {
  isValid: boolean;
  canGenerateConcepts: boolean;
  errors: DesignBriefValidationError[];
}

export type DesignBriefDraftInput = Omit<
  DesignBrief,
  | 'id'
  | 'threadId'
  | 'kind'
  | 'version'
  | 'lifecycleState'
  | 'approvalState'
  | 'approvalKind'
  | 'createdAt'
  | 'updatedAt'
  | 'authorSource'
  | 'provenance'
  | 'validationLinkage'
>;

function hasNonEmptyString(value: string): boolean {
  return value.trim().length > 0;
}

function hasNonEmptyItems(values: string[]): boolean {
  return values.some((value) => value.trim().length > 0);
}

export function createSourceArtifactLineageEntry(
  artifact: Pick<DesignArtifactMetadata, 'id' | 'version' | 'approvalState' | 'updatedAt' | 'kind'>,
  options?: {
    sourceRole?: MaterializationSourceRole;
  },
): SourceArtifactLineageEntry {
  return {
    artifactId: artifact.id,
    artifactKind: artifact.kind,
    artifactVersionId: `${artifact.id}@v${artifact.version}`,
    sourceRole: options?.sourceRole ?? 'supporting',
    approvalState: artifact.approvalState,
    approvalTimestamp: artifact.updatedAt,
  };
}

export function validateDesignBrief(brief: DesignBrief): DesignBriefValidationResult {
  const errors: DesignBriefValidationError[] = [];

  if (!hasNonEmptyString(brief.audience)) {
    errors.push({ field: 'audience', message: 'Audience is required.' });
  }
  if (!hasNonEmptyString(brief.businessObjective)) {
    errors.push({ field: 'businessObjective', message: 'Business objective is required.' });
  }
  if (!hasNonEmptyItems(brief.keyDecisions)) {
    errors.push({ field: 'keyDecisions', message: 'At least one key decision is required.' });
  }
  if (!hasNonEmptyItems(brief.primaryKpis)) {
    errors.push({ field: 'primaryKpis', message: 'At least one primary KPI is required.' });
  }
  if (!hasNonEmptyItems(brief.dimensions)) {
    errors.push({ field: 'dimensions', message: 'At least one dimension is required.' });
  }
  if (!hasNonEmptyString(brief.intendedStory)) {
    errors.push({ field: 'intendedStory', message: 'Intended story is required.' });
  }
  if (!hasNonEmptyItems(brief.successCriteria)) {
    errors.push({ field: 'successCriteria', message: 'At least one success criterion is required.' });
  }
  if (!hasNonEmptyString(brief.navigationExpectations)) {
    errors.push({ field: 'navigationExpectations', message: 'Navigation expectations are required.' });
  }

  const isValid = errors.length === 0;
  const isApproved = brief.approvalState === 'approved';

  if (isValid && !isApproved) {
    errors.push({
      field: 'approvalState',
      message: 'Design Brief must be approved before concept generation can proceed.',
    });
  }

  return {
    isValid,
    canGenerateConcepts: isValid && isApproved,
    errors,
  };
}

export function compareAlternateConcepts(
  alternateConcepts: AlternateReportConcept[],
  preferredConceptId?: string,
): AlternateConceptComparison {
  const preferred = alternateConcepts.find((concept) => concept.id === preferredConceptId)
    ?? alternateConcepts[0];

  return {
    preferredConceptId: preferred?.id ?? '',
    summary: preferred
      ? `Baseline concept selected: ${preferred.label}.`
      : 'No alternate concepts are available for comparison.',
    decisions: alternateConcepts.map((concept) => ({
      conceptId: concept.id,
      label: concept.label,
      disposition: concept.id === preferred?.id ? 'preferredBaseline' : 'alternative',
    })),
  };
}

export function evaluateConceptDraftReadiness(reportConcept: ReportConcept): ConceptDraftReadiness {
  const reasons: string[] = [];

  if (!reportConcept.preferredBaselineConceptId) {
    reasons.push('A preferred baseline must be selected before Draft Studio review.');
  }
  if (!reportConcept.approvedBaselineConceptId) {
    reasons.push('A concept baseline must be explicitly approved before Draft Studio can proceed.');
  }
  if (reportConcept.approvedBaselineConceptId && reportConcept.approvedBaselineConceptId !== reportConcept.preferredBaselineConceptId) {
    reasons.push('The approved concept baseline must match the currently selected preferred baseline.');
  }

  return {
    canEnterDraftStudio: reasons.length === 0,
    reasons,
  };
}
