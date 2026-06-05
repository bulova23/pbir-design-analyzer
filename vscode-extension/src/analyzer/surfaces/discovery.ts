import * as path from 'path';
import { resolvePbirProjectPath } from '../project/pathing';
import { detectFabricAppSurface } from './fabricAppDiscovery';
import type { AnalyzableSurface, SurfaceDiscoveryResult } from './types';

function buildPbirSurface(projectPath: string): AnalyzableSurface {
  return {
    surfaceType: 'pbirReport',
    displayName: path.basename(projectPath),
    sourceLocation: projectPath,
    availableEvidenceKinds: ['pbirMetadata', 'interaction', 'navigation', 'semanticModel', 'portability'],
    availableAnalyzerTypes: ['pbirDesignReview', 'fabricAppReadiness'],
    availableAnalyzerProfiles: ['default', 'migrationReadiness'],
    analysisCapabilities: ['findings', 'evidence', 'remediation', 'governanceSignals'],
    governanceCapabilities: ['analytics'],
  };
}

export function detectAnalyzableSurface(selectionPath: string): SurfaceDiscoveryResult {
  const projectPath = resolvePbirProjectPath(selectionPath);
  if (projectPath) {
    return {
      status: 'supported',
      surface: buildPbirSurface(projectPath),
    };
  }

  const normalizedPath = selectionPath.toLowerCase();
  if (
    normalizedPath.endsWith('.pbip') ||
    normalizedPath.endsWith('.report') ||
    normalizedPath.endsWith(`${path.sep}definition.pbir`) ||
    normalizedPath.endsWith(`${path.sep}definition${path.sep}report.json`)
  ) {
    return {
      status: 'supported',
      surface: buildPbirSurface(selectionPath),
    };
  }

  const fabricAppResult = detectFabricAppSurface(selectionPath);
  if (fabricAppResult.status !== 'unsupported' || fabricAppResult.reasonCode !== 'unsupportedSurface') {
    return fabricAppResult;
  }

  return {
    status: 'unsupported',
    reasonCode: 'unsupportedSurface',
    reason: 'Only PBIR report surfaces are supported in this release slice. Select a PBIR report or PBIP project.',
  };
}
