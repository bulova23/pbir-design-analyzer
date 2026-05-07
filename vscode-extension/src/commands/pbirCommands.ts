import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import { LSPModelService } from '../services/lsp/LSPModelService';
import { PbirTreeItem, PbirTreeProvider } from '../providers/PbirTreeProvider';
import { PbirScorePanel } from '../views/PbirScorePanel';
import { registerPbirExplorerReveal } from '../views/pbirExplorerReveal';

interface RefactorResultData {
    appliedOperations: string[];
    warnings: string[];
    reportPath?: string;
    isClean: boolean;
}

// PBIR command IDs — must match CommandDispatcher registrations
export const PBIR_COMMANDS = {
    getTree: 'pbir.getTree',
    refreshTree: 'pbir.refreshTree',
    scoreReport: 'pbir.scoreReport',
    refactor: 'pbir.refactor',
    governanceCheck: 'pbir.governanceCheck',
} as const;

/** Shared provider instance (exported so register.ts can pass it to the treeview). */
export let pbirTreeProvider: PbirTreeProvider | undefined;

type PbirCommandTarget = string | PbirTreeItem | undefined;

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

    while (true) {
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
            typeof rawNode.displayName === 'string' && rawNode.displayName.length > 0
                ? rawNode.displayName
                : typeof target.label === 'string' && target.label.length > 0
                    ? target.label
                    : typeof rawNode.name === 'string' && rawNode.name.length > 0
                        ? rawNode.name
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

/**
 * Registers all PBIR-related VS Code commands.
 * Called from register.ts during extension activation.
 */
export function registerPbirCommands(
    context: vscode.ExtensionContext,
    getBridge: () => LSPModelService | undefined
): PbirTreeProvider {
    pbirTreeProvider = new PbirTreeProvider();
    pbirTreeProvider.setLSPModelService(getBridge());

    // Wire provider into the treeview
    const treeView = vscode.window.createTreeView('powerbiModeling.pbirExplorer', {
        treeDataProvider: pbirTreeProvider,
        showCollapseAll: true,
    });
    registerPbirExplorerReveal(pbirTreeProvider, treeView);
    context.subscriptions.push(treeView);

    // pbir.refreshTree — manual refresh command
    context.subscriptions.push(
        vscode.commands.registerCommand(PBIR_COMMANDS.refreshTree, () => {
            pbirTreeProvider?.refresh();
        })
    );

    // pbir.scoreReport — opens score panel for the active PBIR report (T023)
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
                    } catch (err) {
                        // Silently fail and fall back to file picker.
                    }
                }

                if (!reportPath) {
                    const uris = await vscode.window.showOpenDialog({
                        title: 'Select PBIR Report',
                        filters: { 'PBIR Reports': ['pbir'], 'All Files': ['*'] },
                        canSelectMany: false,
                        canSelectFolders: false,
                        openLabel: 'Score Report',
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

                // Feature 003: Pass pageName to panel if provided (for per-page scoring)
                await PbirScorePanel.createOrShow(context, bridge, reportPath, pageName);
            } catch (error: unknown) {
                // Prevent extension host crash by catching and handling errors gracefully
                const message = error instanceof Error
                    ? error.message
                    : 'Unknown error occurred while scoring report';
                console.error('[pbir.scoreReport] Error:', error);
                vscode.window.showErrorMessage(`Failed to score report: ${message}`);
                
                // Optional: Log to output channel for debugging
                try {
                    const outputChannel = vscode.window.createOutputChannel('PBIR Design Analyzer');
                    outputChannel.appendLine(`[ERROR] ${message}`);
                    outputChannel.appendLine(
                        `Stack: ${error instanceof Error ? error.stack ?? 'No stack trace' : 'No stack trace'}`,
                    );
                } catch (e) {
                    // Silently ignore if output channel creation fails
                }
            }
        })
    );

    // pbir.refactor — QuickPick operations then call LSP (T027)
    context.subscriptions.push(
        vscode.commands.registerCommand(PBIR_COMMANDS.refactor, async (target?: PbirCommandTarget) => {
            const bridge = getBridge();
            let reportPath = resolveCommandTarget(target).reportPath;

            if (!reportPath) {
                const input = await vscode.window.showInputBox({
                    prompt: 'Enter path to the PBIP project root or .Report folder',
                    placeHolder: '/path/to/my-project.pbip',
                });
                reportPath = input;
            }

            if (!reportPath) {
                return;
            }

            const operationItems: vscode.QuickPickItem[] = [
                { label: 'snapToGrid',          description: 'Snap all visuals to the 12-column grid', picked: true },
                { label: 'normalizeFonts',       description: 'Set font sizes per visual hierarchy (KPI→32, chart→16, text→12)', picked: true },
                { label: 'reduceColorVariance',  description: 'Reduce theme data colours to ≤5 most distinct', picked: false },
                { label: 'flagPieCharts',        description: 'Flag pie/donut chart visuals as warnings', picked: true },
            ];

            const selected = await vscode.window.showQuickPick(operationItems, {
                canPickMany: true,
                placeHolder: 'Select refactoring operations to apply',
                title: 'PBIR: Internal Refactor',
            });

            if (!selected || selected.length === 0) {
                return;
            }

            const operations = selected.map(item => item.label);

            if (!bridge) {
                vscode.window.showErrorMessage('PBIR: LSP server not available.');
                return;
            }

            await vscode.window.withProgress(
                { location: vscode.ProgressLocation.Notification, title: 'PBIR: Running internal refactor…', cancellable: false },
                async () => {
                    try {
                        const response = await bridge.executeRequest('model/pbir/refactor', {
                            reportPath,
                            operations,
                        }) as { success: boolean; error?: string; data?: RefactorResultData };

                        if (!response?.success) {
                            vscode.window.showErrorMessage(
                                `PBIR Refactor failed: ${response?.error ?? 'unknown error'}`
                            );
                            return;
                        }

                        const data = response.data;
                        const appliedCount = data?.appliedOperations?.length ?? 0;
                        const warnCount    = data?.warnings?.length ?? 0;

                        const summary = appliedCount > 0
                            ? data!.appliedOperations.join('\n')
                            : 'No changes were necessary.';

                        if (warnCount > 0) {
                            vscode.window.showWarningMessage(
                                `PBIR Refactor: ${appliedCount} operation(s) applied, ${warnCount} warning(s).\n${summary}`
                            );
                        } else {
                            vscode.window.showInformationMessage(
                                `PBIR Refactor complete: ${appliedCount} operation(s) applied.\n${summary}`
                            );
                        }

                        // Refresh tree after structural changes.
                        pbirTreeProvider?.refresh();
                    } catch (err) {
                        vscode.window.showErrorMessage(
                            `PBIR Refactor error: ${err instanceof Error ? err.message : String(err)}`
                        );
                    }
                }
            );
        })
    );

    // pbir.governanceCheck — score first, then evaluate policy (T035)
    context.subscriptions.push(
        vscode.commands.registerCommand(PBIR_COMMANDS.governanceCheck, async (target?: PbirCommandTarget) => {
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

            const themeId = await vscode.window.showInputBox({
                prompt: 'Enter the theme name to validate (leave blank to skip theme check)',
                placeHolder: 'CorporateBlue',
                ignoreFocusOut: true,
            }) ?? '';

            type GovernanceResult = {
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

                if (result.blocked) {
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

    return pbirTreeProvider;
}
