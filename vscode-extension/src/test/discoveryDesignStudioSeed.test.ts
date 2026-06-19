import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import { buildDesignStudioWorkspace } from '../design-studio/presentation/designStudioWorkspace';
import { loadConceptState } from '../design-studio/state/conceptStore';
import { loadDesignBriefState } from '../design-studio/state/designBriefStore';
import { loadDraftState } from '../design-studio/state/draftStore';
import { selectDiscoveryRecommendationForDesignStudio } from '../design-studio/state/discoveryStartingPointStore';

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
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-discovery-design-studio-seed-test-'));
}

describe('discoveryStartingPointStore', () => {
  it('selects a recommendation and seeds Design Studio artifacts with lineage while preserving approval boundaries', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);
    const reportPath = '/tmp/Discovery Seed.Report';

    const seeded = await selectDiscoveryRecommendationForDesignStudio(context, {
      reportPath,
      semanticModelSource: '/tmp/models/sales.SemanticModel',
      discoveryProfileId: 'discovery-profile:sales',
      opportunityId: 'opportunity:regional-performance',
      opportunityCategory: 'comparativePerformanceManagement',
      recommendationId: 'recommendation:regional-performance',
      recommendationName: 'Regional Performance Investigation',
      experienceType: 'analyticalInvestigationExperience',
      expectedAudience: 'Analytical',
      expectedBusinessOutcome: 'Investigate regional outliers and drivers.',
      whyWeRecommendIt: 'Strong region and variance semantic coverage.',
      supportingSignals: ['Region analysis support', 'Variance measure support'],
      limitingFactors: ['Requires disciplined drill path design.'],
      confidenceNote: 'High confidence because the semantic model strongly supports this use case.',
      complexityNote: 'High complexity because an analytical drill-based experience needs broader semantic coordination and design shaping.',
      blueprint: {
        blueprintId: 'blueprint:regional-performance',
        recommendedPages: [
          {
            pageName: 'Question',
            pageIntent: 'Frame the business question and target segments.',
            suggestedFilters: ['Date', 'Region'],
            suggestedVisualTypes: ['KpiCard', 'BarChart'],
          },
          {
            pageName: 'Investigation',
            pageIntent: 'Break down the drivers by region and product.',
            suggestedFilters: ['Region', 'Product'],
            suggestedVisualTypes: ['Matrix', 'BarChart'],
          },
          {
            pageName: 'Evidence',
            pageIntent: 'Validate the main signal with detailed evidence.',
            suggestedFilters: ['Customer Segment'],
            suggestedVisualTypes: ['Table', 'TrendLine'],
          },
        ],
        primaryKpis: ['Revenue Variance', 'Gross Margin'],
        suggestedGlobalFilters: ['Date', 'Region', 'Product'],
        analyticalFlow: {
          question: 'What changed?',
          investigation: 'Investigate the highest-variance regions.',
          evidence: 'Confirm the root cause with supporting evidence.',
          decision: 'Choose the next intervention.',
        },
        navigationIntent: {
          flow: 'Question to investigation to evidence.',
          sequence: ['Question', 'Investigation', 'Evidence'],
        },
        successCriteriaSeed: ['Analyst can isolate the top outlier quickly'],
      },
    });

    expect(seeded.selectedRecommendationId).toBe('recommendation:regional-performance');

    const briefState = await loadDesignBriefState(context, seeded.threadId);
    const conceptState = await loadConceptState(context, seeded.threadId);
    const draftState = await loadDraftState(context, seeded.threadId);
    const workspaceState = await buildDesignStudioWorkspace(context, reportPath);

    expect(briefState?.current.audience).toBe('Analytical');
    expect(briefState?.current.businessObjective).toBe('Investigate regional outliers and drivers.');
    expect(briefState?.current.primaryKpis).toEqual(['Revenue Variance', 'Gross Margin']);
    expect(briefState?.current.dimensions).toEqual(['Date', 'Region', 'Product']);
    expect(briefState?.current.approvalState).toBe('notSubmitted');
    expect(briefState?.current.lifecycleState).toBe('draft');
    expect(briefState?.current.approvalKind).toBe('designApproval');
    expect(briefState?.current.validationLinkage).toBeUndefined();
    expect(briefState?.current.provenance.source).toBe('discoveryWizard');
    expect(briefState?.current.provenance.lineage?.map((entry) => entry.stage)).toEqual([
      'semanticModel',
      'discoveryProfile',
      'opportunity',
      'recommendation',
      'experienceBlueprint',
    ]);

    expect(conceptState?.currentConcept.approvalState).toBe('notSubmitted');
    expect(conceptState?.currentConcept.alternateConcepts.length).toBeGreaterThan(1);
    expect(conceptState?.currentConcept.sourceBriefVersionId).toBe(`${briefState?.current.id}@v${briefState?.current.version}`);
    expect(conceptState?.currentConcept.provenance.lineage?.map((entry) => entry.sourceId)).toEqual([
      '/tmp/models/sales.SemanticModel',
      'discovery-profile:sales',
      'opportunity:regional-performance',
      'recommendation:regional-performance',
      'blueprint:regional-performance',
    ]);

    expect(draftState?.currentDraft.approvalState).toBe('notSubmitted');
    expect(draftState?.currentDraft.draftStatus.productionState).toBe('nonProduction');
    expect(draftState?.pageArtifacts).toHaveLength(3);
    expect(draftState?.layoutArtifacts).toHaveLength(3);
    expect(draftState?.navigationArtifacts).toHaveLength(1);
    expect(draftState?.currentDraft.validationLinkage).toBeUndefined();
    expect(draftState?.currentDraft.provenance.lineage?.map((entry) => entry.stage)).toEqual([
      'semanticModel',
      'discoveryProfile',
      'opportunity',
      'recommendation',
      'experienceBlueprint',
    ]);

    expect(workspaceState.threadId).toBe(seeded.threadId);
    expect(workspaceState.currentBrief?.audience).toBe('Analytical');
    expect(workspaceState.workspace.currentStage).toBe('brief');
    expect(workspaceState.workspace.conceptReview?.alternateConcepts?.length).toBeGreaterThan(1);
    expect(workspaceState.workspace.draftReview?.draftPages).toHaveLength(3);
  });
});
