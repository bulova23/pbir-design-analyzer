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
import { ClosedLoopView } from './views/ClosedLoopView';

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

function buildInvestigationSupport(conceptReview: NonNullable<DesignStudioStudioState['workspace']>['conceptReview']) {
  if (!conceptReview) {
    return undefined;
  }

  return {
    question: conceptReview.analyticalFlow[0]?.objective ?? conceptReview.summary,
    investigation: conceptReview.analyticalFlow.map((step) => step.objective),
    evidence: conceptReview.chapterStructure.map((chapter) => chapter.title),
    conclusion: conceptReview.analyticalFlow.at(-1)?.objective ?? conceptReview.summary,
    decision: conceptReview.selectedConceptLabel,
  };
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

          <ApprovalTeachingCard />

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

          {selectedStage === 'concept' && workspace.conceptReview ? (
            (() => {
              const conceptReview = workspace.conceptReview;

              return (
                <section className='detail-card'>
                  <h3>{conceptReview.title}</h3>
                  <p>{conceptReview.summary}</p>
                  <p><strong>Selected baseline:</strong> {conceptReview.selectedConceptLabel}</p>

                  <section className='detail-card'>
                    <h4>Chapter Structure</h4>
                    <ul>
                      {conceptReview.chapterStructure.map((chapter) => (
                        <li key={`${chapter.title}:${chapter.objective}`}>
                          <strong>{chapter.title}</strong>
                          <div>{chapter.objective}</div>
                        </li>
                      ))}
                    </ul>
                  </section>

                  <section className='detail-card'>
                    <h4>KPI Hierarchy</h4>
                    <ul>
                      {conceptReview.kpiHierarchy.map((node) => (
                        <li key={`${node.label}:${node.depth}`}>
                          {`${'  '.repeat(node.depth)}${node.label}`}
                        </li>
                      ))}
                    </ul>
                  </section>

                  <section className='detail-card'>
                    <h4>Navigation Structure</h4>
                    <ul>
                      {conceptReview.navigationStructure.map((node) => (
                        <li key={`${node.label}:${node.depth}`}>
                          {`${'  '.repeat(node.depth)}${node.label}`}
                        </li>
                      ))}
                    </ul>
                  </section>

                  <section className='detail-card'>
                    <h4>Analytical Flow</h4>
                    <ul>
                      {conceptReview.analyticalFlow.map((step) => (
                        <li key={`${step.label}:${step.objective}`}>
                          <strong>{step.label}</strong>
                          <div>{step.objective}</div>
                        </li>
                      ))}
                    </ul>
                  </section>

                  <section className='detail-card'>
                    <h4>Analytical Investigation Support</h4>
                    {(() => {
                      const investigationSupport = buildInvestigationSupport(conceptReview);
                      if (!investigationSupport) {
                        return null;
                      }

                      return (
                        <>
                          <p><strong>Question</strong></p>
                          <p>{investigationSupport.question}</p>
                          <p><strong>Investigation</strong></p>
                          <ul>
                            {investigationSupport.investigation.map((item) => (
                              <li key={`investigation:${item}`}>{item}</li>
                            ))}
                          </ul>
                          <p><strong>Evidence</strong></p>
                          <ul>
                            {investigationSupport.evidence.map((item) => (
                              <li key={`evidence:${item}`}>{item}</li>
                            ))}
                          </ul>
                          <p><strong>Conclusion</strong></p>
                          <p>{investigationSupport.conclusion}</p>
                          <p><strong>Decision</strong></p>
                          <p>{investigationSupport.decision}</p>
                        </>
                      );
                    })()}
                  </section>

                  {conceptReview.comparisons?.map((comparison) => (
                    <section key={comparison.comparisonConceptLabel} className='detail-card'>
                      <h4>{`${conceptReview.selectedConceptLabel} vs ${comparison.comparisonConceptLabel}`}</h4>

                      <section className='detail-card'>
                        <h5>Chapter Structure Comparison</h5>
                        <p><strong>{conceptReview.selectedConceptLabel}</strong></p>
                        <ul>
                          {comparison.chapterStructure.baselineItems.map((item) => (
                            <li key={`chapter:baseline:${comparison.comparisonConceptLabel}:${item}`}>{item}</li>
                          ))}
                        </ul>
                        <p><strong>{comparison.comparisonConceptLabel}</strong></p>
                        <ul>
                          {comparison.chapterStructure.comparisonItems.map((item) => (
                            <li key={`chapter:comparison:${comparison.comparisonConceptLabel}:${item}`}>{item}</li>
                          ))}
                        </ul>
                      </section>

                      <section className='detail-card'>
                        <h5>KPI Hierarchy Comparison</h5>
                        <p><strong>{conceptReview.selectedConceptLabel}</strong></p>
                        <ul>
                          {comparison.kpiHierarchy.baselineItems.map((item) => (
                            <li key={`kpi:baseline:${comparison.comparisonConceptLabel}:${item}`}>{item}</li>
                          ))}
                        </ul>
                        <p><strong>{comparison.comparisonConceptLabel}</strong></p>
                        <ul>
                          {comparison.kpiHierarchy.comparisonItems.map((item) => (
                            <li key={`kpi:comparison:${comparison.comparisonConceptLabel}:${item}`}>{item}</li>
                          ))}
                        </ul>
                      </section>

                      <section className='detail-card'>
                        <h5>Navigation Structure Comparison</h5>
                        <p><strong>{conceptReview.selectedConceptLabel}</strong></p>
                        <ul>
                          {comparison.navigationStructure.baselineItems.map((item) => (
                            <li key={`navigation:baseline:${comparison.comparisonConceptLabel}:${item}`}>{item}</li>
                          ))}
                        </ul>
                        <p><strong>{comparison.comparisonConceptLabel}</strong></p>
                        <ul>
                          {comparison.navigationStructure.comparisonItems.map((item) => (
                            <li key={`navigation:comparison:${comparison.comparisonConceptLabel}:${item}`}>{item}</li>
                          ))}
                        </ul>
                      </section>

                      <section className='detail-card'>
                        <h5>Analytical Flow Comparison</h5>
                        <p><strong>{conceptReview.selectedConceptLabel}</strong></p>
                        <ul>
                          {comparison.analyticalFlow.baselineItems.map((item) => (
                            <li key={`flow:baseline:${comparison.comparisonConceptLabel}:${item}`}>{item}</li>
                          ))}
                        </ul>
                        <p><strong>{comparison.comparisonConceptLabel}</strong></p>
                        <ul>
                          {comparison.analyticalFlow.comparisonItems.map((item) => (
                            <li key={`flow:comparison:${comparison.comparisonConceptLabel}:${item}`}>{item}</li>
                          ))}
                        </ul>
                      </section>
                    </section>
                  ))}
                </section>
              );
            })()
          ) : null}

          {selectedStage === 'draft' && workspace.draftReview ? (
            <section className='detail-card'>
              <h3>{workspace.draftReview.title}</h3>
              <p>{workspace.draftReview.summary}</p>
              <p><strong>Draft status:</strong> {workspace.draftReview.draftStatusLabel}</p>

              <section className='detail-card'>
                <h4>Draft Pages</h4>
                <ul>
                  {workspace.draftReview.draftPages.map((page) => (
                    <li key={`${page.title}:${page.structureSummary}`}>
                      <strong>{page.title}</strong>
                      <div>{page.structureSummary}</div>
                      <div>{page.kpiPlacement.join(', ')}</div>
                    </li>
                  ))}
                </ul>
              </section>

              <section className='detail-card'>
                <h4>Draft Layouts</h4>
                <ul>
                  {workspace.draftReview.draftLayouts.map((layout) => (
                    <li key={`${layout.title}:${layout.layoutType}`}>
                      <strong>{layout.title}</strong>
                      <div>{layout.layoutType}</div>
                      <div>{layout.zones.join(', ')}</div>
                    </li>
                  ))}
                </ul>
              </section>

              <section className='detail-card'>
                <h4>Draft Navigation</h4>
                <ul>
                  {workspace.draftReview.draftNavigation.map((item) => (
                    <li key={`${item.label}:${item.pageTitle}`}>
                      <strong>{item.label}</strong>
                      <div>{item.pageTitle}</div>
                    </li>
                  ))}
                </ul>
              </section>
            </section>
          ) : null}

          {selectedStage === 'materialize' && workspace.materializationReadiness ? (
            <section className='detail-card'>
              <h3>Review preparation</h3>
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
              <h3>Design review handoff</h3>
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

          {selectedStage === 'compare' ? (
            <section className='detail-card'>
              <ClosedLoopView iterations={viewState.state.iterationHistory} />
            </section>
          ) : null}
        </section>
      </div>
    </main>
  );
}
