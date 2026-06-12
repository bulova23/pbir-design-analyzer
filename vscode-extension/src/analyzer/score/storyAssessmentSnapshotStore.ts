import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import type { StoryAssessmentReportSnapshot } from '../contracts/scorePanel';

interface StoryAssessmentSnapshotSession {
  reportPath: string;
  reportKey: string;
  snapshot: StoryAssessmentReportSnapshot;
  createdAt: string;
  updatedAt: string;
}

function reportKeyFromPath(reportPath: string): string {
  return crypto.createHash('md5').update(reportPath).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, reportKey: string): string {
  return path.join(context.globalStorageUri.fsPath, 'story-assessment-snapshots', reportKey);
}

export async function loadStoryAssessmentSnapshot(
  context: vscode.ExtensionContext,
  reportPath: string,
): Promise<StoryAssessmentReportSnapshot | undefined> {
  const reportKey = reportKeyFromPath(reportPath);
  const manifestPath = path.join(sessionDir(context, reportKey), 'snapshot.json');

  if (!fs.existsSync(manifestPath)) {
    return undefined;
  }

  try {
    const content = fs.readFileSync(manifestPath, 'utf8');
    const session = JSON.parse(content) as StoryAssessmentSnapshotSession;
    return session.snapshot;
  } catch {
    return undefined;
  }
}

export async function saveStoryAssessmentSnapshot(
  context: vscode.ExtensionContext,
  reportPath: string,
  snapshot: StoryAssessmentReportSnapshot,
): Promise<void> {
  const reportKey = reportKeyFromPath(reportPath);
  const dir = sessionDir(context, reportKey);
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }

  const manifestPath = path.join(dir, 'snapshot.json');
  const now = new Date().toISOString();
  const existing = fs.existsSync(manifestPath)
    ? await loadStoryAssessmentSnapshotSession(manifestPath)
    : undefined;

  const session: StoryAssessmentSnapshotSession = {
    reportPath,
    reportKey,
    snapshot,
    createdAt: existing?.createdAt ?? now,
    updatedAt: now,
  };

  fs.writeFileSync(manifestPath, JSON.stringify(session, null, 2), 'utf8');
}

async function loadStoryAssessmentSnapshotSession(manifestPath: string): Promise<StoryAssessmentSnapshotSession | undefined> {
  try {
    return JSON.parse(fs.readFileSync(manifestPath, 'utf8')) as StoryAssessmentSnapshotSession;
  } catch {
    return undefined;
  }
}
