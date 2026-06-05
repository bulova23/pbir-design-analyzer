import type {
  RefactoringAffectedScope,
  RefactoringDomain,
  RefactoringEvidenceLink,
  RefactoringTradeoff,
} from '../../contracts/scorePanel';
import type { RefactoringContext } from './refactoringContextBuilder';

export interface RefactoringScenarioOptionCandidate {
  optionId?: string;
  label?: string;
  title: string;
  summary: string;
  proposedChanges: string[];
  affectedScope: RefactoringAffectedScope;
  rationale: string;
  evidenceLinks?: RefactoringEvidenceLink[];
  businessImpact: string;
  tradeoffs?: RefactoringTradeoff[];
  confidence?: number;
}

export interface RefactoringScenarioCandidate {
  scenarioId?: string;
  domain: RefactoringDomain;
  title: string;
  summary: string;
  options: RefactoringScenarioOptionCandidate[];
}

export interface RefactoringProviderResponse {
  status: 'available' | 'refused' | 'error';
  scenarios?: RefactoringScenarioCandidate[];
  refusalReason?: string;
}

export interface RefactoringProvider {
  providerName: string;
  isConfigured(): Promise<boolean>;
  generate(input: {
    context: RefactoringContext;
    requestedDomains: RefactoringDomain[];
    optionCount: number;
  }): Promise<RefactoringProviderResponse>;
}
