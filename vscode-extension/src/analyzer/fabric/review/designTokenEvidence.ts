import { collectRepoFiles, readRepoText, toRelativePath } from './repoEvidence';
import type { DesignTokenEvidenceReport } from './reviewTypes';

const TOKEN_PATTERN = /(--[a-z0-9-]+)\s*:/gi;
const COLOR_BYPASS_PATTERN = /#[0-9a-f]{3,8}/gi;
const SPACING_BYPASS_PATTERN = /\b(?:padding|margin|gap)\s*:\s*['"`]?\d+px/gi;

export function extractDesignTokenEvidence(rootPath: string): DesignTokenEvidenceReport {
  const files = collectRepoFiles(rootPath).filter((filePath) => /\.(ts|tsx|css|scss)$/i.test(filePath));

  return files.reduce<DesignTokenEvidenceReport>((report, filePath) => {
    const text = readRepoText(filePath);
    const relativePath = toRelativePath(rootPath, filePath);

    for (const match of text.matchAll(TOKEN_PATTERN)) {
      report.tokens.push({
        filePath: relativePath,
        token: match[1],
        summary: `Token ${match[1]} is defined.`,
      });
    }

    for (const match of text.matchAll(COLOR_BYPASS_PATTERN)) {
      report.bypasses.push({
        filePath: relativePath,
        summary: `Hard-coded color bypass detected: ${match[0]}.`,
      });
    }

    for (const match of text.matchAll(SPACING_BYPASS_PATTERN)) {
      report.bypasses.push({
        filePath: relativePath,
        summary: `Hard-coded spacing bypass detected: ${match[0]}.`,
      });
    }

    return report;
  }, {
    tokens: [],
    bypasses: [],
  });
}
