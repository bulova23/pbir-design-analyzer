import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type { FixFileVersionSnapshot, RollbackFileBackup } from '../contracts/scorePanel';

export class FixPersistenceValidationError extends Error {
  public readonly validationErrors: string[];

  public constructor(validationErrors: string[]) {
    super(validationErrors[0] ?? 'fix-persistence-validation-failed');
    this.name = 'FixPersistenceValidationError';
    this.validationErrors = validationErrors;
  }
}

export interface FixPersistenceService {
  readJsonFile(filePath: string): Promise<Record<string, unknown>>;
  captureFileVersion(filePath: string): Promise<FixFileVersionSnapshot>;
  writeJsonFilesAtomically(
    fileJson: Map<string, Record<string, unknown>>,
    options?: {
      validate?: Array<() => Promise<string[]>>;
    },
  ): Promise<Map<string, FixFileVersionSnapshot>>;
  restoreBackups(
    backups: RollbackFileBackup[],
    expectedAppliedVersions?: Map<string, FixFileVersionSnapshot>,
  ): Promise<{ restoredFiles: string[]; conflictErrors: string[] }>;
}

function buildFileVersion(buffer: Buffer, stats: fs.Stats): FixFileVersionSnapshot {
  return {
    contentHash: crypto.createHash('sha256').update(buffer).digest('hex'),
    size: stats.size,
    modifiedTimeMs: stats.mtimeMs,
  };
}

function sameFileVersion(left: FixFileVersionSnapshot | undefined, right: FixFileVersionSnapshot | undefined): boolean {
  return Boolean(left)
    && Boolean(right)
    && left!.contentHash === right!.contentHash
    && left!.size === right!.size
    && left!.modifiedTimeMs === right!.modifiedTimeMs;
}

export function captureFixFileVersionSync(filePath: string): FixFileVersionSnapshot {
  const buffer = fs.readFileSync(filePath);
  const stats = fs.statSync(filePath);
  return buildFileVersion(buffer, stats);
}

export class NodeFixPersistenceService implements FixPersistenceService {
  public async readJsonFile(filePath: string): Promise<Record<string, unknown>> {
    const content = await fs.promises.readFile(filePath, 'utf8');
    return JSON.parse(content) as Record<string, unknown>;
  }

  public async captureFileVersion(filePath: string): Promise<FixFileVersionSnapshot> {
    const [buffer, stats] = await Promise.all([
      fs.promises.readFile(filePath),
      fs.promises.stat(filePath),
    ]);
    return buildFileVersion(buffer, stats);
  }

  public async writeJsonFilesAtomically(
    fileJson: Map<string, Record<string, unknown>>,
    options?: {
      validate?: Array<() => Promise<string[]>>;
    },
  ): Promise<Map<string, FixFileVersionSnapshot>> {
    const tempFiles: string[] = [];

    try {
      for (const [targetFile, json] of fileJson.entries()) {
        const tempFile = path.join(
          path.dirname(targetFile),
          `${path.basename(targetFile)}.${Date.now()}.${Math.random().toString(16).slice(2)}.tmp`,
        );
        tempFiles.push(tempFile);
        await fs.promises.writeFile(tempFile, JSON.stringify(json, null, 2), 'utf8');
        await fs.promises.rename(tempFile, targetFile);
      }

      const validationErrors = (
        await Promise.all((options?.validate ?? []).map(async (validate) => validate()))
      ).flat();
      if (validationErrors.length > 0) {
        throw new FixPersistenceValidationError(validationErrors);
      }

      const writtenVersions = new Map<string, FixFileVersionSnapshot>();
      for (const targetFile of fileJson.keys()) {
        writtenVersions.set(targetFile, await this.captureFileVersion(targetFile));
      }

      return writtenVersions;
    } finally {
      await Promise.all(tempFiles.map(async (tempFile) => {
        try {
          await fs.promises.rm(tempFile, { force: true });
        } catch {
          // best-effort cleanup for abandoned temp files
        }
      }));
    }
  }

  public async restoreBackups(
    backups: RollbackFileBackup[],
    expectedAppliedVersions?: Map<string, FixFileVersionSnapshot>,
  ): Promise<{ restoredFiles: string[]; conflictErrors: string[] }> {
    const conflictErrors: string[] = [];
    const restorable = backups.filter((backup) => {
      const expectedVersion = expectedAppliedVersions?.get(backup.targetFile) ?? backup.appliedVersion;
      if (!expectedVersion) {
        return true;
      }

      const currentVersion = captureFixFileVersionSync(backup.targetFile);
      if (!sameFileVersion(currentVersion, expectedVersion)) {
        conflictErrors.push(`rollback-conflict:${backup.targetFile}`);
        return false;
      }

      return true;
    });

    if (conflictErrors.length > 0) {
      return {
        restoredFiles: [],
        conflictErrors,
      };
    }

    for (const backup of restorable) {
      try {
        const currentContent = await fs.promises.readFile(backup.targetFile, 'utf8');
        if (currentContent === backup.beforeContent) {
          continue;
        }
      } catch {
        // fall through to the write path when the current file cannot be read
      }

      const tempFile = path.join(
        path.dirname(backup.targetFile),
        `${path.basename(backup.targetFile)}.${Date.now()}.${Math.random().toString(16).slice(2)}.tmp`,
      );
      try {
        await fs.promises.writeFile(tempFile, backup.beforeContent, 'utf8');
        await fs.promises.rename(tempFile, backup.targetFile);
      } finally {
        try {
          await fs.promises.rm(tempFile, { force: true });
        } catch {
          // best-effort cleanup for abandoned temp files
        }
      }
    }

    return {
      restoredFiles: restorable.map((backup) => backup.targetFile),
      conflictErrors: [],
    };
  }
}
