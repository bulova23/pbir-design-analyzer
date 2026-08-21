import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { runTests } from '@vscode/test-electron';
import { backendTargets } from './backend-targets.mjs';

const extensionRoot = process.cwd();
const repoRoot = path.resolve(extensionRoot, '..');
const version = JSON.parse(fs.readFileSync(path.join(extensionRoot, 'package.json'), 'utf8')).version;
const packageJson = JSON.parse(fs.readFileSync(path.join(extensionRoot, 'package.json'), 'utf8'));
const target = process.argv[process.argv.indexOf('--target') + 1] ?? backendTargets.find((item) => item.target === 'darwin-arm64')?.target;
const descriptor = backendTargets.find((item) => item.target === target);
if (!descriptor) throw new Error(`Unknown target: ${target ?? '(none)'}`);
const vsixPath = path.join(extensionRoot, `pbir-design-analyzer-${version}-${target}.vsix`);
if (!fs.existsSync(vsixPath)) throw new Error(`VSIX is missing: ${vsixPath}`);

const hostExecutable = process.env.PBIR_VSCODE_EXECUTABLE
  ?? '/Applications/Visual Studio Code.app/Contents/MacOS/Code';
if (!fs.existsSync(hostExecutable)) throw new Error(`VS Code test host is missing: ${hostExecutable}`);

const root = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-packaged-host-'));
const userDataDir = path.join(root, 'user-data');
const extensionsDir = path.join(root, 'extensions');
fs.mkdirSync(userDataDir, { recursive: true });
fs.mkdirSync(extensionsDir, { recursive: true });

const cli = process.env.PBIR_VSCODE_CLI ?? 'code';
const install = spawnSync(cli, [
  '--user-data-dir', userDataDir,
  '--extensions-dir', extensionsDir,
  '--install-extension', vsixPath,
  '--force',
], { stdio: 'inherit' });
if (install.status !== 0) throw new Error(`VSIX installation failed with exit code ${install.status ?? 1}.`);

const installedExtensionPath = fs.readdirSync(extensionsDir, { withFileTypes: true })
  .filter((entry) => entry.isDirectory() && entry.name.startsWith(`${packageJson.publisher}.${packageJson.name}-`))
  .map((entry) => path.join(extensionsDir, entry.name))
  .find((candidate) => fs.existsSync(path.join(candidate, 'package.json')));
if (!installedExtensionPath) throw new Error('Installed VSIX extension directory was not found.');

const exitCode = await runTests({
  vscodeExecutablePath: hostExecutable,
  extensionDevelopmentPath: installedExtensionPath,
  extensionTestsPath: path.join(extensionRoot, 'scripts', 'packaged-host-acceptance-runner.cjs'),
  launchArgs: [
    '--user-data-dir', userDataDir,
    '--extensions-dir', extensionsDir,
    '--disable-workspace-trust',
    '--skip-welcome',
    '--skip-release-notes',
    '--disable-updates',
  ],
  extensionTestsEnv: {
    EXPECTED_EXTENSION_ID: `${packageJson.publisher}.${packageJson.name}`,
    EXPECTED_BACKEND_PATH: path.join(installedExtensionPath, 'backend', 'rpc', descriptor.executableName),
    EXPECTED_VERSION: version,
  },
});
if (exitCode !== 0) throw new Error(`Installed VSIX host acceptance failed with exit code ${exitCode}.`);

const evidence = {
  target,
  version,
  acceptanceLayer: 'installed-vscode-host',
  activated: true,
  backendRelativePath: `extension/backend/rpc/${descriptor.executableName}`,
};
const evidenceDir = path.join(repoRoot, 'docs', 'release-evidence');
fs.mkdirSync(evidenceDir, { recursive: true });
fs.writeFileSync(path.join(evidenceDir, `packaged-host-acceptance-${target}.json`), `${JSON.stringify(evidence, null, 2)}\n`);
console.log(JSON.stringify(evidence, null, 2));
