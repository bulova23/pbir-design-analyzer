import {
  formatPoints,
  getFeedbackCriterionLabel,
  getFindingTypeClassName,
  getFindingTypeLabel,
  getFixOpportunityCategoryLabel,
  getFixOpportunityStateLabel,
  getFixOutcomeStatusLabel,
  getImpactAreaLabel,
  getMatrixStatusClassName,
  getMatrixStatusLabel,
  getNormalizedFindingSeverityClassName,
  getNormalizedFindingSeverityLabel,
  getReadinessBandLabel,
  getReadinessEffortLabel,
  getScopeLabel,
  getScoreTone,
} from './scoreLabels';

describe('getScoreTone', () => {
  it.each([
    [90, 'tone-good'],
    [75, 'tone-good'],
    [74, 'tone-warn'],
    [50, 'tone-warn'],
    [49, 'tone-bad'],
    [0, 'tone-bad'],
  ])('maps score %d to %s', (score, tone) => {
    expect(getScoreTone(score)).toBe(tone);
  });
});

describe('getFeedbackCriterionLabel', () => {
  it('takes the text before the first colon', () => {
    expect(getFeedbackCriterionLabel('Title Clarity: needs work')).toBe('Title Clarity');
  });

  it('falls back to the trimmed full text when there is no colon', () => {
    expect(getFeedbackCriterionLabel('  No separator here  ')).toBe('No separator here');
  });
});

describe('formatPoints', () => {
  it('formats whole numbers without decimals', () => {
    expect(formatPoints(4)).toBe('4');
    expect(formatPoints(4.0)).toBe('4');
  });

  it('formats fractional points to one decimal place', () => {
    expect(formatPoints(4.25)).toBe('4.3');
    expect(formatPoints(4.14)).toBe('4.1');
  });
});

describe('getFindingTypeLabel', () => {
  it.each([
    ['objective', 'Objective'],
    ['stylePreference', 'Style'],
    ['strongHeuristic', 'Heuristic'],
  ] as const)('labels %s as %s', (findingType, label) => {
    expect(getFindingTypeLabel(findingType)).toBe(label);
  });
});

describe('getFindingTypeClassName', () => {
  it.each([
    ['objective', 'finding-badge-objective'],
    ['stylePreference', 'finding-badge-style'],
    ['strongHeuristic', 'finding-badge-heuristic'],
  ] as const)('classes %s as %s', (findingType, className) => {
    expect(getFindingTypeClassName(findingType)).toBe(className);
  });
});

describe('getNormalizedFindingSeverityLabel', () => {
  it.each([
    ['high', 'High severity'],
    ['medium', 'Medium severity'],
    ['low', 'Low severity'],
    ['info', 'Informational'],
  ] as const)('labels %s as %s', (severity, label) => {
    expect(getNormalizedFindingSeverityLabel(severity)).toBe(label);
  });
});

describe('getNormalizedFindingSeverityClassName', () => {
  it.each([
    ['high', 'issue-severity-high'],
    ['medium', 'issue-severity-medium'],
    ['low', 'issue-severity-low'],
    ['info', 'issue-severity-info'],
  ] as const)('classes %s as %s', (severity, className) => {
    expect(getNormalizedFindingSeverityClassName(severity)).toBe(className);
  });
});

describe('getScopeLabel', () => {
  it('special-cases crossPage', () => {
    expect(getScopeLabel('crossPage')).toBe('Cross-page');
  });

  it.each([
    ['visual', 'Visual'],
    ['page', 'Page'],
    ['report', 'Report'],
  ] as const)('capitalizes %s as %s', (scope, label) => {
    expect(getScopeLabel(scope)).toBe(label);
  });
});

describe('getImpactAreaLabel', () => {
  it('special-cases kpiEffectiveness', () => {
    expect(getImpactAreaLabel('kpiEffectiveness')).toBe('KPI effectiveness');
  });

  it.each([
    ['layout', 'Layout'],
    ['storytelling', 'Storytelling'],
    ['accessibility', 'Accessibility'],
    ['governance', 'Governance'],
    ['density', 'Density'],
    ['navigation', 'Navigation'],
    ['benchmark', 'Benchmark'],
    ['actionability', 'Actionability'],
    ['metadata', 'Metadata'],
  ] as const)('capitalizes %s as %s', (impactArea, label) => {
    expect(getImpactAreaLabel(impactArea)).toBe(label);
  });
});

describe('getReadinessBandLabel', () => {
  it.each([
    ['strongCandidate', 'Strong Candidate'],
    ['possibleCandidate', 'Possible Candidate'],
    ['redesignRequired', 'Redesign Required'],
    ['keepAsReport', 'Keep As Report'],
  ] as const)('labels %s as %s', (band, label) => {
    expect(getReadinessBandLabel(band)).toBe(label);
  });
});

describe('getReadinessEffortLabel', () => {
  it('returns Unknown when effort is undefined', () => {
    expect(getReadinessEffortLabel(undefined)).toBe('Unknown');
  });

  it.each([
    ['low', 'Low'],
    ['medium', 'Medium'],
    ['high', 'High'],
  ] as const)('capitalizes %s as %s', (effort, label) => {
    expect(getReadinessEffortLabel(effort)).toBe(label);
  });
});

describe('getFixOpportunityCategoryLabel', () => {
  it.each([
    ['title', 'Title'],
    ['semanticColor', 'Semantic color'],
    ['alignment', 'Alignment'],
    ['spacing', 'Spacing'],
    ['grid', 'Grid'],
    ['navigation', 'Navigation'],
    ['crossPageConsistency', 'Cross-page consistency'],
  ] as const)('labels %s as %s', (category, label) => {
    expect(getFixOpportunityCategoryLabel(category)).toBe(label);
  });
});

describe('getFixOpportunityStateLabel', () => {
  it.each([
    ['Previewed', 'Previewed'],
    ['Approved', 'Approved'],
    ['Applied', 'Applied'],
    ['RolledBack', 'Rolled back'],
    ['Stale', 'Stale'],
    ['FailedValidation', 'Failed validation'],
    ['AppliedWithUnexpectedOutcome', 'Applied with unexpected outcome'],
  ] as const)('labels %s as %s', (state, label) => {
    expect(getFixOpportunityStateLabel(state)).toBe(label);
  });
});

describe('getFixOutcomeStatusLabel', () => {
  it.each([
    ['Resolved', 'Resolved'],
    ['Improved', 'Improved'],
    ['Unchanged', 'Unchanged'],
    ['Unexpected', 'Unexpected'],
  ] as const)('labels %s as %s', (status, label) => {
    expect(getFixOutcomeStatusLabel(status)).toBe(label);
  });
});

describe('getMatrixStatusClassName', () => {
  it.each([
    ['weak', 'matrix-status-weak'],
    ['watch', 'matrix-status-watch'],
    ['strong', 'matrix-status-strong'],
    ['unknown', 'matrix-status-unknown'],
  ] as const)('classes %s as %s', (status, className) => {
    expect(getMatrixStatusClassName(status)).toBe(className);
  });
});

describe('getMatrixStatusLabel', () => {
  it.each([
    ['weak', 'Weak'],
    ['watch', 'Watch'],
    ['strong', 'Strong'],
    ['unknown', 'Unknown'],
  ] as const)('capitalizes %s as %s', (status, label) => {
    expect(getMatrixStatusLabel(status)).toBe(label);
  });
});
