import fs from 'fs';
import os from 'os';
import path from 'path';
import vm from 'vm';
import { createRequire } from 'module';
import { TextEncoder, TextDecoder } from 'util';

const nodeRequire = createRequire(import.meta.url);

function createPanelHarness() {
  const postedMessages = [];
  let messageHandler;

  return {
    panel: {
      webview: {
        html: '',
        cspSource: 'vscode-webview://phase2-bundle-smoke',
        asWebviewUri: (uri) => uri,
        postMessage: async (message) => {
          postedMessages.push(message);
          return true;
        },
        onDidReceiveMessage: (handler) => {
          messageHandler = handler;
          return { dispose() {} };
        },
      },
      onDidDispose: () => ({ dispose() {} }),
      reveal() {},
      dispose() {},
    },
    postedMessages,
    getMessageHandler() {
      return messageHandler;
    },
    latest(type) {
      for (let index = postedMessages.length - 1; index >= 0; index -= 1) {
        if (postedMessages[index]?.type === type) {
          return postedMessages[index];
        }
      }
      return undefined;
    },
    clear() {
      postedMessages.length = 0;
    },
  };
}

function wait(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function waitFor(predicate, message, timeoutMs = 10000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    const result = await predicate();
    if (result) {
      return result;
    }
    await wait(50);
  }
  throw new Error(message);
}

function createDeterministicReportFixture(root) {
  const reportRoot = path.join(root, 'Phase2Smoke.Report');
  const definitionRoot = path.join(reportRoot, 'definition');
  const overviewRoot = path.join(definitionRoot, 'pages', 'OverviewPage');
  const title1 = path.join(overviewRoot, 'visuals', 'title-1', 'visual.json');
  const title2 = path.join(overviewRoot, 'visuals', 'title-2', 'visual.json');

  fs.mkdirSync(path.dirname(title1), { recursive: true });
  fs.mkdirSync(path.dirname(title2), { recursive: true });
  fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
  fs.writeFileSync(path.join(definitionRoot, 'report.json'), JSON.stringify({ name: 'Phase 2 Smoke' }));
  fs.writeFileSync(path.join(definitionRoot, 'pages', 'pages.json'), JSON.stringify({ pageOrder: ['OverviewPage'] }));
  fs.writeFileSync(path.join(overviewRoot, 'page.json'), JSON.stringify({ name: 'OverviewPage', displayName: 'Overview' }));
  fs.writeFileSync(title1, JSON.stringify({
    name: 'title-1',
    position: { x: 42, y: 24, width: 400, height: 48 },
    title: { text: 'Executive Overview' },
    visual: { visualType: 'textbox' },
  }, null, 2));
  fs.writeFileSync(title2, JSON.stringify({
    name: 'title-2',
    position: { x: 80, y: 120, width: 400, height: 48 },
    title: { text: 'Overview Support' },
    visual: { visualType: 'textbox' },
  }, null, 2));

  return { reportRoot, title1, title2 };
}

function buildConfig() {
  return {
    frameworks: [
      { id: 'gestalt', name: 'Gestalt Principles', enabled: true, weight: 60 },
      { id: 'cognitive', name: 'Cognitive Load', enabled: true, weight: 40 },
    ],
    navigationScoring: {
      enabled: true,
      weight: 25,
    },
    governance: [],
  };
}

function buildResult(paths, includeFindings = true) {
  return {
    gestaltScore: 77,
    cognitiveLoadScore: 72,
    dataInkScore: 70,
    accessibilityScore: 70,
    visualBestPracticesScore: 78,
    stephenFewScore: 66,
    enterpriseGovernanceScore: 74,
    tufteScore: 68,
    graphicalPerceptionScore: 70,
    densityScore: 64,
    narrativeScore: 69,
    compositeScore: 77,
    feedback: {},
    pageCount: 1,
    recommendations: ['[High] Layout: Snap visuals to grid'],
    reportPath: paths.reportRoot,
    scoredAt: '2026-06-01T23:00:00.000Z',
    normalizedFindings: includeFindings ? [{
      id: 'story-finding',
      title: 'Story clarity issue',
      summary: 'Title framing needs cleanup.',
      severity: 'high',
      confidence: 88,
      scope: 'page',
      detectionType: 'deterministic',
      affectedPages: ['Overview'],
      impactArea: 'storytelling',
      frameworkImpact: ['Narrative Design'],
      recommendation: 'Normalize the title anchor.',
      sourceKind: 'framework',
      sourceSection: 'issues',
      evidence: [],
    }, {
      id: 'layout-finding',
      title: 'Layout density issue',
      summary: 'The scan path is crowded.',
      severity: 'high',
      confidence: 84,
      scope: 'page',
      detectionType: 'deterministic',
      affectedPages: ['Overview'],
      impactArea: 'layout',
      frameworkImpact: ['Gestalt Principles'],
      recommendation: 'Align the support title and reduce visual density.',
      sourceKind: 'framework',
      sourceSection: 'issues',
      evidence: [],
    }] : [],
    fixPlan: [{
      id: 'fix-story',
      title: 'Clarify page purpose and narrative framing',
      detail: 'Resolve title framing.',
      severity: 'high',
      effort: 'low',
      impact: 'high',
      why: 'Improves page purpose clarity for executive readers.',
      scope: 'page',
      affectedPages: ['Overview'],
      recommendedAction: 'Normalize the title anchor.',
      resolvedOutcomes: ['Story clarity'],
      sourceFindingIds: ['story-finding'],
    }, {
      id: 'fix-layout',
      title: 'Reduce visual density and align layout',
      detail: 'Resolve layout density.',
      severity: 'high',
      effort: 'low',
      impact: 'high',
      why: 'Improves scanability and reduces cognitive load.',
      scope: 'page',
      affectedPages: ['Overview'],
      recommendedAction: 'Align the support title.',
      resolvedOutcomes: ['Layout consistency'],
      sourceFindingIds: ['layout-finding'],
    }],
    fixOpportunities: [{
      id: 'fix-story:title:Overview',
      remediationItemId: 'fix-story',
      title: 'Clarify page purpose and narrative framing (title)',
      category: 'title',
      summary: 'Normalize the primary title anchor.',
      confidence: 95,
      safetyClass: 'safe',
      affectedPages: ['Overview'],
      targetObjectIds: ['title-1'],
      sourceFindingIds: ['story-finding'],
      expectedResolutions: ['Story clarity'],
      mutations: [{
        id: 'mutation-title-1',
        pageName: 'Overview',
        targetObjectId: 'title-1',
        targetFile: paths.title1,
        propertyPath: 'title.text',
        mutationType: 'setTitleText',
        before: 'Executive Overview',
        after: 'Overview',
      }],
      previewRows: [{
        pageName: 'Overview',
        objectId: 'title-1',
        property: 'title.text',
        before: 'Executive Overview',
        after: 'Overview',
      }],
      rollbackPlan: {
        id: 'rollback-fix-story:title:Overview',
        fixOpportunityId: 'fix-story:title:Overview',
        fileBackups: [{
          targetFile: paths.title1,
          beforeContent: fs.readFileSync(paths.title1, 'utf8'),
        }],
        reverseMutations: [],
      },
      state: 'Previewed',
    }, {
      id: 'fix-layout:alignment:Overview',
      remediationItemId: 'fix-layout',
      title: 'Reduce visual density and align layout (alignment)',
      category: 'alignment',
      summary: 'Pull the support title into the shared scan path.',
      confidence: 95,
      safetyClass: 'safe',
      affectedPages: ['Overview'],
      targetObjectIds: ['title-2'],
      sourceFindingIds: ['layout-finding'],
      expectedResolutions: ['Layout consistency'],
      mutations: [{
        id: 'mutation-title-2',
        pageName: 'Overview',
        targetObjectId: 'title-2',
        targetFile: paths.title2,
        propertyPath: 'position.y',
        mutationType: 'setPosition',
        before: 120,
        after: 96,
      }],
      previewRows: [{
        pageName: 'Overview',
        objectId: 'title-2',
        property: 'position.y',
        before: 120,
        after: 96,
      }],
      rollbackPlan: {
        id: 'rollback-fix-layout:alignment:Overview',
        fixOpportunityId: 'fix-layout:alignment:Overview',
        fileBackups: [{
          targetFile: paths.title2,
          beforeContent: fs.readFileSync(paths.title2, 'utf8'),
        }],
        reverseMutations: [],
      },
      state: 'Previewed',
    }],
    pageScores: [{
      pageName: 'Overview',
      gestaltScore: 77,
      cognitiveLoadScore: 72,
      dataInkScore: 70,
      accessibilityScore: 70,
      visualBestPracticesScore: 78,
      stephenFewScore: 66,
      enterpriseGovernanceScore: 74,
      tufteScore: 68,
      graphicalPerceptionScore: 70,
      densityScore: 64,
      narrativeScore: 69,
      compositeScore: 77,
      feedback: {},
      recommendations: [],
      visualMetadata: {
        pageName: 'Overview',
        visiblePageTitle: 'Executive Overview',
        strictVisiblePageTitle: 'Executive Overview',
        canvasWidth: 1280,
        canvasHeight: 720,
        semanticColorMap: [],
        chartIntentSummary: undefined,
        visualCount: 2,
        visibleTitleVisualCount: 2,
        textVisualCount: 2,
        slicerCount: 0,
        legendVisualCount: 0,
        axisLabelVisualCount: 0,
        dataLabelVisualCount: 0,
        formattedVisualCount: 2,
        visuals: [],
      },
    }],
  };
}

function createVscodeMock(harness) {
  return {
    window: {
      createOutputChannel: () => ({ appendLine() {}, show() {}, dispose() {} }),
      createWebviewPanel: () => harness.panel,
      showWarningMessage: async () => undefined,
      showErrorMessage: async () => undefined,
      showOpenDialog: async () => undefined,
      showSaveDialog: async () => undefined,
      createStatusBarItem: () => ({ show() {}, hide() {}, dispose() {}, text: '', tooltip: '' }),
      createTreeView: () => ({ dispose() {} }),
    },
    commands: {
      executeCommand: async () => undefined,
    },
    workspace: {
      workspaceFolders: [],
      findFiles: async () => [],
      getConfiguration: () => ({
        get: (_key, defaultValue) => defaultValue,
        update: async () => undefined,
      }),
    },
    Uri: {
      file: (fsPath) => ({ fsPath, path: fsPath, toString: () => fsPath }),
      joinPath: (base, ...parts) => {
        const fsPath = path.join(base.fsPath ?? base.path ?? '', ...parts);
        return { fsPath, path: fsPath, toString: () => fsPath };
      },
    },
    ViewColumn: {
      Beside: -2,
    },
    TreeItem: class {
      constructor(label, collapsibleState) {
        this.label = label;
        this.collapsibleState = collapsibleState;
      }
    },
    TreeItemCollapsibleState: {
      None: 0,
      Collapsed: 1,
      Expanded: 2,
    },
    ThemeIcon: class {
      constructor(id) {
        this.id = id;
      }
    },
    CompletionItem: class {
      constructor(label, kind) {
        this.label = label;
        this.kind = kind;
      }
    },
    CompletionItemKind: {
      Text: 1,
      Method: 2,
      Function: 3,
      Constructor: 4,
      Field: 5,
      Variable: 6,
      Class: 7,
      Interface: 8,
      Module: 9,
      Property: 10,
      Unit: 11,
      Value: 12,
      Enum: 13,
      Keyword: 14,
      Snippet: 15,
      Color: 16,
      File: 17,
      Reference: 18,
      Folder: 19,
      EnumMember: 20,
      Constant: 21,
      Struct: 22,
      Event: 23,
      Operator: 24,
      TypeParameter: 25,
    },
    StatusBarAlignment: {
      Left: 1,
    },
    EventEmitter: class {
      constructor() {
        this.listeners = [];
      }
      event(listener) {
        this.listeners.push(listener);
        return { dispose() {} };
      }
      fire(value) {
        for (const listener of this.listeners) {
          listener(value);
        }
      }
      dispose() {}
    },
  };
}

function loadPackagedPanel(vscodeMock) {
  const bundlePath = path.resolve('vscode-extension/dist/extension.js');
  const bundleCode = fs.readFileSync(bundlePath, 'utf8')
    + '\nmodule.exports.__phase2Smoke = { PbirScorePanel };';
  const module = { exports: {} };
  const sandbox = {
    module,
    exports: module.exports,
    require: (id) => {
      if (id === 'vscode') {
        return vscodeMock;
      }
      if (id === 'vscode-languageclient/node') {
        return { LanguageClient: class {} };
      }
      return nodeRequire(id);
    },
    __dirname: path.dirname(bundlePath),
    __filename: bundlePath,
    process,
    console,
    Buffer,
    TextEncoder,
    TextDecoder,
    setTimeout,
    clearTimeout,
  };
  sandbox.global = sandbox;
  sandbox.globalThis = sandbox;

  vm.runInNewContext(bundleCode, sandbox, { filename: bundlePath });
  return module.exports.__phase2Smoke.PbirScorePanel;
}

function buildContext(root) {
  const extensionPath = path.resolve('vscode-extension');
  return {
    extensionPath,
    extensionUri: {
      fsPath: extensionPath,
      path: extensionPath,
      toString: () => extensionPath,
    },
    globalStorageUri: {
      fsPath: root,
      path: root,
      toString: () => root,
    },
    subscriptions: [],
    secrets: {
      get: async () => undefined,
      store: async () => undefined,
      delete: async () => undefined,
      onDidChange: () => ({ dispose() {} }),
    },
    workspaceState: {
      get: () => undefined,
      update: async () => undefined,
    },
    globalState: {
      get: () => undefined,
      update: async () => undefined,
    },
  };
}

const harness = createPanelHarness();
const vscodeMock = createVscodeMock(harness);
const PbirScorePanel = loadPackagedPanel(vscodeMock);
const reportRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-phase2-bundle-'));
const storageRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-phase2-bundle-storage-'));
const paths = createDeterministicReportFixture(reportRoot);
const initialResult = buildResult(paths, true);
const postApplyResult = buildResult(paths, false);
const context = buildContext(storageRoot);

const panel = await PbirScorePanel.createOrShow(context, {
  executeRequest: async () => ({ success: true, data: { reportPath: paths.reportRoot, scoredAt: '2026-06-01T23:00:00.000Z' } }),
}, paths.reportRoot);
await waitFor(() => harness.getMessageHandler(), 'Packaged panel did not register a message handler');
await harness.getMessageHandler()({ type: 'webviewReady' });

panel.currentResult = initialResult;
panel.savedConfig = buildConfig();
let refreshCount = 0;
panel.refresh = async function refreshOverride() {
  refreshCount += 1;
  this.currentResult = refreshCount === 1 ? postApplyResult : initialResult;
};
harness.clear();
await panel.postCurrentScoreState();

await harness.getMessageHandler()({ type: 'toggleFixOpportunitySelection', opportunityId: 'fix-story:title:Overview' });
await harness.getMessageHandler()({ type: 'toggleFixOpportunitySelection', opportunityId: 'fix-layout:alignment:Overview' });
await harness.getMessageHandler()({ type: 'previewSelectedFixOpportunities' });

const previewState = await waitFor(() => harness.latest('scoreState'), 'Grouped preview state was not posted');
if (!previewState.state.fixSelection?.groupedPreview || previewState.state.fixSelection.groupedPreview.opportunityIds.length !== 2) {
  throw new Error('Grouped preview did not include both packaged opportunities.');
}

await harness.getMessageHandler()({ type: 'approveSelectedFixOpportunities' });
await harness.getMessageHandler()({ type: 'applySelectedFixOpportunities' });

const appliedTitle = JSON.parse(fs.readFileSync(paths.title1, 'utf8'));
const appliedSupport = JSON.parse(fs.readFileSync(paths.title2, 'utf8'));
if (appliedTitle.title.text !== 'Overview' || appliedSupport.position.y !== 96) {
  throw new Error('Packaged bundle apply did not mutate both selected opportunities.');
}

const appliedState = await waitFor(() => harness.latest('scoreState'), 'Applied state was not posted');
const session = appliedState.state.fixApplySessions?.[0];
if (!session || !session.rollbackAvailable) {
  throw new Error('Packaged bundle apply session was not recorded.');
}

await harness.getMessageHandler()({ type: 'rollbackFixSession', sessionId: session.id });
const rolledBackTitle = JSON.parse(fs.readFileSync(paths.title1, 'utf8'));
const rolledBackSupport = JSON.parse(fs.readFileSync(paths.title2, 'utf8'));
if (rolledBackTitle.title.text !== 'Executive Overview' || rolledBackSupport.position.y !== 120) {
  throw new Error('Packaged bundle rollback did not restore both selected opportunities.');
}

console.log(JSON.stringify({
  groupedPreviewIds: previewState.state.fixSelection.groupedPreview.opportunityIds,
  sessionId: session.id,
}, null, 2));
