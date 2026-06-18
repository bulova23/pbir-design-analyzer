import * as crypto from 'crypto';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import {
  approveConceptBaseline,
  generateConceptArtifacts,
  selectConceptBaseline,
  submitConceptBaselineForApproval,
} from '../design-studio/state/conceptStore';
import {
  approveDesignBrief,
  saveDesignBriefDraft,
  submitDesignBriefForApproval,
} from '../design-studio/state/designBriefStore';
import {
  approveDraftArtifacts,
  generateDraftArtifacts,
  submitDraftForApproval,
} from '../design-studio/state/draftStore';
import {
  approveReviewCandidate,
  createReviewCandidate,
  loadPrepareForReviewState,
  submitReviewCandidateForApproval,
} from '../design-studio/state/prepareForReviewStore';

function makeContext(tmpDir: string): ExtensionContext {
  return {
    globalStorageUri: { fsPath: tmpDir },
    secrets: {
      get: jest.fn(),
      store: jest.fn(),
      delete: jest.fn(),
    },
  } as unknown as ExtensionContext;
}

function makeTempDir(): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-design-studio-prepare-for-review-test-'));
}

function createThreadId(reportPath: string): string {
  return `design-studio:${crypto.createHash('md5').update(reportPath).digest('hex').slice(0, 16)}`;
}

async function createApprovedDraftWorkflow(context: ExtensionContext, threadId: string) {
  await saveDesignBriefDraft(context, threadId, {
    audience: 'Sales leaders',
    businessObjective: 'Reduce missed renewals',
    keyDecisions: ['Which regions need intervention first'],
    primaryKpis: ['Renewal rate', 'Gross margin', 'Forecast accuracy'],
    dimensions: ['Region', 'Segment'],
    intendedStory: 'Lead with risk, then explain the main drivers and actions.',
    successCriteria: ['Leader can pick the next intervention within five minutes'],
    reportType: 'dashboard',
    navigationExpectations: 'Executive Summary first, then Regional Analysis and Store Detail.',
  });
  await submitDesignBriefForApproval(context, threadId);
  await approveDesignBrief(context, threadId);
  const conceptState = await generateConceptArtifacts(context, threadId);
  await selectConceptBaseline(context, threadId, conceptState.currentConcept.alternateConcepts[0].id);
  await submitConceptBaselineForApproval(context, threadId);
  await approveConceptBaseline(context, threadId);
  await generateDraftArtifacts(context, threadId);
  await submitDraftForApproval(context, threadId);
  return approveDraftArtifacts(context, threadId);
}

describe('prepareForReviewStore', () => {
  it('creates, submits, and approves a review candidate while preserving lineage and version history', async () => {
    const context = makeContext(makeTempDir());
    const reportPath = '/tmp/Prepare For Review Store.Report.pbir';
    const threadId = createThreadId(reportPath);
    const approvedDraft = await createApprovedDraftWorkflow(context, threadId);

    expect(await loadPrepareForReviewState(context, threadId)).toBeUndefined();

    const created = await createReviewCandidate(context, { threadId, reportPath });
    expect(created.currentRequest.approvalState).toBe('notSubmitted');
    expect(created.currentCandidate.approvalState).toBe('notSubmitted');
    expect(created.currentCandidate.sourceLineage).toEqual([
      expect.objectContaining({
        artifactId: approvedDraft.currentDraft.id,
        artifactVersionId: `${approvedDraft.currentDraft.id}@v${approvedDraft.currentDraft.version}`,
        approvalState: 'approved',
      }),
    ]);
    expect(created.currentCandidate.materializationDiagnostics).toEqual(expect.arrayContaining([
      'No PBIR files were created.',
      'No analyzer handoff was executed.',
      'No analyzer workspace was opened.',
      'No report mutation occurred.',
    ]));

    const submitted = await submitReviewCandidateForApproval(context, threadId);
    expect(submitted.currentRequest.approvalState).toBe('pendingApproval');
    expect(submitted.currentCandidate.approvalState).toBe('pendingApproval');
    expect(submitted.currentCandidate.version).toBe(created.currentCandidate.version + 1);
    expect(submitted.currentCandidate.sourceLineage).toEqual(created.currentCandidate.sourceLineage);

    const approved = await approveReviewCandidate(context, threadId);
    expect(approved.currentRequest.approvalState).toBe('approved');
    expect(approved.currentCandidate.approvalState).toBe('approved');
    expect(approved.currentCandidate.version).toBe(submitted.currentCandidate.version + 1);
    expect(approved.currentCandidate.sourceLineage).toEqual(submitted.currentCandidate.sourceLineage);
    expect(approved.history).toHaveLength(3);
  });
});
