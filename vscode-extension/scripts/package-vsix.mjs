import fs from 'fs';
import os from 'os';
import { spawnSync } from 'child_process';
import path from 'path';
import { backendTargets, detectDefaultTarget } from './backend-targets.mjs';

const rootDir = process.cwd();
const lockPath = path.join(rootDir, '.package-vsix.lock');
const vsceBin = path.join(rootDir, 'node_modules', '@vscode', 'vsce', 'vsce');

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : undefined;
}

const packageJson = JSON.parse(fs.readFileSync(path.resolve('package.json'), 'utf8'));
const version = packageJson.version;

const requestedTargets = process.argv.includes('--all')
  ? backendTargets.map((descriptor) => descriptor.target)
  : [readArg('--target') ?? detectDefaultTarget()];

acquirePackagingLock();

try {
  run(['npm', 'run', 'compile']);
  run(['npm', 'run', 'bundle:extension']);
  run(['npm', 'run', 'build:webview']);

  for (const target of requestedTargets) {
    const backendOutputDir = path.join(rootDir, 'backend', 'targets', target, 'rpc');
    run(['node', 'scripts/build-backend.mjs', '--target', target, '--output', backendOutputDir]);
    packageTarget(target, backendOutputDir);
  }
} finally {
  releasePackagingLock();
}

function packageTarget(target, backendOutputDir) {
  const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), `pbir-vsix-${target}-`));
  try {
    stagePackageRoot(tempRoot, backendOutputDir);
    const outputPath = path.join(rootDir, `pbir-design-analyzer-${version}-${target}.vsix`);
    fs.rmSync(outputPath, { force: true });

    const result = spawnSync(
      process.execPath,
      [
        vsceBin,
        'package',
        '--no-dependencies',
        '--target',
        target,
        '--out',
        outputPath,
      ],
      {
        cwd: tempRoot,
        stdio: 'inherit',
        env: {
          ...process.env,
          PBIR_SKIP_VSCE_PREPUBLISH: '1',
        },
      },
    );

    if (result.status !== 0) {
      throw new Error(`vsce packaging failed for ${target} with exit code ${result.status ?? 1}.`);
    }
  } finally {
    fs.rmSync(tempRoot, { recursive: true, force: true });
  }
}

function stagePackageRoot(tempRoot, backendOutputDir) {
  copyItem(path.join(rootDir, '.vscodeignore'), path.join(tempRoot, '.vscodeignore'));
  copyItem(path.join(rootDir, 'LICENSE'), path.join(tempRoot, 'LICENSE'));
  copyItem(path.join(rootDir, 'README.md'), path.join(tempRoot, 'README.md'));
  copyItem(path.join(rootDir, 'package.json'), path.join(tempRoot, 'package.json'));
  copyItem(path.join(rootDir, 'config'), path.join(tempRoot, 'config'));
  copyItem(path.join(rootDir, 'dist'), path.join(tempRoot, 'dist'));
  copyItem(path.join(rootDir, 'resources'), path.join(tempRoot, 'resources'));
  copyItem(path.join(rootDir, 'webview-dist'), path.join(tempRoot, 'webview-dist'));
  copyItem(path.join(rootDir, 'scripts', 'vsce-prepublish.mjs'), path.join(tempRoot, 'scripts', 'vsce-prepublish.mjs'));
  copyItem(backendOutputDir, path.join(tempRoot, 'backend', 'rpc'));
}

function copyItem(source, destination) {
  const stats = fs.statSync(source);
  if (stats.isDirectory()) {
    fs.mkdirSync(destination, { recursive: true });
    for (const entry of fs.readdirSync(source)) {
      copyItem(path.join(source, entry), path.join(destination, entry));
    }
    return;
  }

  fs.mkdirSync(path.dirname(destination), { recursive: true });
  fs.copyFileSync(source, destination);
}

function acquirePackagingLock() {
  try {
    fs.writeFileSync(lockPath, String(process.pid), { flag: 'wx' });
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    throw new Error(`Another packaging run is already using the VSIX build lock at ${lockPath}. ${message}`);
  }
}

function releasePackagingLock() {
  fs.rmSync(lockPath, { force: true });
}

function run(command) {
  const [bin, ...args] = command;
  const result = spawnSync(bin, args, { stdio: 'inherit', shell: process.platform === 'win32' });
  if (result.status !== 0) {
    throw new Error(`Command failed: ${[bin, ...args].join(' ')} (exit ${result.status ?? 1})`);
  }
}
