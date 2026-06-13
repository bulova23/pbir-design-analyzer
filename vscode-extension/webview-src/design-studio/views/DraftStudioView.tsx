import React from 'react';
import type {
  DraftLayoutArtifact,
  DraftNavigationArtifact,
  DraftPageArtifact,
  DraftReportArtifact,
} from '../../../src/design-studio/contracts/designStudioModels';
import type { DraftProviderCapabilityPlaceholder } from '../../../src/design-studio/providers/draftProviderAdapter';

interface DraftStudioViewProps {
  canGenerateDrafts: boolean;
  currentDraft?: DraftReportArtifact;
  pageArtifacts: DraftPageArtifact[];
  layoutArtifacts: DraftLayoutArtifact[];
  navigationArtifacts: DraftNavigationArtifact[];
  providerCapabilities: DraftProviderCapabilityPlaceholder[];
  onGenerateDrafts(): void;
}

export function DraftStudioView({
  canGenerateDrafts,
  currentDraft,
  pageArtifacts,
  layoutArtifacts,
  navigationArtifacts,
  providerCapabilities,
  onGenerateDrafts,
}: DraftStudioViewProps) {
  return (
    <section>
      <h1>Draft Studio</h1>
      {!canGenerateDrafts ? (
        <p>Draft generation is blocked until the Design Brief and Concept baseline are approved.</p>
      ) : (
        <p>Draft Studio produces isolated, reviewable, non-production draft artifacts only.</p>
      )}

      <button
        type='button'
        disabled={!canGenerateDrafts}
        onClick={onGenerateDrafts}
      >
        Generate Draft Artifacts
      </button>

      {currentDraft ? (
        <>
          <h2>{currentDraft.summary}</h2>
          <p>Draft status: {currentDraft.draftStatus.productionState}</p>
          <p>Page drafts: {pageArtifacts.length}</p>
          <p>Layout drafts: {layoutArtifacts.length}</p>
          <p>Navigation drafts: {navigationArtifacts.length}</p>
        </>
      ) : null}

      {providerCapabilities.length > 0 ? (
        <>
          <h2>Provider capabilities</h2>
          <ul>
            {providerCapabilities.map((capability) => (
              <li key={capability.capabilityId}>
                {capability.providerDisplayName}: {capability.capabilityKind}
              </li>
            ))}
          </ul>
        </>
      ) : (
        <p>No draft providers installed. Draft Studio still works with system-generated artifacts.</p>
      )}
    </section>
  );
}
