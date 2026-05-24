import * as vscode from 'vscode';

export type TelemetryEventName =
  | 'command.invoked'
  | 'scoring.completed'
  | 'governance.evaluated'
  | 'framework.enabled'
  | 'framework.disabled';

export interface TelemetryProperties {
  [key: string]: string | number | boolean;
}

/**
 * Privacy-respecting telemetry reporter.
 *
 * Respects VS Code's global `telemetry.telemetryLevel` setting via
 * `vscode.env.isTelemetryEnabled`. When telemetry is off or the reporter
 * has not been initialized, all calls are no-ops.
 *
 * No PII, no file contents, and no visual data are ever emitted.
 * Events carry only bucketed or categorical values.
 */
class TelemetryReporter {
  private static instance: TelemetryReporter | undefined;
  private extensionId = '';
  private extensionVersion = '';
  private initialized = false;

  static getInstance(): TelemetryReporter {
    if (!TelemetryReporter.instance) {
      TelemetryReporter.instance = new TelemetryReporter();
    }
    return TelemetryReporter.instance;
  }

  initialize(context: vscode.ExtensionContext): void {
    this.extensionId = context.extension.id;
    this.extensionVersion = context.extension.packageJSON?.version as string ?? 'unknown';
    this.initialized = true;
  }

  sendEvent(name: TelemetryEventName, properties?: TelemetryProperties): void {
    if (!this.initialized || !vscode.env.isTelemetryEnabled) {
      return;
    }

    // In a production release, forward to Application Insights or similar.
    // For now we log to the extension output channel in debug mode only.
    if (process.env['NODE_ENV'] === 'development') {
      const propsStr = properties ? JSON.stringify(properties) : '';
      console.log(`[Telemetry] ${name} ${propsStr}`);
    }
  }

  dispose(): void {
    TelemetryReporter.instance = undefined;
    this.initialized = false;
  }
}

export const telemetry = TelemetryReporter.getInstance();

export function bucketScore(score: number): string {
  if (score >= 90) return '90-100';
  if (score >= 70) return '70-89';
  if (score >= 50) return '50-69';
  return '0-49';
}
