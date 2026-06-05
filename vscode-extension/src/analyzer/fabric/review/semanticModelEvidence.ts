import * as fs from 'fs';
import * as path from 'path';
import type { SemanticModelEvidenceReport } from './reviewTypes';

const SIGNAL_LIMIT = 6;
const FILE_PATTERN = /\.(ts|tsx|js|jsx|json)$/i;
const SIGNAL_PATTERN = /\b(?:semanticModel|dataset|queryRef|measure|metric)\s*:/i;

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
    if (depth > maxDepth || files.length >= 40) {
      return;
    }

    for (const entry of safeReadDir(currentPath)) {
      if (entry.name === 'node_modules' || entry.name.startsWith('.git')) {
        continue;
      }

      const fullPath = path.join(currentPath, entry.name);
      if (entry.isDirectory()) {
        visit(fullPath, depth + 1);
      } else if (FILE_PATTERN.test(entry.name)) {
        files.push(fullPath);
      }
    }
  }

  visit(rootPath, 0);
  return files;
}

function readFileText(filePath: string): string {
  try {
    return fs.readFileSync(filePath, 'utf8');
  } catch {
    return '';
  }
}

function summarizeSemanticSignal(line: string): string | undefined {
  const compact = line.replace(/\s+/g, ' ').trim();
  if (!compact || !SIGNAL_PATTERN.test(compact)) {
    return undefined;
  }

  const modelMatch = compact.match(/(?:semanticModel|dataset)\s*:\s*['"`]([^'"`]+)['"`]/i);
  const measureMatch = compact.match(/(?:measure|metric)\s*:\s*['"`]([^'"`]+)['"`]/i);
  const parts = [
    modelMatch ? `Semantic source ${modelMatch[1]}` : undefined,
    measureMatch ? `measure ${measureMatch[1]}` : undefined,
  ].filter((part): part is string => Boolean(part));

  if (parts.length > 0) {
    return `${parts.join(' with ')} is referenced by the app shell.`;
  }

  return compact.slice(0, 120);
}

export function extractSemanticModelEvidence(rootPath: string): SemanticModelEvidenceReport {
  const signals: SemanticModelEvidenceReport['signals'] = [];

  for (const filePath of collectFiles(rootPath).sort((left, right) => left.localeCompare(right))) {
    if (signals.length >= SIGNAL_LIMIT) {
      break;
    }

    const text = readFileText(filePath);
    if (!text || !SIGNAL_PATTERN.test(text)) {
      continue;
    }

    for (const line of text.split(/\r?\n/)) {
      if (signals.length >= SIGNAL_LIMIT) {
        break;
      }

      const summary = summarizeSemanticSignal(line);
      if (!summary) {
        continue;
      }

      signals.push({
        filePath: path.relative(rootPath, filePath),
        summary,
      });
    }
  }

  return { signals };
}
