import {
  createRepositorySnapshot,
  type RepositorySnapshot,
  type RepositorySnapshotFile,
} from '../../project/repoSnapshot';

export async function withRepositorySnapshot<T>(
  source: RepositorySnapshot | string,
  action: (snapshot: RepositorySnapshot) => Promise<T>,
): Promise<T> {
  if (typeof source !== 'string') {
    return action(source);
  }

  const snapshot = await createRepositorySnapshot(source);
  try {
    return await action(snapshot);
  } finally {
    snapshot.dispose();
  }
}

export function listSnapshotFiles(
  snapshot: RepositorySnapshot,
  predicate?: (file: RepositorySnapshotFile) => boolean,
): RepositorySnapshotFile[] {
  const files = snapshot.listFiles();
  return predicate ? files.filter(predicate) : [...files];
}

export async function readSnapshotText(
  snapshot: RepositorySnapshot,
  file: RepositorySnapshotFile,
): Promise<string> {
  try {
    return await snapshot.readText(file);
  } catch {
    return '';
  }
}
