import type {
  BenchmarkComparisonSummary,
  CrossPageSummary,
  NormalizedFinding,
  NormalizedFindingSeverity,
  OverviewInsight,
  OverviewSummary,
  ScoreResult,
  SeverityDistribution,
} from '../contracts/scorePanel';

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

function buildSeverityDistribution(findings: NormalizedFinding[]): SeverityDistribution {
  return findings.reduce<SeverityDistribution>((distribution, finding) => {
    distribution[finding.severity] += 1;
    return distribution;
  }, {
    high: 0,
    medium: 0,
    low: 0,
    info: 0,
  });
}

function buildCrossPageSummary(result: ScoreResult): CrossPageSummary {
  const totalPages = result.pageCount || result.pageScores?.length || 0;
  const affectedPages = new Set(result.reportConsistencySummary?.affectedPages ?? []);
  const consistentPages = Math.max(totalPages - affectedPages.size, 0);
  const headline = totalPages > 0
    ? `${consistentPages} of ${totalPages} page${totalPages === 1 ? '' : 's'} show stronger consistency signals.`
    : 'Cross-page consistency data is limited.';

  return {
    headline,
    details: result.reportConsistencySummary?.findings ?? [],
    consistentPages,
    totalPages,
  };
}

function buildBenchmarkSummary(benchmark: BenchmarkComparisonSummary | undefined): string {
  if (!benchmark) {
    return 'Benchmark summary is not available for this report view.';
  }

  return `${benchmark.benchmarkLabel}: ${benchmark.insight}`;
}

function inferMaturityBand(score: number, distribution: SeverityDistribution): OverviewSummary['maturityBand'] {
  if (score >= 85 && distribution.high === 0) {
    return 'Advanced';
  }

  if (score >= 75 && distribution.high <= 1) {
    return 'Mature';
  }

  if (score >= 65) {
    return 'Developing';
  }

  return 'Emerging';
}

function inferRiskBand(distribution: SeverityDistribution): OverviewSummary['riskBand'] {
  if (distribution.high >= 3) {
    return 'High';
  }

  if (distribution.high >= 1 || distribution.medium >= 4) {
    return 'Elevated';
  }

  if (distribution.medium >= 1) {
    return 'Moderate';
  }

  return 'Low';
}

function toInsight(finding: NormalizedFinding): OverviewInsight {
  return {
    id: finding.id,
    title: finding.title,
    detail: finding.summary,
    affectedPages: finding.affectedPages,
    severity: finding.severity,
    sourceFindingIds: [finding.id],
  };
}

function buildTopStrengths(result: ScoreResult, distribution: SeverityDistribution): OverviewInsight[] {
  const strengths: OverviewInsight[] = [];

  if (result.compositeScore >= 75) {
    strengths.push({
      id: 'composite-score-strength',
      title: 'Overall design quality is above the working threshold',
      detail: `The current composite score is ${Math.round(result.compositeScore)} / 100.`,
      affectedPages: [],
      sourceFindingIds: [],
    });
  }

  if (result.benchmarkComparison?.strengths?.[0]) {
    strengths.push({
      id: 'benchmark-strength',
      title: result.benchmarkComparison.strengths[0],
      detail: result.benchmarkComparison.insight,
      affectedPages: [],
      sourceFindingIds: [],
    });
  }

  if (distribution.high === 0) {
    strengths.push({
      id: 'no-high-severity-strength',
      title: 'No high-severity issues were detected',
      detail: 'The current issue profile is weighted toward medium or lower-severity findings.',
      affectedPages: [],
      sourceFindingIds: [],
    });
  }

  return strengths.slice(0, 3);
}

export function buildOverviewSummary(result: ScoreResult): OverviewSummary {
  const findings = result.normalizedFindings ?? [];
  const sortedFindings = [...findings].sort((left, right) => {
    const severityDiff = severityRank(left.severity) - severityRank(right.severity);
    if (severityDiff !== 0) {
      return severityDiff;
    }

    return right.confidence - left.confidence;
  });
  const distribution = buildSeverityDistribution(findings);
  const topIssues = sortedFindings.slice(0, 3).map(toInsight);
  const topActions = sortedFindings.slice(0, 3).map((finding) => ({
    id: `action-${finding.id}`,
    title: finding.title,
    detail: finding.recommendation,
    severity: finding.severity,
    affectedPages: finding.affectedPages,
    sourceFindingIds: [finding.id],
  }));
  const topWeaknesses = sortedFindings.slice(0, 3).map(toInsight);
  const crossPageSummary = buildCrossPageSummary(result);
  const benchmarkSummary = buildBenchmarkSummary(result.benchmarkComparison);
  const maturityBand = inferMaturityBand(result.compositeScore, distribution);
  const riskBand = inferRiskBand(distribution);

  return {
    overallScore: result.compositeScore,
    maturityBand,
    riskBand,
    benchmarkSummary,
    executiveSummary: `This report is ${maturityBand.toLowerCase()} with ${riskBand.toLowerCase()} risk based on the current score and issue mix.`,
    severityDistribution: distribution,
    topStrengths: buildTopStrengths(result, distribution),
    topWeaknesses,
    topIssues,
    topActions,
    crossPageSummary,
  };
}
