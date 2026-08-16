import * as fs from 'fs';
import * as path from 'path';
import { buildAnalyzableSurface } from './catalog';
import type { AnalyzableSurface, SurfaceDiscoveryResult } from './types';

const ANALYTICS_KEYWORDS = [
  'dashboard',
  'kpi',
  'metric',
  'scorecard',
  'visual',
  'chart',
  'executive',
  'overview',
  'analytics',
  'analytic',
  'trend',
  'summary',
];

const NAVIGATION_KEYWORDS = [
  'route',
  'router',
  'navigation',
  'nav',
  'menu',
  'breadcrumb',
  'path:',
  'path =',
];

function safeReadDir(dirPath: string): fs.Dirent[] {
  try {
    return fs.readdirSync(dirPath, { withFileTypes: true });
  } catch {
    return [];
  }
}

function collectFiles(rootPath: string, maxDepth = 4): string[] {
  const files: string[] = [];

  function visit(currentPath: string, depth: number): void {
    if (depth > maxDepth) {
      return;
    }

    for (const entry of safeReadDir(currentPath)) {
      if (entry.name === 'node_modules' || entry.name.startsWith('.git')) {
        continue;
      }

      const fullPath = path.join(currentPath, entry.name);
      if (entry.isDirectory()) {
        visit(fullPath, depth + 1);
        continue;
      }

      files.push(fullPath);
    }
  }

  visit(rootPath, 0);
  return files;
}

function readFileText(filePath: string): string {
  try {
    return fs.readFileSync(filePath, 'utf8');
  } catch {
    return '';
  }
}

function buildFabricSurface(repoPath: string): AnalyzableSurface {
  return buildAnalyzableSurface('fabricApp', {
    displayName: path.basename(repoPath),
    sourceLocation: repoPath,
  });
}

export function detectFabricAppSurface(selectionPath: string): SurfaceDiscoveryResult {
  if (!fs.existsSync(selectionPath) || !fs.statSync(selectionPath).isDirectory()) {
    return {
      status: 'unsupported',
      reasonCode: 'unsupportedSurface',
      reason: 'Select a Fabric App repository folder to run Fabric App Review.',
    };
  }

  const files = collectFiles(selectionPath);
  const repoIndicator = files.some((filePath) => {
    const basename = path.basename(filePath).toLowerCase();
    return basename === 'package.json' || basename === 'tsconfig.json' || basename.startsWith('vite.config.');
  });

  if (!repoIndicator) {
    return {
      status: 'unsupported',
      reasonCode: 'missingFabricAppIndicators',
      reason: 'This folder does not expose the minimum Fabric App repo indicators such as package.json, tsconfig.json, or vite config.',
    };
  }

  const typeScriptFiles = files.filter((filePath) => /\.(ts|tsx)$/i.test(filePath));
  const navigationFiles = typeScriptFiles.filter((filePath) => {
    const normalizedPath = filePath.toLowerCase();
    const text = readFileText(filePath).toLowerCase();
    return NAVIGATION_KEYWORDS.some((keyword) => normalizedPath.includes(keyword) || text.includes(keyword));
  });

  if (navigationFiles.length === 0) {
    return {
      status: 'unsupported',
      reasonCode: 'missingNavigationArtifacts',
      reason: 'This repo does not expose route or navigation artifacts needed for Fabric App review.',
    };
  }

  const analyticsFiles = typeScriptFiles.filter((filePath) => {
    const normalizedPath = filePath.toLowerCase();
    if (normalizedPath.includes('/route') || normalizedPath.includes('\\route')) {
      return false;
    }

    const text = readFileText(filePath).toLowerCase();
    return ANALYTICS_KEYWORDS.some((keyword) => normalizedPath.includes(keyword) || text.includes(keyword));
  });

  if (analyticsFiles.length === 0) {
    const hasAnyNonNavigationTypeScript = typeScriptFiles.some((filePath) => !navigationFiles.includes(filePath));
    if (hasAnyNonNavigationTypeScript) {
      return {
        status: 'ambiguous',
        reasonCode: 'ambiguousAnalyticsSurface',
        reason: 'This repo has app and route structure, but it does not yet clearly look like an analytical Fabric App surface.',
      };
    }

    return {
      status: 'unsupported',
      reasonCode: 'missingAnalyticsTypescript',
      reason: 'This repo does not expose analytics-facing TypeScript layout or composition artifacts.',
    };
  }

  return {
    status: 'supported',
    surface: buildFabricSurface(selectionPath),
  };
}
