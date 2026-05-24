import type { PageScore } from '../../contracts/scorePanel';
import type { VisualAuditFinding, VisualCapture } from '../types';

export interface VisualAuditInput {
  capture: VisualCapture;
  pageName: string;
  pageScore?: PageScore;
}

export interface VisualAuditProvider {
  readonly providerName: string;
  isConfigured(): Promise<boolean>;
  analyzeCapture(input: VisualAuditInput): Promise<VisualAuditFinding[]>;
}
