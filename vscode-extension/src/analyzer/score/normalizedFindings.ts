import type {
  ActionabilityBreakdown,
  BenchmarkComparisonSummary,
  FrameworkFeedbackItem,
  NormalizedFinding,
  NormalizedFindingDetectionType,
  NormalizedFindingImpactArea,
  NormalizedFindingSeverity,
  PageScore,
  ReportConsistencyFinding,
  ScoreResult,
} from '../contracts/scorePanel';
import { normalizeFrameworkId } from './presentation';

const FRAMEWORK_LABELS: Record<string, string> = {
  gestalt: 'Gestalt Principles',
  cognitiveLoad: 'Cognitive Load',
  dataInk: 'Data-Ink Ratio',
  graphicalPerception: 'Graphical Perception',
  accessibility: 'Accessibility',
  visualBestPractices: 'Visual Best Practices',
  governance: 'Enterprise Governance',
  stephenFew: 'Stephen Few',
  tufte: 'Tufte Minimalism',
  density: 'Dashboard Density',
  narrative: 'Narrative Design',
};

function confidenceToScore(confidence: 'high' | 'medium' | 'low'): number {
  switch (confidence) {
    case 'high':
      return 92;
    case 'medium':
      return 74;
    default:
      return 58;
  }
}

function normalizeWhitespace(value: string): string {
  return value.replace(/\s+/g, ' ').trim();
}

function sanitizeIdPart(value: string): string {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
}

function splitFeedbackText(text: string): {
  title: string;
  summary: string;
  recommendation?: string;
} {
  const separatorIndex = text.indexOf(':');
  const title = separatorIndex > 0 ? text.slice(0, separatorIndex).trim() : text.trim();
  const remainder = separatorIndex > 0 ? text.slice(separatorIndex + 1).trim() : text.trim();
  const divider = remainder.includes(' — ')
    ? ' — '
    : remainder.includes(' – ')
      ? ' – '
      : undefined;

  if (!divider) {
    return {
      title,
      summary: remainder,
    };
  }

  const [summary, recommendation] = remainder.split(divider, 2);
  return {
    title,
    summary: summary.trim(),
    recommendation: recommendation?.trim(),
  };
}

function frameworkImpactArea(frameworkKey: string): NormalizedFindingImpactArea {
  switch (normalizeFrameworkId(frameworkKey)) {
    case 'accessibility':
      return 'accessibility';
    case 'governance':
      return 'governance';
    case 'cognitiveLoad':
    case 'density':
    case 'dataInk':
      return 'density';
    case 'narrative':
      return 'storytelling';
    case 'graphicalPerception':
    case 'stephenFew':
      return 'kpiEffectiveness';
    default:
      return 'layout';
  }
}

function frameworkLabel(frameworkKey: string): string {
  const normalizedKey = normalizeFrameworkId(frameworkKey);
  return FRAMEWORK_LABELS[normalizedKey] ?? normalizedKey;
}

function inferSeverityFromFeedback(item: FrameworkFeedbackItem): NormalizedFindingSeverity {
  if (
    typeof item.earnedPoints === 'number' &&
    typeof item.possiblePoints === 'number' &&
    item.possiblePoints > 0
  ) {
    const percent = (item.earnedPoints / item.possiblePoints) * 100;
    if (percent < 45) {
      return 'high';
    }

    if (percent < 70) {
      return 'medium';
    }

    return 'low';
  }

  return item.findingType === 'objective' ? 'high' : 'medium';
}

function inferConfidenceFromFeedback(item: FrameworkFeedbackItem): number {
  switch (item.findingType) {
    case 'objective':
      return 94;
    case 'strongHeuristic':
      return 82;
    default:
      return 68;
  }
}

function inferDetectionType(item: FrameworkFeedbackItem): NormalizedFindingDetectionType {
  return item.findingType === 'stylePreference' ? 'mixed' : 'deterministic';
}

function inferFindingScope(pageName: string | undefined, item: FrameworkFeedbackItem): NormalizedFinding['scope'] {
  if (item.affectedVisuals && item.affectedVisuals.length > 0) {
    return 'visual';
  }

  return pageName ? 'page' : 'report';
}

function inferAffectedPages(pageName: string | undefined, item: FrameworkFeedbackItem): string[] {
  const affectedPages = item.affectedVisuals?.map((visual) => visual.pageName) ?? [];
  if (affectedPages.length > 0) {
    return [...new Set(affectedPages)];
  }

  return pageName ? [pageName] : [];
}

function buildReportConsistencyFinding(issue: ReportConsistencyFinding): NormalizedFinding {
  return {
    id: `report-consistency-${sanitizeIdPart(issue.issueCategory)}-${sanitizeIdPart(issue.affectedPages.join('-') || issue.category)}`,
    title: issue.category,
    summary: issue.overallFinding,
    severity: issue.severity,
    confidence: confidenceToScore(issue.confidence),
    scope: 'crossPage',
    detectionType: 'deterministic',
    affectedPages: issue.affectedPages,
    impactArea: issue.category === 'navigation'
      ? 'navigation'
      : issue.category === 'semanticColors' || issue.category === 'metricGovernance'
        ? 'governance'
        : 'layout',
    frameworkImpact: ['Enterprise Governance'],
    recommendation: issue.recommendedRemediation,
    sourceKind: 'reportConsistency',
    sourceSection: 'issues',
    evidence: [
      {
        kind: 'consistency',
        label: issue.issueCategory,
        detail: issue.overallFinding,
      },
    ],
  };
}

function buildFrameworkFinding(
  frameworkKey: string,
  item: FrameworkFeedbackItem,
  pageName?: string,
): NormalizedFinding | undefined {
  if (item.ok) {
    return undefined;
  }

  const details = splitFeedbackText(item.text);
  const frameworkName = frameworkLabel(frameworkKey);
  const affectedPages = inferAffectedPages(pageName, item);
  const summary = normalizeWhitespace(details.summary || item.text);

  return {
    id: `${sanitizeIdPart(pageName ?? 'report')}-${sanitizeIdPart(frameworkKey)}-${sanitizeIdPart(details.title)}-${sanitizeIdPart(summary).slice(0, 40)}`,
    title: details.title,
    summary,
    severity: inferSeverityFromFeedback(item),
    confidence: inferConfidenceFromFeedback(item),
    scope: inferFindingScope(pageName, item),
    detectionType: inferDetectionType(item),
    affectedPages,
    impactArea: frameworkImpactArea(frameworkKey),
    frameworkImpact: [frameworkName],
    recommendation: details.recommendation ?? `Address the ${details.title.toLowerCase()} issue in ${frameworkName.toLowerCase()}.`,
    sourceKind: 'frameworkFeedback',
    sourceSection: 'issues',
    evidence: [
      {
        kind: 'framework',
        label: frameworkName,
        pageName,
        frameworkKey: normalizeFrameworkId(frameworkKey),
        detail: item.text,
      },
      ...(item.affectedVisuals ?? []).map((visual) => ({
        kind: 'framework' as const,
        label: `${visual.visualType} ${visual.visualId}`,
        pageName: visual.pageName,
        visualId: visual.visualId,
      })),
    ],
  };
}

function buildActionabilityFinding(
  pageName: string | undefined,
  breakdown: ActionabilityBreakdown,
): NormalizedFinding | undefined {
  if (breakdown.gaps.length === 0 && breakdown.score >= 70) {
    return undefined;
  }

  const severity: NormalizedFindingSeverity = breakdown.expectationLevel === 'high' && breakdown.score < 60
    ? 'high'
    : breakdown.score < 75
      ? 'medium'
      : 'low';

  return {
    id: `${sanitizeIdPart(pageName ?? 'report')}-actionability`,
    title: 'Actionability gap',
    summary: breakdown.summary,
    severity,
    confidence: 88,
    scope: pageName ? 'page' : 'report',
    detectionType: 'deterministic',
    affectedPages: pageName ? [pageName] : [],
    impactArea: 'actionability',
    frameworkImpact: ['Narrative Design', 'Stephen Few'],
    recommendation: breakdown.gaps[0] ?? 'Strengthen the decision path on the page.',
    sourceKind: 'actionability',
    sourceSection: 'issues',
    evidence: [
      {
        kind: 'actionability',
        label: 'Actionability score',
        pageName,
        detail: `${breakdown.score}/100 · ${breakdown.summary}`,
      },
    ],
  };
}

function buildBenchmarkFinding(
  pageName: string | undefined,
  benchmark: BenchmarkComparisonSummary,
): NormalizedFinding | undefined {
  if (benchmark.comparativePosition === 'above' && benchmark.gaps.length === 0 && !benchmark.beautifulButUseless) {
    return undefined;
  }

  return {
    id: `${sanitizeIdPart(pageName ?? 'report')}-benchmark`,
    title: benchmark.beautifulButUseless ? 'Beautiful but weakly actionable' : 'Benchmark gap',
    summary: benchmark.insight,
    severity: benchmark.comparativePosition === 'below' || benchmark.beautifulButUseless ? 'high' : 'medium',
    confidence: 84,
    scope: pageName ? 'page' : 'report',
    detectionType: 'deterministic',
    affectedPages: pageName ? [pageName] : [],
    impactArea: 'benchmark',
    frameworkImpact: ['Narrative Design', 'Visual Best Practices'],
    recommendation: benchmark.gaps[0] ?? `Bring the page closer to the ${benchmark.benchmarkLabel}.`,
    sourceKind: 'benchmark',
    sourceSection: 'issues',
    evidence: [
      {
        kind: 'benchmark',
        label: benchmark.archetype,
        pageName,
        detail: benchmark.insight,
      },
    ],
  };
}

function pushPageFindings(findings: NormalizedFinding[], page: Pick<PageScore, 'pageName' | 'feedback' | 'actionabilityBreakdown' | 'benchmarkComparison'>): void {
  for (const [frameworkKey, items] of Object.entries(page.feedback ?? {})) {
    for (const item of items) {
      const finding = buildFrameworkFinding(frameworkKey, item, page.pageName);
      if (finding) {
        findings.push(finding);
      }
    }
  }

  if (page.actionabilityBreakdown) {
    const actionabilityFinding = buildActionabilityFinding(page.pageName, page.actionabilityBreakdown);
    if (actionabilityFinding) {
      findings.push(actionabilityFinding);
    }
  }

  if (page.benchmarkComparison) {
    const benchmarkFinding = buildBenchmarkFinding(page.pageName, page.benchmarkComparison);
    if (benchmarkFinding) {
      findings.push(benchmarkFinding);
    }
  }
}

function dedupeFindings(findings: NormalizedFinding[]): NormalizedFinding[] {
  const seen = new Set<string>();
  return findings.filter((finding) => {
    const key = `${finding.title}|${finding.summary}|${finding.affectedPages.join('|')}`;
    if (seen.has(key)) {
      return false;
    }

    seen.add(key);
    return true;
  });
}

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

export function buildNormalizedFindings(result: ScoreResult): NormalizedFinding[] {
  const findings: NormalizedFinding[] = [];

  for (const issue of result.reportConsistencySummary?.issues ?? []) {
    findings.push(buildReportConsistencyFinding(issue));
  }

  if (result.pageScores && result.pageScores.length > 0) {
    for (const page of result.pageScores) {
      pushPageFindings(findings, page);
    }
  } else {
    for (const [frameworkKey, items] of Object.entries(result.feedback ?? {})) {
      for (const item of items) {
        const finding = buildFrameworkFinding(frameworkKey, item, result.scoredPageName);
        if (finding) {
          findings.push(finding);
        }
      }
    }

    if (result.actionabilityBreakdown) {
      const actionabilityFinding = buildActionabilityFinding(result.scoredPageName, result.actionabilityBreakdown);
      if (actionabilityFinding) {
        findings.push(actionabilityFinding);
      }
    }

    if (result.benchmarkComparison) {
      const benchmarkFinding = buildBenchmarkFinding(result.scoredPageName, result.benchmarkComparison);
      if (benchmarkFinding) {
        findings.push(benchmarkFinding);
      }
    }
  }

  return dedupeFindings(findings).sort((left, right) => {
    const severityDelta = severityRank(left.severity) - severityRank(right.severity);
    if (severityDelta !== 0) {
      return severityDelta;
    }

    return right.confidence - left.confidence;
  });
}
