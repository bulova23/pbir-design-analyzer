import * as fs from 'fs';
import * as path from 'path';

export function collectRepoFiles(rootPath: string, maxDepth = 4): string[] {
  const files: string[] = [];

  function visit(currentPath: string, depth: number): void {
    if (depth > maxDepth) {
      return;
    }

    let entries: fs.Dirent[] = [];
    try {
      entries = fs.readdirSync(currentPath, { withFileTypes: true });
    } catch {
      return;
    }

    for (const entry of entries) {
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

export function readRepoText(filePath: string): string {
  try {
    return fs.readFileSync(filePath, 'utf8');
  } catch {
    return '';
  }
}

export function toRelativePath(rootPath: string, filePath: string): string {
  return path.relative(rootPath, filePath) || path.basename(filePath);
}
