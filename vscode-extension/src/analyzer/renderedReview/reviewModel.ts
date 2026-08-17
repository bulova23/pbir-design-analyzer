import type { NormalizedFinding } from '../contracts/scorePanel';
import type {
  RenderedReviewCategory,
  RenderedReviewChecklistItem,
  RenderedReviewClassification,
  RenderedReviewGuidance,
  RenderedScreenshotEvidence,
  RenderedReviewStatus,
} from './types';

const CATEGORY_RULES: Array<{
  category: RenderedReviewCategory;
  label: string;
  terms: string[];
  guidance: RenderedReviewGuidance;
}> = [
  {
    category: 'whitespaceBalance',
    label: 'Whitespace balance',
    terms: ['whitespace', 'white space', 'empty space', 'spacing'],
    guidance: {
      why: 'Rendered spacing can change the perceived balance of a page beyond what PBIR geometry shows.',
      lookFor: 'Check whether large gaps or tightly packed regions make the page feel uneven.',
      expectedOutcome: 'Whitespace should separate ideas and keep the page visually balanced.',
    },
  },
  {
    category: 'kpiProminence',
    label: 'KPI prominence',
    terms: ['kpi prominence', 'kpi emphasis', 'kpi', 'key performance indicator'],
    guidance: {
      why: 'KPI prominence depends on rendered scale and placement, not metadata alone.',
      lookFor: 'Verify that KPI cards visually dominate supporting charts and secondary labels.',
      expectedOutcome: 'The decision-driving KPI should be immediately discoverable.',
    },
  },
  {
    category: 'visualHierarchy',
    label: 'Visual hierarchy',
    terms: ['visual hierarchy', 'hierarchy', 'emphasis'],
    guidance: {
      why: 'Rendered size, contrast, and position determine which message is noticed first.',
      lookFor: 'Confirm that the primary takeaway is visually stronger than supporting detail.',
      expectedOutcome: 'The intended reading order should be obvious at a glance.',
    },
  },
  {
    category: 'titleWrapping',
    label: 'Title wrapping',
    terms: ['title wrapping', 'title wrap', 'wrapped title'],
    guidance: {
      why: 'Text wrapping depends on the rendered font, viewport, and available width.',
      lookFor: 'Check that titles do not wrap awkwardly, collide, or hide the intended meaning.',
      expectedOutcome: 'Titles should remain legible and preserve their intended hierarchy.',
    },
  },
  {
    category: 'clippedLabels',
    label: 'Clipped labels',
    terms: ['clipped label', 'label clipping', 'truncated label', 'cut off'],
    guidance: {
      why: 'Clipping is a rendered outcome that cannot be established reliably from PBIR metadata.',
      lookFor: 'Inspect axis labels, legends, headers, and filter text at the target viewport.',
      expectedOutcome: 'Labels should be fully visible or intentionally abbreviated.',
    },
  },
  {
    category: 'crowdedVisuals',
    label: 'Crowded visuals',
    terms: ['crowded visual', 'crowding', 'overlap', 'dense visual'],
    guidance: {
      why: 'A page can be technically valid while still being difficult to scan when rendered.',
      lookFor: 'Look for overlapping objects, compressed controls, and competing visual regions.',
      expectedOutcome: 'Each visual should have enough breathing room to support quick scanning.',
    },
  },
  {
    category: 'tableReadability',
    label: 'Table readability',
    terms: ['table readability', 'table read', 'table density', 'matrix readability'],
    guidance: {
      why: 'Rendered row height, column width, and contrast determine whether a table can be read comfortably.',
      lookFor: 'Check column widths, row density, headers, totals, and horizontal scanning effort.',
      expectedOutcome: 'The table should support accurate scanning without excessive zooming or scrolling.',
    },
  },
  {
    category: 'visualBalance',
    label: 'Visual balance',
    terms: ['visual balance', 'balance'],
    guidance: {
      why: 'Visual weight is a property of the rendered composition rather than a single score.',
      lookFor: 'Compare visual weight across the page and check for a dominant side or corner.',
      expectedOutcome: 'The composition should feel intentional and stable across the viewport.',
    },
  },
  {
    category: 'colorHarmony',
    label: 'Color harmony',
    terms: ['color harmony', 'colour harmony', 'color balance', 'color contrast'],
    guidance: {
      why: 'Rendered color relationships can alter emphasis and readability even when tokens are consistent.',
      lookFor: 'Check that colors reinforce meaning, maintain contrast, and do not create distracting competition.',
      expectedOutcome: 'Color should communicate hierarchy and meaning consistently across the page.',
    },
  },
  {
    category: 'pageReadability',
    label: 'Page readability',
    terms: ['page readability', 'readability', 'page clarity', 'hard to read'],
    guidance: {
      why: 'Overall readability emerges from the rendered combination of text, visuals, spacing, and contrast.',
      lookFor: 'Review the page at normal viewing size and identify anything that slows comprehension.',
      expectedOutcome: 'A reviewer should understand the page purpose and next action without excessive effort.',
    },
  },
  {
    category: 'unsupportedVisualType',
    label: 'Unsupported visual type',
    terms: [], // routed by evidence kind below, not by keyword matching
    guidance: {
      why: 'This visual type is not semantically analyzed by deterministic scoring — it may be a Deneb chart, an HTML Content visual, or another custom/AppSource visual.',
      lookFor: 'Confirm the visual renders as intended and communicates what it should.',
      expectedOutcome: 'The visual should be legible, correctly styled, and free of unexpected behavior.',
    },
  },
];

function textFor(finding: NormalizedFinding): string {
  return [finding.title, finding.summary].join(' ').toLowerCase();
}

function findRule(finding: NormalizedFinding) {
  const text = textFor(finding);
  return CATEGORY_RULES.find((rule) => rule.terms.some((term) => text.includes(term)));
}

export function classifyRenderedReviewFinding(finding: NormalizedFinding): {
  classification: RenderedReviewClassification;
  category?: RenderedReviewCategory;
} {
  if (finding.evidence.some((evidence) => evidence.kind === 'semanticModel') || /semantic/i.test(finding.sourceKind)) {
    return { classification: 'semantic' };
  }

  if (finding.evidence.some((evidence) => evidence.kind === 'customVisual')) {
    return { classification: 'renderedReviewRecommended', category: 'unsupportedVisualType' };
  }

  const rule = findRule(finding);
  return rule
    ? { classification: 'renderedReviewRecommended', category: rule.category }
    : { classification: 'deterministic' };
}

export function getRenderedReviewRule(category: RenderedReviewCategory) {
  return CATEGORY_RULES.find((rule) => rule.category === category)!;
}

export function buildRenderedReviewChecklist(findings: NormalizedFinding[]): RenderedReviewChecklistItem[] {
  const grouped = new Map<RenderedReviewCategory, NormalizedFinding[]>();
  for (const finding of findings) {
    const classification = classifyRenderedReviewFinding(finding);
    if (classification.classification !== 'renderedReviewRecommended' || !classification.category) continue;
    const existing = grouped.get(classification.category) ?? [];
    existing.push(finding);
    grouped.set(classification.category, existing);
  }

  return CATEGORY_RULES
    .filter((rule) => grouped.has(rule.category))
    .map((rule) => {
      const categoryFindings = grouped.get(rule.category)!;
      return {
        id: `rendered-review-${rule.category}`,
        category: rule.category,
        label: rule.label,
        findingIds: categoryFindings.map((finding) => finding.id).sort(),
        pageNames: [...new Set(categoryFindings.flatMap((finding) => finding.affectedPages))].sort(),
        guidance: rule.guidance,
        status: 'Not Reviewed' as const,
      };
    });
}

export function updateRenderedReviewItem(
  item: RenderedReviewChecklistItem,
  update: {
    status?: RenderedReviewStatus;
    reviewerNote?: string;
    screenshot?: RenderedScreenshotEvidence;
  },
): RenderedReviewChecklistItem {
  return {
    ...item,
    status: update.status ?? item.status,
    reviewerNote: update.reviewerNote ?? item.reviewerNote,
    screenshotEvidence: update.screenshot
      ? [...(item.screenshotEvidence ?? []), update.screenshot]
      : item.screenshotEvidence,
  };
}
