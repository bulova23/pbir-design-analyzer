import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import type {
  FixPlanItem,
  GuidedStoryImprovement,
  NormalizedFinding,
  StoryAssessmentReportSnapshot,
} from '../analyzer/contracts/scorePanel';
import {
  approveConceptBaseline,
  generateConceptArtifacts,
  selectConceptBaseline,
  submitConceptBaselineForApproval,
} from '../design-studio/state/conceptStore';
import {
  approveDesignBrief,
  saveDesignBriefDraft,
  submitDesignBriefForApproval,
} from '../design-studio/state/designBriefStore';
import {
  generateDraftArtifacts,
} from '../design-studio/state/draftStore';
import {
  approveRefinementProposal,
  deferRefinementProposal,
  ingestCrossPageNarrativeOutput,
  ingestFixPlanItems,
  ingestGuidedStoryImprovements,
  ingestIssues,
  ingestStoryAssessmentOutput,
  loadRefinementState,
  rejectRefinementProposal,
  reviewRefinementProposal,
  type CrossPageNarrativeAnalyzerOutput,
} from '../design-studio/state/refinementStore';

function makeContext(tmpDir: string): ExtensionContext {
  return {
    globalStorageUri: { fsPath: tmpDir },
    secrets: {
      get: jest.fn(),
      store: jest.fn(),
      delete: jest.fn(),
    },
  } as unknown as ExtensionContext;
}

function makeTempDir(): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-refinement-store-test-'));
}

async function createDraftState(context: ExtensionContext, threadId: string) {
  await saveDesignBriefDraft(context, threadId, {
    audience: 'Sales leaders',
    businessObjective: 'Reduce missed renewals',
    keyDecisions: ['Which regions need intervention first'],
    primaryKpis: ['Renewal rate', 'At-risk pipeline'],
    dimensions: ['Region', 'Segment'],
    intendedStory: 'Lead with risk, then explain the main drivers and next steps.',
    successCriteria: ['Leader can decide the next action within five minutes'],
    reportType: 'dashboard',
    navigationExpectations: 'Overview first, detail second.',
    consumptionContext: 'Weekly renewal review',
    decisionCadence: 'Weekly',
    narrativeRisksOrConstraints: ['Avoid hiding segment outliers'],
    requiredEvidenceDomains: ['renewal trend', 'pipeline coverage'],
    targetAnalyzableSurfaceFamily: 'pbir',
  });
  await submitDesignBriefForApproval(context, threadId);
  await approveDesignBrief(context, threadId);
  const conceptState = await generateConceptArtifacts(context, threadId);
  await selectConceptBaseline(context, threadId, conceptState.currentConcept.alternateConcepts[0].id);
  await submitConceptBaselineForApproval(context, threadId);
  await approveConceptBaseline(context, threadId);
  return generateDraftArtifacts(context, threadId);
}

function sourceVersionIds(state: Awaited<ReturnType<typeof createDraftState>>): string[] {
  return [
    `${state.brief.id}@v${state.brief.version}`,
    `${state.concept.id}@v${state.concept.version}`,
    `${state.concept.navigationStructure.id}@v${state.concept.navigationStructure.version}`,
    `${state.concept.kpiHierarchy.id}@v${state.concept.kpiHierarchy.version}`,
    ...state.concept.pageConcepts.map((pageConcept) => `${pageConcept.id}@v${pageConcept.version}`),
    `${state.currentDraft.id}@v${state.currentDraft.version}`,
    ...state.pageArtifacts.map((artifact) => `${artifact.id}@v${artifact.version}`),
    ...state.layoutArtifacts.map((artifact) => `${artifact.id}@v${artifact.version}`),
    ...state.navigationArtifacts.map((artifact) => `${artifact.id}@v${artifact.version}`),
  ];
}

function improvement(pageName: string, overrides: Partial<GuidedStoryImprovement> = {}): GuidedStoryImprovement {
  return {
    id: 'improvement-1',
    title: 'Strengthen the page question',
    summary: 'The page needs a clearer narrative headline.',
    rationale: 'The story intent is not obvious.',
    expectedImpact: 'Users will understand the decision path faster.',
    priority: 'high',
    relatedImpactArea: 'storytelling',
    navigationTarget: {
      kind: 'page',
      pageName,
      label: pageName,
      reason: 'Story issue',
      supportState: 'direct',
    },
    ...overrides,
  };
}

function finding(pageName: string, overrides: Partial<NormalizedFinding> = {}): NormalizedFinding {
  return {
    id: 'finding-1',
    title: 'Navigation drift',
    summary: 'Users do not move cleanly from summary to detail.',
    severity: 'high',
    confidence: 0.91,
    scope: 'crossPage',
    detectionType: 'deterministic',
    affectedPages: [pageName],
    impactArea: 'navigation',
    frameworkImpact: ['narrative'],
    recommendation: 'Reduce the number of branches and clarify the path.',
    sourceKind: 'issues',
    sourceSection: 'issues',
    evidence: [
      {
        kind: 'storyAssessment',
        label: 'Narrative breakdown',
        pageName,
      },
    ],
    ...overrides,
  };
}

function fixPlanItem(pageName: string): FixPlanItem {
  return {
    id: 'fix-1',
    title: 'Re-sequence navigation',
    detail: 'Move from overview into one focused driver page before optional drill paths.',
    severity: 'high',
    effort: 'medium',
    impact: 'high',
    why: 'This reduces cognitive branching early in the story.',
    scope: 'crossPage',
    affectedPages: [pageName],
    recommendedAction: 'Propose a simpler navigation and story sequence.',
    resolvedOutcomes: ['Users reach the detail path with less confusion'],
    sourceFindingIds: ['finding-1'],
    navigationTarget: {
      kind: 'page',
      pageName,
      label: pageName,
      reason: 'Fix plan target',
      supportState: 'direct',
    },
  };
}

describe('refinementStore', () => {
  it('ingests Story Assessment outputs into advisory refinement proposals with lineage preserved', async () => {
    const context = makeContext(makeTempDir());
    const draftState = await createDraftState(context, 'thread-refine-story');
    const pageName = draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';
    const storyAssessment: StoryAssessmentReportSnapshot = {
      reportPath: '/tmp/sales.pbir',
      scoredAt: '2026-06-13T10:00:00.000Z',
      pages: [
        {
          pageName,
          storyMaturity: 'Developing',
          strongSignals: ['Headline KPI is visible'],
          missingSignals: ['No clear question'],
          topImprovementIds: ['improvement-1'],
          recommendations: [improvement(pageName)],
        },
      ],
    };

    const state = await ingestStoryAssessmentOutput(context, 'thread-refine-story', {
      analyzerRunId: 'run-story-1',
      resultReference: 'story-assessment:2026-06-13',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      reportPath: storyAssessment.reportPath,
      scoredAt: storyAssessment.scoredAt,
      storyAssessment,
    });

    expect(state.proposals).toHaveLength(1);
    expect(state.proposals[0]).toEqual(expect.objectContaining({
      kind: 'refinementProposal',
      sourceAnalyzerOutput: expect.objectContaining({
        analyzerSource: 'storyAssessment',
      }),
      affectedArtifactIds: expect.arrayContaining([draftState.concept.pageConcepts[0]?.id]),
      affectedArtifactVersionIds: expect.arrayContaining([`${draftState.concept.pageConcepts[0]?.id}@v${draftState.concept.pageConcepts[0]?.version}`]),
      approvalState: 'pendingApproval',
      approvalKind: 'refinementApproval',
      noMutationGuarantee: expect.objectContaining({
        directReportMutation: false,
        materializationTriggered: false,
        analyzerHandoffTriggered: false,
      }),
    }));
    expect(state.backlinks).not.toHaveLength(0);
  });

  it('maps Guided Story Improvements, Issues, Fix Plan, and Cross-Page Narrative outputs into advisory proposals', async () => {
    const context = makeContext(makeTempDir());
    const draftState = await createDraftState(context, 'thread-refine-all');
    const pageName = draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';
    const versions = sourceVersionIds(draftState);

    await ingestGuidedStoryImprovements(context, 'thread-refine-all', {
      analyzerRunId: 'run-gsi-1',
      resultReference: 'guided-story:1',
      sourceArtifactVersionIds: versions,
      reportPath: '/tmp/sales.pbir',
      scoredAt: '2026-06-13T10:00:00.000Z',
      guidedStoryImprovements: {
        highPriorityImprovements: [improvement(pageName)],
        mediumPriorityImprovements: [],
        storyImprovementRationale: 'Start with a stronger business question.',
      },
    });
    await ingestIssues(context, 'thread-refine-all', {
      analyzerRunId: 'run-issues-1',
      resultReference: 'issues:1',
      sourceArtifactVersionIds: versions,
      reportPath: '/tmp/sales.pbir',
      scoredAt: '2026-06-13T10:01:00.000Z',
      issues: [finding(pageName)],
    });
    await ingestFixPlanItems(context, 'thread-refine-all', {
      analyzerRunId: 'run-fixplan-1',
      resultReference: 'fix-plan:1',
      sourceArtifactVersionIds: versions,
      reportPath: '/tmp/sales.pbir',
      scoredAt: '2026-06-13T10:02:00.000Z',
      fixPlanItems: [fixPlanItem(pageName)],
    });
    const crossPageNarrative: CrossPageNarrativeAnalyzerOutput = {
      scoreSummary: {
        score: 64,
        confidence: 'medium',
        dominantObjective: 'executive performance review',
      },
      gaps: [
        {
          id: 'gap-1',
          title: 'Missing executive entry point',
          summary: 'The report opens too deep in the detail path.',
          affectedPageNames: [pageName],
        },
      ],
      narrativePath: [pageName],
      summary: 'The report should establish a clearer overview-to-detail flow.',
    };
    await ingestCrossPageNarrativeOutput(context, 'thread-refine-all', {
      analyzerRunId: 'run-cross-page-1',
      resultReference: 'cross-page:1',
      sourceArtifactVersionIds: versions,
      reportPath: '/tmp/sales.pbir',
      scoredAt: '2026-06-13T10:03:00.000Z',
      crossPageNarrative,
    });

    const state = await loadRefinementState(context, 'thread-refine-all');
    expect(state?.proposals.map((proposal) => proposal.sourceAnalyzerOutput.analyzerSource)).toEqual([
      'guidedStoryImprovements',
      'issues',
      'fixPlan',
      'crossPageNarrative',
    ]);
    expect(state?.proposals.every((proposal) => proposal.noMutationGuarantee.directReportMutation === false)).toBe(true);
    expect(state?.proposals.every((proposal) => proposal.noMutationGuarantee.materializationTriggered === false)).toBe(true);
    expect(state?.proposals.every((proposal) => proposal.noMutationGuarantee.analyzerHandoffTriggered === false)).toBe(true);
  });

  it('rejects stale analyzer outputs safely and does not persist refinement state', async () => {
    const context = makeContext(makeTempDir());
    const draftState = await createDraftState(context, 'thread-refine-stale');
    const pageName = draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';

    await expect(ingestIssues(context, 'thread-refine-stale', {
      analyzerRunId: 'run-stale-1',
      resultReference: 'issues:stale',
      sourceArtifactVersionIds: [`${draftState.concept.id}@v${draftState.concept.version - 1}`],
      reportPath: '/tmp/sales.pbir',
      scoredAt: '2026-06-13T10:10:00.000Z',
      issues: [finding(pageName)],
    })).rejects.toThrow('Analyzer outputs reference stale or incomplete approved design artifact versions.');

    await expect(loadRefinementState(context, 'thread-refine-stale')).resolves.toBeUndefined();
  });

  it('rejects partial source-version matches as stale with explicit diagnostics', async () => {
    const context = makeContext(makeTempDir());
    const draftState = await createDraftState(context, 'thread-refine-partial');
    const pageName = draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';
    const partialFingerprint = sourceVersionIds(draftState).slice(0, 3);

    await expect(ingestIssues(context, 'thread-refine-partial', {
      analyzerRunId: 'run-partial-1',
      resultReference: 'issues:partial',
      sourceArtifactVersionIds: partialFingerprint,
      reportPath: '/tmp/sales.pbir',
      scoredAt: '2026-06-13T10:15:00.000Z',
      issues: [finding(pageName)],
    })).rejects.toMatchObject({
      message: 'Analyzer outputs reference stale or incomplete approved design artifact versions.',
      diagnostics: expect.arrayContaining([
        expect.stringContaining('Expected approved artifact fingerprint'),
        expect.stringContaining('Missing approved artifact versions'),
      ]),
    });
  });

  it('records lineage and explicit transitions for refinement proposal review and approval', async () => {
    const context = makeContext(makeTempDir());
    const draftState = await createDraftState(context, 'thread-refine-workflow');
    const pageName = draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';
    const created = await ingestIssues(context, 'thread-refine-workflow', {
      analyzerRunId: 'run-workflow-1',
      resultReference: 'issues:workflow',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      reportPath: '/tmp/sales.pbir',
      scoredAt: '2026-06-13T10:20:00.000Z',
      issues: [finding(pageName)],
    });

    const proposalId = created.proposals[0]?.id;
    expect(proposalId).toBeDefined();
    expect(created.proposals[0]?.sourceLineage).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          artifactId: draftState.concept.pageConcepts[0]?.id,
          artifactVersionId: `${draftState.concept.pageConcepts[0]?.id}@v${draftState.concept.pageConcepts[0]?.version}`,
          approvalState: 'approved',
          approvalTimestamp: expect.any(String),
        }),
      ]),
    );

    const reviewed = await reviewRefinementProposal(context, 'thread-refine-workflow', proposalId!);
    expect(reviewed.proposals[0]).toEqual(expect.objectContaining({
      lifecycleState: 'reviewed',
      approvalState: 'pendingApproval',
      approvalKind: 'refinementApproval',
    }));

    const approved = await approveRefinementProposal(context, 'thread-refine-workflow', proposalId!);
    expect(approved.proposals[0]).toEqual(expect.objectContaining({
      lifecycleState: 'approved',
      approvalState: 'approved',
      approvalKind: 'refinementApproval',
    }));
    expect(approved.proposals[0]?.noMutationGuarantee).toEqual(expect.objectContaining({
      directReportMutation: false,
      materializationTriggered: false,
      analyzerHandoffTriggered: false,
      analyzableSurfaceCreated: false,
    }));
  });

  it('allows refinement proposals to be explicitly rejected after review', async () => {
    const context = makeContext(makeTempDir());
    const draftState = await createDraftState(context, 'thread-refine-reject');
    const pageName = draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';
    const created = await ingestIssues(context, 'thread-refine-reject', {
      analyzerRunId: 'run-reject-1',
      resultReference: 'issues:reject',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      reportPath: '/tmp/sales.pbir',
      scoredAt: '2026-06-13T10:25:00.000Z',
      issues: [finding(pageName)],
    });

    const proposalId = created.proposals[0]?.id;
    await reviewRefinementProposal(context, 'thread-refine-reject', proposalId!);
    const rejected = await rejectRefinementProposal(context, 'thread-refine-reject', proposalId!);

    expect(rejected.proposals[0]).toEqual(expect.objectContaining({
      lifecycleState: 'reviewed',
      approvalState: 'rejected',
      approvalKind: 'refinementApproval',
    }));
  });

  it('allows refinement proposals to be explicitly deferred back to pending review state', async () => {
    const context = makeContext(makeTempDir());
    const draftState = await createDraftState(context, 'thread-refine-defer');
    const pageName = draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';
    const created = await ingestIssues(context, 'thread-refine-defer', {
      analyzerRunId: 'run-defer-1',
      resultReference: 'issues:defer',
      sourceArtifactVersionIds: sourceVersionIds(draftState),
      reportPath: '/tmp/sales.pbir',
      scoredAt: '2026-06-13T10:30:00.000Z',
      issues: [finding(pageName)],
    });

    const proposalId = created.proposals[0]?.id;
    await approveRefinementProposal(context, 'thread-refine-defer', proposalId!);
    const deferred = await deferRefinementProposal(context, 'thread-refine-defer', proposalId!);

    expect(deferred.proposals[0]).toEqual(expect.objectContaining({
      lifecycleState: 'reviewed',
      approvalState: 'pendingApproval',
      approvalKind: 'refinementApproval',
      noMutationGuarantee: expect.objectContaining({
        directReportMutation: false,
        pbirAssetGenerationTriggered: false,
        autoApplied: false,
      }),
    }));
  });
});
