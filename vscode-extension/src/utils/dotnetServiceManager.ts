import * as vscode from 'vscode';
import { spawn, spawnSync, ChildProcessWithoutNullStreams } from 'child_process';

export class DotnetServiceManager {
  private proc: ChildProcessWithoutNullStreams | null = null;
  private output: vscode.OutputChannel;
  private readonly servicePath: string;
  private readonly candidatePorts: number[];
  private runningPort: number | null = null;

  constructor(servicePath: string, ports: number | number[]) {
    this.servicePath = servicePath;
    this.candidatePorts = Array.isArray(ports) ? ports : [ports];
    this.output = vscode.window.createOutputChannel('Power BI Modeling Service');
  }

  /**
   * Try to locate the `dotnet` executable. Return absolute path or null.
   */
  private resolveDotnetPath(): string | null {
    // 1) DOTNET_ROOT if set
    try {
      // Prefer the PATH-installed `dotnet` first (works better inside VS Code host env)
      try {
        const which = spawnSync('which', ['dotnet']);
        if (which.status === 0) {
          const path = which.stdout.toString().trim();
          if (path) {
            try {
              const r = spawnSync(path, ['--info'], { stdio: 'ignore' });
              if (r.status === 0) return path;
            } catch { }
          }
        }
      } catch { }

      try {
        const cmdv = spawnSync('command', ['-v', 'dotnet']);
        if (cmdv.status === 0) {
          const path = cmdv.stdout.toString().trim();
          if (path) {
            try {
              const r = spawnSync(path, ['--info'], { stdio: 'ignore' });
              if (r.status === 0) return path;
            } catch { }
          }
        }
      } catch { }

      // Next, honor DOTNET_ROOT if set
      const dotnetRoot = process.env.DOTNET_ROOT || process.env.DOTNET_HOME;
      if (dotnetRoot) {
        const candidate = `${dotnetRoot.replace(/"/g, '')}/dotnet`;
        try {
          const r = spawnSync(candidate, ['--info'], { stdio: 'ignore' });
          if (r.status === 0) return candidate;
        } catch { }
      }

      // Finally try common installation locations (macOS/Linux)
      const common = [
        '/usr/local/share/dotnet/dotnet',
        '/usr/local/bin/dotnet',
        '/usr/bin/dotnet',
        '/opt/homebrew/bin/dotnet'
      ];

      for (const p of common) {
        try {
          const r = spawnSync(p, ['--info'], { stdio: 'ignore' });
          if (r.status === 0) return p;
        } catch { }
      }
    } catch (error) {
      // ignore
    }

    return null;
  }

  async start(restart: boolean): Promise<void> {
    this.output.appendLine(`[Service] start() called with restart=${restart}, candidatePorts=${this.candidatePorts.join(',')}`);

    if (restart) {
      this.output.appendLine('[Service] Restart requested, stopping existing service...');
      await this.stop();
    }

    if (this.proc) {
      this.output.appendLine('[Service] Service process already exists, skipping start');
      return;
    }

    // Try each candidate port sequentially until one starts and reports healthy
    for (const port of this.candidatePorts) {
      this.output.appendLine(`[Service] Attempting to start service on port ${port}...`);

      // Immediately update client's base URL to avoid races where other parts
      // of the extension perform health checks against a stale port.
      // DEPRECATED: HTTP client removed in favor of CLI daemon
      // try {
      //   client.setBaseUrl(`http://localhost:${port}`);
      // } catch { }

      // Kill any process using the target port
      await this.killProcessOnPort(port);

      // Best-effort: if an external instance is running, kill it (macOS/Linux)
      try {
        this.output.appendLine('[Service] Attempting to kill any existing PowerBIModelingService processes...');
        const kill = spawn('pkill', ['-f', 'PowerBIModelingService']);
        await new Promise<void>((resolve) => {
          kill.on('exit', (code) => {
            this.output.appendLine(`[Service] pkill exited with code ${code}`);
            resolve();
          });
          // Timeout after 2 seconds
          setTimeout(() => resolve(), 2000);
        });
      } catch (error: any) {
        this.output.appendLine(`[Service] pkill failed: ${error.message}`);
      }

      // Wait a short moment for the OS to release sockets after kill
      await new Promise((r) => setTimeout(r, 500));

      this.output.appendLine(`[Service] Starting .NET service on port ${port}...`);
      this.output.appendLine(`[Service] Working directory: ${this.servicePath}`);

      const env = { ...process.env, PORT: String(port) };
      this.output.appendLine(`[Service] Environment: PORT=${port}`);

      // Allow an explicit `powerbiModeling.dotnetPath` or legacy `powerbi-mcp.dotnetPath` setting to override PATH resolution
      let dotnetPath: string | null = null;
      try {
        const cfg1 = vscode.workspace.getConfiguration('powerbiModeling');
        const cfg2 = vscode.workspace.getConfiguration('powerbi-mcp');
        const configured1 = cfg1.get<string>('dotnetPath');
        const configured2 = cfg2.get<string>('dotnetPath');
        const configured = configured1 || configured2;
        if (configured) {
          this.output.appendLine(`[Service] Using configured dotnet path from settings: ${configured}`);
          dotnetPath = configured;
        }
      } catch (err) {
        // ignore
      }

      if (!dotnetPath) dotnetPath = this.resolveDotnetPath();
      if (!dotnetPath) {
        this.output.appendLine('[Service] ✗ Could not locate `dotnet` executable in PATH or common locations.');
        this.output.appendLine(`[Service] PATH=${process.env.PATH}`);
        this.output.appendLine(`[Service] DOTNET_ROOT=${process.env.DOTNET_ROOT || ''}`);

        // Show an actionable warning rather than an error dialog so the extension can continue functioning
        const choice = await vscode.window.showWarningMessage(
          'Could not find `dotnet` executable. Install the .NET SDK or configure the `powerbiModeling.dotnetPath` setting.',
          'Install .NET',
          'Open Settings',
          'Ignore'
        );

        if (choice === 'Install .NET') {
          try {
            await vscode.env.openExternal(vscode.Uri.parse('https://dotnet.microsoft.com/en-us/download'));
          } catch (e) {
            this.output.appendLine('[Service] Failed to open browser to .NET download page');
          }
        } else if (choice === 'Open Settings') {
          try {
            await vscode.commands.executeCommand('workbench.action.openSettings', 'powerbiModeling.dotnetPath');
          } catch (e) {
            this.output.appendLine('[Service] Failed to open settings');
          }
        }

        // don't keep attempting other ports if dotnet is missing
        return;
      }

      this.output.appendLine(`[Service] Command: ${dotnetPath} run`);

      try {
        this.proc = spawn(dotnetPath, ['run'], { cwd: this.servicePath, env });
        this.output.appendLine(`[Service] Process spawned with PID: ${this.proc && this.proc.pid ? this.proc.pid : 'unknown'}`);
      } catch (error: any) {
        this.output.appendLine(`[Service] ✗ Failed to spawn dotnet process on port ${port}: ${error.message}`);
        this.proc = null;
        // try next port
        continue;
      }

      this.proc.stdout.on('data', (data) => {
        const output = data.toString().trim();
        this.output.appendLine(`[stdout] ${output}`);
      });

      this.proc.stderr.on('data', (data) => {
        const output = data.toString().trim();
        this.output.appendLine(`[stderr] ${output}`);
      });

      this.proc.on('error', (error) => {
        this.output.appendLine(`[Service] ✗ Process error: ${error.message}`);
        if ((error as any).code === 'ENOENT') {
          this.output.appendLine('[Service] ✗ ENOENT while spawning process - `dotnet` not found in extension host PATH.');
          this.output.appendLine(`[Service] PATH=${process.env.PATH}`);
          vscode.window.showWarningMessage('The extension cannot start the .NET helper because `dotnet` was not found in PATH. Install .NET SDK or configure DOTNET_ROOT/PATH.');
        }
        this.proc = null; // Set to null so waitForHealthy can stop
      });

      // Fallback: if the process fails to spawn (ENOENT), offer to run the service in an integrated terminal
      // Note: we do this separately because the above 'error' handler may run synchronously during spawn
      setTimeout(() => {
        if (!this.proc) {
          const cmd = `${dotnetPath || 'dotnet'} run`;
          this.output.appendLine(`[Service] Fallback: offering to run service in integrated terminal: cd ${this.servicePath} && ${cmd}`);

          vscode.window.showWarningMessage(
            'Could not spawn `dotnet` from the extension host. Run the service in an integrated terminal?',
            'Open Terminal',
            'Cancel'
          ).then((choice) => {
            if (choice === 'Open Terminal') {
              try {
                const term = vscode.window.createTerminal({ name: 'Power BI .NET Service' });
                term.show(true);
                // Ensure we cd into service path and run with the configured dotnet path if available
                term.sendText(`cd "${this.servicePath}" && ${cmd}`);
              } catch (e: any) {
                this.output.appendLine(`[Service] Failed to open integrated terminal: ${e.message}`);
              }
            }
          });
        }
      }, 200);

      this.proc.on('exit', (code, signal) => {
        this.output.appendLine(`[Service] Process exited with code ${code}, signal ${signal}`);
        this.proc = null;
      });

      this.output.appendLine('[Service] Process started, waiting for health check...');

      // Update client's base URL to point at candidate port before health checks
      // DEPRECATED: HTTP client removed in favor of CLI daemon
      // try {
      //   client.setBaseUrl(`http://localhost:${port}`);
      // } catch { }

      const healthy = false; // DEPRECATED: await this.waitForHealthy(client, 60000);
      if (healthy) {
        this.output.appendLine(`[Service] Service started and healthy on port ${port}`);
        this.runningPort = port;
        return;
      } else {
        this.output.appendLine(`[Service] Service failed to become healthy on port ${port}, stopping process and trying next port`);
        try { if (this.proc) this.proc.kill('SIGINT'); } catch { }
        this.proc = null;
        // small delay before next attempt
        await new Promise(r => setTimeout(r, 500));
        continue;
      }
    }

    // If we exit the loop without returning, none of the ports worked
    this.output.appendLine('[Service] ✗ All candidate ports failed to start a healthy service');
    vscode.window.showWarningMessage('Could not start .NET service on any candidate port. See output for details.');
  }

  async stop(): Promise<void> {
    if (this.proc) {
      this.output.appendLine('[Service] Stopping existing .NET service...');
      try {
        this.proc.kill('SIGINT');
      } catch { }
      this.proc = null;
      await new Promise((r) => setTimeout(r, 500));
    }
  }

  private async killProcessOnPort(port: number): Promise<void> {
    try {
      this.output.appendLine(`[Service] Checking for processes on port ${port}...`);

      // Use lsof to find processes using the port
      const lsof = spawn('lsof', ['-ti', `:${port}`]);
      let pids = '';

      lsof.stdout.on('data', (data) => {
        pids += data.toString();
      });

      await new Promise<void>((resolve) => {
        lsof.on('exit', async (code) => {
          if (code === 0 && pids.trim()) {
            const pidList = pids.trim().split('\n');
            this.output.appendLine(`[Service] Found ${pidList.length} process(es) on port ${port}: ${pidList.join(', ')}`);

            // Kill each process
            for (const pid of pidList) {
              try {
                this.output.appendLine(`[Service] Killing process ${pid}...`);
                process.kill(parseInt(pid), 'SIGTERM');
              } catch (error: any) {
                this.output.appendLine(`[Service] Failed to kill process ${pid}: ${error.message}`);
              }
            }

            // Wait for processes to die
            await new Promise((r) => setTimeout(r, 500));
          } else {
            this.output.appendLine(`[Service] No processes found on port ${port}`);
          }
          resolve();
        });

        // Timeout after 3 seconds
        setTimeout(() => resolve(), 3000);
      });
    } catch (error: any) {
      this.output.appendLine(`[Service] Error checking port ${port}: ${error.message}`);
    }
  }

  /**
   * Wait for the service to report healthy. Returns true if healthy within timeout.
   */
  private async waitForHealthy(timeoutMs: number = 60000): Promise<boolean> {
    const start = Date.now();
    let attemptCount = 0;

    this.output.appendLine(`[Service] Waiting for service to become healthy (timeout: ${timeoutMs}ms)...`);

    while (Date.now() - start < timeoutMs) {
      // If the process has died or failed to spawn (no PID), stop waiting
      if (!this.proc || !this.proc.pid) {
        this.output.appendLine('[Service] ✗ Process exited prematurely or failed to spawn during health check wait');
        return false;
      }

      attemptCount++;
      const elapsed = Date.now() - start;

      try {
        this.output.appendLine(`[Service] Health check attempt ${attemptCount} (${elapsed}ms elapsed)...`);
        // DEPRECATED: HTTP client removed
        // const h = await client.health();
        const h: any = null;

        this.output.appendLine(`[Service] Health check response: status=${h.status}, connected=${h.connected}`);

        if (h.status === 'ok') {
          this.output.appendLine(`[Service] ✓ Service is healthy after ${elapsed}ms (${attemptCount} attempts)`);
          return true;
        } else {
          this.output.appendLine(`[Service] Service responded but status is not 'ok': ${h.status}`);
        }
      } catch (error: any) {
        this.output.appendLine(`[Service] Health check failed: ${error.message}`);

        if (error.response) {
          this.output.appendLine(`[Service] HTTP Status: ${error.response.status}`);
          try {
            // Log headers and raw response data for diagnosis (may be HTML/error page)
            this.output.appendLine(`[Service] Response headers: ${JSON.stringify(error.response.headers)}`);
            const raw = error.response.data;
            const rawStr = Buffer.isBuffer(raw) ? raw.toString('utf8') : String(raw);
            this.output.appendLine(`[Service] Response body (first 2000 chars): ${rawStr.slice(0, 2000)}`);
          } catch (e: any) {
            this.output.appendLine(`[Service] Failed to stringify error.response: ${e.message}`);
          }
        } else if (error.code) {
          this.output.appendLine(`[Service] Error code: ${error.code}`);
        }
      }

      await new Promise((r) => setTimeout(r, 1000));
    }

    this.output.appendLine(`[Service] ✗ Timeout after ${timeoutMs}ms and ${attemptCount} attempts`);
    return false;
  }
}
