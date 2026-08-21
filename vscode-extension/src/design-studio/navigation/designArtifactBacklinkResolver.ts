import type {
  CrossPageNarrativeAnalyzerOutput,
  DesignArtifactBacklinkRecord,
  DraftNavigationArtifact,
  DraftState,
  NormalizedFindingImpactArea,
  PageConcept,
  RefinementAnalyzerSource,
  RefinementBacklinkArtifactKind,
  StableArtifactBacklinkIdentity,
} from '../state/refinementStoreTypes';

export interface BacklinkResolutionRequest {
  analyzerSource: RefinementAnalyzerSource;
  analyzerReferenceId: string;
  pageNames: string[];
  impactAreas: NormalizedFindingImpactArea[];
  findingIds: string[];
  narrative?: CrossPageNarrativeAnalyzerOutput;
  stableArtifactIdentities?: StableArtifactBacklinkIdentity[];
}

interface ArtifactCandidate {
  artifactId: string;
  artifactKind: RefinementBacklinkArtifactKind;
  artifactVersionId: string;
  stableIdentity: StableArtifactBacklinkIdentity;
  pageName?: string;
  reason: string;
}

function toVersionId(artifact: { id: string; version: number }): string {
  return `${artifact.id}@v${artifact.version}`;
}

function normalize(value: string | undefined): string {
  return value?.trim().toLowerCase() ?? '';
}

function matchesPageName(pageName: string, pageConcept: PageConcept): boolean {
  return normalize(pageConcept.title) === normalize(pageName);
}

function collectRequestedPageNames(state: DraftState, request: BacklinkResolutionRequest): string[] {
  const pageNames = [...request.pageNames];

  if (request.narrative) {
    pageNames.push(...request.narrative.narrativePath);
    for (const gap of request.narrative.gaps) {
      pageNames.push(...gap.affectedPageNames);
    }
  }

  if (pageNames.length > 0) {
    return Array.from(new Set(pageNames.map((value) => value.trim()).filter((value) => value.length > 0)));
  }

  return state.concept.pageConcepts.map((pageConcept) => pageConcept.title);
}

function buildPageCandidates(
  state: DraftState,
  requestedPageNames: string[],
): ArtifactCandidate[] {
  const candidates: ArtifactCandidate[] = [];

  for (const pageName of requestedPageNames) {
    const pageConcept = state.concept.pageConcepts.find((candidate) => matchesPageName(pageName, candidate));
    if (!pageConcept) {
      continue;
    }

    const draftPage = state.pageArtifacts.find((artifact) => artifact.pageConceptId === pageConcept.id);

    candidates.push({
      artifactId: pageConcept.id,
      artifactKind: 'pageConcept',
      artifactVersionId: toVersionId(pageConcept),
      stableIdentity: {
        designArtifactId: pageConcept.id,
        designArtifactVersionId: toVersionId(pageConcept),
        draftArtifactId: draftPage?.id ?? pageConcept.id,
        draftArtifactVersionId: draftPage ? toVersionId(draftPage) : toVersionId(pageConcept),
      },
      pageName: pageConcept.title,
      reason: `Linked to page concept for ${pageConcept.title}.`,
    });

    if (draftPage) {
      candidates.push({
        artifactId: draftPage.id,
        artifactKind: 'draftPageArtifact',
        artifactVersionId: toVersionId(draftPage),
        stableIdentity: {
          designArtifactId: pageConcept.id,
          designArtifactVersionId: toVersionId(pageConcept),
          draftArtifactId: draftPage.id,
          draftArtifactVersionId: toVersionId(draftPage),
        },
        pageName: pageConcept.title,
        reason: `Linked to draft page artifact for ${pageConcept.title}.`,
      });
    }

    const draftLayout = state.layoutArtifacts.find((artifact) => artifact.pageConceptId === pageConcept.id);
    if (draftLayout) {
      candidates.push({
        artifactId: draftLayout.id,
        artifactKind: 'draftLayoutArtifact',
        artifactVersionId: toVersionId(draftLayout),
        stableIdentity: {
          designArtifactId: pageConcept.id,
          designArtifactVersionId: toVersionId(pageConcept),
          draftArtifactId: draftLayout.id,
          draftArtifactVersionId: toVersionId(draftLayout),
        },
        pageName: pageConcept.title,
        reason: `Linked to draft layout artifact for ${pageConcept.title}.`,
      });
    }
  }

  return candidates;
}

function shouldLinkNavigation(request: BacklinkResolutionRequest): boolean {
  return request.analyzerSource === 'crossPageNarrative'
    || request.impactAreas.includes('navigation')
    || request.impactAreas.includes('storytelling');
}

function shouldLinkKpiHierarchy(request: BacklinkResolutionRequest): boolean {
  return request.impactAreas.includes('kpiEffectiveness')
    || request.impactAreas.includes('benchmark')
    || request.impactAreas.includes('actionability');
}

function buildNavigationCandidate(navigationArtifact: DraftNavigationArtifact): ArtifactCandidate {
  return {
    artifactId: navigationArtifact.navigationConceptId ?? navigationArtifact.id,
    artifactKind: 'navigationConcept',
    artifactVersionId: navigationArtifact.sourceNavigationConceptVersionId ?? toVersionId(navigationArtifact),
    stableIdentity: {
      designArtifactId: navigationArtifact.navigationConceptId ?? navigationArtifact.id,
      designArtifactVersionId: navigationArtifact.sourceNavigationConceptVersionId ?? toVersionId(navigationArtifact),
      draftArtifactId: navigationArtifact.id,
      draftArtifactVersionId: toVersionId(navigationArtifact),
    },
    reason: 'Linked to navigation concept due to navigation or story-flow impact.',
  };
}

function buildStableIdentityCandidates(
  request: BacklinkResolutionRequest,
): ArtifactCandidate[] {
  return (request.stableArtifactIdentities ?? []).flatMap((identity) => ([
    {
      artifactId: identity.designArtifactId,
      artifactKind: 'pageConcept' as const,
      artifactVersionId: identity.designArtifactVersionId,
      stableIdentity: identity,
      reason: 'Linked through stable design artifact identity.',
    },
    {
      artifactId: identity.draftArtifactId,
      artifactKind: 'draftPageArtifact' as const,
      artifactVersionId: identity.draftArtifactVersionId,
      stableIdentity: identity,
      reason: 'Linked through stable draft artifact identity.',
    },
  ]));
}

export function resolveDesignArtifactBacklinks(
  state: DraftState,
  request: BacklinkResolutionRequest,
): DesignArtifactBacklinkRecord[] {
  const requestedPageNames = collectRequestedPageNames(state, request);
  const candidates = [
    ...buildStableIdentityCandidates(request),
    ...buildPageCandidates(state, requestedPageNames),
    ...(shouldLinkNavigation(request) ? state.navigationArtifacts.map(buildNavigationCandidate) : []),
    ...(shouldLinkKpiHierarchy(request)
      ? [{
          artifactId: state.concept.kpiHierarchy.id,
          artifactKind: 'kpiHierarchyConcept' as const,
          artifactVersionId: toVersionId(state.concept.kpiHierarchy),
          stableIdentity: {
            designArtifactId: state.concept.kpiHierarchy.id,
            designArtifactVersionId: toVersionId(state.concept.kpiHierarchy),
            draftArtifactId: state.currentDraft.id,
            draftArtifactVersionId: toVersionId(state.currentDraft),
          },
          reason: 'Linked to KPI hierarchy concept due to KPI or benchmark impact.',
        }]
      : []),
  ];

  const deduped = new Map<string, DesignArtifactBacklinkRecord>();
  for (const candidate of candidates) {
    const key = `${candidate.artifactKind}:${candidate.artifactVersionId}`;
    if (deduped.has(key)) {
      continue;
    }

    deduped.set(key, {
      analyzerSource: request.analyzerSource,
      analyzerReferenceId: request.analyzerReferenceId,
      artifactId: candidate.artifactId,
      artifactKind: candidate.artifactKind,
      artifactVersionId: candidate.artifactVersionId,
      stableIdentity: candidate.stableIdentity,
      pageName: candidate.pageName,
      reason: candidate.reason,
      linkedFindingIds: request.findingIds,
    });
  }

  return Array.from(deduped.values());
}
