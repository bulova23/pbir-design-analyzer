import * as fs from 'fs';
import * as path from 'path';
import { extractStateName, matchFilenameToPages } from '../../audit/filenameMatching';
import type { ScreenshotEvidenceReport } from './reviewTypes';

const IMAGE_EXTENSIONS = new Set(['.png', '.jpg', '.jpeg', '.webp']);

function safeReadDir(dirPath: string): fs.Dirent[] {
  try {
    return fs.readdirSync(dirPath, { withFileTypes: true });
  } catch {
    return [];
  }
}

function collectFiles(rootPath: string, maxDepth = 4): string[] {
  const files: string[] = [];

  function visit(currentPath: string, depth: number): void {
    if (depth > maxDepth) {
      return;
    }

    for (const entry of safeReadDir(currentPath)) {
      if (entry.name === 'node_modules' || entry.name.startsWith('.git')) {
        continue;
      }

      const fullPath = path.join(currentPath, entry.name);
      if (entry.isDirectory()) {
        visit(fullPath, depth + 1);
      } else {
        files.push(fullPath);
      }
    }
  }

  visit(rootPath, 0);
  return files;
}

export function extractScreenshotEvidence(
  rootPath: string,
  pageNames: string[],
): ScreenshotEvidenceReport {
  const files = collectFiles(rootPath)
    .filter((filePath) => IMAGE_EXTENSIONS.has(path.extname(filePath).toLowerCase()))
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
        filePath: path.relative(rootPath, filePath),
        fileName,
        pageName: match.pageName,
        stateName,
      });
      continue;
    }

    unmatchedCaptures.push({
      filePath: path.relative(rootPath, filePath),
      fileName,
      stateName,
    });
  }

  return {
    captures,
    unmatchedCaptures,
  };
}
