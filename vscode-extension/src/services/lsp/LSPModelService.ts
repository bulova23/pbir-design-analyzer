/**
 * LSPModelService - Replacement for DotNetBridge using Language Server Protocol
 *
 * This service provides the same API as DotNetBridge but uses the standard
 * vscode-languageclient library to communicate with the ModelingLanguageServer.
 *
 * Benefits over custom daemon:
 * - Native VS Code lifecycle management (auto-start, auto-restart)
 * - Built-in request queuing and timeout handling
 * - Standard LSP protocol
 * - No manual process management required
 */

import { LanguageClient } from 'vscode-languageclient/node';
import { EventEmitter } from 'events';
import * as vscode from 'vscode';

/**
 * LSP Model Service state
 */
export enum LSPState {
  UNINITIALIZED = 'uninitialized',
  STARTING = 'starting',
  READY = 'ready',
  ERROR = 'error'
}

/**
 * Singleton service for Power BI model operations via LSP
 * Mirrors DotNetBridge API for easy migration
 */
export class LSPModelService extends EventEmitter {
  private static instance: LSPModelService | null = null;
  private client: LanguageClient | null = null;
  private state: LSPState = LSPState.UNINITIALIZED;
  private readonly defaultTimeout = 30000; // 30 seconds
  private outputChannel: vscode.OutputChannel | null = null;

  private constructor() {
    super();
    // Create dedicated output channel for LSP communication
    this.outputChannel = vscode.window.createOutputChannel('Modeling Language Server');
  }

  /**
   * Get the singleton instance
   */
  static getInstance(): LSPModelService {
    if (!LSPModelService.instance) {
      LSPModelService.instance = new LSPModelService();
    }
    return LSPModelService.instance;
  }

  /**
   * Reset the singleton instance (for testing)
   */
  static resetInstance(): void {
    if (LSPModelService.instance) {
      LSPModelService.instance.shutdown();
      LSPModelService.instance = null;
    }
  }

  /**
   * Initialize the service with the language client
   * This should be called after the language client is created and started
   */
  async initialize(client: LanguageClient): Promise<void> {
    if (this.state !== LSPState.UNINITIALIZED) {
      return;
    }

    this.state = LSPState.STARTING;
    this.client = client;

    // Wait for the client to reach Running state
    console.log('[LSPModelService] Waiting for client to reach Running state...');
    this.outputChannel?.appendLine('[LSPModelService] Waiting for client to reach Running state...');

    // The client is already started by the caller, wait a moment for it to be fully running
    await new Promise(resolve => setTimeout(resolve, 500));

    // Client is ready
    this.state = LSPState.READY;
    console.log('[LSPModelService] ✅ LSP Connected - Client is ready');
    this.outputChannel?.appendLine('[LSPModelService] ✅ LSP Connected - Client is ready');
    this.emit('stateChange', LSPState.READY);
    this.emit('connected'); // Emit a specific 'connected' event for the extension to listen to
  }

  /**
   * Get current state
   */
  getState(): LSPState {
    return this.state;
  }

  /**
   * Check if service is initialized and ready
   * (Compatibility method for DotNetBridge migration)
   */
  isInitialized(): boolean {
    return this.state === LSPState.READY;
  }

  /**
   * Register a state change listener
   */
  onStateChange(listener: (state: LSPState) => void): void {
    this.on('stateChange', listener);
  }

  /**
   * Shutdown the service
   */
  async shutdown(): Promise<void> {
    if (this.client) {
      await this.client.stop();
      this.client = null;
    }
    this.state = LSPState.UNINITIALIZED;
    this.emit('stateChange', LSPState.UNINITIALIZED);
  }

  /**
   * Send a request to the language server
   */
  private async sendRequest<T>(method: string, params: any = {}, timeout: number = this.defaultTimeout): Promise<T> {
    if (!this.client) {
      throw new Error('LSPModelService not initialized. Call initialize() first.');
    }

    if (this.state !== LSPState.READY) {
      throw new Error(`LSPModelService not ready. Current state: ${this.state}`);
    }

    const timestamp = new Date().toISOString();
    const requestId = Math.random().toString(36).substring(7);

    this.outputChannel?.appendLine(`\n[${timestamp}] >>> Outgoing Request [${requestId}]`);
    this.outputChannel?.appendLine(`Method: ${method}`);
    this.outputChannel?.appendLine(`Params: ${JSON.stringify(params, null, 2)}`);

    console.log(`[LSPModelService] Sending request: ${method} [${requestId}]`, params);
    const startTime = Date.now();

    try {
      // Create a promise that will timeout if needed
      const timeoutPromise = new Promise<never>((_, reject) => {
        setTimeout(() => reject(new Error(`Request timeout after ${timeout}ms`)), timeout);
      });

      // Race between the actual request and the timeout
      const resultPromise = this.client.sendRequest<T>(method, params);
      const result = await Promise.race([resultPromise, timeoutPromise]);

      const elapsed = Date.now() - startTime;

      this.outputChannel?.appendLine(`\n[${new Date().toISOString()}] <<< Incoming Response [${requestId}]`);
      this.outputChannel?.appendLine(`Method: ${method}`);
      this.outputChannel?.appendLine(`Elapsed: ${elapsed}ms`);
      this.outputChannel?.appendLine(`Response: ${JSON.stringify(result, null, 2)}`);

      console.log(`[LSPModelService] Request ${method} completed in ${elapsed}ms [${requestId}]`);
      console.log(`[LSPModelService] Response:`, JSON.stringify(result, null, 2));

      return result;
    } catch (error: any) {
      const elapsed = Date.now() - startTime;

      this.outputChannel?.appendLine(`\n[${new Date().toISOString()}] !!! Request Error [${requestId}]`);
      this.outputChannel?.appendLine(`Method: ${method}`);
      this.outputChannel?.appendLine(`Elapsed: ${elapsed}ms`);
      this.outputChannel?.appendLine(`Error: ${error.message}`);
      this.outputChannel?.appendLine(`Stack: ${error.stack}`);

      console.error(`[LSPModelService] Request ${method} failed after ${elapsed}ms [${requestId}]:`, error);
      throw new Error(`LSP request failed: ${error.message}`);
    }
  }

  // ============================================================================
  // MODEL OPERATION API (mirrors DotNetBridge)
  // ============================================================================

  /**
   * Ping the language server to check if it's alive
   */
  async ping(): Promise<any> {
    return this.sendRequest('model/ping');
  }

  /**
   * Connect to a Fabric workspace and semantic model
   */
  async connect(workspace: string, semanticModel: string, accessToken: string): Promise<any> {
    return this.sendRequest('model/connect', { workspace, semanticModel, accessToken });
  }

  /**
   * Load a PBIP project from file path
   */
  async loadPbip(projectPath: string): Promise<any> {
    return this.sendRequest('model/loadPBIP', { projectPath });
  }

  /**
   * Get connection status and type (Fabric, PBIP, or None)
   */
  async getConnectionStatus(): Promise<any> {
    const response: any = await this.sendRequest('model/connectionStatus');
    // Backend returns { success, data: { connectionType, isConnected, ... } }
    // Unwrap it for consistency with client expectations
    if (response && response.data) {
      return { success: response.success, ...response.data };
    }
    return response;
  }

  /**
   * Get full model metadata (tables, columns, measures, relationships, etc.)
   * Includes retry logic to handle TOM cache warmup
   */
  async getModelMetadata(timeoutMs: number = 120000): Promise<any> {
    this.outputChannel?.appendLine(`\n[${new Date().toISOString()}] Getting model metadata with retry logic...`);

    let result: any = await this.sendRequest('model/getMetadata', {}, timeoutMs);

    // Check if result is empty or invalid (TOM cache may be warming up)
    if (!result || !result.success || !result.data || !result.data.model) {
      this.outputChannel?.appendLine(`[${new Date().toISOString()}] First attempt returned empty/invalid data. Waiting 500ms and retrying...`);
      console.log('[LSPModelService] First getModelMetadata attempt failed or returned empty. Retrying in 500ms...');

      // Wait 500ms for TOM cache to warm up
      await new Promise(resolve => setTimeout(resolve, 500));

      // Retry once
      this.outputChannel?.appendLine(`[${new Date().toISOString()}] Retrying getModelMetadata...`);
      result = await this.sendRequest('model/getMetadata', {}, timeoutMs);

      if (!result || !result.success || !result.data || !result.data.model) {
        this.outputChannel?.appendLine(`[${new Date().toISOString()}] Retry also returned empty/invalid data.`);
        console.warn('[LSPModelService] Second getModelMetadata attempt also failed or returned empty.');
      } else {
        this.outputChannel?.appendLine(`[${new Date().toISOString()}] Retry succeeded!`);
        console.log('[LSPModelService] Second getModelMetadata attempt succeeded.');
      }
    }

    return result;
  }

  /**
   * List all tables in the connected model
   */
  async listTables(): Promise<any> {
    return this.sendRequest('model/listTables');
  }

  /**
   * Get metadata for a specific column (optimized for webview loading)
   * Much more efficient than getModelMetadata for single column properties
   */
  async getColumnMetadata(tableName: string, columnName: string): Promise<any> {
    return this.sendRequest('model/getColumnMetadata', { tableName, columnName });
  }

  /**
   * Get metadata for a specific measure (optimized for webview loading)
   * Much more efficient than getModelMetadata for single measure properties
   * Includes synonyms, translations, KPI expressions, and more
   */
  async getMeasureMetadata(tableName: string, measureName: string): Promise<any> {
    return this.sendRequest('model/getMeasureMetadata', { tableName, measureName });
  }

  /**
   * Update column properties
   */
  async updateColumn(tableName: string, columnName: string, properties: any): Promise<any> {
    return this.sendRequest('model/updateColumn', {
      tableName,
      columnName,
      ...properties
    });
  }

  /**
   * Update measure properties
   */
  async updateMeasureProperties(tableName: string, measureName: string, properties: any): Promise<any> {
    return this.sendRequest('model/updateMeasure', {
      tableName,
      measureName,
      ...properties
    });
  }

  /**
   * Get all cultures (languages) in the model
   */
  async getCultures(): Promise<any> {
    return this.sendRequest('model/listCultures');
  }

  /**
   * Get all translations for a column across all cultures
   */
  async getColumnTranslations(tableName: string, columnName: string): Promise<any> {
    return this.sendRequest('model/listTranslations', { tableName, columnName });
  }

  /**
   * Update a translation for a column property in a specific culture
   */
  async updateColumnTranslation(
    tableName: string,
    columnName: string,
    cultureName: string,
    property: string,
    value: string
  ): Promise<any> {
    return this.sendRequest('model/updateTranslation', {
      tableName,
      columnName,
      cultureName,
      property,
      value
    });
  }

  /**
   * Get TMDL file path for a table/object (PBIP only)
   */
  async getTmdlFilePath(tableName: string, objectName?: string, objectType?: string): Promise<any> {
    return this.sendRequest('model/getTmdlFilePath', {
      tableName,
      objectName,
      objectType
    });
  }

  /**
   * Get TMDL content for a table (Fabric only)
   */
  async getTableTmdl(tableName: string, workspaceId?: string, datasetId?: string): Promise<any> {
    return this.sendRequest('model/getTableTmdl', {
      tableName,
      workspaceId,
      datasetId
    });
  }

  /**
   * Execute a DAX query
   */
  async executeDax(query: string): Promise<any> {
    // DAX execution not yet implemented in LSP
    console.warn('[LSPModelService] executeDax not yet implemented in LSP');
    return { success: false, error: 'Not implemented in LSP' };
  }

  /**
   * Health check
   */
  async health(): Promise<{ status: string; connected: boolean }> {
    try {
      const result = await this.ping();
      return {
        status: result.status || 'ready',
        connected: result.connected || false
      };
    } catch (error) {
      return {
        status: 'error',
        connected: false
      };
    }
  }

  // ============================================================================
  // HIERARCHY OPERATIONS
  // ============================================================================

  /**
   * Create a new hierarchy in a table
   */
  async createHierarchy(
    tableName: string,
    hierarchyName: string,
    levels: string[],
    description?: string
  ): Promise<any> {
    return this.sendRequest('model/createHierarchy', {
      tableName,
      hierarchyName,
      levels,
      description
    });
  }

  /**
   * Update an existing hierarchy
   */
  async updateHierarchy(
    tableName: string,
    hierarchyName: string,
    levels: string[]
  ): Promise<any> {
    return this.sendRequest('model/updateHierarchy', {
      tableName,
      hierarchyName,
      levels
    });
  }

  /**
   * Delete a hierarchy from a table
   */
  async deleteHierarchy(tableName: string, hierarchyName: string): Promise<any> {
    return this.sendRequest('model/deleteHierarchy', {
      tableName,
      hierarchyName
    });
  }

  /**
   * List all hierarchies in a table (or all tables if not specified)
   */
  async listHierarchies(tableName?: string): Promise<any> {
    return this.sendRequest('model/listHierarchies', tableName ? { tableName } : {});
  }

  // ============================================================================
  // ROLE OPERATIONS
  // ============================================================================

  /**
   * List all roles in the model
   */
  async listRoles(): Promise<any> {
    return this.sendRequest('model/listRoles');
  }

  // ============================================================================
  // PERSPECTIVE OPERATIONS
  // ============================================================================

  /**
   * List all perspectives in the model
   */
  async listPerspectives(): Promise<any> {
    return this.sendRequest('model/listPerspectives');
  }

  // ============================================================================
  // CALCULATION GROUP OPERATIONS
  // ============================================================================

  /**
   * List all calculation groups in the model
   */
  async listCalculationGroups(): Promise<any> {
    return this.sendRequest('model/listCalculationGroups');
  }

  // ============================================================================
  // TREE VIEW SUPPORT (Hierarchical API)
  // ============================================================================

  /**
   * Get model tree elements for hierarchical tree view
   * This is the new simplified API for Model Explorer
   */
  async getModelTreeElements(parentId?: string): Promise<any> {
    this.outputChannel?.appendLine(`\n[${new Date().toISOString()}] Getting tree elements for parentId: ${parentId || 'ROOT'}`);
    return this.sendRequest('powerbi/getModelMetadata', { parentId: parentId || null });
  }

  // ============================================================================
  // PBIR COMMANDS
  // ============================================================================

  /**
   * Returns the PBIR report hierarchy tree (pages → visuals → theme) for the
   * given PBIP project path or .pbip file path.
   */
  async getPbirTree(reportPath: string): Promise<any> {
    return this.sendRequest('model/pbir/getTree', { reportPath });
  }

  /**
   * Public wrapper around the private `sendRequest` method.
   * Used by webview panels (e.g. PbirWizardPanel) that need to call arbitrary LSP methods.
   */
  async executeRequest<T = any>(method: string, params: any = {}): Promise<T> {
    return this.sendRequest<T>(method, params);
  }
}
