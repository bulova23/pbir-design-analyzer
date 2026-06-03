import type {
  ActionabilityBreakdown,
  AffectedVisualReference,
  BenchmarkComparisonSummary,
  FixApplySessionRecord,
  FixOpportunity,
  FixSelectionApprovalState,
  FixSelectionState,
  ProposalEnrichment,
  ProposalEnrichmentValidationCode,
  ChartIntentConfidence,
  ChartIntentSummary,
  FindingType,
  FrameworkFeedbackItem,
  PageIntentProfile,
  PageStorySummary,
  PageVisualMetadataSummary,
  PageScore,
  ReportConsistencyFinding,
  ReportConsistencySummary,
  SemanticColorAssignment,
  ScoreResult,
  VisualMetadataItem,
} from '../analyzer/contracts/scorePanel';
import { buildFixBatchPreview } from '../analyzer/fixes/fixBatchPreview';
import { evaluateFixOpportunityCompatibility } from '../analyzer/fixes/fixCompatibility';
import { buildFixOpportunities } from '../analyzer/fixes/fixOpportunityBuilder';
import { buildCrossPageMatrix } from '../analyzer/score/crossPageMatrix';
import { buildFixPlan } from '../analyzer/score/fixPlan';
import { buildNormalizedFindings } from '../analyzer/score/normalizedFindings';
import { buildOverviewSummary } from '../analyzer/score/overviewSummary';
import { buildPagePurposeAnalysis } from '../analyzer/score/pagePurposeAnalysis';
import { getReviewPresentationPersonaProfiles } from '../analyzer/score/personaPresentation';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function alternateCaseKey(key: string): string {
  if (!key) {
    return key;
  }

  return `${key[0].toUpperCase()}${key.slice(1)}`;
}

function readProperty(source: Record<string, unknown>, key: string): unknown {
  if (key in source) {
    return source[key];
  }

  const alternateKey = alternateCaseKey(key);
  return alternateKey in source ? source[alternateKey] : undefined;
}

function readRequiredNumber(source: Record<string, unknown>, key: string): number {
  const value = readProperty(source, key);
  return typeof value === 'number' ? value : 0;
}

function readOptionalNumber(source: Record<string, unknown>, key: string): number | undefined {
  const value = readProperty(source, key);
  return typeof value === 'number' ? value : undefined;
}

function readOptionalString(source: Record<string, unknown>, key: string): string | undefined {
  const value = readProperty(source, key);
  return typeof value === 'string' ? value : undefined;
}

function readRequiredBoolean(source: Record<string, unknown>, key: string): boolean {
  const value = readProperty(source, key);
  return value === true;
}

function readOptionalBoolean(source: Record<string, unknown>, key: string): boolean | undefined {
  const value = readProperty(source, key);
  return typeof value === 'boolean' ? value : undefined;
}

function readStringArray(source: Record<string, unknown>, key: string): string[] {
  const value = readProperty(source, key);
  if (!Array.isArray(value)) {
    return [];
  }

  return value.filter((entry): entry is string => typeof entry === 'string');
}

function normalizeSemanticColorAssignment(value: unknown): SemanticColorAssignment | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const semanticKey = readOptionalString(value, 'semanticKey');
  const color = readOptionalString(value, 'color');
  const sourceVisualId = readOptionalString(value, 'sourceVisualId');
  const sourcePageName = readOptionalString(value, 'sourcePageName');
  if (!semanticKey || !color || !sourceVisualId || !sourcePageName) {
    return undefined;
  }

  return {
    semanticKey,
    displayLabel: readOptionalString(value, 'displayLabel'),
    color,
    sourceVisualId,
    sourcePageName,
  };
}

function normalizeChartIntentSummary(value: unknown): ChartIntentSummary | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const intent = readOptionalString(value, 'intent');
  if (!intent) {
    return undefined;
  }

  return {
    intent,
    confidence: normalizeChartIntentConfidence(readProperty(value, 'confidence')),
    evidence: readStringArray(value, 'evidence'),
    fitStatus: readOptionalString(value, 'fitStatus'),
    recommendedAlternatives: readStringArray(value, 'recommendedAlternatives'),
  };
}

function normalizeProposalEnrichment(value: unknown): ProposalEnrichment | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const remediationItemId = readOptionalString(value, 'remediationItemId');
  const status = readOptionalString(value, 'status');
  const source = readOptionalString(value, 'source');
  if (!remediationItemId || !status || !source) {
    return undefined;
  }

  const titleSuggestionsValue = readProperty(value, 'titleSuggestions');
  const explanationValue = readProperty(value, 'explanation');
  const whyThisMattersValue = readProperty(value, 'whyThisMatters');
  const advisoryPriorityValue = readProperty(value, 'advisoryPriority');
  const expectedOutcomeValue = readProperty(value, 'expectedOutcome');
  const advisoryAlternativesValue = readProperty(value, 'advisoryAlternatives');
  const validationValue = readProperty(value, 'validation');
  const provenanceValue = readProperty(value, 'provenance');

  return {
    remediationItemId,
    status: status as ProposalEnrichment['status'],
    source: source as ProposalEnrichment['source'],
    enrichersApplied: readStringArray(value, 'enrichersApplied') as ProposalEnrichment['enrichersApplied'],
    titleSuggestions: Array.isArray(titleSuggestionsValue)
      ? titleSuggestionsValue
        .filter(isRecord)
        .map((item) => ({
          title: readOptionalString(item, 'title') ?? '',
          confidence: readRequiredNumber(item, 'confidence'),
          rationale: readOptionalString(item, 'rationale') ?? '',
        }))
        .filter((item) => item.title.length > 0)
      : undefined,
    explanation: isRecord(explanationValue)
      ? {
          shortText: readOptionalString(explanationValue, 'shortText') ?? '',
          expandedText: readOptionalString(explanationValue, 'expandedText'),
        }
      : undefined,
    whyThisMatters: isRecord(whyThisMattersValue)
      ? {
          text: readOptionalString(whyThisMattersValue, 'text') ?? '',
        }
      : undefined,
    advisoryPriority: isRecord(advisoryPriorityValue)
      ? {
          tier: readOptionalString(advisoryPriorityValue, 'tier') as NonNullable<ProposalEnrichment['advisoryPriority']>['tier'],
          rationale: readOptionalString(advisoryPriorityValue, 'rationale') ?? '',
        }
      : undefined,
    expectedOutcome: isRecord(expectedOutcomeValue)
      ? {
          text: readOptionalString(expectedOutcomeValue, 'text') ?? '',
          areas: readStringArray(expectedOutcomeValue, 'areas'),
        }
      : undefined,
    advisoryAlternatives: Array.isArray(advisoryAlternativesValue)
      ? advisoryAlternativesValue
        .filter(isRecord)
        .map((item) => ({
          title: readOptionalString(item, 'title') ?? '',
          description: readOptionalString(item, 'description') ?? '',
        }))
        .filter((item) => item.title.length > 0 && item.description.length > 0)
      : [],
    validation: isRecord(validationValue)
      ? {
          status: (readOptionalString(validationValue, 'status') as ProposalEnrichment['validation']['status']) ?? 'passed',
          issues: Array.isArray(readProperty(validationValue, 'issues'))
            ? (readProperty(validationValue, 'issues') as unknown[])
              .filter(isRecord)
              .map((item) => ({
                code: readOptionalString(item, 'code') as ProposalEnrichmentValidationCode,
                message: readOptionalString(item, 'message') ?? '',
                section: readOptionalString(item, 'section') as ProposalEnrichment['validation']['issues'][number]['section'],
              }))
              .filter((item) => item.code && item.message)
            : [],
        }
      : {
          status: 'passed',
          issues: [],
        },
    provenance: isRecord(provenanceValue)
      ? {
          providerName: readOptionalString(provenanceValue, 'providerName'),
          usedFallback: readRequiredBoolean(provenanceValue, 'usedFallback'),
          enrichedAt: readOptionalString(provenanceValue, 'enrichedAt') ?? '',
          sourceFindingIds: readStringArray(provenanceValue, 'sourceFindingIds'),
        }
      : {
          usedFallback: false,
          enrichedAt: '',
          sourceFindingIds: [],
        },
  };
}

function normalizeReportConsistencySummary(value: unknown): ReportConsistencySummary | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const consistentTitleAnchors = readOptionalBoolean(value, 'consistentTitleAnchors');
  const consistentFilterBand = readOptionalBoolean(value, 'consistentFilterBand');
  const consistentMetricLabels = readOptionalBoolean(value, 'consistentMetricLabels');
  const consistentSemanticColors = readOptionalBoolean(value, 'consistentSemanticColors');
  if (
    consistentTitleAnchors === undefined ||
    consistentFilterBand === undefined ||
    consistentMetricLabels === undefined ||
    consistentSemanticColors === undefined
  ) {
    return undefined;
  }

  return {
    consistentTitleAnchors,
    consistentFilterBand,
    consistentMetricLabels,
    consistentSemanticColors,
    overallFinding: readOptionalString(value, 'overallFinding'),
    affectedPages: readStringArray(value, 'affectedPages'),
    issueCount: readRequiredNumber(value, 'issueCount'),
    issues: Array.isArray(readProperty(value, 'issues'))
      ? (readProperty(value, 'issues') as unknown[])
          .map((entry) => normalizeReportConsistencyFinding(entry))
          .filter((entry): entry is ReportConsistencyFinding => Boolean(entry))
      : [],
    findings: readStringArray(value, 'findings'),
  };
}

function normalizeReportConsistencyFinding(value: unknown): ReportConsistencyFinding | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const category = readOptionalString(value, 'category');
  const issueCategory = readOptionalString(value, 'issueCategory');
  const overallFinding = readOptionalString(value, 'overallFinding');
  const severity = readOptionalString(value, 'severity');
  const confidence = readOptionalString(value, 'confidence');
  const recommendedRemediation = readOptionalString(value, 'recommendedRemediation');
  if (!category || !issueCategory || !overallFinding || !recommendedRemediation) {
    return undefined;
  }

  if (
    (severity !== 'high' && severity !== 'medium' && severity !== 'low') ||
    (confidence !== 'high' && confidence !== 'medium' && confidence !== 'low')
  ) {
    return undefined;
  }

  return {
    category,
    issueCategory,
    overallFinding,
    affectedPages: readStringArray(value, 'affectedPages'),
    severity,
    confidence,
    recommendedRemediation,
  };
}

function normalizeChartIntentConfidence(value: unknown): ChartIntentConfidence | undefined {
  return value === 'high' || value === 'medium' || value === 'low'
    ? value
    : undefined;
}

function normalizeStoryConfidence(value: unknown): PageStorySummary['confidence'] | undefined {
  return value === 'high' || value === 'medium' || value === 'low'
    ? value
    : undefined;
}

function normalizePageStorySummary(value: unknown): PageStorySummary | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const intentProfile = readOptionalString(value, 'intentProfile');
  const storyArchetype = readOptionalString(value, 'storyArchetype');
  const inferredStory = readOptionalString(value, 'inferredStory');
  const confidence = normalizeStoryConfidence(readProperty(value, 'confidence'));
  if (!intentProfile || !storyArchetype || !inferredStory || !confidence) {
    return undefined;
  }

  return {
    intentProfile,
    storyArchetype,
    inferredStory,
    confidence,
    evidence: readStringArray(value, 'evidence'),
  };
}

function normalizePageIntentProfileType(value: unknown): PageIntentProfile['inferredProfile'] | undefined {
  return value === 'executive' || value === 'operational' || value === 'analytical' || value === 'appendix'
    ? value
    : undefined;
}

function normalizePageIntentProfile(value: unknown): PageIntentProfile | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const inferredProfile = normalizePageIntentProfileType(readProperty(value, 'inferredProfile'));
  const actionabilityExpectation = readProperty(value, 'actionabilityExpectation');
  if (
    !inferredProfile ||
    (actionabilityExpectation !== 'high' && actionabilityExpectation !== 'medium' && actionabilityExpectation !== 'low')
  ) {
    return undefined;
  }

  return {
    inferredProfile,
    actionabilityExpectation,
    reviewGuidance: readStringArray(value, 'reviewGuidance'),
    evidence: readStringArray(value, 'evidence'),
  };
}

function normalizeActionabilityBreakdown(value: unknown): ActionabilityBreakdown | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const expectationLevel = readProperty(value, 'expectationLevel');
  const summary = readOptionalString(value, 'summary');
  if (
    !summary ||
    (expectationLevel !== 'high' && expectationLevel !== 'medium' && expectationLevel !== 'low')
  ) {
    return undefined;
  }

  return {
    score: readRequiredNumber(value, 'score'),
    targetBenchmarkPresent: readRequiredBoolean(value, 'targetBenchmarkPresent'),
    exceptionVisibility: readRequiredBoolean(value, 'exceptionVisibility'),
    urgencySignaling: readRequiredBoolean(value, 'urgencySignaling'),
    priorPeriodContext: readRequiredBoolean(value, 'priorPeriodContext'),
    drillPathPresent: readRequiredBoolean(value, 'drillPathPresent'),
    expectationLevel,
    strengths: readStringArray(value, 'strengths'),
    gaps: readStringArray(value, 'gaps'),
    summary,
  };
}

function normalizeBenchmarkComparison(value: unknown): BenchmarkComparisonSummary | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const archetype = readOptionalString(value, 'archetype');
  const benchmarkLabel = readOptionalString(value, 'benchmarkLabel');
  const comparativePosition = readProperty(value, 'comparativePosition');
  const insight = readOptionalString(value, 'insight');
  if (
    !archetype ||
    !benchmarkLabel ||
    !insight ||
    (comparativePosition !== 'above' && comparativePosition !== 'mixed' && comparativePosition !== 'below')
  ) {
    return undefined;
  }

  return {
    archetype,
    benchmarkLabel,
    comparativePosition,
    beautifulButUseless: readRequiredBoolean(value, 'beautifulButUseless'),
    insight,
    strengths: readStringArray(value, 'strengths'),
    gaps: readStringArray(value, 'gaps'),
  };
}

function normalizeAffectedVisual(value: unknown): AffectedVisualReference | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const pageName = readOptionalString(value, 'pageName');
  const visualId = readOptionalString(value, 'visualId');
  const visualType = readOptionalString(value, 'visualType');

  if (!pageName || !visualId || !visualType) {
    return undefined;
  }

  return {
    pageName,
    visualId,
    visualType,
  };
}

function normalizeFindingType(value: unknown): FindingType {
  return value === 'objective' || value === 'strongHeuristic' || value === 'stylePreference'
    ? value
    : 'strongHeuristic';
}

function readStringRecord(source: Record<string, unknown>, key: string): Record<string, string> {
  const value = readProperty(source, key);
  if (!isRecord(value)) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(value).filter((entry): entry is [string, string] => typeof entry[1] === 'string'),
  );
}

function readNumberRecord(source: Record<string, unknown>, key: string): Record<string, number> | undefined {
  const value = readProperty(source, key);
  if (!isRecord(value)) {
    return undefined;
  }

  return Object.fromEntries(
    Object.entries(value).filter((entry): entry is [string, number] => typeof entry[1] === 'number'),
  );
}

function normalizeFeedbackItem(value: unknown): FrameworkFeedbackItem | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const text = readOptionalString(value, 'text');
  if (!text) {
    return undefined;
  }

  return {
    ok: readProperty(value, 'ok') === true,
    text,
    findingType: normalizeFindingType(readProperty(value, 'findingType')),
    affectedVisuals: Array.isArray(readProperty(value, 'affectedVisuals'))
      ? (readProperty(value, 'affectedVisuals') as unknown[])
          .map((entry) => normalizeAffectedVisual(entry))
          .filter((entry): entry is AffectedVisualReference => Boolean(entry))
      : undefined,
    earnedPoints: readOptionalNumber(value, 'earnedPoints'),
    possiblePoints: readOptionalNumber(value, 'possiblePoints'),
  };
}

function normalizeFeedback(value: unknown): Record<string, FrameworkFeedbackItem[]> {
  if (!isRecord(value)) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(value).map(([frameworkKey, items]) => {
      if (!Array.isArray(items)) {
        return [frameworkKey, []];
      }

      return [
        frameworkKey,
        items
          .map((item) => normalizeFeedbackItem(item))
          .filter((item): item is FrameworkFeedbackItem => Boolean(item)),
      ];
    }),
  );
}

function normalizeVisualMetadataItem(value: unknown): VisualMetadataItem | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const visualId = readOptionalString(value, 'visualId');
  const visualType = readOptionalString(value, 'visualType');
  if (!visualId || !visualType) {
    return undefined;
  }

  return {
    visualId,
    visualType,
    x: readRequiredNumber(value, 'x'),
    y: readRequiredNumber(value, 'y'),
    width: readRequiredNumber(value, 'width'),
    height: readRequiredNumber(value, 'height'),
    isHidden: readRequiredBoolean(value, 'isHidden'),
    isNavigationElement: readRequiredBoolean(value, 'isNavigationElement'),
    isDecorative: readRequiredBoolean(value, 'isDecorative'),
    isSlicer: readRequiredBoolean(value, 'isSlicer'),
    visibleTitleText: readOptionalString(value, 'visibleTitleText'),
    visibleSubtitleText: readOptionalString(value, 'visibleSubtitleText'),
    textBoxText: readOptionalString(value, 'textBoxText'),
    bestVisibleText: readOptionalString(value, 'bestVisibleText'),
    hasVisibleTitleIntent: readRequiredBoolean(value, 'hasVisibleTitleIntent'),
    hasLegend: readOptionalBoolean(value, 'hasLegend'),
    hasAxisLabels: readOptionalBoolean(value, 'hasAxisLabels'),
    hasDataLabels: readOptionalBoolean(value, 'hasDataLabels'),
    categoryHints: readStringArray(value, 'categoryHints'),
    valueHints: readStringArray(value, 'valueHints'),
    seriesHints: readStringArray(value, 'seriesHints'),
    measureHints: readStringArray(value, 'measureHints'),
    backgroundFillColor: readOptionalString(value, 'backgroundFillColor'),
    fontColor: readOptionalString(value, 'fontColor'),
    hasBorder: readOptionalBoolean(value, 'hasBorder'),
    cornerRadius: readOptionalNumber(value, 'cornerRadius'),
    hasShadow: readOptionalBoolean(value, 'hasShadow'),
    semanticColors: Array.isArray(readProperty(value, 'semanticColors'))
      ? (readProperty(value, 'semanticColors') as unknown[])
          .map((entry) => normalizeSemanticColorAssignment(entry))
          .filter((entry): entry is SemanticColorAssignment => Boolean(entry))
      : [],
    chartIntent: normalizeChartIntentSummary(readProperty(value, 'chartIntent')),
  };
}

function normalizePageVisualMetadata(value: unknown): PageVisualMetadataSummary | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const pageName = readOptionalString(value, 'pageName');
  if (!pageName) {
    return undefined;
  }

  const visualsValue = readProperty(value, 'visuals');

  return {
    pageName,
    visiblePageTitle: readOptionalString(value, 'visiblePageTitle'),
    strictVisiblePageTitle: readOptionalString(value, 'strictVisiblePageTitle'),
    canvasWidth: readOptionalNumber(value, 'canvasWidth'),
    canvasHeight: readOptionalNumber(value, 'canvasHeight'),
    semanticColorMap: Array.isArray(readProperty(value, 'semanticColorMap'))
      ? (readProperty(value, 'semanticColorMap') as unknown[])
          .map((entry) => normalizeSemanticColorAssignment(entry))
          .filter((entry): entry is SemanticColorAssignment => Boolean(entry))
      : [],
    chartIntentSummary: normalizeChartIntentSummary(readProperty(value, 'chartIntentSummary')),
    visualCount: readRequiredNumber(value, 'visualCount'),
    visibleTitleVisualCount: readRequiredNumber(value, 'visibleTitleVisualCount'),
    textVisualCount: readRequiredNumber(value, 'textVisualCount'),
    slicerCount: readRequiredNumber(value, 'slicerCount'),
    legendVisualCount: readRequiredNumber(value, 'legendVisualCount'),
    axisLabelVisualCount: readRequiredNumber(value, 'axisLabelVisualCount'),
    dataLabelVisualCount: readRequiredNumber(value, 'dataLabelVisualCount'),
    formattedVisualCount: readRequiredNumber(value, 'formattedVisualCount'),
    visuals: Array.isArray(visualsValue)
      ? visualsValue
          .map((entry) => normalizeVisualMetadataItem(entry))
          .filter((entry): entry is VisualMetadataItem => Boolean(entry))
      : [],
  };
}

function normalizePageScore(value: unknown): PageScore {
  const candidate = isRecord(value) ? value : {};

  return {
    pageName: readOptionalString(candidate, 'pageName') ?? 'Page',
    gestaltScore: readRequiredNumber(candidate, 'gestaltScore'),
    cognitiveLoadScore: readRequiredNumber(candidate, 'cognitiveLoadScore'),
    dataInkScore: readRequiredNumber(candidate, 'dataInkScore'),
    accessibilityScore: readRequiredNumber(candidate, 'accessibilityScore'),
    visualBestPracticesScore: readRequiredNumber(candidate, 'visualBestPracticesScore'),
    stephenFewScore: readRequiredNumber(candidate, 'stephenFewScore'),
    enterpriseGovernanceScore: readRequiredNumber(candidate, 'enterpriseGovernanceScore'),
    tufteScore: readRequiredNumber(candidate, 'tufteScore'),
    graphicalPerceptionScore: readRequiredNumber(candidate, 'graphicalPerceptionScore'),
    densityScore: readRequiredNumber(candidate, 'densityScore'),
    narrativeScore: readRequiredNumber(candidate, 'narrativeScore'),
    dataVisualCount: readOptionalNumber(candidate, 'dataVisualCount'),
    navigationVisualCount: readOptionalNumber(candidate, 'navigationVisualCount'),
    hiddenVisualCount: readOptionalNumber(candidate, 'hiddenVisualCount'),
    compositeScore: readRequiredNumber(candidate, 'compositeScore'),
    feedback: normalizeFeedback(readProperty(candidate, 'feedback')),
    recommendations: readStringArray(candidate, 'recommendations'),
    reportConsistencyNotes: readStringArray(candidate, 'reportConsistencyNotes'),
    inferredStorySummary: normalizePageStorySummary(readProperty(candidate, 'inferredStorySummary')),
    pageIntentProfile: normalizePageIntentProfile(readProperty(candidate, 'pageIntentProfile')),
    actionabilityBreakdown: normalizeActionabilityBreakdown(readProperty(candidate, 'actionabilityBreakdown')),
    benchmarkComparison: normalizeBenchmarkComparison(readProperty(candidate, 'benchmarkComparison')),
    scoringError: readOptionalString(candidate, 'scoringError'),
    frameworkWeights: readNumberRecord(candidate, 'frameworkWeights'),
    visualMetadata: normalizePageVisualMetadata(readProperty(candidate, 'visualMetadata')),
    pagePurposeAnalysis: undefined,
  };
}

export function normalizeScoreResultPayload(value: unknown): ScoreResult {
  const candidate = isRecord(value) ? value : {};
  const pageScoresValue = readProperty(candidate, 'pageScores');
  const normalized: ScoreResult = {
    gestaltScore: readRequiredNumber(candidate, 'gestaltScore'),
    cognitiveLoadScore: readRequiredNumber(candidate, 'cognitiveLoadScore'),
    dataInkScore: readRequiredNumber(candidate, 'dataInkScore'),
    accessibilityScore: readRequiredNumber(candidate, 'accessibilityScore'),
    visualBestPracticesScore: readRequiredNumber(candidate, 'visualBestPracticesScore'),
    stephenFewScore: readRequiredNumber(candidate, 'stephenFewScore'),
    enterpriseGovernanceScore: readRequiredNumber(candidate, 'enterpriseGovernanceScore'),
    tufteScore: readRequiredNumber(candidate, 'tufteScore'),
    graphicalPerceptionScore: readRequiredNumber(candidate, 'graphicalPerceptionScore'),
    densityScore: readRequiredNumber(candidate, 'densityScore'),
    narrativeScore: readRequiredNumber(candidate, 'narrativeScore'),
    compositeScore: readRequiredNumber(candidate, 'compositeScore'),
    feedback: normalizeFeedback(readProperty(candidate, 'feedback')),
    pageCount: readRequiredNumber(candidate, 'pageCount'),
    recommendations: readStringArray(candidate, 'recommendations'),
    reportPath: readOptionalString(candidate, 'reportPath') ?? '',
    scoredAt: readOptionalString(candidate, 'scoredAt') ?? new Date().toISOString(),
    dataVisualCount: readOptionalNumber(candidate, 'dataVisualCount'),
    navigationVisualCount: readOptionalNumber(candidate, 'navigationVisualCount'),
    hiddenVisualCount: readOptionalNumber(candidate, 'hiddenVisualCount'),
    pageScores: Array.isArray(pageScoresValue)
      ? pageScoresValue.map((page) => normalizePageScore(page))
      : undefined,
    scoredPageName: readOptionalString(candidate, 'scoredPageName'),
    scoringErrors: readStringRecord(candidate, 'scoringErrors'),
    reportConsistencySummary: normalizeReportConsistencySummary(readProperty(candidate, 'reportConsistencySummary')),
    inferredStorySummary: normalizePageStorySummary(readProperty(candidate, 'inferredStorySummary')),
    pageIntentProfile: normalizePageIntentProfile(readProperty(candidate, 'pageIntentProfile')),
    actionabilityBreakdown: normalizeActionabilityBreakdown(readProperty(candidate, 'actionabilityBreakdown')),
    benchmarkComparison: normalizeBenchmarkComparison(readProperty(candidate, 'benchmarkComparison')),
    layoutScore: readOptionalNumber(candidate, 'layoutScore'),
    themeScore: readOptionalNumber(candidate, 'themeScore'),
    governanceScore: readOptionalNumber(candidate, 'governanceScore'),
    frameworkWeights: readNumberRecord(candidate, 'frameworkWeights'),
    visualMetadata: normalizePageVisualMetadata(readProperty(candidate, 'visualMetadata')),
    pagePurposeAnalysis: undefined,
    proposalEnrichments: Array.isArray(readProperty(candidate, 'proposalEnrichments'))
      ? (readProperty(candidate, 'proposalEnrichments') as unknown[])
        .map((entry) => normalizeProposalEnrichment(entry))
        .filter((entry): entry is ProposalEnrichment => Boolean(entry))
      : [],
  };

  normalized.pageScores = normalized.pageScores?.map((page) => ({
    ...page,
    pagePurposeAnalysis: buildPagePurposeAnalysis({
      storySummary: page.inferredStorySummary,
      pageIntentProfile: page.pageIntentProfile,
      actionabilityBreakdown: page.actionabilityBreakdown,
      benchmarkComparison: page.benchmarkComparison,
    }),
  }));
  normalized.pagePurposeAnalysis = buildPagePurposeAnalysis({
    storySummary: normalized.inferredStorySummary,
    pageIntentProfile: normalized.pageIntentProfile,
    actionabilityBreakdown: normalized.actionabilityBreakdown,
    benchmarkComparison: normalized.benchmarkComparison,
  });
  normalized.normalizedFindings = buildNormalizedFindings(normalized);
  normalized.fixPlan = buildFixPlan(normalized.normalizedFindings);
  normalized.fixOpportunities = buildFixOpportunities(normalized);
  normalized.overviewSummary = buildOverviewSummary(normalized);
  normalized.crossPageMatrix = buildCrossPageMatrix(normalized.normalizedFindings, normalized.pageScores);
  normalized.personaPresentation = {
    activePersona: 'default',
    availablePersonas: getReviewPresentationPersonaProfiles(),
  };
  return normalized;
}

export function buildFixWorkflowPayload(input: {
  opportunities: FixOpportunity[];
  selectedOpportunityIds: string[];
  approvalState: FixSelectionApprovalState;
  message?: string;
  fixApplySessions?: FixApplySessionRecord[];
}): {
  fixSelection: FixSelectionState;
  fixApplySessions: FixApplySessionRecord[];
} {
  const selected = input.opportunities.filter((item) => input.selectedOpportunityIds.includes(item.id));
  const compatibility = evaluateFixOpportunityCompatibility(selected);

  return {
    fixSelection: {
      selectedOpportunityIds: selected.map((item) => item.id),
      compatibility,
      groupedPreview: input.approvalState === 'NeedsPreview' || !compatibility.isCompatible || selected.length === 0
        ? undefined
        : buildFixBatchPreview(selected),
      approvalState: input.approvalState,
      message: input.message,
    },
    fixApplySessions: input.fixApplySessions ?? [],
  };
}
