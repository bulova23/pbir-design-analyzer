import * as fs from 'fs';
import * as vscode from 'vscode';
import {
  buildGeneratePayload,
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
});
