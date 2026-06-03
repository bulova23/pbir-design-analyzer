import type { FixOpportunity } from '../analyzer/contracts/scorePanel';
import { buildFixBatchPreview } from '../analyzer/fixes/fixBatchPreview';

function makeOpportunity(overrides: Partial<FixOpportunity> = {}): FixOpportunity {
  const id = overrides.id ?? 'fix-1';
  return {
    id,
    remediationItemId: overrides.remediationItemId ?? `remediation-${id}`,
    title: overrides.title ?? `Opportunity ${id}`,
    category: overrides.category ?? 'alignment',
    summary: overrides.summary ?? 'Summary',
    confidence: overrides.confidence ?? 95,
    safetyClass: 'safe',
    affectedPages: overrides.affectedPages ?? ['Overview'],
    targetObjectIds: overrides.targetObjectIds ?? ['visual-1'],
    sourceFindingIds: overrides.sourceFindingIds ?? [`finding-${id}`],
    expectedResolutions: overrides.expectedResolutions ?? ['Layout consistency'],
    mutations: overrides.mutations ?? [],
    previewRows: overrides.previewRows ?? [],
    rollbackPlan: overrides.rollbackPlan ?? {
      id: `rollback-${id}`,
      fixOpportunityId: id,
      fileBackups: [{
        targetFile: `/tmp/${id}.json`,
        beforeContent: '{}',
      }],
      reverseMutations: [],
    },
    state: overrides.state ?? 'Previewed',
    outcome: overrides.outcome,
  };
}

describe('buildFixBatchPreview', () => {
  it('merges multiple selected opportunities into one grouped preview', () => {
    const preview = buildFixBatchPreview([
      makeOpportunity({
        id: 'fix-a',
        previewRows: [{
          pageName: 'Overview',
          objectId: 'title-1',
          property: 'title.text',
          before: 'Executive Overview',
          after: 'Overview',
        }],
      }),
      makeOpportunity({
        id: 'fix-b',
        targetObjectIds: ['chart-1'],
        expectedResolutions: ['Density'],
        previewRows: [{
          pageName: 'Overview',
          objectId: 'chart-1',
          property: 'position.y',
          before: 120,
          after: 96,
        }],
      }),
    ]);

    expect(preview.opportunityIds).toEqual(['fix-a', 'fix-b']);
    expect(preview.summary).toMatchObject({
      changedObjectCount: 2,
      changedFileCount: 2,
      expectedOutcomeCount: 2,
    });
  });

  it('groups preview rows by page object and property', () => {
    const preview = buildFixBatchPreview([
      makeOpportunity({
        id: 'fix-a',
        previewRows: [{
          pageName: 'Overview',
          objectId: 'title-1',
          property: 'title.text',
          before: 'Executive Overview',
          after: 'Overview',
        }],
      }),
    ]);

    expect(preview.pageGroups).toEqual([
      expect.objectContaining({
        pageName: 'Overview',
        objectGroups: [
          expect.objectContaining({
            objectId: 'title-1',
            propertyChanges: [
              expect.objectContaining({
                property: 'title.text',
                before: 'Executive Overview',
                after: 'Overview',
              }),
            ],
          }),
        ],
      }),
    ]);
  });

  it('summarizes changed objects and touched files', () => {
    const preview = buildFixBatchPreview([
      makeOpportunity({
        id: 'fix-a',
        targetObjectIds: ['title-1'],
        mutations: [{
          id: 'mutation-1',
          pageName: 'Overview',
          targetObjectId: 'title-1',
          targetFile: '/tmp/overview/title.json',
          propertyPath: 'title.text',
          mutationType: 'setTitleText',
          before: 'Executive Overview',
          after: 'Overview',
        }],
        previewRows: [{
          pageName: 'Overview',
          objectId: 'title-1',
          property: 'title.text',
          before: 'Executive Overview',
          after: 'Overview',
        }],
      }),
    ]);

    expect(preview.summary.touchedFiles).toEqual(['/tmp/overview/title.json']);
    expect(preview.summary.changedObjects).toEqual(['title-1']);
  });

  it('separates mutation facts from expected outcomes', () => {
    const preview = buildFixBatchPreview([
      makeOpportunity({
        id: 'fix-a',
        expectedResolutions: ['Actionability gap', 'Benchmark gap'],
        previewRows: [{
          pageName: 'Overview',
          objectId: 'title-1',
          property: 'title.text',
          before: 'Executive Overview',
          after: 'Overview',
        }],
      }),
    ]);

    expect(preview.mutationFacts).toEqual([
      expect.objectContaining({
        pageName: 'Overview',
        objectId: 'title-1',
        property: 'title.text',
      }),
    ]);
    expect(preview.expectedOutcomes).toEqual(['Actionability gap', 'Benchmark gap']);
  });
});
