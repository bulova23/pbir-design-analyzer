import * as vscode from 'vscode';
import type { FixApplySessionRecord, FixOpportunity, ScoreResult } from '../analyzer/contracts/scorePanel';
import { applyFixOpportunity, applyFixOpportunityBatch, rollbackFixOpportunity, rollbackFixSession } from '../analyzer/fixes/fixApplyEngine';
import { evaluateFixOpportunityCompatibility } from '../analyzer/fixes/fixCompatibility';
import { createFixApplySessionRecord, markFixSessionRegenerated, recordFixSessionRollback } from '../analyzer/fixes/fixSessionHistory';
import { evaluateFixOutcome, summarizeBatchFixOutcomes } from '../analyzer/fixes/fixOutcomeEvaluator';

type FixDeps = {
  getCurrentResult: () => ScoreResult | undefined;
  getFixOpportunityHistory: () => Map<string, FixOpportunity>;
  getSelectedFixOpportunityIds: () => string[];
  setSelectedFixOpportunityIds: (ids: string[]) => void;
  getFixSelectionApprovalState: () => 'NeedsPreview' | 'Previewed' | 'Approved';
  setFixSelectionApprovalState: (state: 'NeedsPreview' | 'Previewed' | 'Approved') => void;
  getFixApplySessions: () => FixApplySessionRecord[];
  setFixApplySessions: (sessions: FixApplySessionRecord[]) => void;
  getFixWorkflowMessage: () => string | undefined;
  setFixWorkflowMessage: (message: string | undefined) => void;
  refresh: () => Promise<void>;
  postCurrentScoreState: () => Promise<void>;
  showWarningMessage?: typeof vscode.window.showWarningMessage;
  evaluateFixOpportunityCompatibility?: typeof evaluateFixOpportunityCompatibility;
  applyFixOpportunityBatch?: typeof applyFixOpportunityBatch;
  applyFixOpportunity?: typeof applyFixOpportunity;
  rollbackFixOpportunity?: typeof rollbackFixOpportunity;
  rollbackFixSession?: typeof rollbackFixSession;
  evaluateFixOutcome?: typeof evaluateFixOutcome;
  summarizeBatchFixOutcomes?: typeof summarizeBatchFixOutcomes;
  createFixApplySessionRecord?: typeof createFixApplySessionRecord;
  recordFixSessionRollback?: typeof recordFixSessionRollback;
  markFixSessionRegenerated?: typeof markFixSessionRegenerated;
};

export function createScorePanelFixWorkflowService(deps: FixDeps) {
  const showWarningMessage = deps.showWarningMessage ?? vscode.window.showWarningMessage;
  const evaluateFixOpportunityCompatibilityImpl = deps.evaluateFixOpportunityCompatibility ?? evaluateFixOpportunityCompatibility;
  const applyFixOpportunityBatchImpl = deps.applyFixOpportunityBatch ?? applyFixOpportunityBatch;
  const applyFixOpportunityImpl = deps.applyFixOpportunity ?? applyFixOpportunity;
  const rollbackFixOpportunityImpl = deps.rollbackFixOpportunity ?? rollbackFixOpportunity;
  const rollbackFixSessionImpl = deps.rollbackFixSession ?? rollbackFixSession;
  const evaluateFixOutcomeImpl = deps.evaluateFixOutcome ?? evaluateFixOutcome;
  const summarizeBatchFixOutcomesImpl = deps.summarizeBatchFixOutcomes ?? summarizeBatchFixOutcomes;
  const createFixApplySessionRecordImpl = deps.createFixApplySessionRecord ?? createFixApplySessionRecord;
  const recordFixSessionRollbackImpl = deps.recordFixSessionRollback ?? recordFixSessionRollback;
  const markFixSessionRegeneratedImpl = deps.markFixSessionRegenerated ?? markFixSessionRegenerated;

  function currentPreviewableOpportunities(): FixOpportunity[] {
    return (deps.getCurrentResult()?.fixOpportunities ?? []).filter((item) => item.state !== 'Applied' && item.state !== 'RolledBack');
  }

  function selectedFixOpportunities(): FixOpportunity[] {
    const selectedSet = new Set(deps.getSelectedFixOpportunityIds());
    return currentPreviewableOpportunities().filter((item) => selectedSet.has(item.id));
  }

  function findFixOpportunity(opportunityId: string): FixOpportunity | undefined {
    return deps.getCurrentResult()?.fixOpportunities?.find((item) => item.id === opportunityId)
      ?? deps.getFixOpportunityHistory().get(opportunityId);
  }

  return {
    findFixOpportunity,
    currentPreviewableOpportunities,
    selectedFixOpportunities,
    async toggleFixOpportunitySelection(opportunityId: string): Promise<void> {
      const opportunity = findFixOpportunity(opportunityId);
      if (!opportunity || opportunity.state === 'Applied' || opportunity.state === 'RolledBack') {
        return;
      }

      const selectedIds = deps.getSelectedFixOpportunityIds();
      deps.setSelectedFixOpportunityIds(
        selectedIds.includes(opportunityId)
          ? selectedIds.filter((id) => id !== opportunityId)
          : [...selectedIds, opportunityId],
      );
      deps.setFixSelectionApprovalState('NeedsPreview');
      deps.setFixWorkflowMessage(undefined);
      await deps.postCurrentScoreState();
    },
    async previewSelectedFixOpportunities(): Promise<void> {
      const selected = selectedFixOpportunities();
      if (selected.length === 0) {
        deps.setFixWorkflowMessage('Select one or more opportunities before previewing fixes.');
        await deps.postCurrentScoreState();
        return;
      }

      const compatibility = evaluateFixOpportunityCompatibilityImpl(selected);
      if (!compatibility.isCompatible) {
        deps.setFixSelectionApprovalState('NeedsPreview');
        deps.setFixWorkflowMessage('Selected opportunities are incompatible or stale. Resolve the blocked items before previewing.');
        await deps.postCurrentScoreState();
        return;
      }

      deps.setFixSelectionApprovalState('Previewed');
      deps.setFixWorkflowMessage(undefined);
      await deps.postCurrentScoreState();
    },
    async approveSelectedFixOpportunities(): Promise<void> {
      const selected = selectedFixOpportunities();
      const compatibility = evaluateFixOpportunityCompatibilityImpl(selected);
      if (selected.length === 0 || !compatibility.isCompatible || deps.getFixSelectionApprovalState() !== 'Previewed') {
        return;
      }

      deps.setFixSelectionApprovalState('Approved');
      await deps.postCurrentScoreState();
    },
    async applySelectedFixOpportunities(): Promise<void> {
      const selected = selectedFixOpportunities();
      const previousResult = deps.getCurrentResult();
      if (selected.length === 0 || !previousResult || deps.getFixSelectionApprovalState() !== 'Approved') {
        return;
      }

      const applyResult = await applyFixOpportunityBatchImpl(selected);
      if (applyResult.state !== 'Applied') {
        for (const opportunity of selected) {
          deps.getFixOpportunityHistory().set(opportunity.id, {
            ...opportunity,
            state: applyResult.state,
          });
        }

        deps.setFixWorkflowMessage(
          applyResult.state === 'Stale'
            ? 'Selected opportunities are stale or drifted. Regenerate them before retrying.'
            : 'Selected opportunities cannot be applied together.',
        );
        deps.setFixSelectionApprovalState('NeedsPreview');
        await deps.postCurrentScoreState();
        return;
      }

      await deps.refresh();
      const refreshedResult = deps.getCurrentResult();
      if (!refreshedResult) {
        return;
      }

      const outcomeItems = selected.map((opportunity) => {
        const outcome = evaluateFixOutcomeImpl(opportunity, previousResult, refreshedResult);
        deps.getFixOpportunityHistory().set(opportunity.id, {
          ...opportunity,
          state: outcome.nextState,
          outcome: outcome.outcome,
        });
        return {
          opportunityId: opportunity.id,
          title: opportunity.title,
          state: outcome.nextState,
          outcome: outcome.outcome,
        };
      });

      const session = createFixApplySessionRecordImpl({
        appliedAt: applyResult.session?.appliedAt ?? new Date().toISOString(),
        opportunities: selected.map((opportunity) => ({
          id: opportunity.id,
          title: opportunity.title,
          state: deps.getFixOpportunityHistory().get(opportunity.id)?.state ?? 'Applied',
        })),
        rollbackAvailable: applyResult.session?.rollbackAvailable ?? false,
        groupedOutcomeSummary: summarizeBatchFixOutcomesImpl(outcomeItems),
      });

      deps.setFixApplySessions([
        {
          ...session,
          id: applyResult.session?.id ?? session.id,
        },
        ...deps.getFixApplySessions(),
      ]);
      deps.setSelectedFixOpportunityIds([]);
      deps.setFixSelectionApprovalState('NeedsPreview');
      deps.setFixWorkflowMessage(undefined);
      await deps.postCurrentScoreState();
    },
    async rollbackFixSession(sessionId: string): Promise<void> {
      const session = deps.getFixApplySessions().find((item) => item.id === sessionId);
      if (!session) {
        return;
      }

      const opportunities = session.opportunityIds
        .map((id) => deps.getFixOpportunityHistory().get(id))
        .filter((item): item is FixOpportunity => Boolean(item));
      const rollback = await rollbackFixSessionImpl(session, opportunities);

      deps.setFixApplySessions(
        deps.getFixApplySessions().map((item) => item.id === sessionId
          ? recordFixSessionRollbackImpl(item, rollback.rollbackHistory[rollback.rollbackHistory.length - 1])
          : item),
      );

      if (rollback.state === 'RolledBack') {
        for (const opportunity of opportunities) {
          deps.getFixOpportunityHistory().set(opportunity.id, {
            ...opportunity,
            state: 'RolledBack',
            outcome: undefined,
          });
        }
        deps.setFixWorkflowMessage(undefined);
      } else {
        deps.setFixWorkflowMessage(
          rollback.rollbackHistory[rollback.rollbackHistory.length - 1]?.validationErrors?.[0]
            ?? 'Rollback could not be completed safely because the target files changed after apply.',
        );
      }

      await deps.refresh();
      await deps.postCurrentScoreState();
    },
    async regenerateFixOpportunities(opportunityIds?: string[]): Promise<void> {
      const currentResult = deps.getCurrentResult();
      const selected = currentResult?.fixOpportunities ?? [];
      const staleIds = opportunityIds
        ?? evaluateFixOpportunityCompatibilityImpl(selected).blockingReasons
          .filter((reason) => reason.code === 'staleOpportunity' || reason.code === 'targetDrifted')
          .flatMap((reason) => reason.opportunityIds);

      const staleSet = new Set(staleIds);
      await deps.refresh();
      const regeneratedOpportunityIds = (deps.getCurrentResult()?.fixOpportunities ?? [])
        .filter((item) => staleSet.has(item.id) || staleSet.has(item.remediationItemId))
        .map((item) => item.id);

      const sessions = deps.getFixApplySessions();
      if (sessions[0]) {
        deps.setFixApplySessions([
          markFixSessionRegeneratedImpl(sessions[0], {
            staleOpportunityIds: staleIds,
            regeneratedOpportunityIds,
          }),
          ...sessions.slice(1),
        ]);
      }

      deps.setSelectedFixOpportunityIds(regeneratedOpportunityIds);
      deps.setFixSelectionApprovalState('NeedsPreview');
      deps.setFixWorkflowMessage(
        staleIds.length > 0
          ? `Regenerated ${regeneratedOpportunityIds.length} opportunity${regeneratedOpportunityIds.length === 1 ? '' : 'ies'} from stale selections.`
          : 'Fix opportunities regenerated from the latest score state.',
      );
      await deps.postCurrentScoreState();
    },
    async approveFixOpportunity(opportunityId: string): Promise<void> {
      const opportunity = findFixOpportunity(opportunityId);
      if (!opportunity) {
        void showWarningMessage(`Fix opportunity '${opportunityId}' is no longer available.`);
        return;
      }

      deps.getFixOpportunityHistory().set(opportunityId, {
        ...opportunity,
        state: 'Approved',
      });
      await deps.postCurrentScoreState();
    },
    async applyFixOpportunity(opportunityId: string): Promise<void> {
      const opportunity = findFixOpportunity(opportunityId);
      const previousResult = deps.getCurrentResult();
      if (!opportunity || !previousResult) {
        void showWarningMessage(`Fix opportunity '${opportunityId}' is no longer available.`);
        return;
      }

      const applyResult = await applyFixOpportunityImpl(opportunity);
      deps.getFixOpportunityHistory().set(opportunityId, {
        ...opportunity,
        state: applyResult.state,
      });

      if (applyResult.state !== 'Applied') {
        await deps.postCurrentScoreState();
        return;
      }

      await deps.refresh();
      const refreshedResult = deps.getCurrentResult();
      if (!refreshedResult) {
        return;
      }

      const outcome = evaluateFixOutcomeImpl(opportunity, previousResult, refreshedResult);
      deps.getFixOpportunityHistory().set(opportunityId, {
        ...opportunity,
        state: outcome.nextState,
        outcome: outcome.outcome,
      });
      await deps.postCurrentScoreState();
    },
    async rollbackFixOpportunity(opportunityId: string): Promise<void> {
      const opportunity = findFixOpportunity(opportunityId);
      if (!opportunity) {
        void showWarningMessage(`Fix opportunity '${opportunityId}' is no longer available for rollback.`);
        return;
      }

      const rollbackResult = await rollbackFixOpportunityImpl(opportunity);
      deps.getFixOpportunityHistory().set(opportunityId, {
        ...opportunity,
        state: rollbackResult.state,
        outcome: undefined,
      });
      deps.setFixWorkflowMessage(
        rollbackResult.state === 'RolledBack'
          ? undefined
          : rollbackResult.validationErrors[0] ?? 'Rollback could not be completed safely.',
      );
      await deps.refresh();
      await deps.postCurrentScoreState();
    },
  };
}
