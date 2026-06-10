import * as fs from 'fs';
import * as path from 'path';

export interface RepositorySnapshotFs {
  readdir(dirPath: string, options: { withFileTypes: true }): Promise<fs.Dirent[]>;
  readFile(filePath: string, encoding: BufferEncoding): Promise<string>;
  stat(targetPath: string): Promise<fs.Stats>;
}

export interface RepositorySnapshotOptions {
  maxDepth?: number;
  fileSystem?: RepositorySnapshotFs;
}

export interface RepositorySnapshotFile {
  absolutePath: string;
  relativePath: string;
  name: string;
  extension: string;
  depth: number;
}

export interface RepositorySnapshot {
  readonly rootPath: string;
  readonly maxDepth: number;
  listFiles(): readonly RepositorySnapshotFile[];
  resolveFile(relativePath: string): RepositorySnapshotFile;
  readText(file: RepositorySnapshotFile): Promise<string>;
  dispose(): void;
}

const defaultFileSystem: RepositorySnapshotFs = {
  readdir(dirPath, options) {
    return fs.promises.readdir(dirPath, options);
  },
  readFile(filePath, encoding) {
    return fs.promises.readFile(filePath, encoding);
  },
  stat(targetPath) {
    return fs.promises.stat(targetPath);
  },
};

function compareText(left: string, right: string): number {
  if (left < right) {
    return -1;
  }

  if (left > right) {
    return 1;
  }

  return 0;
}

function shouldSkipEntry(entry: fs.Dirent): boolean {
  return entry.name === 'node_modules' || entry.name.startsWith('.git');
}

class RepositorySnapshotImpl implements RepositorySnapshot {
  private disposed = false;
  private readonly textCache = new Map<string, string>();

  constructor(
    readonly rootPath: string,
    readonly maxDepth: number,
    private readonly files: RepositorySnapshotFile[],
    private readonly fileSystem: RepositorySnapshotFs,
  ) {}

  listFiles(): readonly RepositorySnapshotFile[] {
    this.ensureActive();
    return this.files;
  }

  resolveFile(relativePath: string): RepositorySnapshotFile {
    this.ensureActive();

    const normalized = relativePath.split(path.sep).join('/');
    const file = this.files.find((entry) => entry.relativePath === normalized);
    if (!file) {
      throw new Error(`Repository snapshot file not found: ${relativePath}`);
    }

    return file;
  }

  async readText(file: RepositorySnapshotFile): Promise<string> {
    this.ensureActive();

    const cached = this.textCache.get(file.absolutePath);
    if (cached !== undefined) {
      return cached;
    }

    try {
      const text = await this.fileSystem.readFile(file.absolutePath, 'utf8');
      this.textCache.set(file.absolutePath, text);
      return text;
    } catch {
      this.textCache.set(file.absolutePath, '');
      return '';
    }
  }

  dispose(): void {
    this.disposed = true;
    this.textCache.clear();
  }

  private ensureActive(): void {
    if (this.disposed) {
      throw new Error('Repository snapshot has been disposed');
    }
  }
}

export async function createRepositorySnapshot(
  rootPath: string,
  options: RepositorySnapshotOptions = {},
): Promise<RepositorySnapshot> {
  const maxDepth = options.maxDepth ?? 4;
  const fileSystem = options.fileSystem ?? defaultFileSystem;

  const rootStats = await fileSystem.stat(rootPath);
  if (!rootStats.isDirectory()) {
    throw new Error(`Repository snapshot root must be a directory: ${rootPath}`);
  }

  const files: RepositorySnapshotFile[] = [];

  async function visit(currentPath: string, depth: number): Promise<void> {
    if (depth > maxDepth) {
      return;
    }

    let entries: fs.Dirent[] = [];
    try {
      entries = await fileSystem.readdir(currentPath, { withFileTypes: true });
    } catch {
      return;
    }

    entries.sort((left, right) => compareText(left.name, right.name));

    for (const entry of entries) {
      if (shouldSkipEntry(entry)) {
        continue;
      }

      const fullPath = path.join(currentPath, entry.name);
      if (entry.isDirectory()) {
        await visit(fullPath, depth + 1);
        continue;
      }

      files.push({
        absolutePath: fullPath,
        relativePath: path.relative(rootPath, fullPath).split(path.sep).join('/'),
        name: entry.name,
        extension: path.extname(entry.name).toLowerCase(),
        depth,
      });
    }
  }

  await visit(rootPath, 0);
  files.sort((left, right) => compareText(left.relativePath, right.relativePath));

  return new RepositorySnapshotImpl(rootPath, maxDepth, files, fileSystem);
}
