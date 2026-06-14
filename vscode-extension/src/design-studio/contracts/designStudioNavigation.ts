export const DESIGN_STUDIO_PANES = [
  'brief',
  'concept',
  'draft',
  'refinement',
  'materialize',
  'handoff',
  'compare',
] as const;

export type DesignStudioPaneId = typeof DESIGN_STUDIO_PANES[number];

export interface DesignStudioNavigationState {
  activePane: DesignStudioPaneId;
  selectedArtifactId?: string;
  selectedIterationId?: string;
}
