import * as fs from 'fs';
import * as vscode from 'vscode';
import {
  buildGeneratePayload,
  buildRenamePagePayload,
  buildCuratedMutationPayload,
  formatCuratedMutationPreview,
  formatMutationPreview,
  formatAuthoringError,
  PbirAuthoringWorkflow,
  generationVersionForSchema,
} from '../services/rpc/PbirAuthoringWorkflow';
import { resetOutputChannelsForTesting } from '../platform/outputChannels';

describe('PbirAuthoringWorkflow', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    resetOutputChannelsForTesting();
  });

  it('maps only pinned typed generation request versions', () => {
    expect(generationVersionForSchema('local-pbir-generation-request/v7')).toBe('v7');
    expect(generationVersionForSchema('local-pbir-generation-request/v8')).toBeUndefined();
    expect(buildGeneratePayload({ schemaVersion: 'local-pbir-generation-request/v2', requestId: 'request' })).toEqual(expect.objectContaining({
      operation: 'generate',
      generate: { request: { v2: { schemaVersion: 'local-pbir-generation-request/v2', requestId: 'request' } } },
    }));
  });

  it('maps stable structured error categories to concise messages', () => {
    expect(formatAuthoringError({ error: { category: 'unsupportedAuthoring', code: 'PBIR', summary: 'bounded detail' } }))
      .toBe('Unsupported PBIR construct: bounded detail');
  });

  it('does not call the backend when the user cancels selection', async () => {
    (vscode.window.showOpenDialog as jest.Mock).mockResolvedValue(undefined);
    const executeAuthoringRequest = jest.fn();

    await new PbirAuthoringWorkflow(() => ({ executeAuthoringRequest })).generate();

    expect(executeAuthoringRequest).not.toHaveBeenCalled();
  });

  it('retains the generated opaque handle for the next analyze call', async () => {
    const filePath = `/tmp/pbir-authoring-${Date.now()}.json`;
    fs.writeFileSync(filePath, JSON.stringify({ schemaVersion: 'local-pbir-generation-request/v1' }));
    (vscode.window.showOpenDialog as jest.Mock).mockResolvedValue([{ fsPath: filePath }]);
    const executeAuthoringRequest = jest.fn()
      .mockResolvedValueOnce({
        succeeded: true,
        generateResult: { artifact: { schemaVersion: 'artifact', artifactId: 'artifact-1', artifactHash: 'hash', manifestId: 'manifest', manifestHash: 'manifest-hash' } },
      })
      .mockResolvedValueOnce({ succeeded: true, analyzer: { score: 91, pageCount: 1, visualCount: 1 } });
    const workflow = new PbirAuthoringWorkflow(() => ({ executeAuthoringRequest }));

    await workflow.generate();
    await workflow.analyze();

    expect(executeAuthoringRequest).toHaveBeenNthCalledWith(2, expect.objectContaining({
      operation: 'analyze',
      analyze: { artifact: { artifactId: 'artifact-1', artifactHash: 'hash', manifestId: 'manifest', manifestHash: 'manifest-hash', schemaVersion: 'artifact' } },
    }));
    fs.unlinkSync(filePath);
  });

  it('builds a RenamePage request without frontend preview fields or output paths', () => {
    expect(buildRenamePagePayload('page-1', 'Executive Summary')).toEqual(expect.objectContaining({
      schemaVersion: 'pbir-authoring-rpc/v1',
      operation: 'mutate',
      mutate: expect.objectContaining({
        mode: 'preview',
        request: expect.objectContaining({
          schemaVersion: 'local-pbir-mutation-request/v1',
          sourceDirectory: '',
          outputBaseDirectory: '',
          targetDirectoryName: '',
          operations: [{ kind: 'renamePage', target: { pageId: 'page-1' }, displayName: 'Executive Summary' }],
        }),
      }),
    }));
  });

  it('formats the backend preview without computing the intended change', () => {
    expect(formatMutationPreview({
      mutationKind: 'renamePage',
      targetPageId: 'page-1',
      currentDisplayName: 'Overview',
      proposedDisplayName: 'Executive Summary',
      executionAdmissible: true,
      isNoOp: false,
    })).toBe('Rename page\n\nCurrent:\nOverview\n\nNew:\nExecutive Summary');
  });

  it('builds typed payloads for every curated mutation without frontend diff fields', () => {
    expect(buildCuratedMutationPayload('addPage', { pageId: 'page-2', displayName: 'Details', order: 1 }))
      .toEqual(expect.objectContaining({
        operation: 'mutate',
        mutate: expect.objectContaining({
          request: expect.objectContaining({
            operations: [{ kind: 'addPage', page: { pageId: 'page-2', displayName: 'Details', order: 1 } }],
          }),
        }),
      }));
    expect(buildCuratedMutationPayload('resizeVisual', { visualId: 'visual-1', layout: { x: 8, y: 12, width: 320, height: 180 } }))
      .toEqual(expect.objectContaining({
        mutate: expect.objectContaining({
          request: expect.objectContaining({
            operations: [{ kind: 'resizeVisual', target: { visualId: 'visual-1' }, layout: { x: 8, y: 12, width: 320, height: 180 } }],
          }),
        }),
      }));
  });

  it('renders operation-specific backend preview payloads', () => {
    expect(formatCuratedMutationPreview({
      mutationKind: 'moveVisual',
      targetPageId: 'page-1',
      currentDisplayName: '',
      proposedDisplayName: '',
      executionAdmissible: true,
      isNoOp: false,
      payload: { kind: 'moveVisual', visual: { currentPageId: 'page-1', proposedPageId: 'page-2', currentOrder: 1, proposedOrder: 2 } },
      diffs: [{ kind: 'visualMoved', objectId: 'visual-1', beforePageId: 'page-1', afterPageId: 'page-2' }],
    })).toContain('page-1 → page-2');
  });

  it('previews and executes RenamePage only after confirmation, retaining the artifact handle', async () => {
    (vscode.window.showOpenDialog as jest.Mock).mockResolvedValue([{ fsPath: '/reports/sales' }]);
    (vscode.window.showQuickPick as jest.Mock).mockResolvedValue({ label: 'Overview', pageId: 'page-1' });
    (vscode.window.showInputBox as jest.Mock).mockResolvedValue('Executive Summary');
    (vscode.window.showInformationMessage as jest.Mock).mockResolvedValue('Rename Page');
    const executeAuthoringRequest = jest.fn()
      .mockResolvedValueOnce({
        succeeded: true,
        importResult: {
          snapshot: { schemaVersion: 'snapshot', snapshotId: 'snapshot-1', sourceIdentity: { sourceDirectoryName: 'sales', contentHash: 'hash', fileCount: 1 } },
          pages: [{ pageId: 'page-1', displayName: 'Overview' }],
        },
      })
      .mockResolvedValueOnce({
        succeeded: true,
        mutateResult: {
          preview: {
            mutationKind: 'renamePage',
            targetPageId: 'page-1',
            currentDisplayName: 'Overview',
            proposedDisplayName: 'Executive Summary',
            executionAdmissible: true,
            isNoOp: false,
          },
        },
      })
      .mockResolvedValueOnce({
        succeeded: true,
        artifactIdentity: { artifactId: 'artifact-1', artifactHash: 'hash', manifestId: 'manifest', manifestHash: 'manifest-hash' },
        mutateResult: {
          artifact: { schemaVersion: 'artifact', artifactId: 'artifact-1', artifactHash: 'hash', manifestId: 'manifest', manifestHash: 'manifest-hash' },
          comparison: { before: { score: 80, pageCount: 1, visualCount: 1 }, after: { score: 82, pageCount: 1, visualCount: 1 }, scoreDelta: 2 },
        },
        analyzer: { score: 82, pageCount: 1, visualCount: 1 },
      });
    const workflow = new PbirAuthoringWorkflow(() => ({ executeAuthoringRequest }));
    await workflow.import();
    await workflow.renamePage();
    expect(executeAuthoringRequest).toHaveBeenCalledTimes(3);
    expect((executeAuthoringRequest as jest.Mock).mock.calls[1][0]).toEqual(expect.objectContaining({
      operation: 'mutate',
      mutate: expect.objectContaining({ mode: 'preview', snapshot: expect.objectContaining({ snapshotId: 'snapshot-1' }) }),
    }));
    expect((executeAuthoringRequest as jest.Mock).mock.calls[2][0]).toEqual(expect.objectContaining({
      operation: 'mutate',
      mutate: expect.objectContaining({ mode: 'execute', snapshot: expect.objectContaining({ snapshotId: 'snapshot-1' }) }),
    }));
  });

  it('does not call the backend when the curated mutation picker is cancelled', async () => {
    (vscode.window.showQuickPick as jest.Mock).mockResolvedValue(undefined);
    const executeAuthoringRequest = jest.fn();
    const workflow = new PbirAuthoringWorkflow(() => ({ executeAuthoringRequest }));
    (workflow as unknown as { snapshot: unknown }).snapshot = {
      schemaVersion: 'snapshot',
      snapshotId: 'snapshot-1',
      sourceIdentity: { sourceDirectoryName: 'sales', contentHash: 'hash', fileCount: 1 },
    };
    (workflow as unknown as { pages: unknown }).pages = [{ pageId: 'page-1', displayName: 'Overview' }];

    await workflow.mutate();

    expect(executeAuthoringRequest).not.toHaveBeenCalled();
  });
});
