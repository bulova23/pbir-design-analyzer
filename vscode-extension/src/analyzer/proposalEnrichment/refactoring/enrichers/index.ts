import type {
  RefactoringDomain,
  RefactoringScenario,
} from '../../../contracts/scorePanel';
import type { RefactoringContext } from '../refactoringContextBuilder';
import { buildExecutiveExperienceRefactoringScenarios } from './executiveExperienceEnricher';
import { buildLayoutRefactoringScenarios } from './layoutRefactoringEnricher';
import { buildNavigationRefactoringScenarios } from './navigationRefactoringEnricher';
import { buildStorytellingRefactoringScenarios } from './storytellingRefactoringEnricher';

type Enricher = (context: RefactoringContext) => RefactoringScenario[];

const ENRICHERS: Partial<Record<RefactoringDomain, Enricher>> = {
  layout: buildLayoutRefactoringScenarios,
  storytelling: buildStorytellingRefactoringScenarios,
  navigation: buildNavigationRefactoringScenarios,
  executiveExperience: buildExecutiveExperienceRefactoringScenarios,
};

export function buildRefactoringEnricherScenarios(context: RefactoringContext): RefactoringScenario[] {
  return context.requestedDomains.flatMap((domain) => {
    const enricher = ENRICHERS[domain];
    return enricher ? enricher(context) : [];
  });
}
