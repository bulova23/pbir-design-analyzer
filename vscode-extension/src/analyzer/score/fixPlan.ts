import type {
  FixPlanEffort,
  FixPlanItem,
  NormalizedFinding,
  NormalizedFindingSeverity,
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

function inferEffort(finding: NormalizedFinding): FixPlanEffort {
  if (finding.scope === 'crossPage' || finding.affectedPages.length > 1) {
    return 'high';
  }

  if (finding.scope === 'page' || finding.impactArea === 'actionability' || finding.impactArea === 'navigation') {
    return 'medium';
  }

  return 'low';
}

export function buildFixPlan(findings: NormalizedFinding[] | undefined): FixPlanItem[] {
  if (!findings || findings.length === 0) {
    return [];
  }

  return findings
    .filter((finding) => finding.sourceSection === 'issues')
    .sort((left, right) => {
      const severityDiff = severityRank(left.severity) - severityRank(right.severity);
      if (severityDiff !== 0) {
        return severityDiff;
      }

      return right.confidence - left.confidence;
    })
    .slice(0, 8)
    .map((finding) => ({
      id: `fix-${finding.id}`,
      title: finding.title,
      detail: finding.summary,
      severity: finding.severity,
      effort: inferEffort(finding),
      scope: finding.scope,
      affectedPages: finding.affectedPages,
      recommendedAction: finding.recommendation,
      sourceFindingIds: [finding.id],
    }));
}
