import type {
  GuidedStoryImprovement,
  NormalizedFinding,
} from '../analyzer/contracts/scorePanel';
import type { CrossPageNarrativeAnalyzerOutput } from '../design-studio/state/refinementStore';
import { resolveDesignArtifactBacklinks } from '../design-studio/navigation/designArtifactBacklinkResolver';
import type { DraftState } from '../design-studio/state/draftStore';
import {
  generateDraftArtifacts,
} from '../design-studio/state/draftStore';
import {
  approveConceptBaseline,
  generateConceptArtifacts,
} from '../design-studio/state/conceptStore';
import {
  approveDesignBrief,
  saveDesignBriefDraft,
} from '../design-studio/state/designBriefStore';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';

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
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-backlink-resolver-test-'));
}

async function createDraftState(context: ExtensionContext, threadId: string): Promise<DraftState> {
  await saveDesignBriefDraft(context, threadId, {
    audience: 'Sales leaders',
    businessObjective: 'Reduce missed renewals',
    keyDecisions: ['Which region needs intervention first'],
    primaryKpis: ['Renewal rate', 'At-risk pipeline'],
    dimensions: ['Region', 'Segment'],
    intendedStory: 'Lead with risk, then isolate the main drivers and next steps.',
    successCriteria: ['Leader can choose the next intervention within five minutes'],
    reportType: 'dashboard',
    navigationExpectations: 'Overview first, detail second.',
    consumptionContext: 'Weekly renewal review',
    decisionCadence: 'Weekly',
    narrativeRisksOrConstraints: ['Avoid hiding segment outliers'],
    requiredEvidenceDomains: ['trend', 'pipeline coverage'],
    targetAnalyzableSurfaceFamily: 'pbir',
  });
  await approveDesignBrief(context, threadId);
  await generateConceptArtifacts(context, threadId);
  await approveConceptBaseline(context, threadId);
  return generateDraftArtifacts(context, threadId);
}

function improvement(overrides: Partial<GuidedStoryImprovement> = {}): GuidedStoryImprovement {
  return {
    id: 'gsi-1',
    title: 'Clarify the main question',
    summary: 'The page does not clearly frame the decision it supports.',
    rationale: 'The narrative setup is weak.',
    expectedImpact: 'Leaders understand the intended action faster.',
    priority: 'high',
    relatedImpactArea: 'storytelling',
    navigationTarget: {
      kind: 'page',
      pageName: 'Executive overview',
      label: 'Executive overview',
      reason: 'Story clarity issue',
      supportState: 'direct',
    },
    ...overrides,
  };
}

function finding(overrides: Partial<NormalizedFinding> = {}): NormalizedFinding {
  return {
    id: 'finding-1',
    title: 'Navigation flow is fragmented',
    summary: 'Users do not have a clear path from summary to detail.',
    severity: 'high',
    confidence: 0.93,
    scope: 'crossPage',
    detectionType: 'deterministic',
    affectedPages: ['Executive overview'],
    impactArea: 'navigation',
    frameworkImpact: ['narrative'],
    recommendation: 'Simplify the navigation sequence and clarify section ownership.',
    sourceKind: 'storyAssessment',
    sourceSection: 'issues',
    evidence: [
      {
        kind: 'storyAssessment',
        label: 'Story flow breakdown',
        pageName: 'Executive overview',
      },
    ],
    ...overrides,
  };
}

describe('designArtifactBacklinkResolver', () => {
  it('resolves page, draft, layout, navigation, and KPI backlinks from analyzer evidence', async () => {
    const context = makeContext(makeTempDir());
    const draftState = await createDraftState(context, 'thread-backlinks');
    const pageName = draftState.pageArtifacts[0]?.structureSummary.includes('Executive overview')
      ? 'Executive overview'
      : draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';

    const links = resolveDesignArtifactBacklinks(draftState, {
      analyzerSource: 'issues',
      analyzerReferenceId: 'finding-1',
      pageNames: [pageName],
      impactAreas: ['navigation', 'kpiEffectiveness'],
      findingIds: ['finding-1'],
    });

    expect(links).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ artifactKind: 'pageConcept' }),
        expect.objectContaining({ artifactKind: 'draftPageArtifact' }),
        expect.objectContaining({ artifactKind: 'draftLayoutArtifact' }),
        expect.objectContaining({ artifactKind: 'navigationConcept' }),
        expect.objectContaining({ artifactKind: 'kpiHierarchyConcept' }),
      ]),
    );
    expect(new Set(links.map((link) => link.artifactVersionId)).size).toBe(links.length);
  });

  it('uses navigation targets and report-level narrative hints when page names are sparse', async () => {
    const context = makeContext(makeTempDir());
    const draftState = await createDraftState(context, 'thread-backlinks-2');
    const pageName = draftState.concept.pageConcepts[0]?.title ?? 'Executive overview';
    const narrative: CrossPageNarrativeAnalyzerOutput = {
      scoreSummary: {
        score: 61,
        confidence: 'medium',
        dominantObjective: 'executive performance review',
      },
      gaps: [
        {
          id: 'missingExecutiveEntryPoint',
          title: 'Missing executive entry point',
          summary: 'The report lacks a clear opening summary page.',
          affectedPageNames: [pageName],
        },
      ],
      narrativePath: [pageName, draftState.concept.pageConcepts[1]?.title ?? 'Region hotspots'],
      summary: 'The narrative path should move from overview to supporting evidence.',
    };

    const links = resolveDesignArtifactBacklinks(draftState, {
      analyzerSource: 'crossPageNarrative',
      analyzerReferenceId: 'cross-page-1',
      pageNames: [],
      impactAreas: ['navigation', 'storytelling'],
      narrative,
      findingIds: ['missingExecutiveEntryPoint'],
    });

    expect(links).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ artifactKind: 'navigationConcept' }),
        expect.objectContaining({ artifactKind: 'pageConcept' }),
      ]),
    );
  });

  it('resolves stable backlink identities even after concept titles change', async () => {
    const context = makeContext(makeTempDir());
    const draftState = await createDraftState(context, 'thread-backlinks-3');
    const originalPage = draftState.concept.pageConcepts[0];
    expect(originalPage).toBeDefined();

    draftState.concept.pageConcepts[0] = {
      ...originalPage,
      title: 'Renamed executive landing page',
    };

    const links = resolveDesignArtifactBacklinks(draftState, {
      analyzerSource: 'issues',
      analyzerReferenceId: 'finding-stable-1',
      pageNames: ['Executive overview'],
      impactAreas: ['navigation'],
      findingIds: ['finding-stable-1'],
      stableArtifactIdentities: [
        {
          designArtifactId: originalPage!.id,
          designArtifactVersionId: `${originalPage!.id}@v${originalPage!.version}`,
          draftArtifactId: draftState.pageArtifacts[0]!.id,
          draftArtifactVersionId: `${draftState.pageArtifacts[0]!.id}@v${draftState.pageArtifacts[0]!.version}`,
        },
      ],
    });

    expect(links).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          artifactId: originalPage!.id,
          artifactKind: 'pageConcept',
          artifactVersionId: `${originalPage!.id}@v${originalPage!.version}`,
        }),
        expect.objectContaining({
          artifactId: draftState.pageArtifacts[0]!.id,
          artifactKind: 'draftPageArtifact',
          artifactVersionId: `${draftState.pageArtifacts[0]!.id}@v${draftState.pageArtifacts[0]!.version}`,
        }),
      ]),
    );
  });
});
