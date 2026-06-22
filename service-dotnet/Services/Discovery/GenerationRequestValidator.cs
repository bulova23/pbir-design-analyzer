using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationRequestValidator
{
    internal GenerationRequestValidationResult Validate(GenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var missingSections = new List<string>();
        var missingFields = new List<string>();
        var missingInputs = new List<string>();
        var unsupportedTargets = new List<string>();
        var unsupportedSchemaVersions = new List<string>();
        var compatibilityFailures = new List<string>();

        ValidateNotBlank(request.SchemaVersion, "schemaVersion", missingFields);
        if (!string.IsNullOrWhiteSpace(request.SchemaVersion) &&
            !string.Equals(request.SchemaVersion, GenerationRequestContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            unsupportedSchemaVersions.Add(request.SchemaVersion);
        }

        ValidateNotBlank(request.RequestId, "requestId", missingFields);
        ValidateNotBlank(request.SourceDesignPackageRef, "sourceDesignPackageRef", missingFields);

        if (request.TargetArtifactProfile is null)
        {
            missingSections.Add("targetArtifactProfile");
        }
        else
        {
            ValidateNotBlank(request.TargetArtifactProfile.ProfileId, "targetArtifactProfile.profileId", missingFields);

            if (!GenerationRequestTargetProfileCatalog.IsSupportedArtifactType(request.TargetArtifactProfile.ArtifactType))
            {
                unsupportedTargets.Add(GenerationRequestTargetProfileCatalog.ToContractValue(request.TargetArtifactProfile.ArtifactType));
            }

            if (!string.IsNullOrWhiteSpace(request.TargetArtifactProfile.ProfileId) &&
                !GenerationRequestTargetProfileCatalog.IsSupportedProfileId(request.TargetArtifactProfile.ProfileId))
            {
                unsupportedTargets.Add(request.TargetArtifactProfile.ProfileId);
            }

            if (!GenerationRequestTargetProfileCatalog.IsCompatibleProfile(request.TargetArtifactProfile))
            {
                compatibilityFailures.Add("targetArtifactProfile.sourceExperienceType is incompatible with the requested artifact profile.");
            }
        }

        if (request.GenerationMode is null)
        {
            missingSections.Add("generationMode");
        }
        else
        {
            ValidateNotBlank(request.GenerationMode.Authority, "generationMode.authority", missingFields);
            if (!request.GenerationMode.ReviewRequired)
            {
                compatibilityFailures.Add("generationMode.reviewRequired must stay true because generation remains review-gated.");
            }
        }

        if (request.DesignIntent is null)
        {
            missingSections.Add("designIntent");
        }
        else
        {
            ValidateNotBlank(request.DesignIntent.PrimaryAudience, "designIntent.primaryAudience", missingFields);
            ValidateNotBlank(request.DesignIntent.BusinessOutcome, "designIntent.businessOutcome", missingFields);

            if (request.DesignIntent.AnalyticalFlow is null)
            {
                missingSections.Add("designIntent.analyticalFlow");
            }
            else
            {
                ValidateNotBlank(request.DesignIntent.AnalyticalFlow.Question, "designIntent.analyticalFlow.question", missingFields);
                ValidateNotBlank(request.DesignIntent.AnalyticalFlow.Investigation, "designIntent.analyticalFlow.investigation", missingFields);
                ValidateNotBlank(request.DesignIntent.AnalyticalFlow.Evidence, "designIntent.analyticalFlow.evidence", missingFields);
                ValidateNotBlank(request.DesignIntent.AnalyticalFlow.Decision, "designIntent.analyticalFlow.decision", missingFields);
            }
        }

        if (request.StructuralIntent is null)
        {
            missingSections.Add("structuralIntent");
        }
        else
        {
            if (request.StructuralIntent.Pages is null || request.StructuralIntent.Pages.Count == 0)
            {
                missingSections.Add("structuralIntent.pages");
            }
            else
            {
                foreach (var page in request.StructuralIntent.Pages)
                {
                    ValidateNotBlank(page.Name, "structuralIntent.pages.name", missingFields);
                    ValidateNotBlank(page.Purpose, "structuralIntent.pages.purpose", missingFields);
                    ValidateNotBlank(page.NavigationIntent, "structuralIntent.pages.navigationIntent", missingFields);
                }
            }

            if (request.StructuralIntent.Navigation is null)
            {
                missingSections.Add("structuralIntent.navigation");
            }
            else
            {
                if (request.StructuralIntent.Navigation.Hierarchy is null || request.StructuralIntent.Navigation.Hierarchy.Count == 0)
                {
                    missingSections.Add("structuralIntent.navigation.hierarchy");
                }

                if (request.StructuralIntent.Navigation.WorkflowPath is null || request.StructuralIntent.Navigation.WorkflowPath.Count == 0)
                {
                    missingSections.Add("structuralIntent.navigation.workflowPath");
                }
            }
        }

        if (request.DataIntent is null)
        {
            missingSections.Add("dataIntent");
        }
        else
        {
            if (request.DataIntent.Kpis is null || request.DataIntent.Kpis.Count == 0)
            {
                missingSections.Add("dataIntent.kpis");
            }
            else
            {
                foreach (var kpi in request.DataIntent.Kpis)
                {
                    ValidateNotBlank(kpi.Name, "dataIntent.kpis.name", missingFields);
                    ValidateNotBlank(kpi.Purpose, "dataIntent.kpis.purpose", missingFields);
                    ValidateNotBlank(kpi.Grouping, "dataIntent.kpis.grouping", missingFields);
                }
            }

            if (request.DataIntent.Filters is null)
            {
                missingSections.Add("dataIntent.filters");
            }

            if (request.DataIntent.SemanticBinding is null)
            {
                missingSections.Add("dataIntent.semanticBinding");
            }
            else
            {
                ValidateNotBlank(request.DataIntent.SemanticBinding.SemanticModelRef, "dataIntent.semanticBinding.semanticModelRef", missingFields);
                ValidateNotBlank(request.DataIntent.SemanticBinding.SemanticModelLabel, "dataIntent.semanticBinding.semanticModelLabel", missingFields);
            }
        }

        if (request.SuccessContract is null)
        {
            missingSections.Add("successContract");
        }
        else
        {
            if (request.SuccessContract.BusinessSuccessCriteria is null || request.SuccessContract.BusinessSuccessCriteria.Count == 0)
            {
                missingSections.Add("successContract.businessSuccessCriteria");
            }

            if (request.SuccessContract.AnalyticalSuccessCriteria is null || request.SuccessContract.AnalyticalSuccessCriteria.Count == 0)
            {
                missingSections.Add("successContract.analyticalSuccessCriteria");
            }

            if (request.SuccessContract.ValidationRequirements is null || request.SuccessContract.ValidationRequirements.Count == 0)
            {
                missingSections.Add("successContract.validationRequirements");
            }
        }

        if (request.Provenance is null)
        {
            missingSections.Add("provenance");
        }
        else
        {
            ValidateNotBlank(request.Provenance.SourceDesignPackageRef, "provenance.sourceDesignPackageRef", missingFields);

            if (request.Provenance.Lineage is null || request.Provenance.Lineage.Count == 0)
            {
                missingSections.Add("provenance.lineage");
            }
            else
            {
                foreach (var reference in request.Provenance.Lineage)
                {
                    ValidateNotBlank(reference.Stage, "provenance.lineage.stage", missingFields);
                    ValidateNotBlank(reference.ReferenceId, "provenance.lineage.referenceId", missingFields);
                    ValidateNotBlank(reference.Label, "provenance.lineage.label", missingFields);
                }
            }

            if (request.Provenance.AdapterMetadata is null)
            {
                missingSections.Add("provenance.adapterMetadata");
            }
            else
            {
                ValidateNotBlank(request.Provenance.AdapterMetadata.AdapterFamily, "provenance.adapterMetadata.adapterFamily", missingFields);
                ValidateNotBlank(request.Provenance.AdapterMetadata.ExecutionMode, "provenance.adapterMetadata.executionMode", missingFields);

                if (!string.Equals(request.Provenance.AdapterMetadata.AdapterFamily, GenerationRequestContract.ProviderNeutralAdapterFamily, StringComparison.Ordinal))
                {
                    compatibilityFailures.Add("provenance.adapterMetadata.adapterFamily must stay providerNeutral.");
                }

                if (!string.Equals(request.Provenance.AdapterMetadata.ExecutionMode, GenerationRequestContract.PromptSegmentsOnlyExecutionMode, StringComparison.Ordinal))
                {
                    compatibilityFailures.Add("provenance.adapterMetadata.executionMode must stay promptSegmentsOnly until provider adapters exist.");
                }

                if (request.Provenance.AdapterMetadata.ProviderSpecificExecution)
                {
                    compatibilityFailures.Add("provenance.adapterMetadata.providerSpecificExecution must stay false until provider adapters exist.");
                }
            }
        }

        if (request.ReviewPolicy is null)
        {
            missingSections.Add("reviewPolicy");
        }
        else
        {
            if (!request.ReviewPolicy.DesignApprovalRequired)
            {
                compatibilityFailures.Add("reviewPolicy.designApprovalRequired must stay true.");
            }

            if (!request.ReviewPolicy.GenerationApprovalRequired)
            {
                compatibilityFailures.Add("reviewPolicy.generationApprovalRequired must stay true.");
            }

            if (!request.ReviewPolicy.AnalyzerReviewRequired)
            {
                compatibilityFailures.Add("reviewPolicy.analyzerReviewRequired must stay true.");
            }
        }

        if (request.Provenance is not null &&
            !string.IsNullOrWhiteSpace(request.SourceDesignPackageRef) &&
            !string.IsNullOrWhiteSpace(request.Provenance.SourceDesignPackageRef) &&
            !string.Equals(request.SourceDesignPackageRef, request.Provenance.SourceDesignPackageRef, StringComparison.Ordinal))
        {
            compatibilityFailures.Add("provenance.sourceDesignPackageRef must match sourceDesignPackageRef.");
        }

        if (request.DataIntent?.SemanticBinding is not null &&
            request.Provenance?.Lineage is not null &&
            request.Provenance.Lineage.All(reference => !string.Equals(reference.ReferenceId, request.DataIntent.SemanticBinding.SemanticModelRef, StringComparison.Ordinal)))
        {
            compatibilityFailures.Add("dataIntent.semanticBinding.semanticModelRef must resolve from provenance.lineage.");
        }

        var diagnostics = new GenerationRequestValidationDiagnostics(
            MissingRequiredSections: missingSections.Distinct(StringComparer.Ordinal).ToArray(),
            MissingRequiredFields: missingFields.Distinct(StringComparer.Ordinal).ToArray(),
            MissingInputs: missingInputs.Distinct(StringComparer.Ordinal).ToArray(),
            UnsupportedTargetProfiles: unsupportedTargets.Distinct(StringComparer.Ordinal).ToArray(),
            UnsupportedSchemaVersions: unsupportedSchemaVersions.Distinct(StringComparer.Ordinal).ToArray(),
            CompatibilityFailures: compatibilityFailures.Distinct(StringComparer.Ordinal).ToArray());

        return new GenerationRequestValidationResult(diagnostics);
    }
    private static void ValidateNotBlank(string? value, string fieldName, ICollection<string> missingFields)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingFields.Add(fieldName);
        }
    }
}
