import * as fs from 'fs';
import type { FixMutation, RollbackPlan } from '../contracts/scorePanel';
import { captureFixFileVersionSync } from './fixPersistenceService';

export function buildRollbackPlan(
  fixOpportunityId: string,
  mutations: FixMutation[],
): RollbackPlan {
  const targetFiles = [...new Set(mutations.map((mutation) => mutation.targetFile))];
  const fileBackups = targetFiles.map((targetFile) => ({
    targetFile,
    beforeContent: fs.readFileSync(targetFile, 'utf8'),
    beforeVersion: captureFixFileVersionSync(targetFile),
  }));

  return {
    id: `rollback-${fixOpportunityId}`,
    fixOpportunityId,
    fileBackups,
    reverseMutations: mutations.map((mutation) => ({
      ...mutation,
      before: mutation.after,
      after: mutation.before,
    })),
  };
}
