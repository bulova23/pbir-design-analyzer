import type {
  ActionabilityBreakdown,
  BenchmarkComparisonSummary,
  PageIntentProfile,
  PagePurposeAnalysisSummary,
  PageStorySummary,
} from '../contracts/scorePanel';

interface BuildPagePurposeAnalysisOptions {
  storySummary?: PageStorySummary;
  pageIntentProfile?: PageIntentProfile;
  actionabilityBreakdown?: ActionabilityBreakdown;
  benchmarkComparison?: BenchmarkComparisonSummary;
}

function toPurposeLabel(profile: string | undefined): string {
  switch (profile) {
    case 'executive':
    case 'executiveOverview':
      return 'Executive';
    case 'operational':
    case 'operationalMonitoring':
      return 'Operational';
    case 'appendix':
    case 'detailReference':
      return 'Appendix';
    default:
      return 'Analytical';
  }
}

function toBenchmarkStatus(
  comparison: BenchmarkComparisonSummary | undefined,
): string | undefined {
  if (!comparison) {
    return undefined;
  }

  switch (comparison.comparativePosition) {
    case 'above':
      return 'Above expected';
    case 'below':
      return 'Below expected';
    default:
      return 'Mixed against expected';
  }
}

function buildTopGaps(
  actionabilityBreakdown: ActionabilityBreakdown | undefined,
  benchmarkComparison: BenchmarkComparisonSummary | undefined,
): string[] {
  const values = [
    ...(actionabilityBreakdown?.gaps ?? []),
    ...(benchmarkComparison?.gaps ?? []),
  ];

  return [...new Set(values.filter((value) => value.trim().length > 0))].slice(0, 3);
}

function buildRiskSentence(
  purpose: string,
  actionabilityBreakdown: ActionabilityBreakdown | undefined,
  benchmarkComparison: BenchmarkComparisonSummary | undefined,
): string {
  const gapText = (actionabilityBreakdown?.gaps ?? []).join(' ').toLowerCase();
  if (gapText.includes('target') || gapText.includes('benchmark') || gapText.includes('prior-period')) {
    return 'Decision makers may misinterpret KPI values without targets or prior-period comparison.';
  }

  if (gapText.includes('urgency') || gapText.includes('exception')) {
    return 'Important exceptions may be overlooked without stronger urgency and exception cues.';
  }

  if (benchmarkComparison?.beautifulButUseless || benchmarkComparison?.comparativePosition === 'below') {
    return 'Readers may over-trust visual polish even when the decision path is still weaker than expected.';
  }

  switch (purpose) {
    case 'Executive':
      return 'Decision makers may struggle to turn the page into fast, confident action.';
    case 'Operational':
      return 'Operators may miss the context needed to monitor performance reliably.';
    case 'Appendix':
      return 'Supporting readers may struggle to connect the page back to the main story.';
    default:
      return 'Readers may struggle to interpret the page’s intended reasoning path.';
  }
}

export function buildPagePurposeAnalysis(
  options: BuildPagePurposeAnalysisOptions,
): PagePurposeAnalysisSummary | undefined {
  const {
    storySummary,
    pageIntentProfile,
    actionabilityBreakdown,
    benchmarkComparison,
  } = options;

  if (!storySummary && !pageIntentProfile && !actionabilityBreakdown && !benchmarkComparison) {
    return undefined;
  }

  const purpose = toPurposeLabel(pageIntentProfile?.inferredProfile ?? storySummary?.intentProfile);
  const contextSentence = `${purpose === 'Executive' ? 'This page appears intended for executive review' : `This page appears intended for ${purpose.toLowerCase()} use`} but lacks the decision context expected for that audience.`;

  return {
    inferredPurpose: purpose,
    confidence: storySummary?.confidence,
    actionabilityScore: actionabilityBreakdown?.score,
    benchmarkStatus: toBenchmarkStatus(benchmarkComparison),
    topGaps: buildTopGaps(actionabilityBreakdown, benchmarkComparison),
    whyThisMatters: `${contextSentence} ${buildRiskSentence(purpose, actionabilityBreakdown, benchmarkComparison)}`,
  };
}
