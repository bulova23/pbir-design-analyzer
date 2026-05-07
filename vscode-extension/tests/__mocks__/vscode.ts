/// <reference types="jest" />
import path from 'path';

// Mock window object with status bar/output channel support
export const window = {
  showErrorMessage: jest.fn(),
  showInformationMessage: jest.fn(),
  showWarningMessage: jest.fn(),
  showOpenDialog: jest.fn().mockResolvedValue(undefined),
  showQuickPick: jest.fn().mockResolvedValue(undefined),
  showInputBox: jest.fn().mockResolvedValue(undefined),
  setStatusBarMessage: jest.fn(),
  createStatusBarItem: jest.fn(() => ({
    show: jest.fn(),
    hide: jest.fn(),
    dispose: jest.fn(),
    text: '',
    command: ''
  })),
  createTreeView: jest.fn(() => ({
    dispose: jest.fn(),
  })),
  createOutputChannel: jest.fn(() => ({
    appendLine: jest.fn(),
    show: jest.fn(),
    dispose: jest.fn(),
  })),
  createWebviewPanel: jest.fn(() => ({
    webview: {
      html: '',
      postMessage: jest.fn(),
      onDidReceiveMessage: jest.fn((callback: any) => {
        return { dispose: jest.fn() };
      }),
    },
    onDidDispose: jest.fn((callback: any) => {
      return { dispose: jest.fn() };
    }),
    reveal: jest.fn(),
    dispose: jest.fn(),
  })),
  withProgress: jest.fn(async (_options: any, task: any) => task({ report: jest.fn() }, {})),
};

// Mock workspace
export const workspace = {
  workspaceFolders: [],
  findFiles: jest.fn().mockResolvedValue([]),
  createFileSystemWatcher: jest.fn(() => ({
    onDidCreate: jest.fn(),
    onDidChange: jest.fn(),
    onDidDelete: jest.fn(),
    dispose: jest.fn(),
  })),
  onDidChangeConfiguration: jest.fn((callback: any) => ({
    dispose: jest.fn(),
  })),
  getConfiguration: jest.fn((section?: string) => {
    const configMap = new Map<string, any>();
    
    return {
      get: jest.fn(function(this: any, key: string, defaultValue: any) {
        // Allow override via the configMap for testing
        if (this.__overrides && this.__overrides.has(key)) {
          return this.__overrides.get(key);
        }
        return defaultValue;
      }),
      has: jest.fn(() => false),
      inspect: jest.fn(),
      update: jest.fn().mockResolvedValue(undefined),
      __overrides: configMap, // Store overrides for testing
    };
  }),
};

export const commands = {
  executeCommand: jest.fn(),
  registerCommand: jest.fn(() => ({ dispose: jest.fn() })),
};
export const ProgressLocation = {
  Notification: 1,
};
export const lm = { selectChatModels: jest.fn() };

export const LanguageModelChatMessage = {
  User: (content: string) => ({ role: 'user', content }),
};

export class CancellationTokenSource {
  token = {};
  dispose = jest.fn();
}

export const StatusBarAlignment = {
  Left: 1,
  Right: 2
};

export const TreeItemCollapsibleState = {
  None: 0,
  Collapsed: 1,
  Expanded: 2,
};

export class TreeItem {
  label: string;
  collapsibleState: number;
  tooltip?: string;
  contextValue?: string;
  resourceUri?: any;
  command?: any;
  iconPath?: any;
  description?: string;

  constructor(label: string, collapsibleState: number) {
    this.label = label;
    this.collapsibleState = collapsibleState;
  }
}

export class ThemeIcon {
  constructor(public readonly id: string) {}
}

export const ViewColumn = {
  One: 1,
  Two: 2,
  Three: 3,
  Four: 4,
  Five: 5,
  Six: 6,
  Seven: 7,
  Eight: 8,
  Nine: 9,
  Beside: -2,
};

export const Uri = {
  file: (fsPath: string) => ({
    fsPath,
    scheme: 'file',
    authority: '',
    path: fsPath,
    query: '',
    fragment: '',
    toString: () => fsPath,
  }),
  joinPath: (base: any, ...pathsToJoin: string[]) => {
    const basePath = base?.fsPath ?? (typeof base === 'string' ? base : '');
    const fsPath = path.join(basePath, ...pathsToJoin);
    return {
      fsPath,
      scheme: 'file',
      authority: '',
      path: fsPath,
      query: '',
      fragment: '',
      toString: () => fsPath,
    };
  },
};

export const ConfigurationTarget = {
  Global: 1,
  Workspace: 2,
  WorkspaceFolder: 3,
};

export const EventEmitter = class {
  private listeners: Array<(value: any) => void> = [];

  get event() {
    return (listener: (value: any) => void) => {
      this.listeners.push(listener);
      return { dispose: () => {} };
    };
  }

  fire(value: any) {
    this.listeners.forEach(listener => listener(value));
  }

  dispose() {
    this.listeners = [];
  }
};

// Mock CompletionItem for language client support
export class CompletionItem {
  label: string;
  kind?: any;
  detail?: string;
  documentation?: string;

  constructor(label: string, kind?: any) {
    this.label = label;
    this.kind = kind;
  }
}

export const CompletionItemKind = {
  Text: 1,
  Method: 2,
  Function: 3,
  Constructor: 4,
  Field: 5,
  Variable: 6,
  Class: 7,
  Interface: 8,
  Module: 9,
  Property: 10,
  Unit: 11,
  Value: 12,
  Enum: 13,
  Keyword: 14,
  Snippet: 15,
  Color: 16,
  File: 17,
  Reference: 18,
};

export const extensions = {
  getExtension: jest.fn(),
};

export const authentication = {
  getSession: jest.fn().mockResolvedValue(null),
};

export default { 
  window, 
  workspace, 
  commands, 
  ProgressLocation, 
  Uri, 
  ConfigurationTarget, 
  EventEmitter, 
  StatusBarAlignment,
  TreeItem,
  TreeItemCollapsibleState,
  ThemeIcon,
  CompletionItem,
  CompletionItemKind,
  extensions,
  authentication,
};
