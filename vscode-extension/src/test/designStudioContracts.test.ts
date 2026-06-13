import * as fs from 'fs';
import * as path from 'path';
import {
  createSourceArtifactLineageEntry,
  DESIGN_STUDIO_APPROVAL_KINDS,
  DESIGN_STUDIO_APPROVAL_STATES,
  DESIGN_STUDIO_ARTIFACT_KINDS,
  DESIGN_STUDIO_LIFECYCLE_STATES,
  DESIGN_STUDIO_REPORT_TYPES,
  DESIGN_STUDIO_REQUIRED_BRIEF_FIELDS,
  REFINEMENT_ANALYZER_SOURCES,
  validateDesignBrief,
} from '../design-studio/contracts/designStudioModels';
import {
  DESIGN_STUDIO_HOST_MESSAGE_TYPES,
  DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
  DESIGN_STUDIO_PROTOCOL_VERSION,
  DESIGN_STUDIO_WEBVIEW_MESSAGE_TYPES,
} from '../design-studio/contracts/designStudioProtocol';

function readRepoFile(relativePath: string): string {
  return fs.readFileSync(path.join(__dirname, '..', relativePath), 'utf8');
}

describe('designStudio contracts', () => {
  it('defines the required artifact vocabulary as internal studio concepts', () => {
    expect(DESIGN_STUDIO_ARTIFACT_KINDS).toEqual([
      'designBrief',
      'reportConcept',
      'pageConcept',
      'navigationConcept',
      'kpiHierarchyConcept',
      'draftReportArtifact',
      'draftPageArtifact',
      'draftLayoutArtifact',
      'draftNavigationArtifact',
      'refinementProposal',
      'materializationRequest',
      'materializedSurfaceCandidate',
      'designIterationRecord',
    ]);
  });

  it('uses lifecycle and approval vocabularies that stay separate from analyzer promotion state', () => {
    expect(DESIGN_STUDIO_LIFECYCLE_STATES).toEqual([
      'draft',
      'proposed',
      'reviewed',
      'approved',
      'materialized',
      'analyzed',
      'superseded',
      'archived',
    ]);
    expect(DESIGN_STUDIO_APPROVAL_STATES).toEqual([
      'notSubmitted',
      'pendingApproval',
      'approved',
      'rejected',
    ]);
    expect(DESIGN_STUDIO_APPROVAL_KINDS).toEqual([
      'designApproval',
      'refinementApproval',
      'validationApproval',
      'materializationApproval',
    ]);
    expect(DESIGN_STUDIO_LIFECYCLE_STATES).not.toContain('promoted');
    expect(DESIGN_STUDIO_APPROVAL_STATES).not.toContain('promoted');
  });

  it('defines the required design brief fields and report types', () => {
    expect(DESIGN_STUDIO_REQUIRED_BRIEF_FIELDS).toEqual([
      'audience',
      'businessObjective',
      'keyDecisions',
      'primaryKpis',
      'dimensions',
      'intendedStory',
      'successCriteria',
      'reportType',
      'navigationExpectations',
    ]);
    expect(DESIGN_STUDIO_REPORT_TYPES).toContain('dashboard');
  });

  it('rejects concept generation prerequisites until a brief is valid and approved', () => {
    const invalidResult = validateDesignBrief({
      id: 'brief-1',
      threadId: 'thread-1',
      kind: 'designBrief',
      version: 1,
      lifecycleState: 'draft',
      approvalState: 'notSubmitted',
      approvalKind: 'designApproval',
      createdAt: '2026-06-12T00:00:00.000Z',
      updatedAt: '2026-06-12T00:00:00.000Z',
      authorSource: 'user',
      provenance: { source: 'user' },
      audience: '',
      businessObjective: '',
      keyDecisions: [],
      primaryKpis: [],
      dimensions: [],
      intendedStory: '',
      successCriteria: [],
      reportType: 'dashboard',
      navigationExpectations: '',
      consumptionContext: undefined,
      decisionCadence: undefined,
      narrativeRisksOrConstraints: undefined,
      requiredEvidenceDomains: undefined,
      targetAnalyzableSurfaceFamily: undefined,
    });

    expect(invalidResult.isValid).toBe(false);
    expect(invalidResult.canGenerateConcepts).toBe(false);
    expect(invalidResult.errors.map((error) => error.field)).toEqual(
      expect.arrayContaining(['audience', 'businessObjective', 'keyDecisions', 'primaryKpis', 'dimensions', 'intendedStory', 'successCriteria', 'navigationExpectations']),
    );

    const approvedResult = validateDesignBrief({
      id: 'brief-1',
      threadId: 'thread-1',
      kind: 'designBrief',
      version: 1,
      lifecycleState: 'approved',
      approvalState: 'approved',
      approvalKind: 'designApproval',
      createdAt: '2026-06-12T00:00:00.000Z',
      updatedAt: '2026-06-12T00:00:00.000Z',
      authorSource: 'user',
      provenance: { source: 'user' },
      audience: 'Sales leaders',
      businessObjective: 'Reduce missed renewals',
      keyDecisions: ['Which regions need intervention now'],
      primaryKpis: ['Renewal rate'],
      dimensions: ['Region'],
      intendedStory: 'Start with risk, then isolate drivers.',
      successCriteria: ['Escalations happen from one page'],
      reportType: 'dashboard',
      navigationExpectations: 'Overview first, detail second.',
      consumptionContext: 'Weekly revenue review',
      decisionCadence: 'Weekly',
      narrativeRisksOrConstraints: ['Avoid overstating single-week anomalies'],
      requiredEvidenceDomains: ['trend', 'segment comparison'],
      targetAnalyzableSurfaceFamily: 'pbir',
    });

    expect(approvedResult.isValid).toBe(true);
    expect(approvedResult.canGenerateConcepts).toBe(true);
  });

  it('keeps design approval semantics separate from future validation and materialization approval', () => {
    expect(DESIGN_STUDIO_APPROVAL_KINDS).toContain('designApproval');
    expect(DESIGN_STUDIO_APPROVAL_KINDS).toContain('validationApproval');
    expect(DESIGN_STUDIO_APPROVAL_KINDS).toContain('materializationApproval');
    expect(DESIGN_STUDIO_APPROVAL_KINDS).not.toEqual(['approved']);
  });

  it('builds immutable source lineage entries with exact version and approval metadata', () => {
    const lineage = createSourceArtifactLineageEntry({
      id: 'draft-page:thread-1:page-1',
      kind: 'draftPageArtifact',
      version: 4,
      approvalState: 'approved',
      updatedAt: '2026-06-13T12:00:00.000Z',
    }, { sourceRole: 'primary' });

    expect(lineage).toEqual({
      artifactId: 'draft-page:thread-1:page-1',
      artifactKind: 'draftPageArtifact',
      artifactVersionId: 'draft-page:thread-1:page-1@v4',
      sourceRole: 'primary',
      approvalState: 'approved',
      approvalTimestamp: '2026-06-13T12:00:00.000Z',
    });
  });

  it('defines refinement analyzer sources as advisory ingestion inputs only', () => {
    expect(REFINEMENT_ANALYZER_SOURCES).toEqual([
      'storyAssessment',
      'guidedStoryImprovements',
      'issues',
      'fixPlan',
      'crossPageNarrative',
    ]);
  });

  it('defines an internal-only studio protocol without apply or deploy authority', () => {
    expect(DESIGN_STUDIO_PROTOCOL_VERSION).toBe(1);
    expect(DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION).toBe(1);
    expect(DESIGN_STUDIO_HOST_MESSAGE_TYPES).toEqual([
      'studioState',
      'artifactSaved',
      'artifactProposed',
      'artifactApproved',
      'materializationRequested',
      'iterationComparison',
      'analyzerHandoffOpened',
    ]);
    expect(DESIGN_STUDIO_WEBVIEW_MESSAGE_TYPES).toEqual([
      'webviewReady',
      'loadStudioState',
      'saveArtifact',
      'proposeArtifact',
      'approveArtifact',
      'requestMaterialization',
      'compareIterations',
      'openAnalyzerHandoff',
    ]);

    const protocolText = JSON.stringify({
      host: DESIGN_STUDIO_HOST_MESSAGE_TYPES,
      webview: DESIGN_STUDIO_WEBVIEW_MESSAGE_TYPES,
    });

    expect(protocolText).not.toContain('apply');
    expect(protocolText).not.toContain('deploy');
    expect(protocolText).not.toContain('publish');
  });

  it('does not widen the score-panel, Story Assessment, surface, or analyzer contracts', () => {
    const scorePanelContract = readRepoFile('analyzer/contracts/scorePanel.ts');
    const surfaceTypes = readRepoFile('analyzer/surfaces/types.ts');
    const analyzerTypes = readRepoFile('analyzer/analyzers/types.ts');

    expect(scorePanelContract).not.toContain('DesignStudio');
    expect(scorePanelContract).not.toContain('DesignBrief');
    expect(scorePanelContract).not.toContain('MaterializationRequest');
    expect(scorePanelContract).not.toContain('MaterializedSurfaceCandidate');
    expect(scorePanelContract).not.toContain('RefinementProposal');
    expect(scorePanelContract).not.toContain('ReportChapterMapConcept');
    expect(scorePanelContract).not.toContain('PageRecommendationConcept');
    expect(scorePanelContract).not.toContain('AnalyticalFlowConcept');
    expect(scorePanelContract).not.toContain('AlternateConceptComparison');

    expect(surfaceTypes).not.toContain('designStudio');
    expect(surfaceTypes).not.toContain('designBrief');
    expect(surfaceTypes).not.toContain('reportConcept');
    expect(surfaceTypes).not.toContain('conceptStudio');
    expect(analyzerTypes).not.toContain('designStudio');
    expect(analyzerTypes).not.toContain('conceptStudio');
  });
});
