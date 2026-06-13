import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type * as vscode from 'vscode';
import type {
  DesignArtifactAttribution,
  DesignArtifactMetadata,
  DesignArtifactProvenance,
  DesignBrief,
  DesignStudioArtifactKind,
  DraftArtifactStatus,
  DraftLayoutArtifact,
  DraftNavigationArtifact,
  DraftNavigationSectionArtifact,
  DraftPageArtifact,
  DraftReportArtifact,
  PageConcept,
  ReportConcept,
} from '../contracts/designStudioModels';
import type {
  DraftProviderAdapter,
  DraftProviderCapabilityPlaceholder,
  DraftProviderProposal,
} from '../providers/draftProviderAdapter';
import { loadConceptState } from './conceptStore';
import { loadDesignBriefState } from './designBriefStore';

export interface DraftHistoryEntry {
  version: number;
  savedAt: string;
  draft: DraftReportArtifact;
  pageArtifacts: DraftPageArtifact[];
  layoutArtifacts: DraftLayoutArtifact[];
  navigationArtifacts: DraftNavigationArtifact[];
}

export interface DraftState {
  threadId: string;
  brief: DesignBrief;
  concept: ReportConcept;
  currentDraft: DraftReportArtifact;
  pageArtifacts: DraftPageArtifact[];
  layoutArtifacts: DraftLayoutArtifact[];
  navigationArtifacts: DraftNavigationArtifact[];
  history: DraftHistoryEntry[];
  providerCapabilities: DraftProviderCapabilityPlaceholder[];
}

interface PersistedDraftState {
  threadId: string;
  brief: DesignBrief;
  concept: ReportConcept;
  currentDraft: DraftReportArtifact;
  pageArtifacts: DraftPageArtifact[];
  layoutArtifacts: DraftLayoutArtifact[];
  navigationArtifacts: DraftNavigationArtifact[];
  history: DraftHistoryEntry[];
  providerCapabilities: DraftProviderCapabilityPlaceholder[];
}

interface DraftBuildResult {
  draft: DraftReportArtifact;
  pageArtifacts: DraftPageArtifact[];
  layoutArtifacts: DraftLayoutArtifact[];
  navigationArtifacts: DraftNavigationArtifact[];
}

interface DraftSourceVersionReferences {
  sourceBriefVersionId: string;
  sourceConceptVersionId: string;
  sourceNavigationConceptVersionId: string;
}

const DEFAULT_DRAFT_STATUS: DraftArtifactStatus = {
  isolation: 'isolated',
  reviewability: 'reviewable',
  productionState: 'nonProduction',
};

function threadKey(threadId: string): string {
  return crypto.createHash('md5').update(threadId).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(context.globalStorageUri.fsPath, 'design-studio', 'threads', threadKey(threadId));
}

function manifestPath(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(sessionDir(context, threadId), 'draft-studio.json');
}

function readPersistedState(filePath: string): PersistedDraftState | undefined {
  try {
    return JSON.parse(fs.readFileSync(filePath, 'utf8')) as PersistedDraftState;
  } catch {
    return undefined;
  }
}

function writePersistedState(filePath: string, state: PersistedDraftState): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(state, null, 2), 'utf8');
}

function toArtifactVersionId(artifact: Pick<DesignArtifactMetadata, 'id' | 'version'>): string {
  return `${artifact.id}@v${artifact.version}`;
}

function createAttributedProvenance(
  provenance: DesignArtifactProvenance,
  artifactId: string,
  artifactKind: DesignArtifactMetadata['kind'],
): DesignArtifactProvenance {
  const attribution: DesignArtifactAttribution = {
    artifactId,
    artifactKind,
  };

  return {
    ...provenance,
    artifactAttribution: attribution,
  };
}

function createProvenance(adapter?: DraftProviderAdapter, proposal?: DraftProviderProposal): DesignArtifactProvenance {
  const timestamp = new Date().toISOString();
  if (!adapter) {
    return {
      source: 'system',
      timestamp,
      notes: ['Generated without provider assistance.', 'Draft Studio output remains isolated and non-production.'],
    };
  }

  return {
    source: 'provider',
    providerId: adapter.providerId,
    providerDisplayName: adapter.displayName,
    providerCapabilityId: proposal?.capabilityId ?? adapter.capabilities[0]?.capabilityId,
    providerCapabilityKind: proposal?.capabilityKind ?? adapter.capabilities[0]?.capabilityKind,
    requestId: proposal?.requestId,
    proposalId: proposal?.proposalId,
    modelOrEngineName: proposal?.modelOrEngineName,
    modelOrEngineVersion: proposal?.modelOrEngineVersion,
    timestamp,
    notes: [
      `Provider adapter used: ${adapter.displayName}.`,
      ...(proposal?.provenanceNotes ?? []),
      'Provider output remains advisory-only and non-production.',
    ],
  };
}

function createMetadata<K extends DesignStudioArtifactKind>(
  id: string,
  threadId: string,
  kind: K,
  version: number,
  createdAt: string,
  updatedAt: string,
  provenance: DesignArtifactProvenance,
  authorSource: DesignArtifactMetadata['authorSource'],
): DesignArtifactMetadata & { kind: K } {
  return {
    id,
    threadId,
    kind,
    version,
    lifecycleState: 'draft',
    approvalState: 'pendingApproval',
    approvalKind: 'designApproval',
    createdAt,
    updatedAt,
    authorSource,
    provenance,
  };
}

function createSourceVersionReferences(
  brief: DesignBrief,
  concept: ReportConcept,
): DraftSourceVersionReferences {
  return {
    sourceBriefVersionId: toArtifactVersionId(brief),
    sourceConceptVersionId: toArtifactVersionId(concept),
    sourceNavigationConceptVersionId: toArtifactVersionId(concept.navigationStructure),
  };
}

function inferRecommendedVisualRoles(pageConcept: PageConcept): string[] {
  const roles = ['headlineKpi'];

  if (pageConcept.primaryKpis.length > 1) {
    roles.push('comparison');
  }
  if (pageConcept.supportingDimensions.length > 0) {
    roles.push('breakdown');
  }
  if (pageConcept.navigationRole === 'entry') {
    roles.push('narrativeSummary');
  }

  return roles;
}

function buildPageArtifacts(
  threadId: string,
  reportArtifactId: string,
  brief: DesignBrief,
  concept: ReportConcept,
  pageConcepts: PageConcept[],
  version: number,
  createdAt: string,
  updatedAt: string,
  provenance: DesignArtifactProvenance,
  authorSource: DesignArtifactMetadata['authorSource'],
  proposal?: DraftProviderProposal,
): DraftPageArtifact[] {
  return pageConcepts.map((pageConcept) => {
    const override = proposal?.pageStructures?.[pageConcept.id];
    return {
      ...createMetadata(
        `draft-page:${threadId}:${pageConcept.id}`,
        threadId,
        'draftPageArtifact',
        version,
        createdAt,
        updatedAt,
        createAttributedProvenance(provenance, `draft-page:${threadId}:${pageConcept.id}`, 'draftPageArtifact'),
        authorSource,
      ),
      draftReportArtifactId: reportArtifactId,
      pageConceptId: pageConcept.id,
      sourceBriefVersionId: toArtifactVersionId(brief),
      sourceConceptVersionId: toArtifactVersionId(concept),
      sourcePageConceptVersionId: toArtifactVersionId(pageConcept),
      structureSummary: override?.structureSummary
        ?? `${pageConcept.title} draft structure frames ${pageConcept.intendedPurpose.toLowerCase()}.`,
      recommendedVisualRoles: override?.recommendedVisualRoles ?? inferRecommendedVisualRoles(pageConcept),
      draftStatus: DEFAULT_DRAFT_STATUS,
    };
  });
}

function buildLayoutArtifacts(
  threadId: string,
  brief: DesignBrief,
  concept: ReportConcept,
  pageArtifacts: DraftPageArtifact[],
  pageConcepts: PageConcept[],
  version: number,
  createdAt: string,
  updatedAt: string,
  provenance: DesignArtifactProvenance,
  authorSource: DesignArtifactMetadata['authorSource'],
  proposal?: DraftProviderProposal,
): DraftLayoutArtifact[] {
  return pageArtifacts.map((pageArtifact) => {
    const pageConcept = pageConcepts.find((candidate) => candidate.id === pageArtifact.pageConceptId);
    const override = pageArtifact.pageConceptId ? proposal?.layoutFrameworks?.[pageArtifact.pageConceptId] : undefined;

    return {
      ...createMetadata(
        `draft-layout:${threadId}:${pageArtifact.pageConceptId ?? pageArtifact.id}`,
        threadId,
        'draftLayoutArtifact',
        version,
        createdAt,
        updatedAt,
        createAttributedProvenance(
          provenance,
          `draft-layout:${threadId}:${pageArtifact.pageConceptId ?? pageArtifact.id}`,
          'draftLayoutArtifact',
        ),
        authorSource,
      ),
      draftPageArtifactId: pageArtifact.id,
      pageConceptId: pageArtifact.pageConceptId,
      sourceBriefVersionId: toArtifactVersionId(brief),
      sourceConceptVersionId: toArtifactVersionId(concept),
      sourcePageConceptVersionId: pageConcept ? toArtifactVersionId(pageConcept) : undefined,
      layoutType: override?.layoutType ?? (pageConcept?.navigationRole === 'entry' ? 'heroKpiGrid' : 'detailAnalysisGrid'),
      title: override?.title ?? `${pageConcept?.title ?? 'Draft page'} layout`,
      kpiBindings: override?.kpiBindings ?? pageConcept?.primaryKpis ?? [],
      zones: override?.zones ?? ['header', 'primaryCanvas', 'supportingCanvas'],
      draftStatus: DEFAULT_DRAFT_STATUS,
    };
  });
}

function buildNavigationSections(
  pageArtifacts: DraftPageArtifact[],
  pageConcepts: PageConcept[],
  proposal?: DraftProviderProposal,
): DraftNavigationSectionArtifact[] {
  return pageArtifacts.map((pageArtifact) => {
    const pageConcept = pageConcepts.find((candidate) => candidate.id === pageArtifact.pageConceptId);
    return {
      id: `draft-nav-section:${pageArtifact.id}`,
      label: proposal?.navigationFramework?.sectionLabelsByPageConceptId?.[pageConcept?.id ?? '']
        ?? pageConcept?.title
        ?? 'Draft page',
      pageArtifactId: pageArtifact.id,
      pageConceptId: pageConcept?.id,
    };
  });
}

function buildDraftArtifacts(
  threadId: string,
  brief: DesignBrief,
  concept: ReportConcept,
  version: number,
  existingCreatedAt: string | undefined,
  adapter?: DraftProviderAdapter,
  proposal?: DraftProviderProposal,
): DraftBuildResult {
  const now = new Date().toISOString();
  const createdAt = existingCreatedAt ?? now;
  const reportArtifactId = `draft-report:${threadId}`;
  const provenance = createProvenance(adapter, proposal);
  const authorSource = adapter ? 'provider' : 'system';
  const sourceVersions = createSourceVersionReferences(brief, concept);
  const pageArtifacts = buildPageArtifacts(
    threadId,
    reportArtifactId,
    brief,
    concept,
    concept.pageConcepts,
    version,
    createdAt,
    now,
    provenance,
    authorSource,
    proposal,
  );
  const layoutArtifacts = buildLayoutArtifacts(
    threadId,
    brief,
    concept,
    pageArtifacts,
    concept.pageConcepts,
    version,
    createdAt,
    now,
    provenance,
    authorSource,
    proposal,
  );
  const navigationArtifact: DraftNavigationArtifact = {
    ...createMetadata(
      `draft-navigation:${threadId}`,
      threadId,
      'draftNavigationArtifact',
      version,
      createdAt,
      now,
      createAttributedProvenance(provenance, `draft-navigation:${threadId}`, 'draftNavigationArtifact'),
      authorSource,
    ),
    draftReportArtifactId: reportArtifactId,
    navigationConceptId: concept.navigationStructure.id,
    sourceBriefVersionId: sourceVersions.sourceBriefVersionId,
    sourceConceptVersionId: sourceVersions.sourceConceptVersionId,
    sourceNavigationConceptVersionId: sourceVersions.sourceNavigationConceptVersionId,
    frameworkType: proposal?.navigationFramework?.frameworkType ?? concept.navigationStructure.pattern,
    sections: buildNavigationSections(pageArtifacts, concept.pageConcepts, proposal),
    draftStatus: DEFAULT_DRAFT_STATUS,
  };
  const draft: DraftReportArtifact = {
    ...createMetadata(
      reportArtifactId,
      threadId,
      'draftReportArtifact',
      version,
      createdAt,
      now,
      createAttributedProvenance(provenance, reportArtifactId, 'draftReportArtifact'),
      authorSource,
    ),
    briefId: brief.id,
    conceptId: concept.id,
    sourceBriefVersionId: sourceVersions.sourceBriefVersionId,
    sourceConceptVersionId: sourceVersions.sourceConceptVersionId,
    sourceNavigationConceptVersionId: sourceVersions.sourceNavigationConceptVersionId,
    pageArtifactIds: pageArtifacts.map((artifact) => artifact.id),
    layoutArtifactIds: layoutArtifacts.map((artifact) => artifact.id),
    navigationArtifactIds: [navigationArtifact.id],
    summary: proposal?.reportSummary ?? `Draft structure based on approved concept baseline: ${concept.summary}`,
    draftStatus: DEFAULT_DRAFT_STATUS,
  };

  return {
    draft,
    pageArtifacts,
    layoutArtifacts,
    navigationArtifacts: [navigationArtifact],
  };
}

function toState(persisted: PersistedDraftState): DraftState {
  return persisted;
}

export async function loadDraftState(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<DraftState | undefined> {
  const filePath = manifestPath(context, threadId);
  if (!fs.existsSync(filePath)) {
    return undefined;
  }

  const persisted = readPersistedState(filePath);
  return persisted ? toState(persisted) : undefined;
}

export async function generateDraftArtifacts(
  context: vscode.ExtensionContext,
  threadId: string,
  options?: {
    adapter?: DraftProviderAdapter;
  },
): Promise<DraftState> {
  const briefState = await loadDesignBriefState(context, threadId);
  if (!briefState?.validation.canGenerateConcepts || briefState.current.approvalState !== 'approved') {
    throw new Error('Draft generation requires an approved Design Brief.');
  }

  const conceptState = await loadConceptState(context, threadId);
  if (!conceptState?.readiness.canEnterDraftStudio || conceptState.currentConcept.approvalState !== 'approved') {
    throw new Error('Draft generation requires an approved Concept baseline.');
  }

  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  const version = (existing?.currentDraft.version ?? 0) + 1;
  const proposal = options?.adapter
    ? await options.adapter.proposeDraftArtifacts({
        threadId,
        brief: briefState.current,
        concept: conceptState.currentConcept,
        pageConcepts: conceptState.currentConcept.pageConcepts,
      })
    : undefined;
  const build = buildDraftArtifacts(
    threadId,
    briefState.current,
    conceptState.currentConcept,
    version,
    existing?.currentDraft.createdAt,
    options?.adapter,
    proposal,
  );
  const persisted: PersistedDraftState = {
    threadId,
    brief: briefState.current,
    concept: conceptState.currentConcept,
    currentDraft: build.draft,
    pageArtifacts: build.pageArtifacts,
    layoutArtifacts: build.layoutArtifacts,
    navigationArtifacts: build.navigationArtifacts,
    history: [
      ...(existing?.history ?? []),
      {
        version: build.draft.version,
        savedAt: build.draft.updatedAt,
        draft: build.draft,
        pageArtifacts: build.pageArtifacts,
        layoutArtifacts: build.layoutArtifacts,
        navigationArtifacts: build.navigationArtifacts,
      },
    ],
    providerCapabilities: options?.adapter?.capabilities ?? [],
  };

  writePersistedState(filePath, persisted);
  return toState(persisted);
}
