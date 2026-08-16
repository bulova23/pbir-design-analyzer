import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type * as vscode from 'vscode';
import type {
  AlternateConceptComparison,
  AlternateReportConcept,
  ConceptDraftReadiness,
  DesignBrief,
  PageConcept,
  ReportConcept,
} from '../contracts/designStudioModels';
import { compareAlternateConcepts, evaluateConceptDraftReadiness } from '../contracts/designStudioModels';
import { loadDesignBriefState } from './designBriefStore';

export interface ConceptHistoryEntry {
  version: number;
  savedAt: string;
  concept: ReportConcept;
}

export interface ConceptState {
  threadId: string;
  briefId: string;
  currentConcept: ReportConcept;
  history: ConceptHistoryEntry[];
  readiness: ConceptDraftReadiness;
}

interface PersistedConceptState {
  threadId: string;
  briefId: string;
  currentConcept: ReportConcept;
  history: ConceptHistoryEntry[];
}

function threadKey(threadId: string): string {
  return crypto.createHash('md5').update(threadId).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(context.globalStorageUri.fsPath, 'design-studio', 'threads', threadKey(threadId));
}

function manifestPath(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(sessionDir(context, threadId), 'concept-studio.json');
}

function readPersistedState(filePath: string): PersistedConceptState | undefined {
  try {
    return JSON.parse(fs.readFileSync(filePath, 'utf8')) as PersistedConceptState;
  } catch {
    return undefined;
  }
}

function writePersistedState(filePath: string, state: PersistedConceptState): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(state, null, 2), 'utf8');
}

function listToSentence(values: string[]): string {
  return values.filter((value) => value.trim().length > 0).join(', ');
}

function toArtifactVersionId(artifact: Pick<DesignBrief | ReportConcept | PageConcept, 'id' | 'version'>): string {
  return `${artifact.id}@v${artifact.version}`;
}

function inferNavigationRole(pageId: string, index: number): string {
  if (index === 0 || pageId.includes('overview') || pageId.includes('storyline')) {
    return 'entry';
  }
  if (pageId.includes('proof') || pageId.includes('drivers')) {
    return 'supporting';
  }

  return 'decision';
}

function buildAlternateConcepts(brief: DesignBrief): AlternateReportConcept[] {
  const primaryKpis = brief.primaryKpis.length > 0 ? brief.primaryKpis : ['Primary KPI'];
  const dimensions = brief.dimensions.length > 0 ? brief.dimensions : ['Primary dimension'];

  return [
    {
      id: 'concept-operating-rhythm',
      label: 'Operating-rhythm command deck',
      summary: `Starts with ${primaryKpis[0]} and pivots quickly into action zones by ${dimensions[0]}.`,
      chapterMap: {
        chapters: [
          {
            id: 'chapter-priorities',
            title: 'Decision priorities',
            objective: 'Show where intervention is needed first.',
            pageRecommendationIds: ['page-overview', 'page-hotspots'],
          },
          {
            id: 'chapter-drivers',
            title: 'Driver analysis',
            objective: 'Explain why the KPI is moving.',
            pageRecommendationIds: ['page-drivers'],
          },
        ],
      },
      pageRecommendations: [
        {
          id: 'page-overview',
          title: 'Executive overview',
          objective: `Summarize ${primaryKpis[0]} against the current target.`,
          chapterId: 'chapter-priorities',
          recommendedKpis: [primaryKpis[0]],
        },
        {
          id: 'page-hotspots',
          title: `${dimensions[0]} hotspots`,
          objective: `Rank the highest-risk ${dimensions[0].toLowerCase()} segments for action.`,
          chapterId: 'chapter-priorities',
          recommendedKpis: primaryKpis,
        },
        {
          id: 'page-drivers',
          title: 'Driver diagnostics',
          objective: 'Break down the main KPI into supporting causes.',
          chapterId: 'chapter-drivers',
          recommendedKpis: primaryKpis,
        },
      ],
      kpiHierarchy: {
        nodes: [
          { id: 'kpi-primary', label: primaryKpis[0], level: 'primary', childNodeIds: ['kpi-support-1'] },
          { id: 'kpi-support-1', label: primaryKpis[1] ?? `${primaryKpis[0]} drivers`, level: 'supporting', childNodeIds: [] },
        ],
        supportingDimensions: dimensions,
      },
      navigationStructure: {
        pattern: 'hubAndSpoke',
        rationale: 'Keeps the overview page as the main decision hub before branching into explanation pages.',
        sections: [
          { id: 'nav-priorities', label: 'Priorities', pageRecommendationIds: ['page-overview', 'page-hotspots'] },
          { id: 'nav-drivers', label: 'Drivers', pageRecommendationIds: ['page-drivers'] },
        ],
      },
      analyticalFlow: {
        steps: [
          { id: 'flow-1', label: 'Spot the risk', objective: 'Identify where attention is required.', pageRecommendationId: 'page-overview' },
          { id: 'flow-2', label: 'Localize the issue', objective: `Pinpoint the affected ${dimensions[0].toLowerCase()} scope.`, pageRecommendationId: 'page-hotspots' },
          { id: 'flow-3', label: 'Explain the cause', objective: 'Show the KPI drivers behind the issue.', pageRecommendationId: 'page-drivers' },
        ],
      },
    },
    {
      id: 'concept-narrative',
      label: 'Narrative-first storyline',
      summary: `Leads with the business story, then reinforces it with ${listToSentence(primaryKpis)} and action pages.`,
      chapterMap: {
        chapters: [
          {
            id: 'chapter-story',
            title: 'Story setup',
            objective: 'Frame the business narrative and stakes.',
            pageRecommendationIds: ['page-storyline'],
          },
          {
            id: 'chapter-actions',
            title: 'Action plan',
            objective: 'Map the narrative to next-step decisions.',
            pageRecommendationIds: ['page-actions', 'page-proof'],
          },
        ],
      },
      pageRecommendations: [
        {
          id: 'page-storyline',
          title: 'Narrative setup',
          objective: brief.intendedStory,
          chapterId: 'chapter-story',
          recommendedKpis: [primaryKpis[0]],
        },
        {
          id: 'page-actions',
          title: 'Decision actions',
          objective: listToSentence(brief.keyDecisions),
          chapterId: 'chapter-actions',
          recommendedKpis: primaryKpis,
        },
        {
          id: 'page-proof',
          title: 'Evidence and drill path',
          objective: `Support the recommended action with ${listToSentence(dimensions)} context.`,
          chapterId: 'chapter-actions',
          recommendedKpis: primaryKpis,
        },
      ],
      kpiHierarchy: {
        nodes: [
          { id: 'kpi-narrative-primary', label: primaryKpis[0], level: 'primary', childNodeIds: ['kpi-narrative-support'] },
          { id: 'kpi-narrative-support', label: brief.successCriteria[0] ?? 'Decision confidence', level: 'diagnostic', childNodeIds: [] },
        ],
        supportingDimensions: dimensions,
      },
      navigationStructure: {
        pattern: 'linearNarrative',
        rationale: 'Moves users from the story setup into the recommended action path in a controlled sequence.',
        sections: [
          { id: 'nav-story', label: 'Story', pageRecommendationIds: ['page-storyline'] },
          { id: 'nav-actions', label: 'Actions', pageRecommendationIds: ['page-actions', 'page-proof'] },
        ],
      },
      analyticalFlow: {
        steps: [
          { id: 'n-flow-1', label: 'Frame the story', objective: 'Explain the business objective and stakes.', pageRecommendationId: 'page-storyline' },
          { id: 'n-flow-2', label: 'Recommend action', objective: 'Turn the story into a prioritized decision.', pageRecommendationId: 'page-actions' },
          { id: 'n-flow-3', label: 'Validate confidence', objective: 'Support the recommendation with evidence and context.', pageRecommendationId: 'page-proof' },
        ],
      },
    },
  ];
}

function buildPageConcepts(
  threadId: string,
  reportConceptId: string,
  brief: DesignBrief,
  preferred: AlternateReportConcept,
  version: number,
  createdAt: string,
  updatedAt: string,
  lifecycleState: ReportConcept['lifecycleState'],
  approvalState: ReportConcept['approvalState'],
): PageConcept[] {
  const sourceBriefVersionId = toArtifactVersionId(brief);
  const sourceReportConceptVersionId = `${reportConceptId}@v${version}`;
  return preferred.pageRecommendations.map((pageRecommendation, index) => ({
    id: `page-concept:${threadId}:${pageRecommendation.id}`,
    threadId,
    kind: 'pageConcept',
    version,
    lifecycleState,
    approvalState,
    approvalKind: 'designApproval',
    createdAt,
    updatedAt,
    authorSource: 'system',
    provenance: {
      source: 'system',
      notes: ['Derived from the selected concept baseline without materialization.'],
    },
    reportConceptId,
    sourceBriefVersionId,
    sourceReportConceptVersionId,
    title: pageRecommendation.title,
    intendedPurpose: pageRecommendation.objective,
    targetAudienceOrRole: brief.audience,
    primaryKpis: pageRecommendation.recommendedKpis,
    supportingDimensions: brief.dimensions,
    intendedStoryQuestion: preferred.analyticalFlow.steps.find((step) => step.pageRecommendationId === pageRecommendation.id)?.objective
      ?? pageRecommendation.objective,
    navigationRole: inferNavigationRole(pageRecommendation.id, index),
    relatedChapterId: pageRecommendation.chapterId,
  }));
}

function toState(persisted: PersistedConceptState): ConceptState {
  return {
    ...persisted,
    readiness: evaluateConceptDraftReadiness(persisted.currentConcept),
  };
}

function buildConcept(
  threadId: string,
  brief: DesignBrief,
  existing?: ReportConcept,
  preferredConceptId?: string,
): ReportConcept {
  const now = new Date().toISOString();
  const alternateConcepts = buildAlternateConcepts(brief);
  const selectedBaselineConceptId = preferredConceptId ?? existing?.preferredBaselineConceptId;
  const comparison = selectedBaselineConceptId
    ? compareAlternateConcepts(alternateConcepts, selectedBaselineConceptId)
    : undefined;
  const preferred = alternateConcepts.find((concept) => concept.id === selectedBaselineConceptId) ?? alternateConcepts[0];
  const reportConceptId = existing?.id ?? `report-concept:${threadId}`;
  const version = (existing?.version ?? 0) + 1;
  const isApprovedSelection = !!selectedBaselineConceptId
    && existing?.approvedBaselineConceptId === selectedBaselineConceptId
    && existing.approvalState === 'approved';
  const isPendingSelection = !!selectedBaselineConceptId
    && existing?.preferredBaselineConceptId === selectedBaselineConceptId
    && existing.approvalState === 'pendingApproval';
  const lifecycleState = isApprovedSelection
    ? 'approved'
    : isPendingSelection
      ? 'proposed'
      : 'draft';
  const approvalState = isApprovedSelection
    ? 'approved'
    : isPendingSelection
      ? 'pendingApproval'
      : 'notSubmitted';
  const approvedBaselineConceptId = isApprovedSelection
    ? selectedBaselineConceptId
    : undefined;
  const pageConcepts = buildPageConcepts(
    threadId,
    reportConceptId,
    brief,
    preferred,
    version,
    existing?.createdAt ?? now,
    now,
    lifecycleState,
    approvalState,
  );

  return {
    id: reportConceptId,
    threadId,
    kind: 'reportConcept',
    version,
    lifecycleState,
    approvalState,
    approvalKind: 'designApproval',
    createdAt: existing?.createdAt ?? now,
    updatedAt: now,
    authorSource: 'system',
    provenance: {
      source: 'system',
      notes: ['Concept Studio output is internal-only and advisory.'],
    },
    briefId: brief.id,
    sourceBriefId: brief.id,
    sourceBriefVersionId: toArtifactVersionId(brief),
    summary: preferred?.summary ?? 'Concept Studio generated alternate report concepts.',
    chapterMap: preferred.chapterMap,
    pageRecommendations: preferred.pageRecommendations,
    pageConcepts,
    kpiHierarchy: {
      id: `kpi-hierarchy:${threadId}`,
      threadId,
      kind: 'kpiHierarchyConcept',
      version,
      lifecycleState,
      approvalState,
      approvalKind: 'designApproval',
      createdAt: existing?.createdAt ?? now,
      updatedAt: now,
      authorSource: 'system',
      provenance: { source: 'system' },
      reportConceptId,
      sourceBriefVersionId: toArtifactVersionId(brief),
      sourceReportConceptVersionId: `${reportConceptId}@v${version}`,
      nodes: preferred.kpiHierarchy.nodes,
      supportingDimensions: preferred.kpiHierarchy.supportingDimensions,
    },
    navigationStructure: {
      id: `navigation:${threadId}`,
      threadId,
      kind: 'navigationConcept',
      version,
      lifecycleState,
      approvalState,
      approvalKind: 'designApproval',
      createdAt: existing?.createdAt ?? now,
      updatedAt: now,
      authorSource: 'system',
      provenance: { source: 'system' },
      reportConceptId,
      sourceBriefVersionId: toArtifactVersionId(brief),
      sourceReportConceptVersionId: `${reportConceptId}@v${version}`,
      pattern: preferred.navigationStructure.pattern,
      rationale: preferred.navigationStructure.rationale,
      sections: preferred.navigationStructure.sections,
    },
    analyticalFlow: preferred.analyticalFlow,
    alternateConcepts,
    preferredBaselineConceptId: selectedBaselineConceptId,
    approvedBaselineConceptId,
    comparison,
  };
}

function submitConcept(reportConcept: ReportConcept): ReportConcept {
  if (!reportConcept.preferredBaselineConceptId) {
    throw new Error('A preferred concept baseline must be selected before submission.');
  }

  if (reportConcept.approvalState === 'approved') {
    throw new Error('An approved concept baseline cannot be resubmitted.');
  }

  const updatedAt = new Date().toISOString();
  const version = reportConcept.version + 1;
  const sourceReportConceptVersionId = `${reportConcept.id}@v${version}`;
  return {
    ...reportConcept,
    version,
    lifecycleState: 'proposed',
    approvalState: 'pendingApproval',
    approvalKind: 'designApproval',
    updatedAt,
    approvedBaselineConceptId: undefined,
    pageConcepts: reportConcept.pageConcepts.map((pageConcept) => ({
      ...pageConcept,
      version,
      lifecycleState: 'proposed',
      approvalState: 'pendingApproval',
      approvalKind: 'designApproval',
      updatedAt,
      sourceReportConceptVersionId,
    })),
    kpiHierarchy: {
      ...reportConcept.kpiHierarchy,
      version,
      lifecycleState: 'proposed',
      approvalState: 'pendingApproval',
      approvalKind: 'designApproval',
      updatedAt,
      sourceReportConceptVersionId,
    },
    navigationStructure: {
      ...reportConcept.navigationStructure,
      version,
      lifecycleState: 'proposed',
      approvalState: 'pendingApproval',
      approvalKind: 'designApproval',
      updatedAt,
      sourceReportConceptVersionId,
    },
  };
}

function approveConcept(reportConcept: ReportConcept): ReportConcept {
  if (!reportConcept.preferredBaselineConceptId) {
    throw new Error('A preferred concept baseline must be selected before approval.');
  }

  if (reportConcept.approvalState !== 'pendingApproval') {
    throw new Error('The concept baseline must be submitted for approval before it can be approved.');
  }

  const updatedAt = new Date().toISOString();
  const approvedBaselineConceptId = reportConcept.preferredBaselineConceptId;
  const version = reportConcept.version + 1;
  const sourceReportConceptVersionId = `${reportConcept.id}@v${version}`;
  return {
    ...reportConcept,
    version,
    lifecycleState: 'approved',
    approvalState: 'approved',
    approvalKind: 'designApproval',
    updatedAt,
    approvedBaselineConceptId,
    pageConcepts: reportConcept.pageConcepts.map((pageConcept) => ({
      ...pageConcept,
      version,
      lifecycleState: 'approved',
      approvalState: 'approved',
      approvalKind: 'designApproval',
      updatedAt,
      sourceReportConceptVersionId,
    })),
    kpiHierarchy: {
      ...reportConcept.kpiHierarchy,
      version,
      lifecycleState: 'approved',
      approvalState: 'approved',
      approvalKind: 'designApproval',
      updatedAt,
      sourceReportConceptVersionId,
    },
    navigationStructure: {
      ...reportConcept.navigationStructure,
      version,
      lifecycleState: 'approved',
      approvalState: 'approved',
      approvalKind: 'designApproval',
      updatedAt,
      sourceReportConceptVersionId,
    },
  };
}

export function compareConceptAlternatives(reportConcept: ReportConcept): AlternateConceptComparison {
  return compareAlternateConcepts(
    reportConcept.alternateConcepts,
    reportConcept.preferredBaselineConceptId,
  );
}

export async function loadConceptState(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<ConceptState | undefined> {
  const filePath = manifestPath(context, threadId);
  if (!fs.existsSync(filePath)) {
    return undefined;
  }

  const persisted = readPersistedState(filePath);
  return persisted ? toState(persisted) : undefined;
}

export async function generateConceptArtifacts(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<ConceptState> {
  const briefState = await loadDesignBriefState(context, threadId);
  if (!briefState?.validation.canGenerateConcepts || briefState.current.approvalState !== 'approved') {
    throw new Error('Concept generation requires an approved Design Brief.');
  }

  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  const concept = buildConcept(threadId, briefState.current, existing?.currentConcept);
  const persisted: PersistedConceptState = {
    threadId,
    briefId: briefState.current.id,
    currentConcept: concept,
    history: [
      ...(existing?.history ?? []),
      {
        version: concept.version,
        savedAt: concept.updatedAt,
        concept,
      },
    ],
  };

  writePersistedState(filePath, persisted);
  return toState(persisted);
}

export async function selectConceptBaseline(
  context: vscode.ExtensionContext,
  threadId: string,
  preferredConceptId: string,
): Promise<ConceptState> {
  const existing = await loadConceptState(context, threadId);
  if (!existing) {
    throw new Error(`No Concept Studio state exists for thread ${threadId}.`);
  }

  if (!existing.currentConcept.alternateConcepts.some((concept) => concept.id === preferredConceptId)) {
    throw new Error(`Unknown alternate concept ${preferredConceptId}.`);
  }

  const briefState = await loadDesignBriefState(context, threadId);
  if (!briefState) {
    throw new Error(`No Design Brief exists for thread ${threadId}.`);
  }

  const updatedConcept = buildConcept(
    threadId,
    briefState.current,
    existing.currentConcept,
    preferredConceptId,
  );
  const persisted: PersistedConceptState = {
    threadId,
    briefId: existing.briefId,
    currentConcept: updatedConcept,
    history: [
      ...existing.history,
      {
        version: updatedConcept.version,
        savedAt: updatedConcept.updatedAt,
        concept: updatedConcept,
      },
    ],
  };

  writePersistedState(manifestPath(context, threadId), persisted);
  return toState(persisted);
}

export async function submitConceptBaselineForApproval(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<ConceptState> {
  const existing = await loadConceptState(context, threadId);
  if (!existing) {
    throw new Error(`No Concept Studio state exists for thread ${threadId}.`);
  }

  const updatedConcept = submitConcept(existing.currentConcept);
  const persisted: PersistedConceptState = {
    threadId,
    briefId: existing.briefId,
    currentConcept: updatedConcept,
    history: [
      ...existing.history,
      {
        version: updatedConcept.version,
        savedAt: updatedConcept.updatedAt,
        concept: updatedConcept,
      },
    ],
  };

  writePersistedState(manifestPath(context, threadId), persisted);
  return toState(persisted);
}

export async function approveConceptBaseline(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<ConceptState> {
  const existing = await loadConceptState(context, threadId);
  if (!existing) {
    throw new Error(`No Concept Studio state exists for thread ${threadId}.`);
  }

  const updatedConcept = approveConcept(existing.currentConcept);
  const persisted: PersistedConceptState = {
    threadId,
    briefId: existing.briefId,
    currentConcept: updatedConcept,
    history: [
      ...existing.history,
      {
        version: updatedConcept.version,
        savedAt: updatedConcept.updatedAt,
        concept: updatedConcept,
      },
    ],
  };

  writePersistedState(manifestPath(context, threadId), persisted);
  return toState(persisted);
}
