import * as fs from 'fs';
import * as path from 'path';
import { resolvePbirWorkspaceRoot } from './pathing';

export interface PbirThemeNode {
  name?: string;
  sourcePath?: string;
}

export interface PbirVisualNode {
  name?: string;
  visualType?: string;
  path?: string;
}

export interface PbirPageNode {
  name?: string;
  displayName?: string;
  path?: string;
  visuals?: PbirVisualNode[];
}

export interface PbirReportNode {
  name?: string;
  path?: string;
  theme?: PbirThemeNode;
  pages?: PbirPageNode[];
}

interface PbirReportLocation {
  projectRootPath: string;
  reportRootPath: string;
  definitionPath: string;
  reportJsonPath: string;
  workspaceRootPath: string;
  reportName: string;
}

type JsonRecord = Record<string, unknown>;

function fileExists(targetPath: string): boolean {
  try {
    return fs.statSync(targetPath).isFile();
  } catch {
    return false;
  }
}

function directoryExists(targetPath: string): boolean {
  try {
    return fs.statSync(targetPath).isDirectory();
  } catch {
    return false;
  }
}

function isRecord(value: unknown): value is JsonRecord {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function readJsonFile(filePath: string): JsonRecord | undefined {
  try {
    const parsed = JSON.parse(fs.readFileSync(filePath, 'utf8')) as unknown;
    return isRecord(parsed) ? parsed : undefined;
  } catch {
    return undefined;
  }
}

function toWorkspaceRelativePath(targetPath: string, workspaceRoot: string): string {
  if (!workspaceRoot) {
    return targetPath;
  }

  return path.relative(workspaceRoot, targetPath);
}

function resolveProjectRoot(projectPath: string): string | undefined {
  if (!projectPath) {
    return undefined;
  }

  if (fileExists(projectPath) && projectPath.toLowerCase().endsWith('.pbip')) {
    return path.dirname(projectPath);
  }

  if (fileExists(projectPath) && path.basename(projectPath).toLowerCase() === 'definition.pbir') {
    return path.dirname(projectPath);
  }

  if (fileExists(projectPath) && path.basename(projectPath).toLowerCase() === 'report.json') {
    return path.dirname(path.dirname(projectPath));
  }

  if (directoryExists(projectPath)) {
    return projectPath;
  }

  return undefined;
}

function resolveReportRoot(projectRoot: string): string | undefined {
  if (projectRoot.toLowerCase().endsWith('.report')) {
    return projectRoot;
  }

  if (fileExists(path.join(projectRoot, 'definition.pbir'))) {
    return projectRoot;
  }

  const reportFolders = fs
    .readdirSync(projectRoot, { withFileTypes: true })
    .filter((entry) => entry.isDirectory() && entry.name.toLowerCase().endsWith('.report'))
    .map((entry) => path.join(projectRoot, entry.name));

  return reportFolders.find((reportFolder) =>
    fileExists(path.join(reportFolder, 'definition', 'report.json')),
  );
}

function resolveReportLocation(projectPath: string): PbirReportLocation | undefined {
  const projectRootPath = resolveProjectRoot(projectPath);
  if (!projectRootPath) {
    return undefined;
  }

  const reportRootPath = resolveReportRoot(projectRootPath);
  if (!reportRootPath) {
    return undefined;
  }

  const definitionPath = path.join(reportRootPath, 'definition');
  const reportJsonPath = path.join(definitionPath, 'report.json');
  if (!fileExists(reportJsonPath)) {
    return undefined;
  }

  const reportFolderName = path.basename(reportRootPath);
  const reportName = reportFolderName.toLowerCase().endsWith('.report')
    ? reportFolderName.slice(0, -'.report'.length)
    : reportFolderName;

  return {
    projectRootPath,
    reportRootPath,
    definitionPath,
    reportJsonPath,
    workspaceRootPath: resolvePbirWorkspaceRoot(projectRootPath),
    reportName,
  };
}

function getOrderedPageIds(pagesRoot: string): string[] {
  const pagesMetadata = readJsonFile(path.join(pagesRoot, 'pages.json'));
  const pageOrder = pagesMetadata?.pageOrder;
  if (Array.isArray(pageOrder) && pageOrder.length > 0) {
    return pageOrder.filter((pageId): pageId is string => typeof pageId === 'string' && pageId.length > 0);
  }

  if (!directoryExists(pagesRoot)) {
    return [];
  }

  return fs
    .readdirSync(pagesRoot, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => entry.name);
}

function buildThemeNode(location: PbirReportLocation): PbirThemeNode | undefined {
  const reportJson = readJsonFile(location.reportJsonPath);
  const theme = reportJson?.theme;
  if (!isRecord(theme)) {
    return undefined;
  }

  const themeName = typeof theme.name === 'string' ? theme.name : undefined;
  const themeHref = typeof theme.href === 'string' ? theme.href : undefined;

  let sourcePath: string | undefined;
  if (themeHref) {
    const candidatePath = path.isAbsolute(themeHref)
      ? themeHref
      : path.join(location.definitionPath, themeHref);

    sourcePath = fileExists(candidatePath)
      ? toWorkspaceRelativePath(candidatePath, location.workspaceRootPath)
      : themeHref;
  }

  return {
    name: themeName,
    sourcePath,
  };
}

function buildVisualNodes(pageFolder: string, workspaceRoot: string): PbirVisualNode[] {
  const visualsRoot = path.join(pageFolder, 'visuals');
  if (!directoryExists(visualsRoot)) {
    return [];
  }

  const visuals: PbirVisualNode[] = [];
  for (const entry of fs.readdirSync(visualsRoot, { withFileTypes: true })) {
    if (!entry.isDirectory()) {
      continue;
    }

    const visualFolder = path.join(visualsRoot, entry.name);
    const visualJsonPath = path.join(visualFolder, 'visual.json');
    if (!fileExists(visualJsonPath)) {
      continue;
    }

    const visualJson = readJsonFile(visualJsonPath);
    const visualSection = isRecord(visualJson?.visual) ? visualJson.visual : undefined;
    visuals.push({
      name: typeof visualJson?.name === 'string' ? visualJson.name : entry.name,
      visualType: typeof visualSection?.visualType === 'string' ? visualSection.visualType : undefined,
      path: toWorkspaceRelativePath(visualJsonPath, workspaceRoot),
    });
  }

  return visuals;
}

function buildPageNodes(location: PbirReportLocation): PbirPageNode[] {
  const pagesRoot = path.join(location.definitionPath, 'pages');
  const pages: PbirPageNode[] = [];

  for (const pageId of getOrderedPageIds(pagesRoot)) {
    const pageFolder = path.join(pagesRoot, pageId);
    const pageJsonPath = path.join(pageFolder, 'page.json');
    if (!fileExists(pageJsonPath)) {
      continue;
    }

    const pageJson = readJsonFile(pageJsonPath);
    const name = typeof pageJson?.name === 'string' ? pageJson.name : pageId;
    pages.push({
      name,
      displayName: typeof pageJson?.displayName === 'string' ? pageJson.displayName : name,
      path: toWorkspaceRelativePath(pageJsonPath, location.workspaceRootPath),
      visuals: buildVisualNodes(pageFolder, location.workspaceRootPath),
    });
  }

  return pages;
}

export function buildLocalPbirTree(projectPath: string): PbirReportNode | undefined {
  const location = resolveReportLocation(projectPath);
  if (!location) {
    return undefined;
  }

  return {
    name: location.reportName,
    path: toWorkspaceRelativePath(location.reportJsonPath, location.workspaceRootPath),
    theme: buildThemeNode(location),
    pages: buildPageNodes(location),
  };
}
