import type { RepositorySnapshot } from '../../project/repoSnapshot';
import { listSnapshotFiles, readSnapshotText, withRepositorySnapshot } from './repoEvidence';
import type { DesignTokenEvidenceReport } from './reviewTypes';

const TOKEN_PATTERN = /(--[a-z0-9-]+)\s*:/gi;
const COLOR_BYPASS_PATTERN = /#[0-9a-f]{3,8}/gi;
const SPACING_BYPASS_PATTERN = /\b(?:padding|margin|gap)\s*:\s*['"`]?\d+px/gi;

async function extractDesignTokenEvidenceFromSnapshot(
  snapshot: RepositorySnapshot,
): Promise<DesignTokenEvidenceReport> {
  const files = listSnapshotFiles(snapshot, (file) => /\.(ts|tsx|css|scss)$/i.test(file.relativePath));
  const report: DesignTokenEvidenceReport = {
    tokens: [],
    bypasses: [],
  };

  for (const file of files) {
    const text = await readSnapshotText(snapshot, file);
    const relativePath = file.relativePath;

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
  }

  return report;
}

export async function extractDesignTokenEvidence(
  source: RepositorySnapshot | string,
): Promise<DesignTokenEvidenceReport> {
  return withRepositorySnapshot(source, extractDesignTokenEvidenceFromSnapshot);
}
