import type {
  FixBatchPreview,
  FixBatchPreviewObjectGroup,
  FixBatchPreviewPageGroup,
  FixOpportunity,
} from '../contracts/scorePanel';

function compareText(left: string, right: string): number {
  return left.localeCompare(right);
}

export function buildFixBatchPreview(opportunities: FixOpportunity[]): FixBatchPreview {
  const touchedFiles = [...new Set(opportunities.flatMap((opportunity) => (
    opportunity.mutations.length > 0
      ? opportunity.mutations.map((mutation) => mutation.targetFile)
      : opportunity.rollbackPlan.fileBackups.map((backup) => backup.targetFile)
  )))].sort(compareText);
  const changedObjects = [...new Set(opportunities.flatMap((opportunity) => opportunity.targetObjectIds))].sort(compareText);
  const expectedOutcomes = [...new Set(opportunities.flatMap((opportunity) => opportunity.expectedResolutions))].sort(compareText);
  const mutationFacts = opportunities.flatMap((opportunity) => opportunity.previewRows.map((row) => ({ ...row })));

  const pageGroupsMap = new Map<string, Map<string, FixBatchPreviewObjectGroup>>();
  for (const opportunity of opportunities) {
    for (const row of opportunity.previewRows) {
      const pageName = row.pageName ?? 'Report-wide';
      if (!pageGroupsMap.has(pageName)) {
        pageGroupsMap.set(pageName, new Map<string, FixBatchPreviewObjectGroup>());
      }

      const objectGroups = pageGroupsMap.get(pageName)!;
      if (!objectGroups.has(row.objectId)) {
        objectGroups.set(row.objectId, {
          pageName: row.pageName,
          objectId: row.objectId,
          propertyChanges: [],
        });
      }

      objectGroups.get(row.objectId)!.propertyChanges.push({
        opportunityId: opportunity.id,
        property: row.property,
        before: row.before,
        after: row.after,
      });
    }
  }

  const pageGroups: FixBatchPreviewPageGroup[] = [...pageGroupsMap.entries()]
    .sort(([left], [right]) => compareText(left, right))
    .map(([pageName, objectGroups]) => ({
      pageName,
      objectGroups: [...objectGroups.values()]
        .sort((left, right) => compareText(left.objectId, right.objectId))
        .map((group) => ({
          ...group,
          propertyChanges: group.propertyChanges.sort((left, right) => compareText(left.property, right.property)),
        })),
    }));

  return {
    opportunityIds: opportunities.map((opportunity) => opportunity.id),
    summary: {
      changedFileCount: touchedFiles.length,
      changedObjectCount: changedObjects.length,
      expectedOutcomeCount: expectedOutcomes.length,
      touchedFiles,
      changedObjects,
    },
    pageGroups,
    mutationFacts,
    expectedOutcomes,
  };
}
