import { runTests } from '@vscode/test-electron';
import path from 'path';

const repoRoot = path.resolve(new URL('../..', import.meta.url).pathname);
const extensionRoot = path.join(repoRoot, 'vscode-extension');
const smokeRoot = path.join('/tmp', 'pbir-phase2-deterministic-host');
const vscodeExecutablePath = '/Applications/Visual Studio Code.app/Contents/MacOS/Electron';

const exitCode = await runTests({
  vscodeExecutablePath,
  extensionDevelopmentPath: extensionRoot,
  extensionTestsPath: path.join(extensionRoot, 'scripts', 'phase2-packaged-smoke-runner.cjs'),
  launchArgs: [
    repoRoot,
    '--disable-workspace-trust',
    '--skip-welcome',
    '--skip-release-notes',
    '--disable-updates',
  ],
  extensionTestsEnv: {
    REPO_ROOT: repoRoot,
    REAL_FIXTURE_PATH: process.env.PBIR_REAL_FIXTURE_PATH
      ?? '/Users/bcrowell/Documents/GitHub/PBITesting/Sales & Production.pbip',
    SMOKE_ROOT: smokeRoot,
    SMOKE_MODE: 'deterministic',
  },
});

if (exitCode !== 0) {
  throw new Error(`Phase 2 deterministic host smoke failed with exit code ${exitCode}`);
}

console.log('Phase 2 deterministic host smoke passed.');
