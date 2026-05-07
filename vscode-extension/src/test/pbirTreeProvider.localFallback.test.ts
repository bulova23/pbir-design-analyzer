import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import { PbirTreeProvider } from '../providers/PbirTreeProvider';

describe('PbirTreeProvider local fallback', () => {
  let tempDir: string;
  let projectRoot: string;
  let pbipPath: string;

  beforeEach(() => {
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-tree-provider-'));
    projectRoot = path.join(tempDir, 'PBITesting');
    pbipPath = path.join(projectRoot, 'Sales & Production.pbip');

    fs.mkdirSync(projectRoot, { recursive: true });
    fs.writeFileSync(pbipPath, '{}');

    const reportRoot = path.join(projectRoot, 'Sales & Production.Report');
    const definitionRoot = path.join(reportRoot, 'definition');
    const pageRoot = path.join(definitionRoot, 'pages', 'OverviewPage');
    const visualsRoot = path.join(pageRoot, 'visuals', 'SalesByRegion');

    fs.mkdirSync(visualsRoot, { recursive: true });
    fs.mkdirSync(path.join(definitionRoot, 'themes'), { recursive: true });

    fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
    fs.writeFileSync(
      path.join(definitionRoot, 'report.json'),
      JSON.stringify({
        name: 'Sales & Production',
        theme: {
          name: 'Corporate Theme',
          href: 'themes/corporate.json',
        },
      }),
    );
    fs.writeFileSync(
      path.join(definitionRoot, 'pages', 'pages.json'),
      JSON.stringify({
        pageOrder: ['OverviewPage'],
      }),
    );
    fs.writeFileSync(
      path.join(pageRoot, 'page.json'),
      JSON.stringify({
        name: 'OverviewPage',
        displayName: 'Overview',
      }),
    );
    fs.writeFileSync(
      path.join(visualsRoot, 'visual.json'),
      JSON.stringify({
        name: 'Sales by Region',
        visual: {
          visualType: 'barChart',
        },
      }),
    );
    fs.writeFileSync(path.join(definitionRoot, 'themes', 'corporate.json'), '{}');

    (vscode.workspace as unknown as { workspaceFolders: Array<{ uri: { fsPath: string } }> }).workspaceFolders = [];
    (vscode.workspace.findFiles as jest.Mock).mockResolvedValue([]);
  });

  afterEach(() => {
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  it('builds a tree from local PBIP files when the backend is unavailable', async () => {
    const provider = new PbirTreeProvider();
    provider.setProjectPath(pbipPath);

    const rootItems = await provider.getChildren();
    expect(rootItems).toHaveLength(1);
    expect(rootItems[0].label).toBe('Sales & Production');

    const reportChildren = await provider.getChildren(rootItems[0]);
    expect(reportChildren.map((item) => item.label)).toEqual(['Corporate Theme', 'Overview']);

    const pageChildren = await provider.getChildren(reportChildren[1]);
    expect(pageChildren.map((item) => item.label)).toEqual(['Sales by Region (barChart)']);
  });

  it('auto-detects a PBIR report from the active workspace when no project is selected yet', async () => {
    (vscode.workspace as unknown as { workspaceFolders: Array<{ uri: { fsPath: string } }> }).workspaceFolders = [
      { uri: { fsPath: projectRoot } },
    ];

    const provider = new PbirTreeProvider();
    const rootItems = await provider.getChildren();

    expect(rootItems).toHaveLength(1);
    expect(rootItems[0].label).toBe('Sales & Production');
  });
});
