import type { RepositorySnapshot } from '../../project/repoSnapshot';
import { listSnapshotFiles, readSnapshotText, withRepositorySnapshot } from './repoEvidence';
import type { TypeScriptEvidenceReport } from './reviewTypes';

const LAYOUT_TERMS = ['dashboardlayout', 'layout', 'grid', 'stack', 'section'];
const KPI_TERMS = ['kpicard', 'kpi', 'metric', 'summarycard'];
const COMPOSITION_TERMS = ['herosection', 'trendchart', 'detailgrid', 'overview', 'detail'];
const DISPLAY_NAME_BY_TERM: Record<string, string> = {
  dashboardlayout: 'DashboardLayout',
  layout: 'layout',
  grid: 'grid',
  stack: 'stack',
  section: 'section',
  kpicard: 'KPI card',
  kpi: 'KPI',
  metric: 'metric',
  summarycard: 'SummaryCard',
  herosection: 'HeroSection',
  trendchart: 'TrendChart',
  detailgrid: 'DetailGrid',
  overview: 'overview',
  detail: 'detail',
};

function matchesAny(text: string, terms: string[]): string[] {
  return terms.filter((term) => text.includes(term));
}

async function extractTypeScriptEvidenceFromSnapshot(
  snapshot: RepositorySnapshot,
): Promise<TypeScriptEvidenceReport> {
  const files = listSnapshotFiles(snapshot, (file) => /\.(ts|tsx)$/i.test(file.relativePath));
  const report: TypeScriptEvidenceReport = {
    layoutPatterns: [],
    kpiPatterns: [],
    compositionSignals: [],
  };

  for (const file of files) {
    const content = (await readSnapshotText(snapshot, file)).toLowerCase();
    const relativePath = file.relativePath;

    const layoutMatches = matchesAny(content, LAYOUT_TERMS);
    if (layoutMatches.length > 0) {
      report.layoutPatterns.push({
        filePath: relativePath,
        summary: `Layout structure references ${layoutMatches.map((match) => DISPLAY_NAME_BY_TERM[match] ?? match).join(', ')}.`,
      });
    }

    const kpiMatches = matchesAny(content, KPI_TERMS);
    if (kpiMatches.length > 0) {
      report.kpiPatterns.push({
        filePath: relativePath,
        summary: `KPI structure references ${kpiMatches.map((match) => DISPLAY_NAME_BY_TERM[match] ?? match).join(', ')}.`,
      });
    }

    const compositionMatches = matchesAny(content, COMPOSITION_TERMS);
    if (compositionMatches.length > 0) {
      report.compositionSignals.push({
        filePath: relativePath,
        summary: `Composition flow references ${compositionMatches.map((match) => DISPLAY_NAME_BY_TERM[match] ?? match).join(', ')}.`,
      });
    }
  }

  return report;
}

export async function extractTypeScriptEvidence(
  source: RepositorySnapshot | string,
): Promise<TypeScriptEvidenceReport> {
  return withRepositorySnapshot(source, extractTypeScriptEvidenceFromSnapshot);
}
