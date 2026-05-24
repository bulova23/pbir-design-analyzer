import { existsSync } from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import {
  LanguageClient,
  LanguageClientOptions,
  RevealOutputChannelOn,
  ServerOptions,
  State,
} from 'vscode-languageclient/node';

/**
 * Creates the PBIR analyzer backend client used as the JSON-RPC bridge to the
 * packaged .NET host.
 */
export function createAnalyzerBackendClient(
  context: vscode.ExtensionContext,
): LanguageClient | undefined {
  try {
    const serverPath = getBackendExecutablePath(context);

    if (!serverPath) {
      console.warn('[RPC] No analyzer backend path found. PBIR analysis features disabled.');
      return undefined;
    }

    const env = {
      ...process.env,
      DOTNET_ROOT: '/usr/local/share/dotnet',
      PATH: `${process.env.PATH}:/usr/local/share/dotnet`,
    };

    const serverOptions: ServerOptions = {
      run: { command: serverPath, args: [], options: { env } },
      debug: { command: serverPath, args: [], options: { env } },
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
        console.error('[RPC] Analyzer backend failed to start');
        vscode.window
          .showErrorMessage(
            'PBIR Design Analyzer backend failed to start. Check that the packaged binary is executable and .NET 8 is installed.',
            'Show Output',
          )
          .then((selection) => {
            if (selection === 'Show Output') {
              void vscode.commands.executeCommand('workbench.action.output.show');
            }
          });
      }
    });

    return client;
  } catch (error) {
    console.error('[RPC] Failed to create analyzer backend client:', error);
    return undefined;
  }
}

function getBackendExecutablePath(context: vscode.ExtensionContext): string | null {
  try {
    const repoPath = path.resolve(context.extensionPath, '..');
    const possiblePaths = [
      path.join(context.extensionPath, 'backend', 'rpc', 'ModelingLanguageServer'),
      path.join(repoPath, 'vscode-extension', 'backend', 'rpc', 'ModelingLanguageServer'),
      path.join(repoPath, 'service-dotnet', 'RpcHost', 'bin', 'Debug', 'net8.0', 'ModelingLanguageServer'),
      path.join(repoPath, 'service-dotnet', 'RpcHost', 'bin', 'Release', 'net8.0', 'ModelingLanguageServer'),
      path.join(repoPath, 'service-dotnet', 'RpcHost', 'bin', 'Release', 'net8.0', 'publish', 'ModelingLanguageServer'),
    ];

    for (const basePath of possiblePaths) {
      const absolutePath = path.resolve(basePath);
      if (existsSync(absolutePath)) {
        return absolutePath;
      }

      const windowsPath = `${absolutePath}.exe`;
      if (existsSync(windowsPath)) {
        return windowsPath;
      }
    }

    const checkedPaths = possiblePaths.map((candidate) => path.resolve(candidate));
    vscode.window
      .showErrorMessage(
        'PBIR Design Analyzer backend not found. Build the packaged host first: cd vscode-extension && npm run build:backend',
        'Show Paths',
      )
      .then((selection) => {
        if (selection === 'Show Paths') {
          void vscode.window.showInformationMessage(
            `Checked paths:\n${checkedPaths.join('\n')}`,
            { modal: true },
          );
        }
      });

    return null;
  } catch (error) {
    console.error('[RPC] Error finding analyzer backend path:', error);
    return null;
  }
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
