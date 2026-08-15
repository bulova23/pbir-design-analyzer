import * as vscode from 'vscode';
import type { RenderedEvidenceCapabilityReport } from './types';

export const PBI_LENS_RECOMMENDATION_DISMISSED_KEY = 'pbirAnalyzer.pbiLensRecommendationDismissed';
export const PBI_LENS_MARKETPLACE_URL = 'https://marketplace.visualstudio.com/items?itemName=duckduck-beps.pbi-lens-vscode';

interface GlobalStateLike {
  get<T>(key: string, defaultValue: T): T;
  update(key: string, value: unknown): Thenable<void>;
}

type ShowInformationMessage = (
  message: string,
  ...items: string[]
) => Thenable<string | undefined>;

export async function maybeRecommendPbiLens(
  report: RenderedEvidenceCapabilityReport,
  globalState: GlobalStateLike,
  showInformationMessage: ShowInformationMessage,
  openExternal: (uri: vscode.Uri) => Thenable<boolean> = (uri) => vscode.env.openExternal(uri),
  executeCommand: (command: string, ...args: unknown[]) => Thenable<unknown> = (command, ...args) =>
    vscode.commands.executeCommand(command, ...args),
): Promise<void> {
  if (report.status !== 'NotInstalled' || globalState.get(PBI_LENS_RECOMMENDATION_DISMISSED_KEY, false)) {
    return;
  }

  const selection = await showInformationMessage(
    'Install PBI Lens for future enhanced rendered-design scoring support.',
    'Learn More',
    'Install PBI Lens',
    'Not Now',
  );

  if (selection === 'Learn More') {
    await openExternal(vscode.Uri.parse(PBI_LENS_MARKETPLACE_URL));
    return;
  }

  if (selection === 'Install PBI Lens') {
    await executeCommand('workbench.extensions.search', '@id:duckduck-beps.pbi-lens-vscode');
    return;
  }

  await globalState.update(PBI_LENS_RECOMMENDATION_DISMISSED_KEY, true);
}
