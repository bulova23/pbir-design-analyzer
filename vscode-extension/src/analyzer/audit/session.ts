import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import { matchFilenameToPages } from './filenameMatching';
import type { VisualAuditPageCoverage, VisualAuditSession, VisualCapture } from './types';

function reportKeyFromPath(reportPath: string): string {
  return crypto.createHash('md5').update(reportPath).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, reportKey: string): string {
  return path.join(context.globalStorageUri.fsPath, 'audit-sessions', reportKey);
}

export async function loadSession(
  context: vscode.ExtensionContext,
  reportPath: string,
): Promise<VisualAuditSession> {
  const reportKey = reportKeyFromPath(reportPath);
  const manifestPath = path.join(sessionDir(context, reportKey), 'session.json');

  if (fs.existsSync(manifestPath)) {
    const content = fs.readFileSync(manifestPath, 'utf8');
    return JSON.parse(content) as VisualAuditSession;
  }

  return {
    reportPath,
    reportKey,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    pages: [],
    unmatchedCaptures: [],
  };
}

export async function saveSession(
  context: vscode.ExtensionContext,
  session: VisualAuditSession,
): Promise<void> {
  const dir = sessionDir(context, session.reportKey);
  const assetsDir = path.join(dir, 'assets');

  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
  if (!fs.existsSync(assetsDir)) {
    fs.mkdirSync(assetsDir, { recursive: true });
  }

  session.updatedAt = new Date().toISOString();
  fs.writeFileSync(path.join(dir, 'session.json'), JSON.stringify(session, null, 2), 'utf8');
}

export async function addCaptures(
  context: vscode.ExtensionContext,
  session: VisualAuditSession,
  sourcePaths: string[],
  pageNames: string[],
): Promise<void> {
  const dir = sessionDir(context, session.reportKey);
  const assetsDir = path.join(dir, 'assets');

  if (!fs.existsSync(assetsDir)) {
    fs.mkdirSync(assetsDir, { recursive: true });
  }

  for (const sourcePath of sourcePaths) {
    const fileName = path.basename(sourcePath);
    const captureId = crypto.randomUUID();
    const ext = path.extname(fileName);
    const storedPath = path.join(assetsDir, `${captureId}${ext}`);

    fs.copyFileSync(sourcePath, storedPath);

    const match = matchFilenameToPages(fileName, pageNames);
    const capture: VisualCapture = {
      captureId,
      pageName: match?.pageName ?? '',
      fileName,
      storedPath,
      source: 'upload',
      capturedAt: new Date().toISOString(),
      originalPath: sourcePath,
    };

    if (match) {
      upsertPageCapture(session, match.pageName, capture);
    } else {
      session.unmatchedCaptures.push(capture);
    }
  }
}

export function removeCapture(session: VisualAuditSession, captureId: string): void {
  for (const page of session.pages) {
    page.captures = page.captures.filter((c) => c.captureId !== captureId);
  }
  session.pages = session.pages.filter((p) => p.captures.length > 0 || p.findings.length > 0);
  session.unmatchedCaptures = session.unmatchedCaptures.filter((c) => c.captureId !== captureId);
}

export function assignCapture(
  session: VisualAuditSession,
  captureId: string,
  targetPageName: string,
): void {
  let capture = extractCapture(session, captureId);
  if (!capture) {
    return;
  }

  capture.pageName = targetPageName;
  upsertPageCapture(session, targetPageName, capture);
}

export function computeCoverage(session: VisualAuditSession, allPageNames: string[]) {
  const pagesWithCaptures = new Set(session.pages.filter((p) => p.captures.length > 0).map((p) => p.pageName));
  return {
    totalPages: allPageNames.length,
    pagesWithCaptures: pagesWithCaptures.size,
    unmatchedCaptures: session.unmatchedCaptures.length,
    pagesWithFindings: session.pages.filter((p) => p.findings.length > 0).length,
  };
}

function upsertPageCapture(
  session: VisualAuditSession,
  pageName: string,
  capture: VisualCapture,
): void {
  const existing = session.pages.find((p) => p.pageName === pageName);
  if (existing) {
    existing.captures.push(capture);
  } else {
    const page: VisualAuditPageCoverage = { pageName, captures: [capture], findings: [] };
    session.pages.push(page);
  }
}

function extractCapture(session: VisualAuditSession, captureId: string): VisualCapture | undefined {
  const unmatchedIdx = session.unmatchedCaptures.findIndex((c) => c.captureId === captureId);
  if (unmatchedIdx >= 0) {
    return session.unmatchedCaptures.splice(unmatchedIdx, 1)[0];
  }

  for (const page of session.pages) {
    const idx = page.captures.findIndex((c) => c.captureId === captureId);
    if (idx >= 0) {
      const [capture] = page.captures.splice(idx, 1);
      if (page.captures.length === 0 && page.findings.length === 0) {
        session.pages = session.pages.filter((p) => p !== page);
      }
      return capture;
    }
  }

  return undefined;
}
