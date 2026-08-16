import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import { PBIR_COMMANDS, registerPbirCommands } from '../commands/pbirCommands';
import { PbirScorePanel } from '../views/PbirScorePanel';

jest.mock('../views/PbirScorePanel', () => ({
  PbirScorePanel: {
    createOrShow: jest.fn(),
  },
}));

describe('pbir.uploadScreenshots command', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  afterEach(() => {
    for (const entry of fs.readdirSync(os.tmpdir()).filter((name) => name.startsWith('pbir-upload-screenshots-command-'))) {
      fs.rmSync(path.join(os.tmpdir(), entry), { recursive: true, force: true });
    }
  });

  function getRegisteredHandler(commandId: string): (...args: unknown[]) => Promise<unknown> {
    const registration = (vscode.commands.registerCommand as jest.Mock).mock.calls
      .find(([registeredCommandId]) => registeredCommandId === commandId);

    if (!registration) {
      throw new Error(`Command ${commandId} was not registered`);
    }

    return registration[1] as (...args: unknown[]) => Promise<unknown>;
  }

  function createReportRoot(): string {
    const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-upload-screenshots-command-'));
    const reportRoot = path.join(tempDir, 'Sales.Report');
    fs.mkdirSync(reportRoot, { recursive: true });
    return reportRoot;
  }

  it('opens the panel and triggers screenshot upload instead of rescoring the report', async () => {
    const reportPath = createReportRoot();
    const requestScreenshotUpload = jest.fn().mockResolvedValue(undefined);
    (PbirScorePanel.createOrShow as jest.Mock).mockResolvedValue({
      requestScreenshotUpload,
    });

    registerPbirCommands({ subscriptions: [] } as unknown as vscode.ExtensionContext, () => undefined);
    const uploadHandler = getRegisteredHandler(PBIR_COMMANDS.uploadScreenshots);

    await uploadHandler(reportPath);

    expect(PbirScorePanel.createOrShow).toHaveBeenCalled();
    expect(requestScreenshotUpload).toHaveBeenCalled();
    expect(vscode.commands.executeCommand).not.toHaveBeenCalledWith('pbir.scoreReport', reportPath);
  });
});
