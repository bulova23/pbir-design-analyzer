import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type * as vscode from 'vscode';
import type {
  FixPlanItem,
  GuidedStoryImprovement,
  GuidedStoryImprovements,
  NormalizedFinding,
  StoryAssessmentReportSnapshot,
} from '../../analyzer/contracts/scorePanel';
import type {
  CrossPageNarrativeAnalyzerOutput,
  DesignStudioAnalyzerResultReference,
  DesignArtifactBacklinkRecord,
  RecommendationState,
  RefinementAnalyzerSource,
  RefinementNoMutationGuarantee,
  RefinementProposal,
  RefinementSourceAnalyzerOutput,
  SourceArtifactLineageEntry,
} from '../contracts/designStudioModels';
import { createSourceArtifactLineageEntry } from '../contracts/designStudioModels';
import { resolveDesignArtifactBacklinks } from '../navigation/designArtifactBacklinkResolver';
import { loadDraftState } from './draftStore';

export type { CrossPageNarrativeAnalyzerOutput } from '../contracts/designStudioModels';

export interface RefinementHistoryEntry {
  version: number;
  savedAt: string;
  proposals: RefinementProposal[];
  backlinks: DesignArtifactBacklinkRecord[];
}

export interface RefinementState {
  threadId: string;
  proposals: RefinementProposal[];
  backlinks: DesignArtifactBacklinkRecord[];
  history: RefinementHistoryEntry[];
}

type PersistedRefinementState = RefinementState;

interface BaseRefinementIngestion {
  analyzerRunId: string;
  resultReference: string;
  sourceArtifactVersionIds: string[];
  reportPath: string;
  scoredAt: string;
}

export interface StoryAssessmentIngestion extends BaseRefinementIngestion {
  storyAssessment: StoryAssessmentReportSnapshot;
}

export interface GuidedStoryImprovementsIngestion extends BaseRefinementIngestion {
  guidedStoryImprovements: GuidedStoryImprovements;
}

export interface IssuesIngestion extends BaseRefinementIngestion {
  issues: NormalizedFinding[];
}

export interface FixPlanIngestion extends BaseRefinementIngestion {
  fixPlanItems: FixPlanItem[];
}

export interface CrossPageNarrativeIngestion extends BaseRefinementIngestion {
  crossPageNarrative: CrossPageNarrativeAnalyzerOutput;
}

const NO_MUTATION_GUARANTEE: RefinementNoMutationGuarantee = {
  directReportMutation: false,
  materializationTriggered: false,
  analyzerHandoffTriggered: false,
  pbirAssetGenerationTriggered: false,
  analyzableSurfaceCreated: false,
  autoApplied: false,
};

export class StaleAnalyzerOutputError extends Error {
  readonly diagnostics: string[];

  constructor(message: string, diagnostics: string[]) {
    super(message);
    this.name = 'StaleAnalyzerOutputError';
    this.diagnostics = diagnostics;
  }
}

function threadKey(threadId: string): string {
  return crypto.createHash('md5').update(threadId).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(context.globalStorageUri.fsPath, 'design-studio', 'threads', threadKey(threadId));
}

function manifestPath(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(sessionDir(context, threadId), 'refinement-studio.json');
}

function readPersistedState(filePath: string): PersistedRefinementState | undefined {
  try {
    return JSON.parse(fs.readFileSync(filePath, 'utf8')) as PersistedRefinementState;
  } catch {
    return undefined;
  }
}

function writePersistedState(filePath: string, state: PersistedRefinementState): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(state, null, 2), 'utf8');
}

function toVersionId(artifact: { id: string; version: number }): string {
  return `${artifact.id}@v${artifact.version}`;
}

function collectCurrentVersionIds(draftState: NonNullable<Awaited<ReturnType<typeof loadDraftState>>>): Set<string> {
  return new Set([
    toVersionId(draftState.brief),
    toVersionId(draftState.concept),
    toVersionId(draftState.concept.navigationStructure),
    toVersionId(draftState.concept.kpiHierarchy),
    ...draftState.concept.pageConcepts.map(toVersionId),
    toVersionId(draftState.currentDraft),
    ...draftState.pageArtifacts.map(toVersionId),
    ...draftState.layoutArtifacts.map(toVersionId),
    ...draftState.navigationArtifacts.map(toVersionId),
  ]);
}

function collectCurrentFingerprint(
  draftState: NonNullable<Awaited<ReturnType<typeof loadDraftState>>>,
): string[] {
  return [...collectCurrentVersionIds(draftState)].sort((left, right) => left.localeCompare(right));
}

function assertFreshArtifactVersions(
  draftState: NonNullable<Awaited<ReturnType<typeof loadDraftState>>>,
  sourceArtifactVersionIds: string[],
): void {
  const expectedFingerprint = collectCurrentFingerprint(draftState);
  const providedFingerprint = [...new Set(sourceArtifactVersionIds)].sort((left, right) => left.localeCompare(right));
  const missingVersions = expectedFingerprint.filter((versionId) => !providedFingerprint.includes(versionId));
  const unknownVersions = providedFingerprint.filter((versionId) => !expectedFingerprint.includes(versionId));

  if (
    providedFingerprint.length === 0
    || missingVersions.length > 0
    || unknownVersions.length > 0
    || providedFingerprint.length !== expectedFingerprint.length
  ) {
    throw new StaleAnalyzerOutputError(
      'Analyzer outputs reference stale or incomplete approved design artifact versions.',
      [
        `Expected approved artifact fingerprint: ${expectedFingerprint.join(', ')}`,
        `Received analyzer fingerprint: ${providedFingerprint.join(', ')}`,
        `Missing approved artifact versions: ${missingVersions.join(', ') || 'none'}`,
        `Unknown analyzer artifact versions: ${unknownVersions.join(', ') || 'none'}`,
      ],
    );
  }
}

function createSourceAnalyzerOutput(
  analyzerSource: RefinementAnalyzerSource,
  ingestion: BaseRefinementIngestion,
  payload: unknown,
): RefinementSourceAnalyzerOutput {
  return {
    analyzerSource,
    analyzerRunId: ingestion.analyzerRunId,
    resultReference: ingestion.resultReference,
    reportPath: ingestion.reportPath,
    scoredAt: ingestion.scoredAt,
    sourceArtifactVersionIds: ingestion.sourceArtifactVersionIds,
    payload,
  };
}

function createProposal(
  threadId: string,
  version: number,
  sourceAnalyzerOutput: RefinementSourceAnalyzerOutput,
  sourceArtifactId: string,
  sourceLineage: SourceArtifactLineageEntry[],
  affectedArtifactIds: string[],
  affectedArtifactVersionIds: string[],
  linkedFindingIds: string[],
  suggestedDesignChange: string,
  rationale: string,
  expectedImpact: string,
): RefinementProposal {
  const now = new Date().toISOString();
  return {
    id: `refinement-proposal:${threadId}:${sourceAnalyzerOutput.analyzerSource}:${sourceAnalyzerOutput.resultReference}:${version}`,
    threadId,
    kind: 'refinementProposal',
    version,
    lifecycleState: 'proposed',
    approvalState: 'pendingApproval',
    // Refinement proposals are the authoritative owner of recommendation state.
    recommendationState: 'proposed',
    approvalKind: 'refinementApproval',
    createdAt: now,
    updatedAt: now,
    authorSource: 'system',
    provenance: {
      source: 'system',
      timestamp: now,
      notes: [
        `Derived from ${sourceAnalyzerOutput.analyzerSource} output.`,
        'Refinement Studio proposals remain advisory-only.',
      ],
    },
    validationLinkage: {
      analyzerRunId: sourceAnalyzerOutput.analyzerRunId,
      resultReference: sourceAnalyzerOutput.resultReference,
    },
    sourceArtifactId,
    sourceLineage,
    sourceAnalyzerOutput,
    affectedArtifactIds,
    affectedArtifactVersionIds,
    suggestedDesignChange,
    rationale,
    expectedImpact,
    linkedFindingIds,
    noMutationGuarantee: NO_MUTATION_GUARANTEE,
  };
}

function buildArtifactMetadataMap(
  draftState: NonNullable<Awaited<ReturnType<typeof loadDraftState>>>,
): Map<string, {
  id: string;
  kind: SourceArtifactLineageEntry['artifactKind'];
  version: number;
  approvalState: 'notSubmitted' | 'pendingApproval' | 'approved' | 'rejected';
  updatedAt: string;
}> {
  const artifacts = [
    draftState.brief,
    draftState.concept,
    draftState.concept.navigationStructure,
    draftState.concept.kpiHierarchy,
    ...draftState.concept.pageConcepts,
    draftState.currentDraft,
    ...draftState.pageArtifacts,
    ...draftState.layoutArtifacts,
    ...draftState.navigationArtifacts,
  ];

  return new Map(artifacts.map((artifact) => [artifact.id, artifact]));
}

function buildSourceLineageFromBacklinks(
  draftState: NonNullable<Awaited<ReturnType<typeof loadDraftState>>>,
  backlinks: DesignArtifactBacklinkRecord[],
): SourceArtifactLineageEntry[] {
  const artifactMap = buildArtifactMetadataMap(draftState);
  const deduped = new Map<string, SourceArtifactLineageEntry>();

  for (const backlink of backlinks) {
    for (const identity of [
      { artifactId: backlink.stableIdentity.designArtifactId, artifactVersionId: backlink.stableIdentity.designArtifactVersionId },
      { artifactId: backlink.stableIdentity.draftArtifactId, artifactVersionId: backlink.stableIdentity.draftArtifactVersionId },
    ]) {
      if (deduped.has(identity.artifactVersionId)) {
        continue;
      }

      const artifact = artifactMap.get(identity.artifactId);
      if (!artifact) {
        continue;
      }

      deduped.set(identity.artifactVersionId, createSourceArtifactLineageEntry(artifact, { sourceRole: 'supporting' }));
    }
  }

  return [...deduped.values()];
}

function updateProposalState(
  proposal: RefinementProposal,
  lifecycleState: RefinementProposal['lifecycleState'],
  approvalState: RefinementProposal['approvalState'],
  recommendationState: RecommendationState,
): RefinementProposal {
  return {
    ...proposal,
    lifecycleState,
    approvalState,
    recommendationState,
    approvalKind: 'refinementApproval',
    updatedAt: new Date().toISOString(),
  };
}

async function persistTransition(
  context: vscode.ExtensionContext,
  threadId: string,
  proposalId: string,
  updater: (proposal: RefinementProposal) => RefinementProposal,
): Promise<RefinementState> {
  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  if (!existing) {
    throw new Error(`No Refinement Studio state exists for thread ${threadId}.`);
  }

  const nextProposals = existing.proposals.map((proposal) => (
    proposal.id === proposalId ? updater(proposal) : proposal
  ));
  if (!nextProposals.some((proposal) => proposal.id === proposalId)) {
    throw new Error(`No refinement proposal ${proposalId} exists for thread ${threadId}.`);
  }

  const nextState: PersistedRefinementState = {
    ...existing,
    proposals: nextProposals,
    history: [
      ...existing.history,
      {
        version: (existing.history[existing.history.length - 1]?.version ?? 0) + 1,
        savedAt: new Date().toISOString(),
        proposals: nextProposals.filter((proposal) => proposal.id === proposalId),
        backlinks: existing.backlinks,
      },
    ],
  };
  writePersistedState(filePath, nextState);
  return nextState;
}

function toPersistedState(
  threadId: string,
  existing: PersistedRefinementState | undefined,
  proposals: RefinementProposal[],
  backlinks: DesignArtifactBacklinkRecord[],
): PersistedRefinementState {
  const nextVersion = (existing?.history[existing.history.length - 1]?.version ?? 0) + 1;
  return {
    threadId,
    proposals: [...(existing?.proposals ?? []), ...proposals],
    backlinks: [...(existing?.backlinks ?? []), ...backlinks],
    history: [
      ...(existing?.history ?? []),
      {
        version: nextVersion,
        savedAt: new Date().toISOString(),
        proposals,
        backlinks,
      },
    ],
  };
}

async function loadValidatedDraftState(context: vscode.ExtensionContext, threadId: string) {
  const draftState = await loadDraftState(context, threadId);
  if (!draftState) {
    throw new Error(`No Draft Studio state exists for thread ${threadId}.`);
  }

  return draftState;
}

function addProposalFromImprovement(
  draftState: NonNullable<Awaited<ReturnType<typeof loadDraftState>>>,
  threadId: string,
  version: number,
  sourceAnalyzerOutput: RefinementSourceAnalyzerOutput,
  improvement: GuidedStoryImprovement,
  backlinks: DesignArtifactBacklinkRecord[],
): RefinementProposal {
  const affectedArtifactIds = backlinks.map((link) => link.artifactId);
  const affectedArtifactVersionIds = backlinks.map((link) => link.artifactVersionId);
  return createProposal(
    threadId,
    version,
    sourceAnalyzerOutput,
    affectedArtifactIds[0] ?? `refinement-source:${sourceAnalyzerOutput.resultReference}`,
    buildSourceLineageFromBacklinks(draftState, backlinks),
    affectedArtifactIds,
    affectedArtifactVersionIds,
    [improvement.id],
    `Revise the design around ${improvement.title.toLowerCase()}.`,
    improvement.rationale,
    improvement.expectedImpact,
  );
}

function addProposalFromFinding(
  draftState: NonNullable<Awaited<ReturnType<typeof loadDraftState>>>,
  threadId: string,
  version: number,
  sourceAnalyzerOutput: RefinementSourceAnalyzerOutput,
  analyzerSource: RefinementAnalyzerSource,
  finding: NormalizedFinding,
  backlinks: DesignArtifactBacklinkRecord[],
): RefinementProposal {
  const changePrefix = analyzerSource === 'issues' ? 'Refine the design to address' : 'Update the design sequence for';
  return createProposal(
    threadId,
    version,
    sourceAnalyzerOutput,
    backlinks[0]?.artifactId ?? `refinement-source:${sourceAnalyzerOutput.resultReference}`,
    buildSourceLineageFromBacklinks(draftState, backlinks),
    backlinks.map((link) => link.artifactId),
    backlinks.map((link) => link.artifactVersionId),
    [finding.id],
    `${changePrefix} ${finding.title.toLowerCase()}.`,
    finding.summary,
    finding.recommendation,
  );
}

function addProposalFromFixPlan(
  draftState: NonNullable<Awaited<ReturnType<typeof loadDraftState>>>,
  threadId: string,
  version: number,
  sourceAnalyzerOutput: RefinementSourceAnalyzerOutput,
  item: FixPlanItem,
  backlinks: DesignArtifactBacklinkRecord[],
): RefinementProposal {
  return createProposal(
    threadId,
    version,
    sourceAnalyzerOutput,
    backlinks[0]?.artifactId ?? `refinement-source:${sourceAnalyzerOutput.resultReference}`,
    buildSourceLineageFromBacklinks(draftState, backlinks),
    backlinks.map((link) => link.artifactId),
    backlinks.map((link) => link.artifactVersionId),
    item.sourceFindingIds,
    item.recommendedAction,
    item.detail,
    item.why,
  );
}

function addProposalFromNarrative(
  draftState: NonNullable<Awaited<ReturnType<typeof loadDraftState>>>,
  threadId: string,
  version: number,
  sourceAnalyzerOutput: RefinementSourceAnalyzerOutput,
  narrative: CrossPageNarrativeAnalyzerOutput,
  backlinks: DesignArtifactBacklinkRecord[],
): RefinementProposal[] {
  return narrative.gaps.map((gap, index) => createProposal(
    threadId,
    version + index,
    sourceAnalyzerOutput,
    backlinks[0]?.artifactId ?? `refinement-source:${sourceAnalyzerOutput.resultReference}`,
    buildSourceLineageFromBacklinks(draftState, backlinks),
    backlinks.map((link) => link.artifactId),
    backlinks.map((link) => link.artifactVersionId),
    [gap.id],
    `Propose a story-structure or navigation alternative for ${gap.title.toLowerCase()}.`,
    gap.summary,
    narrative.summary,
  ));
}

async function persistIngestion(
  context: vscode.ExtensionContext,
  threadId: string,
  proposals: RefinementProposal[],
  backlinks: DesignArtifactBacklinkRecord[],
): Promise<RefinementState> {
  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  const nextState = toPersistedState(threadId, existing, proposals, backlinks);
  writePersistedState(filePath, nextState);
  return nextState;
}

export async function loadRefinementState(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<RefinementState | undefined> {
  const filePath = manifestPath(context, threadId);
  if (!fs.existsSync(filePath)) {
    return undefined;
  }

  return readPersistedState(filePath);
}

export async function reviewRefinementProposal(
  context: vscode.ExtensionContext,
  threadId: string,
  proposalId: string,
): Promise<RefinementState> {
  return persistTransition(context, threadId, proposalId, (proposal) => updateProposalState(
    proposal,
    'reviewed',
    'pendingApproval',
    'proposed',
  ));
}

export async function approveRefinementProposal(
  context: vscode.ExtensionContext,
  threadId: string,
  proposalId: string,
): Promise<RefinementState> {
  return persistTransition(context, threadId, proposalId, (proposal) => updateProposalState(
    proposal,
    'approved',
    'approved',
    'approved',
  ));
}

export async function rejectRefinementProposal(
  context: vscode.ExtensionContext,
  threadId: string,
  proposalId: string,
): Promise<RefinementState> {
  return persistTransition(context, threadId, proposalId, (proposal) => updateProposalState(
    proposal,
    'reviewed',
    'rejected',
    'rejected',
  ));
}

export async function deferRefinementProposal(
  context: vscode.ExtensionContext,
  threadId: string,
  proposalId: string,
): Promise<RefinementState> {
  return persistTransition(context, threadId, proposalId, (proposal) => updateProposalState(
    proposal,
    'reviewed',
    'pendingApproval',
    'deferred',
  ));
}

export async function attachAnalyzerResultLineage(
  context: vscode.ExtensionContext,
  threadId: string,
  results: DesignStudioAnalyzerResultReference[],
): Promise<RefinementState | undefined> {
  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  if (!existing) {
    return undefined;
  }

  const resultByKey = new Map(results.map((result) => [
    `${result.analyzerRunId}::${result.resultReference}`,
    result,
  ]));
  const proposals = existing.proposals.map((proposal) => {
    const result = resultByKey.get(`${proposal.sourceAnalyzerOutput.analyzerRunId}::${proposal.sourceAnalyzerOutput.resultReference}`);
    if (!result) {
      return proposal;
    }

    return {
      ...proposal,
      sourceAnalyzerOutput: {
        ...proposal.sourceAnalyzerOutput,
        sourceCandidateId: result.sourceCandidateId,
        sourceArtifactVersionFingerprint: [...result.sourceArtifactVersionFingerprint],
      },
    };
  });

  const nextState: RefinementState = {
    ...existing,
    proposals,
  };
  writePersistedState(filePath, nextState);
  return nextState;
}

export async function ingestStoryAssessmentOutput(
  context: vscode.ExtensionContext,
  threadId: string,
  ingestion: StoryAssessmentIngestion,
): Promise<RefinementState> {
  const draftState = await loadValidatedDraftState(context, threadId);
  assertFreshArtifactVersions(draftState, ingestion.sourceArtifactVersionIds);
  const proposals: RefinementProposal[] = [];
  const backlinks: DesignArtifactBacklinkRecord[] = [];
  let version = 1;

  for (const page of ingestion.storyAssessment.pages) {
    for (const recommendation of page.recommendations) {
      const linkedArtifacts = resolveDesignArtifactBacklinks(draftState, {
        analyzerSource: 'storyAssessment',
        analyzerReferenceId: recommendation.id,
        pageNames: [page.pageName],
        impactAreas: [recommendation.relatedImpactArea],
        findingIds: [recommendation.id],
      });
      backlinks.push(...linkedArtifacts);
      proposals.push(addProposalFromImprovement(
        draftState,
        threadId,
        version,
        createSourceAnalyzerOutput('storyAssessment', ingestion, page),
        recommendation,
        linkedArtifacts,
      ));
      version += 1;
    }
  }

  return persistIngestion(context, threadId, proposals, backlinks);
}

export async function ingestGuidedStoryImprovements(
  context: vscode.ExtensionContext,
  threadId: string,
  ingestion: GuidedStoryImprovementsIngestion,
): Promise<RefinementState> {
  const draftState = await loadValidatedDraftState(context, threadId);
  assertFreshArtifactVersions(draftState, ingestion.sourceArtifactVersionIds);
  const allImprovements = [
    ...ingestion.guidedStoryImprovements.highPriorityImprovements,
    ...ingestion.guidedStoryImprovements.mediumPriorityImprovements,
  ];
  const proposals: RefinementProposal[] = [];
  const backlinks: DesignArtifactBacklinkRecord[] = [];

  allImprovements.forEach((item, index) => {
    const pageName = item.navigationTarget?.pageName;
    const linkedArtifacts = resolveDesignArtifactBacklinks(draftState, {
      analyzerSource: 'guidedStoryImprovements',
      analyzerReferenceId: item.id,
      pageNames: pageName ? [pageName] : [],
      impactAreas: [item.relatedImpactArea],
      findingIds: [item.id],
    });
    backlinks.push(...linkedArtifacts);
    proposals.push(addProposalFromImprovement(
      draftState,
      threadId,
      index + 1,
      createSourceAnalyzerOutput('guidedStoryImprovements', ingestion, item),
      item,
      linkedArtifacts,
    ));
  });

  return persistIngestion(context, threadId, proposals, backlinks);
}

export async function ingestIssues(
  context: vscode.ExtensionContext,
  threadId: string,
  ingestion: IssuesIngestion,
): Promise<RefinementState> {
  const draftState = await loadValidatedDraftState(context, threadId);
  assertFreshArtifactVersions(draftState, ingestion.sourceArtifactVersionIds);
  const proposals: RefinementProposal[] = [];
  const backlinks: DesignArtifactBacklinkRecord[] = [];

  ingestion.issues.forEach((issue, index) => {
    const linkedArtifacts = resolveDesignArtifactBacklinks(draftState, {
      analyzerSource: 'issues',
      analyzerReferenceId: issue.id,
      pageNames: issue.affectedPages,
      impactAreas: [issue.impactArea],
      findingIds: [issue.id],
    });
    backlinks.push(...linkedArtifacts);
    proposals.push(addProposalFromFinding(
      draftState,
      threadId,
      index + 1,
      createSourceAnalyzerOutput('issues', ingestion, issue),
      'issues',
      issue,
      linkedArtifacts,
    ));
  });

  return persistIngestion(context, threadId, proposals, backlinks);
}

export async function ingestFixPlanItems(
  context: vscode.ExtensionContext,
  threadId: string,
  ingestion: FixPlanIngestion,
): Promise<RefinementState> {
  const draftState = await loadValidatedDraftState(context, threadId);
  assertFreshArtifactVersions(draftState, ingestion.sourceArtifactVersionIds);
  const proposals: RefinementProposal[] = [];
  const backlinks: DesignArtifactBacklinkRecord[] = [];

  ingestion.fixPlanItems.forEach((item, index) => {
    const linkedArtifacts = resolveDesignArtifactBacklinks(draftState, {
      analyzerSource: 'fixPlan',
      analyzerReferenceId: item.id,
      pageNames: item.affectedPages,
      impactAreas: item.navigationTarget ? ['navigation'] : [],
      findingIds: item.sourceFindingIds,
    });
    backlinks.push(...linkedArtifacts);
    proposals.push(addProposalFromFixPlan(
      draftState,
      threadId,
      index + 1,
      createSourceAnalyzerOutput('fixPlan', ingestion, item),
      item,
      linkedArtifacts,
    ));
  });

  return persistIngestion(context, threadId, proposals, backlinks);
}

export async function ingestCrossPageNarrativeOutput(
  context: vscode.ExtensionContext,
  threadId: string,
  ingestion: CrossPageNarrativeIngestion,
): Promise<RefinementState> {
  const draftState = await loadValidatedDraftState(context, threadId);
  assertFreshArtifactVersions(draftState, ingestion.sourceArtifactVersionIds);
  const linkedArtifacts = resolveDesignArtifactBacklinks(draftState, {
    analyzerSource: 'crossPageNarrative',
    analyzerReferenceId: ingestion.resultReference,
    pageNames: ingestion.crossPageNarrative.narrativePath,
    impactAreas: ['navigation', 'storytelling'],
    findingIds: ingestion.crossPageNarrative.gaps.map((gap) => gap.id),
    narrative: ingestion.crossPageNarrative,
  });
  const proposals = addProposalFromNarrative(
    draftState,
    threadId,
    1,
    createSourceAnalyzerOutput('crossPageNarrative', ingestion, ingestion.crossPageNarrative),
    ingestion.crossPageNarrative,
    linkedArtifacts,
  );

  return persistIngestion(context, threadId, proposals, linkedArtifacts);
}
