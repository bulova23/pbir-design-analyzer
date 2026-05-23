import type { AffectedVisualReference, FrameworkFeedbackItem } from '../contracts/scorePanel';

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
    }
  }

  return fixes.filter(
    (fix, index, entries) => entries.findIndex((entry) => entry.operation === fix.operation) === index,
  );
}
