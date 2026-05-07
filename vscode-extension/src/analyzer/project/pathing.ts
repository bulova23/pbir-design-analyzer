import * as fs from 'fs';
import * as path from 'path';

function pathExists(targetPath: string): boolean {
  try {
    fs.accessSync(targetPath);
    return true;
  } catch {
    return false;
  }
}

function isFile(targetPath: string): boolean {
  try {
    return fs.statSync(targetPath).isFile();
  } catch {
    return false;
  }
}

function isDirectory(targetPath: string): boolean {
  try {
    return fs.statSync(targetPath).isDirectory();
  } catch {
    return false;
  }
}

function isPbipFile(targetPath: string): boolean {
  return isFile(targetPath) && targetPath.toLowerCase().endsWith('.pbip');
}

function isDefinitionPbirFile(targetPath: string): boolean {
  return isFile(targetPath) && path.basename(targetPath).toLowerCase() === 'definition.pbir';
}

function isReportJsonFile(targetPath: string): boolean {
  return (
    isFile(targetPath) &&
    path.basename(targetPath).toLowerCase() === 'report.json' &&
    path.basename(path.dirname(targetPath)).toLowerCase() === 'definition'
  );
}

function isReportFolder(targetPath: string): boolean {
  return isDirectory(targetPath) && targetPath.toLowerCase().endsWith('.report');
}

function hasDefinitionPbir(targetPath: string): boolean {
  return pathExists(path.join(targetPath, 'definition.pbir'));
}

function hasReportJsonDefinition(targetPath: string): boolean {
  return pathExists(path.join(targetPath, 'definition', 'report.json'));
}

function hasPbirReportDefinition(targetPath: string): boolean {
  return hasDefinitionPbir(targetPath) || hasReportJsonDefinition(targetPath);
}

export function resolvePbirProjectPath(selectionPath: string): string | undefined {
  if (!selectionPath) {
    return undefined;
  }

  if (isDefinitionPbirFile(selectionPath)) {
    return path.dirname(selectionPath);
  }

  if (isReportJsonFile(selectionPath)) {
    return path.dirname(path.dirname(selectionPath));
  }

  if (isPbipFile(selectionPath)) {
    return selectionPath;
  }

  if ((isReportFolder(selectionPath) || hasDefinitionPbir(selectionPath)) && hasPbirReportDefinition(selectionPath)) {
    return selectionPath;
  }

  if (!isDirectory(selectionPath)) {
    return undefined;
  }

  const directoryEntries = fs.readdirSync(selectionPath, { withFileTypes: true });
  const pbipFile = directoryEntries.find((entry) => entry.isFile() && entry.name.toLowerCase().endsWith('.pbip'));
  if (pbipFile) {
    return path.join(selectionPath, pbipFile.name);
  }

  const reportFolder = directoryEntries.find(
    (entry) =>
      entry.isDirectory() &&
      entry.name.toLowerCase().endsWith('.report') &&
      hasPbirReportDefinition(path.join(selectionPath, entry.name)),
  );
  if (reportFolder) {
    return path.join(selectionPath, reportFolder.name);
  }

  return undefined;
}

export function resolvePbirWorkspaceRoot(projectPath: string): string {
  const workspaceOverride = process.env.WORKSPACE_PATH;
  if (workspaceOverride && isDirectory(workspaceOverride)) {
    return workspaceOverride;
  }

  let currentPath = projectPath;
  if (isFile(currentPath)) {
    currentPath = path.dirname(currentPath);
  }

  if (!isDirectory(currentPath)) {
    return projectPath;
  }

  let probe = currentPath;
  while (!isDirectory(path.join(probe, '.git'))) {
    const parent = path.dirname(probe);
    if (parent === probe) {
      return currentPath;
    }

    probe = parent;
  }

  return probe;
}
