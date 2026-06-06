import fs from 'fs';
import path from 'path';
import { spawnSync } from 'child_process';

const targetMap = new Map([
  ['win32-x64', { runtimeId: 'win-x64', selfContained: false }],
  ['win32-arm64', { runtimeId: 'win-arm64', selfContained: true }],
  ['linux-x64', { runtimeId: 'linux-x64', selfContained: false }],
  ['darwin-x64', { runtimeId: 'osx-x64', selfContained: false }],
  ['darwin-arm64', { runtimeId: 'osx-arm64', selfContained: false }],
]);

function detectDefaultTarget() {
  const platform = process.platform;
  const arch = process.arch;
  if (platform === 'win32' && arch === 'x64') {
    return 'win32-x64';
  }
  if (platform === 'win32' && arch === 'arm64') {
    return 'win32-arm64';
  }
  if (platform === 'linux' && arch === 'x64') {
    return 'linux-x64';
  }
  if (platform === 'darwin' && arch === 'x64') {
    return 'darwin-x64';
  }
  if (platform === 'darwin' && arch === 'arm64') {
    return 'darwin-arm64';
  }

  throw new Error(`Unsupported local build platform: ${platform}-${arch}`);
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : undefined;
}

const target = readArg('--target') ?? detectDefaultTarget();
const targetConfig = targetMap.get(target);
const runtimeId = readArg('--rid') ?? targetConfig?.runtimeId;
if (!runtimeId || !targetConfig) {
  throw new Error(`Unsupported target: ${target}`);
}

const outputDir = path.resolve(readArg('--output') ?? 'backend/rpc');
const projectPath = path.resolve('../service-dotnet/RpcHost/RpcHost.csproj');

fs.rmSync(outputDir, { recursive: true, force: true });

const result = spawnSync(
  'dotnet',
  [
    'publish',
    projectPath,
    '-c',
    'Release',
    '-r',
    runtimeId,
    '--self-contained',
    String(targetConfig.selfContained),
    '-p:UseAppHost=true',
    '-o',
    outputDir,
  ],
  {
    stdio: 'inherit',
  },
);

if (result.status !== 0) {
  process.exit(result.status ?? 1);
}
