import type {
  GuidedStoryImprovements,
  PagePurposeAnalysisSummary,
  StoryAssessmentMaturity,
} from '../contracts/scorePanel';

function hasNarrativeAnchor(guidedStoryImprovements: GuidedStoryImprovements | undefined): boolean {
  const improvements = guidedStoryImprovements
    ? [
        ...guidedStoryImprovements.highPriorityImprovements,
        ...guidedStoryImprovements.mediumPriorityImprovements,
      ]
    : [];

  return !improvements.some((item) => item.id === 'missing-title-question-anchor');
}

export function getStoryMaturityLabel(props: {
  analysis: PagePurposeAnalysisSummary;
  guidedStoryImprovements: GuidedStoryImprovements | undefined;
}): StoryAssessmentMaturity {
  const { analysis, guidedStoryImprovements } = props;
  const highPriorityCount = guidedStoryImprovements?.highPriorityImprovements.length ?? 0;
  const mediumPriorityCount = guidedStoryImprovements?.mediumPriorityImprovements.length ?? 0;
  const totalImprovementCount = highPriorityCount + mediumPriorityCount;
  const actionabilityScore = typeof analysis.actionabilityScore === 'number' ? analysis.actionabilityScore : 0;
  const narrativeAnchorPresent = hasNarrativeAnchor(guidedStoryImprovements);

  if (highPriorityCount === 0 && mediumPriorityCount === 0 && actionabilityScore >= 85) {
    return 'Mature';
  }

  if (highPriorityCount === 0 && mediumPriorityCount <= 1 && actionabilityScore >= 70) {
    return 'Strong';
  }

  if (actionabilityScore < 25) {
    return 'Draft';
  }

  if (!narrativeAnchorPresent && actionabilityScore < 45) {
    return 'Draft';
  }

  if (totalImprovementCount >= 5 && actionabilityScore < 35) {
    return 'Draft';
  }

  return 'Developing';
}
