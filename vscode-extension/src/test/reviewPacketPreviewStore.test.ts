import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import {
  loadReviewPacketPreviewOptions,
  saveReviewPacketPreviewOptions,
} from '../analyzer/score/reviewPacketPreviewStore';

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
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-preview-options-test-'));
}

describe('review packet preview options store', () => {
  it('returns default preview options when no stored settings exist', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);

    const options = await loadReviewPacketPreviewOptions(ctx, '/my/report.Report');

    expect(options).toEqual({
      profile: 'consultant',
      templateVariant: 'brandedConsultant',
    });
  });

  it('persists options across reloads for the same report', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);

    await saveReviewPacketPreviewOptions(ctx, '/my/report.Report', {
      profile: 'governance',
      templateVariant: 'standard',
    });

    const reloaded = await loadReviewPacketPreviewOptions(ctx, '/my/report.Report');

    expect(reloaded).toEqual({
      profile: 'governance',
      templateVariant: 'standard',
    });
  });

  it('normalizes non-consultant profiles to the standard template on reload', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);

    await saveReviewPacketPreviewOptions(ctx, '/my/report.Report', {
      profile: 'executive',
      templateVariant: 'brandedConsultant',
    });

    const reloaded = await loadReviewPacketPreviewOptions(ctx, '/my/report.Report');

    expect(reloaded).toEqual({
      profile: 'executive',
      templateVariant: 'standard',
    });
  });

  it('keeps preview options isolated per report path', async () => {
    const tmp = makeTempDir();
    const ctx = makeContext(tmp);

    await saveReviewPacketPreviewOptions(ctx, '/my/report-a.Report', {
      profile: 'executive',
      templateVariant: 'standard',
    });

    const otherReport = await loadReviewPacketPreviewOptions(ctx, '/my/report-b.Report');

    expect(otherReport).toEqual({
      profile: 'consultant',
      templateVariant: 'brandedConsultant',
    });
  });
});
