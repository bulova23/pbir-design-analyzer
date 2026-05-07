/**
 * TypeScript interfaces for column data and webview communication
 */

export type ConnectionType = 'PBIP' | 'Fabric';

export interface ColumnPropertyData {
    // Basic properties
    tableName: string;
    columnName: string;
    dataType: string;
    description?: string;
    displayFolder?: string;
    formatString?: string;
    summarizeBy?: string;
    isHidden?: boolean;
    dataCategory?: string;
    
    // Sort & Source
    sortByColumn?: string;
    sourceColumn?: string;
    
    // Metadata - read-only
    daxIdentifier?: string;
    errorMessage?: string;
    objectType?: string;
    state?: string;
    lineageTag?: string;
    sourceLineageTag?: string;
    
    // Synonyms & Collections
    synonyms?: string;
    annotations?: any[];
    extendedProperties?: any[];
    
    // Options
    encodingHint?: string;
    displayOrdinal?: number;
    sourceProviderType?: string;
    isAvailableInMDX?: boolean;
    keepUniqueRows?: boolean;
    isUnique?: boolean;
    isKey?: boolean;
    isNullable?: boolean;
    isDataTypeInferred?: boolean;
    alignment?: string;
    tableDetailPosition?: number;
    isDefaultLabel?: boolean;
    isDefaultImage?: boolean;
    type?: string;
    
    // Perspectives & Security
    inPerspectives?: { [perspectiveName: string]: boolean };
    translatedNames?: any[];
    translatedDescriptions?: any[];
    translatedDisplayFolders?: any[];
    objectLevelSecurity?: any;
}

export interface ColumnWebviewMessage {
    command: 'saveColumn' | 'aiAssist' | 'loadData';
    payload?: any;
}
