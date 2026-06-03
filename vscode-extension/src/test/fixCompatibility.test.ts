import type { FixOpportunity } from '../analyzer/contracts/scorePanel';
import { evaluateFixOpportunityCompatibility } from '../analyzer/fixes/fixCompatibility';

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
    mutations: overrides.mutations ?? [
      {
        id: `mutation-${id}`,
        pageName: 'Overview',
        targetObjectId: 'visual-1',
        targetFile: `/tmp/${id}.json`,
        propertyPath: 'position.x',
        mutationType: 'setPosition',
        before: 12,
        after: 24,
      },
    ],
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

describe('evaluateFixOpportunityCompatibility', () => {
  it('blocks overlapping property mutations on the same object', () => {
    const first = makeOpportunity({ id: 'fix-a' });
    const second = makeOpportunity({ id: 'fix-b' });

    const result = evaluateFixOpportunityCompatibility([first, second]);

    expect(result.isCompatible).toBe(false);
    expect(result.blockingReasons).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          code: 'overlappingMutation',
          opportunityIds: ['fix-a', 'fix-b'],
        }),
      ]),
    );
  });

  it('blocks incompatible opportunity categories even when properties do not overlap', () => {
    const navigation = makeOpportunity({
      id: 'fix-nav',
      category: 'navigation',
      targetObjectIds: ['nav-1'],
      mutations: [{
        id: 'mutation-nav',
        targetObjectId: 'nav-1',
        targetFile: '/tmp/nav.json',
        propertyPath: 'position.x',
        mutationType: 'setNavigationPlacement',
        before: 'left',
        after: 'top',
      }],
    });
    const crossPage = makeOpportunity({
      id: 'fix-cross',
      category: 'crossPageConsistency',
      targetObjectIds: ['title-1'],
      mutations: [{
        id: 'mutation-cross',
        pageName: 'Overview',
        targetObjectId: 'title-1',
        targetFile: '/tmp/title.json',
        propertyPath: 'title.text',
        mutationType: 'setTitleText',
        before: 'Overview',
        after: 'Executive overview',
      }],
    });

    const result = evaluateFixOpportunityCompatibility([navigation, crossPage]);

    expect(result.isCompatible).toBe(false);
    expect(result.blockingReasons).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          code: 'incompatibleCategory',
          opportunityIds: ['fix-nav', 'fix-cross'],
        }),
      ]),
    );
  });

  it('flags stale previews when the target snapshot changes', () => {
    const stale = makeOpportunity({
      id: 'fix-stale',
      state: 'Stale',
    });

    const result = evaluateFixOpportunityCompatibility([stale]);

    expect(result.isCompatible).toBe(false);
    expect(result.blockingReasons).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          code: 'staleOpportunity',
          opportunityIds: ['fix-stale'],
        }),
      ]),
    );
  });

  it('flags changed target object detection from explicit drift state', () => {
    const drifted = makeOpportunity({
      id: 'fix-drifted',
      state: 'FailedValidation',
    });

    const result = evaluateFixOpportunityCompatibility([drifted]);

    expect(result.isCompatible).toBe(false);
    expect(result.blockingReasons).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          code: 'targetDrifted',
          opportunityIds: ['fix-drifted'],
        }),
      ]),
    );
  });

  it('keeps non-overlapping selections compatible', () => {
    const first = makeOpportunity({ id: 'fix-a' });
    const second = makeOpportunity({
      id: 'fix-b',
      targetObjectIds: ['visual-2'],
      mutations: [{
        id: 'mutation-b',
        pageName: 'Overview',
        targetObjectId: 'visual-2',
        targetFile: '/tmp/fix-b.json',
        propertyPath: 'position.y',
        mutationType: 'setPosition',
        before: 40,
        after: 24,
      }],
    });

    const result = evaluateFixOpportunityCompatibility([first, second]);

    expect(result).toMatchObject({
      isCompatible: true,
      compatibleOpportunityIds: ['fix-a', 'fix-b'],
      blockingReasons: [],
    });
  });
});
