export type RenderedReviewClassification =
  | 'deterministic'
  | 'semantic'
  | 'renderedReviewRecommended';

export type RenderedReviewCategory =
  | 'whitespaceBalance'
  | 'visualHierarchy'
  | 'kpiProminence'
  | 'titleWrapping'
  | 'clippedLabels'
  | 'crowdedVisuals'
  | 'tableReadability'
  | 'visualBalance'
  | 'colorHarmony'
  | 'pageReadability';

export type RenderedReviewStatus = 'Not Reviewed' | 'Reviewed' | 'Confirmed' | 'Rejected' | 'Deferred';

export interface RenderedReviewGuidance {
  why: string;
  lookFor: string;
  expectedOutcome: string;
}

export interface RenderedScreenshotEvidence {
  report: string;
  page?: string;
  timestamp: string;
  provider: string;
  fileReference: string;
  notes?: string;
}

export interface RenderedReviewChecklistItem {
  id: string;
  category: RenderedReviewCategory;
  label: string;
  findingIds: string[];
  pageNames: string[];
  guidance: RenderedReviewGuidance;
  status: RenderedReviewStatus;
  reviewerNote?: string;
  screenshotEvidence?: RenderedScreenshotEvidence[];
}

