import * as fs from 'fs';
import * as path from 'path';
import { createRepositorySnapshot, type RepositorySnapshot } from './repoSnapshot';
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

async function fileExists(targetPath: string): Promise<boolean> {
  try {
    return (await fs.promises.stat(targetPath)).isFile();
  } catch {
    return false;
  }
}

async function directoryExists(targetPath: string): Promise<boolean> {
  try {
    return (await fs.promises.stat(targetPath)).isDirectory();
  } catch {
    return false;
  }
}

function isRecord(value: unknown): value is JsonRecord {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

async function readJsonFile(filePath: string): Promise<JsonRecord | undefined> {
  try {
    const parsed = JSON.parse(await fs.promises.readFile(filePath, 'utf8')) as unknown;
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

async function resolveProjectRoot(projectPath: string): Promise<string | undefined> {
  if (!projectPath) {
    return undefined;
  }

  if (await fileExists(projectPath) && projectPath.toLowerCase().endsWith('.pbip')) {
    return path.dirname(projectPath);
  }

  if (await fileExists(projectPath) && path.basename(projectPath).toLowerCase() === 'definition.pbir') {
    return path.dirname(projectPath);
  }

  if (await fileExists(projectPath) && path.basename(projectPath).toLowerCase() === 'report.json') {
    return path.dirname(path.dirname(projectPath));
  }

  if (await directoryExists(projectPath)) {
    return projectPath;
  }

  return undefined;
}

async function resolveReportRoot(projectRoot: string): Promise<string | undefined> {
  if (projectRoot.toLowerCase().endsWith('.report')) {
    return projectRoot;
  }

  if (await fileExists(path.join(projectRoot, 'definition.pbir'))) {
    return projectRoot;
  }

  let entries: fs.Dirent[] = [];
  try {
    entries = await fs.promises.readdir(projectRoot, { withFileTypes: true });
  } catch {
    return undefined;
  }

  const reportFolders = entries
    .filter((entry) => entry.isDirectory() && entry.name.toLowerCase().endsWith('.report'))
    .map((entry) => path.join(projectRoot, entry.name));

  for (const reportFolder of reportFolders) {
    if (await fileExists(path.join(reportFolder, 'definition', 'report.json'))) {
      return reportFolder;
    }
  }

  return undefined;
}

async function resolveReportLocation(projectPath: string): Promise<PbirReportLocation | undefined> {
  const projectRootPath = await resolveProjectRoot(projectPath);
  if (!projectRootPath) {
    return undefined;
  }

  const reportRootPath = await resolveReportRoot(projectRootPath);
  if (!reportRootPath) {
    return undefined;
  }

  const definitionPath = path.join(reportRootPath, 'definition');
  const reportJsonPath = path.join(definitionPath, 'report.json');
  if (!await fileExists(reportJsonPath)) {
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

function compareText(left: string, right: string): number {
  if (left < right) {
    return -1;
  }

  if (left > right) {
    return 1;
  }

  return 0;
}

function toSnapshotRelativePath(snapshot: RepositorySnapshot, targetPath: string): string {
  return path.relative(snapshot.rootPath, targetPath).split(path.sep).join('/');
}

function tryResolveSnapshotFile(snapshot: RepositorySnapshot, relativePath: string) {
  try {
    return snapshot.resolveFile(relativePath);
  } catch {
    return undefined;
  }
}

async function readSnapshotJson(
  snapshot: RepositorySnapshot,
  relativePath: string,
): Promise<JsonRecord | undefined> {
  const file = tryResolveSnapshotFile(snapshot, relativePath);
  if (!file) {
    return undefined;
  }

  try {
    const parsed = JSON.parse(await snapshot.readText(file)) as unknown;
    return isRecord(parsed) ? parsed : undefined;
  } catch {
    return undefined;
  }
}

function hasSnapshotDirectory(snapshot: RepositorySnapshot, relativeDirPath: string): boolean {
  const normalizedDir = relativeDirPath.replace(/\\/g, '/').replace(/\/+$/, '');
  const prefix = normalizedDir.length > 0 ? `${normalizedDir}/` : '';
  return snapshot.listFiles().some((file) => file.relativePath.startsWith(prefix));
}

async function getOrderedPageIds(
  snapshot: RepositorySnapshot,
  location: PbirReportLocation,
): Promise<string[]> {
  const pagesRoot = path.join(location.definitionPath, 'pages');
  const pagesMetadata = await readSnapshotJson(
    snapshot,
    toSnapshotRelativePath(snapshot, path.join(pagesRoot, 'pages.json')),
  );
  const pageOrder = pagesMetadata?.pageOrder;
  if (Array.isArray(pageOrder) && pageOrder.length > 0) {
    return pageOrder.filter((pageId): pageId is string => typeof pageId === 'string' && pageId.length > 0);
  }

  const pagesRootRelative = toSnapshotRelativePath(snapshot, pagesRoot);
  if (!hasSnapshotDirectory(snapshot, pagesRootRelative)) {
    return [];
  }

  const pageIds = new Set<string>();
  const prefix = `${pagesRootRelative}/`;
  for (const file of snapshot.listFiles()) {
    if (!file.relativePath.startsWith(prefix)) {
      continue;
    }

    const remainder = file.relativePath.slice(prefix.length);
    const [pageId] = remainder.split('/');
    if (pageId) {
      pageIds.add(pageId);
    }
  }

  return [...pageIds].sort(compareText);
}

async function buildThemeNode(
  snapshot: RepositorySnapshot,
  location: PbirReportLocation,
): Promise<PbirThemeNode | undefined> {
  const reportJson = await readSnapshotJson(
    snapshot,
    toSnapshotRelativePath(snapshot, location.reportJsonPath),
  );
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

    sourcePath = tryResolveSnapshotFile(snapshot, toSnapshotRelativePath(snapshot, candidatePath))
      ? toWorkspaceRelativePath(candidatePath, location.workspaceRootPath)
      : themeHref;
  }

  return {
    name: themeName,
    sourcePath,
  };
}

async function buildVisualNodes(
  snapshot: RepositorySnapshot,
  pageFolder: string,
  workspaceRoot: string,
): Promise<PbirVisualNode[]> {
  const visualsRoot = path.join(pageFolder, 'visuals');
  const visualsRootRelative = toSnapshotRelativePath(snapshot, visualsRoot);
  if (!hasSnapshotDirectory(snapshot, visualsRootRelative)) {
    return [];
  }

  const visuals: PbirVisualNode[] = [];
  const visualIds = new Set<string>();
  const prefix = `${visualsRootRelative}/`;
  for (const file of snapshot.listFiles()) {
    if (!file.relativePath.startsWith(prefix)) {
      continue;
    }

    const remainder = file.relativePath.slice(prefix.length);
    const [visualId] = remainder.split('/');
    if (visualId) {
      visualIds.add(visualId);
    }
  }

  for (const visualId of [...visualIds].sort(compareText)) {
    const visualFolder = path.join(visualsRoot, visualId);
    const visualJsonPath = path.join(visualFolder, 'visual.json');
    const visualJsonRelative = toSnapshotRelativePath(snapshot, visualJsonPath);
    if (!tryResolveSnapshotFile(snapshot, visualJsonRelative)) {
      continue;
    }

    const visualJson = await readSnapshotJson(snapshot, visualJsonRelative);
    const visualSection = isRecord(visualJson?.visual) ? visualJson.visual : undefined;
    visuals.push({
      name: typeof visualJson?.name === 'string' ? visualJson.name : visualId,
      visualType: typeof visualSection?.visualType === 'string' ? visualSection.visualType : undefined,
      path: toWorkspaceRelativePath(visualJsonPath, workspaceRoot),
    });
  }

  return visuals;
}

async function buildPageNodes(
  snapshot: RepositorySnapshot,
  location: PbirReportLocation,
): Promise<PbirPageNode[]> {
  const pages: PbirPageNode[] = [];

  for (const pageId of await getOrderedPageIds(snapshot, location)) {
    const pageFolder = path.join(location.definitionPath, 'pages', pageId);
    const pageJsonPath = path.join(pageFolder, 'page.json');
    const pageJsonRelative = toSnapshotRelativePath(snapshot, pageJsonPath);
    if (!tryResolveSnapshotFile(snapshot, pageJsonRelative)) {
      continue;
    }

    const pageJson = await readSnapshotJson(snapshot, pageJsonRelative);
    const name = typeof pageJson?.name === 'string' ? pageJson.name : pageId;
    pages.push({
      name,
      displayName: typeof pageJson?.displayName === 'string' ? pageJson.displayName : name,
      path: toWorkspaceRelativePath(pageJsonPath, location.workspaceRootPath),
      visuals: await buildVisualNodes(snapshot, pageFolder, location.workspaceRootPath),
    });
  }

  return pages;
}

export async function buildLocalPbirTree(projectPath: string): Promise<PbirReportNode | undefined> {
  const location = await resolveReportLocation(projectPath);
  if (!location) {
    return undefined;
  }

  const snapshot = await createRepositorySnapshot(location.projectRootPath, { maxDepth: 8 });

  try {
    return {
      name: location.reportName,
      path: toWorkspaceRelativePath(location.reportJsonPath, location.workspaceRootPath),
      theme: await buildThemeNode(snapshot, location),
      pages: await buildPageNodes(snapshot, location),
    };
  } finally {
    snapshot.dispose();
  }
}
