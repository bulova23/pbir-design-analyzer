import React, { useState } from 'react';
import type { DesignStudioMaterializationWorkflowViewModel } from '../../../src/design-studio/contracts/designStudioProtocol';

interface LocalPbirMaterializationWorkflowProps {
  workflow: DesignStudioMaterializationWorkflowViewModel;
  onPreview(): void;
  onApply(): void;
  onRecovery(): void;
  onCancel(): void;
}

function outcomeLabel(outcome?: string): string {
  switch (outcome) {
    case 'absent-destination': return 'Destination is absent';
    case 'empty-destination': return 'Destination is empty';
    case 'exact-match': return 'Exact match';
    case 'managed-replacement': return 'Managed replacement available';
    case 'conflict': return 'Conflict detected';
    case 'stale-preview': return 'Preview is stale';
    case 'recovery-required': return 'Recovery required';
    case 'cancelled': return 'Operation cancelled';
    case 'transaction-reused': return 'Transaction was already used';
    case 'invalid-request': return 'Invalid materialization request';
    case 'unsafe-destination': return 'Destination is unsafe';
    case 'schema-failure': return 'PBIR schema validation failed';
    case 'unsupported-operation': return 'Operation is unsupported';
    case 'applied': return 'Applied locally';
    case 'failure': return 'Local materialization failed';
    default: return 'Local PBIR materialization';
  }
}

function isApplyable(workflow: DesignStudioMaterializationWorkflowViewModel): boolean {
  return workflow.status === 'preview-ready'
    && ['absent-destination', 'empty-destination', 'managed-replacement'].includes(workflow.outcome ?? '');
}

export function LocalPbirMaterializationWorkflow({
  workflow,
  onPreview,
  onApply,
  onRecovery,
  onCancel,
}: LocalPbirMaterializationWorkflowProps) {
  const [confirmationOpen, setConfirmationOpen] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const inFlight = ['previewing', 'applying', 'inspecting-recovery'].includes(workflow.status);
  const applyable = isApplyable(workflow) && !submitted;
  const needsFreshPreview = ['conflict', 'stale-preview', 'recovery-required', 'cancelled', 'failure', 'transaction-reused'].includes(workflow.outcome ?? '')
    || workflow.status === 'preview-required';

  const beginApply = () => {
    if (!applyable) return;
    setConfirmationOpen(true);
  };

  const confirmApply = () => {
    if (submitted) return;
    setSubmitted(true);
    onApply();
  };

  const beginPreview = () => {
    setSubmitted(false);
    onPreview();
  };

  return (
    <section className='detail-card local-pbir-materialization' aria-labelledby='local-pbir-materialization-title'>
      <h3 id='local-pbir-materialization-title'>Local PBIR materialization</h3>
      <p>Preview and explicitly apply a local PBIR transaction. This workflow does not deploy or publish a report.</p>

      <div role='status' aria-live='polite' className='local-pbir-status'>
        {workflow.status === 'previewing' ? 'Preparing local PBIR preview' : null}
        {workflow.status === 'applying' ? 'Applying the validated local PBIR preview' : null}
        {workflow.status === 'inspecting-recovery' ? 'Inspecting recovery state (read-only)' : null}
        {workflow.status === 'disconnected' ? 'The backend disconnected. Start a fresh preview after it reconnects.' : null}
        {workflow.outcome ? outcomeLabel(workflow.outcome) : null}
      </div>

      {workflow.summary ? (
        <dl className='local-pbir-summary'>
          <div><dt>Destination</dt><dd>{workflow.summary.destinationClassification}</dd></div>
          <div><dt>Artifacts</dt><dd>{workflow.summary.artifactCount}</dd></div>
          <div><dt>Validated identity</dt><dd>{workflow.summary.identityReference ?? 'Not available'}</dd></div>
          <div><dt>Preview hash</dt><dd>{workflow.summary.previewHash ?? 'Not available'}</dd></div>
          <div><dt>Target state</dt><dd>{workflow.summary.targetStateHash ?? 'Not available'}</dd></div>
          <div><dt>Recovery status</dt><dd>{workflow.summary.activeTransactionRef ? 'Recovery reference available' : 'No recovery reference reported'}</dd></div>
        </dl>
      ) : null}

      {workflow.writtenFiles.length > 0 ? (
        <p>Validated file inventory: {workflow.writtenFiles.length} relative files.</p>
      ) : null}

      {workflow.diagnostics.length > 0 ? (
        <section aria-labelledby='local-pbir-diagnostics-title'>
          <h4 id='local-pbir-diagnostics-title'>Safe diagnostics</h4>
          <ul>
            {workflow.diagnostics.map((diagnostic) => <li key={`${diagnostic.code}:${diagnostic.field}`}>{diagnostic.message}</li>)}
          </ul>
        </section>
      ) : null}

      {needsFreshPreview ? <p>Start a fresh preview before applying again.</p> : null}

      <div className='workflow-actions'>
        <button type='button' onClick={beginPreview} disabled={inFlight}>
          Start read-only preview
        </button>
        <button type='button' onClick={beginApply} disabled={!applyable || inFlight}>
          Apply this preview
        </button>
        <button type='button' onClick={onRecovery} disabled={inFlight}>
          Inspect recovery (read-only)
        </button>
        {inFlight ? (
          <button type='button' onClick={onCancel} aria-label='Cancel local PBIR operation'>
            Cancel
          </button>
        ) : null}
      </div>

      {confirmationOpen ? (
        <div role='dialog' aria-modal='true' aria-labelledby='local-pbir-confirm-title' className='local-pbir-confirmation'>
          <h4 id='local-pbir-confirm-title'>Confirm local PBIR apply</h4>
          <p>Apply only the exact validated preview shown above? A new transaction ID will be used.</p>
          {submitted ? <p role='status'>Apply submitted. Waiting for the typed outcome.</p> : null}
          <button type='button' onClick={confirmApply} disabled={submitted}>
            Confirm local PBIR apply
          </button>
          <button type='button' onClick={() => setConfirmationOpen(false)} disabled={submitted}>
            Cancel apply
          </button>
        </div>
      ) : null}
    </section>
  );
}
