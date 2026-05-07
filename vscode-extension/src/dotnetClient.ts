/**
 * Legacy DotNetClient shim.
 *
 * The extension now uses DotNetBridge directly. This class exists to keep
 * older tests compiling until they are migrated.
 */
export class DotNetClient {
  constructor(_options?: { baseUrl?: string }) {
    // No-op: shim for legacy tests.
  }

  // Intentionally minimal; tests mock this type as needed.
  async getTableTmdl(_tableName: string, _workspaceId?: string, _datasetId?: string): Promise<any> {
    throw new Error('Not implemented');
  }

  async updateTableTmdl(_tableName: string, _content: string): Promise<any> {
    throw new Error('Not implemented');
  }

  async updateColumn(_tableName: string, _columnName: string, _properties: any): Promise<any> {
    throw new Error('Not implemented');
  }

  async updateMeasureProperties(_tableName: string, _measureName: string, _properties: any): Promise<any> {
    throw new Error('Not implemented');
  }

  async getMetadata(): Promise<any> {
    throw new Error('Not implemented');
  }

  async validateTmdlContent(_content: string): Promise<any> {
    throw new Error('Not implemented');
  }
}
