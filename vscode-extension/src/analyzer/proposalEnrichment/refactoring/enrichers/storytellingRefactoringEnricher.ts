import type {
  RefactoringEvidenceLink,
  RefactoringScenario,
} from '../../../contracts/scorePanel';
import { classifyRefactoringScenario } from '../refactoringCompilationClassifier';
import type {
  RefactoringContext,
  RefactoringContextFindingSummary,
  RefactoringContextPageSummary,
} from '../refactoringContextBuilder';

function storyFinding(context: RefactoringContext): RefactoringContextFindingSummary | undefined {
  return context.findings.find((finding) =>
    finding.impactArea === 'storytelling'
    || /\bstory\b|\bnarrative\b|\bheadline\b|\bevidence\b|\bsequence\b/i.test(`${finding.summary} ${finding.recommendation}`));
}

function primaryPage(context: RefactoringContext): RefactoringContextPageSummary | undefined {
  return context.pageSummaries[0];
}

function isRelevant(context: RefactoringContext): boolean {
  const page = primaryPage(context);
  return Boolean(
    page
    && (
      page.storyArchetype
      || page.inferredStory
      || context.crossPageCues.some((cue) => cue.dimension === 'story')
      || storyFinding(context)
    ),
  );
}

function evidenceLinks(context: RefactoringContext, page: RefactoringContextPageSummary): RefactoringEvidenceLink[] {
  const finding = storyFinding(context);
  const links: RefactoringEvidenceLink[] = [];

  if (finding) {
    links.push({
      findingId: finding.id,
      label: finding.title,
      pageName: page.pageName,
      detail: finding.summary,
    });
  }

  if (page.inferredStory) {
    links.push({
      label: `Inferred story: ${page.storyArchetype ?? 'page flow'}`,
      pageName: page.pageName,
      detail: page.inferredStory,
    });
  }

  return links;
}

export function buildStorytellingRefactoringScenarios(context: RefactoringContext): RefactoringScenario[] {
  const page = primaryPage(context);
  if (!page || !isRelevant(context)) {
    return [];
  }

  return [
    classifyRefactoringScenario({
      scenarioId: `storytelling-${context.remediationItemId}`,
      domain: 'storytelling',
      title: `Storytelling refactoring for ${page.visiblePageTitle ?? page.pageName}`,
      summary: 'Compare bounded narrative-flow options that improve the path from headline to supporting evidence.',
      options: [
        {
          optionId: `storytelling-${context.remediationItemId}-option-a`,
          label: 'Option A',
          title: 'Lead with the business takeaway before the evidence',
          summary: 'Resequence the page so the top-line statement comes first and the proof points appear immediately after it.',
          proposedChanges: [
            'Lead with the business question before the KPI explanation.',
            'Place supporting evidence after the top-line takeaway and before secondary detail.',
            'Close the page with deeper context only after the reader understands the headline message.',
          ],
          affectedScope: {
            scope: 'page',
            pageNames: [page.pageName],
          },
          rationale: page.inferredStory ?? context.remediationWhy,
          evidenceLinks: evidenceLinks(context, page),
          businessImpact: 'Improves narrative flow for readers who need a clear explanation of what matters before why it matters.',
          tradeoffs: [
            {
              title: 'Less immediate detail density',
              description: 'A cleaner narrative flow can delay access to secondary detail for readers who already know the headline context.',
            },
          ],
          confidence: 0.78,
          compilation: {
            status: 'advisoryOnly',
            hints: [],
          },
        },
      ],
    }),
  ];
}
