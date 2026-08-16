const assert = require('assert');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { TextEncoder, TextDecoder } = require('util');
const vm = require('vm');
const vscode = require('vscode');

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function waitFor(predicate, message, timeoutMs = 60000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    const result = await predicate();
    if (result) {
      return result;
    }
    await delay(100);
  }

  throw new Error(message);
}

function createPanelHarness() {
  const postedMessages = [];
  let messageHandler;

  const panel = {
    webview: {
      html: '',
      cspSource: 'vscode-webview://phase2-smoke',
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
  };

  return {
    panel,
    get postedMessages() {
      return postedMessages;
    },
    get messageHandler() {
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

function installPanelStub(harness) {
  const original = vscode.window.createWebviewPanel;
  vscode.window.createWebviewPanel = () => {
    harness.postedMessages.push({ type: '__panelCreated' });
    return harness.panel;
  };
  return () => {
    vscode.window.createWebviewPanel = original;
  };
}

function fixtureVisualFile(baseDir, visualId) {
  return path.join(baseDir, 'definition', 'pages', 'OverviewPage', 'visuals', visualId, 'visual.json');
}

function createDeterministicReportFixture(root) {
  const reportRoot = path.join(root, 'Phase2Smoke.Report');
  const definitionRoot = path.join(reportRoot, 'definition');
  const overviewRoot = path.join(definitionRoot, 'pages', 'OverviewPage');
  const title1 = fixtureVisualFile(reportRoot, 'title-1');
  const title2 = fixtureVisualFile(reportRoot, 'title-2');

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

function buildDeterministicResult(paths) {
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
    normalizedFindings: [{
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
    }],
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

function buildPostApplyResult(paths) {
  const result = buildDeterministicResult(paths);
  result.normalizedFindings = [];
  return result;
}

function buildContext(basePath) {
  const extensionPath = path.join(process.env.REPO_ROOT, 'vscode-extension');
  return {
    extensionPath,
    extensionUri: vscode.Uri.file(extensionPath),
    globalStorageUri: vscode.Uri.file(basePath),
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

async function runPackagedRealFixtureSmoke() {
  const fixturePath = process.env.REAL_FIXTURE_PATH;
  assert.ok(fs.existsSync(fixturePath), `Real fixture not found: ${fixturePath}`);

  const harness = createPanelHarness();
  const restore = installPanelStub(harness);
  const originalError = vscode.window.showErrorMessage;
  const originalWarning = vscode.window.showWarningMessage;
  const uiMessages = [];
  vscode.window.showErrorMessage = async (...args) => {
    uiMessages.push({ level: 'error', args });
    return undefined;
  };
  vscode.window.showWarningMessage = async (...args) => {
    uiMessages.push({ level: 'warning', args });
    return undefined;
  };

  try {
    const extension = vscode.extensions.getExtension('bcrowell.pbir-design-analyzer');
    assert.ok(extension, 'Packaged extension was not loaded');
    await extension.activate();
    const commands = await vscode.commands.getCommands(true);
    assert.ok(commands.includes('pbir.scoreReport') || commands.includes('pbirAnalyzer.scoreReport'), 'Score command was not registered');

    await vscode.commands.executeCommand(commands.includes('pbir.scoreReport') ? 'pbir.scoreReport' : 'pbirAnalyzer.scoreReport', fixturePath);
    await waitFor(
      () => harness.messageHandler || harness.latest('__panelCreated') || uiMessages.length > 0,
      `Packaged score command did not create a panel. UI messages: ${JSON.stringify(uiMessages)}`,
    );
    assert.ok(harness.messageHandler, `Packaged score panel did not register a webview handler. UI messages: ${JSON.stringify(uiMessages)}`);
    await harness.messageHandler({ type: 'webviewReady' });

    const fullReportState = await waitFor(() => harness.latest('scoreState'), 'Full-report score state was not posted');
    assert.equal(fullReportState.state.result.reportPath, fixturePath);
    assert.ok(Array.isArray(fullReportState.state.result.fixPlan), 'Fix Plan was not present in the packaged score state.');
    assert.ok(fullReportState.state.result.fixPlan.length > 0, 'Fix Plan did not contain any remediation items.');
    assert.ok(Array.isArray(fullReportState.state.result.fixOpportunities));
    assert.ok(Array.isArray(fullReportState.state.result.proposalEnrichments), 'Proposal enrichments were not present in the packaged score state.');
    assert.ok(fullReportState.state.result.proposalEnrichments.length > 0, 'Fallback advisory proposal enrichment did not appear in the packaged score state.');
    assert.ok(
      fullReportState.state.result.proposalEnrichments.every((item) => item.source === 'fallback' && item.provenance?.usedFallback === true && !item.provenance?.providerName),
      'Provider-backed enrichment was unexpectedly enabled by default in the packaged score state.',
    );

    harness.clear();
    await vscode.commands.executeCommand('pbir.scoreReport', fixturePath, 'Net Sales');
    const pageReportState = await waitFor(() => harness.latest('scoreState'), 'Single-page score state was not posted');
    assert.equal(pageReportState.state.result.scoredPageName, 'Net Sales');
    assert.deepEqual(uiMessages, [], `Unexpected packaged smoke UI errors/warnings were surfaced: ${JSON.stringify(uiMessages)}`);

    return {
      fullReportOpportunityCount: fullReportState.state.result.fixOpportunities.length,
      netSalesOpportunityCount: (pageReportState.state.result.fixOpportunities ?? []).length,
      fixPlanCount: fullReportState.state.result.fixPlan.length,
      proposalEnrichmentCount: fullReportState.state.result.proposalEnrichments.length,
    };
  } finally {
    vscode.window.showErrorMessage = originalError;
    vscode.window.showWarningMessage = originalWarning;
    restore();
  }
}

async function runDeterministicGroupedWorkflowSmoke() {
  const repoRoot = process.env.REPO_ROOT;
  const bundlePath = path.join(repoRoot, 'vscode-extension', 'dist', 'extension.js');
  const bundleCode = fs.readFileSync(bundlePath, 'utf8')
    + '\nmodule.exports.__phase2Smoke = { PbirScorePanel };';
  const bundleModule = { exports: {} };
  const sandbox = {
    module: bundleModule,
    exports: bundleModule.exports,
    require,
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
  const { PbirScorePanel } = bundleModule.exports.__phase2Smoke;
  const fixtureRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-phase2-det-'));
  const storageRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-phase2-storage-'));
  const paths = createDeterministicReportFixture(fixtureRoot);
  const originalResult = buildDeterministicResult(paths);
  const postApplyResult = buildPostApplyResult(paths);
  const harness = createPanelHarness();
  const restore = installPanelStub(harness);

  try {
    const context = buildContext(storageRoot);
    const panel = await PbirScorePanel.createOrShow(context, {
      executeRequest: async () => ({ success: true, data: { reportPath: paths.reportRoot, scoredAt: '2026-06-01T23:00:00.000Z' } }),
    }, paths.reportRoot);

    await waitFor(() => harness.messageHandler, 'Deterministic panel did not register a webview handler');
    await harness.messageHandler({ type: 'webviewReady' });

    panel.currentResult = originalResult;
    panel.savedConfig = buildConfig();
    let refreshCount = 0;
    panel.refresh = async function refreshOverride() {
      refreshCount += 1;
      this.currentResult = refreshCount === 1 ? postApplyResult : originalResult;
    };
    harness.clear();
    await panel.postCurrentScoreState();

    await harness.messageHandler({ type: 'toggleFixOpportunitySelection', opportunityId: 'fix-story:title:Overview' });
    await harness.messageHandler({ type: 'toggleFixOpportunitySelection', opportunityId: 'fix-layout:alignment:Overview' });
    await harness.messageHandler({ type: 'previewSelectedFixOpportunities' });

    const previewState = await waitFor(() => harness.latest('scoreState'), 'Grouped preview state was not posted');
    assert.deepEqual(previewState.state.fixSelection.groupedPreview.opportunityIds.sort(), [
      'fix-layout:alignment:Overview',
      'fix-story:title:Overview',
    ]);

    await harness.messageHandler({ type: 'approveSelectedFixOpportunities' });
    await harness.messageHandler({ type: 'applySelectedFixOpportunities' });

    const appliedTitle = JSON.parse(fs.readFileSync(paths.title1, 'utf8'));
    const appliedSupport = JSON.parse(fs.readFileSync(paths.title2, 'utf8'));
    assert.equal(appliedTitle.title.text, 'Overview');
    assert.equal(appliedSupport.position.y, 96);

    const appliedState = await waitFor(() => harness.latest('scoreState'), 'Applied score state was not posted');
    const session = appliedState.state.fixApplySessions[0];
    assert.ok(session, 'Grouped apply session was not recorded');
    assert.equal(session.rollbackAvailable, true);
    assert.ok(session.groupedOutcomeSummary);

    await harness.messageHandler({ type: 'rollbackFixSession', sessionId: session.id });

    const rolledBackTitle = JSON.parse(fs.readFileSync(paths.title1, 'utf8'));
    const rolledBackSupport = JSON.parse(fs.readFileSync(paths.title2, 'utf8'));
    assert.equal(rolledBackTitle.title.text, 'Executive Overview');
    assert.equal(rolledBackSupport.position.y, 120);

    const rolledBackState = await waitFor(() => harness.latest('scoreState'), 'Rolled-back score state was not posted');
    assert.equal(rolledBackState.state.fixApplySessions[0].rollbackHistory.at(-1).state, 'RolledBack');

    return {
      groupedPreviewIds: previewState.state.fixSelection.groupedPreview.opportunityIds,
      sessionId: session.id,
    };
  } finally {
    restore();
  }
}

async function run() {
  const mode = process.env.SMOKE_MODE ?? 'deterministic';
  if (mode !== 'deterministic' && mode !== 'packaged') {
    throw new Error(`Unsupported smoke mode '${mode}'.`);
  }

  const packaged = mode === 'packaged'
    ? await runPackagedRealFixtureSmoke()
    : undefined;
  const deterministic = await runDeterministicGroupedWorkflowSmoke();

  console.log(JSON.stringify({
    packaged,
    deterministic,
  }, null, 2));
}

module.exports = { run };
