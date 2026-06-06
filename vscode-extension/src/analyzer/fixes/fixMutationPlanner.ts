import * as fs from 'fs';
import * as path from 'path';
import type {
  FixMutation,
  FixOpportunityCategory,
  PageVisualMetadataSummary,
  ScoreResult,
  VisualMetadataItem,
} from '../contracts/scorePanel';

function compareText(left: string, right: string): number {
  if (left < right) {
    return -1;
  }

  if (left > right) {
    return 1;
  }

  return 0;
}

interface ReportDefinitionPaths {
  definitionRoot: string;
  reportJsonPath: string;
  themeFilePath?: string;
  pages: Array<{
    pageId: string;
    pageName: string;
    displayName: string;
    pageJsonPath: string;
    visualFiles: Map<string, string>;
  }>;
}

interface PlanningPage {
  pageName: string;
  visualMetadata?: PageVisualMetadataSummary;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function readJsonFile(filePath: string): Record<string, unknown> {
  return JSON.parse(fs.readFileSync(filePath, 'utf8')) as Record<string, unknown>;
}

export function resolveReportDefinitionPaths(reportPath: string): ReportDefinitionPaths | undefined {
  const reportRoot = reportPath.toLowerCase().endsWith('.report')
    ? reportPath
    : path.join(reportPath, `${path.basename(reportPath, path.extname(reportPath))}.Report`);
  const definitionRoot = path.join(reportRoot, 'definition');
  const reportJsonPath = path.join(definitionRoot, 'report.json');
  if (!fs.existsSync(reportJsonPath)) {
    return undefined;
  }

  const reportJson = readJsonFile(reportJsonPath);
  const themeNode = isRecord(reportJson.theme) ? reportJson.theme : undefined;
  const themeHref = typeof themeNode?.href === 'string' ? themeNode.href : undefined;
  const themeFilePath = themeHref ? path.join(definitionRoot, themeHref) : undefined;

  const pagesRoot = path.join(definitionRoot, 'pages');
  const pagesJsonPath = path.join(pagesRoot, 'pages.json');
  const pageOrder = fs.existsSync(pagesJsonPath)
    ? ((readJsonFile(pagesJsonPath).pageOrder as unknown[]) ?? []).filter((entry): entry is string => typeof entry === 'string')
    : fs.readdirSync(pagesRoot)
      .filter((entry) => fs.statSync(path.join(pagesRoot, entry)).isDirectory())
      .sort(compareText);

  const pages = pageOrder.map((pageId) => {
    const pageDir = path.join(pagesRoot, pageId);
    const pageJsonPath = path.join(pageDir, 'page.json');
    const pageJson = fs.existsSync(pageJsonPath) ? readJsonFile(pageJsonPath) : {};
    const pageName = typeof pageJson.name === 'string'
        ? pageJson.name
        : pageId;
    const displayName = typeof pageJson.displayName === 'string'
      ? pageJson.displayName
      : pageName;
    const visualsRoot = path.join(pageDir, 'visuals');
    const visualFiles = new Map<string, string>();

    if (fs.existsSync(visualsRoot)) {
      for (const visualId of fs.readdirSync(visualsRoot).sort(compareText)) {
        const visualJsonPath = path.join(visualsRoot, visualId, 'visual.json');
        if (fs.existsSync(visualJsonPath)) {
          visualFiles.set(visualId, visualJsonPath);
        }
      }
    }

    return {
      pageId,
      pageName,
      displayName,
      pageJsonPath,
      visualFiles,
    };
  });

  return {
    definitionRoot,
    reportJsonPath,
    themeFilePath: themeFilePath && fs.existsSync(themeFilePath) ? themeFilePath : undefined,
    pages,
  };
}

function snapDown32(value: number): number {
  return Math.floor(value / 32) * 32;
}

function getPlanningPages(result: ScoreResult): PlanningPage[] {
  if (result.pageScores && result.pageScores.length > 0) {
    return result.pageScores.map((page) => ({
      pageName: page.pageName,
      visualMetadata: page.visualMetadata,
    }));
  }

  if (result.scoredPageName && result.visualMetadata) {
    return [{
      pageName: result.scoredPageName,
      visualMetadata: result.visualMetadata,
    }];
  }

  return [];
}

function findPage(paths: ReportDefinitionPaths, pageName: string) {
  return paths.pages.find((page) => page.pageName === pageName || page.displayName === pageName);
}

function isSupportedMutationCategory(category: FixOpportunityCategory): boolean {
  switch (category) {
    case 'title':
    case 'semanticColor':
      return false;
    default:
      return true;
  }
}

function findTitleVisual(page: PlanningPage | undefined): VisualMetadataItem | undefined {
  return page?.visualMetadata?.visuals.find((visual) => visual.hasVisibleTitleIntent)
    ?? page?.visualMetadata?.visuals.find((visual) => visual.visualType.toLowerCase().includes('text'));
}

function buildTitleMutations(
  paths: ReportDefinitionPaths,
  page: PlanningPage | undefined,
  pageName: string,
): FixMutation[] {
  const titleVisual = findTitleVisual(page);
  const pageDef = findPage(paths, pageName);
  if (!titleVisual || !pageDef) {
    return [];
  }

  const targetFile = pageDef.visualFiles.get(titleVisual.visualId);
  if (!targetFile) {
    return [];
  }

  const mutations: FixMutation[] = [];
  if (titleVisual.y !== 24) {
    mutations.push({
      id: `${titleVisual.visualId}-position-y`,
      pageName,
      targetObjectId: titleVisual.visualId,
      targetFile,
      propertyPath: 'position.y',
      mutationType: 'setPosition',
      before: titleVisual.y,
      after: 24,
    });
  }

  if (titleVisual.x !== 24) {
    mutations.push({
      id: `${titleVisual.visualId}-position-x`,
      pageName,
      targetObjectId: titleVisual.visualId,
      targetFile,
      propertyPath: 'position.x',
      mutationType: 'setPosition',
      before: titleVisual.x,
      after: 24,
    });
  }

  const desiredTitle = pageName;
  if (titleVisual.bestVisibleText !== desiredTitle) {
    mutations.push({
      id: `${titleVisual.visualId}-title-text`,
      pageName,
      targetObjectId: titleVisual.visualId,
      targetFile,
      propertyPath: 'title.text',
      mutationType: 'setTitleText',
      before: titleVisual.bestVisibleText,
      after: desiredTitle,
    });
  }

  return mutations;
}

function buildLayoutMutations(
  paths: ReportDefinitionPaths,
  page: PlanningPage | undefined,
  pageName: string,
): FixMutation[] {
  const pageDef = findPage(paths, pageName);
  if (!page?.visualMetadata || !pageDef) {
    return [];
  }

  return page.visualMetadata.visuals
    .filter((visual) => !visual.isNavigationElement)
    .flatMap((visual) => {
      const targetFile = pageDef.visualFiles.get(visual.visualId);
      if (!targetFile) {
        return [];
      }

      const snappedX = snapDown32(visual.x);
      const snappedY = snapDown32(visual.y);
      const mutations: FixMutation[] = [];
      if (snappedX !== visual.x) {
        mutations.push({
          id: `${visual.visualId}-layout-x`,
          pageName,
          targetObjectId: visual.visualId,
          targetFile,
          propertyPath: 'position.x',
          mutationType: 'setPosition',
          before: visual.x,
          after: snappedX,
        });
      }
      if (snappedY !== visual.y) {
        mutations.push({
          id: `${visual.visualId}-layout-y`,
          pageName,
          targetObjectId: visual.visualId,
          targetFile,
          propertyPath: 'position.y',
          mutationType: 'setPosition',
          before: visual.y,
          after: snappedY,
        });
      }
      return mutations;
    });
}

function buildNavigationMutations(
  paths: ReportDefinitionPaths,
  pages: PlanningPage[],
  affectedPages: string[],
): FixMutation[] {
  const navigationVisuals = pages
    .filter((page) => affectedPages.includes(page.pageName))
    .flatMap((page) => (page.visualMetadata?.visuals ?? [])
      .filter((visual) => visual.isNavigationElement)
      .map((visual) => ({ pageName: page.pageName, visual })));
  if (navigationVisuals.length < 2) {
    return [];
  }

  const anchorX = Math.min(...navigationVisuals.map((entry) => entry.visual.x));
  const anchorY = Math.min(...navigationVisuals.map((entry) => entry.visual.y));

  return navigationVisuals.flatMap(({ pageName, visual }) => {
    const pageDef = findPage(paths, pageName);
    const targetFile = pageDef?.visualFiles.get(visual.visualId);
    if (!targetFile) {
      return [];
    }

    const mutations: FixMutation[] = [];
    if (visual.x !== anchorX) {
      mutations.push({
        id: `${visual.visualId}-nav-x`,
        pageName,
        targetObjectId: visual.visualId,
        targetFile,
        propertyPath: 'position.x',
        mutationType: 'setNavigationPlacement',
        before: visual.x,
        after: anchorX,
      });
    }
    if (visual.y !== anchorY) {
      mutations.push({
        id: `${visual.visualId}-nav-y`,
        pageName,
        targetObjectId: visual.visualId,
        targetFile,
        propertyPath: 'position.y',
        mutationType: 'setNavigationPlacement',
        before: visual.y,
        after: anchorY,
      });
    }
    return mutations;
  });
}

function buildCrossPageTitleMutations(paths: ReportDefinitionPaths, pages: PlanningPage[], affectedPages: string[]): FixMutation[] {
  const titleVisuals = pages
    .filter((page) => affectedPages.includes(page.pageName))
    .map((page) => ({ pageName: page.pageName, visual: findTitleVisual(page) }))
    .filter((entry): entry is { pageName: string; visual: VisualMetadataItem } => Boolean(entry.visual));

  if (titleVisuals.length < 2) {
    return [];
  }

  const anchorX = Math.min(...titleVisuals.map((entry) => entry.visual.x));
  const anchorY = Math.min(...titleVisuals.map((entry) => entry.visual.y));
  return titleVisuals.flatMap(({ pageName, visual }) => {
    const pageDef = findPage(paths, pageName);
    const targetFile = pageDef?.visualFiles.get(visual.visualId);
    if (!targetFile) {
      return [];
    }
    const mutations: FixMutation[] = [];
    if (visual.x !== anchorX) {
      mutations.push({
        id: `${visual.visualId}-cross-title-x`,
        pageName,
        targetObjectId: visual.visualId,
        targetFile,
        propertyPath: 'position.x',
        mutationType: 'setPosition',
        before: visual.x,
        after: anchorX,
      });
    }
    if (visual.y !== anchorY) {
      mutations.push({
        id: `${visual.visualId}-cross-title-y`,
        pageName,
        targetObjectId: visual.visualId,
        targetFile,
        propertyPath: 'position.y',
        mutationType: 'setPosition',
        before: visual.y,
        after: anchorY,
      });
    }
    return mutations;
  });
}

function buildSemanticColorMutations(paths: ReportDefinitionPaths, pages: PlanningPage[], affectedPages: string[]): FixMutation[] {
  const assignments = pages
    .filter((page) => affectedPages.includes(page.pageName))
    .flatMap((page) => page.visualMetadata?.semanticColorMap ?? []);
  const semanticKey = assignments[0]?.semanticKey;
  if (!semanticKey) {
    return [];
  }

  const colors = [...new Set(assignments.map((assignment) => assignment.color.toLowerCase()))];
  if (colors.length < 2) {
    return [];
  }

  const canonicalColor = assignments[0].color;
  return assignments.flatMap((assignment) => {
    if (assignment.color.toLowerCase() === canonicalColor.toLowerCase()) {
      return [];
    }

    const pageDef = findPage(paths, assignment.sourcePageName);
    const targetFile = pageDef?.visualFiles.get(assignment.sourceVisualId);
    if (!targetFile) {
      return [];
    }

    return [{
      id: `${assignment.sourceVisualId}-semantic-color`,
      pageName: assignment.sourcePageName,
      targetObjectId: assignment.sourceVisualId,
      targetFile,
      propertyPath: 'background.color',
      mutationType: 'setSemanticColor' as const,
      before: assignment.color,
      after: canonicalColor,
    }];
  });
}

export function planMutationsForCategory(args: {
  category: FixOpportunityCategory;
  result: ScoreResult;
  pageName?: string;
  affectedPages: string[];
}): FixMutation[] {
  if (!isSupportedMutationCategory(args.category)) {
    return [];
  }

  const paths = resolveReportDefinitionPaths(args.result.reportPath);
  if (!paths) {
    return [];
  }

  const pages = getPlanningPages(args.result);
  const selectedPage = args.pageName ? pages.find((page) => page.pageName === args.pageName) : undefined;

  switch (args.category) {
    case 'title':
      return args.pageName ? buildTitleMutations(paths, selectedPage, args.pageName) : [];
    case 'alignment':
      return args.pageName ? buildLayoutMutations(paths, selectedPage, args.pageName) : [];
    case 'navigation':
      return buildNavigationMutations(paths, pages, args.affectedPages);
    case 'crossPageConsistency':
      return buildCrossPageTitleMutations(paths, pages, args.affectedPages);
    case 'semanticColor':
      return buildSemanticColorMutations(paths, pages, args.affectedPages);
    default:
      return [];
  }
}
