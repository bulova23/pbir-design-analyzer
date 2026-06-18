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
import { getRecommendationState, type RecommendationState } from '../../src/design-studio/contracts/designStudioModels';
import { ClosedLoopView } from './views/ClosedLoopView';
import { ConceptStudioView } from './views/ConceptStudioView';
import { DesignBriefView } from './views/DesignBriefView';
import { DraftStudioView } from './views/DraftStudioView';
import {
  createInitialDesignBriefState,
  designBriefReducer,
  toDesignBriefDraftInput,
  type DesignBriefEditorAction,
  type DesignBriefEditorState,
} from './state/designBriefReducer';

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

function recommendationStateLabel(value: RecommendationState): string {
  switch (value) {
    case 'approved':
      return 'Approved';
    case 'rejected':
      return 'Rejected';
    case 'deferred':
      return 'Deferred';
    default:
      return 'Outstanding';
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
  const stateLabel = card.kind === 'validationApproval' && card.approvalState === 'approved'
    ? 'Validated'
    : approvalStateLabel(card.approvalState);

  return (
    <section className='approval-card' aria-label={card.title}>
      <div className='approval-card-header'>
        <h3>{card.title}</h3>
        <span className='approval-card-state'>{stateLabel}</span>
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

function ApprovalTeachingCard() {
  return (
    <section className='detail-card'>
      <h3>Approval stages</h3>
      <div className='approval-teaching-grid'>
        <article className='detail-card'>
          <h4>Ready</h4>
          <p>Owner: Design Studio workflow state</p>
          <p>Ready means the stage can move into review.</p>
          <p>Effect: the consultant can inspect the design without approving it yet.</p>
        </article>
        <article className='detail-card'>
          <h4>Approved</h4>
          <p>Owner: Design Studio</p>
          <p>Approved means Design Studio accepted the current design baseline.</p>
          <p>Effect: the next stage can use that baseline explicitly.</p>
        </article>
        <article className='detail-card'>
          <h4>Validated</h4>
          <p>Owner: Analyzer Workspace</p>
          <p>Validated means Analyzer Workspace recorded the review outcome.</p>
          <p>Effect: the current iteration has analyzer-owned validation evidence.</p>
        </article>
      </div>
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
    case 'completion':
      return [];
    default:
      return [];
  }
}

function buildConceptStudioState(
  workspace: NonNullable<DesignStudioStudioState['workspace']>,
  currentBrief: DesignStudioStudioState['currentBrief'],
) {
  const conceptReview = workspace.conceptReview;
  const briefApprovalState = currentBrief?.approvalState ?? 'notSubmitted';

  return {
    briefApprovalState,
    canGenerateConcepts: briefApprovalState === 'approved',
    conceptId: conceptReview?.conceptId,
    approvalState: conceptReview?.approvalState ?? 'notSubmitted',
    alternateConcepts: conceptReview?.alternateConcepts ?? [],
    preferredBaselineConceptId: conceptReview?.preferredBaselineConceptId,
    approvedBaselineConceptId: conceptReview?.approvedBaselineConceptId,
    comparison: conceptReview?.comparison,
  };
}

export function App() {
  const vscodeApiRef = useRef<VsCodeApi | undefined>(undefined);
  const [viewState, setViewState] = useState<ViewState>({ kind: 'loading' });
  const [briefEditorState, setBriefEditorState] = useState<DesignBriefEditorState>(() => createInitialDesignBriefState());
  const threadId = defaultThreadId();

  const dispatchBriefAction = (action: DesignBriefEditorAction) => {
    setBriefEditorState((previous) => designBriefReducer(previous, action));
  };

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
        const studioState = parsed.message.state;
        const fallbackSelectedStage = studioState.workspace?.currentStage ?? 'brief';
        setViewState((previous) => {
          const previousSelection = previous.kind === 'ready' ? previous.selectedStage : fallbackSelectedStage;
          const nextSelection = studioState.workspace?.stages.some((stage) => stage.id === previousSelection)
            ? previousSelection
            : fallbackSelectedStage;

          return { kind: 'ready', state: studioState, selectedStage: nextSelection };
        });
      }
    };

    window.addEventListener('message', listener);
    return () => window.removeEventListener('message', listener);
  }, [threadId]);

  const workspace = viewState.kind === 'ready' ? viewState.state.workspace : undefined;
  const selectedStage = viewState.kind === 'ready' ? viewState.selectedStage : 'brief';
  const currentBrief = viewState.kind === 'ready' ? viewState.state.currentBrief : undefined;

  const selectedStageModel = useMemo(() => (
    workspace?.stages.find((stage) => stage.id === selectedStage)
  ), [workspace, selectedStage]);
  const conceptStudioState = useMemo(() => (
    workspace ? buildConceptStudioState(workspace, currentBrief) : undefined
  ), [workspace, currentBrief]);
  const visibleApprovalCards = useMemo(() => {
    if (!workspace) {
      return [];
    }

    const allowedKinds = new Set(approvalKindsForStage(selectedStage));
    return workspace.approvalCards.filter((card) => allowedKinds.has(card.kind));
  }, [workspace, selectedStage]);

  useEffect(() => {
    if (viewState.kind !== 'ready') {
      return;
    }

    setBriefEditorState(createInitialDesignBriefState(viewState.state.currentBrief));
  }, [viewState.kind === 'ready' ? `${viewState.state.currentBrief?.id ?? 'none'}:${viewState.state.currentBrief?.version ?? 0}:${viewState.state.currentBrief?.updatedAt ?? 'none'}` : 'loading']);

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
          <strong>{selectedStageModel?.title ?? workspace.currentStageSummary.title}</strong>
        </div>
      </header>

      <div className='studio-layout'>
        <nav className='workflow-rail' aria-label='Workflow stages'>
          {workspace.stages.map((stage) => (
            <button
              key={stage.id}
              type='button'
              className={stage.id === selectedStage ? 'workflow-stage is-active' : 'workflow-stage'}
              disabled={stage.status === 'blocked'}
              aria-disabled={stage.status === 'blocked' ? 'true' : undefined}
              onClick={() => {
                if (stage.status === 'blocked') {
                  return;
                }

                setViewState({ kind: 'ready', state: viewState.state, selectedStage: stage.id });
              }}
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

          <ApprovalTeachingCard />

          {selectedStage === 'brief' ? (
            <DesignBriefView
              state={briefEditorState}
              dispatch={dispatchBriefAction}
              onSave={() => {
                vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                  type: 'saveArtifact',
                  artifactKind: 'designBrief',
                  artifact: toDesignBriefDraftInput(briefEditorState),
                }));
              }}
              onSubmitForApproval={() => {
                vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                  type: 'proposeArtifact',
                  artifactKind: 'designBrief',
                  artifactId: currentBrief?.id ?? `design-brief:${threadId}`,
                }));
              }}
              onApprove={() => {
                vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                  type: 'approveArtifact',
                  artifactKind: 'designBrief',
                  artifactId: currentBrief?.id ?? `design-brief:${threadId}`,
                }));
              }}
            />
          ) : null}

          {selectedStage === 'refinement' && workspace.refinementExperience ? (
            <section className='detail-card'>
              <h3>{workspace.refinementExperience.title}</h3>
              <p>{workspace.refinementExperience.summary}</p>
              {workspace.refinementExperience.emptyState ? (
                <p>{workspace.refinementExperience.emptyState}</p>
              ) : null}
              {workspace.refinementExperience.groups.length > 0 ? (
                <section className='detail-card'>
                  <h4>Recommendation Outcomes</h4>
                  <div className='summary-metric-grid'>
                    {(['approved', 'rejected', 'deferred', 'proposed'] as const).map((state) => {
                      const count = workspace.refinementExperience!.groups
                        .flatMap((group) => group.proposals)
                        .filter((proposal) => getRecommendationState(proposal) === state)
                        .length;

                      return (
                        <article key={state} className='summary-metric-card'>
                          <p>{recommendationStateLabel(state)}</p>
                          <strong>{count}</strong>
                        </article>
                      );
                    })}
                  </div>
                </section>
              ) : null}
              {workspace.refinementExperience.groups.map((group) => (
                <section key={group.id} className='detail-card'>
                  <h4>{group.title}</h4>
                  <p>{group.summary}</p>
                  {group.proposals.map((proposal) => (
                    <article key={proposal.id} className='approval-card' aria-label={proposal.title}>
                      <div className='approval-card-header'>
                        <h5>{proposal.title}</h5>
                        <span className='approval-card-state'>
                          {recommendationStateLabel(getRecommendationState(proposal))}
                        </span>
                      </div>
                      <p>{proposal.summary}</p>
                      <p><strong>Recommendation:</strong> {proposal.recommendation}</p>
                      <p><strong>Rationale:</strong> {proposal.rationale}</p>
                      <p><strong>Why this matters:</strong> {proposal.expectedImpact}</p>
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

          {selectedStage === 'concept' && conceptStudioState ? (
            <ConceptStudioView
              state={conceptStudioState}
              dispatch={() => undefined}
              onGenerateConcepts={() => {
                vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                  type: 'generateConcepts',
                }));
              }}
              onSelectBaseline={(conceptId) => {
                vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                  type: 'selectConceptBaseline',
                  conceptId,
                }));
              }}
              onSubmitBaselineForApproval={() => {
                vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                  type: 'proposeArtifact',
                  artifactKind: 'reportConcept',
                  artifactId: workspace.conceptReview?.conceptId ?? `report-concept:${threadId}`,
                }));
              }}
              onApproveBaseline={() => {
                vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                  type: 'approveArtifact',
                  artifactKind: 'reportConcept',
                  artifactId: workspace.conceptReview?.conceptId ?? `report-concept:${threadId}`,
                }));
              }}
            />
          ) : null}

          {selectedStage === 'draft' ? (
            <DraftStudioView
              canGenerateDrafts={selectedStageModel?.status !== 'blocked'}
              draftReview={workspace.draftReview}
              onGenerateDrafts={() => {
                vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                  type: 'generateDrafts',
                }));
              }}
              onSubmitDraftForApproval={() => {
                vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                  type: 'proposeArtifact',
                  artifactKind: 'draftReportArtifact',
                  artifactId: workspace.draftReview?.draftId ?? `draft-report:${threadId}`,
                }));
              }}
              onApproveDraft={() => {
                vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                  type: 'approveArtifact',
                  artifactKind: 'draftReportArtifact',
                  artifactId: workspace.draftReview?.draftId ?? `draft-report:${threadId}`,
                }));
              }}
            />
          ) : null}

          {selectedStage === 'materialize' && workspace.materializationReadiness ? (
            <section className='detail-card'>
              <h3>Prepare Design For Review</h3>
              <p>{workspace.materializationReadiness.nextStepGuidance ?? workspace.materializationReadiness.readinessLabel}</p>
              <section>
                <h4>Review Candidate Status</h4>
                <p>{workspace.materializationReadiness.candidateStatusLabel ?? workspace.materializationReadiness.readinessLabel}</p>
              </section>
              <section>
                <h4>Candidate Summary</h4>
                <dl>
                  <div>
                    <dt>Source draft</dt>
                    <dd>{workspace.materializationReadiness.sourceDraftVersionId ?? 'Not available yet'}</dd>
                  </div>
                  <div>
                    <dt>Source concept</dt>
                    <dd>{workspace.materializationReadiness.sourceConceptVersionId ?? 'Not available yet'}</dd>
                  </div>
                  <div>
                    <dt>Source design brief</dt>
                    <dd>{workspace.materializationReadiness.sourceDesignBriefVersionId ?? 'Not available yet'}</dd>
                  </div>
                </dl>
              </section>
              <section>
                <h4>Review Readiness</h4>
                <p>{workspace.materializationReadiness.readinessLabel}</p>
              </section>
              <dl>
                <div>
                  <dt>Materialization status</dt>
                  <dd>{workspace.materializationReadiness.materializationStatus ?? workspace.materializationReadiness.executableEligibility}</dd>
                </div>
                <div>
                  <dt>Review destination</dt>
                  <dd>{workspace.materializationReadiness.targetAnalyzer}</dd>
                </div>
                <div>
                  <dt>Review profile</dt>
                  <dd>{workspace.materializationReadiness.targetAnalyzerProfile}</dd>
                </div>
              </dl>
              {workspace.materializationReadiness.lineage?.length ? (
                <section>
                  <h4>Review Lineage</h4>
                  <ul>
                    {workspace.materializationReadiness.lineage.map((entry) => (
                      <li key={`${entry.label}:${entry.artifactVersionId}`}>
                        {entry.label}: {entry.artifactVersionId} ({approvalStateLabel(entry.approvalState)})
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}
              {workspace.materializationReadiness.approvalsUsed?.length ? (
                <section>
                  <h4>Approvals Used</h4>
                  <ul>
                    {workspace.materializationReadiness.approvalsUsed.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                </section>
              ) : null}
              <section>
                <h4>Review Diagnostics</h4>
                <ul>
                  {workspace.materializationReadiness.diagnostics.map((diagnostic) => (
                    <li key={diagnostic}>{diagnostic}</li>
                  ))}
                </ul>
              </section>
              <div className='workflow-actions'>
                {workspace.materializationReadiness.canCreateCandidate ? (
                  <button
                    type='button'
                    onClick={() => {
                      vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                        type: 'createReviewCandidate',
                      }));
                    }}
                  >
                    Create Review Candidate
                  </button>
                ) : null}
                {workspace.materializationReadiness.canSubmitCandidateForApproval ? (
                  <button
                    type='button'
                    onClick={() => {
                      vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                        type: 'proposeArtifact',
                        artifactKind: 'materializedSurfaceCandidate',
                        artifactId: workspace.materializationReadiness?.candidateId ?? `materialized-surface-candidate:${threadId}`,
                      }));
                    }}
                  >
                    Submit Candidate For Approval
                  </button>
                ) : null}
                {workspace.materializationReadiness.canApproveCandidate ? (
                  <button
                    type='button'
                    onClick={() => {
                      vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                        type: 'approveArtifact',
                        artifactKind: 'materializedSurfaceCandidate',
                        artifactId: workspace.materializationReadiness?.candidateId ?? `materialized-surface-candidate:${threadId}`,
                      }));
                    }}
                  >
                    Approve Candidate
                  </button>
                ) : null}
              </div>
            </section>
          ) : null}

          {selectedStage === 'handoff' && workspace.reviewDesign ? (
            <section className='detail-card'>
              <h3>Review Design</h3>
              <p>{workspace.reviewDesign.nextStepGuidance}</p>
              <section>
                <h4>Candidate Summary</h4>
                <dl>
                  <div>
                    <dt>Source design brief</dt>
                    <dd>{workspace.reviewDesign.sourceDesignBriefVersionId ?? 'Not available yet'}</dd>
                  </div>
                  <div>
                    <dt>Source concept</dt>
                    <dd>{workspace.reviewDesign.sourceConceptVersionId ?? 'Not available yet'}</dd>
                  </div>
                  <div>
                    <dt>Source draft</dt>
                    <dd>{workspace.reviewDesign.sourceDraftVersionId ?? 'Not available yet'}</dd>
                  </div>
                  <div>
                    <dt>Approved review candidate</dt>
                    <dd>{workspace.reviewDesign.approvedReviewCandidateVersionId ?? 'Not available yet'}</dd>
                  </div>
                </dl>
              </section>
              <section>
                <h4>Review Readiness</h4>
                <p>{workspace.reviewDesign.reviewReadinessLabel}</p>
              </section>
              <section>
                <h4>Handoff Status</h4>
                <p>{workspace.reviewDesign.handoffStatusLabel}</p>
                <p>
                  Analyzer: <strong>{workspace.reviewDesign.analyzerId}</strong>
                  {' '}· Profile: <strong>{workspace.reviewDesign.analyzerProfileId}</strong>
                </p>
              </section>
              <section>
                <h4>Review Status</h4>
                <p>{workspace.reviewDesign.reviewStatusLabel}</p>
                <p>{workspace.reviewDesign.completionStatusLabel}</p>
              </section>
              <section>
                <h4>Result Attachment</h4>
                <p>{workspace.reviewDesign.resultStatusLabel ?? 'No analyzer results are attached yet.'}</p>
              </section>
              <section>
                <h4>Analyzer Ownership</h4>
                <ul>
                  {workspace.reviewDesign.ownershipMessages.map((message) => (
                    <li key={message}>{message}</li>
                  ))}
                </ul>
              </section>
              {(workspace.reviewDesign.availableResults?.length ?? 0) > 0 ? (
                <section>
                  <h4>Available Analyzer Results</h4>
                  <ul>
                    {(workspace.reviewDesign.availableResults ?? []).map((result) => (
                      <li key={`${result.analyzerRunId}:${result.resultReference}`}>
                        <strong>{result.analyzerSourceLabel}</strong>
                        {' '}· {result.resultReference}
                        {' '}· {result.analyzerRunId}
                        {' '}· Validation status: {result.validationResultStatusLabel}
                        {' '}· Validation approval: {result.validationApprovalStateLabel}
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}
              <section>
                <h4>Review Diagnostics</h4>
                <ul>
                  {workspace.reviewDesign.readinessDiagnostics.map((diagnostic) => (
                    <li key={diagnostic}>{diagnostic}</li>
                  ))}
                </ul>
              </section>
              <div className='workflow-actions'>
                <button
                  type='button'
                  disabled={!workspace.reviewDesign.canOpenAnalyzerWorkspace}
                  onClick={() => {
                    vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                      type: 'openAnalyzerHandoff',
                      requestId: workspace.reviewDesign!.requestId,
                    }));
                  }}
                >
                  Open Analyzer Workspace
                </button>
                {workspace.reviewDesign.canMarkReviewCompleted ? (
                  <button
                    type='button'
                    onClick={() => {
                      vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                        type: 'markReviewCompleted',
                        requestId: workspace.reviewDesign!.requestId,
                      }));
                    }}
                  >
                    Mark Review Completed
                  </button>
                ) : null}
                {workspace.reviewDesign.canAttachAnalyzerResults ? (
                  <button
                    type='button'
                    onClick={() => {
                      vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                        type: 'attachAnalyzerResults',
                        requestId: workspace.reviewDesign!.requestId,
                      }));
                    }}
                  >
                    Attach Analyzer Results
                  </button>
                ) : null}
              </div>
            </section>
          ) : null}

          {selectedStage === 'compare' ? (
            <section className='detail-card'>
              <ClosedLoopView iterations={viewState.state.iterationHistory} />
            </section>
          ) : null}

          {selectedStage === 'completion' && workspace.workflowCompletion ? (
            <section className='detail-card'>
              <h3>Workflow Completion</h3>
              <p>{workspace.workflowCompletion.nextStepGuidance}</p>
              <p>Completion is a workflow state. It does not grant validation approval, deployment approval, or report publication authority.</p>
              <section>
                <h4>Completion Checklist</h4>
                <ul>
                  {workspace.workflowCompletion.checklist.map((item) => (
                    <li key={item.id}>
                      {item.label}: {item.satisfied ? 'Satisfied' : 'Incomplete'}
                    </li>
                  ))}
                </ul>
              </section>
              <section>
                <h4>Outstanding Items</h4>
                {workspace.workflowCompletion.outstandingItems.length > 0 ? (
                  <ul>
                    {workspace.workflowCompletion.outstandingItems.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                ) : (
                  <p>No blocking workflow items remain.</p>
                )}
              </section>
              <section>
                <h4>Completed Approvals</h4>
                {workspace.workflowCompletion.approvalsSatisfied.length > 0 ? (
                  <ul>
                    {workspace.workflowCompletion.approvalsSatisfied.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                ) : (
                  <p>No approvals are satisfied yet.</p>
                )}
              </section>
              <section>
                <h4>Recommendation Summary</h4>
                <p>Deferred recommendations: {workspace.workflowCompletion.deferredRecommendationCount}</p>
                <p>Unresolved recommendations: {workspace.workflowCompletion.unresolvedRecommendationCount}</p>
              </section>
              {workspace.workflowCompletion.completedAt ? (
                <section>
                  <h4>Completion Audit</h4>
                  <p>Completed by {workspace.workflowCompletion.completedBy ?? 'Unknown'} on {workspace.workflowCompletion.completedAt}</p>
                  {workspace.workflowCompletion.reopenedAt ? (
                    <p>Reopened by {workspace.workflowCompletion.reopenedBy ?? 'Unknown'} on {workspace.workflowCompletion.reopenedAt}</p>
                  ) : null}
                </section>
              ) : null}
              <div className='workflow-actions'>
                {workspace.workflowCompletion.canCompleteIteration ? (
                  <button
                    type='button'
                    onClick={() => {
                      vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                        type: 'completeIteration',
                      }));
                    }}
                  >
                    Complete Iteration
                  </button>
                ) : null}
                {workspace.workflowCompletion.canReopenIteration ? (
                  <button
                    type='button'
                    onClick={() => {
                      vscodeApiRef.current?.postMessage(withDesignStudioEnvelope({
                        type: 'reopenIteration',
                      }));
                    }}
                  >
                    Reopen Iteration
                  </button>
                ) : null}
              </div>
            </section>
          ) : null}
        </section>
      </div>
    </main>
  );
}
