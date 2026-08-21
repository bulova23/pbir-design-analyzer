import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const repositoryRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const releaseContract = JSON.parse(
  fs.readFileSync(path.join(repositoryRoot, 'vscode-extension', 'config', 'release-targets.json'), 'utf8'),
);

export const backendTargets = releaseContract.targets;

export const backendTargetMap = new Map(
  backendTargets.map((descriptor) => [descriptor.target, descriptor]),
);

export function detectDefaultTarget(platform = process.platform, arch = process.arch) {
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

  throw new Error(`Unsupported local platform: ${platform}-${arch}`);
}

export function getRuntimeCriticalFiles(descriptor) {
  return [
    descriptor.executableName,
    'ModelingLanguageServer.dll',
    'ModelingLanguageServer.deps.json',
    'ModelingLanguageServer.runtimeconfig.json',
  ];
}
