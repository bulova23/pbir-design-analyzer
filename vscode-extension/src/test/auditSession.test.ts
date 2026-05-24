import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import {
  addCaptures,
  assignCapture,
  computeCoverage,
  loadSession,
  removeCapture,
  saveSession,
} from '../analyzer/audit/session';
import type { VisualAuditSession } from '../analyzer/audit/types';

function makeContext(tmpDir: string) {
  return {
    globalStorageUri: { fsPath: tmpDir },
    secrets: {
      get: jest.fn(),
      store: jest.fn(),
      delete: jest.fn(),
    },
  } as unknown as import('vscode').ExtensionContext;
}

function makeTempDir(): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-audit-test-'));
}

function makeImageFile(dir: string, name: string): string {
  const filePath = path.join(dir, name);
  fs.writeFileSync(filePath, 'fake-image-data');
  return filePath;
}

describe('loadSession', () => {
  it('returns a new empty session when no manifest exists', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);

    const session = await loadSession(ctx, '/my/report.Report');
    expect(session.reportPath).toBe('/my/report.Report');
    expect(session.pages).toHaveLength(0);
    expect(session.unmatchedCaptures).toHaveLength(0);
    expect(session.reportKey).toBeTruthy();
  });

  it('loads a persisted session on second call', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);
    const reportPath = '/my/report.Report';

    const session = await loadSession(ctx, reportPath);
    session.pages.push({ pageName: 'Overview', captures: [], findings: [] });
    await saveSession(ctx, session);

    const reloaded = await loadSession(ctx, reportPath);
    expect(reloaded.pages).toHaveLength(1);
    expect(reloaded.pages[0].pageName).toBe('Overview');
  });
});

describe('addCaptures', () => {
  it('auto-matches screenshot to page by filename', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);
    const srcDir = makeTempDir();
    const session = await loadSession(ctx, '/report.Report');

    const imgPath = makeImageFile(srcDir, 'Overview.png');
    await addCaptures(ctx, session, [imgPath], ['Overview', 'Sales Detail']);

    expect(session.pages).toHaveLength(1);
    expect(session.pages[0].pageName).toBe('Overview');
    expect(session.pages[0].captures).toHaveLength(1);
    expect(session.pages[0].captures[0].fileName).toBe('Overview.png');
    expect(session.unmatchedCaptures).toHaveLength(0);
  });

  it('places unrecognized screenshot in unmatchedCaptures', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);
    const srcDir = makeTempDir();
    const session = await loadSession(ctx, '/report.Report');

    const imgPath = makeImageFile(srcDir, 'random-screenshot.png');
    await addCaptures(ctx, session, [imgPath], ['Overview', 'Sales Detail']);

    expect(session.pages).toHaveLength(0);
    expect(session.unmatchedCaptures).toHaveLength(1);
    expect(session.unmatchedCaptures[0].fileName).toBe('random-screenshot.png');
  });

  it('copies the file to the session assets folder', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);
    const srcDir = makeTempDir();
    const session = await loadSession(ctx, '/report.Report');

    const imgPath = makeImageFile(srcDir, 'Overview.png');
    await addCaptures(ctx, session, [imgPath], ['Overview']);

    const storedPath = session.pages[0].captures[0].storedPath;
    expect(fs.existsSync(storedPath)).toBe(true);
  });

  it('supports multiple captures on the same page', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);
    const srcDir = makeTempDir();
    const session = await loadSession(ctx, '/report.Report');

    const img1 = makeImageFile(srcDir, 'Overview.png');
    const img2 = makeImageFile(srcDir, '01 Overview - Bookmark1.png');
    await addCaptures(ctx, session, [img1, img2], ['Overview']);

    expect(session.pages).toHaveLength(1);
    expect(session.pages[0].captures).toHaveLength(2);
  });
});

describe('removeCapture', () => {
  it('removes a capture from a matched page', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);
    const srcDir = makeTempDir();
    const session = await loadSession(ctx, '/report.Report');

    const img = makeImageFile(srcDir, 'Overview.png');
    await addCaptures(ctx, session, [img], ['Overview']);

    const captureId = session.pages[0].captures[0].captureId;
    removeCapture(session, captureId);

    expect(session.pages).toHaveLength(0);
  });

  it('removes a capture from unmatchedCaptures', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);
    const srcDir = makeTempDir();
    const session = await loadSession(ctx, '/report.Report');

    const img = makeImageFile(srcDir, 'random.png');
    await addCaptures(ctx, session, [img], ['Overview']);

    const captureId = session.unmatchedCaptures[0].captureId;
    removeCapture(session, captureId);

    expect(session.unmatchedCaptures).toHaveLength(0);
  });
});

describe('assignCapture', () => {
  it('moves a capture from unmatchedCaptures to a page', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);
    const srcDir = makeTempDir();
    const session = await loadSession(ctx, '/report.Report');

    const img = makeImageFile(srcDir, 'random.png');
    await addCaptures(ctx, session, [img], ['Overview']);
    expect(session.unmatchedCaptures).toHaveLength(1);

    const captureId = session.unmatchedCaptures[0].captureId;
    assignCapture(session, captureId, 'Sales Detail');

    expect(session.unmatchedCaptures).toHaveLength(0);
    expect(session.pages).toHaveLength(1);
    expect(session.pages[0].pageName).toBe('Sales Detail');
    expect(session.pages[0].captures[0].captureId).toBe(captureId);
  });

  it('moves a capture between pages', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);
    const srcDir = makeTempDir();
    const session = await loadSession(ctx, '/report.Report');

    const img = makeImageFile(srcDir, 'Overview.png');
    await addCaptures(ctx, session, [img], ['Overview']);

    const captureId = session.pages[0].captures[0].captureId;
    assignCapture(session, captureId, 'Sales Detail');

    expect(session.pages.find((p) => p.pageName === 'Overview')).toBeUndefined();
    const target = session.pages.find((p) => p.pageName === 'Sales Detail');
    expect(target).toBeDefined();
    expect(target!.captures[0].captureId).toBe(captureId);
  });
});

describe('computeCoverage', () => {
  it('counts pages with and without captures correctly', async () => {
    const session: VisualAuditSession = {
      reportPath: '/report.Report',
      reportKey: 'abc',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      pages: [
        { pageName: 'Overview', captures: [{ captureId: 'c1', pageName: 'Overview', fileName: 'x.png', storedPath: '', source: 'upload', capturedAt: '' }], findings: [] },
        { pageName: 'Sales', captures: [], findings: [{ findingId: 'f1', pageName: 'Sales', captureId: 'c1', findingType: 'objective', severity: 'warning', confidence: 'high', text: 'test' }] },
      ],
      unmatchedCaptures: [
        { captureId: 'c2', pageName: '', fileName: 'y.png', storedPath: '', source: 'upload', capturedAt: '' },
      ],
    };

    const coverage = computeCoverage(session, ['Overview', 'Sales', 'Net Sales']);
    expect(coverage.totalPages).toBe(3);
    expect(coverage.pagesWithCaptures).toBe(1);
    expect(coverage.unmatchedCaptures).toBe(1);
    expect(coverage.pagesWithFindings).toBe(1);
  });

  it('returns zero counts for empty session', () => {
    const session: VisualAuditSession = {
      reportPath: '/report.Report',
      reportKey: 'abc',
      createdAt: '',
      updatedAt: '',
      pages: [],
      unmatchedCaptures: [],
    };

    const coverage = computeCoverage(session, ['Overview', 'Sales']);
    expect(coverage.totalPages).toBe(2);
    expect(coverage.pagesWithCaptures).toBe(0);
    expect(coverage.unmatchedCaptures).toBe(0);
    expect(coverage.pagesWithFindings).toBe(0);
  });
});
