import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import { registerCommands, PBIR_COMMANDS } from '../commands/register';
import { registerPbirCommands } from '../commands/pbirCommands';
import { PbirScorePanel } from '../views/PbirScorePanel';

jest.mock('../views/PbirScorePanel', () => ({
  PbirScorePanel: {
    createOrShow: jest.fn(),
  },
}));

describe('pbir.exportReviewWorkflow command', () => {
  let tempDir: string;
  let reportRoot: string;
  let reportJsonPath: string;

  beforeEach(() => {
    jest.clearAllMocks();

    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-review-export-command-'));
    reportRoot = path.join(tempDir, 'FY26 Executive.Report');
    reportJsonPath = path.join(reportRoot, 'definition', 'report.json');

    fs.mkdirSync(path.dirname(reportJsonPath), { recursive: true });
    fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
    fs.writeFileSync(reportJsonPath, '{}');
  });

  afterEach(() => {
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  function getRegisteredHandler(commandId: string): (...args: unknown[]) => Promise<unknown> {
    const registration = (vscode.commands.registerCommand as jest.Mock).mock.calls
      .find(([registeredCommandId]) => registeredCommandId === commandId);

    if (!registration) {
      throw new Error(`Command ${commandId} was not registered`);
    }

    return registration[1] as (...args: unknown[]) => Promise<unknown>;
  }

  it('opens the score panel for the selected report and delegates export to the panel', async () => {
    const exportReviewWorkflow = jest.fn().mockResolvedValue(undefined);
    (PbirScorePanel.createOrShow as jest.Mock).mockResolvedValue({ exportReviewWorkflow });

    registerPbirCommands({ subscriptions: [] } as unknown as vscode.ExtensionContext, () => undefined);

    const exportHandler = getRegisteredHandler(PBIR_COMMANDS.exportReviewWorkflow);
    await exportHandler({
      kind: 'report',
      jsonFilePath: reportJsonPath,
    });

    expect(PbirScorePanel.createOrShow).toHaveBeenCalledWith(
      expect.anything(),
      undefined,
      reportRoot,
    );
    expect(exportReviewWorkflow).toHaveBeenCalledTimes(1);
  });

  it('registers the analyzer-level alias and routes it to the PBIR export command', async () => {
    registerCommands({ subscriptions: [] } as unknown as vscode.ExtensionContext, () => undefined);

    const aliasHandler = getRegisteredHandler('pbir.exportReviewWorkflow');
    await aliasHandler('/tmp/example.Report');

    expect(vscode.commands.executeCommand).toHaveBeenCalledWith(
      PBIR_COMMANDS.exportReviewWorkflow,
      '/tmp/example.Report',
    );
  });
});
