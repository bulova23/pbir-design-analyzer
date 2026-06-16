import fs from 'fs';
import path from 'path';
import {
  backendTargets,
  backendTargetMap,
  getRuntimeCriticalFiles,
} from './backend-targets.mjs';

const rootDir = process.cwd();
const targetsRoot = path.join(rootDir, 'backend', 'targets');
const targetDirectories = backendTargets.map((descriptor) => descriptor.target).sort();

const issues = [];

if (!fs.existsSync(targetsRoot)) {
  issues.push(`Missing backend target root: ${targetsRoot}`);
} else {
  const discoveredTargets = fs.readdirSync(targetsRoot, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => entry.name)
    .sort();

  for (const expectedTarget of targetDirectories) {
    if (!discoveredTargets.includes(expectedTarget)) {
      issues.push(`Missing backend target directory: backend/targets/${expectedTarget}`);
    }
  }

  for (const discoveredTarget of discoveredTargets) {
    if (!backendTargetMap.has(discoveredTarget)) {
      issues.push(`Unexpected backend target directory: backend/targets/${discoveredTarget}`);
    }
  }

  for (const descriptor of backendTargets) {
    const rpcDir = path.join(targetsRoot, descriptor.target, 'rpc');
    if (!fs.existsSync(rpcDir)) {
      issues.push(`Missing backend runtime directory: backend/targets/${descriptor.target}/rpc`);
      continue;
    }

    for (const fileName of getRuntimeCriticalFiles(descriptor)) {
      const filePath = path.join(rpcDir, fileName);
      if (!fs.existsSync(filePath)) {
        issues.push(`Missing runtime asset: backend/targets/${descriptor.target}/rpc/${fileName}`);
      }
    }
  }
}

if (issues.length > 0) {
  console.error('Backend target verification failed:');
  for (const issue of issues) {
    console.error(`- ${issue}`);
  }
  process.exit(1);
}

console.log('Backend target verification passed.');
console.log(`Verified ${targetDirectories.length} packaged targets under backend/targets.`);
