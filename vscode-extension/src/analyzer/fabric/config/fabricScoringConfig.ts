export interface FabricScoringConfigInput {
  review: {
    qualityScore: {
      base: number;
      minimum: number;
      penalties: {
        high: number;
        medium: number;
        low: number;
        info: number;
      };
    };
    findingConfidence: number;
    semanticModelSignalLimit: number;
  };
  readiness: {
    formulas: {
      layoutPortability: {
        visualCountThreshold: number;
        visualPenaltyPerExtra: number;
      };
      interactionPortability: {
        base: number;
        slicerPenalty: number;
        navigationPenalty: number;
        hiddenVisualPenalty: number;
        drillPathBonus: number;
      };
      narrativePortability: {
        narrativeWeight: number;
        actionabilityWeight: number;
      };
      semanticModelSuitability: {
        base: number;
        measureHintBonus: number;
        visualBonusCap: number;
        visualBonusPerVisual: number;
        pageIntentBonus: number;
      };
      navigationPortability: {
        base: number;
        navigationPenalty: number;
        slicerThreshold: number;
        slicerThresholdPenalty: number;
        navigationDriftPenalty: number;
      };
      governancePortability: {
        governanceScoreWeight: number;
        stableNavigationBonus: number;
        navigationDriftPenalty: number;
      };
      accessibilityPortability: {
        visualCountThreshold: number;
        visualPenaltyPerExtra: number;
        titlePresentBonus: number;
        titleMissingPenalty: number;
      };
      visualizationAsCodeOpportunity: {
        base: number;
        titleBonus: number;
        semanticColorBonus: number;
        measureHintBonus: number;
        hiddenVisualPenalty: number;
        visualCountThreshold: number;
        visualPenaltyPerExtra: number;
      };
    };
    thresholds: {
      pageCandidate: {
        strongCandidateScore: number;
        strongCandidateMaxBlockers: number;
        possibleCandidateScore: number;
        possibleCandidateMaxBlockers: number;
        redesignRequiredScore: number;
      };
      reportCandidate: {
        strongCandidateScore: number;
        possibleCandidateScore: number;
        redesignRequiredScore: number;
        minimumCandidatePages: number;
      };
      blockers: {
        slicerHeavyCount: number;
        accessibilityLowScore: number;
        navigationLowScore: number;
        layoutLowScore: number;
      };
      unsupportedPatterns: {
        navigationVisualCount: number;
      };
      positiveSignals: {
        focusedVisualCount: number;
        semanticModelStructuredScore: number;
        codeFirstOpportunityScore: number;
      };
      migrationNotes: {
        narrativeLowScore: number;
        navigationLowScore: number;
        semanticModelLowScore: number;
      };
      redesignAreas: {
        layoutLowScore: number;
        navigationLowScore: number;
        accessibilityLowScore: number;
        narrativeLowScore: number;
      };
      visualizationOpportunityScore: number;
    };
    findings: {
      goodCandidateConfidence: number;
      blockerConfidence: number;
      redesignConfidence: number;
      unsupportedPatternConfidence: number;
      visualizationOpportunityConfidence: number;
    };
  };
}

export interface FabricScoringConfig extends FabricScoringConfigInput {
  provenance: {
    source: 'builtin' | 'override';
    version: '0.6.0';
    overrideKeys: string[];
  };
}

export type FabricScoringConfigOverride = DeepPartial<FabricScoringConfigInput>;

type DeepPartial<T> = {
  [K in keyof T]?: T[K] extends Array<unknown>
    ? T[K]
    : T[K] extends object
      ? DeepPartial<T[K]>
      : T[K];
};

const DEFAULT_FABRIC_SCORING_CONFIG: FabricScoringConfigInput = {
  review: {
    qualityScore: {
      base: 82,
      minimum: 25,
      penalties: {
        high: 18,
        medium: 10,
        low: 5,
        info: 5,
      },
    },
    findingConfidence: 82,
    semanticModelSignalLimit: 6,
  },
  readiness: {
    formulas: {
      layoutPortability: {
        visualCountThreshold: 8,
        visualPenaltyPerExtra: 3,
      },
      interactionPortability: {
        base: 82,
        slicerPenalty: 7,
        navigationPenalty: 5,
        hiddenVisualPenalty: 10,
        drillPathBonus: 4,
      },
      narrativePortability: {
        narrativeWeight: 0.6,
        actionabilityWeight: 0.4,
      },
      semanticModelSuitability: {
        base: 52,
        measureHintBonus: 8,
        visualBonusCap: 6,
        visualBonusPerVisual: 3,
        pageIntentBonus: 8,
      },
      navigationPortability: {
        base: 84,
        navigationPenalty: 7,
        slicerThreshold: 3,
        slicerThresholdPenalty: 12,
        navigationDriftPenalty: 10,
      },
      governancePortability: {
        governanceScoreWeight: 0.8,
        stableNavigationBonus: 6,
        navigationDriftPenalty: 10,
      },
      accessibilityPortability: {
        visualCountThreshold: 8,
        visualPenaltyPerExtra: 4,
        titlePresentBonus: 6,
        titleMissingPenalty: 6,
      },
      visualizationAsCodeOpportunity: {
        base: 48,
        titleBonus: 10,
        semanticColorBonus: 8,
        measureHintBonus: 5,
        hiddenVisualPenalty: 8,
        visualCountThreshold: 10,
        visualPenaltyPerExtra: 3,
      },
    },
    thresholds: {
      pageCandidate: {
        strongCandidateScore: 75,
        strongCandidateMaxBlockers: 0,
        possibleCandidateScore: 60,
        possibleCandidateMaxBlockers: 1,
        redesignRequiredScore: 45,
      },
      reportCandidate: {
        strongCandidateScore: 75,
        possibleCandidateScore: 60,
        redesignRequiredScore: 45,
        minimumCandidatePages: 1,
      },
      blockers: {
        slicerHeavyCount: 4,
        accessibilityLowScore: 50,
        navigationLowScore: 50,
        layoutLowScore: 45,
      },
      unsupportedPatterns: {
        navigationVisualCount: 2,
      },
      positiveSignals: {
        focusedVisualCount: 6,
        semanticModelStructuredScore: 70,
        codeFirstOpportunityScore: 70,
      },
      migrationNotes: {
        narrativeLowScore: 60,
        navigationLowScore: 65,
        semanticModelLowScore: 65,
      },
      redesignAreas: {
        layoutLowScore: 60,
        navigationLowScore: 60,
        accessibilityLowScore: 60,
        narrativeLowScore: 60,
      },
      visualizationOpportunityScore: 55,
    },
    findings: {
      goodCandidateConfidence: 82,
      blockerConfidence: 88,
      redesignConfidence: 84,
      unsupportedPatternConfidence: 86,
      visualizationOpportunityConfidence: 76,
    },
  },
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function cloneConfig<T>(value: T): T {
  return JSON.parse(JSON.stringify(value)) as T;
}

function mergeConfig<T>(base: T, override?: DeepPartial<T>): T {
  if (!override) {
    return cloneConfig(base);
  }

  const merged = cloneConfig(base) as Record<string, unknown>;
  for (const [key, value] of Object.entries(override)) {
    if (value === undefined) {
      continue;
    }

    if (isRecord(value) && isRecord(merged[key])) {
      merged[key] = mergeConfig(merged[key], value as DeepPartial<Record<string, unknown>>);
      continue;
    }

    merged[key] = cloneConfig(value);
  }

  return merged as T;
}

function collectOverrideKeys(
  override: DeepPartial<FabricScoringConfigInput> | undefined,
  prefix = '',
): string[] {
  if (!override || !isRecord(override)) {
    return [];
  }

  const keys: string[] = [];
  for (const [key, value] of Object.entries(override)) {
    const nextPrefix = prefix ? `${prefix}.${key}` : key;
    if (isRecord(value)) {
      keys.push(...collectOverrideKeys(value as DeepPartial<FabricScoringConfigInput>, nextPrefix));
    } else if (value !== undefined) {
      keys.push(nextPrefix);
    }
  }

  return keys.sort((left, right) => left.localeCompare(right));
}

export function resolveFabricScoringConfig(
  override?: FabricScoringConfigOverride,
): FabricScoringConfig {
  const merged = mergeConfig<FabricScoringConfigInput>(DEFAULT_FABRIC_SCORING_CONFIG, override);
  const overrideKeys = collectOverrideKeys(override);

  return {
    ...merged,
    provenance: {
      source: overrideKeys.length > 0 ? 'override' : 'builtin',
      version: '0.6.0',
      overrideKeys,
    },
  };
}

export function getDefaultFabricScoringConfig(): FabricScoringConfig {
  return resolveFabricScoringConfig();
}
