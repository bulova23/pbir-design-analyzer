import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync, spawnSync } from 'node:child_process';
const root = path.resolve(new URL('..', import.meta.url).pathname);
const extension = path.join(root, 'vscode-extension');
const version = JSON.parse(fs.readFileSync(path.join(extension, 'package.json'), 'utf8')).version;
const targets = JSON.parse(fs.readFileSync(path.join(extension, 'config', 'release-targets.json'), 'utf8')).targets;
const sha256 = (file) => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const artifacts = targets.map((descriptor) => {
  const packagePath = path.join(extension, `pbir-design-analyzer-${version}-${descriptor.target}.vsix`);
  const packageInfo = fs.existsSync(packagePath) ? { path: path.relative(root, packagePath), sha256: sha256(packagePath) } : null;
  const backendEntry = `extension/backend/rpc/${descriptor.executableName}`;
  const backend = packageInfo ? spawnSync('unzip', ['-p', packagePath, backendEntry]).stdout : null;
  return { target: descriptor.target, runtimeId: descriptor.runtimeId, package: packageInfo, backend: backend?.length ? { path: backendEntry, sha256: crypto.createHash('sha256').update(backend).digest('hex') } : null };
});
const manifest = {
  schemaVersion: 'release-artifact-manifest/v1',
  sourceCommit: execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(),
  extensionVersion: version,
  nodeVersion: process.version,
  dotnetVersion: execFileSync('dotnet', ['--version'], { encoding: 'utf8' }).trim(),
  fixtureId: 'characterization-minimal',
  fixtureManifest: 'service-dotnet/tests/Fixtures/Characterization/manifest.json',
  artifacts,
  sbom: { status: 'not generated; optional under current v1.0 policy' },
  provenance: { status: 'workflow identity and source commit recorded; signed attestation not configured' }
};
const output = path.join(root, 'docs', 'release-evidence', 'artifact-manifest.json');
fs.mkdirSync(path.dirname(output), { recursive: true });
fs.writeFileSync(output, `${JSON.stringify(manifest, null, 2)}\n`);
console.log(JSON.stringify(manifest, null, 2));
