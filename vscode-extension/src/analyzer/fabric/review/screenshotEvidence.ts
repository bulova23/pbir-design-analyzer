import * as path from 'path';
import { extractStateName, matchFilenameToPages } from '../../audit/filenameMatching';
import type { RepositorySnapshot } from '../../project/repoSnapshot';
import { listSnapshotFiles, withRepositorySnapshot } from './repoEvidence';
import type { ScreenshotEvidenceReport } from './reviewTypes';

const IMAGE_EXTENSIONS = new Set(['.png', '.jpg', '.jpeg', '.webp']);

async function extractScreenshotEvidenceFromSnapshot(
  snapshot: RepositorySnapshot,
  pageNames: string[],
): Promise<ScreenshotEvidenceReport> {
  const files = listSnapshotFiles(snapshot, (file) => IMAGE_EXTENSIONS.has(path.extname(file.relativePath).toLowerCase()))
    .map((file) => file.relativePath)
    .sort((left, right) => left.localeCompare(right))
    .slice(0, 12);

  const captures: ScreenshotEvidenceReport['captures'] = [];
  const unmatchedCaptures: ScreenshotEvidenceReport['unmatchedCaptures'] = [];

  for (const filePath of files) {
    const fileName = path.basename(filePath);
    const stateName = extractStateName(fileName);
    const match = matchFilenameToPages(fileName, pageNames);

    if (match) {
      captures.push({
        filePath: filePath,
        fileName,
        pageName: match.pageName,
        stateName,
      });
      continue;
    }

    unmatchedCaptures.push({
      filePath: filePath,
      fileName,
      stateName,
    });
  }

  return {
    captures,
    unmatchedCaptures,
  };
}

export async function extractScreenshotEvidence(
  source: RepositorySnapshot | string,
  pageNames: string[],
): Promise<ScreenshotEvidenceReport> {
  return withRepositorySnapshot(source, async (snapshot) => extractScreenshotEvidenceFromSnapshot(snapshot, pageNames));
}
