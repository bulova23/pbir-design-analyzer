import { buildQuickFixList } from '../analyzer/score/quickFixes';
import type { FrameworkFeedbackItem } from '../analyzer/contracts/scorePanel';

function feedback(
  text: string,
  overrides: Partial<FrameworkFeedbackItem> = {},
): FrameworkFeedbackItem {
  return {
    ok: false,
    text,
    findingType: 'strongHeuristic',
    ...overrides,
  };
}

describe('buildQuickFixList', () => {
  it('returns an empty list when nothing matches', () => {
    const fixes = buildQuickFixList([], []);

    expect(fixes).toEqual([]);
  });

  it('emits ConsolidateFilters from a filter consolidation finding', () => {
    const fixes = buildQuickFixList(
      [],
      [
        feedback(
          'Filter consolidation: Slicers on Overview span the entire page width — pull them into one band.',
          {
            affectedVisuals: [
              { pageName: 'Overview', visualId: 's1', visualType: 'slicer' },
              { pageName: 'Overview', visualId: 's2', visualType: 'slicer' },
            ],
          },
        ),
      ],
    );

    const fix = fixes.find((entry) => entry.operation === 'ConsolidateFilters');
    expect(fix).toBeDefined();
    expect(fix?.affectedVisuals).toHaveLength(2);
  });

  it('emits ConsolidateFilters from a filter placement finding too', () => {
    const fixes = buildQuickFixList(
      [],
      [feedback('Filter placement: Slicer disrupts the primary reading flow.')],
    );

    expect(fixes.some((fix) => fix.operation === 'ConsolidateFilters')).toBe(true);
  });

  it('emits NormalizeCardAlignment from a top-band KPI consistency finding', () => {
    const fixes = buildQuickFixList(
      [],
      [
        feedback(
          'Top-band KPI consistency: KPI cards do not share a uniform Y baseline.',
          {
            affectedVisuals: [
              { pageName: 'Overview', visualId: 'k1', visualType: 'card' },
              { pageName: 'Overview', visualId: 'k2', visualType: 'card' },
            ],
          },
        ),
      ],
    );

    const fix = fixes.find((fix) => fix.operation === 'NormalizeCardAlignment');
    expect(fix).toBeDefined();
    expect(fix?.affectedVisuals).toHaveLength(2);
  });

  it('emits ReplaceDonutWithBar only when pie/donut feedback mentions overview-page usage', () => {
    const overviewFixes = buildQuickFixList(
      [],
      [feedback('Pie avoidance: 2 pie/donut chart(s) detected, including overview-page use — replace.')],
    );
    expect(overviewFixes.some((fix) => fix.operation === 'ReplaceDonutWithBar')).toBe(true);

    const nonOverviewFixes = buildQuickFixList(
      [],
      [feedback('Pie avoidance: 1 pie/donut chart(s) detected — replace with bar or column charts.')],
    );
    expect(nonOverviewFixes.some((fix) => fix.operation === 'ReplaceDonutWithBar')).toBe(false);
  });

  it('emits StandardizeLabelNaming from a metric label consistency finding', () => {
    const fixes = buildQuickFixList(
      [],
      [feedback('Metric label consistency: Mixed `Sum of` prefixes and human-readable labels.')],
    );

    expect(fixes.some((fix) => fix.operation === 'StandardizeLabelNaming')).toBe(true);
  });

  it('skips fixes for passing feedback items (ok === true)', () => {
    const fixes = buildQuickFixList(
      [],
      [feedback('Top-band KPI consistency: All KPI cards share a baseline.', { ok: true })],
    );

    expect(fixes).toEqual([]);
  });

  it('preserves the existing recommendation-driven fixes', () => {
    const fixes = buildQuickFixList([
      '[High] Layout: Snap visuals to the 12-column grid.',
      '[Low] Theme: Reduce data colour variance.',
      '[Medium] Data-Ink: Remove decorative shapes.',
    ]);

    const operations = fixes.map((fix) => fix.operation);
    expect(operations).toEqual(
      expect.arrayContaining(['SnapToGrid', 'ReduceColorVariance', 'RemoveDecorativeElements']),
    );
  });

  it('deduplicates by operation when multiple feedback items trigger the same fix', () => {
    const fixes = buildQuickFixList(
      [],
      [
        feedback('Filter consolidation: Issue A.'),
        feedback('Filter placement: Issue B.'),
      ],
    );

    expect(fixes.filter((fix) => fix.operation === 'ConsolidateFilters')).toHaveLength(1);
  });
});
