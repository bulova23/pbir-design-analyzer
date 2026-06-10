import type { RepositorySnapshot } from '../../project/repoSnapshot';
import { getDefaultFabricScoringConfig } from '../config/fabricScoringConfig';
import { listSnapshotFiles, readSnapshotText, withRepositorySnapshot } from './repoEvidence';
import type { SemanticModelEvidenceReport } from './reviewTypes';

const FILE_PATTERN = /\.(ts|tsx|js|jsx|json)$/i;
const SIGNAL_PATTERN = /\b(?:semanticModel|dataset|queryRef|measure|metric)\s*:/i;

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

async function extractSemanticModelEvidenceFromSnapshot(
  snapshot: RepositorySnapshot,
): Promise<SemanticModelEvidenceReport> {
  const signals: SemanticModelEvidenceReport['signals'] = [];
  const signalLimit = getDefaultFabricScoringConfig().review.semanticModelSignalLimit;
  const candidateFiles = listSnapshotFiles(
    snapshot,
    (file) => FILE_PATTERN.test(file.relativePath),
  )
    .slice(0, 40)
    .sort((left, right) => left.relativePath.localeCompare(right.relativePath));

  for (const file of candidateFiles) {
    if (signals.length >= signalLimit) {
      break;
    }

    const text = await readSnapshotText(snapshot, file);
    if (!text || !SIGNAL_PATTERN.test(text)) {
      continue;
    }

    for (const line of text.split(/\r?\n/)) {
      if (signals.length >= signalLimit) {
        break;
      }

      const summary = summarizeSemanticSignal(line);
      if (!summary) {
        continue;
      }

      signals.push({
        filePath: file.relativePath,
        summary,
      });
    }
  }

  return { signals };
}

export async function extractSemanticModelEvidence(
  source: RepositorySnapshot | string,
): Promise<SemanticModelEvidenceReport> {
  return withRepositorySnapshot(source, extractSemanticModelEvidenceFromSnapshot);
}
