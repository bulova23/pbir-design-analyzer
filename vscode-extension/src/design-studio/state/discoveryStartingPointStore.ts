import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type * as vscode from 'vscode';
import type {
  AlternateReportConcept,
  AnalyticalFlowConcept,
  ChapterConcept,
  DesignArtifactLineageEntry,
  DesignArtifactMetadata,
  DesignArtifactProvenance,
  DesignBrief,
  DraftArtifactStatus,
  DraftLayoutArtifact,
  DraftNavigationArtifact,
  DraftNavigationSectionArtifact,
  DraftPageArtifact,
  DraftReportArtifact,
  KpiHierarchyConcept,
  KpiHierarchyNodeConcept,
  NavigationConcept,
  NavigationSectionConcept,
  PageConcept,
  PageRecommendationConcept,
  ReportChapterMapConcept,
  ReportConcept,
} from '../contracts/designStudioModels';

type DiscoveryExperienceType =
  | 'executiveDashboard'
  | 'pbirReport'
  | 'fabricApp'
  | 'fabricDataApp'
  | 'operationalMonitoringExperience'
  | 'analyticalInvestigationExperience';

type DiscoveryOpportunityCategory =
  | 'executiveReporting'
  | 'comparativePerformanceManagement'
  | 'customerAnalysis'
  | 'inventoryOptimization'
  | 'rootCauseInvestigation';

export interface DiscoveryBlueprintSeed {
  blueprintId: string;
  recommendedPages: Array<{
    pageName: string;
    pageIntent: string;
    suggestedFilters: string[];
    suggestedVisualTypes: string[];
  }>;
  primaryKpis: string[];
  suggestedGlobalFilters: string[];
  analyticalFlow: {
    question: string;
    investigation: string;
    evidence: string;
    decision: string;
  };
  navigationIntent: {
    flow: string;
    sequence: string[];
  };
  successCriteriaSeed: string[];
}

export interface DiscoveryDesignStudioSelectionInput {
  reportPath: string;
  semanticModelSource: string;
  discoveryProfileId: string;
  opportunityId: string;
  opportunityCategory: DiscoveryOpportunityCategory;
  recommendationId: string;
  recommendationName: string;
  experienceType: DiscoveryExperienceType;
  expectedAudience: string;
  expectedBusinessOutcome: string;
  whyWeRecommendIt: string;
  supportingSignals: string[];
  limitingFactors: string[];
  confidenceNote: string;
  complexityNote: string;
  blueprint: DiscoveryBlueprintSeed;
}

export interface DiscoveryStartingPointSeedResult {
  threadId: string;
  selectedRecommendationId: string;
  brief: DesignBrief;
  concept: ReportConcept;
  draft: DraftReportArtifact;
}

interface PersistedDesignBriefState {
  threadId: string;
  current: DesignBrief;
  history: Array<{
    version: number;
    savedAt: string;
    brief: DesignBrief;
  }>;
}

interface PersistedConceptState {
  threadId: string;
  briefId: string;
  currentConcept: ReportConcept;
  history: Array<{
    version: number;
    savedAt: string;
    concept: ReportConcept;
  }>;
}

interface PersistedDraftState {
  threadId: string;
  brief: DesignBrief;
  concept: ReportConcept;
  currentDraft: DraftReportArtifact;
  pageArtifacts: DraftPageArtifact[];
  layoutArtifacts: DraftLayoutArtifact[];
  navigationArtifacts: DraftNavigationArtifact[];
  history: Array<{
    version: number;
    savedAt: string;
    draft: DraftReportArtifact;
    pageArtifacts: DraftPageArtifact[];
    layoutArtifacts: DraftLayoutArtifact[];
    navigationArtifacts: DraftNavigationArtifact[];
  }>;
  providerCapabilities: [];
}

const DEFAULT_DRAFT_STATUS: DraftArtifactStatus = {
  isolation: 'isolated',
  reviewability: 'reviewable',
  productionState: 'nonProduction',
};

function threadKey(threadId: string): string {
  return crypto.createHash('md5').update(threadId).digest('hex').slice(0, 16);
}

function createThreadId(reportPath: string): string {
  return `design-studio:${crypto.createHash('md5').update(reportPath).digest('hex').slice(0, 16)}`;
}

function sessionDir(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(context.globalStorageUri.fsPath, 'design-studio', 'threads', threadKey(threadId));
}

function toVersionId(artifact: Pick<DesignArtifactMetadata, 'id' | 'version'>): string {
  return `${artifact.id}@v${artifact.version}`;
}

function normalize(values: string[]): string[] {
  return values.map((value) => value.trim()).filter((value) => value.length > 0);
}

function createLineage(input: DiscoveryDesignStudioSelectionInput): DesignArtifactLineageEntry[] {
  return [
    {
      stage: 'semanticModel',
      sourceKind: 'semanticModel',
      sourceId: input.semanticModelSource,
      label: 'Semantic model source',
    },
    {
      stage: 'discoveryProfile',
      sourceKind: 'discoveryProfile',
      sourceId: input.discoveryProfileId,
      label: 'Discovery Profile',
    },
    {
      stage: 'opportunity',
      sourceKind: 'opportunity',
      sourceId: input.opportunityId,
      label: input.opportunityCategory,
    },
    {
      stage: 'recommendation',
      sourceKind: 'recommendation',
      sourceId: input.recommendationId,
      label: input.recommendationName,
    },
    {
      stage: 'experienceBlueprint',
      sourceKind: 'experienceBlueprint',
      sourceId: input.blueprint.blueprintId,
      label: input.recommendationName,
    },
  ];
}

function createProvenance(
  artifactId: string,
  artifactKind: DesignArtifactMetadata['kind'],
  input: DiscoveryDesignStudioSelectionInput,
): DesignArtifactProvenance {
  const timestamp = new Date().toISOString();

  return {
    source: 'discoveryWizard',
    timestamp,
    artifactAttribution: {
      artifactId,
      artifactKind,
    },
    lineage: createLineage(input),
    notes: [
      `Created from selected recommendation ${input.recommendationName}.`,
      'Design Studio owns all downstream approvals and workflow progression.',
      'No validation approval, deployable asset, or mutation authority was created.',
    ],
  };
}

function createMetadata<K extends DesignArtifactMetadata['kind']>(
  id: string,
  threadId: string,
  kind: K,
  input: DiscoveryDesignStudioSelectionInput,
): DesignArtifactMetadata & { kind: K } {
  const now = new Date().toISOString();

  return {
    id,
    threadId,
    kind,
    version: 1,
    lifecycleState: 'draft',
    approvalState: 'notSubmitted',
    approvalKind: 'designApproval',
    createdAt: now,
    updatedAt: now,
    authorSource: 'system',
    provenance: createProvenance(id, kind, input),
  };
}

function mapReportType(experienceType: DiscoveryExperienceType): DesignBrief['reportType'] {
  switch (experienceType) {
    case 'operationalMonitoringExperience':
      return 'operationalMonitoring';
    case 'analyticalInvestigationExperience':
      return 'narrativeBriefing';
    default:
      return 'dashboard';
  }
}

function mapSurfaceFamily(experienceType: DiscoveryExperienceType): string {
  return experienceType === 'fabricApp' || experienceType === 'fabricDataApp'
    ? 'fabricApp'
    : 'pbir';
}

function inferDecisionCadence(experienceType: DiscoveryExperienceType): string {
  return experienceType === 'operationalMonitoringExperience'
    ? 'Daily'
    : 'Weekly';
}

function inferNavigationPattern(flow: string): string {
  const normalized = flow.toLowerCase();
  if (normalized.includes('question') || normalized.includes('investigation')) {
    return 'guidedInvestigation';
  }
  if (normalized.includes('summary')) {
    return 'hubAndSpoke';
  }
  return 'guidedFlow';
}

function createBrief(threadId: string, input: DiscoveryDesignStudioSelectionInput): DesignBrief {
  const metadata = createMetadata(`design-brief:${threadId}`, threadId, 'designBrief', input);
  const dimensions = normalize(input.blueprint.suggestedGlobalFilters);

  return {
    ...metadata,
    audience: input.expectedAudience,
    businessObjective: input.expectedBusinessOutcome,
    keyDecisions: normalize([
      input.blueprint.analyticalFlow.question,
      input.blueprint.analyticalFlow.investigation,
      input.blueprint.analyticalFlow.decision,
    ]),
    primaryKpis: normalize(input.blueprint.primaryKpis),
    dimensions,
    intendedStory: `Track ${input.expectedBusinessOutcome.toLowerCase()} by starting with ${input.blueprint.analyticalFlow.question.toLowerCase()} and ending with ${input.blueprint.analyticalFlow.decision.toLowerCase()}.`,
    successCriteria: normalize(input.blueprint.successCriteriaSeed),
    reportType: mapReportType(input.experienceType),
    navigationExpectations: `Use a ${input.blueprint.navigationIntent.flow.toLowerCase()} path: ${input.blueprint.navigationIntent.sequence.join(' -> ')}.`,
    consumptionContext: `Discovery-backed starting point for ${input.recommendationName}.`,
    decisionCadence: inferDecisionCadence(input.experienceType),
    narrativeRisksOrConstraints: normalize(input.limitingFactors),
    requiredEvidenceDomains: ['semanticModel', 'experienceBlueprint'],
    targetAnalyzableSurfaceFamily: mapSurfaceFamily(input.experienceType),
  };
}

function createChapterMap(pages: PageRecommendationConcept[]): ReportChapterMapConcept {
  const chapter: ChapterConcept = {
    id: 'chapter-blueprint-flow',
    title: 'Blueprint flow',
    objective: 'Follow the experience blueprint in a Design Studio-ready structure.',
    pageRecommendationIds: pages.map((page) => page.id),
  };

  return { chapters: [chapter] };
}

function createAnalyticalFlow(
  pageRecommendations: PageRecommendationConcept[],
  input: DiscoveryDesignStudioSelectionInput,
): AnalyticalFlowConcept {
  const resolvePageId = (index: number): string => {
    if (pageRecommendations.length === 0) {
      return 'page-1';
    }

    return pageRecommendations[Math.min(index, pageRecommendations.length - 1)].id;
  };

  return {
    steps: [
      { id: 'flow-question', label: 'Question', objective: input.blueprint.analyticalFlow.question, pageRecommendationId: resolvePageId(0) },
      { id: 'flow-investigation', label: 'Investigation', objective: input.blueprint.analyticalFlow.investigation, pageRecommendationId: resolvePageId(1) },
      { id: 'flow-evidence', label: 'Evidence', objective: input.blueprint.analyticalFlow.evidence, pageRecommendationId: resolvePageId(2) },
      { id: 'flow-decision', label: 'Decision', objective: input.blueprint.analyticalFlow.decision, pageRecommendationId: resolvePageId(pageRecommendations.length - 1) },
    ],
  };
}

function createConcept(threadId: string, brief: DesignBrief, input: DiscoveryDesignStudioSelectionInput): ReportConcept {
  const reportConceptId = `report-concept:${threadId}`;
  const pageRecommendations: PageRecommendationConcept[] = input.blueprint.recommendedPages.map((page, index) => ({
    id: `page-${index + 1}`,
    title: page.pageName,
    objective: page.pageIntent,
    chapterId: 'chapter-blueprint-flow',
    recommendedKpis: [...input.blueprint.primaryKpis],
  }));
  const chapterMap = createChapterMap(pageRecommendations);
  const analyticalFlow = createAnalyticalFlow(pageRecommendations, input);
  const reportConceptVersionId = `${reportConceptId}@v1`;

  const pageConcepts: PageConcept[] = pageRecommendations.map((page, index) => ({
    ...createMetadata(`page-concept:${threadId}:${index}`, threadId, 'pageConcept', input),
    reportConceptId,
    sourceBriefVersionId: toVersionId(brief),
    sourceReportConceptVersionId: reportConceptVersionId,
    title: page.title,
    intendedPurpose: page.objective,
    targetAudienceOrRole: input.expectedAudience,
    primaryKpis: [...page.recommendedKpis],
    supportingDimensions: [...brief.dimensions],
    intendedStoryQuestion: analyticalFlow.steps[Math.min(index, analyticalFlow.steps.length - 1)]?.objective ?? page.objective,
    navigationRole: index === 0 ? 'entry' : index === pageRecommendations.length - 1 ? 'decision' : 'supporting',
    relatedChapterId: page.chapterId,
  }));

  const kpiNodes: KpiHierarchyNodeConcept[] = input.blueprint.primaryKpis.map((kpi, index) => ({
    id: `kpi-${index}`,
    label: kpi,
    level: index === 0 ? 'primary' : 'supporting',
    childNodeIds: [],
  }));
  const navigationSections: NavigationSectionConcept[] = input.blueprint.navigationIntent.sequence.map((label, index) => ({
    id: `nav-${index}`,
    label,
    pageRecommendationIds: [pageRecommendations[Math.min(index, pageRecommendations.length - 1)]?.id ?? 'page-1'],
  }));
  const navigationPattern = inferNavigationPattern(input.blueprint.navigationIntent.flow);

  const alternateConcepts: AlternateReportConcept[] = [
    {
      id: 'concept-blueprint-aligned',
      label: 'Blueprint-aligned flow',
      summary: `Follows the ${input.recommendationName} blueprint sequence directly.`,
      chapterMap,
      pageRecommendations,
      kpiHierarchy: {
        nodes: kpiNodes,
        supportingDimensions: [...brief.dimensions],
      },
      navigationStructure: {
        pattern: navigationPattern,
        rationale: 'Preserves the recommended blueprint sequence as the initial baseline.',
        sections: navigationSections,
      },
      analyticalFlow,
    },
    {
      id: 'concept-scan-first',
      label: 'Scan-first KPI flow',
      summary: `Starts with KPI emphasis and then expands into ${input.recommendationName.toLowerCase()}.`,
      chapterMap,
      pageRecommendations,
      kpiHierarchy: {
        nodes: kpiNodes,
        supportingDimensions: [...brief.dimensions],
      },
      navigationStructure: {
        pattern: 'hubAndSpoke',
        rationale: 'Creates a scan-first baseline while staying within the same blueprint evidence model.',
        sections: navigationSections,
      },
      analyticalFlow,
    },
  ];

  const kpiHierarchy: KpiHierarchyConcept = {
    ...createMetadata(`kpi-hierarchy:${threadId}`, threadId, 'kpiHierarchyConcept', input),
    reportConceptId,
    sourceBriefVersionId: toVersionId(brief),
    sourceReportConceptVersionId: reportConceptVersionId,
    nodes: kpiNodes,
    supportingDimensions: [...brief.dimensions],
  };

  const navigationStructure: NavigationConcept = {
    ...createMetadata(`navigation:${threadId}`, threadId, 'navigationConcept', input),
    reportConceptId,
    sourceBriefVersionId: toVersionId(brief),
    sourceReportConceptVersionId: reportConceptVersionId,
    pattern: navigationPattern,
    rationale: 'Derived from the selected recommendation blueprint without approving a baseline.',
    sections: navigationSections,
  };

  return {
    ...createMetadata(reportConceptId, threadId, 'reportConcept', input),
    briefId: brief.id,
    sourceBriefId: brief.id,
    sourceBriefVersionId: toVersionId(brief),
    summary: alternateConcepts[0].summary,
    chapterMap,
    pageRecommendations,
    pageConcepts,
    kpiHierarchy,
    navigationStructure,
    analyticalFlow,
    alternateConcepts,
    preferredBaselineConceptId: undefined,
    approvedBaselineConceptId: undefined,
    comparison: undefined,
  };
}

function createDraft(
  threadId: string,
  brief: DesignBrief,
  concept: ReportConcept,
  input: DiscoveryDesignStudioSelectionInput,
): {
  draft: DraftReportArtifact;
  pageArtifacts: DraftPageArtifact[];
  layoutArtifacts: DraftLayoutArtifact[];
  navigationArtifacts: DraftNavigationArtifact[];
} {
  const draftId = `draft-report:${threadId}`;
  const pageArtifacts: DraftPageArtifact[] = concept.pageConcepts.map((pageConcept, index) => ({
    ...createMetadata(`draft-page:${threadId}:${index}`, threadId, 'draftPageArtifact', input),
    draftReportArtifactId: draftId,
    pageConceptId: pageConcept.id,
    sourceBriefVersionId: toVersionId(brief),
    sourceConceptVersionId: toVersionId(concept),
    sourcePageConceptVersionId: toVersionId(pageConcept),
    structureSummary: `${pageConcept.title} draft seed frames ${pageConcept.intendedPurpose.toLowerCase()}.`,
    recommendedVisualRoles: [...input.blueprint.recommendedPages[Math.min(index, input.blueprint.recommendedPages.length - 1)]!.suggestedVisualTypes],
    draftStatus: DEFAULT_DRAFT_STATUS,
  }));

  const layoutArtifacts: DraftLayoutArtifact[] = pageArtifacts.map((pageArtifact, index) => ({
    ...createMetadata(`draft-layout:${threadId}:${index}`, threadId, 'draftLayoutArtifact', input),
    draftPageArtifactId: pageArtifact.id,
    pageConceptId: pageArtifact.pageConceptId,
    sourceBriefVersionId: toVersionId(brief),
    sourceConceptVersionId: toVersionId(concept),
    sourcePageConceptVersionId: pageArtifact.sourcePageConceptVersionId,
    layoutType: index === 0 ? 'heroKpiGrid' : 'detailAnalysisGrid',
    title: `${concept.pageConcepts[index]?.title ?? 'Draft page'} layout`,
    kpiBindings: [...(concept.pageConcepts[index]?.primaryKpis ?? [])],
    zones: ['header', 'primaryCanvas', 'supportingCanvas'],
    draftStatus: DEFAULT_DRAFT_STATUS,
  }));

  const navigationSections: DraftNavigationSectionArtifact[] = pageArtifacts.map((pageArtifact, index) => ({
    id: `draft-nav-section:${index}`,
    label: concept.pageConcepts[index]?.title ?? `Page ${index + 1}`,
    pageArtifactId: pageArtifact.id,
    pageConceptId: pageArtifact.pageConceptId,
  }));

  const navigationArtifact: DraftNavigationArtifact = {
    ...createMetadata(`draft-navigation:${threadId}`, threadId, 'draftNavigationArtifact', input),
    draftReportArtifactId: draftId,
    navigationConceptId: concept.navigationStructure.id,
    sourceBriefVersionId: toVersionId(brief),
    sourceConceptVersionId: toVersionId(concept),
    sourceNavigationConceptVersionId: toVersionId(concept.navigationStructure),
    frameworkType: concept.navigationStructure.pattern,
    sections: navigationSections,
    draftStatus: DEFAULT_DRAFT_STATUS,
  };

  const draft: DraftReportArtifact = {
    ...createMetadata(draftId, threadId, 'draftReportArtifact', input),
    briefId: brief.id,
    conceptId: concept.id,
    sourceBriefVersionId: toVersionId(brief),
    sourceConceptVersionId: toVersionId(concept),
    sourceNavigationConceptVersionId: toVersionId(concept.navigationStructure),
    pageArtifactIds: pageArtifacts.map((artifact) => artifact.id),
    layoutArtifactIds: layoutArtifacts.map((artifact) => artifact.id),
    navigationArtifactIds: [navigationArtifact.id],
    summary: `Discovery-backed draft seed for ${input.recommendationName}.`,
    draftStatus: DEFAULT_DRAFT_STATUS,
  };

  return {
    draft,
    pageArtifacts,
    layoutArtifacts,
    navigationArtifacts: [navigationArtifact],
  };
}

function writeJson(filePath: string, value: unknown): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(value, null, 2), 'utf8');
}

export async function selectDiscoveryRecommendationForDesignStudio(
  context: vscode.ExtensionContext,
  input: DiscoveryDesignStudioSelectionInput,
): Promise<DiscoveryStartingPointSeedResult> {
  const threadId = createThreadId(input.reportPath);
  const threadDir = sessionDir(context, threadId);
  const brief = createBrief(threadId, input);
  const concept = createConcept(threadId, brief, input);
  const draftBuild = createDraft(threadId, brief, concept, input);

  const briefState: PersistedDesignBriefState = {
    threadId,
    current: brief,
    history: [
      {
        version: brief.version,
        savedAt: brief.updatedAt,
        brief,
      },
    ],
  };

  const conceptState: PersistedConceptState = {
    threadId,
    briefId: brief.id,
    currentConcept: concept,
    history: [
      {
        version: concept.version,
        savedAt: concept.updatedAt,
        concept,
      },
    ],
  };

  const draftState: PersistedDraftState = {
    threadId,
    brief,
    concept,
    currentDraft: draftBuild.draft,
    pageArtifacts: draftBuild.pageArtifacts,
    layoutArtifacts: draftBuild.layoutArtifacts,
    navigationArtifacts: draftBuild.navigationArtifacts,
    history: [
      {
        version: draftBuild.draft.version,
        savedAt: draftBuild.draft.updatedAt,
        draft: draftBuild.draft,
        pageArtifacts: draftBuild.pageArtifacts,
        layoutArtifacts: draftBuild.layoutArtifacts,
        navigationArtifacts: draftBuild.navigationArtifacts,
      },
    ],
    providerCapabilities: [],
  };

  writeJson(path.join(threadDir, 'design-brief.json'), briefState);
  writeJson(path.join(threadDir, 'concept-studio.json'), conceptState);
  writeJson(path.join(threadDir, 'draft-studio.json'), draftState);

  return {
    threadId,
    selectedRecommendationId: input.recommendationId,
    brief,
    concept,
    draft: draftBuild.draft,
  };
}
