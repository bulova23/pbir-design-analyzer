import type {
  FixPlanItem,
  NormalizedFinding,
  NormalizedFindingSeverity,
} from '../../src/analyzer/contracts/scorePanel';
import { buildFixPlan } from '../../src/analyzer/score/fixPlan';

export interface RemediationFilterState {
  severity: NormalizedFindingSeverity | 'all';
  pageName: string | 'all';
  dimension: 'layout' | 'story' | 'accessibility' | 'consistency' | 'navigation' | 'actionability' | 'all';
  impactArea: NormalizedFinding['impactArea'] | 'all';
  scope: NormalizedFinding['scope'] | 'all';
  detectionType: NormalizedFinding['detectionType'] | 'all';
}

export interface RemediationFocus {
  pageName?: string;
  dimension?: Exclude<RemediationFilterState['dimension'], 'all'>;
  impactArea?: NormalizedFinding['impactArea'];
  label: string;
  helperText: string;
}

export interface ContextAwareFixPlanItem extends FixPlanItem {
  coverageBySeverity: Record<NormalizedFindingSeverity, number>;
  findingCoverageLabel: string;
  sourceFindings: NormalizedFinding[];
}

export interface ContextAwareRemediationQueue {
  focus: RemediationFocus;
  items: ContextAwareFixPlanItem[];
}

function capitalize(value: string): string {
  return value[0].toUpperCase() + value.slice(1);
}

function getImpactAreaLabel(impactArea: NormalizedFinding['impactArea']): string {
  switch (impactArea) {
    case 'kpiEffectiveness':
      return 'KPI effectiveness';
    default:
      return capitalize(impactArea);
  }
}

function getDimensionLabel(dimension: Exclude<RemediationFilterState['dimension'], 'all'>): string {
  return capitalize(dimension);
}

function mapDimensionToImpactAreas(dimension: Exclude<RemediationFilterState['dimension'], 'all'>): NormalizedFinding['impactArea'][] {
  switch (dimension) {
    case 'layout':
      return ['layout', 'density'];
    case 'story':
      return ['storytelling', 'kpiEffectiveness', 'benchmark'];
    case 'consistency':
      return ['governance', 'metadata'];
    default:
      return [dimension];
  }
}

function buildFocus(filters: RemediationFilterState, selectedPageName: string | undefined): RemediationFocus {
  const pageName = filters.pageName !== 'all' ? filters.pageName : selectedPageName;
  const dimension = filters.dimension !== 'all' ? filters.dimension : undefined;
  const impactArea = filters.impactArea !== 'all' ? filters.impactArea : undefined;
  const domainLabel = impactArea
    ? getImpactAreaLabel(impactArea)
    : dimension
      ? getDimensionLabel(dimension)
      : 'All problem areas';
  const label = [pageName ?? 'Report-wide', domainLabel].join(' · ');

  return {
    pageName,
    dimension,
    impactArea,
    label,
    helperText: 'Actions are grouped by problem area rather than individual findings. Severity, scope, and detection filters refine Issues but do not fully constrain remediation recommendations.',
  };
}

function matchesFocusedPage(finding: NormalizedFinding, pageName: string | undefined): boolean {
  if (!pageName) {
    return true;
  }

  if (finding.scope === 'report' || finding.affectedPages.length === 0) {
    return true;
  }

  return finding.affectedPages.includes(pageName);
}

function matchesDrivingFilters(
  finding: NormalizedFinding,
  focus: RemediationFocus,
): boolean {
  if (finding.sourceSection !== 'issues') {
    return false;
  }

  if (!matchesFocusedPage(finding, focus.pageName)) {
    return false;
  }

  if (focus.dimension && !mapDimensionToImpactAreas(focus.dimension).includes(finding.impactArea)) {
    return false;
  }

  if (focus.impactArea && finding.impactArea !== focus.impactArea) {
    return false;
  }

  return true;
}

function buildCoverage(sourceFindings: NormalizedFinding[]): Record<NormalizedFindingSeverity, number> {
  return sourceFindings.reduce<Record<NormalizedFindingSeverity, number>>(
    (coverage, finding) => {
      coverage[finding.severity] += 1;
      return coverage;
    },
    {
      high: 0,
      medium: 0,
      low: 0,
      info: 0,
    },
  );
}

function buildCoverageLabel(coverage: Record<NormalizedFindingSeverity, number>): string {
  const parts = [
    coverage.high > 0 ? `${coverage.high} High` : undefined,
    coverage.medium > 0 ? `${coverage.medium} Medium` : undefined,
    coverage.low > 0 ? `${coverage.low} Low` : undefined,
    coverage.info > 0 ? `${coverage.info} Info` : undefined,
  ].filter((part): part is string => Boolean(part));

  return parts.join(' · ');
}

export function buildContextAwareRemediationQueue(args: {
  findings: NormalizedFinding[] | undefined;
  selectedPageName: string | undefined;
  filters: RemediationFilterState;
}): ContextAwareRemediationQueue {
  const focus = buildFocus(args.filters, args.selectedPageName);
  const findings = args.findings ?? [];
  const focusedFindings = findings.filter((finding) => matchesDrivingFilters(finding, focus));
  const findingLookup = new Map(focusedFindings.map((finding) => [finding.id, finding]));
  const items = buildFixPlan(focusedFindings).map((item) => {
    const sourceFindings = item.sourceFindingIds
      .map((findingId) => findingLookup.get(findingId))
      .filter((finding): finding is NormalizedFinding => Boolean(finding));
    const coverageBySeverity = buildCoverage(sourceFindings);

    return {
      ...item,
      coverageBySeverity,
      findingCoverageLabel: buildCoverageLabel(coverageBySeverity),
      sourceFindings,
    };
  });

  return {
    focus,
    items,
  };
}
