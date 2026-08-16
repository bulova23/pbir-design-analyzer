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

function navigationFinding(context: RefactoringContext): RefactoringContextFindingSummary | undefined {
  return context.findings.find((finding) =>
    finding.impactArea === 'navigation'
    || /\bnavigation\b|\bdrill\b|\bdetail\b|\breturn path\b/i.test(`${finding.summary} ${finding.recommendation}`));
}

function primaryPage(context: RefactoringContext): RefactoringContextPageSummary | undefined {
  return context.pageSummaries[0];
}

function isRelevant(context: RefactoringContext): boolean {
  const page = primaryPage(context);
  return Boolean(
    page
    && (
      navigationFinding(context)
      || context.crossPageCues.some((cue) => cue.dimension === 'navigation')
      || (page.visualSummary?.navigationVisualCount ?? 0) > 0
    ),
  );
}

function evidenceLinks(context: RefactoringContext, page: RefactoringContextPageSummary): RefactoringEvidenceLink[] {
  const finding = navigationFinding(context);
  const links: RefactoringEvidenceLink[] = [];

  if (finding) {
    links.push({
      findingId: finding.id,
      label: finding.title,
      pageName: page.pageName,
      detail: finding.summary,
    });
  }

  const cue = context.crossPageCues.find((entry) => entry.dimension === 'navigation');
  if (cue) {
    links.push({
      label: `Cross-page cue: ${cue.summary}`,
      pageName: cue.pageName,
      detail: cue.status,
    });
  }

  return links;
}

export function buildNavigationRefactoringScenarios(context: RefactoringContext): RefactoringScenario[] {
  const page = primaryPage(context);
  if (!page || !isRelevant(context)) {
    return [];
  }

  return [
    classifyRefactoringScenario({
      scenarioId: `navigation-${context.remediationItemId}`,
      domain: 'navigation',
      title: `Navigation refactoring for ${page.visiblePageTitle ?? page.pageName}`,
      summary: 'Compare bounded navigation options that strengthen the path from executive summary to supporting detail and back.',
      options: [
        {
          optionId: `navigation-${context.remediationItemId}-option-a`,
          label: 'Option A',
          title: 'Make the executive-to-detail path explicit',
          summary: 'Clarify the primary navigation path to supporting detail and make the return path visible from the same region.',
          proposedChanges: [
            'Clarify the navigation label from overview to supporting detail.',
            'Add a consistent return navigation path back to the executive summary.',
          ],
          affectedScope: {
            scope: context.affectedPages.length > 1 ? 'crossPage' : 'page',
            pageNames: [...context.affectedPages],
          },
          rationale: 'This reduces hunting for the next page in the decision flow.',
          evidenceLinks: evidenceLinks(context, page),
          businessImpact: 'Shortens the time required to move between summary status and the evidence that explains it.',
          tradeoffs: [
            {
              title: 'More visible navigation chrome',
              description: 'Better destination clarity can cost a small amount of report canvas space.',
            },
          ],
          confidence: 0.8,
          compilation: {
            status: 'advisoryOnly',
            hints: [],
          },
        },
      ],
    }),
  ];
}
