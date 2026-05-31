import type { AffectedVisualReference, FrameworkFeedbackItem, PageScore } from '../contracts/scorePanel';

export interface QuickFixOption {
  label: string;
  operation: string;
  detail?: string;
  affectedVisuals?: AffectedVisualReference[];
}

/**
 * Lightweight subset of a feedback item — accepts the shape the host and the webview both
 * already produce so call sites do not have to translate between API and panel types.
 */
export interface QuickFixFeedbackItem {
  text: string;
  ok: boolean;
  affectedVisuals?: AffectedVisualReference[];
}

/**
 * Returns the advisory quick fixes that should accompany a score result. Quick fixes are
 * derived from two evidence streams:
 *
 * 1. The flat recommendations list (severity-tagged strings produced by the scoring service).
 * 2. The per-framework feedback items, when available — these carry the structured finding
 *    prefix (e.g. "Top-band KPI consistency:") plus the affected visuals, both of which let
 *    the panel surface a fix that points at the actual visuals to adjust.
 *
 * Fixes remain advisory in v1: each entry describes what the user should do; no automated
 * mutation is performed.
 */
export function buildQuickFixList(
  recommendations: string[],
  feedback?: ReadonlyArray<QuickFixFeedbackItem | FrameworkFeedbackItem>,
  page?: Pick<PageScore, 'actionabilityBreakdown' | 'benchmarkComparison'>,
): QuickFixOption[] {
  const fixes: QuickFixOption[] = [];

  for (const recommendation of recommendations) {
    if (recommendation.startsWith('[High] Layout:') || recommendation.startsWith('[High] Layout')) {
      fixes.push({ label: 'Snap visuals to grid', operation: 'SnapToGrid' });
    } else if (recommendation.startsWith('[Low] Theme:') && recommendation.includes('colour')) {
      fixes.push({ label: 'Normalise colour palette', operation: 'ReduceColorVariance' });
    } else if (recommendation.startsWith('[Medium] Data-Ink:') && recommendation.includes('decorative')) {
      fixes.push({
        label: 'Remove decorative elements to improve data-ink ratio',
        operation: 'RemoveDecorativeElements',
      });
    }
  }

  if (feedback && feedback.length > 0) {
    for (const item of feedback) {
      if (item.ok) {
        continue;
      }

      if (
        item.text.startsWith('Filter consolidation:') ||
        item.text.startsWith('Filter placement:')
      ) {
        fixes.push({
          label: 'Consolidate filters into a single top band or left rail',
          operation: 'ConsolidateFilters',
          detail:
            'Move scattered slicers into one consistent control band so the reading flow is not interrupted.',
          affectedVisuals: item.affectedVisuals,
        });
        continue;
      }

      if (item.text.startsWith('Top-band KPI consistency:')) {
        fixes.push({
          label: 'Normalise top-band KPI card alignment',
          operation: 'NormalizeCardAlignment',
          detail:
            'Align the KPI cards along a shared Y baseline and uniform card height so the top band reads as a single layer.',
          affectedVisuals: item.affectedVisuals,
        });
        continue;
      }

      if (
        item.text.startsWith('Pie avoidance:') &&
        item.text.toLowerCase().includes('overview-page use')
      ) {
        fixes.push({
          label: 'Replace overview-page pie/donut with a bar or column chart',
          operation: 'ReplaceDonutWithBar',
          detail:
            'Landing pages benefit from exact comparison — swap pie/donut visuals on overview pages for a bar or clustered column chart.',
          affectedVisuals: item.affectedVisuals,
        });
        continue;
      }

      if (item.text.startsWith('Metric label consistency:')) {
        fixes.push({
          label: 'Standardise KPI and metric label naming',
          operation: 'StandardizeLabelNaming',
          detail:
            'Keep modifier placement consistent and replace auto-generated `Sum of …` labels with human-readable measure names.',
          affectedVisuals: item.affectedVisuals,
        });
        continue;
      }

      if (item.text.startsWith('Visible page purpose:')) {
        fixes.push({
          label: 'Rewrite the page title around the decision or question',
          operation: 'RewritePageTitle',
          detail:
            'Replace vague titles with a decision-led headline so users know what this page should answer in the first scan.',
          affectedVisuals: item.affectedVisuals,
        });
        continue;
      }

      if (item.text.startsWith('Sequential fit:') || item.text.startsWith('Relationship fit:') || item.text.startsWith('Composition fit:')) {
        fixes.push({
          label: 'Replace the chart with a better analytical fit',
          operation: 'ReplaceChartForIntent',
          detail:
            'Swap the current visual for a chart family that matches the task more directly, then keep the supporting fields unchanged.',
          affectedVisuals: item.affectedVisuals,
        });
        continue;
      }
    }
  }

  if (page?.actionabilityBreakdown && !page.actionabilityBreakdown.targetBenchmarkPresent) {
    fixes.push({
      label: 'Add target, benchmark, or prior-period context to the KPI layer',
      operation: 'AddKpiContext',
      detail:
        'Pair each headline KPI with a target, budget, variance, or prior-period reference so the value can drive a decision.',
    });
  }

  if (page?.actionabilityBreakdown && !page.actionabilityBreakdown.drillPathPresent) {
    fixes.push({
      label: 'Separate overview from detail with a supporting evidence path',
      operation: 'AddOverviewDetailSeparation',
      detail:
        'Keep the headline message in the first scan path, then add one supporting chart or detail table that explains why the KPI moved.',
    });
  }

  if (page?.benchmarkComparison?.beautifulButUseless) {
    fixes.push({
      label: 'Normalize semantic colors around action states',
      operation: 'NormalizeSemanticColors',
      detail:
        'Use one stable status palette so the page does not rely on polish alone to imply meaning.',
    });
  }

  return fixes.filter(
    (fix, index, entries) => entries.findIndex((entry) => entry.operation === fix.operation) === index,
  );
}
