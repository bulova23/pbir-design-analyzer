import * as path from 'path';
import * as vscode from 'vscode';
import {
    buildLocalPbirTree,
    PbirPageNode,
    PbirReportNode,
    PbirVisualNode,
} from '../analyzer/project/localTree';
import { detectWorkspacePbirProjectPath } from '../analyzer/project/discovery';
import { AnalyzerBridgeService } from '../services/rpc/AnalyzerBridgeService';
import { resolvePbirWorkspaceRoot } from '../analyzer/project/pathing';

// ── Tree item types ───────────────────────────────────────────────────────────

type PbirItemKind = 'report' | 'page' | 'visual' | 'theme' | 'disconnected';

/**
 * A single node in the PBIR treeview (report → page → visual, with a theme sibling).
 */
export class PbirTreeItem extends vscode.TreeItem {
    constructor(
        label: string,
        public readonly kind: PbirItemKind,
        collapsibleState: vscode.TreeItemCollapsibleState,
        /** Absolute path to the JSON file that backs this node (click-to-open). */
        public readonly jsonFilePath?: string,
        /** Raw API node payload for building children. */
        public readonly rawNode?: PbirReportNode | PbirPageNode | PbirVisualNode,
    ) {
        super(label, collapsibleState);

        this.contextValue = kind;
        this.tooltip = jsonFilePath ?? label;

        if (jsonFilePath) {
            this.resourceUri = vscode.Uri.file(jsonFilePath);
            this.command = {
                command: 'vscode.open',
                title: 'Open PBIR JSON',
                arguments: [this.resourceUri],
            };
        }

        this.iconPath = PbirTreeItem.iconFor(kind);
    }

    private static iconFor(kind: PbirItemKind): vscode.ThemeIcon {
        switch (kind) {
            case 'report':     return new vscode.ThemeIcon('file-code');
            case 'page':       return new vscode.ThemeIcon('layout');
            case 'visual':     return new vscode.ThemeIcon('graph');
            case 'theme':      return new vscode.ThemeIcon('paintcan');
            case 'disconnected': return new vscode.ThemeIcon('circle-slash');
        }
    }
}

// ── Provider ─────────────────────────────────────────────────────────────────

/**
 * VS Code TreeDataProvider for the `pbirAnalyzer.explorer` view.
 *
 * Displays the PBIR report hierarchy:
 * ```
 * Report Name
 *   ├── [theme icon] Theme Name
 *   ├── [page icon]  Page 1
 *   │     ├── [graph icon] Visual 1 (barChart)
 *   │     └── [graph icon] Visual 2 (card)
 *   └── [page icon]  Page 2
 * ```
 */
export class PbirTreeProvider implements vscode.TreeDataProvider<PbirTreeItem> {
    private _onDidChangeTreeData = new vscode.EventEmitter<PbirTreeItem | undefined | void>();
    readonly onDidChangeTreeData = this._onDidChangeTreeData.event;

    private _bridge: AnalyzerBridgeService | undefined;
    private _projectPath: string | undefined;

    /** Call when the active PBIP project path changes. */
    setProjectPath(projectPath: string | undefined): void {
        this._projectPath = projectPath;
        this.refresh();
    }

    /** Inject the RPC bridge (called after extension activation). */
    setBridgeService(bridge: AnalyzerBridgeService | undefined): void {
        this._bridge = bridge;
    }

    /** Force a full tree refresh. */
    refresh(): void {
        this._onDidChangeTreeData.fire();
    }

    async findReportItem(): Promise<PbirTreeItem | undefined> {
        const rootItems = await this.getChildren();
        return rootItems.find((item) => item.kind === 'report');
    }

    async findPageItem(pageName: string): Promise<PbirTreeItem | undefined> {
        const reportItem = await this.findReportItem();
        if (!reportItem) {
            return undefined;
        }

        const pageItems = await this.getChildren(reportItem);
        return pageItems.find((item) => this._matchesPage(item, pageName));
    }

    async findVisualItem(pageName: string, visualId: string): Promise<PbirTreeItem | undefined> {
        const reportItem = await this.findReportItem();
        if (!reportItem) {
            return undefined;
        }

        const pageItem = await this.findPageItem(pageName);
        if (!pageItem) {
            return undefined;
        }

        const visualItems = await this.getChildren(pageItem);
        return visualItems.find((item) => this._matchesVisual(item, visualId));
    }

    // ── TreeDataProvider ─────────────────────────────────────────────────────

    getTreeItem(element: PbirTreeItem): vscode.TreeItem {
        return element;
    }

    async getChildren(element?: PbirTreeItem): Promise<PbirTreeItem[]> {
        if (!element) {
            return this._getRootItems();
        }

        switch (element.kind) {
            case 'report': return this._getReportChildren(element.rawNode as PbirReportNode | undefined);
            case 'page':   return this._getPageChildren(element.rawNode as PbirPageNode | undefined);
            default:       return [];
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /** Root level: one Report node (or a "no model" placeholder). */
    private async _getRootItems(): Promise<PbirTreeItem[]> {
        if (!this._projectPath) {
            const detectedProjectPath = await detectWorkspacePbirProjectPath();
            if (detectedProjectPath) {
                this._projectPath = detectedProjectPath;
            }
        }

        if (!this._projectPath) {
            return [new PbirTreeItem(
                'No PBIP project selected',
                'disconnected',
                vscode.TreeItemCollapsibleState.None,
            )];
        }

        let backendError: string | undefined;
        if (this._bridge?.isInitialized()) {
            try {
                const response = await this._bridge.getPbirTree(this._projectPath) as {
                    success?: boolean;
                    data?: PbirReportNode;
                    error?: string;
                };
                if (response?.success && response.data) {
                    return [this._createReportItem(response.data)];
                }

                backendError = response?.error ?? 'PBIR report not found';
            } catch (err) {
                backendError = `Error loading PBIR: ${(err as Error).message}`;
            }
        }

        const localTree = await buildLocalPbirTree(this._projectPath);
        if (localTree) {
            return [this._createReportItem(localTree)];
        }

        if (backendError) {
            return [new PbirTreeItem(
                backendError,
                'disconnected',
                vscode.TreeItemCollapsibleState.None,
            )];
        }

        return [new PbirTreeItem(
            'PBIR report not found',
            'disconnected',
            vscode.TreeItemCollapsibleState.None,
        )];
    }

    private _createReportItem(tree: PbirReportNode): PbirTreeItem {
        return new PbirTreeItem(
            tree.name ?? 'Report',
            'report',
            vscode.TreeItemCollapsibleState.Expanded,
            tree.path ? this._resolvePath(tree.path) : undefined,
            tree,
        );
    }

    /** Report children: optional theme node + page nodes. */
    private _getReportChildren(tree?: PbirReportNode): PbirTreeItem[] {
        const children: PbirTreeItem[] = [];

        if (tree?.theme) {
            const themeName = tree.theme.name ?? 'Custom Theme';
            children.push(new PbirTreeItem(
                themeName,
                'theme',
                vscode.TreeItemCollapsibleState.None,
                tree.theme.sourcePath ? this._resolvePath(tree.theme.sourcePath) : undefined,
            ));
        }

        const pages = tree?.pages ?? [];
        for (const page of pages) {
            const hasVisuals = Array.isArray(page.visuals) && page.visuals.length > 0;
            children.push(new PbirTreeItem(
                page.displayName ?? page.name ?? 'Page',
                'page',
                hasVisuals
                    ? vscode.TreeItemCollapsibleState.Collapsed
                    : vscode.TreeItemCollapsibleState.None,
                page.path ? this._resolvePath(page.path) : undefined,
                page,
            ));
        }

        return children;
    }

    /** Page children: visual nodes. */
    private _getPageChildren(page?: PbirPageNode): PbirTreeItem[] {
        const visuals = page?.visuals ?? [];
        return visuals.map((visual) => {
            const label = visual.visualType
                ? `${visual.name ?? 'Visual'} (${visual.visualType})`
                : (visual.name ?? 'Visual');
            return new PbirTreeItem(
                label,
                'visual',
                vscode.TreeItemCollapsibleState.None,
                visual.path ? this._resolvePath(visual.path) : undefined,
                visual,
            );
        });
    }

    private _matchesPage(item: PbirTreeItem, pageName: string): boolean {
        if (item.kind !== 'page') {
            return false;
        }

        const rawNode = item.rawNode as PbirPageNode | undefined;
        const candidates = [
            rawNode?.displayName,
            rawNode?.name,
            typeof item.label === 'string' ? item.label : undefined,
        ];

        return candidates.some((candidate) => this._matchesIdentifier(candidate, pageName));
    }

    private _matchesVisual(item: PbirTreeItem, visualId: string): boolean {
        if (item.kind !== 'visual') {
            return false;
        }

        const rawNode = item.rawNode as PbirVisualNode | undefined;
        const candidates = [
            rawNode?.id,
            rawNode?.name,
            typeof item.label === 'string' ? item.label : undefined,
            item.resourceUri?.fsPath ? path.basename(path.dirname(item.resourceUri.fsPath)) : undefined,
        ];

        return candidates.some((candidate) => this._matchesIdentifier(candidate, visualId));
    }

    private _matchesIdentifier(candidate: string | undefined, expected: string): boolean {
        return typeof candidate === 'string' &&
            candidate.trim().localeCompare(expected.trim(), undefined, { sensitivity: 'accent' }) === 0;
    }

    /**
     * Converts a workspace-relative path returned by the server back to an absolute path.
     * Falls back to the raw value if the project path is unknown.
     */
    private _resolvePath(relativePath: string): string {
        if (!this._projectPath) return relativePath;
        if (path.isAbsolute(relativePath) || /^[a-z]+:\/\//i.test(relativePath)) return relativePath;
        const workspaceRoot = resolvePbirWorkspaceRoot(this._projectPath);
        if (!workspaceRoot) return relativePath;
        return path.resolve(workspaceRoot, relativePath);
    }
}
