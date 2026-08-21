import fs from 'fs';
import path from 'path';
import { execFileSync } from 'child_process';

const packageJson = JSON.parse(fs.readFileSync('package.json', 'utf8'));
const manifest = JSON.parse(fs.readFileSync('config/release-targets.json', 'utf8'));
const requestedPath = process.argv[2];
const vsixPath = path.resolve(requestedPath ?? `pbir-design-analyzer-${packageJson.version}.vsix`);

if (!fs.existsSync(vsixPath)) {
  throw new Error(`VSIX does not exist: ${vsixPath}`);
}

const entries = execFileSync('unzip', ['-Z1', vsixPath], { encoding: 'utf8' })
  .split(/\r?\n/)
  .filter(Boolean)
  .sort();
const entrySet = new Set(entries);
const target = manifest.targets.find((candidate) => vsixPath.includes(`-${candidate.target}.vsix`));

if (!target) {
  throw new Error(`VSIX filename does not identify a supported target: ${path.basename(vsixPath)}`);
}

const packageManifest = JSON.parse(execFileSync('unzip', ['-p', vsixPath, 'extension/package.json'], { encoding: 'utf8' }));
if (packageManifest.version !== packageJson.version) {
  throw new Error(`VSIX version ${packageManifest.version} does not match package version ${packageJson.version}.`);
}

const backendEntry = `extension/backend/rpc/${target.executableName}`;
if (!entrySet.has(backendEntry)) {
  throw new Error(`VSIX is missing the target backend entrypoint: ${backendEntry}`);
}

const forbiddenPrefixes = ['extension/node_modules/', 'extension/src/', 'extension/service-dotnet/'];
const forbiddenEntries = entries.filter((entry) => forbiddenPrefixes.some((prefix) => entry.startsWith(prefix)));
if (forbiddenEntries.length > 0) {
  throw new Error(`VSIX contains source/dependency content that must not ship: ${forbiddenEntries.join(', ')}`);
}

for (const required of ['extension/package.json', 'extension/dist/extension.js', 'extension/readme.md']) {
  if (!entrySet.has(required)) {
    throw new Error(`VSIX is missing required entry: ${required}`);
  }
}

console.log(JSON.stringify({
  vsix: path.basename(vsixPath),
  target: target.target,
  version: packageManifest.version,
  entryCount: entries.length,
  backendEntry,
}, null, 2));
