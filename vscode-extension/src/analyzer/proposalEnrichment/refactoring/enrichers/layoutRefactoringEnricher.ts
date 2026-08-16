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

function layoutFinding(context: RefactoringContext): RefactoringContextFindingSummary | undefined {
  return context.findings.find((finding) => finding.impactArea === 'layout' || finding.impactArea === 'density');
}

function primaryPage(context: RefactoringContext): RefactoringContextPageSummary | undefined {
  return context.pageSummaries[0];
}

function isRelevant(context: RefactoringContext): boolean {
  const page = primaryPage(context);
  const finding = layoutFinding(context);
  const text = [
    context.remediationTitle,
    context.remediationDetail,
    context.recommendedAction,
    finding?.summary,
    finding?.recommendation,
  ].join(' ');

  return Boolean(
    page
    && (
      !!finding
      || context.deterministicSupport.supportedOpportunityCategories.some((category) => ['alignment', 'spacing', 'grid', 'title'].includes(category))
      || /\blayout\b|\bdensity\b|\balign|\bspacing\b|\bhierarchy\b|\bgroup/i.test(text)
      || (page.visualSummary?.visualCount ?? 0) >= 6
    ),
  );
}

function evidenceLinks(context: RefactoringContext, page: RefactoringContextPageSummary): RefactoringEvidenceLink[] {
  const finding = layoutFinding(context);
  const links: RefactoringEvidenceLink[] = [];

  if (finding) {
    links.push({
      findingId: finding.id,
      label: finding.title,
      pageName: page.pageName,
      detail: finding.evidenceLabels.join(', '),
    });
  }

  if (page.visiblePageTitle) {
    links.push({
      label: `Page title: ${page.visiblePageTitle}`,
      pageName: page.pageName,
      detail: page.inferredPurpose,
    });
  }

  return links;
}

export function buildLayoutRefactoringScenarios(context: RefactoringContext): RefactoringScenario[] {
  const page = primaryPage(context);
  if (!page || !isRelevant(context)) {
    return [];
  }

  return [
    classifyRefactoringScenario({
      scenarioId: `layout-${context.remediationItemId}`,
      domain: 'layout',
      title: `Layout refactoring for ${page.visiblePageTitle ?? page.pageName}`,
      summary: 'Compare bounded density-reduction, grouping, alignment, and visual-hierarchy options.',
      options: [
        {
          optionId: `layout-${context.remediationItemId}-option-a`,
          label: 'Option A',
          title: 'Tighten the summary scan band',
          summary: 'Reduce density at the top of the page so the KPI line reads as one aligned summary zone.',
          proposedChanges: [
            'Align KPI cards to a single baseline.',
            'Reduce spacing variance between the KPI strip and the supporting trend.',
            'Strengthen title hierarchy above the summary zone.',
          ],
          affectedScope: {
            scope: 'page',
            pageNames: [page.pageName],
          },
          rationale: page.whyThisMatters ?? context.remediationWhy,
          evidenceLinks: evidenceLinks(context, page),
          businessImpact: 'Improves first-pass scanning and lowers visual friction for the primary summary view.',
          tradeoffs: [
            {
              title: 'Less room for annotations',
              description: 'A tighter summary zone improves scanability but leaves less whitespace for supporting notes.',
            },
          ],
          confidence: 0.82,
          compilation: {
            status: 'advisoryOnly',
            hints: [],
          },
        },
        {
          optionId: `layout-${context.remediationItemId}-option-b`,
          label: 'Option B',
          title: 'Separate summary and evidence into clearer zones',
          summary: 'Group the headline KPI layer apart from supporting visuals so the eye can move top-down with less competition.',
          proposedChanges: [
            'Group top-line KPIs into a dedicated summary zone.',
            'Move supporting evidence into a second zone below the summary layer.',
          ],
          affectedScope: {
            scope: 'page',
            pageNames: [page.pageName],
          },
          rationale: 'This emphasizes information grouping before introducing supporting evidence.',
          evidenceLinks: evidenceLinks(context, page),
          businessImpact: 'Improves information hierarchy for readers who need to separate headline status from supporting detail.',
          tradeoffs: [
            {
              title: 'Longer vertical travel',
              description: 'Clearer zoning improves grouping but can increase the amount of scrolling or eye travel.',
            },
          ],
          confidence: 0.74,
          compilation: {
            status: 'advisoryOnly',
            hints: [],
          },
        },
      ],
    }),
  ];
}
