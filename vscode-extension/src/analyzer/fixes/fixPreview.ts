import type { FixMutation, FixPreviewRow } from '../contracts/scorePanel';

export function buildFixPreviewRows(mutations: FixMutation[]): FixPreviewRow[] {
  return mutations.map((mutation) => ({
    pageName: mutation.pageName,
    objectId: mutation.targetObjectId,
    property: mutation.propertyPath,
    before: mutation.before,
    after: mutation.after,
  }));
}
