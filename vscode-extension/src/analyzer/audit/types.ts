export type VisualAuditFindingType = 'objective' | 'strongHeuristic' | 'stylePreference';
export type VisualAuditSeverity = 'critical' | 'warning' | 'info';
export type VisualAuditConfidence = 'high' | 'medium' | 'low';
export type VisualCaptureSource = 'upload' | 'browser';
export type VisualAuditIssueSource = 'renderedLayout' | 'metadataModel';

export interface VisualCapture {
  captureId: string;
  pageName: string;
  stateName?: string;
  fileName: string;
  storedPath: string;
  source: VisualCaptureSource;
  capturedAt: string;
  originalPath?: string;
}

export interface VisualAuditFinding {
  findingId: string;
  pageName: string;
  captureId: string;
  findingType: VisualAuditFindingType;
  severity: VisualAuditSeverity;
  confidence: VisualAuditConfidence;
  issueSource?: VisualAuditIssueSource;
  text: string;
  recommendation?: string;
  regionHint?: string;
}

export interface VisualAuditPageCoverage {
  pageName: string;
  captures: VisualCapture[];
  findings: VisualAuditFinding[];
}

export interface VisualAuditSession {
  reportPath: string;
  reportKey: string;
  createdAt: string;
  updatedAt: string;
  pages: VisualAuditPageCoverage[];
  unmatchedCaptures: VisualCapture[];
}
