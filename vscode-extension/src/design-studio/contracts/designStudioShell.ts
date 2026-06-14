import type {
  DesignArtifactApprovalKind,
  DesignArtifactApprovalState,
  MaterializationHandoffEligibility,
} from './designStudioModels';

export const DESIGN_STUDIO_WORKFLOW_STAGE_IDS = [
  'brief',
  'concept',
  'draft',
  'refinement',
  'materialize',
  'handoff',
  'compare',
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
}

export interface DesignStudioAnalyzerHandoffViewModel {
  requestId: string;
  readinessLabel: string;
  analyzerId: string;
  analyzerProfileId: string;
  canOpen: boolean;
  diagnostics: string[];
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

export interface DesignStudioWorkspaceViewModel {
  reportLabel: string;
  currentStage: DesignStudioWorkflowStageId;
  stages: DesignStudioWorkflowStageViewModel[];
  currentStageSummary: DesignStudioStageSummary;
  approvalCards: DesignStudioApprovalCardViewModel[];
  materializationReadiness?: DesignStudioMaterializationReadinessViewModel;
  analyzerHandoff?: DesignStudioAnalyzerHandoffViewModel;
  refinementExperience?: DesignStudioRefinementExperienceViewModel;
}
