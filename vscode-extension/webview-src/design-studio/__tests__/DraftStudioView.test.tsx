import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { DraftStudioView } from '../views/DraftStudioView';

describe('DraftStudioView', () => {
  it('renders draft pages, layouts, navigation, and KPI placement as reviewable artifacts', () => {
    render(
      <DraftStudioView
        canGenerateDrafts={true}
        currentDraft={{
          id: 'draft-report:1',
          threadId: 'thread-1',
          kind: 'draftReportArtifact',
          version: 2,
          lifecycleState: 'approved',
          approvalState: 'approved',
          approvalKind: 'designApproval',
          createdAt: '2026-06-14T12:00:00.000Z',
          updatedAt: '2026-06-14T12:00:00.000Z',
          authorSource: 'system',
          provenance: { source: 'system' },
          briefId: 'design-brief:1',
          conceptId: 'report-concept:1',
          sourceBriefVersionId: 'design-brief:1@v2',
          sourceConceptVersionId: 'report-concept:1@v3',
          sourceNavigationConceptVersionId: 'navigation-concept:1@v3',
          pageArtifactIds: ['draft-page:1', 'draft-page:2'],
          layoutArtifactIds: ['draft-layout:1', 'draft-layout:2'],
          navigationArtifactIds: ['draft-navigation:1'],
          summary: 'Executive dashboard draft',
          draftStatus: {
            isolation: 'isolated',
            reviewability: 'reviewable',
            productionState: 'nonProduction',
          },
        }}
        pageArtifacts={[
          {
            id: 'draft-page:1',
            threadId: 'thread-1',
            kind: 'draftPageArtifact',
            version: 2,
            lifecycleState: 'approved',
            approvalState: 'approved',
            approvalKind: 'designApproval',
            createdAt: '2026-06-14T12:00:00.000Z',
            updatedAt: '2026-06-14T12:00:00.000Z',
            authorSource: 'system',
            provenance: { source: 'system' },
            draftReportArtifactId: 'draft-report:1',
            pageConceptId: 'page-concept:overview',
            sourceBriefVersionId: 'design-brief:1@v2',
            sourceConceptVersionId: 'report-concept:1@v3',
            sourcePageConceptVersionId: 'page-concept:overview@v3',
            structureSummary: 'Executive Summary page with KPI row and risk narrative.',
            recommendedVisualRoles: ['Revenue', 'Margin'],
            draftStatus: {
              isolation: 'isolated',
              reviewability: 'reviewable',
              productionState: 'nonProduction',
            },
          },
        ]}
        layoutArtifacts={[
          {
            id: 'draft-layout:1',
            threadId: 'thread-1',
            kind: 'draftLayoutArtifact',
            version: 2,
            lifecycleState: 'approved',
            approvalState: 'approved',
            approvalKind: 'designApproval',
            createdAt: '2026-06-14T12:00:00.000Z',
            updatedAt: '2026-06-14T12:00:00.000Z',
            authorSource: 'system',
            provenance: { source: 'system' },
            draftPageArtifactId: 'draft-page:1',
            pageConceptId: 'page-concept:overview',
            sourceBriefVersionId: 'design-brief:1@v2',
            sourceConceptVersionId: 'report-concept:1@v3',
            sourcePageConceptVersionId: 'page-concept:overview@v3',
            layoutType: 'kpiGrid',
            title: 'Executive KPI layout',
            kpiBindings: ['Revenue', 'Margin'],
            zones: ['Top row', 'Narrative panel'],
            draftStatus: {
              isolation: 'isolated',
              reviewability: 'reviewable',
              productionState: 'nonProduction',
            },
          },
        ]}
        navigationArtifacts={[
          {
            id: 'draft-navigation:1',
            threadId: 'thread-1',
            kind: 'draftNavigationArtifact',
            version: 2,
            lifecycleState: 'approved',
            approvalState: 'approved',
            approvalKind: 'designApproval',
            createdAt: '2026-06-14T12:00:00.000Z',
            updatedAt: '2026-06-14T12:00:00.000Z',
            authorSource: 'system',
            provenance: { source: 'system' },
            draftReportArtifactId: 'draft-report:1',
            navigationConceptId: 'navigation-concept:1',
            sourceBriefVersionId: 'design-brief:1@v2',
            sourceConceptVersionId: 'report-concept:1@v3',
            sourceNavigationConceptVersionId: 'navigation-concept:1@v3',
            frameworkType: 'guidedFlow',
            sections: [
              { id: 'nav-1', label: 'Executive Summary', pageArtifactId: 'draft-page:1', pageConceptId: 'page-concept:overview' },
            ],
            draftStatus: {
              isolation: 'isolated',
              reviewability: 'reviewable',
              productionState: 'nonProduction',
            },
          },
        ]}
        providerCapabilities={[]}
        onGenerateDrafts={() => undefined}
      />,
    );

    expect(screen.getByRole('heading', { name: 'Draft Pages' })).toBeInTheDocument();
    expect(screen.getByText('Executive Summary page with KPI row and risk narrative.')).toBeInTheDocument();
    expect(screen.getByText('Revenue, Margin')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Draft Layouts' })).toBeInTheDocument();
    expect(screen.getByText('Executive KPI layout')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Draft Navigation' })).toBeInTheDocument();
    expect(screen.getByText('Executive Summary')).toBeInTheDocument();
  });
});
