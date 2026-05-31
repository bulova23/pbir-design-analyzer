import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import type { ReviewPacketPreviewOptions } from './reviewPacketPreview';
import {
  defaultReviewPacketPreviewOptions,
  normalizeReviewPacketPreviewOptions,
} from './reviewPacketPreview';

interface ReviewPacketPreviewOptionsSession {
  reportPath: string;
  reportKey: string;
  options: ReviewPacketPreviewOptions;
  createdAt: string;
  updatedAt: string;
}

function reportKeyFromPath(reportPath: string): string {
  return crypto.createHash('md5').update(reportPath).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, reportKey: string): string {
  return path.join(context.globalStorageUri.fsPath, 'review-packet-preview', reportKey);
}

export async function loadReviewPacketPreviewOptions(
  context: vscode.ExtensionContext,
  reportPath: string,
): Promise<ReviewPacketPreviewOptions> {
  const reportKey = reportKeyFromPath(reportPath);
  const manifestPath = path.join(sessionDir(context, reportKey), 'preview-options.json');

  if (!fs.existsSync(manifestPath)) {
    return defaultReviewPacketPreviewOptions;
  }

  const content = fs.readFileSync(manifestPath, 'utf8');
  const session = JSON.parse(content) as ReviewPacketPreviewOptionsSession;
  return normalizeReviewPacketPreviewOptions(session.options);
}

export async function saveReviewPacketPreviewOptions(
  context: vscode.ExtensionContext,
  reportPath: string,
  options: Partial<ReviewPacketPreviewOptions>,
): Promise<void> {
  const reportKey = reportKeyFromPath(reportPath);
  const dir = sessionDir(context, reportKey);
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }

  const manifestPath = path.join(dir, 'preview-options.json');
  const now = new Date().toISOString();
  const existing = fs.existsSync(manifestPath)
    ? JSON.parse(fs.readFileSync(manifestPath, 'utf8')) as ReviewPacketPreviewOptionsSession
    : undefined;

  const session: ReviewPacketPreviewOptionsSession = {
    reportPath,
    reportKey,
    options: normalizeReviewPacketPreviewOptions(options),
    createdAt: existing?.createdAt ?? now,
    updatedAt: now,
  };

  fs.writeFileSync(manifestPath, JSON.stringify(session, null, 2), 'utf8');
}
