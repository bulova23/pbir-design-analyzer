import type {
  CrossPageMatrixCell,
  CrossPageMatrixDimension,
  CrossPageMatrixSummary,
  NormalizedFinding,
  NormalizedFindingSeverity,
  PageScore,
} from '../contracts/scorePanel';

const DIMENSIONS: CrossPageMatrixDimension[] = [
  'layout',
  'story',
  'accessibility',
  'consistency',
  'navigation',
  'actionability',
];

const IMPACT_DIMENSION_MAP: Record<NormalizedFinding['impactArea'], CrossPageMatrixDimension> = {
  layout: 'layout',
  density: 'layout',
  storytelling: 'story',
  governance: 'consistency',
  accessibility: 'accessibility',
  navigation: 'navigation',
  kpiEffectiveness: 'story',
  benchmark: 'story',
  actionability: 'actionability',
  metadata: 'consistency',
};

function severityRank(severity: NormalizedFindingSeverity): number {
  switch (severity) {
    case 'high':
      return 0;
    case 'medium':
      return 1;
    case 'low':
      return 2;
    default:
      return 3;
  }
}

function highestSeverity(findings: NormalizedFinding[]): NormalizedFindingSeverity | undefined {
  if (findings.length === 0) {
    return undefined;
  }

  return [...findings]
    .sort((left, right) => severityRank(left.severity) - severityRank(right.severity))[0]?.severity;
}

function averageConfidence(findings: NormalizedFinding[]): number | undefined {
  if (findings.length === 0) {
    return undefined;
  }

  const total = findings.reduce((sum, finding) => sum + finding.confidence, 0);
  return Math.round((total / findings.length) * 10) / 10;
}

function buildStatus(findings: NormalizedFinding[]): CrossPageMatrixCell['status'] {
  if (findings.length === 0) {
    return 'unknown';
  }

  const highSeverityCount = findings.filter((finding) => finding.severity === 'high').length;
  const mediumSeverityCount = findings.filter((finding) => finding.severity === 'medium').length;
  if (highSeverityCount > 0 || mediumSeverityCount > 1 || findings.some((finding) => finding.scope === 'crossPage')) {
    return 'weak';
  }

  if (mediumSeverityCount > 0 || findings.some((finding) => finding.severity === 'low')) {
    return 'watch';
  }

  return 'strong';
}

function buildSummary(pageName: string, dimension: CrossPageMatrixDimension, findings: NormalizedFinding[]): string {
  if (findings.length === 0) {
    return `No mapped ${dimension} findings were generated for ${pageName}.`;
  }

  return findings[0].summary;
}

function buildCell(
  pageName: string,
  dimension: CrossPageMatrixDimension,
  findings: NormalizedFinding[],
): CrossPageMatrixCell {
  const highSeverityCount = findings.filter((finding) => finding.severity === 'high').length;
  return {
    pageName,
    dimension,
    severity: highestSeverity(findings),
    findingCount: findings.length,
    highSeverityCount,
    confidenceAverage: averageConfidence(findings),
    status: buildStatus(findings),
    relatedFindingIds: findings.map((finding) => finding.id),
    summary: buildSummary(pageName, dimension, findings),
  };
}

export function buildCrossPageMatrix(
  findings: NormalizedFinding[] | undefined,
  pageScores: PageScore[] | undefined,
): CrossPageMatrixSummary | undefined {
  if (!findings || findings.length === 0 || !pageScores || pageScores.length < 2) {
    return undefined;
  }

  const pageNames = pageScores.map((page) => page.pageName);
  return {
    dimensions: DIMENSIONS,
    rows: pageNames.map((pageName) => ({
      pageName,
      cells: DIMENSIONS.map((dimension) => buildCell(
        pageName,
        dimension,
        findings.filter((finding) => (
          finding.affectedPages.includes(pageName) &&
          IMPACT_DIMENSION_MAP[finding.impactArea] === dimension
        )),
      )),
    })),
  };
}
