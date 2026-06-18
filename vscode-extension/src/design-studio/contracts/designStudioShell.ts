import type {
  AlternateConceptComparison,
  AlternateReportConcept,
  DesignArtifactApprovalKind,
  DesignArtifactApprovalState,
  DesignStudioWorkflowCompletionState,
  IterationCompletionChecklistItem,
  MaterializationHandoffEligibility,
  RecommendationState,
} from './designStudioModels';

export const DESIGN_STUDIO_WORKFLOW_STAGE_IDS = [
  'brief',
  'concept',
  'draft',
  'refinement',
  'materialize',
  'handoff',
  'compare',
  'completion',
] as const;

export type DesignStudioWorkflowStageId = typeof DESIGN_STUDIO_WORKFLOW_STAGE_IDS[number];

export const DESIGN_STUDIO_WORKFLOW_STAGE_STATUSES = [
  'notStarted',
  'inProgress',
  'ready',
  'approved',
  'blocked',
] as const;

export type DesignStudioWorkflowStageStatus = typeof DESIGN_STUDIO_WORKFLOW_STAGE_STATUSES[number];

export interface DesignStudioWorkflowStageViewModel {
  id: DesignStudioWorkflowStageId;
  label: string;
  status: DesignStudioWorkflowStageStatus;
  readinessLabel: string;
  title: string;
  description: string;
}

export interface DesignStudioStageSummary {
  title: string;
  description: string;
}

export interface DesignStudioApprovalCardViewModel {
  kind: DesignArtifactApprovalKind;
  title: string;
  approvalState: DesignArtifactApprovalState;
  owner: string;
  unlock: string;
  nonEffects: string[];
}

export interface DesignStudioMaterializationReadinessViewModel {
  readinessLabel: string;
  executableEligibility: MaterializationHandoffEligibility;
  targetAnalyzer: string;
  targetAnalyzerProfile: string;
  diagnostics: string[];
  candidateId?: string;
  requestId?: string;
  candidateStatusLabel?: string;
  materializationStatus?: string;
  nextStepGuidance?: string;
  canCreateCandidate?: boolean;
  canSubmitCandidateForApproval?: boolean;
  canApproveCandidate?: boolean;
  sourceDraftVersionId?: string;
  sourceConceptVersionId?: string;
  sourceDesignBriefVersionId?: string;
  lineage?: DesignStudioPrepareForReviewLineageItemViewModel[];
  approvalsUsed?: string[];
}

export interface DesignStudioPrepareForReviewLineageItemViewModel {
  label: string;
  artifactVersionId: string;
  approvalState: DesignArtifactApprovalState;
}

export interface DesignStudioAnalyzerHandoffViewModel {
  requestId: string;
  readinessLabel: string;
  analyzerId: string;
  analyzerProfileId: string;
  canOpen: boolean;
  diagnostics: string[];
}

export interface DesignStudioReviewDesignViewModel {
  requestId: string;
  candidateId?: string;
  sourceDraftVersionId?: string;
  sourceConceptVersionId?: string;
  sourceDesignBriefVersionId?: string;
  approvedReviewCandidateVersionId?: string;
  reviewReadinessLabel: string;
  handoffStatusLabel: string;
  reviewStatusLabel: string;
  completionStatusLabel: string;
  analyzerId: string;
  analyzerProfileId: string;
  readinessDiagnostics: string[];
  ownershipMessages: string[];
  nextStepGuidance: string;
  canOpenAnalyzerWorkspace: boolean;
  canMarkReviewCompleted: boolean;
  canAttachAnalyzerResults?: boolean;
  resultStatusLabel?: string;
  availableResults?: DesignStudioAnalyzerResultSummaryViewModel[];
}

export interface DesignStudioAnalyzerResultSummaryViewModel {
  analyzerSourceLabel: string;
  analyzerRunId: string;
  resultReference: string;
  scoredAt: string;
  sourceCandidateId: string;
  sourceArtifactVersionFingerprint: string[];
  validationResultStatusLabel: string;
  validationApprovalStateLabel: string;
  linkedRecommendationCount: number;
}

export const DESIGN_STUDIO_REFINEMENT_GROUP_IDS = [
  'story',
  'layout',
  'kpi',
  'navigation',
  'structure',
] as const;

export type DesignStudioRefinementGroupId = typeof DESIGN_STUDIO_REFINEMENT_GROUP_IDS[number];
export type DesignStudioRefinementProposalAction = 'approve' | 'reject' | 'defer';

export interface DesignStudioRefinementProposalComparisonViewModel {
  originalDesignIntent: string;
  currentDesignState: string;
  proposedRefinement: string;
}

export interface DesignStudioRefinementProposalViewModel {
  id: string;
  title: string;
  summary: string;
  recommendation: string;
  rationale: string;
  expectedImpact: string;
  approvalState: DesignArtifactApprovalState;
  recommendationState?: RecommendationState;
  sourceAnalyzerLabel: string;
  affectedArtifacts: string[];
  supportingEvidence: string[];
  comparison: DesignStudioRefinementProposalComparisonViewModel;
  availableActions: DesignStudioRefinementProposalAction[];
}

export interface DesignStudioRefinementGroupViewModel {
  id: DesignStudioRefinementGroupId;
  title: string;
  summary: string;
  proposals: DesignStudioRefinementProposalViewModel[];
}

export interface DesignStudioRefinementExperienceViewModel {
  title: string;
  summary: string;
  groups: DesignStudioRefinementGroupViewModel[];
  emptyState?: string;
}

export interface DesignStudioConceptChapterViewModel {
  title: string;
  objective: string;
}

export interface DesignStudioConceptKpiNodeViewModel {
  label: string;
  level: 'primary' | 'supporting' | 'diagnostic';
  depth: number;
}

export interface DesignStudioConceptNavigationNodeViewModel {
  label: string;
  depth: number;
}

export interface DesignStudioConceptFlowStepViewModel {
  label: string;
  objective: string;
}

export interface DesignStudioConceptComparisonDomainViewModel {
  baselineItems: string[];
  comparisonItems: string[];
}

export interface DesignStudioConceptComparisonViewModel {
  comparisonConceptLabel: string;
  chapterStructure: DesignStudioConceptComparisonDomainViewModel;
  kpiHierarchy: DesignStudioConceptComparisonDomainViewModel;
  navigationStructure: DesignStudioConceptComparisonDomainViewModel;
  analyticalFlow: DesignStudioConceptComparisonDomainViewModel;
}

export interface DesignStudioConceptReviewViewModel {
  title: string;
  summary: string;
  conceptId?: string;
  approvalState?: DesignArtifactApprovalState;
  alternateConcepts?: AlternateReportConcept[];
  comparison?: AlternateConceptComparison;
  preferredBaselineConceptId?: string;
  approvedBaselineConceptId?: string;
  selectedConceptLabel: string;
  chapterStructure: DesignStudioConceptChapterViewModel[];
  kpiHierarchy: DesignStudioConceptKpiNodeViewModel[];
  navigationStructure: DesignStudioConceptNavigationNodeViewModel[];
  analyticalFlow: DesignStudioConceptFlowStepViewModel[];
  comparisons?: DesignStudioConceptComparisonViewModel[];
}

export interface DesignStudioDraftPageReviewViewModel {
  title: string;
  structureSummary: string;
  kpiPlacement: string[];
}

export interface DesignStudioDraftLayoutReviewViewModel {
  title: string;
  layoutType: string;
  zones: string[];
}

export interface DesignStudioDraftNavigationReviewViewModel {
  label: string;
  pageTitle: string;
}

export interface DesignStudioDraftReviewViewModel {
  title: string;
  summary: string;
  draftId?: string;
  approvalState?: DesignArtifactApprovalState;
  draftStatusLabel: string;
  draftPages: DesignStudioDraftPageReviewViewModel[];
  draftLayouts: DesignStudioDraftLayoutReviewViewModel[];
  draftNavigation: DesignStudioDraftNavigationReviewViewModel[];
}

export interface DesignStudioWorkflowCompletionViewModel {
  state: DesignStudioWorkflowCompletionState;
  checklist: IterationCompletionChecklistItem[];
  outstandingItems: string[];
  approvalsSatisfied: string[];
  deferredRecommendationCount: number;
  unresolvedRecommendationCount: number;
  nextStepGuidance: string;
  completedAt?: string;
  completedBy?: string;
  reopenedAt?: string;
  reopenedBy?: string;
  canCompleteIteration: boolean;
  canReopenIteration: boolean;
}

export interface DesignStudioWorkspaceViewModel {
  reportLabel: string;
  currentStage: DesignStudioWorkflowStageId;
  stages: DesignStudioWorkflowStageViewModel[];
  currentStageSummary: DesignStudioStageSummary;
  approvalCards: DesignStudioApprovalCardViewModel[];
  materializationReadiness?: DesignStudioMaterializationReadinessViewModel;
  analyzerHandoff?: DesignStudioAnalyzerHandoffViewModel;
  reviewDesign?: DesignStudioReviewDesignViewModel;
  refinementExperience?: DesignStudioRefinementExperienceViewModel;
  conceptReview?: DesignStudioConceptReviewViewModel;
  draftReview?: DesignStudioDraftReviewViewModel;
  workflowCompletion?: DesignStudioWorkflowCompletionViewModel;
}
