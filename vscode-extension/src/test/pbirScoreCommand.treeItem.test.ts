import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import { registerPbirCommands, PBIR_COMMANDS } from '../commands/pbirCommands';
import { PbirScorePanel } from '../views/PbirScorePanel';

jest.mock('../views/PbirScorePanel', () => ({
  PbirScorePanel: {
    createOrShow: jest.fn().mockResolvedValue(undefined),
    copyCurrentScoreDiagnostics: jest.fn().mockResolvedValue(false),
  },
}));

describe('pbir.scoreReport tree item targets', () => {
  let tempDir: string;
  let reportRoot: string;
  let reportJsonPath: string;
  let pageJsonPath: string;

  beforeEach(() => {
    jest.clearAllMocks();

    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-score-command-'));
    reportRoot = path.join(tempDir, 'Sales & Production.Report');
    reportJsonPath = path.join(reportRoot, 'definition', 'report.json');
    pageJsonPath = path.join(reportRoot, 'definition', 'pages', 'OverviewPage', 'page.json');

    fs.mkdirSync(path.dirname(pageJsonPath), { recursive: true });
    fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
    fs.writeFileSync(reportJsonPath, '{}');
    fs.writeFileSync(pageJsonPath, '{}');
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

  it('resolves a selected report node to the .Report folder path', async () => {
    registerPbirCommands({ subscriptions: [] } as unknown as vscode.ExtensionContext, () => undefined);

    const scoreHandler = getRegisteredHandler(PBIR_COMMANDS.scoreReport);
    await scoreHandler({
      kind: 'report',
      jsonFilePath: reportJsonPath,
    });

    expect(PbirScorePanel.createOrShow).toHaveBeenCalledWith(
      expect.anything(),
      undefined,
      reportRoot,
      undefined,
    );
    expect(vscode.window.showErrorMessage).not.toHaveBeenCalledWith(
      expect.stringContaining('Report not found'),
    );
  });

  it('resolves a selected page node to the report root and stable page name', async () => {
    registerPbirCommands({ subscriptions: [] } as unknown as vscode.ExtensionContext, () => undefined);

    const scoreHandler = getRegisteredHandler(PBIR_COMMANDS.scoreReport);
    await scoreHandler({
      kind: 'page',
      label: 'Overview',
      jsonFilePath: pageJsonPath,
      rawNode: {
        name: 'OverviewPage',
        displayName: 'Overview',
      },
    });

    expect(PbirScorePanel.createOrShow).toHaveBeenCalledWith(
      expect.anything(),
      undefined,
      reportRoot,
      'OverviewPage',
    );
  });

  it('warns when score diagnostics are not available yet', async () => {
    registerPbirCommands({ subscriptions: [] } as unknown as vscode.ExtensionContext, () => undefined);

    const diagnosticsHandler = getRegisteredHandler(PBIR_COMMANDS.copyScoreDiagnostics);
    await diagnosticsHandler();

    expect(PbirScorePanel.copyCurrentScoreDiagnostics).toHaveBeenCalled();
    expect(vscode.window.showWarningMessage).toHaveBeenCalledWith(
      'No score diagnostics are available yet. Run Score Report first.',
    );
  });

  it('confirms when score diagnostics are copied', async () => {
    (PbirScorePanel.copyCurrentScoreDiagnostics as jest.Mock).mockResolvedValueOnce(true);
    registerPbirCommands({ subscriptions: [] } as unknown as vscode.ExtensionContext, () => undefined);

    const diagnosticsHandler = getRegisteredHandler(PBIR_COMMANDS.copyScoreDiagnostics);
    await diagnosticsHandler();

    expect(vscode.window.showInformationMessage).toHaveBeenCalledWith(
      'Current score diagnostics copied to the clipboard.',
    );
  });
});
