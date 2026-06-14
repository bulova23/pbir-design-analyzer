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

  it('renders the compare stage with iteration history, before-and-after selection, and user-facing evolution summaries', async () => {
    render(<App />);

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        iterationHistory: [
          {
            id: 'design-iteration:1',
            threadId: 'design-studio:active-report',
            kind: 'designIterationRecord',
            version: 1,
            lifecycleState: 'reviewed',
            createdAt: '2026-06-13T12:00:00.000Z',
            updatedAt: '2026-06-13T12:00:00.000Z',
            authorSource: 'system',
            provenance: { source: 'system', notes: ['Closed loop records remain audit-only.'] },
            sourceArtifactVersionIds: ['draft-report:active@v1'],
            analyzerResults: [{ analyzerSource: 'storyAssessment', analyzerRunId: 'run-1', resultReference: 'issues:1', scoredAt: '2026-06-13T12:00:00.000Z', validationResultStatus: 'needsReview' }],
            refinementProposals: [{ proposalId: 'proposal:1', approvalState: 'approved', suggestedDesignChange: 'Clarify the executive question.', expectedImpact: 'Improve report flow.', linkedFindingIds: ['finding-1'] }],
            approvalCheckpoint: {
              designApproval: { approvalKind: 'designApproval', approvalState: 'approved' },
              materializationApproval: { approvalKind: 'materializationApproval', approvalState: 'approved' },
              refinementApproval: { approvalKind: 'refinementApproval', approvalState: 'pendingApproval' },
              validationApproval: { approvalKind: 'validationApproval', approvalState: 'notSubmitted', validationResultStatus: 'needsReview' },
            },
            comparisonSnapshot: {
              concept: { summary: 'Original concept summary', pageTitles: ['Executive overview'], navigationPattern: 'guidedFlow' },
              draft: { summary: 'Original draft summary', pageStructureSummaries: ['Executive overview scaffold'], layoutTitles: ['KPI grid'], navigationFrameworks: ['guidedFlow'] },
              analyzerOutputs: [{ resultReference: 'issues:1', analyzerRunId: 'run-1', analyzerSource: 'storyAssessment', validationResultStatus: 'needsReview' }],
              recommendations: [{ proposalId: 'proposal:1', suggestedDesignChange: 'Clarify the executive question.', expectedImpact: 'Improve report flow.', approvalState: 'approved' }],
              validationStatus: 'needsReview',
            },
            guardrails: { autoOptimizationTriggered: false, analyzerExecutionTriggered: false, reportMutationTriggered: false, pbirFilesGenerated: false },
            comparisonSummary: 'Started with an executive overview draft.',
          },
          {
            id: 'design-iteration:2',
            threadId: 'design-studio:active-report',
            kind: 'designIterationRecord',
            version: 2,
            lifecycleState: 'reviewed',
            createdAt: '2026-06-13T15:30:00.000Z',
            updatedAt: '2026-06-13T15:30:00.000Z',
            authorSource: 'system',
            provenance: { source: 'system', notes: ['Closed loop records remain audit-only.'] },
            previousIterationId: 'design-iteration:1',
            sourceArtifactVersionIds: ['draft-report:active@v2'],
            analyzerResults: [{ analyzerSource: 'guidedStoryImprovements', analyzerRunId: 'run-2', resultReference: 'issues:2', scoredAt: '2026-06-13T15:30:00.000Z', validationResultStatus: 'validated' }],
            refinementProposals: [{ proposalId: 'proposal:2', approvalState: 'rejected', suggestedDesignChange: 'Add benchmark recommendation.', expectedImpact: 'Strengthen comparison context.', linkedFindingIds: ['finding-2'] }],
            approvalCheckpoint: {
              designApproval: { approvalKind: 'designApproval', approvalState: 'approved' },
              materializationApproval: { approvalKind: 'materializationApproval', approvalState: 'approved' },
              refinementApproval: { approvalKind: 'refinementApproval', approvalState: 'rejected' },
              validationApproval: { approvalKind: 'validationApproval', approvalState: 'approved', validationResultStatus: 'validated', owner: 'analyzerWorkspace', analyzerRunId: 'run-2', resultReference: 'issues:2' },
            },
            comparisonSnapshot: {
              concept: { summary: 'Refined concept summary', pageTitles: ['Executive overview', 'Benchmark detail'], navigationPattern: 'hubAndSpoke' },
              draft: { summary: 'Refined draft summary', pageStructureSummaries: ['Executive overview scaffold', 'Benchmark comparison page'], layoutTitles: ['KPI grid', 'Benchmark comparison'], navigationFrameworks: ['hubAndSpoke'] },
              analyzerOutputs: [{ resultReference: 'issues:2', analyzerRunId: 'run-2', analyzerSource: 'guidedStoryImprovements', validationResultStatus: 'validated' }],
              recommendations: [{ proposalId: 'proposal:2', suggestedDesignChange: 'Add benchmark recommendation.', expectedImpact: 'Strengthen comparison context.', approvalState: 'rejected' }],
              validationStatus: 'validated',
            },
            guardrails: { autoOptimizationTriggered: false, analyzerExecutionTriggered: false, reportMutationTriggered: false, pbirFilesGenerated: false },
            comparisonSummary: 'Improved report flow and added benchmark context.',
          },
        ],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'compare',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'approved', readinessLabel: 'Approved', title: 'Concept Studio', description: 'Approve the concept baseline.' },
            { id: 'draft', label: 'Draft Studio', status: 'approved', readinessLabel: 'Approved', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'approved', readinessLabel: 'Approved', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Materialize Candidate', status: 'approved', readinessLabel: 'Approved', title: 'Materialize Candidate', description: 'Prepare an analyzable candidate.' },
            { id: 'handoff', label: 'Analyze Draft', status: 'approved', readinessLabel: 'Approved', title: 'Analyze Draft', description: 'Launch Analyzer Workspace explicitly.' },
            { id: 'compare', label: 'Compare Iterations', status: 'approved', readinessLabel: 'Approved', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Compare Iterations',
            description: 'Review what changed.',
          },
          approvalCards: [
            {
              kind: 'validationApproval',
              title: 'Validation Approval',
              approvalState: 'approved',
              owner: 'Analyzer Workspace',
              unlock: 'Records analyzer-owned validation outcome.',
              nonEffects: ['Cannot be self-approved by Design Studio.'],
            },
          ],
        },
      },
    }));

    expect(await screen.findByRole('heading', { name: 'Iteration Timeline' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'What Improved' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'What Was Accepted' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'What Changed' })).toBeInTheDocument();
    expect(screen.getByText('Changed navigation structure.')).toBeInTheDocument();
    expect(screen.getByText('Rejected recommendation: Add benchmark recommendation.')).toBeInTheDocument();
    expect(screen.queryByText('issues:2')).not.toBeInTheDocument();
  });

  it('renders Concept Studio artifacts and consultant-friendly workflow language without changing workflow ids', async () => {
    render(<App />);

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'concept',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'approved', readinessLabel: 'Approved', title: 'Concept Studio', description: 'Review the concept structure and approve the baseline.' },
            { id: 'draft', label: 'Draft Studio', status: 'ready', readinessLabel: 'Ready', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Prepare For Review', status: 'ready', readinessLabel: 'Ready', title: 'Prepare For Review', description: 'Prepare the approved draft for consultant review without changing the report.' },
            { id: 'handoff', label: 'Review Design', status: 'blocked', readinessLabel: 'Blocked', title: 'Review Design', description: 'Open Analyzer Workspace explicitly when the prepared review candidate is ready.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Concept Studio',
            description: 'Review the concept structure and approve the baseline.',
          },
          approvalCards: [
            {
              kind: 'designApproval',
              title: 'Design Approval',
              approvalState: 'approved',
              owner: 'Design Studio',
              unlock: 'Allows the next design stage to proceed.',
              nonEffects: ['Does not validate the report.', 'Does not prepare the draft for review.'],
            },
          ],
          conceptReview: {
            title: 'Concept Review Artifacts',
            summary: 'Review the report concept before Draft Studio work begins.',
            selectedConceptLabel: 'Narrative-first storyline',
            chapterStructure: [
              { title: 'Executive Summary', objective: 'Open with the main business question.' },
              { title: 'Regional Analysis', objective: 'Show where performance diverges.' },
            ],
            kpiHierarchy: [
              { label: 'Revenue', level: 'primary', depth: 0 },
              { label: 'Margin', level: 'supporting', depth: 1 },
              { label: 'Forecast Accuracy', level: 'diagnostic', depth: 2 },
            ],
            navigationStructure: [
              { label: 'Executive Summary', depth: 0 },
              { label: 'Regional Analysis', depth: 1 },
              { label: 'Store Detail', depth: 2 },
            ],
            analyticalFlow: [
              { label: 'Question', objective: 'Where is revenue underperforming?' },
              { label: 'Investigation', objective: 'Compare regions and segments.' },
              { label: 'Conclusion', objective: 'Identify where intervention is needed first.' },
            ],
            comparisons: [
              {
                comparisonConceptLabel: 'Operating-rhythm command deck',
                chapterStructure: {
                  baselineItems: ['Story setup', 'Regional Analysis'],
                  comparisonItems: ['Decision priorities', 'Regional Analysis'],
                },
                kpiHierarchy: {
                  baselineItems: ['Revenue', 'Margin', 'Forecast Accuracy'],
                  comparisonItems: ['Revenue', 'Intervention Rate'],
                },
                navigationStructure: {
                  baselineItems: ['Executive Summary', 'Regional Analysis', 'Store Detail'],
                  comparisonItems: ['Priorities', 'Regional Analysis', 'Action Queue'],
                },
                analyticalFlow: {
                  baselineItems: ['Question', 'Investigation', 'Conclusion'],
                  comparisonItems: ['Question', 'Investigation', 'Evidence', 'Decision'],
                },
              },
            ],
          },
        },
      },
    }));

    expect(await screen.findByText('Prepare For Review')).toBeInTheDocument();
    expect(screen.getByText('Review Design')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Concept Review Artifacts' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Chapter Structure' })).toBeInTheDocument();
    expect(screen.getAllByText('Executive Summary').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'KPI Hierarchy' })).toBeInTheDocument();
    expect(screen.getAllByText('Forecast Accuracy').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'Navigation Structure' })).toBeInTheDocument();
    expect(screen.getAllByText('Store Detail').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'Analytical Flow' })).toBeInTheDocument();
    expect(screen.getAllByText('Where is revenue underperforming?').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'Chapter Structure Comparison' })).toBeInTheDocument();
    expect(screen.getByText('Narrative-first storyline vs Operating-rhythm command deck')).toBeInTheDocument();
    expect(screen.getAllByText('Evidence').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Decision').length).toBeGreaterThan(0);
  });

  it('renders Draft Studio artifacts and keeps Ready, Approved, and Validated distinct', async () => {
    render(<App />);

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'draft',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'approved', readinessLabel: 'Approved', title: 'Concept Studio', description: 'Approve the concept baseline.' },
            { id: 'draft', label: 'Draft Studio', status: 'approved', readinessLabel: 'Approved', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Prepare For Review', status: 'ready', readinessLabel: 'Ready', title: 'Prepare For Review', description: 'Prepare the approved draft for consultant review without changing the report.' },
            { id: 'handoff', label: 'Review Design', status: 'blocked', readinessLabel: 'Blocked', title: 'Review Design', description: 'Open Analyzer Workspace explicitly when the prepared review candidate is ready.' },
            { id: 'compare', label: 'Compare Iterations', status: 'approved', readinessLabel: 'Validated', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Draft Studio',
            description: 'Review the designed pages before approval.',
          },
          approvalCards: [
            {
              kind: 'designApproval',
              title: 'Design Approval',
              approvalState: 'approved',
              owner: 'Design Studio',
              unlock: 'Confirms the current draft is ready for the next workflow step.',
              nonEffects: ['Does not validate the report.', 'Does not prepare the draft automatically.'],
            },
            {
              kind: 'validationApproval',
              title: 'Validation Approval',
              approvalState: 'approved',
              owner: 'Analyzer Workspace',
              unlock: 'Records the analyzer-owned validation outcome.',
              nonEffects: ['Cannot be self-approved by Design Studio.'],
            },
          ],
          draftReview: {
            title: 'Draft Review Artifacts',
            summary: 'Review the designed pages, layouts, navigation, and KPI placement before approval.',
            draftStatusLabel: 'Approved draft',
            draftPages: [
              {
                title: 'Executive Summary',
                structureSummary: 'Executive page with KPI row and risk narrative.',
                kpiPlacement: ['Revenue', 'Margin'],
              },
            ],
            draftLayouts: [
              {
                title: 'Executive KPI layout',
                layoutType: 'kpiGrid',
                zones: ['Top row', 'Narrative panel'],
              },
            ],
            draftNavigation: [
              {
                label: 'Executive Summary',
                pageTitle: 'Executive Summary',
              },
            ],
          },
        },
      },
    }));

    expect(await screen.findByRole('heading', { name: 'Draft Review Artifacts' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Draft Pages' })).toBeInTheDocument();
    expect(screen.getByText('Executive page with KPI row and risk narrative.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Draft Layouts' })).toBeInTheDocument();
    expect(screen.getByText('Executive KPI layout')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Draft Navigation' })).toBeInTheDocument();
    expect(screen.getAllByText('Approved').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Ready').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Validated').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'Approval stages' })).toBeInTheDocument();
    expect(screen.getByText('Ready means the stage can move into review.')).toBeInTheDocument();
    expect(screen.getByText('Approved means Design Studio accepted the current design baseline.')).toBeInTheDocument();
    expect(screen.getByText('Validated means Analyzer Workspace recorded the review outcome.')).toBeInTheDocument();
  });
});
