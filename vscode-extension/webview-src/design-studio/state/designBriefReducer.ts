import type {
  DesignBrief,
  DesignBriefDraftInput,
  DesignBriefValidationError,
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
  approvalState: DesignBrief['approvalState'];
  validationErrors: DesignBriefValidationError[];
  validationMessages: string[];
  isValid: boolean;
  canGenerateConcepts: boolean;
}

export type DesignBriefEditorAction =
  | { type: 'setField'; field: keyof Omit<DesignBriefEditorState, 'approvalState' | 'validationErrors' | 'validationMessages' | 'isValid' | 'canGenerateConcepts'>; value: string }
  | { type: 'validate' }
  | { type: 'markSubmitted' }
  | { type: 'markApproved' }
  | { type: 'hydrate'; brief?: DesignBrief };

function parseLines(value: string): string[] {
  return value
    .split('\n')
    .map((entry) => entry.trim())
    .filter((entry) => entry.length > 0);
}

function createBriefPayload(state: DesignBriefEditorState): DesignBrief {
  return {
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
  };
}

function withValidation(state: Omit<DesignBriefEditorState, 'validationErrors' | 'validationMessages' | 'isValid' | 'canGenerateConcepts'>): DesignBriefEditorState {
  const validation = validateDesignBrief(createBriefPayload({
    ...state,
    validationErrors: [],
    validationMessages: [],
    isValid: false,
    canGenerateConcepts: false,
  }));

  return {
    ...state,
    validationErrors: validation.errors,
    validationMessages: validation.errors.map((error) => error.message),
    isValid: validation.isValid,
    canGenerateConcepts: validation.canGenerateConcepts,
  };
}

function fromBrief(brief?: DesignBrief): DesignBriefEditorState {
  if (!brief) {
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
    });
  }

  return withValidation({
    audience: brief.audience,
    businessObjective: brief.businessObjective,
    keyDecisions: brief.keyDecisions.join('\n'),
    primaryKpis: brief.primaryKpis.join('\n'),
    dimensions: brief.dimensions.join('\n'),
    intendedStory: brief.intendedStory,
    successCriteria: brief.successCriteria.join('\n'),
    reportType: brief.reportType,
    navigationExpectations: brief.navigationExpectations,
    consumptionContext: brief.consumptionContext ?? '',
    decisionCadence: brief.decisionCadence ?? '',
    narrativeRisksOrConstraints: (brief.narrativeRisksOrConstraints ?? []).join('\n'),
    requiredEvidenceDomains: (brief.requiredEvidenceDomains ?? []).join('\n'),
    targetAnalyzableSurfaceFamily: brief.targetAnalyzableSurfaceFamily ?? '',
    approvalState: brief.approvalState,
  });
}

export function createInitialDesignBriefState(brief?: DesignBrief): DesignBriefEditorState {
  return fromBrief(brief);
}

export function toDesignBriefDraftInput(state: DesignBriefEditorState): DesignBriefDraftInput {
  return {
    audience: state.audience.trim(),
    businessObjective: state.businessObjective.trim(),
    keyDecisions: parseLines(state.keyDecisions),
    primaryKpis: parseLines(state.primaryKpis),
    dimensions: parseLines(state.dimensions),
    intendedStory: state.intendedStory.trim(),
    successCriteria: parseLines(state.successCriteria),
    reportType: state.reportType,
    navigationExpectations: state.navigationExpectations.trim(),
    consumptionContext: state.consumptionContext.trim() || undefined,
    decisionCadence: state.decisionCadence.trim() || undefined,
    narrativeRisksOrConstraints: parseLines(state.narrativeRisksOrConstraints),
    requiredEvidenceDomains: parseLines(state.requiredEvidenceDomains),
    targetAnalyzableSurfaceFamily: state.targetAnalyzableSurfaceFamily.trim() || undefined,
  };
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
        approvalState: state.approvalState === 'approved' ? 'approved' : 'notSubmitted',
      });
    case 'validate':
      return withValidation(state);
    case 'markSubmitted':
      return withValidation({
        ...state,
        approvalState: 'pendingApproval',
      });
    case 'markApproved':
      return withValidation({
        ...state,
        approvalState: 'approved',
      });
    case 'hydrate':
      return fromBrief(action.brief);
    default:
      return state;
  }
}
