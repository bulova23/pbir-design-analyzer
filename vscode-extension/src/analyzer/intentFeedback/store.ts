import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import type { IntentFeedbackEntry } from '../contracts/scorePanel';

export interface IntentFeedbackSession {
  reportPath: string;
  reportKey: string;
  entries: IntentFeedbackEntry[];
  createdAt: string;
  updatedAt: string;
}

function reportKeyFromPath(reportPath: string): string {
  return crypto.createHash('md5').update(reportPath).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, reportKey: string): string {
  return path.join(context.globalStorageUri.fsPath, 'intent-feedback', reportKey);
}

export async function loadIntentFeedbackSession(
  context: vscode.ExtensionContext,
  reportPath: string,
): Promise<IntentFeedbackSession> {
  const reportKey = reportKeyFromPath(reportPath);
  const manifestPath = path.join(sessionDir(context, reportKey), 'feedback.json');

  if (fs.existsSync(manifestPath)) {
    const content = fs.readFileSync(manifestPath, 'utf8');
    return JSON.parse(content) as IntentFeedbackSession;
  }

  const now = new Date().toISOString();
  return {
    reportPath,
    reportKey,
    entries: [],
    createdAt: now,
    updatedAt: now,
  };
}

export async function saveIntentFeedbackSession(
  context: vscode.ExtensionContext,
  session: IntentFeedbackSession,
): Promise<void> {
  const dir = sessionDir(context, session.reportKey);
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }

  session.updatedAt = new Date().toISOString();
  fs.writeFileSync(path.join(dir, 'feedback.json'), JSON.stringify(session, null, 2), 'utf8');
}

export function upsertIntentFeedback(
  session: IntentFeedbackSession,
  entry: IntentFeedbackEntry,
): void {
  const index = session.entries.findIndex((existing) => (
    existing.pageName === entry.pageName
    && existing.inferredIntent === entry.inferredIntent
    && (existing.storyArchetype ?? '') === (entry.storyArchetype ?? '')
  ));

  if (index >= 0) {
    session.entries[index] = entry;
    return;
  }

  session.entries.push(entry);
}
