import type {
  DesignProviderCapabilityKind,
  DesignStudioArtifactKind,
} from '../contracts/designStudioModels';

export type DesignProviderTrustPosture = 'advisoryOnly';
export type DesignProviderProvenanceRequirements = 'required' | 'optional';
export type DesignProviderFailureBehavior = 'degradeGracefully' | 'reportUnavailableButContinue';

export interface DesignProviderWorkflowConstraints {
  requiresApproval: true;
  requiresValidation: true;
  allowsMaterialization: false;
  allowsReportMutation: false;
  allowsPbirAssetGeneration: false;
  allowsAnalyzableSurfaceCreation: false;
}

export interface DesignProviderCapability {
  providerId: string;
  providerDisplayName: string;
  capabilityId: string;
  capabilityKind: DesignProviderCapabilityKind;
  supportedArtifactKinds: DesignStudioArtifactKind[];
  supportedSurfaceFamilies: string[];
  requiresExternalService: boolean;
  supportsOfflineOperation: boolean;
  trustPosture: DesignProviderTrustPosture;
  provenanceRequirements: DesignProviderProvenanceRequirements;
  failureBehavior: DesignProviderFailureBehavior;
  workflowConstraints: DesignProviderWorkflowConstraints;
}

export interface DesignStudioProviderRegistration {
  providerId: string;
  displayName: string;
  capabilities: DesignProviderCapability[];
}

export interface DesignProviderCapabilityFilter {
  capabilityKind?: DesignProviderCapabilityKind;
  artifactKind?: DesignStudioArtifactKind;
  surfaceFamily?: string;
}

export interface DesignProviderStatus {
  providerId: string;
  isRegistered: boolean;
  degradation: 'none' | 'providerUnavailable';
  canContinueCoreWorkflow: boolean;
}

export const DESIGN_PROVIDER_WORKFLOW_CONSTRAINTS: DesignProviderWorkflowConstraints = Object.freeze({
  requiresApproval: true,
  requiresValidation: true,
  allowsMaterialization: false,
  allowsReportMutation: false,
  allowsPbirAssetGeneration: false,
  allowsAnalyzableSurfaceCreation: false,
});

export function createDesignProviderCapability(
  input: Omit<DesignProviderCapability, 'workflowConstraints'>,
): DesignProviderCapability {
  return {
    ...input,
    workflowConstraints: DESIGN_PROVIDER_WORKFLOW_CONSTRAINTS,
  };
}

export function designProviderCapabilitySupportsArtifactKind(
  capability: DesignProviderCapability,
  artifactKind: DesignStudioArtifactKind,
): boolean {
  return capability.supportedArtifactKinds.includes(artifactKind);
}

export function designProviderCapabilitySupportsSurfaceFamily(
  capability: DesignProviderCapability,
  surfaceFamily: string,
): boolean {
  return capability.supportedSurfaceFamilies.includes(surfaceFamily);
}

export class DesignProviderRegistry {
  private readonly providers = new Map<string, DesignStudioProviderRegistration>();

  registerProvider(provider: DesignStudioProviderRegistration): void {
    this.providers.set(provider.providerId, provider);
  }

  listProviders(): DesignStudioProviderRegistration[] {
    return [...this.providers.values()];
  }

  listCapabilities(): DesignProviderCapability[] {
    return this.listProviders().flatMap((provider) => provider.capabilities);
  }

  findCapabilities(filter: DesignProviderCapabilityFilter = {}): DesignProviderCapability[] {
    return this.listCapabilities().filter((capability) => {
      if (filter.capabilityKind && capability.capabilityKind !== filter.capabilityKind) {
        return false;
      }
      if (filter.artifactKind && !designProviderCapabilitySupportsArtifactKind(capability, filter.artifactKind)) {
        return false;
      }
      if (filter.surfaceFamily && !designProviderCapabilitySupportsSurfaceFamily(capability, filter.surfaceFamily)) {
        return false;
      }
      return true;
    });
  }

  getProviderStatus(providerId: string): DesignProviderStatus {
    if (!this.providers.has(providerId)) {
      return {
        providerId,
        isRegistered: false,
        degradation: 'providerUnavailable',
        canContinueCoreWorkflow: true,
      };
    }

    return {
      providerId,
      isRegistered: true,
      degradation: 'none',
      canContinueCoreWorkflow: true,
    };
  }
}
