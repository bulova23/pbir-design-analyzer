import type {
  CrossPageMatrixCell,
  FabricAppReadinessBand,
  FabricAppRedesignEffort,
  FindingType,
  FixOpportunityCategory,
  FixOpportunityState,
  FixOutcomeStatus,
  NormalizedFinding,
  NormalizedFindingSeverity,
} from '../../../src/analyzer/contracts/scorePanel';

export function getScoreTone(score: number): string {
  if (score >= 75) {
    return 'tone-good';
  }

  if (score >= 50) {
    return 'tone-warn';
  }

  return 'tone-bad';
}

export function getFeedbackCriterionLabel(text: string): string {
  const separatorIndex = text.indexOf(':');
  return separatorIndex > 0 ? text.slice(0, separatorIndex).trim() : text.trim();
}

export function formatPoints(points: number): string {
  const rounded = Math.round(points * 10) / 10;
  return Number.isInteger(rounded) ? rounded.toFixed(0) : rounded.toFixed(1);
}

export function getFindingTypeLabel(findingType: FindingType): string {
  switch (findingType) {
    case 'objective':
      return 'Objective';
    case 'stylePreference':
      return 'Style';
    default:
      return 'Heuristic';
  }
}

export function getFindingTypeClassName(findingType: FindingType): string {
  switch (findingType) {
    case 'objective':
      return 'finding-badge-objective';
    case 'stylePreference':
      return 'finding-badge-style';
    default:
      return 'finding-badge-heuristic';
  }
}

export function getNormalizedFindingSeverityLabel(severity: NormalizedFindingSeverity): string {
  switch (severity) {
    case 'high':
      return 'High severity';
    case 'medium':
      return 'Medium severity';
    case 'low':
      return 'Low severity';
    default:
      return 'Informational';
  }
}

export function getNormalizedFindingSeverityClassName(severity: NormalizedFindingSeverity): string {
  switch (severity) {
    case 'high':
      return 'issue-severity-high';
    case 'medium':
      return 'issue-severity-medium';
    case 'low':
      return 'issue-severity-low';
    default:
      return 'issue-severity-info';
  }
}

export function getScopeLabel(scope: NormalizedFinding['scope']): string {
  switch (scope) {
    case 'crossPage':
      return 'Cross-page';
    default:
      return scope[0].toUpperCase() + scope.slice(1);
  }
}

export function getImpactAreaLabel(impactArea: NormalizedFinding['impactArea']): string {
  switch (impactArea) {
    case 'kpiEffectiveness':
      return 'KPI effectiveness';
    default:
      return impactArea[0].toUpperCase() + impactArea.slice(1);
  }
}

export function getReadinessBandLabel(band: FabricAppReadinessBand): string {
  switch (band) {
    case 'strongCandidate':
      return 'Strong Candidate';
    case 'possibleCandidate':
      return 'Possible Candidate';
    case 'redesignRequired':
      return 'Redesign Required';
    default:
      return 'Keep As Report';
  }
}

export function getReadinessEffortLabel(effort: FabricAppRedesignEffort | undefined): string {
  if (!effort) {
    return 'Unknown';
  }

  return effort[0].toUpperCase() + effort.slice(1);
}

export function getFixOpportunityCategoryLabel(category: FixOpportunityCategory): string {
  switch (category) {
    case 'title':
      return 'Title';
    case 'semanticColor':
      return 'Semantic color';
    case 'alignment':
      return 'Alignment';
    case 'spacing':
      return 'Spacing';
    case 'grid':
      return 'Grid';
    case 'navigation':
      return 'Navigation';
    default:
      return 'Cross-page consistency';
  }
}

export function getFixOpportunityStateLabel(state: FixOpportunityState): string {
  switch (state) {
    case 'Previewed':
      return 'Previewed';
    case 'Approved':
      return 'Approved';
    case 'Applied':
      return 'Applied';
    case 'RolledBack':
      return 'Rolled back';
    case 'Stale':
      return 'Stale';
    case 'FailedValidation':
      return 'Failed validation';
    default:
      return 'Applied with unexpected outcome';
  }
}

export function getFixOutcomeStatusLabel(status: FixOutcomeStatus): string {
  switch (status) {
    case 'Resolved':
      return 'Resolved';
    case 'Improved':
      return 'Improved';
    case 'Unchanged':
      return 'Unchanged';
    default:
      return 'Unexpected';
  }
}

export function getMatrixStatusClassName(status: CrossPageMatrixCell['status']): string {
  switch (status) {
    case 'weak':
      return 'matrix-status-weak';
    case 'watch':
      return 'matrix-status-watch';
    case 'strong':
      return 'matrix-status-strong';
    default:
      return 'matrix-status-unknown';
  }
}

export function getMatrixStatusLabel(status: CrossPageMatrixCell['status']): string {
  return status[0].toUpperCase() + status.slice(1);
}
