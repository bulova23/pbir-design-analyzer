import React from 'react';
import { act, fireEvent, render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { withDesignStudioEnvelope } from '../../../src/design-studio/contracts/designStudioProtocol';
import { App } from '../App';

const postMessage = jest.fn();

function dispatchHostMessage(message: unknown): void {
  act(() => {
    window.dispatchEvent(new MessageEvent('message', { data: message }));
  });
}

describe('DesignStudio App shell', () => {
  beforeEach(() => {
    postMessage.mockReset();
    (globalThis as unknown as { acquireVsCodeApi: () => { postMessage: typeof postMessage } }).acquireVsCodeApi =
      () => ({ postMessage });
  });

  it('loads the shell, renders the workflow rail, and shows stage statuses and approval cards', async () => {
    render(<App />);

    expect(postMessage).toHaveBeenNthCalledWith(1, withDesignStudioEnvelope({ type: 'webviewReady' }));
    expect(postMessage).toHaveBeenNthCalledWith(2, withDesignStudioEnvelope({
      type: 'loadStudioState',
      threadId: 'design-studio:active-report',
    }));

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'materialize',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'approved', readinessLabel: 'Approved', title: 'Concept Studio', description: 'Approve the concept baseline.' },
            { id: 'draft', label: 'Draft Studio', status: 'inProgress', readinessLabel: 'In progress', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Materialize Candidate', status: 'ready', readinessLabel: 'Ready', title: 'Materialize Candidate', description: 'Prepare an analyzable candidate without mutating the report.' },
            { id: 'handoff', label: 'Analyze Draft', status: 'ready', readinessLabel: 'Ready', title: 'Analyze Draft', description: 'Launch Analyzer Workspace explicitly.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Materialize Candidate',
            description: 'Prepare an analyzable candidate without mutating the report.',
          },
          approvalCards: [
            {
              kind: 'designApproval',
              title: 'Design Approval',
              approvalState: 'approved',
              owner: 'Design Studio',
              unlock: 'Allows the next design stage to proceed.',
              nonEffects: ['Does not validate the report.', 'Does not materialize the draft.'],
            },
            {
              kind: 'materializationApproval',
              title: 'Materialization Approval',
              approvalState: 'pendingApproval',
              owner: 'Design Studio',
              unlock: 'Allows candidate preparation for analysis.',
              nonEffects: ['Does not run analyzers.', 'Does not mutate PBIR assets.'],
            },
            {
              kind: 'refinementApproval',
              title: 'Refinement Approval',
              approvalState: 'notSubmitted',
              owner: 'Design Studio',
              unlock: 'Accepts advisory design improvements into a new iteration.',
              nonEffects: ['Does not validate the refined result.'],
            },
            {
              kind: 'validationApproval',
              title: 'Validation Approval',
              approvalState: 'notSubmitted',
              owner: 'Analyzer Workspace',
              unlock: 'Records analyzer-owned validation outcome.',
              nonEffects: ['Cannot be self-approved by Design Studio.'],
            },
          ],
          materializationReadiness: {
            readinessLabel: 'Ready for analysis',
            executableEligibility: 'executable',
            targetAnalyzer: 'pbirDesignReview',
            targetAnalyzerProfile: 'consultant',
            diagnostics: ['Repository-backed candidate is available for explicit analyzer handoff.'],
          },
          analyzerHandoff: {
            requestId: 'materialization-request:1',
            readinessLabel: 'Ready to open Analyzer Workspace',
            analyzerId: 'pbirDesignReview',
            analyzerProfileId: 'consultant',
            canOpen: true,
            diagnostics: ['Analysis has not started. Launch is explicit.'],
          },
        },
      },
    }));

    expect(await screen.findByRole('heading', { name: 'Report Design Studio' })).toBeInTheDocument();
    expect(screen.getByText('Sales & Production')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Design Brief/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Refinement Studio/ })).toBeInTheDocument();
    expect(screen.getAllByText('Approved').length).toBeGreaterThan(0);
    expect(screen.getAllByText('In progress').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Blocked').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Ready').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'Materialization Approval' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Design Approval' })).not.toBeInTheDocument();
  });

  it('renders materialization readiness and exposes an explicit analyzer handoff entry without auto-launching it', async () => {
    render(<App />);

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'handoff',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'approved', readinessLabel: 'Approved', title: 'Concept Studio', description: 'Approve the concept baseline.' },
            { id: 'draft', label: 'Draft Studio', status: 'approved', readinessLabel: 'Approved', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Materialize Candidate', status: 'approved', readinessLabel: 'Approved', title: 'Materialize Candidate', description: 'Prepare an analyzable candidate without mutating the report.' },
            { id: 'handoff', label: 'Analyze Draft', status: 'ready', readinessLabel: 'Ready', title: 'Analyze Draft', description: 'Launch Analyzer Workspace explicitly when the candidate is ready.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Analyze Draft',
            description: 'Launch Analyzer Workspace explicitly when the candidate is ready.',
          },
          approvalCards: [],
          materializationReadiness: {
            readinessLabel: 'Ready for analysis',
            executableEligibility: 'executable',
            targetAnalyzer: 'pbirDesignReview',
            targetAnalyzerProfile: 'consultant',
            diagnostics: ['Repository-backed candidate is available for explicit analyzer handoff.'],
          },
          analyzerHandoff: {
            requestId: 'materialization-request:2',
            readinessLabel: 'Ready to open Analyzer Workspace',
            analyzerId: 'pbirDesignReview',
            analyzerProfileId: 'consultant',
            canOpen: true,
            diagnostics: ['Analysis has not started. Launch is explicit.'],
          },
        },
      },
    }));

    expect(screen.getAllByText('pbirDesignReview').length).toBeGreaterThan(0);
    expect(screen.getAllByText('consultant').length).toBeGreaterThan(0);
    expect(await screen.findByText('Ready to open Analyzer Workspace')).toBeInTheDocument();

    expect(postMessage).not.toHaveBeenCalledWith(withDesignStudioEnvelope({
      type: 'openAnalyzerHandoff',
      requestId: 'materialization-request:2',
    }));

    fireEvent.click(screen.getByRole('button', { name: 'Open Analyzer Workspace' }));

    expect(postMessage).toHaveBeenCalledWith(withDesignStudioEnvelope({
      type: 'openAnalyzerHandoff',
      requestId: 'materialization-request:2',
    }));
  });

  it('renders grouped suggested improvements with rationale, impact, comparison, and explicit proposal actions', async () => {
    render(<App />);

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'refinement',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'approved', readinessLabel: 'Approved', title: 'Concept Studio', description: 'Approve the concept baseline.' },
            { id: 'draft', label: 'Draft Studio', status: 'approved', readinessLabel: 'Approved', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'inProgress', readinessLabel: 'In progress', title: 'Refinement Studio', description: 'Review analyzer-derived design improvements without mutating the report automatically.' },
            { id: 'materialize', label: 'Materialize Candidate', status: 'approved', readinessLabel: 'Approved', title: 'Materialize Candidate', description: 'Prepare an analyzable candidate without mutating the report.' },
            { id: 'handoff', label: 'Analyze Draft', status: 'ready', readinessLabel: 'Ready', title: 'Analyze Draft', description: 'Launch Analyzer Workspace explicitly.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Refinement Studio',
            description: 'Review analyzer-derived design improvements.',
          },
          approvalCards: [
            {
              kind: 'refinementApproval',
              title: 'Refinement Approval',
              approvalState: 'pendingApproval',
              owner: 'Design Studio',
              unlock: 'Accepts advisory design changes into a new iteration path.',
              nonEffects: ['Does not validate the refined result.'],
            },
          ],
          refinementExperience: {
            title: 'Suggested Improvements',
            summary: 'Review grouped consultant-style recommendations, understand the reasoning, and decide which proposals should shape the next iteration.',
            groups: [
              {
                id: 'story',
                title: 'Story Improvements',
                summary: 'Clarify the headline question, comparison context, and decision path.',
                proposals: [
                  {
                    id: 'refinement-proposal:1',
                    title: 'Strengthen the page question',
                    summary: 'The page needs a clearer narrative headline.',
                    recommendation: 'Revise the page so the headline states the business question first.',
                    rationale: 'The page opens with metrics before the decision context is established.',
                    expectedImpact: 'Stronger story clarity',
                    approvalState: 'pendingApproval',
                    sourceAnalyzerLabel: 'Story Assessment',
                    affectedArtifacts: ['Page concept: Executive overview', 'Layout: KPI summary row'],
                    supportingEvidence: ['Narrative headline is missing a clear question.', 'Affected area: Page concept: Executive overview'],
                    comparison: {
                      originalDesignIntent: 'Lead with risk, then explain the main drivers and next steps.',
                      currentDesignState: 'The current draft opens with KPIs before the user understands the main question.',
                      proposedRefinement: 'Revise the page so the headline states the business question first.',
                    },
                    availableActions: ['approve', 'reject', 'defer'],
                  },
                ],
              },
              {
                id: 'navigation',
                title: 'Navigation Improvements',
                summary: 'Reduce branching and make the report flow easier to follow.',
                proposals: [
                  {
                    id: 'refinement-proposal:2',
                    title: 'Navigation drift',
                    summary: 'Users do not move cleanly from summary to detail.',
                    recommendation: 'Reduce the number of branches and clarify the path into detail pages.',
                    rationale: 'Executives branch too early and lose the main story thread.',
                    expectedImpact: 'Easier navigation',
                    approvalState: 'approved',
                    sourceAnalyzerLabel: 'Issues',
                    affectedArtifacts: ['Navigation draft: tabbed story flow'],
                    supportingEvidence: ['Analyzer signal: Navigation drift'],
                    comparison: {
                      originalDesignIntent: 'Overview first, detail second.',
                      currentDesignState: 'The current navigation branches into multiple peer pages from the landing page.',
                      proposedRefinement: 'Reduce the number of branches and clarify the path into detail pages.',
                    },
                    availableActions: ['defer', 'reject'],
                  },
                ],
              },
            ],
          },
        },
      },
    }));

    expect(await screen.findByRole('heading', { name: 'Suggested Improvements' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Refinement Approval' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Story Improvements' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Navigation Improvements' })).toBeInTheDocument();
    expect(screen.getByText('The page opens with metrics before the decision context is established.')).toBeInTheDocument();
    expect(screen.getAllByText('Stronger story clarity').length).toBeGreaterThan(0);
    expect(screen.getByText('Easier navigation')).toBeInTheDocument();
    expect(screen.getAllByText('Original Design Intent:').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Current Design State:').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Proposed Refinement:').length).toBeGreaterThan(0);
    expect(screen.getByRole('button', { name: 'Approve Proposal' })).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Defer Proposal' }).length).toBeGreaterThan(0);

    fireEvent.click(screen.getByRole('button', { name: 'Approve Proposal' }));
    expect(postMessage).toHaveBeenCalledWith(withDesignStudioEnvelope({
      type: 'setRefinementProposalState',
      proposalId: 'refinement-proposal:1',
      action: 'approve',
    }));

    fireEvent.click(screen.getAllByRole('button', { name: 'Reject Proposal' })[0]!);
    expect(postMessage).toHaveBeenCalledWith(withDesignStudioEnvelope({
      type: 'setRefinementProposalState',
      proposalId: 'refinement-proposal:1',
      action: 'reject',
    }));

    fireEvent.click(screen.getAllByRole('button', { name: 'Defer Proposal' })[0]!);
    expect(postMessage).toHaveBeenCalledWith(withDesignStudioEnvelope({
      type: 'setRefinementProposalState',
      proposalId: 'refinement-proposal:1',
      action: 'defer',
    }));

    expect(postMessage).not.toHaveBeenCalledWith(expect.objectContaining({
      type: 'openAnalyzerHandoff',
    }));
  });
});
