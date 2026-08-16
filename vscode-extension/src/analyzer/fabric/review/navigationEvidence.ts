import type { RepositorySnapshot, RepositorySnapshotFile } from '../../project/repoSnapshot';
import { listSnapshotFiles, readSnapshotText, withRepositorySnapshot } from './repoEvidence';
import type { NavigationEvidenceReport, NavigationRouteEvidence } from './reviewTypes';

const ROUTE_PATTERN = /path\s*:\s*['"`]([^'"`]+)['"`][^}]*label\s*:\s*['"`]([^'"`]+)['"`]/g;

async function parseRoutes(
  snapshot: RepositorySnapshot,
  file: RepositorySnapshotFile,
): Promise<NavigationRouteEvidence[]> {
  const text = await readSnapshotText(snapshot, file);
  const relativePath = file.relativePath;
  const routes: NavigationRouteEvidence[] = [];

  for (const match of text.matchAll(ROUTE_PATTERN)) {
    routes.push({
      path: match[1],
      label: match[2],
      filePath: relativePath,
    });
  }

  return routes;
}

async function extractNavigationEvidenceFromSnapshot(
  snapshot: RepositorySnapshot,
): Promise<NavigationEvidenceReport> {
  const routeFiles = listSnapshotFiles(snapshot, (file) => /\.(ts|tsx)$/i.test(file.relativePath));
  const routeGroups = await Promise.all(routeFiles.map((file) => parseRoutes(snapshot, file)));
  const routes = routeGroups.flat();
  const normalizedLabels = routes.map((route) => route.label.toLowerCase());
  const normalizedPaths = routes.map((route) => route.path.toLowerCase());
  const hasOverview = normalizedLabels.some((label) => label.includes('overview') || label.includes('executive'))
    || normalizedPaths.some((routePath) => routePath.includes('overview') || routePath === '/');
  const hasDetail = normalizedLabels.some((label) => label.includes('detail'))
    || normalizedPaths.some((routePath) => routePath.includes('detail'));

  return {
    routes,
    hasExecutiveToDetailFlow: hasOverview && hasDetail,
    summary: routes.length > 0
      ? `Detected ${routes.length} route definition(s) including ${routes.map((route) => route.path).join(', ')}.`
      : 'No route definitions detected.',
  };
}

export async function extractNavigationEvidence(
  source: RepositorySnapshot | string,
): Promise<NavigationEvidenceReport> {
  return withRepositorySnapshot(source, extractNavigationEvidenceFromSnapshot);
}
