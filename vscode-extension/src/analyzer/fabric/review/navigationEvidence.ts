import { collectRepoFiles, readRepoText, toRelativePath } from './repoEvidence';
import type { NavigationEvidenceReport, NavigationRouteEvidence } from './reviewTypes';

const ROUTE_PATTERN = /path\s*:\s*['"`]([^'"`]+)['"`][^}]*label\s*:\s*['"`]([^'"`]+)['"`]/g;

function parseRoutes(rootPath: string, filePath: string): NavigationRouteEvidence[] {
  const text = readRepoText(filePath);
  const relativePath = toRelativePath(rootPath, filePath);
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

export function extractNavigationEvidence(rootPath: string): NavigationEvidenceReport {
  const routeFiles = collectRepoFiles(rootPath).filter((filePath) => /\.(ts|tsx)$/i.test(filePath));
  const routes = routeFiles.flatMap((filePath) => parseRoutes(rootPath, filePath));
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
