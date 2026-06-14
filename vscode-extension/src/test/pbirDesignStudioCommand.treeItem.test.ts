import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import { registerPbirCommands, PBIR_COMMANDS } from '../commands/pbirCommands';
import { PbirDesignStudioPanel } from '../views/PbirDesignStudioPanel';

jest.mock('../views/PbirDesignStudioPanel', () => ({
  PbirDesignStudioPanel: {
    createOrShow: jest.fn().mockResolvedValue(undefined),
  },
}));

describe('pbir.designStudio tree item targets', () => {
  let tempDir: string;
  let reportRoot: string;
  let reportJsonPath: string;

  beforeEach(() => {
    jest.clearAllMocks();

    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-design-studio-command-'));
    reportRoot = path.join(tempDir, 'Sales & Production.Report');
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

  it('opens Design Studio from a selected report node', async () => {
    registerPbirCommands({ subscriptions: [] } as unknown as vscode.ExtensionContext, () => undefined);

    const handler = getRegisteredHandler(PBIR_COMMANDS.openDesignStudio);
    await handler({
      kind: 'report',
      jsonFilePath: reportJsonPath,
    });

    expect(PbirDesignStudioPanel.createOrShow).toHaveBeenCalledWith(
      expect.anything(),
      reportRoot,
    );
  });
});
