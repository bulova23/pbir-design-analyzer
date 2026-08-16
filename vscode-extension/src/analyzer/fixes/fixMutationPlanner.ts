import * as fs from 'fs';
import * as path from 'path';
import type {
  FixMutation,
  FixOpportunityCategory,
  PageVisualMetadataSummary,
  ScoreResult,
  VisualMetadataItem,
} from '../contracts/scorePanel';
import { captureFixFileVersionSync } from './fixPersistenceService';

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
  pageId?: string;
  pageName: string;
  visualMetadata?: PageVisualMetadataSummary;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function readJsonFile(filePath: string): Record<string, unknown> {
  return JSON.parse(fs.readFileSync(filePath, 'utf8')) as Record<string, unknown>;
}

function getTargetFileVersion(filePath: string) {
  return captureFixFileVersionSync(filePath);
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
      pageId: page.pageId,
      pageName: page.pageName,
      visualMetadata: page.visualMetadata,
    }));
  }

  if (result.scoredPageName && result.visualMetadata) {
    return [{
      pageId: result.scoredPageId,
      pageName: result.scoredPageName,
      visualMetadata: result.visualMetadata,
    }];
  }

  return [];
}

function selectPlanningPage(pages: PlanningPage[], pageName: string): PlanningPage | undefined {
  const matches = pages.filter((page) => page.pageName === pageName);
  return matches.length === 1 ? matches[0] : undefined;
}

function findPage(paths: ReportDefinitionPaths, planningPage: PlanningPage | undefined) {
  if (!planningPage) {
    return undefined;
  }

  if (planningPage.pageId) {
    return paths.pages.find((page) => page.pageId === planningPage.pageId);
  }

  const matches = paths.pages.filter((page) => page.displayName === planningPage.pageName);
  return matches.length === 1 ? matches[0] : undefined;
}

function isSupportedMutationCategory(category: FixOpportunityCategory): boolean {
  switch (category) {
    case 'semanticColor':
      return false;
    default:
      return true;
  }
}

function getValueAtPath(source: unknown, storagePath: Array<string | number>): unknown {
  return storagePath.reduce<unknown>((current, segment) => {
    if (typeof segment === 'number') {
      return Array.isArray(current) ? current[segment] : undefined;
    }

    return isRecord(current) ? current[segment] : undefined;
  }, source);
}

function decodePbirStringLiteral(value: unknown): string | undefined {
  if (typeof value !== 'string') {
    return undefined;
  }

  if (value.length >= 2 && value.startsWith('\'') && value.endsWith('\'')) {
    return value.slice(1, -1).replace(/''/g, '\'');
  }

  return value;
}

function resolveTitleStoragePath(visualJson: Record<string, unknown>): { storagePath: Array<string | number>; before: string; storageValueFormat: 'plain' | 'pbirStringLiteral' } | undefined {
  const pbirPath: Array<string | number> = ['visual', 'visualContainerObjects', 'title', 0, 'properties', 'text', 'expr', 'Literal', 'Value'];
  const pbirValue = getValueAtPath(visualJson, pbirPath);
  const decodedPbirValue = decodePbirStringLiteral(pbirValue);
  if (decodedPbirValue !== undefined) {
    return {
      storagePath: pbirPath,
      before: decodedPbirValue,
      storageValueFormat: 'pbirStringLiteral',
    };
  }

  const legacyPath: Array<string | number> = ['title', 'text'];
  const legacyValue = getValueAtPath(visualJson, legacyPath);
  if (typeof legacyValue === 'string') {
    return {
      storagePath: legacyPath,
      before: legacyValue,
      storageValueFormat: 'plain',
    };
  }

  return undefined;
}

function findTitleVisual(page: PlanningPage | undefined): VisualMetadataItem | undefined {
  return page?.visualMetadata?.visuals.find((visual) => visual.hasVisibleTitleIntent)
    ?? page?.visualMetadata?.visuals.find((visual) => visual.visualType.toLowerCase().includes('text'));
}

function buildTitleMutations(
  paths: ReportDefinitionPaths,
  page: PlanningPage | undefined,
): FixMutation[] {
  const titleVisual = findTitleVisual(page);
  const pageDef = findPage(paths, page);
  if (!titleVisual || !pageDef) {
    return [];
  }

  const targetFile = pageDef.visualFiles.get(titleVisual.visualId);
  if (!targetFile) {
    return [];
  }

  const titleTarget = resolveTitleStoragePath(readJsonFile(targetFile));
  if (!titleTarget || !page) {
    return [];
  }

  const mutations: FixMutation[] = [];
  const targetFileVersion = getTargetFileVersion(targetFile);
  if (titleVisual.y !== 24) {
    mutations.push({
      id: `${titleVisual.visualId}-position-y`,
      pageName: page.pageName,
      targetObjectId: titleVisual.visualId,
      targetFile,
      targetFileVersion,
      propertyPath: 'position.y',
      storagePath: ['position', 'y'],
      storageValueFormat: 'plain',
      mutationType: 'setPosition',
      before: titleVisual.y,
      after: 24,
    });
  }

  if (titleVisual.x !== 24) {
    mutations.push({
      id: `${titleVisual.visualId}-position-x`,
      pageName: page.pageName,
      targetObjectId: titleVisual.visualId,
      targetFile,
      targetFileVersion,
      propertyPath: 'position.x',
      storagePath: ['position', 'x'],
      storageValueFormat: 'plain',
      mutationType: 'setPosition',
      before: titleVisual.x,
      after: 24,
    });
  }

  const desiredTitle = page.pageName;
  if (titleTarget.before !== desiredTitle) {
    mutations.push({
      id: `${titleVisual.visualId}-title-text`,
      pageName: page.pageName,
      targetObjectId: titleVisual.visualId,
      targetFile,
      targetFileVersion,
      propertyPath: 'title.text',
      storagePath: titleTarget.storagePath,
      storageValueFormat: titleTarget.storageValueFormat,
      mutationType: 'setTitleText',
      before: titleTarget.before,
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
  const pageDef = findPage(paths, page);
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
      const targetFileVersion = getTargetFileVersion(targetFile);
      if (snappedX !== visual.x) {
        mutations.push({
          id: `${visual.visualId}-layout-x`,
          pageName,
          targetObjectId: visual.visualId,
          targetFile,
          targetFileVersion,
          propertyPath: 'position.x',
          storagePath: ['position', 'x'],
          storageValueFormat: 'plain',
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
          targetFileVersion,
          propertyPath: 'position.y',
          storagePath: ['position', 'y'],
          storageValueFormat: 'plain',
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
    const pageDef = findPage(paths, pages.find((page) => page.pageName === pageName));
    const targetFile = pageDef?.visualFiles.get(visual.visualId);
    if (!targetFile) {
      return [];
    }

    const mutations: FixMutation[] = [];
    const targetFileVersion = getTargetFileVersion(targetFile);
    if (visual.x !== anchorX) {
      mutations.push({
        id: `${visual.visualId}-nav-x`,
        pageName,
        targetObjectId: visual.visualId,
        targetFile,
        targetFileVersion,
        propertyPath: 'position.x',
        storagePath: ['position', 'x'],
        storageValueFormat: 'plain',
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
        targetFileVersion,
        propertyPath: 'position.y',
        storagePath: ['position', 'y'],
        storageValueFormat: 'plain',
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
    .map((page) => ({ page, visual: findTitleVisual(page) }))
    .filter((entry): entry is { page: PlanningPage; visual: VisualMetadataItem } => Boolean(entry.visual));

  if (titleVisuals.length < 2) {
    return [];
  }

  const anchorX = Math.min(...titleVisuals.map((entry) => entry.visual.x));
  const anchorY = Math.min(...titleVisuals.map((entry) => entry.visual.y));
  return titleVisuals.flatMap(({ page, visual }) => {
    const pageDef = findPage(paths, page);
    const targetFile = pageDef?.visualFiles.get(visual.visualId);
    if (!targetFile) {
      return [];
    }
    const mutations: FixMutation[] = [];
    const targetFileVersion = getTargetFileVersion(targetFile);
    if (visual.x !== anchorX) {
      mutations.push({
        id: `${visual.visualId}-cross-title-x`,
        pageName: page.pageName,
        targetObjectId: visual.visualId,
        targetFile,
        targetFileVersion,
        propertyPath: 'position.x',
        storagePath: ['position', 'x'],
        storageValueFormat: 'plain',
        mutationType: 'setPosition',
        before: visual.x,
        after: anchorX,
      });
    }
    if (visual.y !== anchorY) {
      mutations.push({
        id: `${visual.visualId}-cross-title-y`,
        pageName: page.pageName,
        targetObjectId: visual.visualId,
        targetFile,
        targetFileVersion,
        propertyPath: 'position.y',
        storagePath: ['position', 'y'],
        storageValueFormat: 'plain',
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

    const pageDef = findPage(paths, pages.find((page) => page.pageName === assignment.sourcePageName));
    const targetFile = pageDef?.visualFiles.get(assignment.sourceVisualId);
    if (!targetFile) {
      return [];
    }

    return [{
      id: `${assignment.sourceVisualId}-semantic-color`,
      pageName: assignment.sourcePageName,
      targetObjectId: assignment.sourceVisualId,
      targetFile,
      targetFileVersion: getTargetFileVersion(targetFile),
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
  const selectedPage = args.pageName ? selectPlanningPage(pages, args.pageName) : undefined;

  switch (args.category) {
    case 'title':
      return args.pageName ? buildTitleMutations(paths, selectedPage) : [];
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
