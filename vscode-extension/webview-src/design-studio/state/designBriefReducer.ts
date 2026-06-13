import type {
  DesignArtifactApprovalState,
  DesignStudioReportType,
} from '../../../src/design-studio/contracts/designStudioModels';
import { validateDesignBrief } from '../../../src/design-studio/contracts/designStudioModels';

export interface DesignBriefEditorState {
  audience: string;
  businessObjective: string;
  keyDecisions: string;
  primaryKpis: string;
  dimensions: string;
  intendedStory: string;
  successCriteria: string;
  reportType: DesignStudioReportType;
  navigationExpectations: string;
  consumptionContext: string;
  decisionCadence: string;
  narrativeRisksOrConstraints: string;
  requiredEvidenceDomains: string;
  targetAnalyzableSurfaceFamily: string;
  approvalState: DesignArtifactApprovalState;
  validationMessages: string[];
  canGenerateConcepts: boolean;
}

export type DesignBriefEditorAction =
  | { type: 'setField'; field: keyof Omit<DesignBriefEditorState, 'approvalState' | 'validationMessages' | 'canGenerateConcepts'>; value: string }
  | { type: 'validate' }
  | { type: 'markApprovalRequested' }
  | { type: 'markApproved' };

function parseLines(value: string): string[] {
  return value
    .split('\n')
    .map((entry) => entry.trim())
    .filter((entry) => entry.length > 0);
}

function validateEditorState(state: DesignBriefEditorState) {
  return validateDesignBrief({
    id: 'editor-brief',
    threadId: 'editor-thread',
    kind: 'designBrief',
    version: 1,
    lifecycleState: state.approvalState === 'approved' ? 'approved' : 'draft',
    approvalState: state.approvalState,
    approvalKind: 'designApproval',
    createdAt: '2026-06-12T00:00:00.000Z',
    updatedAt: '2026-06-12T00:00:00.000Z',
    authorSource: 'user',
    provenance: { source: 'user' },
    audience: state.audience,
    businessObjective: state.businessObjective,
    keyDecisions: parseLines(state.keyDecisions),
    primaryKpis: parseLines(state.primaryKpis),
    dimensions: parseLines(state.dimensions),
    intendedStory: state.intendedStory,
    successCriteria: parseLines(state.successCriteria),
    reportType: state.reportType,
    navigationExpectations: state.navigationExpectations,
    consumptionContext: state.consumptionContext || undefined,
    decisionCadence: state.decisionCadence || undefined,
    narrativeRisksOrConstraints: parseLines(state.narrativeRisksOrConstraints),
    requiredEvidenceDomains: parseLines(state.requiredEvidenceDomains),
    targetAnalyzableSurfaceFamily: state.targetAnalyzableSurfaceFamily || undefined,
  });
}

function withValidation(state: DesignBriefEditorState): DesignBriefEditorState {
  const validation = validateEditorState(state);
  return {
    ...state,
    validationMessages: validation.errors.map((error) => error.message),
    canGenerateConcepts: validation.canGenerateConcepts,
  };
}

export function createInitialDesignBriefState(): DesignBriefEditorState {
  return withValidation({
    audience: '',
    businessObjective: '',
    keyDecisions: '',
    primaryKpis: '',
    dimensions: '',
    intendedStory: '',
    successCriteria: '',
    reportType: 'dashboard',
    navigationExpectations: '',
    consumptionContext: '',
    decisionCadence: '',
    narrativeRisksOrConstraints: '',
    requiredEvidenceDomains: '',
    targetAnalyzableSurfaceFamily: '',
    approvalState: 'notSubmitted',
    validationMessages: [],
    canGenerateConcepts: false,
  });
}

export function designBriefReducer(
  state: DesignBriefEditorState,
  action: DesignBriefEditorAction,
): DesignBriefEditorState {
  switch (action.type) {
    case 'setField':
      return withValidation({
        ...state,
        [action.field]: action.value,
      });
    case 'validate':
      return withValidation(state);
    case 'markApprovalRequested':
      return withValidation({
        ...state,
        approvalState: 'pendingApproval',
      });
    case 'markApproved':
      return withValidation({
        ...state,
        approvalState: 'approved',
      });
    default:
      return state;
  }
}
