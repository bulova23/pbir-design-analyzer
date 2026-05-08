import path from 'path';
import type * as vscode from 'vscode';
import {
  loadDesignAnalyzerConfig,
  validateDesignAnalyzerConfig,
} from '../analyzer/config/store';

describe('design analyzer config defaults', () => {
  function createContext(initialValue?: unknown): vscode.ExtensionContext {
    let storedValue = initialValue;

    return {
      extensionPath: path.resolve(__dirname, '../..'),
      globalState: {
        get: jest.fn(() => storedValue),
        update: jest.fn(async (_key: string, value: unknown) => {
          storedValue = value;
        }),
      },
    } as unknown as vscode.ExtensionContext;
  }

  it('loads defaults with enterprise governance disabled and valid total weight', async () => {
    const context = createContext();

    const config = await loadDesignAnalyzerConfig(context);
    const validation = validateDesignAnalyzerConfig(config);
    const governanceFramework = config.frameworks.find((framework) => framework.id === 'governance');

    expect(governanceFramework).toBeDefined();
    expect(governanceFramework?.enabled).toBe(false);
    expect(governanceFramework?.weight).toBe(0);
    expect(validation.isValid).toBe(true);
    expect(validation.totalWeight).toBe(100);
  });
});
