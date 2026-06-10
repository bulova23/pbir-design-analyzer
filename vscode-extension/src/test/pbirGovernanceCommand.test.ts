import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import { PBIR_COMMANDS, registerPbirCommands } from '../commands/pbirCommands';

describe('pbirAnalyzer.checkGovernance command', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  afterEach(() => {
    for (const entry of fs.readdirSync(os.tmpdir()).filter((name) => name.startsWith('pbir-governance-command-'))) {
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

  function createReportWithTheme(themeName: string): string {
    const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-governance-command-'));
    const reportRoot = path.join(tempDir, 'Sales.Report');
    const definitionRoot = path.join(reportRoot, 'definition');
    fs.mkdirSync(definitionRoot, { recursive: true });
    fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
    fs.writeFileSync(path.join(definitionRoot, 'report.json'), JSON.stringify({
      name: 'Sales',
      theme: {
        name: themeName,
        href: 'themes/corporate.json',
      },
    }));
    return reportRoot;
  }

  it('does not prompt for theme input when workspace governance is disabled', async () => {
    const bridge = {
      executeRequest: jest.fn().mockResolvedValue({
        success: true,
        data: {
          policyState: 'notConfigured',
          statusMessage: 'No workspace governance policy is enabled.',
          blocked: false,
          evaluatedScore: 77,
          requiredThreshold: 0,
        },
      }),
    };

    (vscode.workspace.getConfiguration as jest.Mock).mockReturnValue({
      get: jest.fn((key: string, defaultValue: unknown) => {
        if (key === 'governance.enabled') {
          return false;
        }

        if (key === 'governance.approvedThemeIds') {
          return [];
        }

        return defaultValue;
      }),
    });

    registerPbirCommands({ subscriptions: [] } as unknown as vscode.ExtensionContext, () => bridge as never);
    const governanceHandler = getRegisteredHandler(PBIR_COMMANDS.checkGovernance);

    await governanceHandler('/tmp/Sales.Report');

    expect(vscode.window.showInputBox).not.toHaveBeenCalled();
    expect(bridge.executeRequest).toHaveBeenCalledWith('model/pbir/governanceCheck', {
      reportPath: '/tmp/Sales.Report',
      themeId: '',
    });
    expect(vscode.window.showInformationMessage).toHaveBeenCalledWith(
      'No workspace governance policy is enabled.',
    );
  });

  it('reads the theme identifier from PBIR metadata instead of prompting the user', async () => {
    const reportPath = createReportWithTheme('CorporateBlue');
    const bridge = {
      executeRequest: jest.fn().mockResolvedValue({
        success: true,
        data: {
          policyState: 'configured',
          blocked: false,
          evaluatedScore: 88,
          requiredThreshold: 80,
        },
      }),
    };

    (vscode.workspace.getConfiguration as jest.Mock).mockReturnValue({
      get: jest.fn((key: string, defaultValue: unknown) => {
        if (key === 'governance.enabled') {
          return true;
        }

        if (key === 'governance.approvedThemeIds') {
          return ['CorporateBlue'];
        }

        return defaultValue;
      }),
    });

    registerPbirCommands({ subscriptions: [] } as unknown as vscode.ExtensionContext, () => bridge as never);
    const governanceHandler = getRegisteredHandler(PBIR_COMMANDS.checkGovernance);

    await governanceHandler(reportPath);

    expect(vscode.window.showInputBox).not.toHaveBeenCalled();
    expect(bridge.executeRequest).toHaveBeenCalledWith('model/pbir/governanceCheck', {
      reportPath,
      themeId: 'CorporateBlue',
    });
  });

  it('falls back to legacy powerbi-modeling governance settings when pbirAnalyzer settings are not present', async () => {
    const reportPath = createReportWithTheme('CorporateBlue');
    const bridge = {
      executeRequest: jest.fn().mockResolvedValue({
        success: true,
        data: {
          policyState: 'configured',
          blocked: false,
          evaluatedScore: 88,
          requiredThreshold: 80,
        },
      }),
    };

    const newConfig = {
      get: jest.fn((key: string, defaultValue: unknown) => defaultValue),
    };
    const legacyConfig = {
      get: jest.fn((key: string, defaultValue: unknown) => {
        if (key === 'governance.enabled') {
          return true;
        }

        if (key === 'governance.approvedThemeIds') {
          return ['CorporateBlue'];
        }

        return defaultValue;
      }),
    };

    (vscode.workspace.getConfiguration as jest.Mock).mockImplementation((section?: string) => {
      if (section === 'pbirAnalyzer') {
        return newConfig;
      }

      if (section === 'powerbi-modeling') {
        return legacyConfig;
      }

      return { get: jest.fn((_key: string, defaultValue: unknown) => defaultValue) };
    });

    registerPbirCommands({ subscriptions: [] } as unknown as vscode.ExtensionContext, () => bridge as never);
    const governanceHandler = getRegisteredHandler(PBIR_COMMANDS.checkGovernance);

    await governanceHandler(reportPath);

    expect(vscode.workspace.getConfiguration).toHaveBeenCalledWith('pbirAnalyzer');
    expect(vscode.workspace.getConfiguration).toHaveBeenCalledWith('powerbi-modeling');
    expect(bridge.executeRequest).toHaveBeenCalledWith('model/pbir/governanceCheck', {
      reportPath,
      themeId: 'CorporateBlue',
    });
  });
});
