import type {
  FixPlanItem,
  NormalizedFinding,
  OverviewSummary,
  ReviewPresentationPersona,
  ReviewPresentationPersonaProfile,
} from '../analyzer/contracts/scorePanel';
import {
  applyPersonaPresentation,
  getReviewPresentationPersonaProfiles,
} from '../analyzer/score/personaPresentation';

function buildFinding(overrides: Partial<NormalizedFinding>): NormalizedFinding {
  return {
    id: 'finding',
    title: 'Finding',
    summary: 'Finding summary',
    severity: 'medium',
    confidence: 70,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: ['Overview'],
    impactArea: 'layout',
    frameworkImpact: [],
    recommendation: 'Fix it.',
    sourceKind: 'frameworkFeedback',
    sourceSection: 'issues',
    evidence: [],
    ...overrides,
  };
}

function buildOverviewSummary(): OverviewSummary {
  return {
    overallScore: 77,
    maturityBand: 'Mature',
    riskBand: 'Elevated',
    benchmarkSummary: 'Benchmark summary',
    executiveSummary: 'Executive summary',
    severityDistribution: { high: 2, medium: 2, low: 0, info: 0 },
    topStrengths: [],
    topWeaknesses: [],
    topIssues: [],
    topActions: [],
    crossPageSummary: {
      headline: 'Cross-page summary',
      details: [],
      consistentPages: 1,
      totalPages: 2,
    },
  };
}

function buildFixPlanItem(overrides: Partial<FixPlanItem>): FixPlanItem {
  return {
    id: 'fix',
    title: 'Fix',
    detail: 'Fix detail',
    severity: 'medium',
    effort: 'medium',
    impact: 'medium',
    why: 'Why this action matters.',
    scope: 'page',
    affectedPages: ['Overview'],
    recommendedAction: 'Fix it.',
    resolvedOutcomes: ['Finding'],
    sourceFindingIds: ['finding'],
    ...overrides,
  };
}

describe('personaPresentation', () => {
  const findings: NormalizedFinding[] = [
    buildFinding({
      id: 'story-high',
      title: 'Story issue',
      severity: 'high',
      confidence: 80,
      impactArea: 'storytelling',
    }),
    buildFinding({
      id: 'actionability-high',
      title: 'Actionability issue',
      severity: 'high',
      confidence: 82,
      impactArea: 'actionability',
    }),
    buildFinding({
      id: 'governance-cross',
      title: 'Governance cross-page issue',
      severity: 'medium',
      confidence: 90,
      impactArea: 'governance',
      scope: 'crossPage',
      affectedPages: ['Overview', 'Details'],
    }),
    buildFinding({
      id: 'accessibility-medium',
      title: 'Accessibility issue',
      severity: 'medium',
      confidence: 84,
      impactArea: 'accessibility',
    }),
  ];

  const overviewSummary = buildOverviewSummary();
  const fixPlan = [
    buildFixPlanItem({
      id: 'fix-story',
      title: 'Story issue',
      severity: 'high',
      sourceFindingIds: ['story-high'],
    }),
    buildFixPlanItem({
      id: 'fix-actionability',
      title: 'Actionability issue',
      severity: 'high',
      sourceFindingIds: ['actionability-high'],
    }),
    buildFixPlanItem({
      id: 'fix-governance',
      title: 'Governance cross-page issue',
      severity: 'medium',
      scope: 'crossPage',
      effort: 'high',
      affectedPages: ['Overview', 'Details'],
      sourceFindingIds: ['governance-cross'],
    }),
  ];

  it('exposes the supported workspace personas', () => {
    expect(getReviewPresentationPersonaProfiles().map((profile: ReviewPresentationPersonaProfile) => profile.id)).toEqual([
      'default',
      'executive',
      'consultant',
      'governance',
      'accessibility',
    ]);
  });

  ([
    ['default', 'actionability-high'],
    ['executive', 'actionability-high'],
    ['consultant', 'governance-cross'],
    ['governance', 'governance-cross'],
    ['accessibility', 'accessibility-medium'],
  ] as Array<[ReviewPresentationPersona, string]>).forEach(([persona, expectedFirstId]) => {
    it(`prioritizes findings for ${persona} persona`, () => {
      const presented = applyPersonaPresentation({
        persona,
        findings,
        overviewSummary,
        fixPlan,
      });

      expect(presented.findings[0].id).toBe(expectedFirstId);
    });
  });

  it('reorders fix plan items and top issues without mutating source values', () => {
    const sourceFindings = findings.map((finding) => ({ ...finding }));
    const presented = applyPersonaPresentation({
      persona: 'governance',
      findings,
      overviewSummary,
      fixPlan,
    });

    expect(presented.fixPlan[0].sourceFindingIds).toContain('governance-cross');
    expect(presented.overviewSummary.topIssues[0]?.sourceFindingIds).toContain('governance-cross');
    expect(findings).toEqual(sourceFindings);
    expect(findings.find((finding) => finding.id === 'governance-cross')?.severity).toBe('medium');
    expect(findings.find((finding) => finding.id === 'governance-cross')?.confidence).toBe(90);
  });
});
