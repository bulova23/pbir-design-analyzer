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
        var brief = BuildDesignBrief(threadId, selectedRecommendation, opportunity, blueprint, lineage);
        var concept = BuildConcept(threadId, brief, selectedRecommendation, opportunity, blueprint, lineage);
        var draftArtifacts = BuildDraft(threadId, brief, concept, selectedRecommendation, opportunity, blueprint, lineage);

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
        OpportunityCandidate? opportunity,
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
            IntendedStory: BuildIntendedStory(recommendation, opportunity, blueprint),
            SuccessCriteria: blueprint.SuccessCriteriaSeed,
            ReportType: MapReportType(recommendation.RecommendedExperienceType),
            NavigationExpectations: BuildNavigationExpectations(recommendation, opportunity, blueprint),
            ConsumptionContext: BuildConsumptionContext(recommendation, opportunity),
            DecisionCadence: InferDecisionCadence(recommendation, opportunity),
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
        OpportunityCandidate? opportunity,
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

        var alternateConcepts = BuildAlternateConcepts(
            brief,
            recommendation,
            opportunity,
            blueprint,
            chapterMap,
            pageRecommendations,
            kpiNodes,
            navigationSections,
            analyticalFlow);

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
        OpportunityCandidate? opportunity,
        ExperienceBlueprint blueprint,
        IReadOnlyList<DesignArtifactLineageLink> lineage)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var draftId = $"draft-report:{threadId}";
        var sourceBriefVersionId = $"{brief.Metadata.Id}@v{brief.Metadata.Version}";
        var sourceConceptVersionId = $"{concept.Metadata.Id}@v{concept.Metadata.Version}";
        var sourceNavigationVersionId = $"{concept.NavigationStructure.Metadata.Id}@v{concept.NavigationStructure.Metadata.Version}";

        var pageArtifacts = concept.PageConcepts.Select((pageConcept, index) =>
        {
            var seed = BuildDraftSeed(recommendation, opportunity, blueprint, pageConcept, index);
            return new DraftPageArtifact(
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
                StructureSummary: seed.StructureSummary,
                RecommendedVisualRoles: blueprint.RecommendedPages[Math.Min(index, blueprint.RecommendedPages.Count - 1)].SuggestedVisualTypes,
                DraftStatus: DefaultDraftStatus);
        })
            .ToArray();

        var layoutArtifacts = pageArtifacts.Select((pageArtifact, index) =>
        {
            var seed = BuildDraftSeed(recommendation, opportunity, blueprint, concept.PageConcepts[index], index);
            return new DraftLayoutArtifact(
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
                LayoutType: seed.LayoutType,
                Title: seed.LayoutTitle,
                KpiBindings: concept.PageConcepts[index].PrimaryKpis,
                Zones: seed.Zones,
                DraftStatus: DefaultDraftStatus);
        })
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

    private static string BuildIntendedStory(
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity,
        ExperienceBlueprint blueprint)
    {
        var cadence = InferDecisionCadence(recommendation, opportunity).ToLowerInvariant();
        var posture = GetExperiencePosture(recommendation.RecommendedExperienceType).ToLowerInvariant();
        return $"{recommendation.ExpectedAudience} uses this {posture} because it exists to {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()} during a {cadence} review rhythm, starting with {blueprint.AnalyticalFlow.Question.ToLowerInvariant()} and ending with {blueprint.AnalyticalFlow.Decision.ToLowerInvariant()}";
    }

    private static string BuildNavigationExpectations(
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity,
        ExperienceBlueprint blueprint)
    {
        var sequence = blueprint.NavigationIntent.Sequence.Count > 0
            ? string.Join(" -> ", blueprint.NavigationIntent.Sequence)
            : blueprint.NavigationIntent.Flow;
        var cadence = InferDecisionCadence(recommendation, opportunity).ToLowerInvariant();
        return $"Use a {blueprint.NavigationIntent.Flow.ToLowerInvariant()} workflow path so {recommendation.ExpectedAudience.ToLowerInvariant()} can move through {sequence} at a {cadence} cadence without losing the intended decision sequence.";
    }

    private static string BuildConsumptionContext(DiscoveryRecommendation recommendation, OpportunityCandidate? opportunity)
    {
        var scope = opportunity?.Category.ToString() ?? recommendation.RecommendedExperienceType.ToString();
        return $"Discovery-backed starting point for {recommendation.RecommendationName} that preserves {scope} intent as advisory-only design framing.";
    }

    private static string InferDecisionCadence(DiscoveryRecommendation recommendation, OpportunityCandidate? opportunity)
    {
        var text = $"{recommendation.RecommendationName} {recommendation.ExpectedBusinessOutcome} {opportunity?.BusinessOutcome}";

        if (ContainsAny(text, "daily", "queue", "backlog", "sla", "exception", "monitor"))
        {
            return "Daily";
        }

        if (ContainsAny(text, "weekly", "forecast", "planning cycle", "plan"))
        {
            return "Weekly";
        }

        if (ContainsAny(text, "monthly", "quarterly", "board"))
        {
            return "Monthly";
        }

        if (ContainsAny(text, "investigate", "root cause", "deep dive", "hypothesis"))
        {
            return "Episodic";
        }

        return recommendation.RecommendedExperienceType == OpportunityExperienceType.OperationalMonitoringExperience ? "Daily" : "Weekly";
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
            OpportunityExperienceType.ExecutiveDashboard => "ExecutiveDashboard",
            OpportunityExperienceType.OperationalMonitoringExperience => "OperationalMonitoring",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "InvestigativeWorkspace",
            OpportunityExperienceType.FabricApp => "Application",
            OpportunityExperienceType.FabricDataApp => "ExplorationApplication",
            OpportunityExperienceType.PbirReport => "NarrativeReport",
            _ => "AdvisoryDesign",
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

    private static IReadOnlyList<AlternateReportConcept> BuildAlternateConcepts(
        DesignBrief brief,
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity,
        ExperienceBlueprint blueprint,
        ReportChapterMapConcept chapterMap,
        IReadOnlyList<PageRecommendationConcept> pageRecommendations,
        IReadOnlyList<KpiHierarchyNodeConcept> kpiNodes,
        IReadOnlyList<NavigationSectionConcept> navigationSections,
        AnalyticalFlowConcept analyticalFlow)
    {
        return recommendation.RecommendedExperienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard =>
            [
                CreateAlternateConcept("concept-briefing-path", "Leadership briefing path", $"Leads with planning or leadership framing before branching into {recommendation.RecommendationName.ToLowerInvariant()}.", "hubAndSpoke", "Keeps the experience optimized for fast leadership alignment before deeper follow-up.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
                CreateAlternateConcept("concept-variance-path", "Variance review path", $"Organizes the experience around the variance checkpoints that shape {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()}.", "guidedReview", "Prioritizes KPI review, variance interpretation, and explicit follow-up checkpoints.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
                CreateAlternateConcept("concept-follow-up-path", "Follow-up checkpoint path", $"Shifts the emphasis toward the owners and checkpoints needed after the initial executive scan.", "guidedEscalation", "Preserves executive posture while making downstream follow-up more explicit.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
            ],
            OpportunityExperienceType.OperationalMonitoringExperience =>
            [
                CreateAlternateConcept("concept-command-center", "Command center flow", $"Starts with queue health and then moves into the operational follow-up path for {recommendation.RecommendationName.ToLowerInvariant()}.", "operationsBoard", "Optimizes for repeated in-day monitoring and action selection.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
                CreateAlternateConcept("concept-exception-path", "Exception-first path", $"Brings exceptions forward so operators can triage before opening supporting detail.", "exceptionLoop", "Improves action sequencing when the first need is triage rather than broad summary.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
                CreateAlternateConcept("concept-follow-through", "Follow-through path", $"Emphasizes the owner handoff and recovery loop after the initial operational scan.", "actionJourney", "Preserves monitoring posture while strengthening ownership and next-action clarity.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
            ],
            OpportunityExperienceType.AnalyticalInvestigationExperience =>
            [
                CreateAlternateConcept("concept-hypothesis", "Hypothesis-led investigation", $"Frames the experience around the leading question before opening the supporting evidence path.", "guidedInvestigation", "Preserves hypothesis discipline and avoids collapsing into a dashboard shell.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
                CreateAlternateConcept("concept-driver-compare", "Driver comparison path", $"Organizes the investigation around competing driver branches and comparative evidence.", "driverMatrix", "Gives analysts real comparison choices instead of one linear readout.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
                CreateAlternateConcept("concept-evidence-dossier", "Evidence dossier path", $"Stages the strongest evidence before the final conclusion to improve investigative confidence.", "evidenceDossier", "Pushes supporting detail forward so the recommendation feels earned.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
            ],
            OpportunityExperienceType.FabricApp =>
            [
                CreateAlternateConcept("concept-workflow-command", "Workflow command path", $"Centers the experience on coordination, routing, and follow-up for {recommendation.RecommendationName.ToLowerInvariant()}.", "workflowCommand", "Keeps the app oriented around handoffs rather than passive scanning.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
                CreateAlternateConcept("concept-role-handoff", "Role handoff path", $"Highlights the role transitions that keep the workflow moving across the app.", "roleJourney", "Makes owner transitions explicit so the app seed feels operational rather than report-like.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
                CreateAlternateConcept("concept-confirmation-loop", "Confirmation loop path", $"Adds a confirmation-oriented closing path after routing and follow-up steps are complete.", "confirmationLoop", "Preserves the app posture while clarifying what completion looks like.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
            ],
            _ =>
            [
                CreateAlternateConcept("concept-blueprint-aligned", "Blueprint-aligned flow", $"Follows the {recommendation.RecommendationName} blueprint sequence directly.", NormalizePattern(blueprint.NavigationIntent.Flow), "Preserves the recommended blueprint sequence as the initial baseline.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
                CreateAlternateConcept("concept-scan-first", "Scan-first KPI flow", $"Starts with KPI emphasis and then expands into {recommendation.RecommendationName.ToLowerInvariant()}.", "hubAndSpoke", "Creates a scan-first baseline while staying within the same blueprint evidence model.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
                CreateAlternateConcept("concept-guided-narrative", "Guided narrative flow", $"Turns the recommendation into a more deliberate staged path before the closing decision.", "guidedNarrative", "Provides a narrative-first alternative without changing the advisory-only boundary.", chapterMap, pageRecommendations, kpiNodes, brief.Dimensions, navigationSections, analyticalFlow),
            ],
        };
    }

    private static AlternateReportConcept CreateAlternateConcept(
        string id,
        string label,
        string summary,
        string navigationPattern,
        string navigationRationale,
        ReportChapterMapConcept chapterMap,
        IReadOnlyList<PageRecommendationConcept> pageRecommendations,
        IReadOnlyList<KpiHierarchyNodeConcept> kpiNodes,
        IReadOnlyList<string> dimensions,
        IReadOnlyList<NavigationSectionConcept> navigationSections,
        AnalyticalFlowConcept analyticalFlow)
    {
        return new AlternateReportConcept(
            Id: id,
            Label: label,
            Summary: summary,
            ChapterMap: chapterMap,
            PageRecommendations: pageRecommendations,
            KpiHierarchyNodes: kpiNodes,
            SupportingDimensions: dimensions,
            NavigationPattern: navigationPattern,
            NavigationRationale: navigationRationale,
            NavigationSections: navigationSections,
            AnalyticalFlow: analyticalFlow);
    }

    private static DraftSeedDescriptor BuildDraftSeed(
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity,
        ExperienceBlueprint blueprint,
        PageConcept pageConcept,
        int index)
    {
        var isEntry = index == 0;
        var isDecision = index == blueprint.RecommendedPages.Count - 1;

        return recommendation.RecommendedExperienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => new DraftSeedDescriptor(
                StructureSummary: isEntry
                    ? $"{pageConcept.Title} draft seed frames the leadership review, KPI posture, and follow-up checkpoint for {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()}."
                    : $"{pageConcept.Title} draft seed supports the executive follow-up path for {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()}.",
                LayoutType: isEntry ? "executiveKpiRunway" : "executiveDecisionCanvas",
                LayoutTitle: isDecision ? $"{pageConcept.Title} executive action layout" : $"{pageConcept.Title} leadership layout",
                Zones: isEntry ? ["header", "headlineKpis", "trendCanvas", "decisionPanel"] : ["header", "comparisonCanvas", "detailCanvas", "decisionPanel"]),
            OpportunityExperienceType.OperationalMonitoringExperience => new DraftSeedDescriptor(
                StructureSummary: isEntry
                    ? $"{pageConcept.Title} draft seed frames the action-oriented command view for {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()}."
                    : $"{pageConcept.Title} draft seed keeps the operator action loop moving through supporting detail.",
                LayoutType: isEntry ? "operationsCommandBoard" : "operationsFollowThroughGrid",
                LayoutTitle: isDecision ? $"{pageConcept.Title} action follow-through layout" : $"{pageConcept.Title} operations command layout",
                Zones: isEntry ? ["header", "alertRibbon", "primaryQueue", "ownerPanel"] : ["header", "exceptionCanvas", "detailCanvas", "ownerPanel"]),
            OpportunityExperienceType.FabricApp => new DraftSeedDescriptor(
                StructureSummary: isEntry
                    ? $"{pageConcept.Title} draft seed frames the workflow command shell for coordination and handoff."
                    : $"{pageConcept.Title} draft seed preserves workflow routing, follow-up, and confirmation inside the app path.",
                LayoutType: isEntry ? "workflowCommandShell" : "workflowRoleCanvas",
                LayoutTitle: isDecision ? $"{pageConcept.Title} workflow confirmation layout" : $"{pageConcept.Title} workflow handoff layout",
                Zones: isEntry ? ["header", "commandRail", "primaryWorkspace", "followUpPanel"] : ["header", "routingCanvas", "detailCanvas", "followUpPanel"]),
            OpportunityExperienceType.AnalyticalInvestigationExperience => new DraftSeedDescriptor(
                StructureSummary: isEntry
                    ? $"{pageConcept.Title} draft seed frames the investigative question and the evidence path it must open."
                    : $"{pageConcept.Title} draft seed preserves the analytical evidence chain before the final conclusion.",
                LayoutType: isEntry ? "investigationQuestionFrame" : "investigationEvidenceCanvas",
                LayoutTitle: isDecision ? $"{pageConcept.Title} conclusion layout" : $"{pageConcept.Title} evidence review layout",
                Zones: isEntry ? ["header", "questionPanel", "hypothesisCanvas", "evidenceRail"] : ["header", "comparisonCanvas", "detailCanvas", "conclusionPanel"]),
            _ => new DraftSeedDescriptor(
                StructureSummary: $"{pageConcept.Title} draft seed frames {pageConcept.IntendedPurpose.ToLowerInvariant()} while preserving the advisory-only discovery posture.",
                LayoutType: isEntry ? "heroKpiGrid" : "detailAnalysisGrid",
                LayoutTitle: $"{pageConcept.Title} layout",
                Zones: ["header", "primaryCanvas", "supportingCanvas"]),
        };
    }

    private static string GetExperiencePosture(OpportunityExperienceType experienceType)
    {
        return experienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => "leadership dashboard",
            OpportunityExperienceType.OperationalMonitoringExperience => "operational monitoring experience",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "investigative workspace",
            OpportunityExperienceType.FabricApp => "workflow application",
            OpportunityExperienceType.FabricDataApp => "exploration application",
            _ => "narrative report"
        };
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record DraftBuildResult(
        DraftReportArtifact Draft,
        IReadOnlyList<DraftPageArtifact> PageArtifacts,
        IReadOnlyList<DraftLayoutArtifact> LayoutArtifacts,
        IReadOnlyList<DraftNavigationArtifact> NavigationArtifacts);

    private sealed record DraftSeedDescriptor(
        string StructureSummary,
        string LayoutType,
        string LayoutTitle,
        IReadOnlyList<string> Zones);
}
