import type {
  FixPlanItem,
  NormalizedFinding,
  NormalizedFindingDetectionType,
  NormalizedFindingImpactArea,
  NormalizedFindingScope,
  NormalizedFindingSeverity,
  OverviewAction,
  OverviewInsight,
  OverviewSummary,
  ReviewPresentationPersona,
  ReviewPresentationPersonaProfile,
} from '../contracts/scorePanel';

interface PersonaPresentationOptions {
  persona: ReviewPresentationPersona;
  findings: NormalizedFinding[];
  overviewSummary: OverviewSummary;
  fixPlan: FixPlanItem[];
}

export interface PersonaPresentationResult {
  findings: NormalizedFinding[];
  overviewSummary: OverviewSummary;
  fixPlan: FixPlanItem[];
  recommendedFilters: {
    severity: NormalizedFindingSeverity[];
    impactAreas: NormalizedFindingImpactArea[];
    scopes: NormalizedFindingScope[];
    detectionTypes: NormalizedFindingDetectionType[];
  };
}

const PERSONA_PROFILES: ReviewPresentationPersonaProfile[] = [
  {
    id: 'default',
    label: 'Default',
    description: 'Balanced prioritization across severity, confidence, and scope.',
    emphasizedImpactAreas: [],
    emphasizedScopes: [],
    defaultSeverityFilter: ['high', 'medium', 'low', 'info'],
    overviewEmphasis: ['issues', 'actions', 'weaknesses', 'benchmark', 'consistency'],
    fixPlanEmphasis: ['severity', 'scope'],
  },
  {
    id: 'executive',
    label: 'Executive',
    description: 'Emphasize decision support, KPI clarity, and narrative issues first.',
    emphasizedImpactAreas: ['actionability', 'kpiEffectiveness', 'storytelling', 'benchmark'],
    emphasizedScopes: ['crossPage', 'page'],
    defaultSeverityFilter: ['high', 'medium'],
    overviewEmphasis: ['issues', 'actions', 'benchmark', 'consistency'],
    fixPlanEmphasis: ['severity', 'scope', 'crossPage'],
  },
  {
    id: 'consultant',
    label: 'Consultant',
    description: 'Prioritize fix sequencing, remediation clarity, and evidence-backed issues.',
    emphasizedImpactAreas: ['actionability', 'storytelling', 'governance', 'navigation'],
    emphasizedScopes: ['crossPage', 'page'],
    defaultSeverityFilter: ['high', 'medium'],
    overviewEmphasis: ['issues', 'actions', 'weaknesses'],
    fixPlanEmphasis: ['severity', 'effort', 'evidence', 'scope', 'crossPage'],
  },
  {
    id: 'governance',
    label: 'Governance',
    description: 'Emphasize cross-page consistency, standards, and semantic drift.',
    emphasizedImpactAreas: ['governance', 'metadata', 'navigation', 'layout'],
    emphasizedScopes: ['crossPage', 'report'],
    defaultSeverityFilter: ['high', 'medium', 'low'],
    overviewEmphasis: ['issues', 'consistency', 'actions'],
    fixPlanEmphasis: ['crossPage', 'scope', 'severity'],
  },
  {
    id: 'accessibility',
    label: 'Accessibility',
    description: 'Emphasize accessibility, readability, and navigation usability.',
    emphasizedImpactAreas: ['accessibility', 'navigation', 'density'],
    emphasizedScopes: ['page', 'crossPage'],
    defaultSeverityFilter: ['high', 'medium', 'low'],
    overviewEmphasis: ['issues', 'actions', 'weaknesses'],
    fixPlanEmphasis: ['severity', 'scope', 'evidence'],
  },
];

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

function scopeRank(scope: NormalizedFindingScope): number {
  switch (scope) {
    case 'crossPage':
      return 0;
    case 'report':
      return 1;
    case 'page':
      return 2;
    default:
      return 3;
  }
}

function personaBonus(persona: ReviewPresentationPersona, finding: NormalizedFinding): number {
  switch (persona) {
    case 'executive':
      return (
        (finding.impactArea === 'actionability' ? 40 : 0) +
        (finding.impactArea === 'kpiEffectiveness' ? 30 : 0) +
        (finding.impactArea === 'storytelling' ? 28 : 0) +
        (finding.impactArea === 'benchmark' ? 24 : 0) +
        (finding.scope === 'crossPage' ? 14 : 0)
      );
    case 'consultant':
      return (
        (finding.scope === 'crossPage' ? 40 : 0) +
        (finding.affectedPages.length > 1 ? 16 : 0) +
        (finding.evidence.length > 0 ? 14 : 0) +
        (finding.recommendation.trim().length > 0 ? 12 : 0) +
        (finding.impactArea === 'navigation' || finding.impactArea === 'governance' ? 8 : 0)
      );
    case 'governance':
      return (
        (finding.scope === 'crossPage' ? 60 : 0) +
        (finding.scope === 'report' ? 45 : 0) +
        (finding.impactArea === 'governance' ? 34 : 0) +
        (finding.impactArea === 'metadata' ? 26 : 0) +
        (finding.impactArea === 'navigation' ? 22 : 0) +
        (finding.impactArea === 'layout' ? 18 : 0)
      );
    case 'accessibility':
      return (
        (finding.impactArea === 'accessibility' ? 60 : 0) +
        (finding.impactArea === 'navigation' ? 20 : 0) +
        (finding.impactArea === 'density' ? 18 : 0)
      );
    default:
      return 0;
  }
}

function compareFindings(
  persona: ReviewPresentationPersona,
  left: NormalizedFinding,
  right: NormalizedFinding,
): number {
  if (persona !== 'default') {
    const leftBonus = personaBonus(persona, left);
    const rightBonus = personaBonus(persona, right);
    if (leftBonus !== rightBonus) {
      return rightBonus - leftBonus;
    }
  }

  const severityDiff = severityRank(left.severity) - severityRank(right.severity);
  if (severityDiff !== 0) {
    return severityDiff;
  }

  if (persona === 'default') {
    const leftBonus = personaBonus(persona, left);
    const rightBonus = personaBonus(persona, right);
    if (leftBonus !== rightBonus) {
      return rightBonus - leftBonus;
    }
  }

  const confidenceDiff = right.confidence - left.confidence;
  if (confidenceDiff !== 0) {
    return confidenceDiff;
  }

  const scopeDiff = scopeRank(left.scope) - scopeRank(right.scope);
  if (scopeDiff !== 0) {
    return scopeDiff;
  }

  return left.id.localeCompare(right.id);
}

function reorderInsights(
  insights: OverviewInsight[],
  sortedFindings: NormalizedFinding[],
): OverviewInsight[] {
  if (insights.length === 0) {
    return insights;
  }

  const scoreByFindingId = new Map(sortedFindings.map((finding, index) => [finding.id, index]));
  return [...insights].sort((left, right) => {
    const leftScore = Math.min(...left.sourceFindingIds.map((id) => scoreByFindingId.get(id) ?? Number.MAX_SAFE_INTEGER));
    const rightScore = Math.min(...right.sourceFindingIds.map((id) => scoreByFindingId.get(id) ?? Number.MAX_SAFE_INTEGER));
    return leftScore - rightScore;
  });
}

function reorderActions(
  actions: OverviewAction[],
  sortedFindings: NormalizedFinding[],
): OverviewAction[] {
  if (actions.length === 0) {
    return actions;
  }

  const scoreByFindingId = new Map(sortedFindings.map((finding, index) => [finding.id, index]));
  return [...actions].sort((left, right) => {
    const leftScore = Math.min(...left.sourceFindingIds.map((id) => scoreByFindingId.get(id) ?? Number.MAX_SAFE_INTEGER));
    const rightScore = Math.min(...right.sourceFindingIds.map((id) => scoreByFindingId.get(id) ?? Number.MAX_SAFE_INTEGER));
    return leftScore - rightScore;
  });
}

function buildTopIssuesFromFindings(sortedFindings: NormalizedFinding[]): OverviewInsight[] {
  return sortedFindings.slice(0, 3).map((finding) => ({
    id: `persona-issue-${finding.id}`,
    title: finding.title,
    detail: finding.summary,
    affectedPages: finding.affectedPages,
    severity: finding.severity,
    sourceFindingIds: [finding.id],
  }));
}

function buildTopActionsFromFindings(sortedFindings: NormalizedFinding[]): OverviewAction[] {
  return sortedFindings.slice(0, 3).map((finding) => ({
    id: `persona-action-${finding.id}`,
    title: finding.title,
    detail: finding.recommendation,
    severity: finding.severity,
    affectedPages: finding.affectedPages,
    sourceFindingIds: [finding.id],
  }));
}

function reorderFixPlan(
  fixPlan: FixPlanItem[],
  sortedFindings: NormalizedFinding[],
): FixPlanItem[] {
  const scoreByFindingId = new Map(sortedFindings.map((finding, index) => [finding.id, index]));
  return [...fixPlan].sort((left, right) => {
    const leftScore = Math.min(...left.sourceFindingIds.map((id) => scoreByFindingId.get(id) ?? Number.MAX_SAFE_INTEGER));
    const rightScore = Math.min(...right.sourceFindingIds.map((id) => scoreByFindingId.get(id) ?? Number.MAX_SAFE_INTEGER));
    return leftScore - rightScore;
  });
}

export function getReviewPresentationPersonaProfiles(): ReviewPresentationPersonaProfile[] {
  return PERSONA_PROFILES.map((profile) => ({
    ...profile,
    emphasizedImpactAreas: [...profile.emphasizedImpactAreas],
    emphasizedScopes: [...profile.emphasizedScopes],
    defaultSeverityFilter: profile.defaultSeverityFilter ? [...profile.defaultSeverityFilter] : undefined,
    defaultDetectionTypes: profile.defaultDetectionTypes ? [...profile.defaultDetectionTypes] : undefined,
    overviewEmphasis: [...profile.overviewEmphasis],
    fixPlanEmphasis: [...profile.fixPlanEmphasis],
  }));
}

export function applyPersonaPresentation(
  options: PersonaPresentationOptions,
): PersonaPresentationResult {
  const profile = getReviewPresentationPersonaProfiles().find((item) => item.id === options.persona)
    ?? getReviewPresentationPersonaProfiles()[0];
  const findings = [...options.findings].sort((left, right) => compareFindings(options.persona, left, right));
  const reorderedOverview = {
    ...options.overviewSummary,
    topWeaknesses: reorderInsights(options.overviewSummary.topWeaknesses, findings),
    topIssues: buildTopIssuesFromFindings(findings),
    topActions: buildTopActionsFromFindings(findings),
  };

  return {
    findings,
    overviewSummary: {
      ...reorderedOverview,
      topIssues: reorderedOverview.topIssues.length > 0
        ? reorderedOverview.topIssues
        : reorderInsights(options.overviewSummary.topIssues, findings),
      topActions: reorderedOverview.topActions.length > 0
        ? reorderedOverview.topActions
        : reorderActions(options.overviewSummary.topActions, findings),
    },
    fixPlan: reorderFixPlan(options.fixPlan, findings),
    recommendedFilters: {
      severity: profile.defaultSeverityFilter ?? ['high', 'medium', 'low', 'info'],
      impactAreas: [...profile.emphasizedImpactAreas],
      scopes: [...profile.emphasizedScopes],
      detectionTypes: profile.defaultDetectionTypes ?? [],
    },
  };
}
