import React, { useEffect, useMemo, useRef, useState } from 'react';
import {
  parseDesignStudioHostMessage,
  withDesignStudioEnvelope,
} from '../../src/design-studio/contracts/designStudioProtocol';
import type { DesignStudioStudioState } from '../../src/design-studio/contracts/designStudioProtocol';
import type {
  DesignStudioApprovalCardViewModel,
  DesignStudioWorkflowStageId,
  DesignStudioWorkflowStageViewModel,
} from '../../src/design-studio/contracts/designStudioShell';

interface VsCodeApi {
  postMessage(message: unknown): void;
}

declare function acquireVsCodeApi(): VsCodeApi;

type ViewState =
  | { kind: 'loading' }
  | { kind: 'error'; message: string }
  | { kind: 'ready'; state: DesignStudioStudioState; selectedStage: DesignStudioWorkflowStageId };

function defaultThreadId(): string {
  return window.__PBIR_DESIGN_STUDIO_BOOTSTRAP__?.threadId ?? 'design-studio:active-report';
}

function approvalStateLabel(value: DesignStudioApprovalCardViewModel['approvalState']): string {
  switch (value) {
    case 'approved':
      return 'Approved';
    case 'pendingApproval':
      return 'Pending approval';
    case 'rejected':
      return 'Rejected';
    default:
      return 'Not submitted';
  }
}

function StageBadge({ stage }: { stage: DesignStudioWorkflowStageViewModel }) {
  return (
    <span className={`stage-badge stage-badge-${stage.status}`}>
      {stage.readinessLabel}
    </span>
  );
}

function ApprovalCard({ card }: { card: DesignStudioApprovalCardViewModel }) {
  return (
    <section className='approval-card' aria-label={card.title}>
      <div className='approval-card-header'>
        <h3>{card.title}</h3>
        <span className='approval-card-state'>{approvalStateLabel(card.approvalState)}</span>
      </div>
      <p><strong>Owner:</strong> {card.owner}</p>
      <p><strong>Unlocks:</strong> {card.unlock}</p>
      <ul>
        {card.nonEffects.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
    </section>
  );
}

function approvalKindsForStage(stageId: DesignStudioWorkflowStageId): DesignStudioApprovalCardViewModel['kind'][] {
  switch (stageId) {
    case 'brief':
    case 'concept':
    case 'draft':
      return ['designApproval'];
    case 'materialize':
      return ['materializationApproval'];
    case 'refinement':
      return ['refinementApproval'];
    case 'compare':
      return ['validationApproval'];
    default:
      return [];
  }
}

export function App() {
  const vscodeApiRef = useRef<VsCodeApi | undefined>(undefined);
  const [viewState, setViewState] = useState<ViewState>({ kind: 'loading' });
  const threadId = defaultThreadId();

  useEffect(() => {
    vscodeApiRef.current = acquireVsCodeApi();
    vscodeApiRef.current.postMessage(withDesignStudioEnvelope({ type: 'webviewReady' }));
    vscodeApiRef.current.postMessage(withDesignStudioEnvelope({ type: 'loadStudioState', threadId }));

    const listener = (event: MessageEvent<unknown>) => {
      const parsed = parseDesignStudioHostMessage(event.data);
      if (!parsed.ok) {
        setViewState({ kind: 'error', message: parsed.error });
        return;
      }

      if (parsed.message.type === 'studioState') {
        const selectedStage = parsed.message.state.workspace?.currentStage ?? 'brief';
        setViewState({ kind: 'ready', state: parsed.message.state, selectedStage });
      }
    };

    window.addEventListener('message', listener);
    return () => window.removeEventListener('message', listener);
  }, [threadId]);

  const workspace = viewState.kind === 'ready' ? viewState.state.workspace : undefined;
  const selectedStage = viewState.kind === 'ready' ? viewState.selectedStage : 'brief';

  const selectedStageModel = useMemo(() => (
    workspace?.stages.find((stage) => stage.id === selectedStage)
  ), [workspace, selectedStage]);
  const visibleApprovalCards = useMemo(() => {
    if (!workspace) {
      return [];
    }

    const allowedKinds = new Set(approvalKindsForStage(selectedStage));
    return workspace.approvalCards.filter((card) => allowedKinds.has(card.kind));
  }, [workspace, selectedStage]);

  if (viewState.kind === 'loading') {
    return <main className='design-studio-shell'><p>Loading Report Design Studio…</p></main>;
  }

  if (viewState.kind === 'error') {
    return <main className='design-studio-shell'><p>{viewState.message}</p></main>;
  }

  if (!workspace) {
    return <main className='design-studio-shell'><p>No Design Studio workspace is available yet.</p></main>;
  }

  return (
    <main className='design-studio-shell'>
      <header className='studio-header'>
        <div>
          <p className='eyebrow'>Report Design Studio</p>
          <h1>Report Design Studio</h1>
          <p>{workspace.reportLabel}</p>
        </div>
        <div className='stage-indicator'>
          <span>Current stage</span>
          <strong>{workspace.currentStageSummary.title}</strong>
        </div>
      </header>

      <div className='studio-layout'>
        <nav className='workflow-rail' aria-label='Workflow stages'>
          {workspace.stages.map((stage) => (
            <button
              key={stage.id}
              type='button'
              className={stage.id === selectedStage ? 'workflow-stage is-active' : 'workflow-stage'}
              onClick={() => setViewState({ kind: 'ready', state: viewState.state, selectedStage: stage.id })}
            >
              <span className='workflow-stage-label'>{stage.label}</span>
              <StageBadge stage={stage} />
            </button>
          ))}
        </nav>

        <section className='stage-canvas'>
          <header className='stage-summary'>
            <div>
              <h2>{selectedStageModel?.title ?? workspace.currentStageSummary.title}</h2>
              <p>{selectedStageModel?.description ?? workspace.currentStageSummary.description}</p>
            </div>
            {selectedStageModel ? <StageBadge stage={selectedStageModel} /> : null}
          </header>

          {visibleApprovalCards.length > 0 ? (
            <section className='approval-grid'>
              {visibleApprovalCards.map((card) => (
                <ApprovalCard key={card.kind} card={card} />
              ))}
            </section>
          ) : null}

          {selectedStage === 'refinement' && workspace.refinementExperience ? (
            <section className='detail-card'>
              <h3>{workspace.refinementExperience.title}</h3>
              <p>{workspace.refinementExperience.summary}</p>
              {workspace.refinementExperience.emptyState ? (
                <p>{workspace.refinementExperience.emptyState}</p>
              ) : null}
              {workspace.refinementExperience.groups.map((group) => (
                <section key={group.id} className='detail-card'>
                  <h4>{group.title}</h4>
                  <p>{group.summary}</p>
                  {group.proposals.map((proposal) => (
                    <article key={proposal.id} className='approval-card' aria-label={proposal.title}>
                      <div className='approval-card-header'>
                        <h5>{proposal.title}</h5>
                        <span className='approval-card-state'>{approvalStateLabel(proposal.approvalState)}</span>
                      </div>
                      <p>{proposal.summary}</p>
                      <p><strong>Recommendation:</strong> {proposal.recommendation}</p>
                      <p><strong>Rationale:</strong> {proposal.rationale}</p>
                      <p><strong>Expected Impact:</strong> {proposal.expectedImpact}</p>
                      <p><strong>Source Analyzer Output:</strong> {proposal.sourceAnalyzerLabel}</p>
                      <p><strong>Affected Design Artifacts:</strong> {proposal.affectedArtifacts.join(', ')}</p>
                      <ul>
                        {proposal.supportingEvidence.map((item) => (
                          <li key={item}>{item}</li>
                        ))}
                      </ul>
                      <section>
                        <h6>Proposal Comparison</h6>
                        <p><strong>Original Design Intent:</strong> {proposal.comparison.originalDesignIntent}</p>
                        <p><strong>Current Design State:</strong> {proposal.comparison.currentDesignState}</p>
                        <p><strong>Proposed Refinement:</strong> {proposal.comparison.proposedRefinement}</p>
                      </section>
                      <div className='workflow-actions'>
                        {proposal.availableActions.includes('approve') ? (
                          <button
                            type='button'
                            onClick={() => {
                              vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                                type: 'setRefinementProposalState',
                                proposalId: proposal.id,
                                action: 'approve',
                              }));
                            }}
                          >
                            Approve Proposal
                          </button>
                        ) : null}
                        {proposal.availableActions.includes('reject') ? (
                          <button
                            type='button'
                            onClick={() => {
                              vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                                type: 'setRefinementProposalState',
                                proposalId: proposal.id,
                                action: 'reject',
                              }));
                            }}
                          >
                            Reject Proposal
                          </button>
                        ) : null}
                        {proposal.availableActions.includes('defer') ? (
                          <button
                            type='button'
                            onClick={() => {
                              vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                                type: 'setRefinementProposalState',
                                proposalId: proposal.id,
                                action: 'defer',
                              }));
                            }}
                          >
                            Defer Proposal
                          </button>
                        ) : null}
                      </div>
                    </article>
                  ))}
                </section>
              ))}
            </section>
          ) : null}

          {selectedStage === 'materialize' && workspace.materializationReadiness ? (
            <section className='detail-card'>
              <h3>Materialization readiness</h3>
              <p>{workspace.materializationReadiness.readinessLabel}</p>
              <dl>
                <div>
                  <dt>Eligibility</dt>
                  <dd>{workspace.materializationReadiness.executableEligibility}</dd>
                </div>
                <div>
                  <dt>Analyzer</dt>
                  <dd>{workspace.materializationReadiness.targetAnalyzer}</dd>
                </div>
                <div>
                  <dt>Profile</dt>
                  <dd>{workspace.materializationReadiness.targetAnalyzerProfile}</dd>
                </div>
              </dl>
              <ul>
                {workspace.materializationReadiness.diagnostics.map((diagnostic) => (
                  <li key={diagnostic}>{diagnostic}</li>
                ))}
              </ul>
            </section>
          ) : null}

          {selectedStage === 'handoff' && workspace.analyzerHandoff ? (
            <section className='detail-card'>
              <h3>Analyzer handoff</h3>
              <p>{workspace.analyzerHandoff.readinessLabel}</p>
              <p>
                Analyzer: <strong>{workspace.analyzerHandoff.analyzerId}</strong>
                {' '}· Profile: <strong>{workspace.analyzerHandoff.analyzerProfileId}</strong>
              </p>
              <ul>
                {workspace.analyzerHandoff.diagnostics.map((diagnostic) => (
                  <li key={diagnostic}>{diagnostic}</li>
                ))}
              </ul>
              <button
                type='button'
                disabled={!workspace.analyzerHandoff.canOpen}
                onClick={() => {
                  vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                    type: 'openAnalyzerHandoff',
                    requestId: workspace.analyzerHandoff!.requestId,
                  }));
                }}
              >
                Open Analyzer Workspace
              </button>
            </section>
          ) : null}
        </section>
      </div>
    </main>
  );
}
