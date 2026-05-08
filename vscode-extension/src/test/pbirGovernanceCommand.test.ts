import * as vscode from 'vscode';
import { PBIR_COMMANDS, registerPbirCommands } from '../commands/pbirCommands';

describe('pbir.governanceCheck command', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  function getRegisteredHandler(commandId: string): (...args: unknown[]) => Promise<unknown> {
    const registration = (vscode.commands.registerCommand as jest.Mock).mock.calls
      .find(([registeredCommandId]) => registeredCommandId === commandId);

    if (!registration) {
      throw new Error(`Command ${commandId} was not registered`);
    }

    return registration[1] as (...args: unknown[]) => Promise<unknown>;
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
    const governanceHandler = getRegisteredHandler(PBIR_COMMANDS.governanceCheck);

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
});
