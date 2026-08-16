using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class OpportunityIdentificationServiceTests : IDisposable
{
    private static readonly Assembly CoreAssembly = typeof(PbirScoringService).Assembly;
    private readonly List<string> _tempDirs = [];

    [Fact(DisplayName = "Opportunity identification infers executive and sales opportunities from revenue and territory signals")]
    public void BuildOpportunityCatalog_RevenueTerritoryModel_ProducesExecutiveOrSalesPerformanceOpportunities()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactSales",
                  "columns": [
                    { "name": "DateKey", "dataType": "int64" },
                    { "name": "RegionKey", "dataType": "int64" },
                    { "name": "TerritoryKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Revenue", "displayFolder": "Revenue KPIs", "description": "Net revenue" },
                    { "name": "Sales Growth", "displayFolder": "Revenue KPIs" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Year", "dataType": "int64" },
                    { "name": "Month", "dataType": "string" }
                  ],
                  "hierarchies": [
                    { "name": "Calendar", "levels": [ "Year", "Month" ] }
                  ]
                },
                {
                  "name": "DimRegion",
                  "columns": [
                    { "name": "Region", "dataType": "string" },
                    { "name": "Territory", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactSales", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactSales", "fromColumn": "RegionKey", "toTable": "DimRegion", "toColumn": "Region", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactSales", "fromColumn": "TerritoryKey", "toTable": "DimRegion", "toColumn": "Territory", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: ExecutiveOverviewPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");

        Assert.Contains(opportunities, opportunity =>
            ReadString(opportunity, "Category") is "ExecutiveReporting" or "SalesPerformance");
        Assert.Contains(opportunities, opportunity =>
            ReadStringList(opportunity, "CandidateExperienceTypes").Contains("ExecutiveDashboard") ||
            ReadStringList(opportunity, "CandidateExperienceTypes").Contains("PbirReport"));
    }

    [Fact(DisplayName = "Opportunity identification infers customer profitability opportunities")]
    public void BuildOpportunityCatalog_CustomerProfitabilityModel_ProducesCustomerProfitabilityOpportunity()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactMargin",
                  "columns": [
                    { "name": "CustomerKey", "dataType": "int64" },
                    { "name": "DateKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Gross Margin", "displayFolder": "Margin KPIs" },
                    { "name": "Profit per Customer", "displayFolder": "Margin KPIs" }
                  ]
                },
                {
                  "name": "DimCustomer",
                  "columns": [
                    { "name": "Customer", "dataType": "string" },
                    { "name": "Customer Segment", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Month", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactMargin", "fromColumn": "CustomerKey", "toTable": "DimCustomer", "toColumn": "Customer", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactMargin", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: AnalyticalOverviewPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");

        Assert.Contains(opportunities, opportunity =>
            ReadString(opportunity, "Name") == "Customer Profitability Analysis" &&
            ReadString(opportunity, "Category") == "ProfitabilityAnalysis");
    }

    [Fact(DisplayName = "Opportunity identification infers inventory operations opportunities")]
    public void BuildOpportunityCatalog_InventoryModel_ProducesInventoryOperationsOpportunity()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactInventory",
                  "columns": [
                    { "name": "WarehouseKey", "dataType": "int64" },
                    { "name": "ItemKey", "dataType": "int64" },
                    { "name": "DateKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Inventory Quantity", "displayFolder": "Inventory KPIs" },
                    { "name": "Inventory Value", "displayFolder": "Inventory KPIs" }
                  ]
                },
                {
                  "name": "DimWarehouse",
                  "columns": [
                    { "name": "Warehouse", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimItem",
                  "columns": [
                    { "name": "Item", "dataType": "string" },
                    { "name": "Category", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Month", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactInventory", "fromColumn": "WarehouseKey", "toTable": "DimWarehouse", "toColumn": "Warehouse", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactInventory", "fromColumn": "ItemKey", "toTable": "DimItem", "toColumn": "Item", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactInventory", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: OperationsPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");

        Assert.Contains(opportunities, opportunity =>
            ReadString(opportunity, "Name") == "Inventory Operations Monitoring" &&
            ReadString(opportunity, "Category") == "InventoryOptimization");
    }

    [Fact(DisplayName = "Opportunity identification expands inventory models into multiple credible opportunity families")]
    public void BuildOpportunityCatalog_InventoryModel_ProducesMultipleCredibleOpportunityFamilies()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactInventory",
                  "columns": [
                    { "name": "WarehouseKey", "dataType": "int64" },
                    { "name": "ItemKey", "dataType": "int64" },
                    { "name": "DateKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Inventory Quantity", "displayFolder": "Inventory KPIs" },
                    { "name": "Inventory Value", "displayFolder": "Inventory KPIs" },
                    { "name": "Stock Variance", "displayFolder": "Inventory KPIs", "description": "Variance used to investigate stock exceptions" }
                  ]
                },
                {
                  "name": "DimWarehouse",
                  "columns": [
                    { "name": "Warehouse", "dataType": "string" },
                    { "name": "Region", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimItem",
                  "columns": [
                    { "name": "Item", "dataType": "string" },
                    { "name": "Category", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Month", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactInventory", "fromColumn": "WarehouseKey", "toTable": "DimWarehouse", "toColumn": "Warehouse", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactInventory", "fromColumn": "ItemKey", "toTable": "DimItem", "toColumn": "Item", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactInventory", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: OperationsPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");
        var families = opportunities
            .Select(opportunity => ReadString(opportunity, "Family"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(opportunities.Count >= 4);
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Inventory Operations Monitoring");
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Inventory Planning");
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Inventory Investigation");
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Warehouse Performance");
        Assert.True(families.Count >= 4);
        Assert.Contains("Monitoring", families);
        Assert.Contains("Planning", families);
        Assert.Contains("Investigation", families);
        Assert.Contains("Performance", families);
    }

    [Fact(DisplayName = "Opportunity identification infers service operations opportunities")]
    public void BuildOpportunityCatalog_ServiceModel_ProducesServiceOperationsOpportunity()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactService",
                  "columns": [
                    { "name": "TechnicianKey", "dataType": "int64" },
                    { "name": "WorkOrderKey", "dataType": "int64" },
                    { "name": "DateKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Open Tickets", "displayFolder": "Service KPIs" },
                    { "name": "Average Resolution Time", "displayFolder": "Service KPIs" }
                  ]
                },
                {
                  "name": "DimTechnician",
                  "columns": [
                    { "name": "Technician", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimWorkOrder",
                  "columns": [
                    { "name": "Work Order", "dataType": "string" },
                    { "name": "Ticket Queue", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Month", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactService", "fromColumn": "TechnicianKey", "toTable": "DimTechnician", "toColumn": "Technician", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactService", "fromColumn": "WorkOrderKey", "toTable": "DimWorkOrder", "toColumn": "Work Order", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactService", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: OperationsPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");

        Assert.Contains(opportunities, opportunity =>
            ReadString(opportunity, "Name") == "Service Operations Dashboard" &&
            ReadString(opportunity, "Category") == "ServiceOperations");
    }

    [Fact(DisplayName = "Opportunity identification expands service models into multiple credible opportunity families")]
    public void BuildOpportunityCatalog_ServiceModel_ProducesMultipleCredibleOpportunityFamilies()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactService",
                  "columns": [
                    { "name": "TechnicianKey", "dataType": "int64" },
                    { "name": "WorkOrderKey", "dataType": "int64" },
                    { "name": "DateKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Open Tickets", "displayFolder": "Service KPIs" },
                    { "name": "Average Resolution Time", "displayFolder": "Service KPIs" },
                    { "name": "SLA Variance", "displayFolder": "Service KPIs", "description": "Variance used to investigate service misses" }
                  ]
                },
                {
                  "name": "DimTechnician",
                  "columns": [
                    { "name": "Technician", "dataType": "string" },
                    { "name": "Region", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimWorkOrder",
                  "columns": [
                    { "name": "Work Order", "dataType": "string" },
                    { "name": "Ticket Queue", "dataType": "string" },
                    { "name": "Status", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Month", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactService", "fromColumn": "TechnicianKey", "toTable": "DimTechnician", "toColumn": "Technician", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactService", "fromColumn": "WorkOrderKey", "toTable": "DimWorkOrder", "toColumn": "Work Order", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactService", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: OperationsPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");
        var families = opportunities
            .Select(opportunity => ReadString(opportunity, "Family"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(opportunities.Count >= 4);
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Service Operations Dashboard");
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Service Workflow Coordination");
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Service Performance Management");
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Service Investigation");
        Assert.True(families.Count >= 4);
        Assert.Contains("Monitoring", families);
        Assert.Contains("Workflow", families);
        Assert.Contains("Performance", families);
        Assert.Contains("Investigation", families);
    }

    [Fact(DisplayName = "Opportunity identification infers forecast accuracy opportunities")]
    public void BuildOpportunityCatalog_ForecastModel_ProducesForecastAccuracyOpportunity()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactPlanning",
                  "columns": [
                    { "name": "DateKey", "dataType": "int64" },
                    { "name": "RegionKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Forecast Amount", "displayFolder": "Forecast KPIs" },
                    { "name": "Actual Revenue", "displayFolder": "Revenue KPIs" },
                    { "name": "Forecast Variance", "displayFolder": "Forecast KPIs" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Month", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimRegion",
                  "columns": [
                    { "name": "Region", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactPlanning", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactPlanning", "fromColumn": "RegionKey", "toTable": "DimRegion", "toColumn": "Region", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: ExecutiveOverviewPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");

        Assert.Contains(opportunities, opportunity =>
            ReadString(opportunity, "Name") == "Forecast Accuracy Dashboard" &&
            ReadString(opportunity, "Category") == "ForecastAccuracy");
    }

    [Fact(DisplayName = "Opportunity identification expands forecasting models into planning and operational opportunities")]
    public void BuildOpportunityCatalog_ForecastModel_ProducesPlanningAndOperationalOpportunities()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactPlanning",
                  "columns": [
                    { "name": "DateKey", "dataType": "int64" },
                    { "name": "RegionKey", "dataType": "int64" },
                    { "name": "ProductKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Forecast Amount", "displayFolder": "Forecast KPIs" },
                    { "name": "Actual Revenue", "displayFolder": "Revenue KPIs" },
                    { "name": "Forecast Variance", "displayFolder": "Forecast KPIs" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Month", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimRegion",
                  "columns": [
                    { "name": "Region", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimProduct",
                  "columns": [
                    { "name": "Product", "dataType": "string" },
                    { "name": "Category", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactPlanning", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactPlanning", "fromColumn": "RegionKey", "toTable": "DimRegion", "toColumn": "Region", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactPlanning", "fromColumn": "ProductKey", "toTable": "DimProduct", "toColumn": "Product", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: ExecutiveOverviewPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");

        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Forecast Accuracy Dashboard");
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Forecast Planning Review");
        Assert.Contains(opportunities, opportunity =>
            ReadString(opportunity, "WorkflowOrientation") == "Act" &&
            ReadString(opportunity, "DecisionPattern") == "Planning");
        Assert.Contains(opportunities, opportunity =>
            ReadString(opportunity, "Family") == "Operational" &&
            ReadString(opportunity, "DecisionPattern") == "Threshold");
    }

    [Fact(DisplayName = "Opportunity identification infers analytical investigation opportunities from drill-rich models")]
    public void BuildOpportunityCatalog_RootCauseModel_ProducesAnalyticalInvestigationOpportunity()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactOperations",
                  "columns": [
                    { "name": "DateKey", "dataType": "int64" },
                    { "name": "RegionKey", "dataType": "int64" },
                    { "name": "ProductKey", "dataType": "int64" },
                    { "name": "CustomerKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Revenue", "displayFolder": "Revenue KPIs" },
                    { "name": "Margin", "displayFolder": "Margin KPIs" },
                    { "name": "Variance", "displayFolder": "Revenue KPIs", "description": "Variance for root cause analysis" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Year", "dataType": "int64" },
                    { "name": "Quarter", "dataType": "string" },
                    { "name": "Month", "dataType": "string" }
                  ],
                  "hierarchies": [
                    { "name": "Calendar", "levels": [ "Year", "Quarter", "Month" ] }
                  ]
                },
                {
                  "name": "DimRegion",
                  "columns": [
                    { "name": "Region", "dataType": "string" },
                    { "name": "Territory", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimProduct",
                  "columns": [
                    { "name": "Product", "dataType": "string" },
                    { "name": "Category", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimCustomer",
                  "columns": [
                    { "name": "Customer", "dataType": "string" },
                    { "name": "Segment", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactOperations", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactOperations", "fromColumn": "RegionKey", "toTable": "DimRegion", "toColumn": "Region", "cardinality": "ManyToOne", "crossFilteringBehavior": "BothDirections" },
                { "fromTable": "FactOperations", "fromColumn": "ProductKey", "toTable": "DimProduct", "toColumn": "Product", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactOperations", "fromColumn": "CustomerKey", "toTable": "DimCustomer", "toColumn": "Customer", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: InvestigationPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");

        Assert.Contains(opportunities, opportunity =>
            ReadString(opportunity, "Name") == "Root Cause Analysis Experience" &&
            ReadString(opportunity, "Category") == "RootCauseInvestigation" &&
            ReadStringList(opportunity, "CandidateExperienceTypes").Contains("AnalyticalInvestigationExperience"));
    }

    [Fact(DisplayName = "Opportunity identification gives investigation models investigative and non-investigative options")]
    public void BuildOpportunityCatalog_RootCauseModel_ProducesInvestigativeAndNonInvestigativeOpportunities()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactOperations",
                  "columns": [
                    { "name": "DateKey", "dataType": "int64" },
                    { "name": "RegionKey", "dataType": "int64" },
                    { "name": "ProductKey", "dataType": "int64" },
                    { "name": "CustomerKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Revenue", "displayFolder": "Revenue KPIs" },
                    { "name": "Margin", "displayFolder": "Margin KPIs" },
                    { "name": "Variance", "displayFolder": "Revenue KPIs", "description": "Variance for root cause analysis" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Year", "dataType": "int64" },
                    { "name": "Quarter", "dataType": "string" },
                    { "name": "Month", "dataType": "string" }
                  ],
                  "hierarchies": [
                    { "name": "Calendar", "levels": [ "Year", "Quarter", "Month" ] }
                  ]
                },
                {
                  "name": "DimRegion",
                  "columns": [
                    { "name": "Region", "dataType": "string" },
                    { "name": "Territory", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimProduct",
                  "columns": [
                    { "name": "Product", "dataType": "string" },
                    { "name": "Category", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimCustomer",
                  "columns": [
                    { "name": "Customer", "dataType": "string" },
                    { "name": "Segment", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactOperations", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactOperations", "fromColumn": "RegionKey", "toTable": "DimRegion", "toColumn": "Region", "cardinality": "ManyToOne", "crossFilteringBehavior": "BothDirections" },
                { "fromTable": "FactOperations", "fromColumn": "ProductKey", "toTable": "DimProduct", "toColumn": "Product", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactOperations", "fromColumn": "CustomerKey", "toTable": "DimCustomer", "toColumn": "Customer", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: InvestigationPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");
        var families = opportunities
            .Select(opportunity => ReadString(opportunity, "Family"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(opportunities.Count >= 3);
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Root Cause Analysis Experience");
        Assert.Contains(opportunities, opportunity =>
            ReadString(opportunity, "Family") == "Investigation" &&
            ReadString(opportunity, "DecisionPattern") == "Diagnostic");
        Assert.Contains(opportunities, opportunity =>
            ReadString(opportunity, "Family") != "Investigation");
        Assert.True(families.Count >= 2);
    }

    [Fact(DisplayName = "Opportunity identification expands revenue models into multiple credible opportunity families")]
    public void BuildOpportunityCatalog_RevenueModel_ProducesMultipleCredibleOpportunityFamilies()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactSales",
                  "columns": [
                    { "name": "DateKey", "dataType": "int64" },
                    { "name": "RegionKey", "dataType": "int64" },
                    { "name": "TerritoryKey", "dataType": "int64" },
                    { "name": "CustomerKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Revenue", "displayFolder": "Revenue KPIs", "description": "Net revenue" },
                    { "name": "Sales Growth", "displayFolder": "Revenue KPIs" },
                    { "name": "Forecast Variance", "displayFolder": "Forecast KPIs" },
                    { "name": "Profit per Customer", "displayFolder": "Margin KPIs" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Year", "dataType": "int64" },
                    { "name": "Month", "dataType": "string" }
                  ],
                  "hierarchies": [
                    { "name": "Calendar", "levels": [ "Year", "Month" ] }
                  ]
                },
                {
                  "name": "DimRegion",
                  "columns": [
                    { "name": "Region", "dataType": "string" },
                    { "name": "Territory", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimCustomer",
                  "columns": [
                    { "name": "Customer", "dataType": "string" },
                    { "name": "Customer Segment", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactSales", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactSales", "fromColumn": "RegionKey", "toTable": "DimRegion", "toColumn": "Region", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactSales", "fromColumn": "TerritoryKey", "toTable": "DimRegion", "toColumn": "Territory", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactSales", "fromColumn": "CustomerKey", "toTable": "DimCustomer", "toColumn": "Customer", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: ExecutiveOverviewPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");
        var families = opportunities
            .Select(opportunity => ReadString(opportunity, "Family"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(opportunities.Count >= 5);
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Executive Revenue Dashboard");
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Revenue Performance Management");
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Forecast Planning Review");
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Customer Profitability Analysis");
        Assert.Contains(opportunities, opportunity => ReadString(opportunity, "Name") == "Sales Investigation Experience");
        Assert.True(families.Count >= 4);
    }

    [Fact(DisplayName = "Opportunity identification attaches richer why evidence audience and outcome context to each opportunity")]
    public void BuildOpportunityCatalog_ExpandedOpportunities_IncludeRichEvidenceContext()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactService",
                  "columns": [
                    { "name": "TechnicianKey", "dataType": "int64" },
                    { "name": "WorkOrderKey", "dataType": "int64" },
                    { "name": "DateKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Open Tickets", "displayFolder": "Service KPIs" },
                    { "name": "Average Resolution Time", "displayFolder": "Service KPIs" },
                    { "name": "SLA Variance", "displayFolder": "Service KPIs" }
                  ]
                },
                {
                  "name": "DimTechnician",
                  "columns": [
                    { "name": "Technician", "dataType": "string" },
                    { "name": "Region", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimWorkOrder",
                  "columns": [
                    { "name": "Work Order", "dataType": "string" },
                    { "name": "Ticket Queue", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Month", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactService", "fromColumn": "TechnicianKey", "toTable": "DimTechnician", "toColumn": "Technician", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactService", "fromColumn": "WorkOrderKey", "toTable": "DimWorkOrder", "toColumn": "Work Order", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactService", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: OperationsPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");

        Assert.NotEmpty(opportunities);
        Assert.All(opportunities, opportunity =>
        {
            Assert.False(string.IsNullOrWhiteSpace(ReadString(opportunity, "WhyThisOpportunityExists")));
            Assert.False(string.IsNullOrWhiteSpace(ReadString(opportunity, "InferredAudience")));
            Assert.False(string.IsNullOrWhiteSpace(ReadString(opportunity, "BusinessOutcome")));
            Assert.NotEmpty(ReadStringList(opportunity, "EvidenceNarrative"));
            Assert.NotEmpty(ReadObjectList(opportunity, "SupportingSemanticSignals"));
        });
    }

    [Fact(DisplayName = "Opportunity identification keeps catalogs materially diverse before ranking begins")]
    public void BuildOpportunityCatalog_ExpandedCatalogs_AreMateriallyDifferentBeforeRanking()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactSales",
                  "columns": [
                    { "name": "DateKey", "dataType": "int64" },
                    { "name": "RegionKey", "dataType": "int64" },
                    { "name": "TerritoryKey", "dataType": "int64" },
                    { "name": "CustomerKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Revenue", "displayFolder": "Revenue KPIs" },
                    { "name": "Forecast Variance", "displayFolder": "Forecast KPIs" },
                    { "name": "Profit per Customer", "displayFolder": "Margin KPIs" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Month", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimRegion",
                  "columns": [
                    { "name": "Region", "dataType": "string" },
                    { "name": "Territory", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimCustomer",
                  "columns": [
                    { "name": "Customer", "dataType": "string" },
                    { "name": "Customer Segment", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactSales", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactSales", "fromColumn": "RegionKey", "toTable": "DimRegion", "toColumn": "Region", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactSales", "fromColumn": "TerritoryKey", "toTable": "DimRegion", "toColumn": "Territory", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactSales", "fromColumn": "CustomerKey", "toTable": "DimCustomer", "toColumn": "Customer", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: ExecutiveOverviewPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");
        var familyCount = opportunities.Select(opportunity => ReadString(opportunity, "Family")).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var audienceCount = opportunities.Select(opportunity => ReadString(opportunity, "InferredAudience")).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var decisionPatternCount = opportunities.Select(opportunity => ReadString(opportunity, "DecisionPattern")).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        Assert.True(opportunities.Count >= 5);
        Assert.True(familyCount >= 3);
        Assert.True(audienceCount >= 3);
        Assert.True(decisionPatternCount >= 3);
    }

    [Fact(DisplayName = "Opportunity identification preserves low-confidence ambiguity for sparse models")]
    public void BuildOpportunityCatalog_SparseModel_ProducesLowConfidenceOpportunityWithAmbiguityNotes()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "Sales",
                  "columns": [
                    { "name": "Category", "dataType": "string" }
                  ],
                  "measures": [
                    { "name": "Sales" }
                  ]
                }
              ]
            }
            """,
            pageJson: ExecutiveOverviewPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");

        Assert.NotEmpty(opportunities);
        Assert.All(opportunities, opportunity =>
        {
            Assert.Equal("Low", ReadString(opportunity, "Confidence"));
            Assert.NotEmpty(ReadStringList(opportunity, "LimitingFactors"));
        });
    }

    [Fact(DisplayName = "Opportunity identification deduplicates near-duplicate opportunities")]
    public void BuildOpportunityCatalog_DeduplicatesNearDuplicates()
    {
        var catalog = BuildOpportunityCatalog(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactSales",
                  "columns": [
                    { "name": "DateKey", "dataType": "int64" },
                    { "name": "RegionKey", "dataType": "int64" },
                    { "name": "TerritoryKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Revenue", "displayFolder": "Revenue KPIs" },
                    { "name": "Sales Amount", "displayFolder": "Revenue KPIs" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Month", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimRegion",
                  "columns": [
                    { "name": "Region", "dataType": "string" },
                    { "name": "Territory", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactSales", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactSales", "fromColumn": "RegionKey", "toTable": "DimRegion", "toColumn": "Region", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactSales", "fromColumn": "TerritoryKey", "toTable": "DimRegion", "toColumn": "Territory", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: ExecutiveOverviewPageJson));

        var opportunities = ReadObjectList(catalog, "Opportunities");
        var salesPerformanceDashboardMatches = opportunities.Count(opportunity =>
            ReadString(opportunity, "Category") == "SalesPerformance" &&
            ReadString(opportunity, "Name") == "Sales Performance Dashboard");

        Assert.Equal(1, salesPerformanceDashboardMatches);
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
            }
        }
    }

    private static string ExecutiveOverviewPageJson =>
        """
        {
          "name": "Page1",
          "displayName": "Executive Sales Overview",
          "visuals": [
            {
              "id": "v1",
              "type": "lineChart",
              "x": 0,
              "y": 100,
              "width": 480,
              "height": 220,
              "title": { "visible": true, "text": "Revenue Trend" },
              "fieldRoles": {
                "category": [ { "displayName": "Calendar", "hierarchy": [ "Year", "Month" ] } ],
                "measure": [ { "displayName": "Revenue" } ]
              }
            }
          ]
        }
        """;

    private static string OperationsPageJson =>
        """
        {
          "name": "Page1",
          "displayName": "Operations Monitor",
          "visuals": [
            {
              "id": "v1",
              "type": "barChart",
              "x": 0,
              "y": 80,
              "width": 480,
              "height": 240,
              "title": { "visible": true, "text": "Operational Review" },
              "fieldRoles": {
                "category": [ { "displayName": "Warehouse" } ],
                "measure": [ { "displayName": "Inventory Quantity" } ]
              }
            }
          ]
        }
        """;

    private static string AnalyticalOverviewPageJson =>
        """
        {
          "name": "Page1",
          "displayName": "Customer Analysis",
          "visuals": [
            {
              "id": "v1",
              "type": "scatterChart",
              "x": 0,
              "y": 80,
              "width": 480,
              "height": 240,
              "title": { "visible": true, "text": "Customer Profitability" },
              "fieldRoles": {
                "category": [ { "displayName": "Customer Segment" } ],
                "measure": [ { "displayName": "Gross Margin" } ]
              }
            }
          ]
        }
        """;

    private static string InvestigationPageJson =>
        """
        {
          "name": "Page1",
          "displayName": "Root Cause Investigation",
          "visuals": [
            {
              "id": "v1",
              "type": "matrix",
              "x": 0,
              "y": 80,
              "width": 640,
              "height": 280,
              "title": { "visible": true, "text": "Variance Root Cause" },
              "fieldRoles": {
                "rows": [
                  { "displayName": "Region" },
                  { "displayName": "Product" },
                  { "displayName": "Customer" }
                ],
                "measure": [ { "displayName": "Variance" } ]
              }
            }
          ]
        }
        """;

    private object BuildOpportunityCatalog(string projectRoot)
    {
        var discoveryServiceType = CoreAssembly.GetType("PowerBIModelingService.Services.Discovery.SemanticModelDiscoveryService", throwOnError: false);
        var opportunityServiceType = CoreAssembly.GetType("PowerBIModelingService.Services.Discovery.OpportunityIdentificationService", throwOnError: false);
        Assert.NotNull(discoveryServiceType);
        Assert.NotNull(opportunityServiceType);

        var discoveryService = Activator.CreateInstance(
            discoveryServiceType!,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args:
            [
                new PbirProjectService(NullLogger<PbirProjectService>.Instance),
                NullLogger.Instance
            ],
            culture: null);
        Assert.NotNull(discoveryService);

        var buildProfileMethod = discoveryServiceType!.GetMethod("BuildDiscoveryProfile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(buildProfileMethod);
        var profile = buildProfileMethod!.Invoke(discoveryService, [projectRoot]);
        Assert.NotNull(profile);

        var opportunityService = Activator.CreateInstance(
            opportunityServiceType!,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: null,
            culture: null);
        Assert.NotNull(opportunityService);

        var buildCatalogMethod = opportunityServiceType!.GetMethod("BuildOpportunityCatalog", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(buildCatalogMethod);
        var catalog = buildCatalogMethod!.Invoke(opportunityService, [profile!]);
        Assert.NotNull(catalog);
        return catalog!;
    }

    private string CreateTempProject(string modelJson, string pageJson)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "pbir-opportunity-" + Guid.NewGuid().ToString("N"));
        var reportDefinitionRoot = Path.Combine(tempRoot, "TestReport.Report", "definition");
        var reportPagesRoot = Path.Combine(reportDefinitionRoot, "pages", "Page1");
        var semanticModelRoot = Path.Combine(tempRoot, "TestModel.SemanticModel", "definition");

        Directory.CreateDirectory(reportPagesRoot);
        Directory.CreateDirectory(semanticModelRoot);
        _tempDirs.Add(tempRoot);

        File.WriteAllText(
            Path.Combine(reportDefinitionRoot, "report.json"),
            """{"id":"test","name":"TestReport","theme":{"name":"CY24SU10"}}""");
        File.WriteAllText(Path.Combine(reportPagesRoot, "page.json"), pageJson);
        File.WriteAllText(Path.Combine(semanticModelRoot, "model.json"), modelJson);

        return tempRoot;
    }

    private static object? GetPropertyValue(object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target);
    }

    private static List<object> ReadObjectList(object target, string propertyName)
    {
        var value = GetPropertyValue(target, propertyName);
        Assert.IsAssignableFrom<IEnumerable>(value);
        return ((IEnumerable)value!).Cast<object>().ToList();
    }

    private static List<string> ReadStringList(object target, string propertyName)
    {
        var value = GetPropertyValue(target, propertyName);
        Assert.IsAssignableFrom<IEnumerable>(value);
        return ((IEnumerable)value!).Cast<object>().Select(item => item?.ToString() ?? string.Empty).ToList();
    }

    private static string ReadString(object target, string propertyName)
    {
        return GetPropertyValue(target, propertyName)?.ToString() ?? string.Empty;
    }
}
