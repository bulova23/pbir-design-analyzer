using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Pbir;
using Serilog;
using Serilog.Events;

[assembly: InternalsVisibleTo("Tests")]

namespace PowerBIModelingService.RpcHost;

/// <summary>
/// Local PBIR analyzer process composition root.
/// </summary>
public static class Program
{
    internal const string BackendVersion = "0.1.11";

    public static async Task<int> Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}",
                standardErrorFromLevel: LogEventLevel.Verbose)
            .CreateLogger();

        try
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
            var services = BuildServices(loggerFactory);
            await using var dispatcher = new AnalyzerRpcDispatcher(
                services,
                new PbirMaterializationRpcAdapter(new PbirMaterializationOrchestrationService()));
            await using var server = new SimpleJsonRpcServer(
                dispatcher,
                Console.OpenStandardInput(),
                Console.OpenStandardOutput(),
                loggerFactory.CreateLogger<SimpleJsonRpcServer>(),
                RpcTransportOptions.Production,
                ownsHandler: false);

            await server.RunAsync().ConfigureAwait(false);
            return 0;
        }
        catch
        {
            Log.Fatal("rpc_transport event=host_fatal state=stopped");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }
    }

    private static AnalyzerServices BuildServices(ILoggerFactory loggerFactory)
    {
        var projectService = new PbirProjectService(loggerFactory.CreateLogger<PbirProjectService>());
        var treeBuilder = new PbirTreeBuilder(loggerFactory.CreateLogger<PbirTreeBuilder>());
        var scoringService = new PbirScoringService(
            projectService,
            loggerFactory.CreateLogger<PbirScoringService>());
        var governanceService = new PbirGovernanceService(
            loggerFactory.CreateLogger<PbirGovernanceService>());

        return new AnalyzerServices(projectService, treeBuilder, scoringService, governanceService);
    }
}

internal sealed record AnalyzerServices(
    PbirProjectService ProjectService,
    PbirTreeBuilder TreeBuilder,
    PbirScoringService ScoringService,
    PbirGovernanceService GovernanceService);
