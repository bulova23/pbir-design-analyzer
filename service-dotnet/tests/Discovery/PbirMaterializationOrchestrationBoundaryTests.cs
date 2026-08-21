using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirMaterializationOrchestrationBoundaryTests
{
    [Fact]
    public void Orchestrator_DependsOnlyOnCanonicalMaterializationServices()
    {
        var fields = typeof(PbirMaterializationOrchestrationService)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(field => field.FieldType)
            .ToArray();

        Assert.Equal(
            new[]
            {
                typeof(PbirDeployableSerializerService),
                typeof(PbirDeployableMaterializationPreviewService),
                typeof(PbirDeployableMaterializationApplyService),
                typeof(PbirDeployableMaterializationRollbackService)
            },
            fields);
        Assert.DoesNotContain(typeof(IPbirDeployableMaterializationFileSystem), fields);
        Assert.DoesNotContain(typeof(PbirDeployableMaterializationPathPolicy), fields);
        Assert.DoesNotContain(typeof(PbirDeployableMaterializationTransactionStore), fields);
        Assert.DoesNotContain(typeof(PbirDeployableMaterializationSchemaValidator), fields);
        Assert.DoesNotContain(typeof(PbirLocalPreviewFileWriterService), fields);
        Assert.DoesNotContain(typeof(HttpClient), fields);
        Assert.DoesNotContain(typeof(Process), fields);
    }

    [Fact]
    public void PublicFailureDiagnostics_RedactAbsoluteAndTransactionPaths()
    {
        using var directory = new PbirMaterializationOrchestrationServiceTests.TemporaryDirectory();
        var secretSegment = "sensitive-customer-root";
        var sensitiveBase = Path.Combine(directory.Path, secretSegment);
        Directory.CreateDirectory(sensitiveBase);
        var input = PbirMaterializationOrchestrationServiceTests.CreateInput(sensitiveBase) with
        {
            TargetDirectoryName = "../sensitive-target"
        };

        var result = new PbirMaterializationOrchestrationService().Preview(
            PbirMaterializationOrchestrationServiceTests.CreatePreviewRequest(input));
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        });

        Assert.Equal(PbirMaterializationOrchestrationOutcome.UnsafeDestination, result.Outcome);
        Assert.DoesNotContain(directory.Path, json, StringComparison.Ordinal);
        Assert.DoesNotContain(secretSegment, json, StringComparison.Ordinal);
        Assert.DoesNotContain(".pbir-design-analyzer", json, StringComparison.Ordinal);
        Assert.DoesNotContain("staging", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("backup", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("quarantine", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyBoundary_RequiresCancellationTokenWithoutExposingLowerWriter()
    {
        var method = typeof(PbirDeployableMaterializationApplyService).GetMethod(
            "Apply", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        Assert.Equal(typeof(CancellationToken), method!.GetParameters().Last().ParameterType);
        Assert.True(method.GetParameters().Last().HasDefaultValue);
    }
}
