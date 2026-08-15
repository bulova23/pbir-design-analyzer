import type {
  ActionabilityBreakdown,
  BenchmarkComparisonSummary,
  FrameworkFeedbackItem,
  GuidedStoryImprovement,
  GuidedStoryImprovements,
  NormalizedFinding,
  NormalizedFindingDetectionType,
  NormalizedFindingImpactArea,
  NormalizedFindingSeverity,
  PageScore,
  ReportConsistencyFinding,
  ScoreResult,
} from '../contracts/scorePanel';
import { normalizeFrameworkId } from './presentation';
import { classifyRenderedReviewFinding } from '../renderedReview/reviewModel';

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

function compareText(left: string, right: string): number {
  if (left < right) {
    return -1;
  }

  if (left > right) {
    return 1;
  }

  return 0;
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
    return [...new Set(affectedPages)].sort(compareText);
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

function guidedPriorityToSeverity(priority: GuidedStoryImprovement['priority']): NormalizedFindingSeverity {
  switch (priority) {
    case 'high':
      return 'high';
    case 'medium':
      return 'medium';
    default:
      return 'low';
  }
}

function buildGuidedStoryImprovementFinding(
  pageName: string | undefined,
  improvement: GuidedStoryImprovement,
): NormalizedFinding {
  return {
    id: `${sanitizeIdPart(pageName ?? 'report')}-guided-story-${sanitizeIdPart(improvement.id)}`,
    title: improvement.title,
    summary: improvement.summary,
    severity: guidedPriorityToSeverity(improvement.priority),
    confidence: improvement.priority === 'high' ? 90 : 78,
    scope: pageName ? 'page' : 'report',
    detectionType: 'deterministic',
    affectedPages: pageName ? [pageName] : [],
    impactArea: improvement.relatedImpactArea,
    frameworkImpact: ['Story Assessment'],
    recommendation: improvement.rationale,
    sourceKind: 'guidedStoryImprovement',
    sourceSection: 'issues',
    evidence: [
      {
        kind: 'storyAssessment',
        label: 'Guided Story Improvements',
        pageName,
        detail: improvement.expectedImpact,
      },
    ],
  };
}

function pushGuidedStoryImprovementFindings(
  findings: NormalizedFinding[],
  pageName: string | undefined,
  guidedStoryImprovements: GuidedStoryImprovements | undefined,
): boolean {
  const improvements = guidedStoryImprovements
    ? [
        ...guidedStoryImprovements.highPriorityImprovements,
        ...guidedStoryImprovements.mediumPriorityImprovements,
      ]
    : [];

  for (const improvement of improvements) {
    findings.push(buildGuidedStoryImprovementFinding(pageName, improvement));
  }

  return improvements.length > 0;
}

function pushPageFindings(findings: NormalizedFinding[], page: Pick<PageScore, 'pageName' | 'feedback' | 'actionabilityBreakdown' | 'benchmarkComparison' | 'guidedStoryImprovements'>): void {
  for (const [frameworkKey, items] of Object.entries(page.feedback ?? {}).sort(([left], [right]) => compareText(left, right))) {
    for (const item of items) {
      const finding = buildFrameworkFinding(frameworkKey, item, page.pageName);
      if (finding) {
        findings.push(finding);
      }
    }
  }

  const hasGuidedStoryImprovements = pushGuidedStoryImprovementFindings(
    findings,
    page.pageName,
    page.guidedStoryImprovements,
  );

  if (!hasGuidedStoryImprovements && page.actionabilityBreakdown) {
    const actionabilityFinding = buildActionabilityFinding(page.pageName, page.actionabilityBreakdown);
    if (actionabilityFinding) {
      findings.push(actionabilityFinding);
    }
  }

  if (!hasGuidedStoryImprovements && page.benchmarkComparison) {
    const benchmarkFinding = buildBenchmarkFinding(page.pageName, page.benchmarkComparison);
    if (benchmarkFinding) {
      findings.push(benchmarkFinding);
    }
  }
}

function dedupeFindings(findings: NormalizedFinding[]): NormalizedFinding[] {
  const seen = new Set<string>();
  return findings.filter((finding) => {
    const key = `${finding.title}|${finding.summary}|${[...finding.affectedPages].sort(compareText).join('|')}`;
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

function compareEvidence(
  left: NormalizedFinding['evidence'][number],
  right: NormalizedFinding['evidence'][number],
): number {
  return compareText(
    `${left.kind}|${left.pageName ?? ''}|${left.frameworkKey ?? ''}|${left.visualId ?? ''}|${left.label}|${left.detail ?? ''}`,
    `${right.kind}|${right.pageName ?? ''}|${right.frameworkKey ?? ''}|${right.visualId ?? ''}|${right.label}|${right.detail ?? ''}`,
  );
}

function normalizeFinding(finding: NormalizedFinding): NormalizedFinding {
  const renderedReview = classifyRenderedReviewFinding(finding);
  const evidenceDomains = [...new Set(finding.evidence.map((evidence) => {
    if (evidence.kind === 'semanticModel') return 'semantic' as const;
    if (evidence.kind === 'screenshot' || evidence.kind === 'audit') return 'rendered' as const;
    return 'deterministic' as const;
  }))];
  return {
    ...finding,
    affectedPages: [...finding.affectedPages].sort(compareText),
    frameworkImpact: [...finding.frameworkImpact].sort(compareText),
    evidence: [...finding.evidence].sort(compareEvidence),
    reviewClassification: renderedReview.classification,
    renderedReviewCategory: renderedReview.category,
    evidenceDomains,
  };
}

export function compareNormalizedFindings(left: NormalizedFinding, right: NormalizedFinding): number {
  const severityDelta = severityRank(left.severity) - severityRank(right.severity);
  if (severityDelta !== 0) {
    return severityDelta;
  }

  const confidenceDelta = right.confidence - left.confidence;
  if (confidenceDelta !== 0) {
    return confidenceDelta;
  }

  return compareText(
    `${left.scope}|${left.impactArea}|${left.title}|${left.summary}|${left.id}`,
    `${right.scope}|${right.impactArea}|${right.title}|${right.summary}|${right.id}`,
  );
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
    for (const [frameworkKey, items] of Object.entries(result.feedback ?? {}).sort(([left], [right]) => compareText(left, right))) {
      for (const item of items) {
        const finding = buildFrameworkFinding(frameworkKey, item, result.scoredPageName);
        if (finding) {
          findings.push(finding);
        }
      }
    }

    const hasGuidedStoryImprovements = pushGuidedStoryImprovementFindings(
      findings,
      result.scoredPageName,
      result.guidedStoryImprovements,
    );

    if (!hasGuidedStoryImprovements && result.actionabilityBreakdown) {
      const actionabilityFinding = buildActionabilityFinding(result.scoredPageName, result.actionabilityBreakdown);
      if (actionabilityFinding) {
        findings.push(actionabilityFinding);
      }
    }

    if (!hasGuidedStoryImprovements && result.benchmarkComparison) {
      const benchmarkFinding = buildBenchmarkFinding(result.scoredPageName, result.benchmarkComparison);
      if (benchmarkFinding) {
        findings.push(benchmarkFinding);
      }
    }
  }

  return dedupeFindings(findings)
    .map(normalizeFinding)
    .sort(compareNormalizedFindings);
}
