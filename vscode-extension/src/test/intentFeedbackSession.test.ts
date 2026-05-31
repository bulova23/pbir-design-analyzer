import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import {
  loadIntentFeedbackSession,
  saveIntentFeedbackSession,
  upsertIntentFeedback,
} from '../analyzer/intentFeedback/store';

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
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-intent-feedback-test-'));
}

describe('intent feedback session', () => {
  it('returns an empty session when no manifest exists', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);

    const session = await loadIntentFeedbackSession(ctx, '/my/report.Report');

    expect(session.reportPath).toBe('/my/report.Report');
    expect(session.entries).toHaveLength(0);
    expect(session.reportKey).toBeTruthy();
  });

  it('persists entries across reloads', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);
    const session = await loadIntentFeedbackSession(ctx, '/my/report.Report');

    upsertIntentFeedback(session, {
      pageName: 'Overview',
      inferredIntent: 'executiveOverview',
      storyArchetype: 'trend',
      userConfirmation: 'yes',
      note: 'Lead KPI band is right, but the supporting chart still needs a target line.',
      timestamp: '2026-05-27T16:02:08.000Z',
      analyzerVersion: '1.2.3',
      reportSessionId: 'abc123:2026-05-27T16:00:00.000Z',
      inferenceConfidence: 'high',
    });

    await saveIntentFeedbackSession(ctx, session);
    const reloaded = await loadIntentFeedbackSession(ctx, '/my/report.Report');

    expect(reloaded.entries).toHaveLength(1);
    expect(reloaded.entries[0].pageName).toBe('Overview');
    expect(reloaded.entries[0].userConfirmation).toBe('yes');
    expect(reloaded.entries[0].note).toBe('Lead KPI band is right, but the supporting chart still needs a target line.');
    expect(reloaded.entries[0].analyzerVersion).toBe('1.2.3');
  });

  it('replaces an existing entry for the same page and inferred story', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);
    const session = await loadIntentFeedbackSession(ctx, '/my/report.Report');

    upsertIntentFeedback(session, {
      pageName: 'Overview',
      inferredIntent: 'executiveOverview',
      storyArchetype: 'trend',
      userConfirmation: 'partial',
      timestamp: '2026-05-27T16:02:08.000Z',
      analyzerVersion: '1.2.3',
      reportSessionId: 'abc123:2026-05-27T16:00:00.000Z',
      inferenceConfidence: 'high',
    });

    upsertIntentFeedback(session, {
      pageName: 'Overview',
      inferredIntent: 'executiveOverview',
      storyArchetype: 'trend',
      userConfirmation: 'no',
      note: 'The page still reads like a comparison page, not an executive overview.',
      timestamp: '2026-05-27T16:03:08.000Z',
      analyzerVersion: '1.2.3',
      reportSessionId: 'abc123:2026-05-27T16:00:00.000Z',
      inferenceConfidence: 'medium',
    });

    expect(session.entries).toHaveLength(1);
    expect(session.entries[0].userConfirmation).toBe('no');
    expect(session.entries[0].note).toBe('The page still reads like a comparison page, not an executive overview.');
    expect(session.entries[0].inferenceConfidence).toBe('medium');
  });
});
