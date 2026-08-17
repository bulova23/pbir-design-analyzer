import type {
  FixPlanImpact,
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

  if (finding.impactArea === 'navigation' || finding.impactArea === 'governance' || finding.impactArea === 'metadata') {
    return 'medium';
  }

  return 'low';
}

function impactRank(impact: FixPlanImpact): number {
  switch (impact) {
    case 'high':
      return 0;
    case 'medium':
      return 1;
    default:
      return 2;
  }
}

function effortRank(effort: FixPlanEffort): number {
  switch (effort) {
    case 'low':
      return 0;
    case 'medium':
      return 1;
    default:
      return 2;
  }
}

interface RemediationBlueprint {
  family: string;
  title: string;
  why: string;
}

function getGuidedStoryImprovementBlueprint(finding: NormalizedFinding): RemediationBlueprint | undefined {
  if (finding.sourceKind !== 'guidedStoryImprovement') {
    return undefined;
  }

  switch (finding.id.split('-guided-story-')[1]) {
    case 'missing-title-question-anchor':
      return {
        family: `guided-${finding.id}`,
        title: 'Add a clearer page question or title',
        why: 'Creates an explicit narrative entry point before readers interpret the visuals.',
      };
    case 'missing-benchmark-target':
      return {
        family: `guided-${finding.id}`,
        title: 'Add a benchmark or target',
        why: 'Gives the headline result an explicit comparison frame.',
      };
    case 'missing-primary-metric':
      return {
        family: `guided-${finding.id}`,
        title: 'Make the primary metric more explicit',
        why: 'Anchors the page around one clear headline measure.',
      };
    case 'missing-primary-dimension':
      return {
        family: `guided-${finding.id}`,
        title: 'Clarify the primary comparison dimension',
        why: 'Makes the comparison logic easier to read and explain.',
      };
    case 'missing-prior-period-context':
      return {
        family: `guided-${finding.id}`,
        title: 'Add prior-period context',
        why: 'Shows whether the current result is improving, declining, or holding steady.',
      };
    case 'scattered-filters':
      return {
        family: `guided-${finding.id}`,
        title: 'Consolidate scattered filters',
        why: 'Keeps controls from interrupting the main reading path.',
      };
    default:
      return {
        family: `guided-${finding.id}`,
        title: finding.title,
        why: 'Improves the page story through one focused narrative change.',
      };
  }
}

function getBlueprint(finding: NormalizedFinding): RemediationBlueprint {
  const guidedBlueprint = getGuidedStoryImprovementBlueprint(finding);
  if (guidedBlueprint) {
    return guidedBlueprint;
  }

  if (finding.sourceKind === 'fabricAppReview') {
    if (finding.impactArea === 'navigation') {
      return {
        family: 'fabric-navigation',
        title: 'Improve navigation clarity',
        why: 'Clarifies executive-to-detail evidence flow across the app.',
      };
    }

    if (finding.impactArea === 'storytelling') {
      return {
        family: 'fabric-storytelling',
        title: 'Improve dashboard hierarchy',
        why: 'Strengthens executive readability and clarifies the primary scan path.',
      };
    }

    return {
      family: 'fabric-tokens',
      title: 'Standardize token usage',
      why: 'Improves consistency across color, spacing, and typography decisions.',
    };
  }

  if (finding.sourceKind === 'fabricAppReadiness') {
    if (finding.impactArea === 'navigation') {
      return {
        family: 'migration-navigation',
        title: 'Simplify navigation for app portability',
        why: 'Reduces reliance on Power BI-specific navigation shells before migration.',
      };
    }

    if (finding.impactArea === 'metadata') {
      return {
        family: 'migration-semantic-labeling',
        title: 'Improve semantic labeling for app reuse',
        why: 'Makes semantic-model-backed app recreation more reliable.',
      };
    }

    if (finding.impactArea === 'storytelling') {
      return {
        family: 'migration-narrative',
        title: 'Improve narrative hierarchy for app migration',
        why: 'Clarifies which pages should become app landing, summary, or drill surfaces.',
      };
    }

    return {
      family: 'migration-portability',
      title: 'Reduce Power BI-only dependencies',
      why: 'Improves report portability before Fabric App migration.',
    };
  }

  switch (finding.impactArea) {
    case 'actionability':
    case 'benchmark':
      return {
        family: 'decision-context',
        title: 'Add benchmarks and decision context',
        why: 'Reduces risk of KPI misinterpretation.',
      };
    case 'storytelling':
    case 'kpiEffectiveness':
      return {
        family: 'story-clarity',
        title: 'Clarify page purpose and narrative framing',
        why: 'Improves page purpose clarity for executive readers.',
      };
    case 'layout':
    case 'density':
      return {
        family: 'layout-density',
        title: 'Reduce visual density and align layout',
        why: 'Improves scanability and reduces cognitive load.',
      };
    case 'navigation':
      return {
        family: 'navigation',
        title: 'Standardize navigation cues',
        why: 'Makes navigation more predictable across related pages.',
      };
    case 'governance':
    case 'metadata':
      return {
        family: 'standards',
        title: 'Normalize cross-page standards',
        why: 'Improves consistency across repeated report workflows.',
      };
    case 'accessibility':
      return {
        family: 'accessibility',
        title: 'Improve accessibility and legibility',
        why: 'Makes the page easier to interpret for a wider range of readers.',
      };
    default:
      return {
        family: 'general',
        title: finding.title,
        why: 'Clarifies a high-priority review issue.',
      };
  }
}

function getResolvedOutcome(finding: NormalizedFinding): string {
  if (finding.sourceKind === 'guidedStoryImprovement') {
    switch (finding.id.split('-guided-story-')[1]) {
      case 'missing-title-question-anchor':
        return 'Story anchor';
      case 'missing-benchmark-target':
        return 'Benchmark context';
      case 'missing-primary-metric':
        return 'Primary metric clarity';
      case 'missing-primary-dimension':
        return 'Primary dimension clarity';
      case 'missing-prior-period-context':
        return 'Prior-period context';
      case 'scattered-filters':
        return 'Filter flow';
      default:
        return finding.title;
    }
  }

  if (finding.sourceKind === 'fabricAppReview') {
    switch (finding.impactArea) {
      case 'navigation':
        return 'Navigation clarity';
      case 'storytelling':
        return 'Executive readability';
      default:
        return 'Token consistency';
    }
  }

  if (finding.sourceKind === 'fabricAppReadiness') {
    switch (finding.impactArea) {
      case 'navigation':
        return 'Navigation portability';
      case 'metadata':
        return 'Semantic-model portability';
      case 'storytelling':
        return 'Narrative portability';
      case 'accessibility':
        return 'Accessibility portability';
      default:
        return 'Migration portability';
    }
  }

  switch (finding.impactArea) {
    case 'benchmark':
      return 'Benchmark gap';
    case 'actionability':
      return 'Actionability gap';
    case 'storytelling':
      return 'Story clarity';
    case 'kpiEffectiveness':
      return 'KPI clarity';
    case 'layout':
      return 'Layout consistency';
    case 'density':
      return 'Readability';
    case 'navigation':
      return 'Navigation consistency';
    case 'governance':
      return 'Governance consistency';
    case 'metadata':
      return 'Metadata consistency';
    case 'accessibility':
      return 'Accessibility support';
    default:
      return finding.title;
  }
}

function buildGroupKey(finding: NormalizedFinding): string {
  const blueprint = getBlueprint(finding);
  const pageKey = finding.scope === 'crossPage' || finding.scope === 'report'
    ? 'report'
    : finding.affectedPages.slice().sort().join('|');
  return `${blueprint.family}:${pageKey}`;
}

function getGuidedStoryImprovementSequenceRank(item: FixPlanItem): number {
  const findingId = item.sourceFindingIds[0]?.split('-guided-story-')[1];
  switch (findingId) {
    case 'missing-title-question-anchor':
      return 0;
    case 'missing-benchmark-target':
      return 1;
    case 'missing-primary-metric':
      return 2;
    case 'missing-primary-dimension':
      return 3;
    case 'missing-prior-period-context':
      return 4;
    case 'scattered-filters':
      return 5;
    default:
      return 99;
  }
}

function inferImpact(findings: NormalizedFinding[]): FixPlanImpact {
  const highestSeverity = [...findings].sort((left, right) => severityRank(left.severity) - severityRank(right.severity))[0]?.severity;
  if (highestSeverity === 'high') {
    return 'high';
  }

  if (highestSeverity === 'medium' || findings.some((finding) => finding.scope === 'crossPage' || finding.scope === 'report')) {
    return 'medium';
  }

  return 'low';
}

export function buildFixPlan(findings: NormalizedFinding[] | undefined): FixPlanItem[] {
  if (!findings || findings.length === 0) {
    return [];
  }

  const grouped = findings
    .filter((finding) => finding.sourceSection === 'issues' && !finding.evidence.some((evidence) => evidence.kind === 'customVisual'))
    .reduce<Map<string, NormalizedFinding[]>>((lookup, finding) => {
      const key = buildGroupKey(finding);
      const existing = lookup.get(key) ?? [];
      existing.push(finding);
      lookup.set(key, existing);
      return lookup;
    }, new Map<string, NormalizedFinding[]>());

  return [...grouped.entries()]
    .map(([key, groupFindings]) => {
      const seed = [...groupFindings].sort((left, right) => {
        const severityDiff = severityRank(left.severity) - severityRank(right.severity);
        if (severityDiff !== 0) {
          return severityDiff;
        }

        return right.confidence - left.confidence;
      })[0];
      const blueprint = getBlueprint(seed);
      const impact = inferImpact(groupFindings);
      const effort = groupFindings.some((finding) => inferEffort(finding) === 'high')
        ? 'high'
        : groupFindings.some((finding) => inferEffort(finding) === 'medium')
          ? 'medium'
          : 'low';
      const sourceFindingIds = [...new Set(groupFindings.map((finding) => finding.id))];
      const resolvedOutcomes = [...new Set(groupFindings.map((finding) => getResolvedOutcome(finding)))];
      const recommendedActions = [...new Set(groupFindings.map((finding) => finding.recommendation.trim()).filter(Boolean))];
      const detail = groupFindings.length > 1
        ? `Resolve ${groupFindings.length} related findings through one remediation step.`
        : seed.summary;

      return {
        id: `fix-${key}`,
        title: blueprint.title,
        detail,
        severity: seed.severity,
        effort,
        impact,
        why: blueprint.why,
        scope: seed.scope,
        affectedPages: [...new Set(groupFindings.flatMap((finding) => finding.affectedPages))],
        recommendedAction: recommendedActions.join(' '),
        resolvedOutcomes,
        sourceFindingIds,
      } satisfies FixPlanItem;
    })
    .sort((left, right) => {
      const leftIsGuided = left.sourceFindingIds.some((id) => id.includes('-guided-story-'));
      const rightIsGuided = right.sourceFindingIds.some((id) => id.includes('-guided-story-'));
      if (leftIsGuided && rightIsGuided) {
        return getGuidedStoryImprovementSequenceRank(left) - getGuidedStoryImprovementSequenceRank(right);
      }

      const severityDiff = severityRank(left.severity) - severityRank(right.severity);
      if (severityDiff !== 0) {
        return severityDiff;
      }

      const impactDiff = impactRank(left.impact) - impactRank(right.impact);
      if (impactDiff !== 0) {
        return impactDiff;
      }

      const effortDiff = effortRank(left.effort) - effortRank(right.effort);
      if (effortDiff !== 0) {
        return effortDiff;
      }

      return left.title.localeCompare(right.title);
    })
    .slice(0, 8);
}
