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

  it('renders the executable Design Brief workflow, keeps header selection in sync, and unlocks Concept Studio only after approval', async () => {
    render(<App />);

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        currentBrief: undefined,
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'brief',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'notStarted', readinessLabel: 'Not started', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Concept Studio', description: 'Concepts stay blocked until the brief is approved.' },
            { id: 'draft', label: 'Draft Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Prepare For Review', status: 'blocked', readinessLabel: 'Blocked', title: 'Prepare For Review', description: 'Prepare an analyzable candidate without mutating the report.' },
            { id: 'handoff', label: 'Review Design', status: 'blocked', readinessLabel: 'Blocked', title: 'Review Design', description: 'Launch Analyzer Workspace explicitly.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Design Brief',
            description: 'Define the brief.',
          },
          approvalCards: [
            {
              kind: 'designApproval',
              title: 'Design Approval',
              approvalState: 'notSubmitted',
              owner: 'Design Studio',
              unlock: 'Allows the next design stage to proceed.',
              nonEffects: ['Does not validate the report.', 'Does not materialize the draft.'],
            },
          ],
        },
      },
    }));

    expect(await screen.findByRole('heading', { name: 'Design Brief' })).toBeInTheDocument();
    expect(screen.getByText('Current stage')).toBeInTheDocument();
    expect(screen.getAllByText('Design Brief').length).toBeGreaterThan(0);
    expect(screen.getByText('Complete required fields to continue.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save Draft' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Submit For Approval' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Approve Brief' })).toBeDisabled();

    fireEvent.change(screen.getByLabelText(/Audience/), { target: { value: 'Executive sponsors' } });
    fireEvent.change(screen.getByLabelText(/Business Objective/), { target: { value: 'Reduce churn risk' } });
    fireEvent.change(screen.getByLabelText(/Key Decisions/), { target: { value: 'Which segments need retention action' } });
    fireEvent.change(screen.getByLabelText(/Primary KPIs/), { target: { value: 'Churn rate' } });
    fireEvent.change(screen.getByLabelText(/Dimensions/), { target: { value: 'Segment' } });
    fireEvent.change(screen.getByLabelText(/Intended Story/), { target: { value: 'Start with risk, then isolate causes.' } });
    fireEvent.change(screen.getByLabelText(/Success Criteria/), { target: { value: 'Sponsor can focus the retention review quickly' } });
    fireEvent.change(screen.getByLabelText(/Navigation Expectations/), { target: { value: 'Overview to segment detail.' } });

    expect(screen.getByText('Ready for approval.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Submit For Approval' })).toBeEnabled();

    fireEvent.click(screen.getByRole('button', { name: 'Save Draft' }));
    expect(postMessage).toHaveBeenCalledWith(expect.objectContaining({
      type: 'saveArtifact',
      artifactKind: 'designBrief',
    }));

    fireEvent.click(screen.getByRole('button', { name: 'Submit For Approval' }));
    expect(postMessage).toHaveBeenCalledWith(expect.objectContaining({
      type: 'proposeArtifact',
      artifactKind: 'designBrief',
    }));

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        currentBrief: {
          id: 'design-brief:design-studio:active-report',
          threadId: 'design-studio:active-report',
          kind: 'designBrief',
          version: 2,
          lifecycleState: 'draft',
          approvalState: 'pendingApproval',
          approvalKind: 'designApproval',
          createdAt: '2026-06-16T12:00:00.000Z',
          updatedAt: '2026-06-16T12:05:00.000Z',
          authorSource: 'user',
          provenance: { source: 'user' },
          audience: 'Executive sponsors',
          businessObjective: 'Reduce churn risk',
          keyDecisions: ['Which segments need retention action'],
          primaryKpis: ['Churn rate'],
          dimensions: ['Segment'],
          intendedStory: 'Start with risk, then isolate causes.',
          successCriteria: ['Sponsor can focus the retention review quickly'],
          reportType: 'dashboard',
          navigationExpectations: 'Overview to segment detail.',
        },
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'brief',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'ready', readinessLabel: 'Ready', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Concept Studio', description: 'Concepts stay blocked until the brief is approved.' },
            { id: 'draft', label: 'Draft Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Prepare For Review', status: 'blocked', readinessLabel: 'Blocked', title: 'Prepare For Review', description: 'Prepare an analyzable candidate without mutating the report.' },
            { id: 'handoff', label: 'Review Design', status: 'blocked', readinessLabel: 'Blocked', title: 'Review Design', description: 'Launch Analyzer Workspace explicitly.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Design Brief',
            description: 'Define the brief.',
          },
          approvalCards: [
            {
              kind: 'designApproval',
              title: 'Design Approval',
              approvalState: 'pendingApproval',
              owner: 'Design Studio',
              unlock: 'Allows the next design stage to proceed.',
              nonEffects: ['Does not validate the report.', 'Does not materialize the draft.'],
            },
          ],
        },
      },
    }));

    expect(await screen.findByText('Submitted for approval. Approve the brief to continue.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Approve Brief' })).toBeEnabled();
    expect(screen.getByRole('button', { name: /Concept Studio/ })).toHaveAttribute('aria-disabled', 'true');

    fireEvent.click(screen.getByRole('button', { name: 'Approve Brief' }));
    expect(postMessage).toHaveBeenCalledWith(expect.objectContaining({
      type: 'approveArtifact',
      artifactKind: 'designBrief',
      artifactId: 'design-brief:design-studio:active-report',
    }));

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        currentBrief: {
          id: 'design-brief:design-studio:active-report',
          threadId: 'design-studio:active-report',
          kind: 'designBrief',
          version: 3,
          lifecycleState: 'approved',
          approvalState: 'approved',
          approvalKind: 'designApproval',
          createdAt: '2026-06-16T12:00:00.000Z',
          updatedAt: '2026-06-16T12:10:00.000Z',
          authorSource: 'user',
          provenance: { source: 'user' },
          audience: 'Executive sponsors',
          businessObjective: 'Reduce churn risk',
          keyDecisions: ['Which segments need retention action'],
          primaryKpis: ['Churn rate'],
          dimensions: ['Segment'],
          intendedStory: 'Start with risk, then isolate causes.',
          successCriteria: ['Sponsor can focus the retention review quickly'],
          reportType: 'dashboard',
          navigationExpectations: 'Overview to segment detail.',
        },
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'concept',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'notStarted', readinessLabel: 'Not started', title: 'Concept Studio', description: 'Concepts can now proceed.' },
            { id: 'draft', label: 'Draft Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Prepare For Review', status: 'blocked', readinessLabel: 'Blocked', title: 'Prepare For Review', description: 'Prepare an analyzable candidate without mutating the report.' },
            { id: 'handoff', label: 'Review Design', status: 'blocked', readinessLabel: 'Blocked', title: 'Review Design', description: 'Launch Analyzer Workspace explicitly.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Concept Studio',
            description: 'Concepts can now proceed.',
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
          ],
        },
      },
    }));

    expect(await screen.findByText('Design Brief approved. Continue to Concept Studio.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Concept Studio/ })).not.toHaveAttribute('aria-disabled', 'true');

    fireEvent.click(screen.getByRole('button', { name: /Concept Studio/ }));
    expect(screen.getByText('Current stage')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Concept Studio' })).toBeInTheDocument();
    expect(screen.getAllByText('Generate concept options from the approved Design Brief.').length).toBeGreaterThan(0);
  });

  it('executes the Concept Studio workflow from generation through approval and keeps the selected-stage header accurate', async () => {
    render(<App />);

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        currentBrief: {
          id: 'design-brief:design-studio:active-report',
          threadId: 'design-studio:active-report',
          kind: 'designBrief',
          version: 3,
          lifecycleState: 'approved',
          approvalState: 'approved',
          approvalKind: 'designApproval',
          createdAt: '2026-06-16T12:00:00.000Z',
          updatedAt: '2026-06-16T12:10:00.000Z',
          authorSource: 'user',
          provenance: { source: 'user' },
          audience: 'Executive sponsors',
          businessObjective: 'Reduce churn risk',
          keyDecisions: ['Which segments need retention action'],
          primaryKpis: ['Churn rate'],
          dimensions: ['Segment'],
          intendedStory: 'Start with risk, then isolate causes.',
          successCriteria: ['Sponsor can focus the retention review quickly'],
          reportType: 'dashboard',
          navigationExpectations: 'Overview to segment detail.',
        },
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'concept',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'notStarted', readinessLabel: 'Not started', title: 'Concept Studio', description: 'Generate concept options from the approved Design Brief.' },
            { id: 'draft', label: 'Draft Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Prepare For Review', status: 'blocked', readinessLabel: 'Blocked', title: 'Prepare For Review', description: 'Prepare an analyzable candidate without mutating the report.' },
            { id: 'handoff', label: 'Review Design', status: 'blocked', readinessLabel: 'Blocked', title: 'Review Design', description: 'Launch Analyzer Workspace explicitly.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Draft Studio',
            description: 'Stale summary that should not override the selected stage header.',
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
          ],
        },
      },
    }));

    fireEvent.click(screen.getByRole('button', { name: /Concept Studio/ }));
    expect(await screen.findByRole('heading', { name: 'Concept Studio' })).toBeInTheDocument();
    expect(screen.getAllByText('Generate concept options from the approved Design Brief.').length).toBeGreaterThan(0);
    expect(screen.getByText('Current stage')).toBeInTheDocument();
    expect(screen.getAllByText('Concept Studio').length).toBeGreaterThan(0);

    fireEvent.click(screen.getByRole('button', { name: 'Generate Concepts' }));
    expect(postMessage).toHaveBeenCalledWith(expect.objectContaining({
      type: 'generateConcepts',
    }));

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        currentBrief: {
          id: 'design-brief:design-studio:active-report',
          threadId: 'design-studio:active-report',
          kind: 'designBrief',
          version: 3,
          lifecycleState: 'approved',
          approvalState: 'approved',
          approvalKind: 'designApproval',
          createdAt: '2026-06-16T12:00:00.000Z',
          updatedAt: '2026-06-16T12:10:00.000Z',
          authorSource: 'user',
          provenance: { source: 'user' },
          audience: 'Executive sponsors',
          businessObjective: 'Reduce churn risk',
          keyDecisions: ['Which segments need retention action'],
          primaryKpis: ['Churn rate'],
          dimensions: ['Segment'],
          intendedStory: 'Start with risk, then isolate causes.',
          successCriteria: ['Sponsor can focus the retention review quickly'],
          reportType: 'dashboard',
          navigationExpectations: 'Overview to segment detail.',
        },
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'concept',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'inProgress', readinessLabel: 'In progress', title: 'Concept Studio', description: 'Review concept alternatives and choose a baseline.' },
            { id: 'draft', label: 'Draft Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Prepare For Review', status: 'blocked', readinessLabel: 'Blocked', title: 'Prepare For Review', description: 'Prepare an analyzable candidate without mutating the report.' },
            { id: 'handoff', label: 'Review Design', status: 'blocked', readinessLabel: 'Blocked', title: 'Review Design', description: 'Launch Analyzer Workspace explicitly.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Design Brief',
            description: 'Stale summary that should not override the selected stage header.',
          },
          approvalCards: [
            {
              kind: 'designApproval',
              title: 'Design Approval',
              approvalState: 'notSubmitted',
              owner: 'Design Studio',
              unlock: 'Allows the next design stage to proceed.',
              nonEffects: ['Does not validate the report.', 'Does not materialize the draft.'],
            },
          ],
          conceptReview: {
            title: 'Concept Review Artifacts',
            summary: 'Review the chapter structure, KPI hierarchy, navigation path, and analytical flow before Draft Studio work begins.',
            selectedConceptLabel: 'Current concept baseline',
            chapterStructure: [],
            kpiHierarchy: [],
            navigationStructure: [],
            analyticalFlow: [],
            alternateConcepts: [
              {
                id: 'concept-operating-rhythm',
                label: 'Operating-rhythm command deck',
                summary: 'Leads with the operating KPI and then branches into intervention pages.',
                chapterMap: { chapters: [{ id: 'chapter-1', title: 'Decision priorities', objective: 'Show what needs intervention first.', pageRecommendationIds: ['page-1'] }] },
                pageRecommendations: [{ id: 'page-1', title: 'Overview', objective: 'Summarize the decision KPI.', chapterId: 'chapter-1', recommendedKpis: ['Churn rate'] }],
                kpiHierarchy: { nodes: [{ id: 'kpi-1', label: 'Churn rate', level: 'primary', childNodeIds: [] }], supportingDimensions: ['Segment'] },
                navigationStructure: { pattern: 'hubAndSpoke', rationale: 'Keeps the top-level action path tight.', sections: [{ id: 'nav-1', label: 'Priorities', pageRecommendationIds: ['page-1'] }] },
                analyticalFlow: { steps: [{ id: 'flow-1', label: 'Find the risk', objective: 'Spot the highest-risk segment.', pageRecommendationId: 'page-1' }] },
              },
              {
                id: 'concept-narrative',
                label: 'Narrative-first storyline',
                summary: 'Frames the business story first and then supports it with action pages.',
                chapterMap: { chapters: [{ id: 'chapter-2', title: 'Story setup', objective: 'Frame the narrative and the stakes.', pageRecommendationIds: ['page-2'] }] },
                pageRecommendations: [{ id: 'page-2', title: 'Narrative setup', objective: 'Explain the business objective and decision path.', chapterId: 'chapter-2', recommendedKpis: ['Churn rate'] }],
                kpiHierarchy: { nodes: [{ id: 'kpi-2', label: 'Churn rate', level: 'primary', childNodeIds: ['kpi-3'] }, { id: 'kpi-3', label: 'Decision confidence', level: 'diagnostic', childNodeIds: [] }], supportingDimensions: ['Segment'] },
                navigationStructure: { pattern: 'linearNarrative', rationale: 'Guides the user through a fixed story sequence.', sections: [{ id: 'nav-2', label: 'Story', pageRecommendationIds: ['page-2'] }] },
                analyticalFlow: { steps: [{ id: 'flow-2', label: 'Frame the story', objective: 'Explain the stakes before details.', pageRecommendationId: 'page-2' }] },
              },
            ],
          },
        },
      },
    }));

    expect(await screen.findByText('Concept alternatives')).toBeInTheDocument();
    expect(screen.getByText('Select a preferred baseline before submitting for approval.')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Choose Narrative-first storyline' }));
    expect(postMessage).toHaveBeenCalledWith(expect.objectContaining({
      type: 'selectConceptBaseline',
      conceptId: 'concept-narrative',
    }));

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        currentBrief: {
          id: 'design-brief:design-studio:active-report',
          threadId: 'design-studio:active-report',
          kind: 'designBrief',
          version: 3,
          lifecycleState: 'approved',
          approvalState: 'approved',
          approvalKind: 'designApproval',
          createdAt: '2026-06-16T12:00:00.000Z',
          updatedAt: '2026-06-16T12:10:00.000Z',
          authorSource: 'user',
          provenance: { source: 'user' },
          audience: 'Executive sponsors',
          businessObjective: 'Reduce churn risk',
          keyDecisions: ['Which segments need retention action'],
          primaryKpis: ['Churn rate'],
          dimensions: ['Segment'],
          intendedStory: 'Start with risk, then isolate causes.',
          successCriteria: ['Sponsor can focus the retention review quickly'],
          reportType: 'dashboard',
          navigationExpectations: 'Overview to segment detail.',
        },
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'concept',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'inProgress', readinessLabel: 'In progress', title: 'Concept Studio', description: 'Review concept alternatives and choose a baseline.' },
            { id: 'draft', label: 'Draft Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Prepare For Review', status: 'blocked', readinessLabel: 'Blocked', title: 'Prepare For Review', description: 'Prepare an analyzable candidate without mutating the report.' },
            { id: 'handoff', label: 'Review Design', status: 'blocked', readinessLabel: 'Blocked', title: 'Review Design', description: 'Launch Analyzer Workspace explicitly.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Draft Studio',
            description: 'Another stale summary.',
          },
          approvalCards: [
            {
              kind: 'designApproval',
              title: 'Design Approval',
              approvalState: 'notSubmitted',
              owner: 'Design Studio',
              unlock: 'Allows the next design stage to proceed.',
              nonEffects: ['Does not validate the report.', 'Does not materialize the draft.'],
            },
          ],
          conceptReview: {
            title: 'Concept Review Artifacts',
            summary: 'Review the chapter structure, KPI hierarchy, navigation path, and analytical flow before Draft Studio work begins.',
            selectedConceptLabel: 'Narrative-first storyline',
            chapterStructure: [{ title: 'Story setup', objective: 'Frame the narrative and the stakes.' }],
            kpiHierarchy: [{ label: 'Churn rate', level: 'primary', depth: 0 }],
            navigationStructure: [{ label: 'Story', depth: 0 }],
            analyticalFlow: [{ label: 'Frame the story', objective: 'Explain the stakes before details.' }],
            alternateConcepts: [
              {
                id: 'concept-operating-rhythm',
                label: 'Operating-rhythm command deck',
                summary: 'Leads with the operating KPI and then branches into intervention pages.',
                chapterMap: { chapters: [{ id: 'chapter-1', title: 'Decision priorities', objective: 'Show what needs intervention first.', pageRecommendationIds: ['page-1'] }] },
                pageRecommendations: [{ id: 'page-1', title: 'Overview', objective: 'Summarize the decision KPI.', chapterId: 'chapter-1', recommendedKpis: ['Churn rate'] }],
                kpiHierarchy: { nodes: [{ id: 'kpi-1', label: 'Churn rate', level: 'primary', childNodeIds: [] }], supportingDimensions: ['Segment'] },
                navigationStructure: { pattern: 'hubAndSpoke', rationale: 'Keeps the top-level action path tight.', sections: [{ id: 'nav-1', label: 'Priorities', pageRecommendationIds: ['page-1'] }] },
                analyticalFlow: { steps: [{ id: 'flow-1', label: 'Find the risk', objective: 'Spot the highest-risk segment.', pageRecommendationId: 'page-1' }] },
              },
              {
                id: 'concept-narrative',
                label: 'Narrative-first storyline',
                summary: 'Frames the business story first and then supports it with action pages.',
                chapterMap: { chapters: [{ id: 'chapter-2', title: 'Story setup', objective: 'Frame the narrative and the stakes.', pageRecommendationIds: ['page-2'] }] },
                pageRecommendations: [{ id: 'page-2', title: 'Narrative setup', objective: 'Explain the business objective and decision path.', chapterId: 'chapter-2', recommendedKpis: ['Churn rate'] }],
                kpiHierarchy: { nodes: [{ id: 'kpi-2', label: 'Churn rate', level: 'primary', childNodeIds: ['kpi-3'] }, { id: 'kpi-3', label: 'Decision confidence', level: 'diagnostic', childNodeIds: [] }], supportingDimensions: ['Segment'] },
                navigationStructure: { pattern: 'linearNarrative', rationale: 'Guides the user through a fixed story sequence.', sections: [{ id: 'nav-2', label: 'Story', pageRecommendationIds: ['page-2'] }] },
                analyticalFlow: { steps: [{ id: 'flow-2', label: 'Frame the story', objective: 'Explain the stakes before details.', pageRecommendationId: 'page-2' }] },
              },
            ],
            preferredBaselineConceptId: 'concept-narrative',
            comparison: {
              preferredConceptId: 'concept-narrative',
              summary: 'Baseline concept selected: Narrative-first storyline.',
              decisions: [
                { conceptId: 'concept-operating-rhythm', label: 'Operating-rhythm command deck', disposition: 'alternative' },
                { conceptId: 'concept-narrative', label: 'Narrative-first storyline', disposition: 'preferredBaseline' },
              ],
            },
          },
        },
      },
    }));

    expect(await screen.findByText('Submit the selected baseline for approval.')).toBeInTheDocument();
    expect(screen.getByText('Preferred baseline: Narrative-first storyline')).toBeInTheDocument();
    expect(screen.getByText('Narrative-first storyline vs Operating-rhythm command deck')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Submit Baseline For Approval' }));
    expect(postMessage).toHaveBeenCalledWith(expect.objectContaining({
      type: 'proposeArtifact',
      artifactKind: 'reportConcept',
      artifactId: 'report-concept:design-studio:active-report',
    }));

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        currentBrief: {
          id: 'design-brief:design-studio:active-report',
          threadId: 'design-studio:active-report',
          kind: 'designBrief',
          version: 3,
          lifecycleState: 'approved',
          approvalState: 'approved',
          approvalKind: 'designApproval',
          createdAt: '2026-06-16T12:00:00.000Z',
          updatedAt: '2026-06-16T12:10:00.000Z',
          authorSource: 'user',
          provenance: { source: 'user' },
          audience: 'Executive sponsors',
          businessObjective: 'Reduce churn risk',
          keyDecisions: ['Which segments need retention action'],
          primaryKpis: ['Churn rate'],
          dimensions: ['Segment'],
          intendedStory: 'Start with risk, then isolate causes.',
          successCriteria: ['Sponsor can focus the retention review quickly'],
          reportType: 'dashboard',
          navigationExpectations: 'Overview to segment detail.',
        },
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'concept',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'ready', readinessLabel: 'Ready', title: 'Concept Studio', description: 'Submit and approve the selected baseline.' },
            { id: 'draft', label: 'Draft Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Prepare For Review', status: 'blocked', readinessLabel: 'Blocked', title: 'Prepare For Review', description: 'Prepare an analyzable candidate without mutating the report.' },
            { id: 'handoff', label: 'Review Design', status: 'blocked', readinessLabel: 'Blocked', title: 'Review Design', description: 'Launch Analyzer Workspace explicitly.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Design Brief',
            description: 'Still stale.',
          },
          approvalCards: [
            {
              kind: 'designApproval',
              title: 'Design Approval',
              approvalState: 'pendingApproval',
              owner: 'Design Studio',
              unlock: 'Allows the next design stage to proceed.',
              nonEffects: ['Does not validate the report.', 'Does not materialize the draft.'],
            },
          ],
          conceptReview: {
            title: 'Concept Review Artifacts',
            summary: 'Review the chapter structure, KPI hierarchy, navigation path, and analytical flow before Draft Studio work begins.',
            selectedConceptLabel: 'Narrative-first storyline',
            chapterStructure: [{ title: 'Story setup', objective: 'Frame the narrative and the stakes.' }],
            kpiHierarchy: [{ label: 'Churn rate', level: 'primary', depth: 0 }],
            navigationStructure: [{ label: 'Story', depth: 0 }],
            analyticalFlow: [{ label: 'Frame the story', objective: 'Explain the stakes before details.' }],
            alternateConcepts: [
              {
                id: 'concept-operating-rhythm',
                label: 'Operating-rhythm command deck',
                summary: 'Leads with the operating KPI and then branches into intervention pages.',
                chapterMap: { chapters: [{ id: 'chapter-1', title: 'Decision priorities', objective: 'Show what needs intervention first.', pageRecommendationIds: ['page-1'] }] },
                pageRecommendations: [{ id: 'page-1', title: 'Overview', objective: 'Summarize the decision KPI.', chapterId: 'chapter-1', recommendedKpis: ['Churn rate'] }],
                kpiHierarchy: { nodes: [{ id: 'kpi-1', label: 'Churn rate', level: 'primary', childNodeIds: [] }], supportingDimensions: ['Segment'] },
                navigationStructure: { pattern: 'hubAndSpoke', rationale: 'Keeps the top-level action path tight.', sections: [{ id: 'nav-1', label: 'Priorities', pageRecommendationIds: ['page-1'] }] },
                analyticalFlow: { steps: [{ id: 'flow-1', label: 'Find the risk', objective: 'Spot the highest-risk segment.', pageRecommendationId: 'page-1' }] },
              },
              {
                id: 'concept-narrative',
                label: 'Narrative-first storyline',
                summary: 'Frames the business story first and then supports it with action pages.',
                chapterMap: { chapters: [{ id: 'chapter-2', title: 'Story setup', objective: 'Frame the narrative and the stakes.', pageRecommendationIds: ['page-2'] }] },
                pageRecommendations: [{ id: 'page-2', title: 'Narrative setup', objective: 'Explain the business objective and decision path.', chapterId: 'chapter-2', recommendedKpis: ['Churn rate'] }],
                kpiHierarchy: { nodes: [{ id: 'kpi-2', label: 'Churn rate', level: 'primary', childNodeIds: ['kpi-3'] }, { id: 'kpi-3', label: 'Decision confidence', level: 'diagnostic', childNodeIds: [] }], supportingDimensions: ['Segment'] },
                navigationStructure: { pattern: 'linearNarrative', rationale: 'Guides the user through a fixed story sequence.', sections: [{ id: 'nav-2', label: 'Story', pageRecommendationIds: ['page-2'] }] },
                analyticalFlow: { steps: [{ id: 'flow-2', label: 'Frame the story', objective: 'Explain the stakes before details.', pageRecommendationId: 'page-2' }] },
              },
            ],
            preferredBaselineConceptId: 'concept-narrative',
            comparison: {
              preferredConceptId: 'concept-narrative',
              summary: 'Baseline concept selected: Narrative-first storyline.',
              decisions: [
                { conceptId: 'concept-operating-rhythm', label: 'Operating-rhythm command deck', disposition: 'alternative' },
                { conceptId: 'concept-narrative', label: 'Narrative-first storyline', disposition: 'preferredBaseline' },
              ],
            },
            conceptId: 'report-concept:design-studio:active-report',
            approvalState: 'pendingApproval',
          },
        },
      },
    }));

    expect(await screen.findByText('Approve the concept baseline to unlock Draft Studio.')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Approve Concept Baseline' }));
    expect(postMessage).toHaveBeenCalledWith(expect.objectContaining({
      type: 'approveArtifact',
      artifactKind: 'reportConcept',
      artifactId: 'report-concept:design-studio:active-report',
    }));

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        currentBrief: {
          id: 'design-brief:design-studio:active-report',
          threadId: 'design-studio:active-report',
          kind: 'designBrief',
          version: 3,
          lifecycleState: 'approved',
          approvalState: 'approved',
          approvalKind: 'designApproval',
          createdAt: '2026-06-16T12:00:00.000Z',
          updatedAt: '2026-06-16T12:10:00.000Z',
          authorSource: 'user',
          provenance: { source: 'user' },
          audience: 'Executive sponsors',
          businessObjective: 'Reduce churn risk',
          keyDecisions: ['Which segments need retention action'],
          primaryKpis: ['Churn rate'],
          dimensions: ['Segment'],
          intendedStory: 'Start with risk, then isolate causes.',
          successCriteria: ['Sponsor can focus the retention review quickly'],
          reportType: 'dashboard',
          navigationExpectations: 'Overview to segment detail.',
        },
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'draft',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'approved', readinessLabel: 'Approved', title: 'Concept Studio', description: 'Selected baseline approved.' },
            { id: 'draft', label: 'Draft Studio', status: 'ready', readinessLabel: 'Ready', title: 'Draft Studio', description: 'Draft Studio is now unlocked.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'blocked', readinessLabel: 'Blocked', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Prepare For Review', status: 'blocked', readinessLabel: 'Blocked', title: 'Prepare For Review', description: 'Prepare an analyzable candidate without mutating the report.' },
            { id: 'handoff', label: 'Review Design', status: 'blocked', readinessLabel: 'Blocked', title: 'Review Design', description: 'Launch Analyzer Workspace explicitly.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Design Brief',
            description: 'Still stale.',
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
          ],
          conceptReview: {
            title: 'Concept Review Artifacts',
            summary: 'Review the chapter structure, KPI hierarchy, navigation path, and analytical flow before Draft Studio work begins.',
            selectedConceptLabel: 'Narrative-first storyline',
            chapterStructure: [{ title: 'Story setup', objective: 'Frame the narrative and the stakes.' }],
            kpiHierarchy: [{ label: 'Churn rate', level: 'primary', depth: 0 }],
            navigationStructure: [{ label: 'Story', depth: 0 }],
            analyticalFlow: [{ label: 'Frame the story', objective: 'Explain the stakes before details.' }],
            alternateConcepts: [],
            preferredBaselineConceptId: 'concept-narrative',
            approvedBaselineConceptId: 'concept-narrative',
            conceptId: 'report-concept:design-studio:active-report',
            approvalState: 'approved',
          },
        },
      },
    }));

    expect(await screen.findByText('Concept baseline approved. Continue to Draft Studio.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Draft Studio/ })).not.toHaveAttribute('aria-disabled', 'true');
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
        currentBrief: {
          id: 'design-brief:design-studio:active-report',
          threadId: 'design-studio:active-report',
          kind: 'designBrief',
          version: 3,
          lifecycleState: 'approved',
          approvalState: 'approved',
          approvalKind: 'designApproval',
          createdAt: '2026-06-16T12:00:00.000Z',
          updatedAt: '2026-06-16T12:10:00.000Z',
          authorSource: 'user',
          provenance: { source: 'user' },
          audience: 'Executive sponsors',
          businessObjective: 'Reduce churn risk',
          keyDecisions: ['Which segments need retention action'],
          primaryKpis: ['Revenue'],
          dimensions: ['Region'],
          intendedStory: 'Start with the business question and move into action.',
          successCriteria: ['Leader can identify where intervention is needed first'],
          reportType: 'dashboard',
          navigationExpectations: 'Executive Summary first, then Regional Analysis and Store Detail.',
        },
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
            conceptId: 'report-concept:design-studio:active-report',
            approvalState: 'approved',
            selectedConceptLabel: 'Narrative-first storyline',
            preferredBaselineConceptId: 'concept-narrative',
            approvedBaselineConceptId: 'concept-narrative',
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
            alternateConcepts: [
              {
                id: 'concept-narrative',
                label: 'Narrative-first storyline',
                summary: 'Frames the business question first and then supports it with action pages.',
                chapterMap: { chapters: [{ id: 'chapter-1', title: 'Executive Summary', objective: 'Open with the main business question.', pageRecommendationIds: ['page-1'] }] },
                pageRecommendations: [{ id: 'page-1', title: 'Executive Summary', objective: 'Where is revenue underperforming?', chapterId: 'chapter-1', recommendedKpis: ['Revenue'] }],
                kpiHierarchy: { nodes: [{ id: 'kpi-1', label: 'Revenue', level: 'primary', childNodeIds: ['kpi-2'] }, { id: 'kpi-2', label: 'Margin', level: 'supporting', childNodeIds: ['kpi-3'] }, { id: 'kpi-3', label: 'Forecast Accuracy', level: 'diagnostic', childNodeIds: [] }], supportingDimensions: ['Region'] },
                navigationStructure: { pattern: 'linearNarrative', rationale: 'Move through the story in order.', sections: [{ id: 'nav-1', label: 'Executive Summary', pageRecommendationIds: ['page-1'] }, { id: 'nav-2', label: 'Store Detail', pageRecommendationIds: ['page-1'] }] },
                analyticalFlow: { steps: [{ id: 'flow-1', label: 'Question', objective: 'Where is revenue underperforming?', pageRecommendationId: 'page-1' }, { id: 'flow-2', label: 'Investigation', objective: 'Compare regions and segments.', pageRecommendationId: 'page-1' }, { id: 'flow-3', label: 'Conclusion', objective: 'Identify where intervention is needed first.', pageRecommendationId: 'page-1' }] },
              },
              {
                id: 'concept-operating-rhythm',
                label: 'Operating-rhythm command deck',
                summary: 'Leads with decision priorities and action zones.',
                chapterMap: { chapters: [{ id: 'chapter-2', title: 'Decision priorities', objective: 'Surface the first intervention points.', pageRecommendationIds: ['page-2'] }] },
                pageRecommendations: [{ id: 'page-2', title: 'Action Queue', objective: 'Rank the next interventions.', chapterId: 'chapter-2', recommendedKpis: ['Revenue'] }],
                kpiHierarchy: { nodes: [{ id: 'kpi-4', label: 'Revenue', level: 'primary', childNodeIds: ['kpi-5'] }, { id: 'kpi-5', label: 'Intervention Rate', level: 'supporting', childNodeIds: [] }], supportingDimensions: ['Region'] },
                navigationStructure: { pattern: 'hubAndSpoke', rationale: 'Lead with priorities first.', sections: [{ id: 'nav-3', label: 'Priorities', pageRecommendationIds: ['page-2'] }, { id: 'nav-4', label: 'Action Queue', pageRecommendationIds: ['page-2'] }] },
                analyticalFlow: { steps: [{ id: 'flow-4', label: 'Question', objective: 'Where is revenue underperforming?', pageRecommendationId: 'page-2' }, { id: 'flow-5', label: 'Investigation', objective: 'Compare regions and segments.', pageRecommendationId: 'page-2' }, { id: 'flow-6', label: 'Evidence', objective: 'Support the recommendation with proof.', pageRecommendationId: 'page-2' }, { id: 'flow-7', label: 'Decision', objective: 'Pick the next intervention.', pageRecommendationId: 'page-2' }] },
              },
            ],
            comparison: {
              preferredConceptId: 'concept-narrative',
              summary: 'Baseline concept selected: Narrative-first storyline.',
              decisions: [
                { conceptId: 'concept-narrative', label: 'Narrative-first storyline', disposition: 'preferredBaseline' },
                { conceptId: 'concept-operating-rhythm', label: 'Operating-rhythm command deck', disposition: 'alternative' },
              ],
            },
          },
        },
      },
    }));

    expect(await screen.findByText('Prepare For Review')).toBeInTheDocument();
    expect(screen.getByText('Review Design')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Concept Studio execution' })).toBeInTheDocument();
    expect(screen.getByText('Concept baseline approved. Continue to Draft Studio.')).toBeInTheDocument();
    expect(screen.getByText('Preferred baseline: Narrative-first storyline')).toBeInTheDocument();
    expect(screen.getByText('Narrative-first storyline vs Operating-rhythm command deck')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'KPI Hierarchy Comparison' })).toBeInTheDocument();
    expect(screen.getAllByText('Forecast Accuracy').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'Navigation Structure Comparison' })).toBeInTheDocument();
    expect(screen.getAllByText('Store Detail').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'Analytical Flow Comparison' })).toBeInTheDocument();
    expect(screen.getAllByText('Where is revenue underperforming?').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'Chapter Structure Comparison' })).toBeInTheDocument();
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
