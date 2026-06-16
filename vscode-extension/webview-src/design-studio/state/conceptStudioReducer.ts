import type {
  AlternateConceptComparison,
  AlternateReportConcept,
  DesignArtifactApprovalState,
} from '../../../src/design-studio/contracts/designStudioModels';
import { compareAlternateConcepts } from '../../../src/design-studio/contracts/designStudioModels';

export interface ConceptStudioState {
  briefApprovalState: DesignArtifactApprovalState;
  canGenerateConcepts: boolean;
  conceptId?: string;
  approvalState: DesignArtifactApprovalState;
  alternateConcepts: AlternateReportConcept[];
  preferredBaselineConceptId?: string;
  approvedBaselineConceptId?: string;
  comparison?: AlternateConceptComparison;
}

export type ConceptStudioAction =
  | { type: 'setBriefApproval'; approvalState: DesignArtifactApprovalState }
  | { type: 'generateConcepts' }
  | { type: 'selectBaseline'; conceptId: string }
  | { type: 'submitBaselineForApproval' }
  | { type: 'approveBaseline'; conceptId: string };

function createSampleAlternateConcepts(): AlternateReportConcept[] {
  return [
    {
      id: 'concept-operating-rhythm',
      label: 'Operating-rhythm command deck',
      summary: 'Leads with the operating KPI and then branches into intervention pages.',
      chapterMap: {
        chapters: [
          { id: 'chapter-1', title: 'Decision priorities', objective: 'Show what needs intervention first.', pageRecommendationIds: ['page-1'] },
        ],
      },
      pageRecommendations: [
        { id: 'page-1', title: 'Overview', objective: 'Summarize the decision KPI.', chapterId: 'chapter-1', recommendedKpis: ['Renewal rate'] },
      ],
      kpiHierarchy: {
        nodes: [
          { id: 'kpi-1', label: 'Renewal rate', level: 'primary', childNodeIds: [] },
        ],
        supportingDimensions: ['Region'],
      },
      navigationStructure: {
        pattern: 'hubAndSpoke',
        rationale: 'Keeps the top-level action path tight.',
        sections: [
          { id: 'nav-1', label: 'Priorities', pageRecommendationIds: ['page-1'] },
        ],
      },
      analyticalFlow: {
        steps: [
          { id: 'flow-1', label: 'Find the risk', objective: 'Spot the highest-risk region.', pageRecommendationId: 'page-1' },
        ],
      },
    },
    {
      id: 'concept-narrative',
      label: 'Narrative-first storyline',
      summary: 'Frames the business story first and then supports it with action pages.',
      chapterMap: {
        chapters: [
          { id: 'chapter-2', title: 'Story setup', objective: 'Frame the narrative and the stakes.', pageRecommendationIds: ['page-2'] },
        ],
      },
      pageRecommendations: [
        { id: 'page-2', title: 'Narrative setup', objective: 'Explain the business objective and decision path.', chapterId: 'chapter-2', recommendedKpis: ['Renewal rate'] },
      ],
      kpiHierarchy: {
        nodes: [
          { id: 'kpi-2', label: 'Renewal rate', level: 'primary', childNodeIds: ['kpi-3'] },
          { id: 'kpi-3', label: 'Decision confidence', level: 'diagnostic', childNodeIds: [] },
        ],
        supportingDimensions: ['Region'],
      },
      navigationStructure: {
        pattern: 'linearNarrative',
        rationale: 'Guides the user through a fixed story sequence.',
        sections: [
          { id: 'nav-2', label: 'Story', pageRecommendationIds: ['page-2'] },
        ],
      },
      analyticalFlow: {
        steps: [
          { id: 'flow-2', label: 'Frame the story', objective: 'Explain the stakes before details.', pageRecommendationId: 'page-2' },
        ],
      },
    },
  ];
}

export function createConceptStudioState(): ConceptStudioState {
  return {
    briefApprovalState: 'notSubmitted',
    canGenerateConcepts: false,
    approvalState: 'notSubmitted',
    alternateConcepts: [],
  };
}

export function conceptStudioReducer(
  state: ConceptStudioState,
  action: ConceptStudioAction,
): ConceptStudioState {
  switch (action.type) {
    case 'setBriefApproval':
      return {
        ...state,
        briefApprovalState: action.approvalState,
        canGenerateConcepts: action.approvalState === 'approved',
      };
    case 'generateConcepts': {
      if (!state.canGenerateConcepts) {
        return state;
      }

      const alternateConcepts = createSampleAlternateConcepts();
      return {
        ...state,
        alternateConcepts,
        approvalState: 'notSubmitted',
        preferredBaselineConceptId: undefined,
        approvedBaselineConceptId: undefined,
        comparison: undefined,
      };
    }
    case 'selectBaseline':
      return {
        ...state,
        approvalState: state.approvalState === 'approved' && state.approvedBaselineConceptId === action.conceptId
          ? 'approved'
          : 'notSubmitted',
        preferredBaselineConceptId: action.conceptId,
        approvedBaselineConceptId: state.approvalState === 'approved' && state.approvedBaselineConceptId === action.conceptId
          ? state.approvedBaselineConceptId
          : undefined,
        comparison: compareAlternateConcepts(state.alternateConcepts, action.conceptId),
      };
    case 'submitBaselineForApproval':
      return {
        ...state,
        approvalState: state.preferredBaselineConceptId ? 'pendingApproval' : state.approvalState,
      };
    case 'approveBaseline':
      return {
        ...state,
        approvalState: 'approved',
        preferredBaselineConceptId: action.conceptId,
        approvedBaselineConceptId: action.conceptId,
        comparison: compareAlternateConcepts(state.alternateConcepts, action.conceptId),
      };
    default:
      return state;
  }
}
