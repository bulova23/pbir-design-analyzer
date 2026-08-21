import { createHash } from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type { BackendLaunchDiagnostics } from '../../languageServer/analyzerBackendClient';
import type { ScoreResult } from '../contracts/scorePanel';

export interface ReportFingerprintFileHash {
  relativePath: string;
  sha256: string;
  sizeBytes: number;
}

export interface ReportFingerprint {
  algorithm: 'sha256';
  fingerprint: string;
  rootPath: string;
  fileCount: number;
  sourceFiles: ReportFingerprintFileHash[];
}

export interface ScoreDeterminismDiagnostics {
  extensionVersion: string;
  backendVersion?: string;
  platform: NodeJS.Platform;
  architecture: string;
  resultSource: 'freshAnalysis';
  cachedPayload: false;
  analyzerType?: string;
  analyzerProfile?: string;
  score: number;
  pageCount: number;
  issueCount: number;
  severityCounts: {
    high: number;
    medium: number;
    low: number;
    info: number;
  };
  readinessScore?: number;
  readinessBand?: string;
  frameworkWeights: Record<string, number>;
  overallFrameworkScores: {
    gestaltScore: number;
    cognitiveLoadScore: number;
    dataInkScore: number;
    accessibilityScore: number;
    visualBestPracticesScore: number;
    stephenFewScore: number;
    enterpriseGovernanceScore: number;
    tufteScore: number;
    graphicalPerceptionScore: number;
    densityScore: number;
    narrativeScore: number;
    compositeScore: number;
  };
  pageProcessingOrder: string[];
  pageSnapshots: Array<{
    pageName: string;
    frameworkWeights: Record<string, number>;
    frameworkScores: {
      gestaltScore: number;
      cognitiveLoadScore: number;
      dataInkScore: number;
      accessibilityScore: number;
      visualBestPracticesScore: number;
      stephenFewScore: number;
      enterpriseGovernanceScore: number;
      tufteScore: number;
      graphicalPerceptionScore: number;
      densityScore: number;
      narrativeScore: number;
      compositeScore: number;
    };
    compositeScore: number;
    visualCount: number;
    navigationVisualCount: number;
    hiddenVisualCount: number;
    visibleTitleVisualCount: number;
    visualIds: string[];
  }>;
  findings: Array<{
    id: string;
    title: string;
    severity: string;
    evidenceCount: number;
  }>;
  evidenceCount: number;
  reportFingerprint: ReportFingerprint;
  backendBinaryPath?: string;
  backendRuntimeId?: string;
  backendTarget?: string;
}

function compareText(left: string, right: string): number {
  if (left < right) {
    return -1;
  }

  if (left > right) {
    return 1;
  }

  return 0;
}

function hashBuffer(buffer: Buffer): string {
  return createHash('sha256').update(buffer).digest('hex');
}

function buildFrameworkScores(source: {
  gestaltScore: number;
  cognitiveLoadScore: number;
  dataInkScore: number;
  accessibilityScore: number;
  visualBestPracticesScore: number;
  stephenFewScore: number;
  enterpriseGovernanceScore: number;
  tufteScore: number;
  graphicalPerceptionScore: number;
  densityScore: number;
  narrativeScore: number;
  compositeScore: number;
}): ScoreDeterminismDiagnostics['overallFrameworkScores'] {
  return {
    gestaltScore: source.gestaltScore,
    cognitiveLoadScore: source.cognitiveLoadScore,
    dataInkScore: source.dataInkScore,
    accessibilityScore: source.accessibilityScore,
    visualBestPracticesScore: source.visualBestPracticesScore,
    stephenFewScore: source.stephenFewScore,
    enterpriseGovernanceScore: source.enterpriseGovernanceScore,
    tufteScore: source.tufteScore,
    graphicalPerceptionScore: source.graphicalPerceptionScore,
    densityScore: source.densityScore,
    narrativeScore: source.narrativeScore,
    compositeScore: source.compositeScore,
  };
}

function buildFrameworkWeights(source: { frameworkWeights?: Record<string, number> | null }): Record<string, number> {
  const entries = Object.entries(source.frameworkWeights ?? {})
    .sort(([left], [right]) => compareText(left, right));

  return Object.fromEntries(entries);
}

export function normalizeFingerprintPath(value: string): string {
  return value.replace(/\\/g, '/');
}

function isExcludedDirectoryName(name: string): boolean {
  const normalized = name.toLowerCase();
  return normalized === '.git'
    || normalized === '.pbi'
    || normalized === '.vs'
    || normalized === 'node_modules'
    || normalized === 'bin'
    || normalized === 'obj'
    || normalized === 'dist'
    || normalized === 'out'
    || normalized === 'tmp'
    || normalized === 'temp'
    || normalized === 'cache'
    || normalized.endsWith('.cache');
}

function isExcludedFileName(name: string): boolean {
  const normalized = name.toLowerCase();
  return normalized === '.ds_store'
    || normalized === 'thumbs.db'
    || normalized === '.platform';
}

function collectFiles(rootPath: string, currentPath: string, output: string[]): void {
  const entries = fs.readdirSync(currentPath, { withFileTypes: true })
    .sort((left, right) => compareText(left.name, right.name));

  for (const entry of entries) {
    if (entry.name === '.' || entry.name === '..') {
      continue;
    }

    const absolutePath = path.join(currentPath, entry.name);
    const relativePath = normalizeFingerprintPath(path.relative(rootPath, absolutePath));

    if (entry.isDirectory()) {
      if (!isExcludedDirectoryName(entry.name)) {
        collectFiles(rootPath, absolutePath, output);
      }
      continue;
    }

    if (entry.isFile() && !isExcludedFileName(entry.name)) {
      output.push(relativePath);
    }
  }
}

function findReportRoot(selectionPath: string): string {
  const absolutePath = path.resolve(selectionPath);

  if (fs.existsSync(absolutePath) && fs.statSync(absolutePath).isFile()) {
    if (absolutePath.toLowerCase().endsWith('.pbip')) {
      return path.dirname(absolutePath);
    }

    return findReportRoot(path.dirname(absolutePath));
  }

  let currentPath = absolutePath;
  while (currentPath !== '') {
    if (
      fs.existsSync(path.join(currentPath, 'definition.pbir'))
      || fs.existsSync(path.join(currentPath, 'definition', 'report.json'))
    ) {
      return currentPath;
    }

    const reportFolders = fs.readdirSync(currentPath, { withFileTypes: true })
      .filter((entry) => entry.isDirectory() && entry.name.toLowerCase().endsWith('.report'))
      .map((entry) => path.join(currentPath, entry.name))
      .sort(compareText);
    for (const reportFolder of reportFolders) {
      if (
        fs.existsSync(path.join(reportFolder, 'definition.pbir'))
        || fs.existsSync(path.join(reportFolder, 'definition', 'report.json'))
      ) {
        return reportFolder;
      }
    }

    const parentPath = path.dirname(currentPath);
    if (parentPath === currentPath) {
      return absolutePath;
    }
    currentPath = parentPath;
  }

  return absolutePath;
}

export function buildReportFingerprint(selectionPath: string): ReportFingerprint {
  const rootPath = findReportRoot(selectionPath);
  const relativePaths: string[] = [];
  collectFiles(rootPath, rootPath, relativePaths);

  const sourceFiles = relativePaths
    .sort(compareText)
    .map((relativePath) => {
      const absolutePath = path.join(rootPath, relativePath);
      const content = fs.readFileSync(absolutePath);
      return {
        relativePath,
        sha256: hashBuffer(content),
        sizeBytes: content.byteLength,
      };
    });

  const fingerprintPayload = sourceFiles
    .map((entry) => `${entry.relativePath}\0${entry.sizeBytes}\0${entry.sha256}`)
    .join('\n');

  return {
    algorithm: 'sha256',
    fingerprint: hashBuffer(Buffer.from(fingerprintPayload, 'utf8')),
    rootPath,
    fileCount: sourceFiles.length,
    sourceFiles,
  };
}

export function buildScoreDeterminismDiagnostics(input: {
  result: ScoreResult;
  reportPath: string;
  extensionVersion: string;
  backendVersion?: string;
  backendLaunchDiagnostics?: BackendLaunchDiagnostics;
}): ScoreDeterminismDiagnostics {
  const findings = [...(input.result.normalizedFindings ?? [])].sort((left, right) => compareText(left.id, right.id));
  const severityCounts = findings.reduce(
    (counts, finding) => {
      counts[finding.severity] += 1;
      return counts;
    },
    { high: 0, medium: 0, low: 0, info: 0 },
  );

  return {
    extensionVersion: input.extensionVersion,
    backendVersion: input.backendVersion,
    platform: process.platform,
    architecture: process.arch,
    resultSource: 'freshAnalysis',
    cachedPayload: false,
    analyzerType: input.result.analysisContext?.analyzerType,
    analyzerProfile: input.result.analysisContext?.analyzerProfile,
    score: input.result.compositeScore,
    pageCount: input.result.pageCount,
    issueCount: findings.length,
    severityCounts,
    readinessScore: input.result.readinessAssessment?.overallReadinessScore,
    readinessBand: input.result.readinessAssessment?.readinessBand,
    frameworkWeights: buildFrameworkWeights(input.result),
    overallFrameworkScores: buildFrameworkScores(input.result),
    pageProcessingOrder: (input.result.pageScores ?? []).map((page) => page.pageName),
    pageSnapshots: (input.result.pageScores ?? []).map((page) => ({
      pageName: page.pageName,
      frameworkWeights: buildFrameworkWeights(page),
      frameworkScores: buildFrameworkScores(page),
      compositeScore: page.compositeScore,
      visualCount: page.visualMetadata?.visualCount ?? 0,
      navigationVisualCount: page.navigationVisualCount ?? page.visualMetadata?.visuals.filter((visual) => visual.isNavigationElement).length ?? 0,
      hiddenVisualCount: page.hiddenVisualCount ?? page.visualMetadata?.visuals.filter((visual) => visual.isHidden).length ?? 0,
      visibleTitleVisualCount: page.visualMetadata?.visibleTitleVisualCount ?? 0,
      visualIds: (page.visualMetadata?.visuals ?? []).map((visual) => visual.visualId),
    })),
    findings: findings.map((finding) => ({
      id: finding.id,
      title: finding.title,
      severity: finding.severity,
      evidenceCount: finding.evidence.length,
    })),
    evidenceCount: findings.reduce((sum, finding) => sum + finding.evidence.length, 0),
    reportFingerprint: buildReportFingerprint(input.reportPath),
    backendBinaryPath: input.backendLaunchDiagnostics?.resolvedBackendPath,
    backendRuntimeId: input.backendLaunchDiagnostics?.runtimeId,
    backendTarget: input.backendLaunchDiagnostics?.selectedTarget,
  };
}
