import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as crypto from 'crypto';
import {
  loadStoryAssessmentSnapshot,
  saveStoryAssessmentSnapshot,
} from '../analyzer/score/storyAssessmentSnapshotStore';

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
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-story-snapshot-test-'));
}

describe('storyAssessmentSnapshotStore', () => {
  it('returns undefined when no snapshot exists for a report yet', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);

    await expect(loadStoryAssessmentSnapshot(ctx, '/my/report.Report')).resolves.toBeUndefined();
  });

  it('persists and reloads the latest snapshot by report path hash', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);

    await saveStoryAssessmentSnapshot(ctx, '/my/report.Report', {
      reportPath: '/my/report.Report',
      scoredAt: '2026-06-12T00:00:00.000Z',
      pages: [
        {
          pageName: 'Overview',
          storyMaturity: 'Developing',
          strongSignals: ['Clear KPI band'],
          missingSignals: ['No visible benchmark or target'],
          topImprovementIds: ['missing-benchmark-target'],
          recommendations: [],
        },
      ],
    });

    await expect(loadStoryAssessmentSnapshot(ctx, '/my/report.Report')).resolves.toEqual({
      reportPath: '/my/report.Report',
      scoredAt: '2026-06-12T00:00:00.000Z',
      pages: [
        {
          pageName: 'Overview',
          storyMaturity: 'Developing',
          strongSignals: ['Clear KPI band'],
          missingSignals: ['No visible benchmark or target'],
          topImprovementIds: ['missing-benchmark-target'],
          recommendations: [],
        },
      ],
    });
  });

  it('recovers gracefully from malformed persisted JSON', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);
    const reportPath = '/my/report.Report';
    const reportKey = crypto.createHash('md5').update(reportPath).digest('hex').slice(0, 16);
    const dir = path.join(tmp, 'story-assessment-snapshots', reportKey);
    fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(path.join(dir, 'snapshot.json'), '{ not valid json', 'utf8');

    await expect(loadStoryAssessmentSnapshot(ctx, reportPath)).resolves.toBeUndefined();
  });

  it('stores snapshots in extension global storage rather than the PBIR repo', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);

    await saveStoryAssessmentSnapshot(ctx, '/Users/me/Workspace/Sales.Report', {
      reportPath: '/Users/me/Workspace/Sales.Report',
      scoredAt: '2026-06-12T00:00:00.000Z',
      pages: [],
    });

    expect(fs.existsSync(path.join('/Users/me/Workspace', 'story-assessment-snapshot.json'))).toBe(false);
    expect(fs.readdirSync(tmp)).toContain('story-assessment-snapshots');
  });
});
