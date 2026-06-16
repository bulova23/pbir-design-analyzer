import fs from 'fs';
import path from 'path';
import { spawnSync } from 'child_process';
import { backendTargetMap, detectDefaultTarget } from './backend-targets.mjs';

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : undefined;
}

const target = readArg('--target') ?? detectDefaultTarget();
const targetConfig = backendTargetMap.get(target);
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
