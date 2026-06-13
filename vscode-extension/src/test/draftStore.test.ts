import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import {
  createDraftProviderCapabilityPlaceholder,
  type DraftProviderAdapter,
} from '../design-studio/providers/draftProviderAdapter';
import {
  generateDraftArtifacts,
  loadDraftState,
} from '../design-studio/state/draftStore';
import {
  approveConceptBaseline,
  generateConceptArtifacts,
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
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-draft-store-test-'));
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

async function saveApprovedConcept(context: ExtensionContext, threadId: string): Promise<void> {
  await saveApprovedBrief(context, threadId);
  await generateConceptArtifacts(context, threadId);
  await approveConceptBaseline(context, threadId);
}

describe('draftStore', () => {
  it('requires an approved design brief before Draft Studio can generate artifacts', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);

    await expect(generateDraftArtifacts(context, 'thread-1')).rejects.toThrow(
      'Draft generation requires an approved Design Brief.',
    );
  });

  it('requires an approved concept baseline before Draft Studio can generate artifacts', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);
    await saveApprovedBrief(context, 'thread-2');
    await generateConceptArtifacts(context, 'thread-2');

    await expect(generateDraftArtifacts(context, 'thread-2')).rejects.toThrow(
      'Draft generation requires an approved Concept baseline.',
    );
  });

  it('preserves PageConcept lineage, versions draft artifacts, and works without providers', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);
    await saveApprovedConcept(context, 'thread-3');

    const initial = await generateDraftArtifacts(context, 'thread-3');
    const updated = await generateDraftArtifacts(context, 'thread-3');
    const reloaded = await loadDraftState(context, 'thread-3');

    expect(initial.providerCapabilities).toEqual([]);
    expect(initial.currentDraft.version).toBe(1);
    expect(updated.currentDraft.version).toBe(2);
    expect(updated.history).toHaveLength(2);
    expect(updated.currentDraft.draftStatus).toEqual({
      isolation: 'isolated',
      reviewability: 'reviewable',
      productionState: 'nonProduction',
    });
    expect(updated.currentDraft.pageArtifactIds).toHaveLength(updated.pageArtifacts.length);
    expect(updated.currentDraft.layoutArtifactIds).toHaveLength(updated.layoutArtifacts.length);
    expect(updated.currentDraft.navigationArtifactIds).toHaveLength(updated.navigationArtifacts.length);
    expect(updated.pageArtifacts).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          kind: 'draftPageArtifact',
          pageConceptId: expect.any(String),
          draftStatus: {
            isolation: 'isolated',
            reviewability: 'reviewable',
            productionState: 'nonProduction',
          },
        }),
      ]),
    );
    expect(updated.layoutArtifacts).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          kind: 'draftLayoutArtifact',
          pageConceptId: expect.any(String),
          layoutType: expect.any(String),
        }),
      ]),
    );
    expect(updated.navigationArtifacts[0]).toEqual(
      expect.objectContaining({
        kind: 'draftNavigationArtifact',
        frameworkType: expect.any(String),
        draftStatus: {
          isolation: 'isolated',
          reviewability: 'reviewable',
          productionState: 'nonProduction',
        },
      }),
    );
    expect(updated.pageArtifacts.map((artifact) => artifact.pageConceptId)).toEqual(
      updated.concept.pageConcepts.map((pageConcept) => pageConcept.id),
    );
    expect(updated.currentDraft.sourceBriefVersionId).toBe(`${updated.brief.id}@v${updated.brief.version}`);
    expect(updated.currentDraft.sourceConceptVersionId).toBe(`${updated.concept.id}@v${updated.concept.version}`);
    expect(updated.currentDraft.sourceNavigationConceptVersionId).toBe(
      `${updated.concept.navigationStructure.id}@v${updated.concept.navigationStructure.version}`,
    );
    expect(updated.pageArtifacts).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          sourceBriefVersionId: `${updated.brief.id}@v${updated.brief.version}`,
          sourceConceptVersionId: `${updated.concept.id}@v${updated.concept.version}`,
          sourcePageConceptVersionId: expect.any(String),
        }),
      ]),
    );
    expect(updated.layoutArtifacts).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          sourceBriefVersionId: `${updated.brief.id}@v${updated.brief.version}`,
          sourceConceptVersionId: `${updated.concept.id}@v${updated.concept.version}`,
          sourcePageConceptVersionId: expect.any(String),
        }),
      ]),
    );
    expect(updated.navigationArtifacts).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          sourceBriefVersionId: `${updated.brief.id}@v${updated.brief.version}`,
          sourceConceptVersionId: `${updated.concept.id}@v${updated.concept.version}`,
          sourceNavigationConceptVersionId: `${updated.concept.navigationStructure.id}@v${updated.concept.navigationStructure.version}`,
        }),
      ]),
    );
    expect(updated.currentDraft).not.toHaveProperty('derivedSurface');
    expect(updated.currentDraft).not.toHaveProperty('materializationDiagnostics');
    expect(updated.currentDraft).not.toHaveProperty('targetSurfaceType');
    expect(fs.existsSync(path.join(tmp, 'design-studio', 'threads'))).toBe(true);
    expect(reloaded).toEqual(updated);
  });

  it('stores provider provenance and capability placeholders when a provider adapter is supplied', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);
    await saveApprovedConcept(context, 'thread-4');

    const adapter: DraftProviderAdapter = {
      providerId: 'provider.mock',
      displayName: 'Mock provider',
      capabilities: [
        createDraftProviderCapabilityPlaceholder({
          providerId: 'provider.mock',
          providerDisplayName: 'Mock provider',
          capabilityId: 'draft-layouts',
          capabilityKind: 'generationAssistance',
          supportedArtifactKinds: ['draftLayoutArtifact', 'draftNavigationArtifact'],
          supportedSurfaceFamilies: ['pbir'],
          requiresExternalService: false,
          supportsOfflineOperation: true,
          trustPosture: 'advisoryOnly',
          provenanceRequirements: 'required',
          failureBehavior: 'degradeGracefully',
        }),
      ],
      async proposeDraftArtifacts() {
        return {
          requestId: 'request-123',
          proposalId: 'proposal-456',
          capabilityId: 'draft-layouts',
          capabilityKind: 'generationAssistance',
          modelOrEngineName: 'mock-engine',
          modelOrEngineVersion: '2026.06',
          reportSummary: 'Provider-assisted draft summary',
          pageStructures: {
            'page-concept:thread-4:page-overview': {
              structureSummary: 'Provider-adjusted executive overview scaffold.',
              recommendedVisualRoles: ['scorecard', 'trend'],
            },
          },
          layoutFrameworks: {
            'page-concept:thread-4:page-overview': {
              layoutType: 'providerKpiGrid',
              title: 'Provider KPI Grid',
              zones: ['topRow', 'detailRow'],
            },
          },
          navigationFramework: {
            frameworkType: 'guidedProviderFlow',
            sectionLabelsByPageConceptId: {
              'page-concept:thread-4:page-overview': 'Provider Priorities',
            },
          },
          provenanceNotes: ['Provider suggested layout framing only.'],
        };
      },
    };

    const state = await generateDraftArtifacts(context, 'thread-4', { adapter });

    expect(state.providerCapabilities).toEqual(adapter.capabilities);
    expect(state.currentDraft.summary).toBe('Provider-assisted draft summary');
    expect(state.currentDraft.provenance).toEqual(
      expect.objectContaining({
        source: 'provider',
        providerId: 'provider.mock',
        providerDisplayName: 'Mock provider',
        providerCapabilityId: 'draft-layouts',
        providerCapabilityKind: 'generationAssistance',
        requestId: 'request-123',
        proposalId: 'proposal-456',
        modelOrEngineName: 'mock-engine',
        modelOrEngineVersion: '2026.06',
        artifactAttribution: expect.objectContaining({
          artifactId: state.currentDraft.id,
          artifactKind: 'draftReportArtifact',
        }),
        timestamp: expect.any(String),
        notes: expect.arrayContaining(['Provider suggested layout framing only.']),
      }),
    );
    expect(state.layoutArtifacts[0].provenance.providerId).toBe('provider.mock');
    expect(state.layoutArtifacts[0].provenance.artifactAttribution).toEqual(
      expect.objectContaining({
        artifactId: state.layoutArtifacts[0].id,
        artifactKind: 'draftLayoutArtifact',
      }),
    );
    expect(state.navigationArtifacts[0].frameworkType).toBe('guidedProviderFlow');
  });

  it('does not materialize surfaces, deploy outputs, or mutate reports while generating drafts', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);
    await saveApprovedConcept(context, 'thread-5');

    const state = await generateDraftArtifacts(context, 'thread-5');
    const persisted = fs.readFileSync(
      path.join(tmp, 'design-studio', 'threads', fs.readdirSync(path.join(tmp, 'design-studio', 'threads'))[0], 'draft-studio.json'),
      'utf8',
    );

    expect(JSON.stringify(state)).not.toContain('MaterializedSurfaceCandidate');
    expect(JSON.stringify(state)).not.toContain('derivedSurface');
    expect(JSON.stringify(state)).not.toContain('deploy');
    expect(JSON.stringify(state)).not.toContain('apply');
    expect(persisted).not.toContain('MaterializedSurfaceCandidate');
    expect(persisted).not.toContain('derivedSurface');
    expect(persisted).not.toContain('deploy');
  });
});
