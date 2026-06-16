import fs from 'fs';
import path from 'path';
import { backendTargets } from './backend-targets.mjs';

const rootDir = process.cwd();
const targetsRoot = path.join(rootDir, 'backend', 'targets');
const removableTargets = new Set(backendTargets.map((descriptor) => descriptor.target));
const requestedTarget = readArg('--target');

if (requestedTarget && !removableTargets.has(requestedTarget)) {
  throw new Error(`Unsupported target: ${requestedTarget}`);
}

const targetsToRemove = requestedTarget
  ? [requestedTarget]
  : [...removableTargets];

let removedCount = 0;

for (const target of targetsToRemove) {
  const targetDir = path.join(targetsRoot, target);
  if (!fs.existsSync(targetDir)) {
    continue;
  }

  fs.rmSync(targetDir, { recursive: true, force: true });
  console.log(`Removed backend target staging: backend/targets/${target}`);
  removedCount += 1;
}

if (removedCount === 0) {
  console.log('No backend target staging directories were removed.');
} else {
  console.log(`Removed ${removedCount} backend target staging director${removedCount === 1 ? 'y' : 'ies'}.`);
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : undefined;
}
