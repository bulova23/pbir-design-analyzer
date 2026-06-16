import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import { AnalyzerBridgeService } from '../services/rpc/AnalyzerBridgeService';
import { PbirTreeItem, PbirTreeProvider } from '../providers/PbirTreeProvider';
import { PbirScorePanel } from '../views/PbirScorePanel';
import { PbirDesignStudioPanel } from '../views/PbirDesignStudioPanel';
import { registerPbirExplorerReveal } from '../views/pbirExplorerReveal';
import { loadDesignAnalyzerConfig } from '../analyzer/config/store';
import { telemetry } from '../telemetry/reporter';
import { PBIR_COMMANDS, PBIR_VIEW_IDS } from '../platform/extensionIds';
import { getExtensionOutputChannel } from '../platform/outputChannels';
import { getAnalyzerSetting } from '../platform/settings';
import {
  buildGovernanceExportData,
  exportAsJson,
  exportAsMarkdown,
  type GovernanceCheckResult,
} from '../analyzer/score/governanceExport';
import type { ScoreResult } from '../analyzer/contracts/scorePanel';

export { PBIR_COMMANDS };

/** Shared provider instance (exported so register.ts can pass it to the treeview). */
export let pbirTreeProvider: PbirTreeProvider | undefined;

type PbirCommandTarget = string | PbirTreeItem | undefined;

type WorkspaceGovernanceSettings = {
    enabled: boolean;
    approvedThemeIds: string[];
};

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function resolveReportJsonPath(reportPath: string): string {
    if (reportPath.toLowerCase().endsWith('.report')) {
        return path.join(reportPath, 'definition', 'report.json');
    }

    const reportFolderName = `${path.basename(reportPath, path.extname(reportPath))}.Report`;
    return path.join(reportPath, reportFolderName, 'definition', 'report.json');
}

function readThemeIdFromPbir(reportPath: string): string {
    try {
        const reportJsonPath = resolveReportJsonPath(reportPath);
        if (!fs.existsSync(reportJsonPath)) {
            return '';
        }

        const reportJson = JSON.parse(fs.readFileSync(reportJsonPath, 'utf8')) as Record<string, unknown>;
        const theme = isRecord(reportJson.theme) ? reportJson.theme : undefined;
        const themeName = typeof theme?.name === 'string' ? theme.name.trim() : '';

        if (themeName.length > 0) {
            return themeName;
        }

        const themeHref = typeof theme?.href === 'string' ? theme.href.trim() : '';
        if (themeHref.length === 0) {
            return '';
        }

        return path.basename(themeHref, path.extname(themeHref));
    } catch {
        return '';
    }
}

type PbirTreeItemLike = {
    kind?: unknown;
    label?: unknown;
    jsonFilePath?: unknown;
    rawNode?: unknown;
};

function isPbirTreeItemLike(value: unknown): value is PbirTreeItemLike {
    return Boolean(value) && typeof value === 'object' && (
        'kind' in (value as Record<string, unknown>) ||
        'jsonFilePath' in (value as Record<string, unknown>)
    );
}

function resolveReportPathFromNodePath(nodePath: string | undefined): string | undefined {
    if (!nodePath) {
        return undefined;
    }

    let currentPath = nodePath;
    try {
        if (fs.statSync(currentPath).isFile()) {
            currentPath = path.dirname(currentPath);
        }
    } catch {
        return undefined;
    }

    for (;;) {
        if (fs.existsSync(path.join(currentPath, 'definition.pbir'))) {
            return currentPath;
        }

        const parentPath = path.dirname(currentPath);
        if (parentPath === currentPath) {
            return undefined;
        }

        currentPath = parentPath;
    }
}

function resolveCommandTarget(
    target: PbirCommandTarget,
    pageNameArg?: string,
): { reportPath?: string; pageName?: string } {
    if (typeof target === 'string') {
        return { reportPath: target, pageName: pageNameArg };
    }

    if (!isPbirTreeItemLike(target)) {
        return { reportPath: undefined, pageName: pageNameArg };
    }

    const rawNode = (target.rawNode ?? {}) as { displayName?: unknown; name?: unknown };
    const resolvedPageName = target.kind === 'page'
        ? (
            typeof rawNode.name === 'string' && rawNode.name.length > 0
                ? rawNode.name
                : typeof target.label === 'string' && target.label.length > 0
                    ? target.label
                    : typeof rawNode.displayName === 'string' && rawNode.displayName.length > 0
                        ? rawNode.displayName
                        : undefined
        )
        : pageNameArg;

    return {
        reportPath: typeof target.jsonFilePath === 'string'
            ? resolveReportPathFromNodePath(target.jsonFilePath)
            : undefined,
        pageName: resolvedPageName,
    };
}

function readWorkspaceGovernanceSettings(): WorkspaceGovernanceSettings {
    const enabled = getAnalyzerSetting<boolean>('governance.enabled', false) === true;
    const approvedThemeIdsRaw = getAnalyzerSetting<unknown>('governance.approvedThemeIds', []);
    const approvedThemeIds = Array.isArray(approvedThemeIdsRaw)
        ? approvedThemeIdsRaw.filter((value): value is string => typeof value === 'string' && value.trim().length > 0)
        : [];

    return {
        enabled,
        approvedThemeIds,
    };
}

function syncExplorerToReport(reportPath: string | undefined): void {
    if (!reportPath) {
        return;
    }

    pbirTreeProvider?.setProjectPath(reportPath);
}

/**
 * Registers all PBIR-related VS Code commands.
 * Called from register.ts during extension activation.
 */
export function registerPbirCommands(
    context: vscode.ExtensionContext,
    getBridge: () => AnalyzerBridgeService | undefined
): PbirTreeProvider {
    pbirTreeProvider = new PbirTreeProvider();
    pbirTreeProvider.setBridgeService(getBridge());

    // Wire provider into the treeview
    const treeView = vscode.window.createTreeView(PBIR_VIEW_IDS.explorer, {
        treeDataProvider: pbirTreeProvider,
        showCollapseAll: true,
    });
    registerPbirExplorerReveal(pbirTreeProvider, treeView);
    context.subscriptions.push(treeView);

    // pbirAnalyzer.refreshReports — manual refresh command
    context.subscriptions.push(
        vscode.commands.registerCommand(PBIR_COMMANDS.openDesignStudio, async (target?: PbirCommandTarget) => {
            telemetry.sendEvent('command.invoked', { commandName: PBIR_COMMANDS.openDesignStudio });
            let reportPath = resolveCommandTarget(target).reportPath;

            if (!reportPath && pbirTreeProvider) {
                try {
                    const rootItems = await pbirTreeProvider.getChildren();
                    if (rootItems.length > 0) {
                        reportPath = resolveCommandTarget(rootItems[0]).reportPath;
                    }
                } catch {
                    // Fall through to picker below.
                }
            }

            if (!reportPath) {
                const uris = await vscode.window.showOpenDialog({
                    title: 'Select PBIR report for Design Studio',
                    filters: { 'PBIR Reports': ['pbir'], 'All Files': ['*'] },
                    canSelectMany: false,
                    canSelectFolders: true,
                    openLabel: 'Open Design Studio',
                });
                reportPath = uris?.[0]?.fsPath;
            }

            if (!reportPath) {
                return;
            }

            if (!fs.existsSync(reportPath)) {
                vscode.window.showErrorMessage(`Report not found: ${reportPath}`);
                return;
            }

            await PbirDesignStudioPanel.createOrShow(context, reportPath);
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand(PBIR_COMMANDS.refreshReports, () => {
            pbirTreeProvider?.refresh();
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand(PBIR_COMMANDS.copyScoreDiagnostics, async () => {
            const copied = await PbirScorePanel.copyCurrentScoreDiagnostics();
            if (!copied) {
                void vscode.window.showWarningMessage('No score diagnostics are available yet. Run Score Report first.');
                return;
            }

            void vscode.window.showInformationMessage('Current score diagnostics copied to the clipboard.');
        })
    );

    // pbirAnalyzer.scoreReport — opens score panel for the active PBIR report (T023)
    // Feature 003: Enhanced to support per-page scoring when called from tree context menu
    /**
     * Scores a PBIR report and displays the Optimization Report panel.
     * 
     * Invocation modes:
     * 1. No arguments: Auto-detects report from tree view or shows file picker
     * 2. reportPathArg: Direct path to .Report folder or .pbir file
     * 3. reportPathArg + pageName: Scores specific page within a multi-page report
     * 
     * Feature 003 (Per-Page Scoring):
     * - Called from tree context menu with (reportPath, pageName)
     * - Supports both full-report scoring (pageName omitted) and single-page scoring (pageName provided)
     * - Page name must match exactly (case-sensitive)
     * 
     * Tree detection logic:
     * - If tree item is report root: pageName is undefined → full report scoring
     * - If tree item is page node: pageName is provided → single-page scoring
     * 
     * Returns:
     * - PbirScorePanel instance with tabbed UI (multi-page) or single-page view
     * - Tabs cached, switching is instant (0ms, no re-scoring)
     * - User can drill into recommendations and inspect affected visuals per page
     * 
     * Performance:
     * - Single-page: ~0.5 seconds
     * - Full 20-page report: ~6-10 seconds
     * 
     * Error handling:
     * - "Report not found" if path invalid
     * - "Page 'xyz' not found" if pageName doesn't match
     * - Backend errors shown in panel with retry button
     * 
     * @param reportPathArg Optional path to report; if omitted, tries tree auto-detect or file picker
     * @param pageName Optional page name for single-page scoring (exact match, case-sensitive)
     */
    context.subscriptions.push(
        vscode.commands.registerCommand(PBIR_COMMANDS.scoreReport, async (target?: PbirCommandTarget, pageNameArg?: string) => {
            telemetry.sendEvent('command.invoked', { commandName: PBIR_COMMANDS.scoreReport });
            try {
                const bridge = getBridge();
                const resolvedTarget = resolveCommandTarget(target, pageNameArg);
                let reportPath = resolvedTarget.reportPath;
                const pageName = resolvedTarget.pageName;

                if (!reportPath && pbirTreeProvider) {
                    // Try to auto-detect the report from the tree: get the root report item.
                    try {
                        const rootItems = await pbirTreeProvider.getChildren();
                        if (rootItems && rootItems.length > 0) {
                            reportPath = resolveCommandTarget(rootItems[0]).reportPath;
                        }
                    } catch {
                        // Silently fail and fall back to file picker.
                    }
                }

                if (!reportPath) {
                    const uris = await vscode.window.showOpenDialog({
                        title: 'Select PBIR report or Fabric App repo',
                        filters: { 'PBIR Reports': ['pbir'], 'All Files': ['*'] },
                        canSelectMany: false,
                        canSelectFolders: true,
                        openLabel: 'Open Review',
                    });
                    reportPath = uris?.[0]?.fsPath;
                }

                if (!reportPath) {
                    return; // User cancelled
                }

                // The report path can be either a folder (.Report) or a file (.pbir).
                // Verify it exists.
                if (!fs.existsSync(reportPath)) {
                    vscode.window.showErrorMessage(`Report not found: ${reportPath}`);
                    return;
                }

                syncExplorerToReport(reportPath);

                // Feature 003: Pass pageName to panel if provided (for per-page scoring)
                await PbirScorePanel.createOrShow(context, bridge, reportPath, pageName);
            } catch (error: unknown) {
                // Prevent extension host crash by catching and handling errors gracefully
                const message = error instanceof Error
                    ? error.message
                    : 'Unknown error occurred while scoring report';
                console.error('[pbirAnalyzer.scoreReport] Error:', error);
                vscode.window.showErrorMessage(`Failed to score report: ${message}`);

                const outputChannel = getExtensionOutputChannel();
                outputChannel.appendLine(`[ERROR] ${message}`);
                outputChannel.appendLine(
                    `Stack: ${error instanceof Error ? error.stack ?? 'No stack trace' : 'No stack trace'}`,
                );
            }
        })
    );

    // pbirAnalyzer.checkGovernance — score first, then evaluate policy (T035)
    context.subscriptions.push(
        vscode.commands.registerCommand(PBIR_COMMANDS.checkGovernance, async (target?: PbirCommandTarget) => {
            telemetry.sendEvent('command.invoked', { commandName: PBIR_COMMANDS.checkGovernance });
            const bridge = getBridge();
            let reportPath = resolveCommandTarget(target).reportPath;
            if (!reportPath) {
                const input = await vscode.window.showInputBox({
                    prompt: 'Enter path to the PBIP project root or .Report folder',
                    placeHolder: '/path/to/my-project.pbip',
                });
                reportPath = input;
            }
            if (!reportPath) return;

            if (!bridge) {
                vscode.window.showErrorMessage('PBIR: LSP server not available.');
                return;
            }

            const workspaceGovernance = readWorkspaceGovernanceSettings();
            const themeId = workspaceGovernance.enabled && workspaceGovernance.approvedThemeIds.length > 0
                ? readThemeIdFromPbir(reportPath)
                : '';

            type GovernanceResult = {
                policyState?: string;
                policyConfigured?: boolean;
                policyEnabled?: boolean;
                statusMessage?: string;
                blocked: boolean;
                reasons?: string[];
                evaluatedScore?: number;
                requiredThreshold?: number;
                policyNotes?: string;
            };

            try {
                const response = await bridge.executeRequest('model/pbir/governanceCheck', {
                    reportPath,
                    themeId,
                }) as { success: boolean; error?: string; data?: GovernanceResult };

                if (!response?.success) {
                    vscode.window.showErrorMessage(
                        `PBIR Governance Check failed: ${response?.error ?? 'unknown error'}`
                    );
                    return;
                }

                const result = response.data;
                if (!result) return;

                telemetry.sendEvent('governance.evaluated', {
                    blocked: result.blocked,
                    reasonCount: result.reasons?.length ?? 0,
                });

                if (result.policyState === 'notConfigured' || result.policyState === 'disabled') {
                    vscode.window.showInformationMessage(
                        result.statusMessage ??
                        'Workspace governance is not active. Publish blocking is off until a workspace policy is enabled.'
                    );
                } else if (result.blocked) {
                    const reasons = result.reasons?.join('\n• ') ?? 'No details.';
                    const notes   = result.policyNotes ? `\n\nPolicy notes: ${result.policyNotes}` : '';
                    const viewScoreAction = 'View Score';

                    const choice = await vscode.window.showErrorMessage(
                        `⛔ Governance check BLOCKED\n• ${reasons}${notes}`,
                        viewScoreAction
                    );

                    if (choice === viewScoreAction) {
                        vscode.commands.executeCommand(PBIR_COMMANDS.scoreReport, reportPath);
                    }
                } else {
                    vscode.window.showInformationMessage(
                        result.statusMessage ??
                        `✅ Governance check passed (score ${result.evaluatedScore?.toFixed(1)} ≥ ${result.requiredThreshold?.toFixed(1)}).`
                    );
                }
            } catch (err) {
                vscode.window.showErrorMessage(
                    `PBIR Governance Check error: ${err instanceof Error ? err.message : String(err)}`
                );
            }
        })
    );

    // pbirAnalyzer.exportGovernanceReport — score the report then export governance summary to Markdown or JSON
    context.subscriptions.push(
        vscode.commands.registerCommand(PBIR_COMMANDS.exportGovernanceReport, async (target?: PbirCommandTarget) => {
            const bridge = getBridge();
            let reportPath = resolveCommandTarget(target).reportPath;

            if (!reportPath) {
                const uris = await vscode.window.showOpenDialog({
                    title: 'Select PBIR Report',
                    filters: { 'PBIR Reports': ['pbir'], 'All Files': ['*'] },
                    canSelectMany: false,
                    canSelectFolders: false,
                    openLabel: 'Export Governance Report',
                });
                reportPath = uris?.[0]?.fsPath;
            }

            if (!reportPath) return;

            if (!fs.existsSync(reportPath)) {
                vscode.window.showErrorMessage(`Report not found: ${reportPath}`);
                return;
            }

            if (!bridge) {
                vscode.window.showErrorMessage('PBIR: LSP server not available.');
                return;
            }

            const formatChoice = await vscode.window.showQuickPick(
                [
                    { label: 'Markdown', description: 'Human-readable summary (.md)' },
                    { label: 'JSON', description: 'Machine-readable for CI/CD ingestion (.json)' },
                ],
                { placeHolder: 'Choose export format' },
            );
            if (!formatChoice) return;

            const isMarkdown = formatChoice.label === 'Markdown';

            const saveUri = await vscode.window.showSaveDialog({
                defaultUri: vscode.Uri.file(
                    path.join(
                        path.dirname(reportPath),
                        `governance-report.${isMarkdown ? 'md' : 'json'}`,
                    ),
                ),
                filters: isMarkdown
                    ? { Markdown: ['md'] }
                    : { JSON: ['json'] },
                saveLabel: 'Export',
            });
            if (!saveUri) return;

            await vscode.window.withProgress(
                { location: vscode.ProgressLocation.Notification, title: 'Exporting governance report…', cancellable: false },
                async () => {
                    try {
                        const config = await loadDesignAnalyzerConfig(context);

                        const [scoreResponse, govResponse] = await Promise.all([
                            bridge.executeRequest('model/pbir/scoreReport', { reportPath, config }) as Promise<{
                                success: boolean;
                                error?: string;
                                data?: ScoreResult;
                            }>,
                            bridge.executeRequest('model/pbir/governanceCheck', { reportPath, themeId: '' }) as Promise<{
                                success: boolean;
                                error?: string;
                                data?: GovernanceCheckResult;
                            }>,
                        ]);

                        if (!scoreResponse?.success || !scoreResponse.data) {
                            vscode.window.showErrorMessage(
                                `Export failed: could not score report — ${scoreResponse?.error ?? 'unknown error'}`,
                            );
                            return;
                        }

                        const governanceResult: GovernanceCheckResult = scoreResponse.data.governanceScore !== undefined
                            ? {
                                blocked: govResponse?.data?.blocked ?? false,
                                policyState: govResponse?.data?.policyState,
                                evaluatedScore: govResponse?.data?.evaluatedScore,
                                requiredThreshold: govResponse?.data?.requiredThreshold,
                                reasons: govResponse?.data?.reasons ?? [],
                                policyNotes: govResponse?.data?.policyNotes,
                            }
                            : { blocked: false, reasons: [] };

                        const exportData = buildGovernanceExportData(scoreResponse.data, governanceResult);
                        const content = isMarkdown ? exportAsMarkdown(exportData) : exportAsJson(exportData);

                        fs.writeFileSync(saveUri.fsPath, content, 'utf8');

                        const openAction = 'Open File';
                        const choice = await vscode.window.showInformationMessage(
                            `Governance report exported to ${path.basename(saveUri.fsPath)}`,
                            openAction,
                        );
                        if (choice === openAction) {
                            vscode.window.showTextDocument(saveUri);
                        }
                    } catch (err) {
                        vscode.window.showErrorMessage(
                            `Export failed: ${err instanceof Error ? err.message : String(err)}`,
                        );
                    }
                },
            );
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand(PBIR_COMMANDS.exportReviewWorkflow, async (target?: PbirCommandTarget) => {
            telemetry.sendEvent('command.invoked', { commandName: PBIR_COMMANDS.exportReviewWorkflow });
            try {
                const bridge = getBridge();
                let reportPath = resolveCommandTarget(target).reportPath;

                if (!reportPath && pbirTreeProvider) {
                    try {
                        const rootItems = await pbirTreeProvider.getChildren();
                        if (rootItems && rootItems.length > 0) {
                            reportPath = resolveCommandTarget(rootItems[0]).reportPath;
                        }
                    } catch {
                        // Silently fail and fall back to file picker.
                    }
                }

                if (!reportPath) {
                    const uris = await vscode.window.showOpenDialog({
                        title: 'Select PBIR Report',
                        filters: { 'PBIR Reports': ['pbir'], 'All Files': ['*'] },
                        canSelectMany: false,
                        canSelectFolders: false,
                        openLabel: 'Export Review Summary',
                    });
                    reportPath = uris?.[0]?.fsPath;
                }

                if (!reportPath) {
                    return;
                }

                if (!fs.existsSync(reportPath)) {
                    vscode.window.showErrorMessage(`Report not found: ${reportPath}`);
                    return;
                }

                syncExplorerToReport(reportPath);
                const panel = await PbirScorePanel.createOrShow(context, bridge, reportPath);
                await panel.exportReviewWorkflow();
            } catch (error: unknown) {
                const message = error instanceof Error
                    ? error.message
                    : 'Unknown error occurred while exporting review workflow';
                console.error('[pbirAnalyzer.exportReviewWorkflow] Error:', error);
                vscode.window.showErrorMessage(`Failed to export review workflow: ${message}`);
            }
        })
    );

    // pbirAnalyzer.uploadScreenshots — opens score panel then triggers screenshot upload via the active panel
    context.subscriptions.push(
        vscode.commands.registerCommand(PBIR_COMMANDS.uploadScreenshots, async (target?: PbirCommandTarget) => {
            const reportPath = resolveCommandTarget(target).reportPath ?? resolveCommandTarget(pbirTreeProvider ? await pbirTreeProvider.getChildren().then((items) => items?.[0]).catch(() => undefined) : undefined).reportPath;
            if (!reportPath) {
                vscode.window.showErrorMessage('Open a report in the score panel before uploading screenshots.');
                return;
            }
            // Open the score panel so the upload dialog has context
            syncExplorerToReport(reportPath);
            const bridge = getBridge();
            const panel = await PbirScorePanel.createOrShow(context, bridge, reportPath);
            await panel.requestScreenshotUpload();
        })
    );

    // pbirAnalyzer.configureAuditProvider — provider picker + key entry, stored in SecretStorage
    context.subscriptions.push(
        vscode.commands.registerCommand(PBIR_COMMANDS.configureAuditProvider, async () => {
            const { runProviderSetupFlow } = await import('../analyzer/audit/providers/providerSetup');
            await runProviderSetupFlow(context);
        })
    );

    return pbirTreeProvider;
}
