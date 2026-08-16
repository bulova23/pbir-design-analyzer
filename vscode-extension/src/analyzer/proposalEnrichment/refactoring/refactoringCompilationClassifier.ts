import type {
  RefactoringCompilationHint,
  RefactoringScenario,
  RefactoringScenarioOption,
} from '../../contracts/scorePanel';

interface HintRule {
  category: RefactoringCompilationHint['category'];
  patterns: RegExp[];
  rationale: string;
}

const HINT_RULES: HintRule[] = [
  {
    category: 'alignment',
    patterns: [/\balign(?:ed|ment)?\b/i, /\bbaseline\b/i, /\bline up\b/i],
    rationale: 'The recommendation maps to an existing deterministic alignment opportunity.',
  },
  {
    category: 'spacing',
    patterns: [/\bspacing\b/i, /\bwhitespace\b/i, /\bgutter\b/i],
    rationale: 'The recommendation maps to an existing deterministic spacing opportunity.',
  },
  {
    category: 'grid',
    patterns: [/\bgrid\b/i, /\bcolumn\b/i],
    rationale: 'The recommendation maps to an existing deterministic grid opportunity.',
  },
  {
    category: 'title',
    patterns: [/\btitle\b/i, /\bheading\b/i, /\bhierarchy\b/i],
    rationale: 'The recommendation maps to an existing deterministic title opportunity.',
  },
  {
    category: 'navigation',
    patterns: [/\bnavigation\b/i, /\bnav\b/i, /\bmenu\b/i, /\btab\b/i],
    rationale: 'The recommendation maps to an existing deterministic navigation opportunity.',
  },
];

function containsPattern(text: string, patterns: RegExp[]): boolean {
  return patterns.some((pattern) => pattern.test(text));
}

function hintsForChange(change: string): RefactoringCompilationHint[] {
  return HINT_RULES
    .filter((rule) => containsPattern(change, rule.patterns))
    .map((rule) => ({
      category: rule.category,
      confidence: 0.8,
      rationale: rule.rationale,
      supportedScopes: ['visual', 'page', 'crossPage', 'report'],
    }));
}

export function buildRefactoringDeterministicHints(proposedChanges: string[]): RefactoringCompilationHint[] {
  const uniqueHints = new Map<RefactoringCompilationHint['category'], RefactoringCompilationHint>();

  for (const change of proposedChanges) {
    for (const entry of hintsForChange(change)) {
      if (!uniqueHints.has(entry.category)) {
        uniqueHints.set(entry.category, entry);
      }
    }
  }

  return [...uniqueHints.values()];
}

function classifyOption(option: RefactoringScenarioOption): RefactoringScenarioOption {
  const matchedCategories = new Set<RefactoringCompilationHint['category']>();
  let matchedChangeCount = 0;

  for (const change of option.proposedChanges) {
    const changeHints = hintsForChange(change);
    if (changeHints.length > 0) {
      matchedChangeCount += 1;
      for (const changeHint of changeHints) {
        matchedCategories.add(changeHint.category);
      }
    }
  }

  const hints = buildRefactoringDeterministicHints(option.proposedChanges)
    .filter((entry) => matchedCategories.has(entry.category));

  if (hints.length === 0) {
    return {
      ...option,
      compilation: {
        status: 'advisoryOnly',
        hints: [],
      },
    };
  }

  return {
    ...option,
    compilation: {
      status: 'compilable',
      coverage: matchedChangeCount === option.proposedChanges.length ? 'full' : 'partial',
      hints,
    },
  };
}

export function classifyRefactoringScenario(scenario: RefactoringScenario): RefactoringScenario {
  return {
    ...scenario,
    options: scenario.options.map(classifyOption),
  };
}
