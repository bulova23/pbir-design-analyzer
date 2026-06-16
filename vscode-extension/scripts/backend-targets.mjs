export const backendTargets = [
  {
    target: 'win32-x64',
    runtimeId: 'win-x64',
    executableName: 'ModelingLanguageServer.exe',
    selfContained: false,
  },
  {
    target: 'win32-arm64',
    runtimeId: 'win-arm64',
    executableName: 'ModelingLanguageServer.exe',
    selfContained: true,
  },
  {
    target: 'linux-x64',
    runtimeId: 'linux-x64',
    executableName: 'ModelingLanguageServer',
    selfContained: false,
  },
  {
    target: 'darwin-x64',
    runtimeId: 'osx-x64',
    executableName: 'ModelingLanguageServer',
    selfContained: false,
  },
  {
    target: 'darwin-arm64',
    runtimeId: 'osx-arm64',
    executableName: 'ModelingLanguageServer',
    selfContained: false,
  },
];

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
