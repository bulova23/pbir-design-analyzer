import type {
  AffectedVisualReference,
  FrameworkFeedbackItem,
  PageScore,
  ScoreResult,
} from '../analyzer/contracts/scorePanel';

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

function readStringArray(source: Record<string, unknown>, key: string): string[] {
  const value = readProperty(source, key);
  if (!Array.isArray(value)) {
    return [];
  }

  return value.filter((entry): entry is string => typeof entry === 'string');
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
    scoringError: readOptionalString(candidate, 'scoringError'),
    frameworkWeights: readNumberRecord(candidate, 'frameworkWeights'),
  };
}

export function normalizeScoreResultPayload(value: unknown): ScoreResult {
  const candidate = isRecord(value) ? value : {};
  const pageScoresValue = readProperty(candidate, 'pageScores');

  return {
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
    layoutScore: readOptionalNumber(candidate, 'layoutScore'),
    themeScore: readOptionalNumber(candidate, 'themeScore'),
    governanceScore: readOptionalNumber(candidate, 'governanceScore'),
    frameworkWeights: readNumberRecord(candidate, 'frameworkWeights'),
  };
}
