import type {
  RefactoringEvidenceLink,
  RefactoringScenario,
} from '../../../contracts/scorePanel';
import { classifyRefactoringScenario } from '../refactoringCompilationClassifier';
import type {
  RefactoringContext,
  RefactoringContextPageSummary,
} from '../refactoringContextBuilder';

function primaryPage(context: RefactoringContext): RefactoringContextPageSummary | undefined {
  return context.pageSummaries[0];
}

function isRelevant(context: RefactoringContext): boolean {
  const page = primaryPage(context);
  const text = [page?.inferredPurpose, page?.whyThisMatters, context.remediationWhy, ...context.resolvedOutcomes].join(' ');

  return Boolean(
    page
    && (
      /\bexecutive\b|\bbenchmark\b|\bdecision\b|\bKPI\b/i.test(text)
      || page.inferredPurpose === 'Executive'
    ),
  );
}

function evidenceLinks(context: RefactoringContext, page: RefactoringContextPageSummary): RefactoringEvidenceLink[] {
  return [
    ...(context.findings[0]
      ? [{
          findingId: context.findings[0].id,
          label: context.findings[0].title,
          pageName: page.pageName,
          detail: context.findings[0].summary,
        }]
      : []),
    ...(page.whyThisMatters
      ? [{
          label: 'Page purpose analysis',
          pageName: page.pageName,
          detail: page.whyThisMatters,
        }]
      : []),
  ];
}

export function buildExecutiveExperienceRefactoringScenarios(context: RefactoringContext): RefactoringScenario[] {
  const page = primaryPage(context);
  if (!page || !isRelevant(context)) {
    return [];
  }

  return [
    classifyRefactoringScenario({
      scenarioId: `executive-experience-${context.remediationItemId}`,
      domain: 'executiveExperience',
      title: `Executive experience refactoring for ${page.visiblePageTitle ?? page.pageName}`,
      summary: 'Compare bounded executive-first options that improve summary clarity, KPI emphasis, decision framing, and benchmark visibility.',
      options: [
        {
          optionId: `executive-experience-${context.remediationItemId}-option-a`,
          label: 'Option A',
          title: 'Promote the top KPI headline and benchmark context',
          summary: 'Frame the page as a decision-ready summary by emphasizing the leading KPI, benchmark context, and a brief interpretation.',
          proposedChanges: [
            'Strengthen the KPI title hierarchy for the leading summary metric.',
            'Align benchmark context directly beneath the primary KPI headline.',
          ],
          affectedScope: {
            scope: 'page',
            pageNames: [page.pageName],
          },
          rationale: page.whyThisMatters ?? context.remediationWhy,
          evidenceLinks: evidenceLinks(context, page),
          businessImpact: 'Improves decision-support framing for leadership readers who need a fast read on status and context.',
          tradeoffs: [
            {
              title: 'More benchmark emphasis, less narrative space',
              description: 'Stronger benchmark visibility can reduce room for secondary supporting commentary near the top of the page.',
            },
          ],
          confidence: 0.81,
          compilation: {
            status: 'advisoryOnly',
            hints: [],
          },
        },
        {
          optionId: `executive-experience-${context.remediationItemId}-option-b`,
          label: 'Option B',
          title: 'Separate status, benchmark, and action framing',
          summary: 'Keep the leading KPI concise, then surface benchmark context and decision framing as distinct follow-on cues.',
          proposedChanges: [
            'Present the status signal first, then place benchmark context in a dedicated supporting callout.',
            'State the decision implication near the supporting trend rather than inside the KPI area.',
          ],
          affectedScope: {
            scope: 'page',
            pageNames: [page.pageName],
          },
          rationale: 'This keeps the executive summary compact while preserving enough context to explain the action signal.',
          evidenceLinks: evidenceLinks(context, page),
          businessImpact: 'Improves readability for executives who prefer a compact headline before reading supporting context.',
          tradeoffs: [
            {
              title: 'Lower benchmark immediacy',
              description: 'Separating the benchmark from the KPI reduces clutter but makes the comparison slightly less immediate.',
            },
          ],
          confidence: 0.76,
          compilation: {
            status: 'advisoryOnly',
            hints: [],
          },
        },
      ],
    }),
  ];
}
