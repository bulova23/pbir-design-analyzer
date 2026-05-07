/**
 * Custom LSP extensions for Power BI Modeling Language Server
 *
 * These custom requests extend the standard LSP protocol to support
 * model operations specific to Power BI (TOM) and TMDL.
 */

/**
 * Custom LSP request methods for model operations
 */
export namespace ModelRequests {
  // Health & Connection
  export const Ping = 'model/ping';
  export const Connect = 'model/connect';
  export const LoadPBIP = 'model/loadPBIP';
  export const ConnectionStatus = 'model/connectionStatus';

  // Metadata Queries
  export const GetModelMetadata = 'model/getMetadata';
  export const ListTables = 'model/listTables';

  // Update Operations
  export const UpdateColumn = 'model/updateColumn';
  export const UpdateMeasure = 'model/updateMeasure';

  // Translation Operations
  export const ListCultures = 'model/listCultures';
  export const ListTranslations = 'model/listTranslations';
  export const UpdateTranslation = 'model/updateTranslation';
}

/**
 * Connection request parameters
 */
export interface ConnectParams {
  workspace: string;
  semanticModel: string;
  accessToken: string;
}

/**
 * Connection response
 */
export interface ConnectResult {
  success: boolean;
  data?: {
    connectionType: string;
    workspace: string;
    model: string;
    tables: Array<{
      name: string;
      columnCount: number;
      measureCount: number;
      isHidden: boolean;
    }>;
  };
  error?: string;
}

/**
 * Load PBIP request parameters
 */
export interface LoadPBIPParams {
  projectPath: string;
}

/**
 * Model metadata result
 */
export interface GetModelMetadataResult {
  success: boolean;
  data?: {
    connectionType: string;
    model: any; // Full model tree structure
  };
  error?: string;
}

/**
 * Update column request parameters
 */
export interface UpdateColumnParams {
  tableName: string;
  columnName: string;
  description?: string;
  displayFolder?: string;
  formatString?: string;
  dataType?: string;
  dataCategory?: string;
  summarizeBy?: string;
  isHidden?: boolean;
  synonyms?: string[];
}

/**
 * Update measure request parameters
 */
export interface UpdateMeasureParams {
  tableName: string;
  measureName: string;
  expression?: string;
  description?: string;
  displayFolder?: string;
  formatString?: string;
  isHidden?: boolean;
}

/**
 * Translation list request parameters
 */
export interface ListTranslationsParams {
  tableName: string;
  columnName: string;
}

/**
 * Update translation request parameters
 */
export interface UpdateTranslationParams {
  tableName: string;
  columnName: string;
  cultureName: string;
  property: string;
  value: string;
}

/**
 * Generic LSP response wrapper
 */
export interface LSPResponse<T = any> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}
