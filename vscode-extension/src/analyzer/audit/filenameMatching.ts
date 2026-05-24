/**
 * Normalizes a bare filename (no extension) to a matchable string.
 * Strips leading numeric prefixes like "01 " or "02-", collapses
 * non-alphanumeric runs to single spaces, and lowercases.
 */
export function normalizeFilename(filename: string): string {
  const withoutExt = filename.replace(/\.[^.]+$/, '');
  const withoutLeadingNum = withoutExt.replace(/^\d+\s*[-_\s]*/, '');
  const withoutStateSuffix = withoutLeadingNum.replace(/\s*-\s*[^-]+$/, '');
  return withoutStateSuffix.toLowerCase().replace(/[^a-z0-9]+/g, ' ').trim();
}

export function normalizePageName(pageName: string): string {
  return pageName.toLowerCase().replace(/[^a-z0-9]+/g, ' ').trim();
}

/**
 * Extracts the state suffix from a filename if present.
 * "Net Sales - Default.png" → "Default"
 */
export function extractStateName(filename: string): string | undefined {
  const withoutExt = filename.replace(/\.[^.]+$/, '');
  const withoutLeadingNum = withoutExt.replace(/^\d+\s*[-_\s]*/, '');
  const match = withoutLeadingNum.match(/\s+-\s+(.+)$/);
  return match ? match[1].trim() : undefined;
}

export interface MatchResult {
  pageName: string;
  score: number;
}

const MIN_MATCH_SCORE = 0.4;

/**
 * Matches a screenshot filename against a list of PBIR page names.
 * Returns the best match above MIN_MATCH_SCORE, or undefined if none qualifies.
 */
export function matchFilenameToPages(
  filename: string,
  pageNames: string[],
): MatchResult | undefined {
  const normalizedFile = normalizeFilename(filename);

  let best: MatchResult | undefined;

  for (const pageName of pageNames) {
    const normalizedPage = normalizePageName(pageName);

    if (normalizedFile === normalizedPage) {
      return { pageName, score: 1.0 };
    }

    const score = computeScore(normalizedFile, normalizedPage);
    if (score >= MIN_MATCH_SCORE && (!best || score > best.score)) {
      best = { pageName, score };
    }
  }

  return best;
}

function computeScore(a: string, b: string): number {
  if (a.startsWith(b) || b.startsWith(a)) {
    return Math.min(a.length, b.length) / Math.max(a.length, b.length);
  }

  const aWords = new Set(a.split(' ').filter(Boolean));
  const bWords = new Set(b.split(' ').filter(Boolean));
  const intersection = [...aWords].filter((w) => bWords.has(w)).length;
  const union = new Set([...aWords, ...bWords]).size;

  return union > 0 ? intersection / union : 0;
}
