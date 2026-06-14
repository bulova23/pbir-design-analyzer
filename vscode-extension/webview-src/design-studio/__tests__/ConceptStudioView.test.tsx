import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { createConceptStudioState, conceptStudioReducer } from '../state/conceptStudioReducer';
import { ConceptStudioView } from '../views/ConceptStudioView';

describe('ConceptStudioView', () => {
  it('blocks concept generation until the brief is approved', () => {
    const onGenerateConcepts = jest.fn();
    const onSelectBaseline = jest.fn();
    const onApproveBaseline = jest.fn();
    const state = createConceptStudioState();

    render(
      <ConceptStudioView
        state={state}
        dispatch={() => undefined}
        onGenerateConcepts={onGenerateConcepts}
        onSelectBaseline={onSelectBaseline}
        onApproveBaseline={onApproveBaseline}
      />,
    );

    expect(screen.getByRole('button', { name: 'Generate Concepts' })).toBeDisabled();
    expect(screen.getByText('Concept generation is blocked until the Design Brief is approved.')).toBeInTheDocument();
  });

  it('supports alternate concept comparison and explicit baseline selection without materialization controls', () => {
    const onGenerateConcepts = jest.fn();
    const onSelectBaseline = jest.fn();
    const onApproveBaseline = jest.fn();
    let state = createConceptStudioState();
    const dispatch = (action: Parameters<typeof conceptStudioReducer>[1]) => {
      state = conceptStudioReducer(state, action);
      rerenderView();
    };

    const { rerender } = render(
      <ConceptStudioView
        state={state}
        dispatch={dispatch}
        onGenerateConcepts={onGenerateConcepts}
        onSelectBaseline={onSelectBaseline}
        onApproveBaseline={onApproveBaseline}
      />,
    );

    function rerenderView(): void {
      rerender(
        <ConceptStudioView
          state={state}
          dispatch={dispatch}
          onGenerateConcepts={onGenerateConcepts}
          onSelectBaseline={onSelectBaseline}
          onApproveBaseline={onApproveBaseline}
        />,
      );
    }

    dispatch({ type: 'setBriefApproval', approvalState: 'approved' });
    fireEvent.click(screen.getByRole('button', { name: 'Generate Concepts' }));

    expect(onGenerateConcepts).toHaveBeenCalled();
    expect(screen.getByText('Concept alternatives')).toBeInTheDocument();
    expect(screen.getByText('Concept comparison')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Chapter Structure Comparison' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'KPI Hierarchy Comparison' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Navigation Structure Comparison' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Analytical Flow Comparison' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /materialize/i })).not.toBeInTheDocument();
    expect(screen.getByText('Concept Studio produces internal concept artifacts only. No PBIR assets or analyzable surfaces are created here.')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Choose Narrative-first storyline' }));

    expect(onSelectBaseline).toHaveBeenCalledWith('concept-narrative');
    expect(screen.getByText('Preferred baseline: Narrative-first storyline')).toBeInTheDocument();
    expect(screen.getByText('Narrative-first storyline vs Operating-rhythm command deck')).toBeInTheDocument();
    expect(screen.getByText('Story setup')).toBeInTheDocument();
    expect(screen.getByText('Decision priorities')).toBeInTheDocument();
    expect(screen.getByText('Question')).toBeInTheDocument();
    expect(screen.getByText('Investigation')).toBeInTheDocument();
    expect(screen.getByText('Evidence')).toBeInTheDocument();
    expect(screen.getByText('Conclusion')).toBeInTheDocument();
    expect(screen.getByText('Selected baseline stays internal to Concept Studio until a future explicit materialization step.')).toBeInTheDocument();
    expect(screen.getByText('Draft Studio approval: Not approved')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Approve for Draft Studio' })).toBeEnabled();
    expect(onApproveBaseline).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: 'Approve for Draft Studio' }));

    expect(onApproveBaseline).toHaveBeenCalledWith('concept-narrative');
    expect(screen.getByText('Draft Studio approval: Approved')).toBeInTheDocument();
  });
});
