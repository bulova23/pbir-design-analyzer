import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { createInitialDesignBriefState, designBriefReducer } from '../state/designBriefReducer';
import { DesignBriefView } from '../views/DesignBriefView';

describe('DesignBriefView', () => {
  it('renders draft workflow guidance and blocks approval actions until the brief is ready', () => {
    const onSave = jest.fn();
    const onSubmitForApproval = jest.fn();
    const onApprove = jest.fn();
    const state = createInitialDesignBriefState();

    render(
      <DesignBriefView
        state={state}
        dispatch={() => undefined}
        onSave={onSave}
        onSubmitForApproval={onSubmitForApproval}
        onApprove={onApprove}
      />,
    );

    expect(screen.getByText('Complete required fields to continue.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save Draft' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Submit For Approval' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Approve Brief' })).toBeDisabled();
    expect(screen.getByRole('heading', { name: 'Start with the essentials' })).toBeInTheDocument();
    expect(screen.queryByLabelText('Required Evidence Domains')).not.toBeInTheDocument();
  });

  it('shows validation errors, saves edits, supports submission, and locks the approved brief', () => {
    const onSave = jest.fn();
    const onSubmitForApproval = jest.fn();
    const onApprove = jest.fn();

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
        onSubmitForApproval={onSubmitForApproval}
        onApprove={onApprove}
      />,
    );

    function rerenderView(): void {
      rerender(
        <DesignBriefView
          state={state}
          dispatch={dispatch}
          onSave={onSave}
          onSubmitForApproval={onSubmitForApproval}
          onApprove={onApprove}
        />,
      );
    }

    fireEvent.click(screen.getByRole('button', { name: 'Save Draft' }));
    expect(onSave).toHaveBeenCalled();
    expect(screen.getAllByText('Audience is required.').length).toBeGreaterThan(0);
    expect(screen.getByLabelText(/Audience/)).toHaveAttribute('aria-invalid', 'true');

    fireEvent.change(screen.getByLabelText(/Audience/), { target: { value: 'Executive sponsors' } });
    fireEvent.change(screen.getByLabelText(/Business Objective/), { target: { value: 'Reduce churn risk' } });
    fireEvent.change(screen.getByLabelText(/Key Decisions/), { target: { value: 'Which segments need retention action' } });
    fireEvent.change(screen.getByLabelText(/Primary KPIs/), { target: { value: 'Churn rate' } });
    fireEvent.change(screen.getByLabelText(/Dimensions/), { target: { value: 'Segment' } });
    fireEvent.change(screen.getByLabelText(/Intended Story/), { target: { value: 'Start with risk, then isolate causes.' } });
    fireEvent.change(screen.getByLabelText(/Success Criteria/), { target: { value: 'Sponsor can focus the retention review quickly' } });
    fireEvent.change(screen.getByLabelText(/Navigation Expectations/), { target: { value: 'Overview to segment detail.' } });
    fireEvent.click(screen.getByRole('button', { name: 'Show advanced brief details' }));
    expect(screen.getByRole('heading', { name: 'Advanced design context' })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText(/Consumption Context/), { target: { value: 'Weekly executive review' } });
    fireEvent.change(screen.getByLabelText(/Decision Cadence/), { target: { value: 'Weekly' } });
    fireEvent.change(screen.getByLabelText(/Report Type/), { target: { value: 'dashboard' } });
    fireEvent.change(screen.getByLabelText(/Required Evidence Domains/), { target: { value: 'Segment trend\nRetention risk' } });

    expect(screen.getByText('Ready for approval.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Submit For Approval' })).toBeEnabled();

    fireEvent.click(screen.getByRole('button', { name: 'Submit For Approval' }));
    expect(onSubmitForApproval).toHaveBeenCalled();
    expect(screen.getByText('Submitted for approval. Approve the brief to continue.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Approve Brief' })).toBeEnabled();

    fireEvent.click(screen.getByRole('button', { name: 'Approve Brief' }));
    expect(onApprove).toHaveBeenCalled();

    dispatch({ type: 'markApproved' });

    expect(screen.getByText('Design Brief approved. Continue to Concept Studio.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save Draft' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Submit For Approval' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Approve Brief' })).toBeDisabled();
    expect(screen.getByLabelText(/Audience/)).toBeDisabled();

    fireEvent.click(screen.getByRole('button', { name: 'Hide advanced brief details' }));
    expect(screen.queryByLabelText('Required Evidence Domains')).not.toBeInTheDocument();
  });
});
