import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { createInitialDesignBriefState, designBriefReducer } from '../state/designBriefReducer';
import { DesignBriefView } from '../views/DesignBriefView';

describe('DesignBriefView', () => {
  it('disables concept generation until the brief is valid and approved', () => {
    const onSave = jest.fn();
    const onApprove = jest.fn();
    const onGenerateConcepts = jest.fn();
    const state = createInitialDesignBriefState();

    render(
      <DesignBriefView
        state={state}
        dispatch={() => undefined}
        onSave={onSave}
        onApprove={onApprove}
        onGenerateConcepts={onGenerateConcepts}
      />,
    );

    expect(screen.getByRole('button', { name: 'Generate Concepts' })).toBeDisabled();
    expect(screen.getByText('Approval status: Not submitted')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Start with the essentials' })).toBeInTheDocument();
    expect(screen.queryByLabelText('Required Evidence Domains')).not.toBeInTheDocument();
  });

  it('shows validation errors, saves edits, and enables concept generation only after approval', () => {
    const onSave = jest.fn();
    const onApprove = jest.fn();
    const onGenerateConcepts = jest.fn();

    let state = createInitialDesignBriefState();
    const dispatch = (action: Parameters<typeof designBriefReducer>[1]) => {
      state = designBriefReducer(state, action);
      rerenderView();
    };

    const { rerender } = render(
      <DesignBriefView
        state={state}
        dispatch={dispatch}
        onSave={onSave}
        onApprove={onApprove}
        onGenerateConcepts={onGenerateConcepts}
      />,
    );

    function rerenderView(): void {
      rerender(
        <DesignBriefView
          state={state}
          dispatch={dispatch}
          onSave={onSave}
          onApprove={onApprove}
          onGenerateConcepts={onGenerateConcepts}
        />,
      );
    }

    fireEvent.click(screen.getByRole('button', { name: 'Save Brief' }));
    expect(onSave).toHaveBeenCalled();
    expect(screen.getByText('Audience is required.')).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Audience'), { target: { value: 'Executive sponsors' } });
    fireEvent.change(screen.getByLabelText('Business Objective'), { target: { value: 'Reduce churn risk' } });
    fireEvent.change(screen.getByLabelText('Key Decisions'), { target: { value: 'Which segments need retention action' } });
    fireEvent.change(screen.getByLabelText('Primary KPIs'), { target: { value: 'Churn rate' } });
    fireEvent.change(screen.getByLabelText('Intended Story'), { target: { value: 'Start with risk, then isolate causes.' } });
    fireEvent.change(screen.getByLabelText('Success Criteria'), { target: { value: 'Sponsor can focus the retention review quickly' } });
    fireEvent.click(screen.getByRole('button', { name: 'Show advanced brief details' }));
    expect(screen.getByRole('heading', { name: 'Advanced design context' })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Dimensions'), { target: { value: 'Segment' } });
    fireEvent.change(screen.getByLabelText('Navigation Expectations'), { target: { value: 'Overview to segment detail.' } });
    fireEvent.change(screen.getByLabelText('Consumption Context'), { target: { value: 'Weekly executive review' } });
    fireEvent.change(screen.getByLabelText('Decision Cadence'), { target: { value: 'Weekly' } });
    fireEvent.change(screen.getByLabelText('Report Type'), { target: { value: 'dashboard' } });
    fireEvent.change(screen.getByLabelText('Required Evidence Domains'), { target: { value: 'Segment trend, retention risk' } });

    fireEvent.click(screen.getByRole('button', { name: 'Request Approval' }));
    expect(onApprove).toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Generate Concepts' })).toBeDisabled();

    dispatch({ type: 'markApproved' });

    expect(screen.getByText('Approval status: Approved')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Generate Concepts' })).toBeEnabled();

    fireEvent.click(screen.getByRole('button', { name: 'Generate Concepts' }));
    expect(onGenerateConcepts).toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Hide advanced brief details' }));
    expect(screen.queryByLabelText('Required Evidence Domains')).not.toBeInTheDocument();
  });
});
