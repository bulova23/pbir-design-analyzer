/**
 * VS Code Mock Utilities
 * 
 * Provides utilities for working with the mocked vscode module in tests.
 * This utility wraps the main vscode mock (from tests/__mocks__/vscode.ts)
 * and provides helper functions for resetting mocks between tests.
 */

import * as vscode from 'vscode';

/**
 * The vscodeMock object provides access to the mocked vscode namespace
 * Simply re-export the vscode mock
 */
export const vscodeMock = vscode;

/**
 * Reset helper to be used in afterEach
 * Clears all mock call histories to prevent test contamination
 */
export const resetVscodeMocks = () => {
  // Reset window mocks
  if (vscode.window.showErrorMessage && jest.isMockFunction(vscode.window.showErrorMessage)) {
    (vscode.window.showErrorMessage as jest.Mock).mockClear();
  }
  if (vscode.window.showInformationMessage && jest.isMockFunction(vscode.window.showInformationMessage)) {
    (vscode.window.showInformationMessage as jest.Mock).mockClear();
  }
  if (vscode.window.setStatusBarMessage && jest.isMockFunction(vscode.window.setStatusBarMessage)) {
    (vscode.window.setStatusBarMessage as jest.Mock).mockClear();
  }
  if (vscode.window.createStatusBarItem && jest.isMockFunction(vscode.window.createStatusBarItem)) {
    (vscode.window.createStatusBarItem as jest.Mock).mockClear();
    // Reset the default mock implementation
    (vscode.window.createStatusBarItem as jest.Mock).mockReturnValue({
      show: jest.fn(),
      hide: jest.fn(),
      dispose: jest.fn(),
      text: '',
      command: ''
    });
  }
  
  // Reset workspace mocks
  if (vscode.workspace.getConfiguration && jest.isMockFunction(vscode.workspace.getConfiguration)) {
    (vscode.workspace.getConfiguration as jest.Mock).mockClear();
    // Reset the default mock implementation
    (vscode.workspace.getConfiguration as jest.Mock).mockReturnValue({
      get: jest.fn((key: string, defaultValue: any) => defaultValue),
      has: jest.fn(() => false),
      inspect: jest.fn(),
      update: jest.fn().mockResolvedValue(undefined),
    });
  }
  if (vscode.workspace.createFileSystemWatcher && jest.isMockFunction(vscode.workspace.createFileSystemWatcher)) {
    (vscode.workspace.createFileSystemWatcher as jest.Mock).mockClear();
    // Reset the default mock implementation
    (vscode.workspace.createFileSystemWatcher as jest.Mock).mockReturnValue({
      onDidCreate: jest.fn(),
      onDidChange: jest.fn(),
      onDidDelete: jest.fn(),
      dispose: jest.fn(),
    });
  }
  if (vscode.workspace.onDidChangeConfiguration && jest.isMockFunction(vscode.workspace.onDidChangeConfiguration)) {
    (vscode.workspace.onDidChangeConfiguration as jest.Mock).mockClear();
    // Reset the default mock implementation
    (vscode.workspace.onDidChangeConfiguration as jest.Mock).mockReturnValue({
      dispose: jest.fn(),
    });
  }
};


