import * as fs from 'fs';
import * as path from 'path';

const expectedTargets = [
  'darwin-arm64',
  'darwin-x64',
  'linux-x64',
  'win32-arm64',
  'win32-x64',
] as const;

describe('checked-in packaged backend targets', () => {
  const targetsRoot = path.resolve(__dirname, '../../backend/targets');

  it('tracks the expected packaged backend target directories', () => {
    const actualTargets = fs.readdirSync(targetsRoot, { withFileTypes: true })
      .filter((entry) => entry.isDirectory())
      .map((entry) => entry.name)
      .sort();

    expect(actualTargets).toEqual(expectedTargets);
  });

  it('includes the runtime-critical backend assets for each target', () => {
    for (const target of expectedTargets) {
      const rpcDir = path.join(targetsRoot, target, 'rpc');
      const executableName = target.startsWith('win32')
        ? 'ModelingLanguageServer.exe'
        : 'ModelingLanguageServer';

      expect(fs.existsSync(path.join(rpcDir, executableName))).toBe(true);
      expect(fs.existsSync(path.join(rpcDir, 'ModelingLanguageServer.dll'))).toBe(true);
      expect(fs.existsSync(path.join(rpcDir, 'ModelingLanguageServer.deps.json'))).toBe(true);
      expect(fs.existsSync(path.join(rpcDir, 'ModelingLanguageServer.runtimeconfig.json'))).toBe(true);
    }
  });
});
