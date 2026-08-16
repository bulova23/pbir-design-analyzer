import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type * as vscode from 'vscode';
import {
  type BackendLaunchDiagnostics,
  describeBackendStartupFailure,
  getBackendRuntimeDescriptor,
  inspectBackendBinary,
  prepareBackendLaunchDiagnosticsForStartup,
  resolveBackendExecutablePath,
} from '../languageServer/analyzerBackendClient';

describe('analyzerBackendClient', () => {
  it('returns the Windows ARM64 packaged backend descriptor', () => {
    const result = getBackendRuntimeDescriptor('win32', 'arm64');

    expect('descriptor' in result).toBe(true);
    if (!('descriptor' in result)) {
      throw new Error(`expected descriptor, received issue: ${result.issue.message}`);
    }

    expect(result.descriptor).toEqual({
      runtimeId: 'win-arm64',
      target: 'win32-arm64',
      executableName: 'ModelingLanguageServer.exe',
      selfContained: true,
    });
  });

  it('resolves the packaged Windows ARM64 backend path from the installed extension folder', () => {
    const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-backend-client-'));
    try {
      const extensionPath = path.join(tempDir, 'extension');
      const backendDir = path.join(extensionPath, 'backend', 'rpc');
      fs.mkdirSync(backendDir, { recursive: true });
      fs.writeFileSync(path.join(backendDir, 'ModelingLanguageServer.exe'), 'binary');

      const context = {
        extensionPath,
      } as vscode.ExtensionContext;

      const descriptorResult = getBackendRuntimeDescriptor('win32', 'arm64');
      if (!('descriptor' in descriptorResult)) {
        throw new Error('expected Windows ARM64 descriptor');
      }

      const resolved = resolveBackendExecutablePath(context, descriptorResult.descriptor);
      expect('serverPath' in resolved).toBe(true);
      if (!('serverPath' in resolved)) {
        throw new Error(`expected server path, received issue: ${resolved.issue.message}`);
      }

      expect(resolved.serverPath).toBe(path.join(backendDir, 'ModelingLanguageServer.exe'));
    } finally {
      fs.rmSync(tempDir, { recursive: true, force: true });
    }
  });

  it('identifies a Windows ARM64 PE executable from its file header', () => {
    const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-backend-binary-'));
    try {
      const exePath = path.join(tempDir, 'ModelingLanguageServer.exe');
      const buffer = Buffer.alloc(512);
      buffer.writeUInt16LE(0x5a4d, 0);
      buffer.writeUInt32LE(0x80, 0x3c);
      buffer.write('PE\u0000\u0000', 0x80, 'ascii');
      buffer.writeUInt16LE(0xaa64, 0x84);
      fs.writeFileSync(exePath, buffer);

      expect(inspectBackendBinary(exePath)).toEqual({
        fileName: 'ModelingLanguageServer.exe',
        format: 'pe',
        architecture: 'arm64',
        description: 'PE arm64 executable',
      });
    } finally {
      fs.rmSync(tempDir, { recursive: true, force: true });
    }
  });

  it('reports actionable Windows ARM64 runtime diagnostics when the backend exits before the handshake', () => {
    const issue = describeBackendStartupFailure(
      new Error('Connection input stream is not set.'),
      {
        processPlatform: 'win32',
        processArch: 'arm64',
        vscodeTarget: 'win32-arm64',
        runtimeId: 'win-arm64',
        selectedTarget: 'win32-arm64',
        resolvedBackendPath: 'C:\\temp\\ModelingLanguageServer.exe',
        backendExists: true,
        backendFileName: 'ModelingLanguageServer.exe',
        backendFileDescription: 'PE arm64 executable',
        launchCommand: 'C:\\temp\\ModelingLanguageServer.exe',
        selfContained: false,
        checkedPaths: ['C:\\temp\\ModelingLanguageServer.exe'],
        dotnetRuntime: {
          available: false,
          command: 'dotnet',
          exitCode: null,
          firstLines: ['dotnet command not found'],
        },
        preflight: {
          attempted: true,
          exitedEarly: true,
          exitCode: 150,
          signal: null,
          stdoutLines: [],
          stderrLines: ['You must install or update .NET to run this application.'],
        },
      },
    );

    expect(issue.message).toContain('.NET 8');
    expect(issue.message).toContain('Windows ARM64');
    expect(issue.detail).toContain('VS Code target: win32-arm64');
    expect(issue.detail).toContain('Resolved backend path: C:\\temp\\ModelingLanguageServer.exe');
    expect(issue.detail).toContain('Preflight exit code: 150');
    expect(issue.detail).toContain('dotnet command not found');
  });

  it('does not resolve repo-local Debug or Release leftovers when packaged backend assets are missing', () => {
    const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-backend-resolution-'));
    try {
      const repoPath = path.join(tempDir, 'repo');
      const staleDebugDir = path.join(repoPath, 'service-dotnet', 'RpcHost', 'bin', 'Debug', 'net8.0', 'osx-arm64');
      const staleReleaseDir = path.join(repoPath, 'service-dotnet', 'RpcHost', 'bin', 'Release', 'net8.0', 'osx-arm64', 'publish');
      fs.mkdirSync(staleDebugDir, { recursive: true });
      fs.mkdirSync(staleReleaseDir, { recursive: true });
      fs.writeFileSync(path.join(staleDebugDir, 'ModelingLanguageServer'), 'stale-debug');
      fs.writeFileSync(path.join(staleReleaseDir, 'ModelingLanguageServer'), 'stale-release');

      const context = {
        extensionPath: path.join(repoPath, 'vscode-extension'),
      } as vscode.ExtensionContext;

      const descriptorResult = getBackendRuntimeDescriptor('darwin', 'arm64');
      if (!('descriptor' in descriptorResult)) {
        throw new Error('expected darwin arm64 descriptor');
      }

      const resolved = resolveBackendExecutablePath(context, descriptorResult.descriptor);
      expect('issue' in resolved).toBe(true);
      if (!('issue' in resolved)) {
        throw new Error(`expected backendMissing issue, received path: ${resolved.serverPath}`);
      }

      expect(resolved.issue.code).toBe('backendMissing');
      expect(resolved.issue.checkedPaths).toEqual([
        path.join(repoPath, 'vscode-extension', 'backend', 'rpc', 'ModelingLanguageServer'),
      ]);
    } finally {
      fs.rmSync(tempDir, { recursive: true, force: true });
    }
  });

  it('skips sacrificial backend preflight during normal startup', async () => {
    const diagnostics: BackendLaunchDiagnostics = {
      processPlatform: 'darwin',
      processArch: 'arm64',
      vscodeTarget: 'darwin-arm64',
      runtimeId: 'osx-arm64',
      selectedTarget: 'darwin-arm64',
      resolvedBackendPath: '/tmp/backend/ModelingLanguageServer',
      backendExists: true,
      backendFileName: 'ModelingLanguageServer',
      backendFileDescription: 'Mach-O arm64 executable',
      launchCommand: '/tmp/backend/ModelingLanguageServer',
      selfContained: false,
      checkedPaths: ['/tmp/backend/ModelingLanguageServer'],
      dotnetRuntime: {
        available: true,
        command: 'dotnet',
        exitCode: 0,
        firstLines: ['.NET 8'],
      },
    };

    const runLaunchPreflight = jest.fn(async () => ({
      ...diagnostics,
      preflight: {
        attempted: true,
        exitedEarly: false,
        exitCode: null,
        signal: null,
        stdoutLines: [],
        stderrLines: [],
      },
    }));

    const prepared = await prepareBackendLaunchDiagnosticsForStartup(diagnostics, {
      enableLaunchPreflight: false,
      runLaunchPreflight,
    });

    expect(prepared).toBe(diagnostics);
    expect(runLaunchPreflight).not.toHaveBeenCalled();
  });
});
