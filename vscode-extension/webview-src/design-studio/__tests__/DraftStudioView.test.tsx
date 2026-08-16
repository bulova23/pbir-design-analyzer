import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { DraftStudioView } from '../views/DraftStudioView';

describe('DraftStudioView', () => {
  it('renders draft pages, layouts, navigation, and KPI placement as reviewable artifacts', () => {
    render(
      <DraftStudioView
        canGenerateDrafts={true}
        draftReview={{
          draftId: 'draft-report:1',
          approvalState: 'approved',
          title: 'Draft Review Artifacts',
          summary: 'Review the designed pages, layouts, navigation, and KPI placement before approval.',
          draftStatusLabel: 'Approved draft',
          draftPages: [
            {
              title: 'Executive Summary',
              structureSummary: 'Executive Summary page with KPI row and risk narrative.',
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
        }}
        onGenerateDrafts={() => undefined}
        onSubmitDraftForApproval={() => undefined}
        onApproveDraft={() => undefined}
      />,
    );

    expect(screen.getByRole('heading', { name: 'Draft Pages' })).toBeInTheDocument();
    expect(screen.getByText('Executive Summary page with KPI row and risk narrative.')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Draft Layouts' })).toBeInTheDocument();
    expect(screen.getByText('Executive KPI layout')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Draft Navigation' })).toBeInTheDocument();
    expect(screen.getAllByText('Executive Summary').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'KPI Placement' })).toBeInTheDocument();
    expect(screen.getByText('Revenue, Margin')).toBeInTheDocument();
  });
});
