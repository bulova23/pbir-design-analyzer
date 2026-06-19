using PowerBIModelingService.Services.DesignStudio.Models;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class DiscoveryDesignStudioAdapterService
{
    private static readonly DraftArtifactStatus DefaultDraftStatus = new(
        Isolation: "Isolated",
        Reviewability: "Reviewable",
        ProductionState: "NonProduction");

    internal DiscoveryDesignStudioStartingPoint CreateStartingPoint(
        DiscoveryProfile profile,
        OpportunityCatalog catalog,
        RecommendationSet recommendations,
        string selectedRecommendationId,
        string threadId)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(recommendations);

        if (string.IsNullOrWhiteSpace(selectedRecommendationId))
        {
            throw new ArgumentException("A selected recommendation identifier is required.", nameof(selectedRecommendationId));
        }

        if (string.IsNullOrWhiteSpace(threadId))
        {
            throw new ArgumentException("A Design Studio thread identifier is required.", nameof(threadId));
        }

        var selectedRecommendation = recommendations.PrimaryRecommendations
            .Concat(recommendations.AlternateRecommendations)
            .FirstOrDefault(recommendation => string.Equals(
                recommendation.RecommendationId,
                selectedRecommendationId,
                StringComparison.Ordinal));
        if (selectedRecommendation is null)
        {
            throw new InvalidOperationException($"Recommendation '{selectedRecommendationId}' was not found.");
        }

        var blueprint = selectedRecommendation.ExperienceBlueprint
            ?? throw new InvalidOperationException("Design Studio seeding requires an attached Experience Blueprint.");
        var opportunity = catalog.Opportunities.FirstOrDefault(candidate =>
            string.Equals(candidate.OpportunityId, blueprint.Provenance.OpportunityId, StringComparison.Ordinal));

        var lineage = BuildLineage(profile, opportunity, selectedRecommendation, blueprint);
        var brief = BuildDesignBrief(threadId, selectedRecommendation, blueprint, lineage);
        var concept = BuildConcept(threadId, brief, selectedRecommendation, blueprint, lineage);
        var draftArtifacts = BuildDraft(threadId, brief, concept, selectedRecommendation, blueprint, lineage);

        return new DiscoveryDesignStudioStartingPoint(
            SelectedRecommendationId: selectedRecommendation.RecommendationId,
            DesignBrief: brief,
            Concept: concept,
            Draft: draftArtifacts.Draft,
            DraftPages: draftArtifacts.PageArtifacts,
            DraftLayouts: draftArtifacts.LayoutArtifacts,
            DraftNavigationArtifacts: draftArtifacts.NavigationArtifacts);
    }

    private static IReadOnlyList<DesignArtifactLineageLink> BuildLineage(
        DiscoveryProfile profile,
        OpportunityCandidate? opportunity,
        DiscoveryRecommendation recommendation,
        ExperienceBlueprint blueprint)
    {
        return
        [
            new("semanticModel", "semanticModel", profile.SemanticModelReferenceId, "Semantic model source"),
            new("discoveryProfile", "discoveryProfile", profile.DiscoveryProfileReferenceId, "Discovery Profile"),
            new("opportunity", "opportunity", opportunity?.OpportunityId ?? blueprint.Provenance.OpportunityId, opportunity?.Name ?? "Opportunity"),
            new("recommendation", "recommendation", recommendation.RecommendationId, recommendation.RecommendationName),
            new("experienceBlueprint", "experienceBlueprint", blueprint.BlueprintId, recommendation.RecommendationName),
        ];
    }

    private static DesignBrief BuildDesignBrief(
        string threadId,
        DiscoveryRecommendation recommendation,
        ExperienceBlueprint blueprint,
        IReadOnlyList<DesignArtifactLineageLink> lineage)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var metadata = CreateMetadata(
            id: $"design-brief:{threadId}",
            threadId: threadId,
            kind: "designBrief",
            timestamp: timestamp,
            lineage: lineage);

        var keyDecisions = BuildKeyDecisions(blueprint);
        var dimensions = blueprint.SuggestedGlobalFilters.Count > 0
            ? blueprint.SuggestedGlobalFilters
            : profileFallbackDimensions(blueprint);

        return new DesignBrief(
            Metadata: metadata,
            Audience: recommendation.ExpectedAudience,
            BusinessObjective: recommendation.ExpectedBusinessOutcome,
            KeyDecisions: keyDecisions,
            PrimaryKpis: blueprint.PrimaryKpis,
            Dimensions: dimensions,
            IntendedStory: BuildIntendedStory(recommendation, blueprint),
            SuccessCriteria: blueprint.SuccessCriteriaSeed,
            ReportType: MapReportType(recommendation.RecommendedExperienceType),
            NavigationExpectations: BuildNavigationExpectations(blueprint),
            ConsumptionContext: $"Discovery-backed starting point for {recommendation.RecommendationName}.",
            DecisionCadence: InferDecisionCadence(recommendation.RecommendedExperienceType),
            NarrativeRisksOrConstraints: recommendation.LimitingFactors,
            RequiredEvidenceDomains: ["semanticModel", "experienceBlueprint"],
            TargetAnalyzableSurfaceFamily: MapSurfaceFamily(recommendation.RecommendedExperienceType));

        static IReadOnlyList<string> profileFallbackDimensions(ExperienceBlueprint blueprint)
        {
            return blueprint.RecommendedPages
                .SelectMany(page => page.SuggestedFilters)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }

    private static ReportConcept BuildConcept(
        string threadId,
        DesignBrief brief,
        DiscoveryRecommendation recommendation,
        ExperienceBlueprint blueprint,
        IReadOnlyList<DesignArtifactLineageLink> lineage)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var chapterMap = new ReportChapterMapConcept(
            Chapters:
            [
                new ChapterConcept(
                    Id: "chapter-blueprint-flow",
                    Title: "Blueprint flow",
                    Objective: "Follow the experience blueprint in a Design Studio-ready structure.",
                    PageRecommendationIds: blueprint.RecommendedPages.Select((_, index) => PageRecommendationId(index)).ToArray())
            ]);

        var pageRecommendations = blueprint.RecommendedPages
            .Select((page, index) => new PageRecommendationConcept(
                Id: PageRecommendationId(index),
                Title: page.PageName,
                Objective: page.PageIntent,
                ChapterId: "chapter-blueprint-flow",
                RecommendedKpis: blueprint.PrimaryKpis))
            .ToArray();

        var analyticalFlow = new AnalyticalFlowConcept(
            Steps:
            [
                new("flow-question", "Question", blueprint.AnalyticalFlow.Question, PageRecommendationId(0)),
                new("flow-investigation", "Investigation", blueprint.AnalyticalFlow.Investigation, PageRecommendationId(Math.Min(1, pageRecommendations.Length - 1))),
                new("flow-evidence", "Evidence", blueprint.AnalyticalFlow.Evidence, PageRecommendationId(Math.Min(2, pageRecommendations.Length - 1))),
                new("flow-decision", "Decision", blueprint.AnalyticalFlow.Decision, PageRecommendationId(Math.Max(0, pageRecommendations.Length - 1))),
            ]);

        var reportConceptId = $"report-concept:{threadId}";
        var sourceBriefVersionId = $"{brief.Metadata.Id}@v{brief.Metadata.Version}";
        var reportConceptVersionId = $"{reportConceptId}@v1";
        var pageConcepts = pageRecommendations.Select((recommendationPage, index) => new PageConcept(
            Metadata: CreateMetadata(
                id: $"page-concept:{threadId}:{index}",
                threadId: threadId,
                kind: "pageConcept",
                timestamp: timestamp,
                lineage: lineage),
            ReportConceptId: reportConceptId,
            SourceBriefVersionId: sourceBriefVersionId,
            SourceReportConceptVersionId: reportConceptVersionId,
            Title: recommendationPage.Title,
            IntendedPurpose: recommendationPage.Objective,
            TargetAudienceOrRole: brief.Audience,
            PrimaryKpis: recommendationPage.RecommendedKpis,
            SupportingDimensions: brief.Dimensions,
            IntendedStoryQuestion: analyticalFlow.Steps[Math.Min(index, analyticalFlow.Steps.Count - 1)].Objective,
            NavigationRole: index == 0 ? "entry" : index == pageRecommendations.Length - 1 ? "decision" : "supporting",
            RelatedChapterId: recommendationPage.ChapterId))
            .ToArray();

        var kpiNodes = blueprint.PrimaryKpis
            .Select((kpi, index) => new KpiHierarchyNodeConcept(
                Id: $"kpi-{index}",
                Label: kpi,
                Level: index == 0 ? "primary" : "supporting",
                ChildNodeIds: []))
            .ToArray();

        var navigationSections = blueprint.NavigationIntent.Sequence
            .Select((label, index) => new NavigationSectionConcept(
                Id: $"nav-{index}",
                Label: label,
                PageRecommendationIds: [PageRecommendationId(Math.Min(index, pageRecommendations.Length - 1))]))
            .ToArray();

        var alternateConcepts = new[]
        {
            new AlternateReportConcept(
                Id: "concept-blueprint-aligned",
                Label: "Blueprint-aligned flow",
                Summary: $"Follows the {recommendation.RecommendationName} blueprint sequence directly.",
                ChapterMap: chapterMap,
                PageRecommendations: pageRecommendations,
                KpiHierarchyNodes: kpiNodes,
                SupportingDimensions: brief.Dimensions,
                NavigationPattern: NormalizePattern(blueprint.NavigationIntent.Flow),
                NavigationRationale: "Preserves the recommended blueprint sequence as the initial baseline.",
                NavigationSections: navigationSections,
                AnalyticalFlow: analyticalFlow),
            new AlternateReportConcept(
                Id: "concept-scan-first",
                Label: "Scan-first KPI flow",
                Summary: $"Starts with KPI emphasis and then expands into {recommendation.RecommendationName.ToLowerInvariant()}.",
                ChapterMap: chapterMap,
                PageRecommendations: pageRecommendations,
                KpiHierarchyNodes: kpiNodes,
                SupportingDimensions: brief.Dimensions,
                NavigationPattern: "hubAndSpoke",
                NavigationRationale: "Creates a scan-first baseline while staying within the same blueprint evidence model.",
                NavigationSections: navigationSections,
                AnalyticalFlow: analyticalFlow),
        };

        return new ReportConcept(
            Metadata: CreateMetadata(
                id: reportConceptId,
                threadId: threadId,
                kind: "reportConcept",
                timestamp: timestamp,
                lineage: lineage),
            BriefId: brief.Metadata.Id,
            SourceBriefId: brief.Metadata.Id,
            SourceBriefVersionId: sourceBriefVersionId,
            Summary: alternateConcepts[0].Summary,
            ChapterMap: chapterMap,
            PageRecommendations: pageRecommendations,
            PageConcepts: pageConcepts,
            KpiHierarchy: new KpiHierarchyConcept(
                Metadata: CreateMetadata(
                    id: $"kpi-hierarchy:{threadId}",
                    threadId: threadId,
                    kind: "kpiHierarchyConcept",
                    timestamp: timestamp,
                    lineage: lineage),
                ReportConceptId: reportConceptId,
                SourceBriefVersionId: sourceBriefVersionId,
                SourceReportConceptVersionId: reportConceptVersionId,
                Nodes: kpiNodes,
                SupportingDimensions: brief.Dimensions),
            NavigationStructure: new NavigationConcept(
                Metadata: CreateMetadata(
                    id: $"navigation:{threadId}",
                    threadId: threadId,
                    kind: "navigationConcept",
                    timestamp: timestamp,
                    lineage: lineage),
                ReportConceptId: reportConceptId,
                SourceBriefVersionId: sourceBriefVersionId,
                SourceReportConceptVersionId: reportConceptVersionId,
                Pattern: NormalizePattern(blueprint.NavigationIntent.Flow),
                Rationale: "Derived from the selected recommendation blueprint without approving a baseline.",
                Sections: navigationSections),
            AnalyticalFlow: analyticalFlow,
            AlternateConcepts: alternateConcepts,
            PreferredBaselineConceptId: null,
            ApprovedBaselineConceptId: null,
            Comparison: null);

        static string PageRecommendationId(int index) => $"page-{index + 1}";
    }

    private static DraftBuildResult BuildDraft(
        string threadId,
        DesignBrief brief,
        ReportConcept concept,
        DiscoveryRecommendation recommendation,
        ExperienceBlueprint blueprint,
        IReadOnlyList<DesignArtifactLineageLink> lineage)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var draftId = $"draft-report:{threadId}";
        var sourceBriefVersionId = $"{brief.Metadata.Id}@v{brief.Metadata.Version}";
        var sourceConceptVersionId = $"{concept.Metadata.Id}@v{concept.Metadata.Version}";
        var sourceNavigationVersionId = $"{concept.NavigationStructure.Metadata.Id}@v{concept.NavigationStructure.Metadata.Version}";

        var pageArtifacts = concept.PageConcepts.Select((pageConcept, index) => new DraftPageArtifact(
            Metadata: CreateMetadata(
                id: $"draft-page:{threadId}:{index}",
                threadId: threadId,
                kind: "draftPageArtifact",
                timestamp: timestamp,
                lineage: lineage),
            DraftReportArtifactId: draftId,
            PageConceptId: pageConcept.Metadata.Id,
            SourceBriefVersionId: sourceBriefVersionId,
            SourceConceptVersionId: sourceConceptVersionId,
            SourcePageConceptVersionId: $"{pageConcept.Metadata.Id}@v{pageConcept.Metadata.Version}",
            StructureSummary: $"{pageConcept.Title} draft seed frames {pageConcept.IntendedPurpose.ToLowerInvariant()}",
            RecommendedVisualRoles: blueprint.RecommendedPages[Math.Min(index, blueprint.RecommendedPages.Count - 1)].SuggestedVisualTypes,
            DraftStatus: DefaultDraftStatus))
            .ToArray();

        var layoutArtifacts = pageArtifacts.Select((pageArtifact, index) => new DraftLayoutArtifact(
            Metadata: CreateMetadata(
                id: $"draft-layout:{threadId}:{index}",
                threadId: threadId,
                kind: "draftLayoutArtifact",
                timestamp: timestamp,
                lineage: lineage),
            DraftPageArtifactId: pageArtifact.Metadata.Id,
            PageConceptId: pageArtifact.PageConceptId,
            SourceBriefVersionId: sourceBriefVersionId,
            SourceConceptVersionId: sourceConceptVersionId,
            SourcePageConceptVersionId: pageArtifact.SourcePageConceptVersionId,
            LayoutType: index == 0 ? "heroKpiGrid" : "detailAnalysisGrid",
            Title: $"{concept.PageConcepts[index].Title} layout",
            KpiBindings: concept.PageConcepts[index].PrimaryKpis,
            Zones: ["header", "primaryCanvas", "supportingCanvas"],
            DraftStatus: DefaultDraftStatus))
            .ToArray();

        var navigationArtifact = new DraftNavigationArtifact(
            Metadata: CreateMetadata(
                id: $"draft-navigation:{threadId}",
                threadId: threadId,
                kind: "draftNavigationArtifact",
                timestamp: timestamp,
                lineage: lineage),
            DraftReportArtifactId: draftId,
            NavigationConceptId: concept.NavigationStructure.Metadata.Id,
            SourceBriefVersionId: sourceBriefVersionId,
            SourceConceptVersionId: sourceConceptVersionId,
            SourceNavigationConceptVersionId: sourceNavigationVersionId,
            FrameworkType: concept.NavigationStructure.Pattern,
            Sections: concept.PageConcepts.Select((pageConcept, index) => new DraftNavigationSectionArtifact(
                Id: $"draft-nav-section:{index}",
                Label: pageConcept.Title,
                PageArtifactId: pageArtifacts[index].Metadata.Id,
                PageConceptId: pageConcept.Metadata.Id)).ToArray(),
            DraftStatus: DefaultDraftStatus);

        var draft = new DraftReportArtifact(
            Metadata: CreateMetadata(
                id: draftId,
                threadId: threadId,
                kind: "draftReportArtifact",
                timestamp: timestamp,
                lineage: lineage),
            BriefId: brief.Metadata.Id,
            ConceptId: concept.Metadata.Id,
            SourceBriefVersionId: sourceBriefVersionId,
            SourceConceptVersionId: sourceConceptVersionId,
            SourceNavigationConceptVersionId: sourceNavigationVersionId,
            PageArtifactIds: pageArtifacts.Select(artifact => artifact.Metadata.Id).ToArray(),
            LayoutArtifactIds: layoutArtifacts.Select(artifact => artifact.Metadata.Id).ToArray(),
            NavigationArtifactIds: [navigationArtifact.Metadata.Id],
            Summary: $"Discovery-backed draft seed for {recommendation.RecommendationName}.",
            DraftStatus: DefaultDraftStatus);

        return new DraftBuildResult(draft, pageArtifacts, layoutArtifacts, [navigationArtifact]);
    }

    private static DesignArtifactMetadata CreateMetadata(
        string id,
        string threadId,
        string kind,
        DateTimeOffset timestamp,
        IReadOnlyList<DesignArtifactLineageLink> lineage)
    {
        return new DesignArtifactMetadata(
            Id: id,
            ThreadId: threadId,
            Kind: kind,
            Version: 1,
            LifecycleState: DesignArtifactLifecycleState.Draft,
            ApprovalState: DesignArtifactApprovalState.NotSubmitted,
            ApprovalKind: DesignArtifactApprovalKind.DesignApproval,
            CreatedAt: timestamp,
            UpdatedAt: timestamp,
            AuthorSource: DesignArtifactAuthorSource.System,
            Provenance: new DesignArtifactProvenance(
                Source: "discoveryWizard",
                Timestamp: timestamp,
                Lineage: lineage,
                Notes:
                [
                    "Created from a selected Report Discovery Wizard recommendation.",
                    "Design Studio owns all downstream approvals and workflow progression.",
                    "No validation approval, deployable asset, or mutation authority was created."
                ]),
            ValidationLinkage: null);
    }

    private static IReadOnlyList<string> BuildKeyDecisions(ExperienceBlueprint blueprint)
    {
        return
        [
            blueprint.AnalyticalFlow.Question,
            blueprint.AnalyticalFlow.Investigation,
            blueprint.AnalyticalFlow.Decision,
        ];
    }

    private static string BuildIntendedStory(DiscoveryRecommendation recommendation, ExperienceBlueprint blueprint)
    {
        return $"Track {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()} by starting with {blueprint.AnalyticalFlow.Question.ToLowerInvariant()} and ending with {blueprint.AnalyticalFlow.Decision.ToLowerInvariant()}";
    }

    private static string BuildNavigationExpectations(ExperienceBlueprint blueprint)
    {
        var sequence = blueprint.NavigationIntent.Sequence.Count > 0
            ? string.Join(" -> ", blueprint.NavigationIntent.Sequence)
            : blueprint.NavigationIntent.Flow;
        return $"Use a {blueprint.NavigationIntent.Flow.ToLowerInvariant()} path: {sequence}.";
    }

    private static string InferDecisionCadence(OpportunityExperienceType experienceType)
    {
        return experienceType == OpportunityExperienceType.OperationalMonitoringExperience ? "Daily" : "Weekly";
    }

    private static string MapSurfaceFamily(OpportunityExperienceType experienceType)
    {
        return experienceType == OpportunityExperienceType.FabricApp || experienceType == OpportunityExperienceType.FabricDataApp
            ? "fabricApp"
            : "pbir";
    }

    private static string MapReportType(OpportunityExperienceType experienceType)
    {
        return experienceType switch
        {
            OpportunityExperienceType.OperationalMonitoringExperience => "OperationalMonitoring",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "NarrativeBriefing",
            _ => "Dashboard",
        };
    }

    private static string NormalizePattern(string flow)
    {
        var normalized = flow.Trim().ToLowerInvariant();
        if (normalized.Contains("question", StringComparison.Ordinal) || normalized.Contains("investigation", StringComparison.Ordinal))
        {
            return "guidedInvestigation";
        }

        if (normalized.Contains("summary", StringComparison.Ordinal))
        {
            return "hubAndSpoke";
        }

        return "guidedFlow";
    }

    private sealed record DraftBuildResult(
        DraftReportArtifact Draft,
        IReadOnlyList<DraftPageArtifact> PageArtifacts,
        IReadOnlyList<DraftLayoutArtifact> LayoutArtifacts,
        IReadOnlyList<DraftNavigationArtifact> NavigationArtifacts);
}
