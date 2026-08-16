import type {
  DesignBrief,
  DraftLayoutArtifact,
  DraftNavigationArtifact,
  DraftPageArtifact,
  PageConcept,
  ReportConcept,
} from '../contracts/designStudioModels';
import type { DesignProviderCapability } from './designProviderRegistry';
import {
  DESIGN_PROVIDER_WORKFLOW_CONSTRAINTS,
  designProviderCapabilitySupportsArtifactKind,
} from './designProviderRegistry';

export const DRAFT_PROVIDER_SUPPORTED_ARTIFACT_KINDS = [
  'draftReportArtifact',
  'draftPageArtifact',
  'draftLayoutArtifact',
  'draftNavigationArtifact',
] as const;

export type DraftProviderSupportedArtifactKind = typeof DRAFT_PROVIDER_SUPPORTED_ARTIFACT_KINDS[number];

export type DraftProviderCapabilityPlaceholder = DesignProviderCapability & {
  supportedArtifactKinds: DraftProviderSupportedArtifactKind[];
};

export interface DraftProviderRequest {
  threadId: string;
  brief: DesignBrief;
  concept: ReportConcept;
  pageConcepts: PageConcept[];
}

export interface DraftProviderProposal {
  requestId?: string;
  proposalId?: string;
  capabilityId?: string;
  capabilityKind?: DraftProviderCapabilityPlaceholder['capabilityKind'];
  modelOrEngineName?: string;
  modelOrEngineVersion?: string;
  reportSummary?: string;
  pageStructures?: Record<string, Partial<Pick<DraftPageArtifact, 'structureSummary' | 'recommendedVisualRoles'>>>;
  layoutFrameworks?: Record<string, Partial<Pick<DraftLayoutArtifact, 'layoutType' | 'title' | 'kpiBindings' | 'zones'>>>;
  navigationFramework?: {
    frameworkType?: DraftNavigationArtifact['frameworkType'];
    sectionLabelsByPageConceptId?: Record<string, string>;
  };
  provenanceNotes?: string[];
}

export interface DraftProviderAdapter {
  providerId: string;
  displayName: string;
  capabilities: DraftProviderCapabilityPlaceholder[];
  proposeDraftArtifacts(request: DraftProviderRequest): Promise<DraftProviderProposal>;
}

export function createDraftProviderCapabilityPlaceholder(
  input: Omit<DraftProviderCapabilityPlaceholder, 'supportedArtifactKinds' | 'workflowConstraints'>
    & { supportedArtifactKinds?: DraftProviderSupportedArtifactKind[] },
): DraftProviderCapabilityPlaceholder {
  return {
    ...input,
    supportedArtifactKinds: input.supportedArtifactKinds ?? [...DRAFT_PROVIDER_SUPPORTED_ARTIFACT_KINDS],
    workflowConstraints: DESIGN_PROVIDER_WORKFLOW_CONSTRAINTS,
  };
}

export function draftProviderSupportsArtifactKind(
  capability: DraftProviderCapabilityPlaceholder,
  artifactKind: DraftProviderSupportedArtifactKind,
): boolean {
  return designProviderCapabilitySupportsArtifactKind(capability, artifactKind);
}
