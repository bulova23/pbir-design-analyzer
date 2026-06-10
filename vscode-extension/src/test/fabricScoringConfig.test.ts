import { classifyPageCandidateState } from '../analyzer/fabric/readiness/readinessScoring';
import {
  getDefaultFabricScoringConfig,
  resolveFabricScoringConfig,
} from '../analyzer/fabric/config/fabricScoringConfig';

describe('fabricScoringConfig', () => {
  it('exposes deterministic builtin defaults with provenance metadata', () => {
    const config = getDefaultFabricScoringConfig();

    expect(config.review.qualityScore.base).toBe(82);
    expect(config.review.semanticModelSignalLimit).toBe(6);
    expect(config.readiness.thresholds.pageCandidate.strongCandidateScore).toBe(75);
    expect(config.provenance).toEqual({
      source: 'builtin',
      version: '0.6.0',
      overrideKeys: [],
    });
  });

  it('records override provenance and keeps override scope explicit', () => {
    const config = resolveFabricScoringConfig({
      review: {
        qualityScore: {
          base: 90,
        },
      },
      readiness: {
        thresholds: {
          pageCandidate: {
            strongCandidateScore: 65,
          },
        },
      },
    });

    expect(config.review.qualityScore.base).toBe(90);
    expect(config.readiness.thresholds.pageCandidate.strongCandidateScore).toBe(65);
    expect(config.provenance).toEqual({
      source: 'override',
      version: '0.6.0',
      overrideKeys: [
        'readiness.thresholds.pageCandidate.strongCandidateScore',
        'review.qualityScore.base',
      ],
    });
  });

  it('supports bounded internal overrides without changing default classifier semantics', () => {
    expect(classifyPageCandidateState(70, 0)).toBe('possibleCandidate');

    const overrideConfig = resolveFabricScoringConfig({
      readiness: {
        thresholds: {
          pageCandidate: {
            strongCandidateScore: 65,
          },
        },
      },
    });

    expect(classifyPageCandidateState(70, 0, overrideConfig)).toBe('strongCandidate');
  });
});
