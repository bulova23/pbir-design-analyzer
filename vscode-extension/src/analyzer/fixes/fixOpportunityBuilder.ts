import type {
  FixOpportunity,
  FixOpportunityCategory,
  FixPlanItem,
  ScoreResult,
} from '../contracts/scorePanel';
import { planMutationsForCategory } from './fixMutationPlanner';
import { buildFixPreviewRows } from './fixPreview';
import { buildRollbackPlan } from './rollbackPlanBuilder';

function inferOpportunityCategories(item: FixPlanItem, result: ScoreResult): Array<{ category: FixOpportunityCategory; pageName?: string }> {
  if (item.title === 'Clarify page purpose and narrative framing') {
    return item.affectedPages.slice(0, 1).map((pageName) => ({ category: 'title', pageName }));
  }

  if (item.title === 'Reduce visual density and align layout') {
    return item.affectedPages.slice(0, 1).map((pageName) => ({ category: 'alignment', pageName }));
  }

  if (item.title === 'Standardize navigation cues') {
    return [{ category: 'navigation' }];
  }

  if (item.title === 'Normalize cross-page standards') {
    const categories: Array<{ category: FixOpportunityCategory; pageName?: string }> = [{
      category: 'crossPageConsistency',
    }];
    const pages = result.pageScores ?? [];
    const assignments = pages.flatMap((page) => page.visualMetadata?.semanticColorMap ?? []);
    const colors = [...new Set(assignments.map((assignment) => assignment.color.toLowerCase()))];
    if (colors.length > 1) {
      categories.push({ category: 'semanticColor' });
    }
    return categories;
  }

  return [];
}

function buildOpportunityId(item: FixPlanItem, category: FixOpportunityCategory, pageName?: string): string {
  return [item.id, category, pageName].filter(Boolean).join(':');
}

export function buildFixOpportunities(result: ScoreResult): FixOpportunity[] {
  const fixPlan = result.fixPlan ?? [];
  const opportunities = fixPlan.flatMap((item) => inferOpportunityCategories(item, result)
    .map(({ category, pageName }): FixOpportunity | undefined => {
      const mutations = planMutationsForCategory({
        category,
        result,
        pageName,
        affectedPages: item.affectedPages,
      });
      if (mutations.length === 0) {
        return undefined;
      }

      const id = buildOpportunityId(item, category, pageName);
      return {
        id,
        remediationItemId: item.id,
        title: `${item.title} (${category})`,
        category,
        summary: item.why,
        confidence: 95,
        safetyClass: 'safe',
        affectedPages: pageName ? [pageName] : item.affectedPages,
        targetObjectIds: [...new Set(mutations.map((mutation) => mutation.targetObjectId))],
        sourceFindingIds: item.sourceFindingIds,
        expectedResolutions: item.resolvedOutcomes,
        mutations,
        previewRows: buildFixPreviewRows(mutations),
        rollbackPlan: buildRollbackPlan(id, mutations),
        state: 'Previewed',
      } satisfies FixOpportunity;
    }));

  return opportunities.filter((item): item is FixOpportunity => item !== undefined);
}
