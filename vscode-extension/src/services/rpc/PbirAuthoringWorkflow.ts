import * as fs from 'fs';
import * as vscode from 'vscode';
import { getExtensionOutputChannel } from '../../platform/outputChannels';
import type { AnalyzerBridgeService } from './AnalyzerBridgeService';

export type PbirAuthoringResponse = {
  succeeded: boolean;
  diagnostics?: Array<{ code: string; field: string; severity: string; summary: string }>;
  error?: { category: string; code: string; summary: string };
  artifactIdentity?: { artifactId: string; artifactHash: string; manifestId: string; manifestHash: string };
  fidelity?: { classification: string; preservedPathCount: number; changedPathCount: number; unexpectedPathCount: number };
  analyzer?: { score: number; pageCount: number; visualCount: number };
  timing?: { dispatchMilliseconds: number; orchestrationMilliseconds: number; serializationMilliseconds: number; analyzerMilliseconds: number };
  generateResult?: { requestVersion: string; artifact?: { schemaVersion: string; artifactId: string; artifactHash: string; manifestId: string; manifestHash: string } };
  importResult?: { snapshot: { schemaVersion: string; snapshotId: string; sourceIdentity: { sourceDirectoryName: string; contentHash: string; fileCount: number } } };
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

export function formatAuthoringError(response: Pick<PbirAuthoringResponse, 'error'>): string {
  const category = response.error?.category ?? 'internalFailure';
  const labels: Record<string, string> = {
    invalidRequest: 'Invalid request',
    unsupportedAuthoring: 'Unsupported PBIR construct',
    importFailed: 'Import failed',
    validationFailed: 'Validation failed',
    analyzerFailed: 'Analyzer failed',
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
      if (response.importResult?.snapshot) this.snapshot = response.importResult.snapshot;
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
