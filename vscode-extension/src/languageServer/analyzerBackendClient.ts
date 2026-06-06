import { existsSync, readFileSync } from 'fs';
import * as path from 'path';
import { spawn, spawnSync } from 'child_process';
import * as vscode from 'vscode';
import {
  LanguageClient,
  LanguageClientOptions,
  RevealOutputChannelOn,
  ServerOptions,
  State,
} from 'vscode-languageclient/node';

export interface BackendRuntimeDescriptor {
  runtimeId: 'win-x64' | 'win-arm64' | 'linux-x64' | 'osx-x64' | 'osx-arm64';
  target: 'win32-x64' | 'win32-arm64' | 'linux-x64' | 'darwin-x64' | 'darwin-arm64';
  executableName: string;
  selfContained: boolean;
}

export interface DotnetRuntimeDiagnostics {
  available: boolean;
  command: string;
  exitCode: number | null;
  firstLines: string[];
}

export interface BackendLaunchPreflight {
  attempted: boolean;
  exitedEarly: boolean;
  exitCode: number | null;
  signal: NodeJS.Signals | null;
  stdoutLines: string[];
  stderrLines: string[];
}

export interface BackendLaunchDiagnostics {
  processPlatform: string;
  processArch: string;
  vscodeTarget: BackendRuntimeDescriptor['target'];
  runtimeId: BackendRuntimeDescriptor['runtimeId'];
  selectedTarget: BackendRuntimeDescriptor['target'];
  resolvedBackendPath: string;
  backendExists: boolean;
  backendFileName: string;
  backendFileDescription: string;
  launchCommand: string;
  selfContained: boolean;
  checkedPaths: string[];
  dotnetRuntime: DotnetRuntimeDiagnostics;
  preflight?: BackendLaunchPreflight;
}

export interface BackendResolutionIssue {
  code: 'unsupportedPlatform' | 'backendMissing' | 'runtimeUnavailable';
  message: string;
  detail?: string;
  checkedPaths?: string[];
  diagnostics?: BackendLaunchDiagnostics;
}

export interface AnalyzerBackendClientResult {
  client?: LanguageClient;
  issue?: BackendResolutionIssue;
  diagnostics?: BackendLaunchDiagnostics;
}

let lastBackendIssue: BackendResolutionIssue | undefined;
let lastBackendLaunchDiagnostics: BackendLaunchDiagnostics | undefined;

export function recordBackendIssue(issue: BackendResolutionIssue | undefined): void {
  lastBackendIssue = issue;
}

export function getRecordedBackendIssue(): BackendResolutionIssue | undefined {
  return lastBackendIssue;
}

export function getRecordedBackendLaunchDiagnostics(): BackendLaunchDiagnostics | undefined {
  return lastBackendLaunchDiagnostics;
}

/**
 * Creates the PBIR analyzer backend client used as the JSON-RPC bridge to the
 * packaged .NET host.
 */
export function createAnalyzerBackendClient(
  context: vscode.ExtensionContext,
): AnalyzerBackendClientResult {
  try {
    const descriptor = getBackendRuntimeDescriptor();
    if ('issue' in descriptor) {
      recordBackendIssue(descriptor.issue);
      return { issue: descriptor.issue };
    }

    const resolved = resolveBackendExecutablePath(context, descriptor.descriptor);

    if ('issue' in resolved) {
      console.warn(`[RPC] ${resolved.issue.message}`);
      recordBackendIssue(resolved.issue);
      return { issue: resolved.issue };
    }

    const diagnostics = buildBackendLaunchDiagnostics(descriptor.descriptor, resolved.serverPath, resolved.checkedPaths);
    lastBackendLaunchDiagnostics = diagnostics;
    const serverOptions: ServerOptions = {
      run: { command: resolved.serverPath, args: [], options: { env: { ...process.env } } },
      debug: { command: resolved.serverPath, args: [], options: { env: { ...process.env } } },
    };

    const clientOptions: LanguageClientOptions = {
      documentSelector: [{ scheme: 'file' }],
      outputChannel: vscode.window.createOutputChannel('PBIR Design Analyzer Backend'),
      traceOutputChannel: vscode.window.createOutputChannel('PBIR Design Analyzer Backend Trace'),
      revealOutputChannelOn: RevealOutputChannelOn.Info,
      connectionOptions: { maxRestartCount: 3 },
    };

    const client = new LanguageClient(
      'pbir-design-analyzer-backend',
      'PBIR Design Analyzer Backend',
      serverOptions,
      clientOptions,
    );

    client.onDidChangeState((event) => {
      console.log(`[RPC] State changed: ${event.oldState} -> ${event.newState}`);

      if (event.newState === State.Running) {
        console.log('[RPC] Analyzer backend started successfully');
      } else if (event.newState === State.Stopped && event.oldState === State.Starting) {
        const issue = createRuntimeUnavailableIssue(
          'PBIR Design Analyzer backend failed to start during the language-client launch sequence.',
          diagnostics,
        );
        recordBackendIssue(issue);
        console.error('[RPC] Analyzer backend failed to start');
        vscode.window
          .showErrorMessage(issue.message, 'Show Output')
          .then((selection) => {
            if (selection === 'Show Output') {
              void vscode.commands.executeCommand('workbench.action.output.show');
            }
          });
      }
    });

    recordBackendIssue(undefined);
    return { client, diagnostics };
  } catch (error) {
    console.error('[RPC] Failed to create analyzer backend client:', error);
    const issue: BackendResolutionIssue = {
      code: 'runtimeUnavailable',
      message: 'PBIR Design Analyzer could not prepare the backend runtime. The extension will continue in degraded mode.',
      detail: error instanceof Error ? error.message : String(error),
    };
    recordBackendIssue(issue);
    lastBackendLaunchDiagnostics = undefined;
    return { issue };
  }
}

export function getBackendRuntimeDescriptor(
  platform: NodeJS.Platform = process.platform,
  arch: string = process.arch,
): { descriptor: BackendRuntimeDescriptor } | { issue: BackendResolutionIssue } {
  if (platform === 'win32' && arch === 'x64') {
    return {
      descriptor: {
        runtimeId: 'win-x64',
        target: 'win32-x64',
        executableName: 'ModelingLanguageServer.exe',
        selfContained: false,
      },
    };
  }
  if (platform === 'win32' && arch === 'arm64') {
    return {
      descriptor: {
        runtimeId: 'win-arm64',
        target: 'win32-arm64',
        executableName: 'ModelingLanguageServer.exe',
        selfContained: true,
      },
    };
  }
  if (platform === 'linux' && arch === 'x64') {
    return {
      descriptor: {
        runtimeId: 'linux-x64',
        target: 'linux-x64',
        executableName: 'ModelingLanguageServer',
        selfContained: false,
      },
    };
  }
  if (platform === 'darwin' && arch === 'x64') {
    return {
      descriptor: {
        runtimeId: 'osx-x64',
        target: 'darwin-x64',
        executableName: 'ModelingLanguageServer',
        selfContained: false,
      },
    };
  }
  if (platform === 'darwin' && arch === 'arm64') {
    return {
      descriptor: {
        runtimeId: 'osx-arm64',
        target: 'darwin-arm64',
        executableName: 'ModelingLanguageServer',
        selfContained: false,
      },
    };
  }

  return {
    issue: {
      code: 'unsupportedPlatform',
      message: `PBIR Design Analyzer does not ship a packaged backend for ${platform}-${arch}. The extension will continue in degraded mode.`,
    },
  };
}

export function describeBackendStartupFailure(
  error: unknown,
  descriptorOrDiagnostics?: BackendRuntimeDescriptor | BackendLaunchDiagnostics,
): BackendResolutionIssue {
  const detail = error instanceof Error ? error.message : String(error);
  const diagnostics = isBackendLaunchDiagnostics(descriptorOrDiagnostics)
    ? descriptorOrDiagnostics
    : undefined;
  const platformMessage = diagnostics
    ? `${diagnostics.selectedTarget} backend`
    : descriptorOrDiagnostics && !isBackendLaunchDiagnostics(descriptorOrDiagnostics)
      ? `${descriptorOrDiagnostics.target} backend`
      : 'backend';

  if (diagnostics) {
    const stderrText = diagnostics.preflight?.stderrLines.join('\n') ?? '';
    const dotnetMissing =
      !diagnostics.selfContained &&
      (!diagnostics.dotnetRuntime.available ||
        /install or update \.net|framework|microsoft\.netcore\.app/i.test(stderrText));

    if (dotnetMissing) {
      const windowsArmHint = diagnostics.selectedTarget === 'win32-arm64'
        ? 'Install the .NET 8 Windows ARM64 runtime or use a validated self-contained Windows ARM64 package.'
        : 'Install the matching .NET 8 runtime for this platform.';

      return {
        code: 'runtimeUnavailable',
        message: `PBIR Design Analyzer could not start the ${platformMessage} because a compatible .NET 8 runtime was not detected. ${windowsArmHint}`,
        detail: formatBackendLaunchDiagnostics(diagnostics, detail),
        diagnostics,
      };
    }

    if (diagnostics.preflight?.exitedEarly) {
      return createRuntimeUnavailableIssue(
        `PBIR Design Analyzer failed to start the ${platformMessage}. The backend exited before the LSP handshake completed.`,
        diagnostics,
        detail,
      );
    }
  }

  if (/ENOENT|not found/i.test(detail)) {
    return {
      code: 'backendMissing',
      message: `PBIR Design Analyzer could not launch the ${platformMessage}. Install .NET 8 and rebuild the packaged backend, or reinstall the correct VSIX for this platform.`,
      detail,
      diagnostics,
    };
  }

  return diagnostics
    ? createRuntimeUnavailableIssue(
        `PBIR Design Analyzer failed to start the ${platformMessage}. The extension will continue in degraded mode with local tree browsing only.`,
        diagnostics,
        detail,
      )
    : {
        code: 'runtimeUnavailable',
        message: `PBIR Design Analyzer failed to start the ${platformMessage}. The extension will continue in degraded mode with local tree browsing only.`,
        detail,
      };
}

export function resolveBackendExecutablePath(
  context: vscode.ExtensionContext,
  descriptor: BackendRuntimeDescriptor,
): { serverPath: string; checkedPaths: string[] } | { issue: BackendResolutionIssue } {
  try {
    const repoPath = path.resolve(context.extensionPath, '..');
    const possiblePaths = [
      path.join(context.extensionPath, 'backend', 'rpc', descriptor.executableName),
      path.join(repoPath, 'vscode-extension', 'backend', 'rpc', descriptor.executableName),
      path.join(repoPath, 'service-dotnet', 'RpcHost', 'bin', 'Debug', 'net8.0', descriptor.runtimeId, descriptor.executableName),
      path.join(repoPath, 'service-dotnet', 'RpcHost', 'bin', 'Debug', 'net8.0', descriptor.runtimeId, 'publish', descriptor.executableName),
      path.join(repoPath, 'service-dotnet', 'RpcHost', 'bin', 'Release', 'net8.0', descriptor.runtimeId, descriptor.executableName),
      path.join(repoPath, 'service-dotnet', 'RpcHost', 'bin', 'Release', 'net8.0', descriptor.runtimeId, 'publish', descriptor.executableName),
    ];

    const checkedPaths = possiblePaths.map((candidate) => path.resolve(candidate));
    for (const absolutePath of checkedPaths) {
      if (existsSync(absolutePath)) {
        return { serverPath: absolutePath, checkedPaths };
      }
    }

    const issue: BackendResolutionIssue = {
      code: 'backendMissing',
      message: `PBIR Design Analyzer backend for ${descriptor.target} was not found. The extension will continue in degraded mode.`,
      detail: 'Build the packaged host first with `cd vscode-extension && npm run build:backend` or install the matching platform VSIX.',
      checkedPaths,
    };
    recordBackendIssue(issue);
    return { issue };
  } catch (error) {
    console.error('[RPC] Error finding analyzer backend path:', error);
    const issue: BackendResolutionIssue = {
      code: 'runtimeUnavailable',
      message: 'PBIR Design Analyzer could not resolve the backend path. The extension will continue in degraded mode.',
      detail: error instanceof Error ? error.message : String(error),
    };
    recordBackendIssue(issue);
    return { issue };
  }
}

export function inspectBackendBinary(executablePath: string): {
  fileName: string;
  format: 'pe' | 'elf' | 'mach-o' | 'unknown';
  architecture: string;
  description: string;
} {
  const fileName = path.basename(executablePath);

  try {
    const buffer = readFileSync(executablePath);
    if (buffer.length >= 0x86 && buffer.readUInt16LE(0) === 0x5a4d) {
      const peOffset = buffer.readUInt32LE(0x3c);
      if (buffer.length >= peOffset + 6 && buffer.toString('ascii', peOffset, peOffset + 4) === 'PE\u0000\u0000') {
        const machine = buffer.readUInt16LE(peOffset + 4);
        const architecture = machine === 0xaa64 ? 'arm64' : machine === 0x8664 ? 'x64' : `machine-0x${machine.toString(16)}`;
        return {
          fileName,
          format: 'pe',
          architecture,
          description: `PE ${architecture} executable`,
        };
      }
    }

    if (buffer.length >= 20 && buffer[0] === 0x7f && buffer[1] === 0x45 && buffer[2] === 0x4c && buffer[3] === 0x46) {
      const machine = buffer.readUInt16LE(18);
      const architecture = machine === 183 ? 'arm64' : machine === 62 ? 'x64' : `machine-${machine}`;
      return {
        fileName,
        format: 'elf',
        architecture,
        description: `ELF ${architecture} executable`,
      };
    }

    if (buffer.length >= 8) {
      const magic = buffer.readUInt32BE(0);
      if (magic === 0xfeedfacf || magic === 0xcffaedfe) {
        const cpuType = buffer.readUInt32BE(4);
        const architecture = cpuType === 0x0100000c ? 'arm64' : cpuType === 0x01000007 ? 'x64' : `cpu-0x${cpuType.toString(16)}`;
        return {
          fileName,
          format: 'mach-o',
          architecture,
          description: `Mach-O ${architecture} executable`,
        };
      }
    }
  } catch {
    // Fall through to unknown.
  }

  return {
    fileName,
    format: 'unknown',
    architecture: 'unknown',
    description: `Unknown executable format (${fileName})`,
  };
}

export function buildBackendLaunchDiagnostics(
  descriptor: BackendRuntimeDescriptor,
  resolvedBackendPath: string,
  checkedPaths: string[],
): BackendLaunchDiagnostics {
  const binary = inspectBackendBinary(resolvedBackendPath);
  return {
    processPlatform: process.platform,
    processArch: process.arch,
    vscodeTarget: descriptor.target,
    runtimeId: descriptor.runtimeId,
    selectedTarget: descriptor.target,
    resolvedBackendPath,
    backendExists: existsSync(resolvedBackendPath),
    backendFileName: binary.fileName,
    backendFileDescription: binary.description,
    launchCommand: resolvedBackendPath,
    selfContained: descriptor.selfContained,
    checkedPaths,
    dotnetRuntime: detectDotnetRuntime(descriptor.selfContained),
  };
}

export async function runBackendLaunchPreflight(
  diagnostics: BackendLaunchDiagnostics,
  timeoutMs: number = 800,
): Promise<BackendLaunchDiagnostics> {
  const child = spawn(diagnostics.launchCommand, [], {
    env: { ...process.env },
    stdio: ['pipe', 'pipe', 'pipe'],
    windowsHide: true,
  });

  const stdoutLines: string[] = [];
  const stderrLines: string[] = [];

  const collect = (buffer: string, target: string[]) => {
    for (const line of buffer.split(/\r?\n/)) {
      const trimmed = line.trim();
      if (trimmed.length > 0 && target.length < 6) {
        target.push(trimmed);
      }
    }
  };

  child.stdout?.on('data', (chunk) => collect(String(chunk), stdoutLines));
  child.stderr?.on('data', (chunk) => collect(String(chunk), stderrLines));

  return await new Promise<BackendLaunchDiagnostics>((resolve) => {
    let settled = false;

    const finish = (preflight: BackendLaunchPreflight) => {
      if (settled) {
        return;
      }
      settled = true;
      resolve({ ...diagnostics, preflight });
    };

    const timer = setTimeout(() => {
      try {
        child.kill();
      } catch {
        // Ignore cleanup failure.
      }
      finish({
        attempted: true,
        exitedEarly: false,
        exitCode: null,
        signal: null,
        stdoutLines,
        stderrLines,
      });
    }, timeoutMs);

    child.on('error', (error) => {
      clearTimeout(timer);
      stderrLines.push(error.message);
      finish({
        attempted: true,
        exitedEarly: true,
        exitCode: null,
        signal: null,
        stdoutLines,
        stderrLines,
      });
    });

    child.on('exit', (code, signal) => {
      clearTimeout(timer);
      finish({
        attempted: true,
        exitedEarly: true,
        exitCode: code,
        signal,
        stdoutLines,
        stderrLines,
      });
    });
  });
}

export function formatBackendLaunchDiagnostics(
  diagnostics: BackendLaunchDiagnostics,
  handshakeFailureReason?: string,
): string {
  const lines = [
    `VS Code target: ${diagnostics.vscodeTarget}`,
    `process.platform: ${diagnostics.processPlatform}`,
    `process.arch: ${diagnostics.processArch}`,
    `Selected VSIX target: ${diagnostics.selectedTarget}`,
    `Resolved backend path: ${diagnostics.resolvedBackendPath}`,
    `Backend exists: ${diagnostics.backendExists ? 'yes' : 'no'}`,
    `Backend file: ${diagnostics.backendFileName}`,
    `Backend file type: ${diagnostics.backendFileDescription}`,
    `Launch command: ${diagnostics.launchCommand}`,
    `Backend runtime packaging: ${diagnostics.selfContained ? 'self-contained' : 'framework-dependent'}`,
    `dotnet detection: ${diagnostics.dotnetRuntime.available ? 'available' : 'not available'}`,
  ];

  if (diagnostics.dotnetRuntime.exitCode !== null) {
    lines.push(`dotnet exit code: ${diagnostics.dotnetRuntime.exitCode}`);
  }

  if (diagnostics.dotnetRuntime.firstLines.length > 0) {
    lines.push(`dotnet output: ${diagnostics.dotnetRuntime.firstLines.join(' | ')}`);
  }

  if (diagnostics.preflight) {
    lines.push(`Preflight exited early: ${diagnostics.preflight.exitedEarly ? 'yes' : 'no'}`);
    if (diagnostics.preflight.exitCode !== null) {
      lines.push(`Preflight exit code: ${diagnostics.preflight.exitCode}`);
    }
    if (diagnostics.preflight.signal) {
      lines.push(`Preflight signal: ${diagnostics.preflight.signal}`);
    }
    if (diagnostics.preflight.stdoutLines.length > 0) {
      lines.push(`Preflight stdout: ${diagnostics.preflight.stdoutLines.join(' | ')}`);
    }
    if (diagnostics.preflight.stderrLines.length > 0) {
      lines.push(`Preflight stderr: ${diagnostics.preflight.stderrLines.join(' | ')}`);
    }
  }

  if (handshakeFailureReason) {
    lines.push(`Handshake/ping failure: ${handshakeFailureReason}`);
  }

  return lines.join('\n');
}

export async function stopAnalyzerBackendClient(client: LanguageClient): Promise<void> {
  try {
    if (client.isRunning()) {
      await client.stop();
    }
  } catch (error) {
    console.error('[RPC] Error stopping analyzer backend client:', error);
  }
}

function detectDotnetRuntime(selfContained: boolean): DotnetRuntimeDiagnostics {
  if (selfContained) {
    return {
      available: true,
      command: 'dotnet',
      exitCode: 0,
      firstLines: ['Self-contained backend package; external .NET runtime not required for launch.'],
    };
  }

  const result = spawnSync('dotnet', ['--info'], {
    encoding: 'utf8',
    shell: process.platform === 'win32',
  });

  if (result.error) {
    return {
      available: false,
      command: 'dotnet',
      exitCode: null,
      firstLines: [result.error.message],
    };
  }

  const output = `${result.stdout ?? ''}\n${result.stderr ?? ''}`;
  const firstLines = output
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.length > 0)
    .slice(0, 6);

  return {
    available: result.status === 0,
    command: 'dotnet',
    exitCode: result.status,
    firstLines,
  };
}

function createRuntimeUnavailableIssue(
  message: string,
  diagnostics: BackendLaunchDiagnostics,
  handshakeFailureReason?: string,
): BackendResolutionIssue {
  return {
    code: 'runtimeUnavailable',
    message,
    detail: formatBackendLaunchDiagnostics(diagnostics, handshakeFailureReason),
    diagnostics,
  };
}

function isBackendLaunchDiagnostics(
  value: BackendRuntimeDescriptor | BackendLaunchDiagnostics | undefined,
): value is BackendLaunchDiagnostics {
  return Boolean(value) && typeof value === 'object' && 'resolvedBackendPath' in value;
}
