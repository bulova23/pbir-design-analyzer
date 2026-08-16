import * as fs from 'fs';
import * as vscode from 'vscode';
import { getExtensionOutputChannel } from '../../platform/outputChannels';
import type { ScoreResult } from '../../analyzer/contracts/scorePanel';
import type { AnalyzerBridgeService } from './AnalyzerBridgeService';

export type PbirAuthoringResponse = {
  succeeded: boolean;
  diagnostics?: Array<{ code: string; field: string; severity: string; summary: string }>;
  error?: { category: string; code: string; summary: string };
  artifactIdentity?: { artifactId: string; artifactHash: string; manifestId: string; manifestHash: string };
  fidelity?: { classification: string; preservedPathCount: number; changedPathCount: number; unexpectedPathCount: number };
  analyzer?: { score: number; pageCount: number; visualCount: number; result?: ScoreResult };
  mutateResult?: {
    artifact?: { schemaVersion: string; artifactId: string; artifactHash: string; manifestId: string; manifestHash: string };
    changedPageCount?: number;
    changedVisualCount?: number;
    preview?: PbirAuthoringMutationPreview;
    comparison?: { before: { score: number; pageCount: number; visualCount: number }; after: { score: number; pageCount: number; visualCount: number }; scoreDelta: number; preservedPageIds?: string[]; preservedVisualIds?: string[] };
  };
  timing?: { dispatchMilliseconds: number; orchestrationMilliseconds: number; serializationMilliseconds: number; analyzerMilliseconds: number };
  generateResult?: { requestVersion: string; artifact?: { schemaVersion: string; artifactId: string; artifactHash: string; manifestId: string; manifestHash: string } };
  importResult?: { snapshot: { schemaVersion: string; snapshotId: string; sourceIdentity: { sourceDirectoryName: string; contentHash: string; fileCount: number } }; pages: Array<{ pageId: string; displayName: string }>; visuals?: Array<{ visualId: string; pageId: string; visualType: string; order: number; layout?: { x: number; y: number; width: number; height: number } }> };
};

export type PbirAuthoringSnapshotHandle = NonNullable<NonNullable<PbirAuthoringResponse['importResult']>['snapshot']>;

export type PbirAuthoringMutationPreview = {
  previewId?: string;
  mutationKind: string;
  targetPageId: string;
  currentDisplayName: string;
  proposedDisplayName: string;
  payload?: {
    kind: string;
    page?: { currentDisplayName?: string; proposedDisplayName?: string; currentPosition?: number; proposedPosition?: number; deterministicPageId?: string; navigationAffectedPageIds?: string[] };
    visual?: { currentPageId?: string; proposedPageId?: string; currentOrder?: number; proposedOrder?: number; currentLayout?: { x: number; y: number; width: number; height: number }; proposedLayout?: { x: number; y: number; width: number; height: number } };
  };
  diffs?: Array<{ kind: string; objectId: string; beforePageId?: string; afterPageId?: string; beforeDisplayName?: string; afterDisplayName?: string; beforeOrder?: number; afterOrder?: number }>;
  affectedPageIds?: string[];
  affectedVisualIds?: string[];
  preservedPageIds?: string[];
  preservedVisualIds?: string[];
  affectedObjectCount?: number;
  diagnostics?: Array<{ code: string; field: string; severity: string; summary: string }>;
  executionAdmissible: boolean;
  isNoOp: boolean;
};

type PbirAuthoringArtifactHandle = NonNullable<NonNullable<PbirAuthoringResponse['generateResult']>['artifact']>;

type AuthoringBridge = Pick<AnalyzerBridgeService, 'executeAuthoringRequest'>;

export function generationVersionForSchema(schemaVersion: unknown): `v${1 | 2 | 3 | 4 | 5 | 6 | 7}` | undefined {
  if (typeof schemaVersion !== 'string') {
    return undefined;
  }

  const match = /^local-pbir-generation-request\/v([1-7])$/.exec(schemaVersion);
  return match ? `v${match[1] as '1' | '2' | '3' | '4' | '5' | '6' | '7'}` : undefined;
}

export function buildGeneratePayload(document: unknown): Record<string, unknown> {
  if (!document || typeof document !== 'object' || Array.isArray(document)) {
    throw new Error('The selected file is not a typed PBIR generation request.');
  }

  const version = generationVersionForSchema((document as { schemaVersion?: unknown }).schemaVersion);
  if (!version) {
    throw new Error('The selected file must use local-pbir-generation-request/v1 through v7.');
  }

  return {
    schemaVersion: 'pbir-authoring-rpc/v1',
    operation: 'generate',
    generate: { request: { [version]: document } },
  };
}

export function buildRenamePagePayload(pageId: string, displayName: string): Record<string, unknown> {
  return {
    schemaVersion: 'pbir-authoring-rpc/v1',
    operation: 'mutate',
    mutate: {
      mode: 'preview',
      request: {
        schemaVersion: 'local-pbir-mutation-request/v1',
        mutationId: `phase47-rename-${Date.now()}`,
        sourceDirectory: '',
        outputBaseDirectory: '',
        targetDirectoryName: '',
        operations: [{ kind: 'renamePage', target: { pageId }, displayName }],
      },
    },
  };
}

export function buildCuratedMutationPayload(kind: string, input: Record<string, unknown>): Record<string, unknown> {
  const { pageId, visualId, ...rest } = input;
  const operation: Record<string, unknown> = { kind };
  if (kind !== 'addPage' && (pageId || visualId)) operation.target = { ...(pageId ? { pageId } : {}), ...(visualId ? { visualId } : {}) };
  if (kind === 'addPage') operation.page = { pageId, ...rest };
  else if (kind === 'renamePage') operation.displayName = rest.displayName;
  else if (kind === 'movePage') operation.order = rest.order;
  else if (kind === 'moveVisual') {
    operation.order = rest.order;
    if (rest.destinationPageId) operation.target = { visualId, pageId: rest.destinationPageId };
  } else if (kind === 'resizeVisual') operation.layout = rest.layout;
  return {
    schemaVersion: 'pbir-authoring-rpc/v1',
    operation: 'mutate',
    mutate: {
      mode: 'preview',
      request: {
        schemaVersion: 'local-pbir-mutation-request/v1',
        mutationId: 'phase48-' + kind + '-' + Date.now(),
        sourceDirectory: '',
        outputBaseDirectory: '',
        targetDirectoryName: '',
        operations: [operation],
      },
    },
  };
}

export function formatMutationPreview(preview: PbirAuthoringMutationPreview): string {
  return `Rename page\n\nCurrent:\n${preview.currentDisplayName}\n\nNew:\n${preview.proposedDisplayName}`;
}

export function formatCuratedMutationPreview(preview: PbirAuthoringMutationPreview): string {
  const lines = [preview.mutationKind];
  const page = preview.payload?.page;
  const visual = preview.payload?.visual;
  if (page?.proposedDisplayName) lines.push('Page: ' + (page.currentDisplayName ?? preview.currentDisplayName) + ' → ' + page.proposedDisplayName);
  if (page?.currentPosition !== undefined || page?.proposedPosition !== undefined) lines.push('Position: ' + (page.currentPosition ?? 'new') + ' → ' + (page.proposedPosition ?? 'new'));
  if (visual?.currentPageId || visual?.proposedPageId) lines.push('Page: ' + (visual.currentPageId ?? 'new') + ' → ' + (visual.proposedPageId ?? 'new'));
  if (visual?.currentOrder !== undefined || visual?.proposedOrder !== undefined) lines.push('Order: ' + (visual.currentOrder ?? 'new') + ' → ' + (visual.proposedOrder ?? 'new'));
  if (visual?.currentLayout || visual?.proposedLayout) lines.push('Layout: ' + JSON.stringify(visual.currentLayout) + ' → ' + JSON.stringify(visual.proposedLayout));
  if (preview.diffs?.length) lines.push('Diffs: ' + preview.diffs.map(diff => diff.kind + ':' + diff.objectId).join(', '));
  return lines.join('\n');
}

export function formatAuthoringError(response: Pick<PbirAuthoringResponse, 'error'>): string {
  const category = response.error?.category ?? 'internalFailure';
  const labels: Record<string, string> = {
    invalidRequest: 'Invalid request',
    unsupportedAuthoring: 'Unsupported PBIR construct',
    importFailed: 'Import failed',
    validationFailed: 'Validation failed',
    analyzerFailed: 'Analyzer failed',
    executionFailed: 'Execution failed',
    mutationConflict: 'Authoring conflict',
    internalFailure: 'Authoring operation failed',
  };
  return `${labels[category] ?? 'Authoring operation failed'}${response.error?.summary ? `: ${response.error.summary}` : '.'}`;
}

export function formatAuthoringResult(operation: string, response: PbirAuthoringResponse): string {
  const lines = [`[Authoring] ${operation}: ${response.succeeded ? 'succeeded' : formatAuthoringError(response)}`];
  if (response.analyzer) {
    lines.push(`Score: ${response.analyzer.score}; pages: ${response.analyzer.pageCount}; visuals: ${response.analyzer.visualCount}`);
  }
  if (response.artifactIdentity) {
    lines.push(`Artifact: ${response.artifactIdentity.artifactId} (${response.artifactIdentity.artifactHash})`);
  }
  if (response.fidelity) {
    lines.push(`Fidelity: ${response.fidelity.classification}; preserved ${response.fidelity.preservedPathCount}, changed ${response.fidelity.changedPathCount}, unexpected ${response.fidelity.unexpectedPathCount}`);
  }
  if (response.mutateResult?.comparison) {
    const comparison = response.mutateResult.comparison;
    lines.push(`Analyzer: before ${comparison.before.score}; after ${comparison.after.score}; delta ${comparison.scoreDelta}`);
    if (comparison.preservedPageIds?.length || comparison.preservedVisualIds?.length) {
      lines.push(`Preserved identities: ${comparison.preservedPageIds?.length ?? 0} pages, ${comparison.preservedVisualIds?.length ?? 0} visuals`);
    }
  }
  if (response.diagnostics?.length) {
    lines.push(`Diagnostics: ${response.diagnostics.map(diagnostic => diagnostic.code).join(', ')}`);
  }
  if (response.timing) {
    lines.push(`Timing: dispatch ${response.timing.dispatchMilliseconds} ms; orchestration ${response.timing.orchestrationMilliseconds} ms; serialization ${response.timing.serializationMilliseconds} ms; analyzer ${response.timing.analyzerMilliseconds} ms`);
  }
  return lines.join('\n');
}

export class PbirAuthoringWorkflow {
  private artifact: PbirAuthoringArtifactHandle | undefined;
  private snapshot: NonNullable<NonNullable<PbirAuthoringResponse['importResult']>['snapshot']> | undefined;
  private pages: Array<{ pageId: string; displayName: string }> = [];
  private visuals: Array<{ visualId: string; pageId: string; visualType: string; order: number; layout?: { x: number; y: number; width: number; height: number } }> = [];
  private readonly outputChannel = getExtensionOutputChannel();

  constructor(private readonly getBridge: () => AuthoringBridge | undefined) {}

  async generate(): Promise<void> {
    const selection = await vscode.window.showOpenDialog({
      title: 'Select typed PBIR generation request',
      openLabel: 'Generate Report',
      canSelectFiles: true,
      canSelectFolders: false,
      canSelectMany: false,
      filters: { 'PBIR Generation Requests': ['json'] },
    });
    const filePath = selection?.[0]?.fsPath;
    if (!filePath) return;

    try {
      const payload = buildGeneratePayload(JSON.parse(fs.readFileSync(filePath, 'utf8')));
      const response = await this.requireBridge().executeAuthoringRequest<PbirAuthoringResponse>(payload);
      if (response.generateResult?.artifact) this.artifact = response.generateResult.artifact;
      this.present('Generate', response);
    } catch (error) {
      this.presentException(error);
    }
  }

  async import(): Promise<void> {
    const selection = await vscode.window.showOpenDialog({
      title: 'Select supported PBIR report folder',
      openLabel: 'Import Report',
      canSelectFiles: false,
      canSelectFolders: true,
      canSelectMany: false,
    });
    const sourceDirectory = selection?.[0]?.fsPath;
    if (!sourceDirectory) return;

    try {
      const response = await this.requireBridge().executeAuthoringRequest<PbirAuthoringResponse>({
        schemaVersion: 'pbir-authoring-rpc/v1',
        operation: 'import',
        import: { sourceDirectory },
      });
      if (response.importResult?.snapshot) {
        this.snapshot = response.importResult.snapshot;
        this.pages = response.importResult.pages ?? [];
        this.visuals = response.importResult.visuals ?? [];
        this.artifact = undefined;
      }
      this.present('Import', response);
    } catch (error) {
      this.presentException(error);
    }
  }

  async analyze(): Promise<void> {
    const analyze: Record<string, unknown> = this.artifact ? { artifact: this.artifact } : this.snapshot ? { snapshot: this.snapshot } : {};
    if (!Object.keys(analyze).length) {
      const selection = await vscode.window.showOpenDialog({
        title: 'Select supported PBIR report folder to analyze',
        openLabel: 'Analyze Report',
        canSelectFiles: false,
        canSelectFolders: true,
        canSelectMany: false,
      });
      const reportDirectory = selection?.[0]?.fsPath;
      if (!reportDirectory) return;
      analyze.reportDirectory = reportDirectory;
    }

    try {
      const response = await this.requireBridge().executeAuthoringRequest<PbirAuthoringResponse>({
        schemaVersion: 'pbir-authoring-rpc/v1',
        operation: 'analyze',
        analyze,
      });
      this.present('Analyze', response);
    } catch (error) {
      this.presentException(error);
    }
  }

  async renamePage(): Promise<void> {
    if (!this.snapshot || this.pages.length === 0) {
      void vscode.window.showWarningMessage('Import a supported PBIR report before renaming a page.');
      return;
    }

    const page = await vscode.window.showQuickPick(
      this.pages.map(item => ({ label: item.displayName, description: item.pageId, pageId: item.pageId })),
      { title: 'Select page to rename', placeHolder: 'Choose an imported page' },
    );
    if (!page) return;

    const displayName = await vscode.window.showInputBox({
      title: 'Rename page',
      prompt: `New display name for ${page.label}`,
      value: page.label,
      validateInput: value => value.trim().length === 0 ? 'Enter a page display name.' : undefined,
    });
    if (displayName === undefined) return;

    const previewPayload = buildRenamePagePayload(page.pageId, displayName);
    const preview = await this.requireBridge().executeAuthoringRequest<PbirAuthoringResponse>({
      ...previewPayload,
      mutate: { ...previewPayload.mutate as Record<string, unknown>, snapshot: this.snapshot },
    });
    if (!preview.succeeded) {
      this.present('Rename Page Preview', preview);
      return;
    }

    const semanticPreview = preview.mutateResult?.preview;
    if (!semanticPreview) {
      this.presentException(new Error('The authoring backend returned no RenamePage preview.'));
      return;
    }
    if (semanticPreview.isNoOp) {
      this.outputChannel.appendLine(`[Authoring] Rename Page: no changes for ${semanticPreview.currentDisplayName}.`);
      void vscode.window.showInformationMessage('The page already has that display name. No mutation was executed.');
      return;
    }
    if (!semanticPreview.executionAdmissible) {
      this.present('Rename Page Preview', preview);
      return;
    }

    const confirmation = await vscode.window.showInformationMessage(formatMutationPreview(semanticPreview), 'Rename Page', 'Cancel');
    if (confirmation !== 'Rename Page') return;

    const execute = await this.requireBridge().executeAuthoringRequest<PbirAuthoringResponse>({
      ...previewPayload,
      mutate: { ...previewPayload.mutate as Record<string, unknown>, mode: 'execute', snapshot: this.snapshot },
    });
    if (execute.mutateResult?.artifact) this.artifact = execute.mutateResult.artifact;
    this.present('Rename Page', execute);
  }

  async mutate(): Promise<void> {
    if (!this.snapshot || this.pages.length === 0) {
      void vscode.window.showWarningMessage('Import a supported PBIR report before applying a curated mutation.');
      return;
    }
    const selected = await vscode.window.showQuickPick<{ label: string; mutationKind: string }>([
      { label: 'Rename page', mutationKind: 'renamePage' },
      { label: 'Add page', mutationKind: 'addPage' },
      { label: 'Remove page', mutationKind: 'removePage' },
      { label: 'Move page', mutationKind: 'movePage' },
      { label: 'Move visual', mutationKind: 'moveVisual' },
      { label: 'Resize visual', mutationKind: 'resizeVisual' },
    ], { title: 'Select curated PBIR mutation', placeHolder: 'Choose one backend-planned mutation' });
    if (!selected) return;

    const input = await this.collectMutationInput(selected.mutationKind);
    if (!input) return;
    await this.previewConfirmExecute(selected.mutationKind, input);
  }

  private async collectMutationInput(kind: string): Promise<Record<string, unknown> | undefined> {
    if (kind === 'addPage') {
      const displayName = await vscode.window.showInputBox({ title: 'New page name', prompt: 'Enter the page display name.' });
      if (!displayName) return undefined;
      const order = await this.readInteger('Page position', 'Enter a zero-based page position.');
      return order === undefined ? undefined : { displayName, order };
    }
    const page = await vscode.window.showQuickPick(this.pages.map(item => ({ label: item.displayName, description: item.pageId, pageId: item.pageId })), { title: 'Select page' });
    if (!page) return undefined;
    if (kind === 'renamePage') {
      const displayName = await vscode.window.showInputBox({ title: 'Rename page', value: page.label, validateInput: value => value.trim() ? undefined : 'Enter a page display name.' });
      return displayName === undefined ? undefined : { pageId: page.pageId, displayName };
    }
    if (kind === 'removePage') return { pageId: page.pageId };
    if (kind === 'movePage') {
      const order = await this.readInteger('Move page', 'Enter a zero-based destination position.');
      return order === undefined ? undefined : { pageId: page.pageId, order };
    }
    const visual = await vscode.window.showQuickPick(this.visuals.map(item => ({ label: item.visualId, description: item.pageId + ' · ' + item.visualType, visualId: item.visualId })), { title: 'Select visual' });
    if (!visual) return undefined;
    if (kind === 'moveVisual') {
      const destination = await vscode.window.showQuickPick(this.pages.map(item => ({ label: item.displayName, description: item.pageId, pageId: item.pageId })), { title: 'Select destination page' });
      if (!destination) return undefined;
      const order = await this.readInteger('Move visual', 'Enter a zero-based destination order.');
      return order === undefined ? undefined : { visualId: visual.visualId, destinationPageId: destination.pageId, order };
    }
    const x = await this.readInteger('Resize visual', 'Enter x coordinate.');
    const y = await this.readInteger('Resize visual', 'Enter y coordinate.');
    const width = await this.readInteger('Resize visual', 'Enter width.');
    const height = await this.readInteger('Resize visual', 'Enter height.');
    return [x, y, width, height].some(value => value === undefined) ? undefined : { visualId: visual.visualId, layout: { x, y, width, height } };
  }

  private async readInteger(title: string, prompt: string): Promise<number | undefined> {
    const value = await vscode.window.showInputBox({ title, prompt, validateInput: input => /^-?\d+$/.test(input.trim()) ? undefined : 'Enter a whole number.' });
    if (value === undefined) return undefined;
    return Number.parseInt(value, 10);
  }

  private async previewConfirmExecute(kind: string, input: Record<string, unknown>): Promise<void> {
    const previewPayload = buildCuratedMutationPayload(kind, input);
    const preview = await this.requireBridge().executeAuthoringRequest<PbirAuthoringResponse>({
      ...previewPayload,
      mutate: { ...previewPayload.mutate as Record<string, unknown>, snapshot: this.snapshot },
    });
    if (!preview.succeeded) {
      this.present('Mutation Preview', preview);
      return;
    }
    const semanticPreview = preview.mutateResult?.preview;
    if (!semanticPreview) {
      this.presentException(new Error('The authoring backend returned no curated mutation preview.'));
      return;
    }
    if (semanticPreview.isNoOp) {
      void vscode.window.showInformationMessage('The requested mutation produces no changes.');
      return;
    }
    if (!semanticPreview.executionAdmissible) {
      this.present('Mutation Preview', preview);
      return;
    }
    const confirmation = await vscode.window.showInformationMessage(formatCuratedMutationPreview(semanticPreview), 'Apply Mutation', 'Cancel');
    if (confirmation !== 'Apply Mutation') return;
    const execute = await this.requireBridge().executeAuthoringRequest<PbirAuthoringResponse>({
      ...previewPayload,
      mutate: { ...previewPayload.mutate as Record<string, unknown>, mode: 'execute', snapshot: this.snapshot },
    });
    if (execute.mutateResult?.artifact) this.artifact = execute.mutateResult.artifact;
    this.present('Mutation', execute);
  }

  private requireBridge(): AuthoringBridge {
    const bridge = this.getBridge();
    if (!bridge) throw new Error('The PBIR authoring backend is not available.');
    return bridge;
  }

  private present(operation: string, response: PbirAuthoringResponse): void {
    const text = formatAuthoringResult(operation, response);
    this.outputChannel.appendLine(text);
    if (response.succeeded) void vscode.window.showInformationMessage(`${operation} completed${response.analyzer ? ` with score ${response.analyzer.score}` : ''}.`);
    else void vscode.window.showErrorMessage(formatAuthoringError(response));
  }

  private presentException(error: unknown): void {
    const message = error instanceof Error ? error.message : String(error);
    this.outputChannel.appendLine(`[Authoring] Exception: ${message}`);
    void vscode.window.showErrorMessage(message);
  }
}
