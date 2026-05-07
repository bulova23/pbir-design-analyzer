import * as vscode from 'vscode';
import * as path from 'path';
import { LanguageClient, LanguageClientOptions, ServerOptions, State, RevealOutputChannelOn } from 'vscode-languageclient/node';

/**
 * Creates and configures the PBIR analyzer backend client.
 * The transport remains LSP/JSON-RPC for now even though the shipped v1 surface
 * is focused on PBIR analysis rather than TMDL authoring.
 */
export function createTmdlLanguageClient(context: vscode.ExtensionContext): LanguageClient | undefined {
  try {
    // Path to the .NET LSP server executable
    // This will be the standalone LspHost executable we create
    const serverPath = getLspServerPath(context);
    
    if (!serverPath) {
      console.warn('[LSP] No LSP server path found. LSP features disabled.');
      return undefined;
    }

    // Server options - launch the .NET LSP server via stdio
    console.log('[LSP] Creating server with command:', serverPath);
    
    const serverOptions: ServerOptions = {
      run: {
        command: serverPath,
        args: [],
        options: {
          env: {
            ...process.env,
            DOTNET_ROOT: '/usr/local/share/dotnet',  // Ensure .NET is found
            PATH: `${process.env.PATH}:/usr/local/share/dotnet`,
          }
        }
      },
      debug: {
        command: serverPath,
        args: [],
        options: {
          env: {
            ...process.env,
            DOTNET_ROOT: '/usr/local/share/dotnet',
            PATH: `${process.env.PATH}:/usr/local/share/dotnet`,
          }
        }
      }
    };

    // Client options - keep the transport lightweight. TMDL editing features are no longer
    // part of the active v1 surface, so the client is primarily used as a request bridge.
    const clientOptions: LanguageClientOptions = {
      documentSelector: [
        { scheme: 'file', language: 'tmdl' }
      ],
      outputChannel: vscode.window.createOutputChannel('PBIR Design Analyzer Backend'),
      traceOutputChannel: vscode.window.createOutputChannel('PBIR Design Analyzer Backend Trace'),
      revealOutputChannelOn: RevealOutputChannelOn.Info,
      connectionOptions: {
        maxRestartCount: 3
      }
    };

    // Create the language client
    const client = new LanguageClient(
      'pbir-design-analyzer-backend',
      'PBIR Design Analyzer Backend',
      serverOptions,
      clientOptions
    );

    // Set up event handlers for client lifecycle
    client.onDidChangeState((event) => {
      console.log(`[LSP] State changed: ${event.oldState} → ${event.newState}`);
      // State: Stopped = 1, Starting = 2, Running = 3
      if (event.newState === State.Running) {
        console.log('[LSP] ✅ TMDL Language Server started successfully');
      } else if (event.newState === State.Stopped && event.oldState === State.Starting) {
        console.error('[LSP] ❌ Server failed to start (returned to Stopped state)');
        vscode.window.showErrorMessage(
          'PBIR Design Analyzer backend failed to start. Check that the packaged binary is executable and .NET 8 is installed.',
          'Show Output'
        ).then(selection => {
          if (selection === 'Show Output') {
            vscode.commands.executeCommand('workbench.action.output.show');
          }
        });
      }
    });

    return client;
  } catch (error) {
    console.error('[LSP] Failed to create language client:', error);
    return undefined;
  }
}

/**
 * Find the backend executable path.
 * Checks the packaged `backend/lsp` bundle first, then falls back to local repo builds.
 */
function getLspServerPath(context: vscode.ExtensionContext): string | null {
  try {
    const repoPath = path.resolve(context.extensionPath, '..');
    console.log('[LSP] Using extension repository path for backend lookup:', repoPath);

    const possiblePaths = [
      path.join(context.extensionPath, 'backend', 'lsp', 'ModelingLanguageServer'),
      path.join(repoPath, 'vscode-extension', 'backend', 'lsp', 'ModelingLanguageServer'),
      path.join(repoPath, 'service-dotnet', 'LspHost', 'bin', 'Debug', 'net8.0', 'ModelingLanguageServer'),
      path.join(repoPath, 'service-dotnet', 'LspHost', 'bin', 'Release', 'net8.0', 'ModelingLanguageServer'),
      path.join(repoPath, 'service-dotnet', 'LspHost', 'bin', 'Release', 'net8.0', 'publish', 'ModelingLanguageServer'),
      path.join(repoPath, 'bin', 'Debug', 'net8.0', 'ModelingLanguageServer'), // legacy location
      path.join(repoPath, 'bin', 'Release', 'net8.0', 'ModelingLanguageServer'),
    ];

    // On macOS/Linux, executables have no extension
    // On Windows, they end with .exe
    const fs = require('fs');

    for (const basePath of possiblePaths) {
      // Resolve to absolute path
      const absolutePath = path.resolve(basePath);

      // Try without extension (macOS/Linux)
      if (fs.existsSync(absolutePath)) {
        console.log('[LSP] ✅ Found LSP server at:', absolutePath);
        return absolutePath;
      }

      // Try with .exe extension (Windows)
      const exePath = absolutePath + '.exe';
      if (fs.existsSync(exePath)) {
        console.log('[LSP] ✅ Found LSP server at:', exePath);
        return exePath;
      }
    }

    const checkedPaths = possiblePaths.map(p => path.resolve(p));
    console.error('[LSP] ❌ LSP server executable not found at expected locations');
    console.error('[LSP] Searched in:', repoPath);
    console.error('[LSP] Checked paths:', checkedPaths);

    // Show user-visible error
    vscode.window.showErrorMessage(
      `PBIR Design Analyzer backend not found at ${checkedPaths[0]}. ` +
      `Build the packaged host first: cd vscode-extension && npm run build:backend`,
      'Show Paths'
    ).then(selection => {
      if (selection === 'Show Paths') {
        vscode.window.showInformationMessage(
          'Searched in: ' + repoPath + '\n\nFirst 3 paths checked:\n' + checkedPaths.slice(0, 3).join('\n'),
          { modal: true }
        );
      }
    });

    return null;
  } catch (error) {
    console.error('[LSP] Error finding LSP server path:', error);
    return null;
  }
}

/**
 * Stop the language client gracefully
 */
export async function stopTmdlLanguageClient(client: LanguageClient): Promise<void> {
  try {
    if (client.isRunning()) {
      console.log('[LSP] Stopping TMDL Language Server');
      await client.stop();
    }
  } catch (error) {
    console.error('[LSP] Error stopping language client:', error);
  }
}
