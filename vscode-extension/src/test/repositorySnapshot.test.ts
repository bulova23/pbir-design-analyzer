import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { extractNavigationEvidence } from '../analyzer/fabric/review/navigationEvidence';
import { extractTypeScriptEvidence } from '../analyzer/fabric/review/typescriptEvidence';
import {
  createRepositorySnapshot,
  type RepositorySnapshotFile,
  type RepositorySnapshotFs,
} from '../analyzer/project/repoSnapshot';

describe('createRepositorySnapshot', () => {
  let tempDir: string;

  beforeEach(() => {
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'repo-snapshot-'));
    fs.mkdirSync(path.join(tempDir, 'src', 'routes'), { recursive: true });
    fs.mkdirSync(path.join(tempDir, 'screenshots'), { recursive: true });
    fs.writeFileSync(path.join(tempDir, 'src', 'app.tsx'), 'export const App = () => <DashboardLayout />;');
    fs.writeFileSync(path.join(tempDir, 'src', 'routes', 'index.tsx'), 'export const routes = [{ path: "/", label: "Overview" }];');
    fs.writeFileSync(path.join(tempDir, 'screenshots', 'Executive Overview.png'), 'fake-image');
  });

  afterEach(() => {
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  it('walks the repository once and caches repeated text reads', async () => {
    const callCounts = {
      readdir: 0,
      readFile: 0,
      stat: 0,
    };

    const instrumentedFs: RepositorySnapshotFs = {
      async readdir(dirPath: string, options: { withFileTypes: true }) {
        callCounts.readdir += 1;
        return fs.promises.readdir(dirPath, options);
      },
      async readFile(filePath: string, encoding: BufferEncoding) {
        callCounts.readFile += 1;
        return fs.promises.readFile(filePath, encoding);
      },
      async stat(targetPath: string) {
        callCounts.stat += 1;
        return fs.promises.stat(targetPath);
      },
    };

    const snapshot = await createRepositorySnapshot(tempDir, { fileSystem: instrumentedFs });
    const initialReaddirCalls = callCounts.readdir;

    expect(snapshot.listFiles().map((file: RepositorySnapshotFile) => file.relativePath)).toEqual([
      'screenshots/Executive Overview.png',
      'src/app.tsx',
      'src/routes/index.tsx',
    ]);

    expect(snapshot.listFiles().map((file: RepositorySnapshotFile) => file.relativePath)).toEqual([
      'screenshots/Executive Overview.png',
      'src/app.tsx',
      'src/routes/index.tsx',
    ]);
    expect(callCounts.readdir).toBe(initialReaddirCalls);

    const appFile = snapshot.listFiles().find((file: RepositorySnapshotFile) => file.relativePath === 'src/app.tsx');
    expect(appFile).toBeDefined();

    const firstRead = await snapshot.readText(appFile!);
    const secondRead = await snapshot.readText(appFile!);

    expect(firstRead).toContain('DashboardLayout');
    expect(secondRead).toBe(firstRead);
    expect(callCounts.readFile).toBe(1);
  });

  it('rejects further access after the snapshot is disposed', async () => {
    const snapshot = await createRepositorySnapshot(tempDir);
    const appFile = snapshot.resolveFile('src/app.tsx');

    snapshot.dispose();

    expect(() => snapshot.listFiles()).toThrow('Repository snapshot has been disposed');
    await expect(snapshot.readText(appFile)).rejects.toThrow(
      'Repository snapshot has been disposed',
    );
  });

  it('supports multiple evidence extractors without a second repository walk', async () => {
    const callCounts = {
      readdir: 0,
      readFile: 0,
      stat: 0,
    };

    const instrumentedFs: RepositorySnapshotFs = {
      async readdir(dirPath: string, options: { withFileTypes: true }) {
        callCounts.readdir += 1;
        return fs.promises.readdir(dirPath, options);
      },
      async readFile(filePath: string, encoding: BufferEncoding) {
        callCounts.readFile += 1;
        return fs.promises.readFile(filePath, encoding);
      },
      async stat(targetPath: string) {
        callCounts.stat += 1;
        return fs.promises.stat(targetPath);
      },
    };

    const snapshot = await createRepositorySnapshot(tempDir, { fileSystem: instrumentedFs });
    const initialReaddirCalls = callCounts.readdir;

    const typeScriptEvidence = await extractTypeScriptEvidence(snapshot);
    const navigationEvidence = await extractNavigationEvidence(snapshot);

    expect(typeScriptEvidence.layoutPatterns).toEqual([
      expect.objectContaining({ filePath: 'src/app.tsx' }),
    ]);
    expect(navigationEvidence.routes).toEqual([
      expect.objectContaining({ path: '/', label: 'Overview', filePath: 'src/routes/index.tsx' }),
    ]);
    expect(callCounts.readdir).toBe(initialReaddirCalls);
  });
});
