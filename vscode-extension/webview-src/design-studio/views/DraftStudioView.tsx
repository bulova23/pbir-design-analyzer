import React from 'react';
import type {
  DesignStudioDraftReviewViewModel,
} from '../../../src/design-studio/contracts/designStudioShell';

interface DraftStudioViewProps {
  canGenerateDrafts: boolean;
  draftReview?: DesignStudioDraftReviewViewModel;
  onGenerateDrafts(): void;
  onSubmitDraftForApproval(): void;
  onApproveDraft(): void;
}

function workflowStatusLabel(canGenerateDrafts: boolean, draftReview?: DesignStudioDraftReviewViewModel): string {
  if (!canGenerateDrafts) {
    return 'Blocked';
  }

  if (!draftReview) {
    return 'Not Started';
  }

  if (draftReview.approvalState === 'approved') {
    return 'Approved';
  }

  if (draftReview.approvalState === 'pendingApproval') {
    return 'Ready For Approval';
  }

  return 'Draft Generated';
}

function nextStepGuidance(canGenerateDrafts: boolean, draftReview?: DesignStudioDraftReviewViewModel): string {
  if (!canGenerateDrafts) {
    return 'Approve the Concept baseline before generating a draft.';
  }

  if (!draftReview) {
    return 'Generate a draft from the approved concept.';
  }

  if (draftReview.approvalState === 'approved') {
    return 'Draft approved. Continue to Prepare For Review.';
  }

  if (draftReview.approvalState === 'pendingApproval') {
    return 'Approve the draft to unlock Prepare For Review.';
  }

  return 'Submit draft for approval.';
}

export function DraftStudioView({
  canGenerateDrafts,
  draftReview,
  onGenerateDrafts,
  onSubmitDraftForApproval,
  onApproveDraft,
}: DraftStudioViewProps) {
  return (
    <section className='detail-card'>
      <h3>Draft Studio execution</h3>
      {!canGenerateDrafts ? (
        <p>Draft generation is blocked until the Concept baseline is approved.</p>
      ) : (
        <p>Draft Studio produces isolated, reviewable, non-production draft artifacts only.</p>
      )}

      <section className='detail-card'>
        <h4>Workflow status</h4>
        <p><strong>Draft stage:</strong> {workflowStatusLabel(canGenerateDrafts, draftReview)}</p>
        {draftReview ? <p><strong>Draft approval:</strong> {draftReview.draftStatusLabel}</p> : null}
        <p>{nextStepGuidance(canGenerateDrafts, draftReview)}</p>
      </section>

      <div className='workflow-actions'>
        <button
          type='button'
          disabled={!canGenerateDrafts || draftReview?.approvalState === 'approved'}
          onClick={onGenerateDrafts}
        >
          Generate Draft
        </button>
        <button
          type='button'
          disabled={!draftReview || draftReview.approvalState !== 'notSubmitted'}
          onClick={onSubmitDraftForApproval}
        >
          Submit Draft For Approval
        </button>
        <button
          type='button'
          disabled={!draftReview || draftReview.approvalState !== 'pendingApproval'}
          onClick={onApproveDraft}
        >
          Approve Draft
        </button>
      </div>

      {draftReview ? (
        <>
          <h3>{draftReview.title}</h3>
          <p>{draftReview.summary}</p>

          <section className='detail-card'>
            <h4>Draft Pages</h4>
            <ul>
              {draftReview.draftPages.map((page) => (
                <li key={`${page.title}:${page.structureSummary}`}>
                  <strong>{page.title}</strong>
                  <div>{page.structureSummary}</div>
                </li>
              ))}
            </ul>
          </section>

          <section className='detail-card'>
            <h4>Draft Layouts</h4>
            <ul>
              {draftReview.draftLayouts.map((layout) => (
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
              {draftReview.draftNavigation.map((item) => (
                <li key={`${item.label}:${item.pageTitle}`}>
                  <strong>{item.label}</strong>
                  <div>{item.pageTitle}</div>
                </li>
              ))}
            </ul>
          </section>

          <section className='detail-card'>
            <h4>KPI Placement</h4>
            <ul>
              {draftReview.draftPages.map((page) => (
                <li key={`${page.title}:kpis`}>
                  <strong>{page.title}</strong>
                  <div>{page.kpiPlacement.join(', ')}</div>
                </li>
              ))}
            </ul>
          </section>
        </>
      ) : null}
    </section>
  );
}
