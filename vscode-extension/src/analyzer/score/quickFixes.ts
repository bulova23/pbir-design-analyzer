export interface QuickFixOption {
  label: string;
  operation: string;
}

export function buildQuickFixList(recommendations: string[]): QuickFixOption[] {
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

  return fixes.filter((fix, index, entries) => entries.findIndex((entry) => entry.operation === fix.operation) === index);
}
