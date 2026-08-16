using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class OpportunityIdentificationService
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    internal OpportunityCatalog BuildOpportunityCatalog(DiscoveryProfile profile)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        var candidates = new List<OpportunityCandidate>();

        AddRevenueReportingCandidates(profile, candidates);
        AddCustomerCandidates(profile, candidates);
        AddInventoryCandidates(profile, candidates);
        AddServiceCandidates(profile, candidates);
        AddForecastCandidates(profile, candidates);
        AddAnalyticalInvestigationCandidates(profile, candidates);
        AddComparativePerformanceCandidates(profile, candidates);

        var deduplicated = Deduplicate(candidates);
        return new OpportunityCatalog(deduplicated);
    }

    private static void AddRevenueReportingCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        if (!HasDomain(profile, "Revenue"))
        {
            return;
        }

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "executive-sales-reporting",
            name: "Executive Sales Reporting",
            category: OpportunityCategory.ExecutiveReporting,
            audience: ChooseAudience(profile, "Executive", "Sales Leadership"),
            businessOutcome: "Track revenue trends and leadership-level performance over time.",
            experienceTypes:
            [
                OpportunityExperienceType.ExecutiveDashboard,
                OpportunityExperienceType.PbirReport,
                OpportunityExperienceType.FabricApp
            ],
            requiredSignals:
            [
                ("Domain", "Revenue"),
                ("DateIntelligence", profile.DateIntelligence.Readiness.ToString())
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasGeography(profile), "Dimension", "Geography"),
                CreateOptionalSignal(HasKpiCluster(profile, "Revenue KPIs"), "KpiCluster", "Revenue KPIs")
            ],
            isTimeDependent: true,
            family: OpportunityFamily.Executive,
            workflowOrientation: OpportunityWorkflowOrientation.Monitor,
            decisionPattern: OpportunityDecisionPattern.Summary,
            whyThisOpportunityExists: "Revenue signals and leadership-oriented time intelligence support a top-level revenue review experience."));

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "executive-revenue-dashboard",
            name: "Executive Revenue Dashboard",
            category: OpportunityCategory.ExecutiveReporting,
            audience: ChooseAudience(profile, "Executive", "Revenue Leadership"),
            businessOutcome: "Summarize revenue posture, KPI movement, and leadership follow-up areas.",
            experienceTypes:
            [
                OpportunityExperienceType.ExecutiveDashboard,
                OpportunityExperienceType.PbirReport
            ],
            requiredSignals:
            [
                ("Domain", "Revenue"),
                ("DateIntelligence", profile.DateIntelligence.Readiness.ToString())
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasKpiCluster(profile, "Revenue KPIs"), "KpiCluster", "Revenue KPIs"),
                CreateOptionalSignal(HasGeography(profile), "Dimension", "Geography")
            ],
            isTimeDependent: true,
            family: OpportunityFamily.Executive,
            workflowOrientation: OpportunityWorkflowOrientation.Monitor,
            decisionPattern: OpportunityDecisionPattern.Summary,
            whyThisOpportunityExists: "The model contains revenue and date intelligence signals strong enough to support a concise executive revenue dashboard."));

        if (HasGeography(profile))
        {
            candidates.Add(CreateCandidate(
                profile,
                opportunityId: "sales-performance-dashboard",
                name: "Sales Performance Dashboard",
                category: OpportunityCategory.SalesPerformance,
                audience: ChooseAudience(profile, "Executive", "Sales Leadership"),
                businessOutcome: "Compare revenue and sales performance across regions and territories.",
                experienceTypes:
                [
                    OpportunityExperienceType.ExecutiveDashboard,
                    OpportunityExperienceType.PbirReport
                ],
                requiredSignals:
                [
                    ("Domain", "Revenue"),
                    ("Dimension", "Geography")
                ],
                optionalSignals:
                [
                    CreateOptionalSignal(HasDateReadiness(profile), "DateIntelligence", profile.DateIntelligence.Readiness.ToString()),
                    CreateOptionalSignal(HasDimension(profile, "Customer"), "Dimension", "Customer")
                ],
                isTimeDependent: true,
                family: OpportunityFamily.Performance,
                workflowOrientation: OpportunityWorkflowOrientation.Monitor,
                decisionPattern: OpportunityDecisionPattern.Comparative,
                whyThisOpportunityExists: "Geography and revenue signals indicate a defensible cross-region sales performance management use case."));

            if (HasDimensionName(profile, "territory"))
            {
                candidates.Add(CreateCandidate(
                    profile,
                    opportunityId: "territory-performance-monitoring",
                    name: "Sales Performance Dashboard",
                    category: OpportunityCategory.SalesPerformance,
                    audience: ChooseAudience(profile, "Executive", "Sales Leadership"),
                    businessOutcome: "Compare revenue and sales performance across regions and territories.",
                    experienceTypes:
                    [
                        OpportunityExperienceType.ExecutiveDashboard,
                        OpportunityExperienceType.PbirReport
                    ],
                    requiredSignals:
                    [
                        ("Domain", "Revenue"),
                        ("Dimension", "Territory")
                    ],
                    optionalSignals:
                    [
                        CreateOptionalSignal(HasDateReadiness(profile), "DateIntelligence", profile.DateIntelligence.Readiness.ToString())
                    ],
                    isTimeDependent: true,
                    family: OpportunityFamily.Performance,
                    workflowOrientation: OpportunityWorkflowOrientation.Monitor,
                    decisionPattern: OpportunityDecisionPattern.Comparative,
                    whyThisOpportunityExists: "Territory structure creates a specific performance-management path rather than only a generic executive rollup."));
            }

            candidates.Add(CreateCandidate(
                profile,
                opportunityId: "revenue-performance-management",
                name: "Revenue Performance Management",
                category: OpportunityCategory.SalesPerformance,
                audience: ChooseAudience(profile, "Operational", "Sales Manager"),
                businessOutcome: "Manage revenue performance by region, territory, and accountable commercial owners.",
                experienceTypes:
                [
                    OpportunityExperienceType.OperationalMonitoringExperience,
                    OpportunityExperienceType.FabricApp,
                    OpportunityExperienceType.PbirReport
                ],
                requiredSignals:
                [
                    ("Domain", "Revenue"),
                    ("Dimension", "Geography")
                ],
                optionalSignals:
                [
                    CreateOptionalSignal(HasDimensionName(profile, "territory"), "Dimension", "Territory"),
                    CreateOptionalSignal(HasDateReadiness(profile), "DateIntelligence", profile.DateIntelligence.Readiness.ToString())
                ],
                isTimeDependent: true,
                family: OpportunityFamily.Operational,
                workflowOrientation: OpportunityWorkflowOrientation.Act,
                decisionPattern: OpportunityDecisionPattern.Threshold,
                whyThisOpportunityExists: "Revenue trends broken out by geography support a performance-management workflow that is more action-oriented than a pure executive dashboard."));
        }

        if (HasMeasureName(profile, "variance") || HasDimension(profile, "Customer"))
        {
            candidates.Add(CreateCandidate(
                profile,
                opportunityId: "sales-investigation-experience",
                name: "Sales Investigation Experience",
                category: OpportunityCategory.RootCauseInvestigation,
                audience: ChooseAudience(profile, "Analytical", "Sales Strategy"),
                businessOutcome: "Investigate why revenue, mix, or segment performance shifts before deciding follow-up actions.",
                experienceTypes:
                [
                    OpportunityExperienceType.AnalyticalInvestigationExperience,
                    OpportunityExperienceType.PbirReport
                ],
                requiredSignals:
                [
                    ("Domain", "Revenue")
                ],
                optionalSignals:
                [
                    CreateOptionalSignal(HasMeasureName(profile, "variance"), "Measure", "Variance"),
                    CreateOptionalSignal(HasDimension(profile, "Customer"), "Dimension", "Customer"),
                    CreateOptionalSignal(profile.Hierarchies.Count > 0, "Drill", "HierarchyRich")
                ],
                isTimeDependent: false,
                family: OpportunityFamily.Investigation,
                workflowOrientation: OpportunityWorkflowOrientation.Investigate,
                decisionPattern: OpportunityDecisionPattern.Diagnostic,
                whyThisOpportunityExists: "Revenue models with variance and drill-supporting structures can justify a diagnostic sales investigation path."));
        }
    }

    private static void AddCustomerCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        if (HasDomain(profile, "Customer") && HasDomain(profile, "Profitability"))
        {
            candidates.Add(CreateCandidate(
                profile,
                opportunityId: "customer-profitability-analysis",
                name: "Customer Profitability Analysis",
                category: OpportunityCategory.ProfitabilityAnalysis,
                audience: ChooseAudience(profile, "Analytical", "Commercial Strategy"),
                businessOutcome: "Identify which customer segments and accounts drive profitable growth.",
                experienceTypes:
                [
                    OpportunityExperienceType.AnalyticalInvestigationExperience,
                    OpportunityExperienceType.PbirReport,
                    OpportunityExperienceType.FabricDataApp
                ],
                requiredSignals:
                [
                    ("Domain", "Customer"),
                    ("Domain", "Profitability")
                ],
                optionalSignals:
                [
                    CreateOptionalSignal(HasDimension(profile, "Customer"), "Dimension", "Customer"),
                    CreateOptionalSignal(HasDimensionName(profile, "segment"), "Dimension", "Customer Segment")
                ],
                isTimeDependent: false,
                family: OpportunityFamily.Analytical,
                workflowOrientation: OpportunityWorkflowOrientation.Analyze,
                decisionPattern: OpportunityDecisionPattern.Comparative,
                whyThisOpportunityExists: "Customer and profitability signals support a customer-level margin analysis path with clear business outcome relevance."));
        }
        else if (HasDomain(profile, "Customer"))
        {
            candidates.Add(CreateCandidate(
                profile,
                opportunityId: "customer-segmentation-experience",
                name: "Customer Segmentation Experience",
                category: OpportunityCategory.CustomerAnalysis,
                audience: ChooseAudience(profile, "Analytical", "Commercial Strategy"),
                businessOutcome: "Segment customers for retention, growth, and experience analysis.",
                experienceTypes:
                [
                    OpportunityExperienceType.PbirReport,
                    OpportunityExperienceType.FabricDataApp,
                    OpportunityExperienceType.AnalyticalInvestigationExperience
                ],
                requiredSignals:
                [
                    ("Domain", "Customer")
                ],
                optionalSignals:
                [
                    CreateOptionalSignal(HasDimensionName(profile, "segment"), "Dimension", "Customer Segment"),
                    CreateOptionalSignal(HasDateReadiness(profile), "DateIntelligence", profile.DateIntelligence.Readiness.ToString())
                ],
                isTimeDependent: false,
                family: OpportunityFamily.Analytical,
                workflowOrientation: OpportunityWorkflowOrientation.Analyze,
                decisionPattern: OpportunityDecisionPattern.Comparative,
                whyThisOpportunityExists: "Customer entities and segmentation hints support an exploratory customer analysis opportunity."));
        }
    }

    private static void AddInventoryCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        if (!HasDomain(profile, "Inventory"))
        {
            return;
        }

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "inventory-operations-monitoring",
            name: "Inventory Operations Monitoring",
            category: OpportunityCategory.InventoryOptimization,
            audience: ChooseAudience(profile, "Operational", "Supply Chain Operations"),
            businessOutcome: "Monitor stock position, warehouse health, and item-level inventory risk.",
            experienceTypes:
            [
                OpportunityExperienceType.OperationalMonitoringExperience,
                OpportunityExperienceType.PbirReport,
                OpportunityExperienceType.FabricApp
            ],
            requiredSignals:
            [
                ("Domain", "Inventory")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasDimension(profile, "Inventory"), "Dimension", "Inventory"),
                CreateOptionalSignal(HasDimension(profile, "Product"), "Dimension", "Product"),
                CreateOptionalSignal(HasMeasureName(profile, "quantity"), "Measure", "Quantity")
            ],
            isTimeDependent: true,
            family: OpportunityFamily.Monitoring,
            workflowOrientation: OpportunityWorkflowOrientation.Monitor,
            decisionPattern: OpportunityDecisionPattern.Threshold,
            whyThisOpportunityExists: "Inventory, product, and quantity signals justify an operations monitoring experience for stock health."));

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "inventory-planning",
            name: "Inventory Planning",
            category: OpportunityCategory.InventoryOptimization,
            audience: "Supply Planning",
            businessOutcome: "Plan inventory posture across warehouses and product groups before shortages emerge.",
            experienceTypes:
            [
                OpportunityExperienceType.ExecutiveDashboard,
                OpportunityExperienceType.PbirReport
            ],
            requiredSignals:
            [
                ("Domain", "Inventory"),
                ("DateIntelligence", profile.DateIntelligence.Readiness.ToString())
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasDimensionName(profile, "warehouse"), "Dimension", "Warehouse"),
                CreateOptionalSignal(HasDimension(profile, "Product"), "Dimension", "Product")
            ],
            isTimeDependent: true,
            family: OpportunityFamily.Planning,
            workflowOrientation: OpportunityWorkflowOrientation.Act,
            decisionPattern: OpportunityDecisionPattern.Planning,
            whyThisOpportunityExists: "Inventory models with warehouse and date coverage can support forward-looking planning decisions, not only current-state monitoring."));

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "inventory-investigation",
            name: "Inventory Investigation",
            category: OpportunityCategory.RootCauseInvestigation,
            audience: ChooseAudience(profile, "Analytical", "Supply Analyst"),
            businessOutcome: "Investigate inventory exceptions, stock variance, and item-level drivers before operational correction.",
            experienceTypes:
            [
                OpportunityExperienceType.AnalyticalInvestigationExperience,
                OpportunityExperienceType.PbirReport
            ],
            requiredSignals:
            [
                ("Domain", "Inventory")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasMeasureName(profile, "variance"), "Measure", "Variance"),
                CreateOptionalSignal(HasMeasureName(profile, "quantity"), "Measure", "Quantity"),
                CreateOptionalSignal(profile.Relationships.Count >= 3, "Drill", "RelationshipRich")
            ],
            isTimeDependent: false,
            family: OpportunityFamily.Investigation,
            workflowOrientation: OpportunityWorkflowOrientation.Investigate,
            decisionPattern: OpportunityDecisionPattern.Diagnostic,
            whyThisOpportunityExists: "Inventory variance and relationship depth support a root-cause path for stock exceptions."));

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "warehouse-performance",
            name: "Warehouse Performance",
            category: OpportunityCategory.InventoryOptimization,
            audience: "Warehouse Leadership",
            businessOutcome: "Compare warehouse performance, stock position, and operational pressure across locations.",
            experienceTypes:
            [
                OpportunityExperienceType.ExecutiveDashboard,
                OpportunityExperienceType.PbirReport
            ],
            requiredSignals:
            [
                ("Domain", "Inventory"),
                ("Dimension", "Warehouse")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasGeography(profile), "Dimension", "Geography"),
                CreateOptionalSignal(HasMeasureName(profile, "value"), "Measure", "Value")
            ],
            isTimeDependent: true,
            family: OpportunityFamily.Performance,
            workflowOrientation: OpportunityWorkflowOrientation.Monitor,
            decisionPattern: OpportunityDecisionPattern.Comparative,
            whyThisOpportunityExists: "Warehouse dimensions create a defensible performance comparison opportunity beyond generic stock monitoring."));
    }

    private static void AddServiceCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        if (!HasDomain(profile, "Service"))
        {
            return;
        }

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "service-operations-dashboard",
            name: "Service Operations Dashboard",
            category: OpportunityCategory.ServiceOperations,
            audience: ChooseAudience(profile, "Service", "Service Operations"),
            businessOutcome: "Monitor service workload, technician performance, and resolution effectiveness.",
            experienceTypes:
            [
                OpportunityExperienceType.OperationalMonitoringExperience,
                OpportunityExperienceType.PbirReport,
                OpportunityExperienceType.FabricApp
            ],
            requiredSignals:
            [
                ("Domain", "Service")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasDimensionName(profile, "technician"), "Dimension", "Technician"),
                CreateOptionalSignal(HasDimensionName(profile, "work order") || HasDimensionName(profile, "ticket"), "Dimension", "Work Order"),
                CreateOptionalSignal(HasMeasureName(profile, "resolution"), "Measure", "Resolution")
            ],
            isTimeDependent: true,
            family: OpportunityFamily.Monitoring,
            workflowOrientation: OpportunityWorkflowOrientation.Monitor,
            decisionPattern: OpportunityDecisionPattern.Threshold,
            whyThisOpportunityExists: "Service workload, technician, and work-order signals support a command-center monitoring opportunity."));

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "service-workflow-coordination",
            name: "Service Workflow Coordination",
            category: OpportunityCategory.ServiceOperations,
            audience: "Service Operations",
            businessOutcome: "Coordinate backlog triage, technician follow-up, and work-order handoffs across the service workflow.",
            experienceTypes:
            [
                OpportunityExperienceType.FabricApp,
                OpportunityExperienceType.OperationalMonitoringExperience,
                OpportunityExperienceType.PbirReport
            ],
            requiredSignals:
            [
                ("Domain", "Service")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasDimensionName(profile, "technician"), "Dimension", "Technician"),
                CreateOptionalSignal(HasDimensionName(profile, "work order") || HasDimensionName(profile, "ticket"), "Dimension", "Work Order"),
                CreateOptionalSignal(HasMeasureName(profile, "resolution"), "Measure", "Resolution")
            ],
            isTimeDependent: true,
            family: OpportunityFamily.Workflow,
            workflowOrientation: OpportunityWorkflowOrientation.Act,
            decisionPattern: OpportunityDecisionPattern.Workflow,
            whyThisOpportunityExists: "Service models with technician and work-order entities can support workflow coordination rather than only passive monitoring."));

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "service-performance-management",
            name: "Service Performance Management",
            category: OpportunityCategory.ServiceOperations,
            audience: "Service Leadership",
            businessOutcome: "Compare service throughput, SLA risk, and team performance across regions and queues.",
            experienceTypes:
            [
                OpportunityExperienceType.ExecutiveDashboard,
                OpportunityExperienceType.PbirReport
            ],
            requiredSignals:
            [
                ("Domain", "Service")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasGeography(profile), "Dimension", "Geography"),
                CreateOptionalSignal(HasMeasureName(profile, "resolution"), "Measure", "Resolution"),
                CreateOptionalSignal(HasDimensionName(profile, "queue"), "Dimension", "Queue")
            ],
            isTimeDependent: true,
            family: OpportunityFamily.Performance,
            workflowOrientation: OpportunityWorkflowOrientation.Monitor,
            decisionPattern: OpportunityDecisionPattern.Comparative,
            whyThisOpportunityExists: "Service performance can be compared across teams and queues when the model exposes the right operating dimensions."));

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "service-investigation",
            name: "Service Investigation",
            category: OpportunityCategory.RootCauseInvestigation,
            audience: ChooseAudience(profile, "Analytical", "Service Analyst"),
            businessOutcome: "Investigate service misses, SLA variance, and root causes before operational changes are assigned.",
            experienceTypes:
            [
                OpportunityExperienceType.AnalyticalInvestigationExperience,
                OpportunityExperienceType.PbirReport
            ],
            requiredSignals:
            [
                ("Domain", "Service")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasMeasureName(profile, "variance"), "Measure", "Variance"),
                CreateOptionalSignal(HasMeasureName(profile, "resolution"), "Measure", "Resolution"),
                CreateOptionalSignal(profile.Relationships.Count >= 3, "Drill", "RelationshipRich")
            ],
            isTimeDependent: false,
            family: OpportunityFamily.Investigation,
            workflowOrientation: OpportunityWorkflowOrientation.Investigate,
            decisionPattern: OpportunityDecisionPattern.Diagnostic,
            whyThisOpportunityExists: "Service variance and operational drill paths support an investigative service opportunity."));
    }

    private static void AddForecastCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        if (!HasDomain(profile, "Forecasting"))
        {
            return;
        }

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "forecast-accuracy-dashboard",
            name: "Forecast Accuracy Dashboard",
            category: OpportunityCategory.ForecastAccuracy,
            audience: ChooseAudience(profile, "Executive", "Planning Leadership"),
            businessOutcome: "Compare forecast, actuals, and variance to improve planning accuracy.",
            experienceTypes:
            [
                OpportunityExperienceType.ExecutiveDashboard,
                OpportunityExperienceType.PbirReport,
                OpportunityExperienceType.AnalyticalInvestigationExperience
            ],
            requiredSignals:
            [
                ("Domain", "Forecasting")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasMeasureName(profile, "actual"), "Measure", "Actual"),
                CreateOptionalSignal(HasMeasureName(profile, "variance"), "Measure", "Variance"),
                CreateOptionalSignal(HasDateReadiness(profile), "DateIntelligence", profile.DateIntelligence.Readiness.ToString())
            ],
            isTimeDependent: true,
            family: OpportunityFamily.Executive,
            workflowOrientation: OpportunityWorkflowOrientation.Monitor,
            decisionPattern: OpportunityDecisionPattern.Summary,
            whyThisOpportunityExists: "Forecasting models support a leadership-facing accuracy review when forecast, actual, and variance signals are present."));

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "forecast-planning-review",
            name: "Forecast Planning Review",
            category: OpportunityCategory.ForecastAccuracy,
            audience: "Planning Leadership",
            businessOutcome: "Review forecast posture, re-plan assumptions, and improve the next planning cycle.",
            experienceTypes:
            [
                OpportunityExperienceType.ExecutiveDashboard,
                OpportunityExperienceType.PbirReport,
                OpportunityExperienceType.AnalyticalInvestigationExperience
            ],
            requiredSignals:
            [
                ("Domain", "Forecasting"),
                ("DateIntelligence", profile.DateIntelligence.Readiness.ToString())
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasMeasureName(profile, "actual"), "Measure", "Actual"),
                CreateOptionalSignal(HasMeasureName(profile, "variance"), "Measure", "Variance")
            ],
            isTimeDependent: true,
            family: OpportunityFamily.Planning,
            workflowOrientation: OpportunityWorkflowOrientation.Act,
            decisionPattern: OpportunityDecisionPattern.Planning,
            whyThisOpportunityExists: "Forecasting plus time intelligence supports a planning-grade review instead of only a generic KPI summary."));

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "forecast-operations-follow-through",
            name: "Forecast Operations Follow-Through",
            category: OpportunityCategory.ForecastAccuracy,
            audience: "Operations Leadership",
            businessOutcome: "Monitor forecast miss thresholds and trigger follow-through where actuals drift from plan.",
            experienceTypes:
            [
                OpportunityExperienceType.OperationalMonitoringExperience,
                OpportunityExperienceType.PbirReport
            ],
            requiredSignals:
            [
                ("Domain", "Forecasting")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasMeasureName(profile, "variance"), "Measure", "Variance"),
                CreateOptionalSignal(HasGeography(profile), "Dimension", "Geography"),
                CreateOptionalSignal(HasDateReadiness(profile), "DateIntelligence", profile.DateIntelligence.Readiness.ToString())
            ],
            isTimeDependent: true,
            family: OpportunityFamily.Operational,
            workflowOrientation: OpportunityWorkflowOrientation.Act,
            decisionPattern: OpportunityDecisionPattern.Threshold,
            whyThisOpportunityExists: "Forecast miss signals can support an operational follow-through path when threshold-based action is needed."));
    }

    private static void AddAnalyticalInvestigationCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        var hasAnalyticalAudience = HasAudience(profile, "Analytical");
        var hasDrillSignals = profile.Hierarchies.Count > 0 || profile.Dimensions.Count >= 4 || profile.Relationships.Count >= 3;
        var hasRootCauseMeasureSignals = HasMeasureName(profile, "variance") || HasMeasureName(profile, "root cause");

        if (!hasAnalyticalAudience || !hasDrillSignals || !hasRootCauseMeasureSignals)
        {
            return;
        }

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "root-cause-analysis-experience",
            name: "Root Cause Analysis Experience",
            category: OpportunityCategory.RootCauseInvestigation,
            audience: "Analytical",
            businessOutcome: "Investigate drivers of variance through drill-based root cause analysis.",
            experienceTypes:
            [
                OpportunityExperienceType.AnalyticalInvestigationExperience,
                OpportunityExperienceType.PbirReport
            ],
            requiredSignals:
            [
                ("Audience", "Analytical"),
                ("Measure", "Variance"),
                ("Drill", "HierarchyRich")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(profile.Relationships.Count >= 3, "RelationshipCount", profile.Relationships.Count.ToString()),
                CreateOptionalSignal(profile.Hierarchies.Count > 0, "HierarchyCount", profile.Hierarchies.Count.ToString())
            ],
            isTimeDependent: false,
            family: OpportunityFamily.Investigation,
            workflowOrientation: OpportunityWorkflowOrientation.Investigate,
            decisionPattern: OpportunityDecisionPattern.Diagnostic,
            whyThisOpportunityExists: "Analytical audience signals, drill depth, and variance evidence justify a dedicated root-cause investigation experience."));
    }

    private static void AddComparativePerformanceCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        if (!(HasDomain(profile, "Revenue") || HasDomain(profile, "Profitability")) || !HasGeography(profile))
        {
            return;
        }

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "comparative-performance-management",
            name: "Comparative Performance Management",
            category: OpportunityCategory.ComparativePerformanceManagement,
            audience: ChooseAudience(profile, "Executive", "Performance Management"),
            businessOutcome: "Compare performance across business units, territories, and periods.",
            experienceTypes:
            [
                OpportunityExperienceType.ExecutiveDashboard,
                OpportunityExperienceType.PbirReport,
                OpportunityExperienceType.AnalyticalInvestigationExperience
            ],
            requiredSignals:
            [
                ("Dimension", "Geography")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(HasDomain(profile, "Revenue"), "Domain", "Revenue"),
                CreateOptionalSignal(HasDomain(profile, "Profitability"), "Domain", "Profitability"),
                CreateOptionalSignal(HasDateReadiness(profile), "DateIntelligence", profile.DateIntelligence.Readiness.ToString())
            ],
            isTimeDependent: true,
            family: OpportunityFamily.Performance,
            workflowOrientation: OpportunityWorkflowOrientation.Analyze,
            decisionPattern: OpportunityDecisionPattern.Comparative,
            whyThisOpportunityExists: "Geography-backed performance comparisons create a defensible management path separate from pure investigation or KPI rollups."));
    }

    private static OpportunityCandidate CreateCandidate(
        DiscoveryProfile profile,
        string opportunityId,
        string name,
        OpportunityCategory category,
        string audience,
        string businessOutcome,
        IReadOnlyList<OpportunityExperienceType> experienceTypes,
        IReadOnlyList<(string SignalType, string Value)> requiredSignals,
        IReadOnlyList<(bool Include, string SignalType, string Value)> optionalSignals,
        bool isTimeDependent,
        OpportunityFamily family,
        OpportunityWorkflowOrientation workflowOrientation,
        OpportunityDecisionPattern decisionPattern,
        string whyThisOpportunityExists)
    {
        var signals = requiredSignals
            .Select(signal => new OpportunitySemanticSignal(signal.SignalType, signal.Value))
            .Concat(optionalSignals
                .Where(signal => signal.Include)
                .Select(signal => new OpportunitySemanticSignal(signal.SignalType, signal.Value)))
            .Distinct()
            .ToList();

        var confidence = CalculateOpportunityConfidence(profile, signals.Count, isTimeDependent);
        return new OpportunityCandidate(
            OpportunityId: opportunityId,
            Name: name,
            Category: category,
            InferredAudience: audience,
            BusinessOutcome: businessOutcome,
            CandidateExperienceTypes: experienceTypes
                .Distinct()
                .OrderBy(type => type.ToString(), NameComparer)
                .ToList(),
            SupportingSemanticSignals: signals,
            LimitingFactors: BuildLimitingFactors(profile, signals.Count, isTimeDependent),
            Confidence: confidence)
        {
            Family = family,
            WorkflowOrientation = workflowOrientation,
            DecisionPattern = decisionPattern,
            WhyThisOpportunityExists = whyThisOpportunityExists,
            EvidenceNarrative = BuildEvidenceNarrative(profile, signals, audience, businessOutcome)
        };
    }

    private static IReadOnlyList<OpportunityCandidate> Deduplicate(IReadOnlyList<OpportunityCandidate> candidates)
    {
        var deduplicated = new Dictionary<string, OpportunityCandidate>(NameComparer);

        foreach (var candidate in candidates)
        {
            var key = BuildDeduplicationKey(candidate);
            if (!deduplicated.TryGetValue(key, out var existing))
            {
                deduplicated[key] = candidate;
                continue;
            }

            deduplicated[key] = MergeCandidate(existing, candidate);
        }

        return deduplicated.Values
            .OrderBy(candidate => candidate.Confidence)
            .ThenBy(candidate => candidate.Name, NameComparer)
            .ToList();
    }

    private static OpportunityCandidate MergeCandidate(OpportunityCandidate existing, OpportunityCandidate candidate)
    {
        var mergedConfidence = Max(existing.Confidence, candidate.Confidence);
        var mergedSignals = existing.SupportingSemanticSignals
            .Concat(candidate.SupportingSemanticSignals)
            .Distinct()
            .OrderBy(signal => signal.SignalType, NameComparer)
            .ThenBy(signal => signal.Value, NameComparer)
            .ToList();
        var mergedLimitingFactors = existing.LimitingFactors
            .Concat(candidate.LimitingFactors)
            .Distinct(NameComparer)
            .OrderBy(note => note, NameComparer)
            .ToList();
        var mergedExperienceTypes = existing.CandidateExperienceTypes
            .Concat(candidate.CandidateExperienceTypes)
            .Distinct()
            .OrderBy(type => type.ToString(), NameComparer)
            .ToList();
        var mergedEvidenceNarrative = existing.EvidenceNarrative
            .Concat(candidate.EvidenceNarrative)
            .Distinct(NameComparer)
            .OrderBy(note => note, NameComparer)
            .ToList();

        return existing with
        {
            CandidateExperienceTypes = mergedExperienceTypes,
            SupportingSemanticSignals = mergedSignals,
            LimitingFactors = mergedLimitingFactors,
            Confidence = mergedConfidence,
            EvidenceNarrative = mergedEvidenceNarrative,
            WhyThisOpportunityExists = existing.WhyThisOpportunityExists.Length >= candidate.WhyThisOpportunityExists.Length
                ? existing.WhyThisOpportunityExists
                : candidate.WhyThisOpportunityExists
        };
    }

    private static string BuildDeduplicationKey(OpportunityCandidate candidate)
    {
        return $"{candidate.Category}|{candidate.Name}|{candidate.InferredAudience}";
    }

    private static IReadOnlyList<string> BuildLimitingFactors(DiscoveryProfile profile, int signalCount, bool isTimeDependent)
    {
        var notes = profile.AmbiguityNotes.ToList();

        if (signalCount <= 1)
        {
            notes.Add("Opportunity is inferred from limited semantic evidence.");
        }

        if (isTimeDependent && profile.DateIntelligence.Readiness == DiscoveryDateIntelligenceReadiness.Low)
        {
            notes.Add("Weak date intelligence reduces confidence for time-based monitoring.");
        }

        return notes
            .Distinct(NameComparer)
            .OrderBy(note => note, NameComparer)
            .ToList();
    }

    private static DiscoveryConfidenceLevel CalculateOpportunityConfidence(DiscoveryProfile profile, int signalCount, bool isTimeDependent)
    {
        var score = profile.Confidence switch
        {
            DiscoveryConfidenceLevel.High => 3,
            DiscoveryConfidenceLevel.Medium => 2,
            _ => 1
        };

        score += signalCount switch
        {
            >= 4 => 2,
            3 => 1,
            _ => 0
        };

        if (isTimeDependent)
        {
            score += profile.DateIntelligence.Readiness switch
            {
                DiscoveryDateIntelligenceReadiness.High => 1,
                DiscoveryDateIntelligenceReadiness.Low => -1,
                _ => 0
            };
        }

        score -= Math.Min(profile.AmbiguityNotes.Count, 2);

        return score >= 5
            ? DiscoveryConfidenceLevel.High
            : score >= 3
                ? DiscoveryConfidenceLevel.Medium
                : DiscoveryConfidenceLevel.Low;
    }

    private static DiscoveryConfidenceLevel Max(DiscoveryConfidenceLevel left, DiscoveryConfidenceLevel right)
    {
        return (int)left >= (int)right ? left : right;
    }

    private static (bool Include, string SignalType, string Value) CreateOptionalSignal(
        bool include,
        string signalType,
        string value)
    {
        return include && !string.IsNullOrWhiteSpace(value)
            ? (true, signalType, value)
            : (false, string.Empty, string.Empty);
    }

    private static bool HasDomain(DiscoveryProfile profile, string domain)
    {
        return profile.BusinessDomains.Any(signal => string.Equals(signal.Domain, domain, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasAudience(DiscoveryProfile profile, string audience)
    {
        return profile.AudienceSignals.Any(signal => string.Equals(signal.Audience, audience, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasDimension(DiscoveryProfile profile, string businessRole)
    {
        return profile.Dimensions.Any(dimension => string.Equals(dimension.BusinessRole, businessRole, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasDimensionName(DiscoveryProfile profile, string value)
    {
        return profile.Dimensions.Any(dimension => dimension.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasGeography(DiscoveryProfile profile)
    {
        return HasDimension(profile, "Geography") || HasDimensionName(profile, "region") || HasDimensionName(profile, "territory");
    }

    private static bool HasMeasureName(DiscoveryProfile profile, string value)
    {
        return profile.Measures.Any(measure =>
            measure.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(measure.Description) &&
             measure.Description.Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasKpiCluster(DiscoveryProfile profile, string clusterName)
    {
        return profile.KpiClusters.Any(cluster => string.Equals(cluster.ClusterName, clusterName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasDateReadiness(DiscoveryProfile profile)
    {
        return profile.DateIntelligence.Readiness != DiscoveryDateIntelligenceReadiness.Low;
    }

    private static string ChooseAudience(DiscoveryProfile profile, string preferredAudience, string fallback)
    {
        return HasAudience(profile, preferredAudience)
            ? preferredAudience
            : fallback;
    }

    private static IReadOnlyList<string> BuildEvidenceNarrative(
        DiscoveryProfile profile,
        IReadOnlyList<OpportunitySemanticSignal> signals,
        string audience,
        string businessOutcome)
    {
        var evidence = new List<string>
        {
            $"Audience support points to {audience}.",
            $"Business outcome focus is to {businessOutcome.ToLowerInvariant()}."
        };

        foreach (var signal in signals)
        {
            evidence.Add(signal.SignalType switch
            {
                "Domain" => $"{signal.Value} domain evidence is present in the semantic model.",
                "Dimension" => $"{signal.Value} dimensions support the opportunity shape.",
                "Measure" => $"{signal.Value} measures support the decision path.",
                "KpiCluster" => $"{signal.Value} KPIs reinforce the scenario focus.",
                "DateIntelligence" => $"{signal.Value} date readiness supports time-based analysis.",
                "Drill" => $"{signal.Value} drill signals support deeper exploration.",
                _ => $"{signal.SignalType} signal '{signal.Value}' contributes supporting semantic evidence."
            });
        }

        if (profile.AmbiguityNotes.Count > 0)
        {
            evidence.Add("Ambiguity notes remain and should be considered during recommendation ranking.");
        }

        return evidence
            .Distinct(NameComparer)
            .OrderBy(note => note, NameComparer)
            .ToList();
    }
}
