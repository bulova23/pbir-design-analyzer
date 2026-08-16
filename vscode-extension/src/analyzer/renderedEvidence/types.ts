export type RenderedEvidenceProviderId = 'pbiLens';

export type RenderedEvidenceProviderStatus =
  | 'NotInstalled'
  | 'InstalledNoProgrammaticSurface'
  | 'CliAvailable'
  | 'McpAvailable'
  | 'Available'
  | 'Misconfigured'
  | 'Error';

export interface RenderedEvidenceCapabilities {
  extensionDetected: boolean;
  publicApiAvailable: boolean;
  cliAvailable: boolean;
  mcpAvailable: boolean;
  pageScreenshotAvailable: boolean;
  reportContextAvailable: boolean;
  visualContextAvailable: boolean;
}

export interface RenderedEvidenceCapabilityReport {
  providerId: RenderedEvidenceProviderId;
  displayName: string;
  extensionId: string;
  installed: boolean;
  activated: boolean;
  version?: string;
  status: RenderedEvidenceProviderStatus;
  capabilities: RenderedEvidenceCapabilities;
  diagnostics: string[];
}

export type RenderedEvidenceKind = 'pageScreenshot' | 'visualScreenshot' | 'reportContext' | 'visualContext';

export interface RenderedEvidenceArtifact {
  providerId: RenderedEvidenceProviderId;
  kind: RenderedEvidenceKind;
  reportId: string;
  pageId?: string;
  pageName?: string;
  capturedAt: string;
  sha256?: string;
  artifactPath?: string;
}

export interface RenderedEvidenceRequest {
  reportId: string;
  pageId?: string;
  pageName?: string;
}

export interface RenderedEvidenceResult {
  providerId: RenderedEvidenceProviderId;
  status: RenderedEvidenceProviderStatus;
  evidence: RenderedEvidenceArtifact[];
  diagnostics: string[];
}

export interface IRenderedDesignEvidenceProvider {
  getCapabilityReport(): RenderedEvidenceCapabilityReport;
  getEvidence(request: RenderedEvidenceRequest): Promise<RenderedEvidenceResult>;
}
