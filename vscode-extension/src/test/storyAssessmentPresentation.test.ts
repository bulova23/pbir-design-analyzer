import { getStoryMaturityLabel } from '../analyzer/score/storyAssessmentPresentation';
import type { GuidedStoryImprovements, PagePurposeAnalysisSummary } from '../analyzer/contracts/scorePanel';

function buildAnalysis(actionabilityScore: number): PagePurposeAnalysisSummary {
  return {
    inferredPurpose: 'Summarize current performance.',
    whyThisMatters: 'Missing context increases interpretation risk.',
    actionabilityScore,
    topGaps: [],
  };
}

function buildImprovements(
  highPriorityCount: number,
  mediumPriorityCount: number,
): GuidedStoryImprovements {
  const improvement = {
    id: 'missing-benchmark-target',
    title: 'Add a benchmark or target',
    summary: 'The current result appears without a visible target.',
    rationale: 'Readers need a visible reference point.',
    expectedImpact: 'Clearer decision context.',
    priority: 'high' as const,
    relatedImpactArea: 'benchmark' as const,
  };

  return {
    highPriorityImprovements: Array.from({ length: highPriorityCount }, (_, index) => ({
      ...improvement,
      id: `${improvement.id}-${index}`,
    })),
    mediumPriorityImprovements: Array.from({ length: mediumPriorityCount }, (_, index) => ({
      ...improvement,
      id: `medium-${index}`,
      priority: 'medium' as const,
    })),
    storyImprovementRationale: '',
  };
}

describe('getStoryMaturityLabel', () => {
  it('classifies a recognizable but incomplete story as Developing instead of Draft', () => {
    expect(getStoryMaturityLabel({
      analysis: buildAnalysis(58),
      guidedStoryImprovements: buildImprovements(2, 1),
    })).toBe('Developing');
  });

  it('still classifies very sparse story evidence as Draft', () => {
    expect(getStoryMaturityLabel({
      analysis: buildAnalysis(18),
      guidedStoryImprovements: buildImprovements(3, 0),
    })).toBe('Draft');
  });

  it('preserves Strong and Mature outcomes for higher-quality pages', () => {
    expect(getStoryMaturityLabel({
      analysis: buildAnalysis(76),
      guidedStoryImprovements: buildImprovements(0, 1),
    })).toBe('Strong');

    expect(getStoryMaturityLabel({
      analysis: buildAnalysis(90),
      guidedStoryImprovements: buildImprovements(0, 0),
    })).toBe('Mature');
  });
});
