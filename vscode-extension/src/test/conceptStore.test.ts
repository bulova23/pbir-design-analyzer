import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import {
  approveConceptBaseline,
  compareConceptAlternatives,
  generateConceptArtifacts,
  loadConceptState,
  selectConceptBaseline,
} from '../design-studio/state/conceptStore';
import {
  approveDesignBrief,
  saveDesignBriefDraft,
} from '../design-studio/state/designBriefStore';

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
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-concept-store-test-'));
}

async function saveApprovedBrief(context: ExtensionContext, threadId: string): Promise<void> {
  await saveDesignBriefDraft(context, threadId, {
    audience: 'Sales leaders',
    businessObjective: 'Reduce missed renewals',
    keyDecisions: ['Which regions need intervention first'],
    primaryKpis: ['Renewal rate', 'At-risk pipeline'],
    dimensions: ['Region', 'Segment'],
    intendedStory: 'Lead with risk, then explain the main drivers and actions.',
    successCriteria: ['Leader can pick the next intervention within five minutes'],
    reportType: 'dashboard',
    navigationExpectations: 'Overview first, root-cause detail second.',
    consumptionContext: 'Weekly renewal review',
    decisionCadence: 'Weekly',
    narrativeRisksOrConstraints: ['Avoid hiding segment outliers'],
    requiredEvidenceDomains: ['Renewal trend', 'pipeline coverage'],
    targetAnalyzableSurfaceFamily: 'pbir',
  });
  await approveDesignBrief(context, threadId);
}

describe('conceptStore', () => {
  it('blocks concept generation until the design brief is approved', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);

    await saveDesignBriefDraft(context, 'thread-1', {
      audience: 'Sales leaders',
      businessObjective: 'Reduce missed renewals',
      keyDecisions: ['Which regions need intervention first'],
      primaryKpis: ['Renewal rate'],
      dimensions: ['Region'],
      intendedStory: 'Lead with risk, then explain the main drivers.',
      successCriteria: ['Leader can pick a next action quickly'],
      reportType: 'dashboard',
      navigationExpectations: 'Overview first, detail second.',
    });

    await expect(generateConceptArtifacts(context, 'thread-1')).rejects.toThrow(
      'Concept generation requires an approved Design Brief.',
    );
  });

  it('persists internal-only alternate concepts and comparison results without materialization', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);
    await saveApprovedBrief(context, 'thread-2');

    const conceptState = await generateConceptArtifacts(context, 'thread-2');
    const comparison = compareConceptAlternatives(conceptState.currentConcept);

    expect(conceptState.currentConcept.chapterMap.chapters.length).toBeGreaterThan(0);
    expect(conceptState.currentConcept.pageRecommendations.length).toBeGreaterThan(0);
    expect(conceptState.currentConcept.kpiHierarchy.nodes.length).toBeGreaterThan(0);
    expect(conceptState.currentConcept.navigationStructure.sections.length).toBeGreaterThan(0);
    expect(conceptState.currentConcept.analyticalFlow.steps.length).toBeGreaterThan(0);
    expect(conceptState.currentConcept.pageConcepts.length).toBeGreaterThan(0);
    expect(conceptState.currentConcept.sourceBriefId).toBe('design-brief:thread-2');
    expect(conceptState.currentConcept.sourceBriefVersionId).toBe('design-brief:thread-2@v2');
    expect(conceptState.currentConcept.pageConcepts[0]).toEqual(
      expect.objectContaining({
        kind: 'pageConcept',
        sourceBriefVersionId: 'design-brief:thread-2@v2',
        sourceReportConceptVersionId: 'report-concept:thread-2@v1',
        title: expect.any(String),
        intendedPurpose: expect.any(String),
        targetAudienceOrRole: expect.any(String),
        primaryKpis: expect.any(Array),
        supportingDimensions: expect.any(Array),
        intendedStoryQuestion: expect.any(String),
        navigationRole: expect.any(String),
        relatedChapterId: expect.any(String),
        provenance: expect.objectContaining({
          source: 'system',
        }),
      }),
    );
    expect(conceptState.currentConcept.alternateConcepts.length).toBeGreaterThan(1);
    expect(comparison.preferredConceptId).toBe(conceptState.currentConcept.alternateConcepts[0].id);
    expect(comparison.summary).toContain('Baseline concept selected');
    expect(comparison.decisions.length).toBe(conceptState.currentConcept.alternateConcepts.length);
    expect(conceptState.currentConcept).not.toHaveProperty('materialization');
    expect(conceptState.currentConcept).not.toHaveProperty('analyzableSurface');
    expect(fs.existsSync(path.join(tmp, 'design-studio', 'threads'))).toBe(true);
    expect(fs.existsSync(path.join('/Users/me/Workspace', 'concept-studio.json'))).toBe(false);
  });

  it('supports explicit comparison and baseline selection across alternate concepts', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);
    await saveApprovedBrief(context, 'thread-3');

    const conceptState = await generateConceptArtifacts(context, 'thread-3');
    const secondConceptId = conceptState.currentConcept.alternateConcepts[1].id;

    const selected = await selectConceptBaseline(context, 'thread-3', secondConceptId);
    const reloaded = await loadConceptState(context, 'thread-3');

    expect(selected.currentConcept.preferredBaselineConceptId).toBe(secondConceptId);
    expect(selected.currentConcept.approvalState).toBe('pendingApproval');
    expect(selected.currentConcept.lifecycleState).toBe('proposed');
    expect(selected.currentConcept.approvedBaselineConceptId).toBeUndefined();
    expect(selected.currentConcept.comparison?.preferredConceptId).toBe(secondConceptId);
    expect(selected.currentConcept.comparison?.decisions).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          conceptId: secondConceptId,
          disposition: 'preferredBaseline',
        }),
      ]),
    );
    expect(reloaded).toEqual(selected);
  });

  it('requires explicit approval before a concept baseline becomes Draft Studio ready', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);
    await saveApprovedBrief(context, 'thread-4');

    const conceptState = await generateConceptArtifacts(context, 'thread-4');
    const secondConceptId = conceptState.currentConcept.alternateConcepts[1].id;
    const selected = await selectConceptBaseline(context, 'thread-4', secondConceptId);

    expect(selected.currentConcept.preferredBaselineConceptId).toBe(secondConceptId);
    expect(selected.currentConcept.approvedBaselineConceptId).toBeUndefined();
    expect(selected.currentConcept.approvalState).toBe('pendingApproval');
    expect(selected.readiness.canEnterDraftStudio).toBe(false);

    const approved = await approveConceptBaseline(context, 'thread-4');

    expect(approved.currentConcept.version).toBe(3);
    expect(approved.currentConcept.approvedBaselineConceptId).toBe(secondConceptId);
    expect(approved.currentConcept.approvalState).toBe('approved');
    expect(approved.currentConcept.lifecycleState).toBe('approved');
    expect(approved.readiness.canEnterDraftStudio).toBe(true);
    expect(approved.history.map((entry) => entry.version)).toEqual([1, 2, 3]);
    expect(approved.history[1].concept.approvalState).toBe('pendingApproval');
    expect(approved.history[2].concept.approvalState).toBe('approved');
    expect(approved.history[1].concept.approvedBaselineConceptId).toBeUndefined();
    expect(approved.history[2].concept.approvedBaselineConceptId).toBe(secondConceptId);
  });
});
