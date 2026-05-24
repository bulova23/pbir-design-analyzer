import type { ScoreResult } from '../contracts/scorePanel';

export interface GovernanceCheckResult {
  policyState?: string;
  blocked: boolean;
  reasons?: string[];
  evaluatedScore?: number;
  requiredThreshold?: number;
  policyNotes?: string;
}

export interface GovernanceExportData {
  reportPath: string;
  scoredAt: string;
  compositeScore: number;
  frameworkScores: Record<string, number>;
  governance: {
    passed: boolean;
    policyState?: string;
    evaluatedScore?: number;
    requiredThreshold?: number;
    reasons: string[];
    policyNotes?: string;
  };
}

const FRAMEWORK_LABELS: Record<string, string> = {
  gestaltScore: 'Gestalt',
  cognitiveLoadScore: 'Cognitive Load',
  dataInkScore: 'Data-Ink',
  accessibilityScore: 'Accessibility',
  visualBestPracticesScore: 'Visual Best Practices',
  stephenFewScore: 'Stephen Few',
  enterpriseGovernanceScore: 'Enterprise Governance',
  tufteScore: 'Tufte',
  graphicalPerceptionScore: 'Graphical Perception',
  densityScore: 'Density',
  narrativeScore: 'Narrative',
};

const FRAMEWORK_KEYS = Object.keys(FRAMEWORK_LABELS) as Array<keyof typeof FRAMEWORK_LABELS>;

export function buildGovernanceExportData(
  scoreResult: ScoreResult,
  governanceResult: GovernanceCheckResult,
): GovernanceExportData {
  const frameworkScores: Record<string, number> = {};
  for (const key of FRAMEWORK_KEYS) {
    const raw = scoreResult[key as keyof ScoreResult];
    if (typeof raw === 'number') {
      frameworkScores[FRAMEWORK_LABELS[key]] = Math.round(raw * 10) / 10;
    }
  }

  return {
    reportPath: scoreResult.reportPath,
    scoredAt: scoreResult.scoredAt,
    compositeScore: Math.round(scoreResult.compositeScore * 10) / 10,
    frameworkScores,
    governance: {
      passed: !governanceResult.blocked,
      policyState: governanceResult.policyState,
      evaluatedScore:
        governanceResult.evaluatedScore !== undefined
          ? Math.round(governanceResult.evaluatedScore * 10) / 10
          : undefined,
      requiredThreshold:
        governanceResult.requiredThreshold !== undefined
          ? Math.round(governanceResult.requiredThreshold * 10) / 10
          : undefined,
      reasons: governanceResult.reasons ?? [],
      policyNotes: governanceResult.policyNotes,
    },
  };
}

export function exportAsJson(data: GovernanceExportData): string {
  return JSON.stringify(data, null, 2);
}

export function exportAsMarkdown(data: GovernanceExportData): string {
  const lines: string[] = [];
  const gov = data.governance;
  const statusEmoji = gov.passed ? '✅' : '⛔';
  const statusText = gov.passed ? 'PASSED' : 'BLOCKED';

  lines.push('# PBIR Governance Report');
  lines.push('');
  lines.push(`**Report:** \`${data.reportPath}\``);
  lines.push(`**Scored:** ${data.scoredAt}`);
  lines.push('');
  lines.push(`## Composite Score: ${data.compositeScore} / 100`);
  lines.push('');
  lines.push('### Framework Scores');
  lines.push('');
  lines.push('| Framework | Score |');
  lines.push('|-----------|------:|');
  for (const [label, score] of Object.entries(data.frameworkScores)) {
    lines.push(`| ${label} | ${score} |`);
  }
  lines.push('');
  lines.push(`## Governance: ${statusEmoji} ${statusText}`);
  lines.push('');

  if (gov.evaluatedScore !== undefined && gov.requiredThreshold !== undefined) {
    lines.push(
      `Score **${gov.evaluatedScore}** vs required threshold **${gov.requiredThreshold}**.`,
    );
    lines.push('');
  }

  if (!gov.passed && gov.reasons.length > 0) {
    lines.push('### Blocked Reasons');
    lines.push('');
    for (const reason of gov.reasons) {
      lines.push(`- ${reason}`);
    }
    lines.push('');
  }

  if (gov.policyNotes) {
    lines.push(`**Policy notes:** ${gov.policyNotes}`);
    lines.push('');
  }

  return lines.join('\n');
}
