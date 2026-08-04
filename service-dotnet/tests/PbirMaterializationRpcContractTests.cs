extern alias RpcHost;

using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using PowerBIModelingService.Tests.Discovery;
using PbirMaterializationRpcAdapter = RpcHost::PowerBIModelingService.RpcHost.PbirMaterializationRpcAdapter;
using PbirMaterializationRpcContract = RpcHost::PowerBIModelingService.RpcHost.PbirMaterializationRpcContract;
using PbirMaterializationRpcValidation = RpcHost::PowerBIModelingService.RpcHost.PbirMaterializationRpcValidation;
using RpcTransportOptions = RpcHost::PowerBIModelingService.RpcHost.RpcTransportOptions;
using Xunit;

namespace PowerBIModelingService.Tests;

public sealed class PbirMaterializationRpcContractTests
{
    [Fact]
    public void Contract_ExposesOnlyTheThreeAuthorizedOperations()
    {
        Assert.Equal(
            new[]
            {
                "pbir/materialization/apply",
                "pbir/materialization/preview",
                "pbir/materialization/recovery/inspect"
            },
            PbirMaterializationRpcContract.SupportedOperations.OrderBy(value => value));
    }

    [Fact]
    public void Adapter_RejectsUnsupportedVersionAndUnknownFieldsBeforeOrchestration()
    {
        var adapter = PbirMaterializationRpcAdapter.CreateForTests();
        using var unsupported = JsonDocument.Parse("""
            {"schemaVersion":"pbir-local-materialization-rpc-request/v2","requestId":"request-1","operation":"pbir/materialization/preview","input":{}}
            """);
        using var unknown = JsonDocument.Parse("""
            {"schemaVersion":"pbir-local-materialization-rpc-request/v1","requestId":"request-1","operation":"pbir/materialization/preview","unexpected":true,"input":{}}
            """);

        Assert.Equal("invalid-request", adapter.ValidateForTests(unsupported.RootElement).Outcome);
        Assert.Equal("invalid-request", adapter.ValidateForTests(unknown.RootElement).Outcome);

        using var duplicate = JsonDocument.Parse("""
            {"schemaVersion":"pbir-local-materialization-preview-request/v1","requestId":"request-1","requestId":"request-2","operation":"pbir/materialization/preview","input":{}}
            """);
        Assert.Equal("invalid-request", adapter.ValidateForTests(duplicate.RootElement).Outcome);
        Assert.True(PbirMaterializationRpcValidation.MaxRequestPayloadBytes <= RpcTransportOptions.Production.MaxPayloadBytes);
        Assert.True(PbirMaterializationRpcValidation.MaxResponsePayloadBytes <= RpcTransportOptions.Production.MaxResponseBytes);
    }

    [Fact]
    public async Task Adapter_ValidPreviewUsesPhase31AndReturnsSafeRelativeMetadata()
    {
        using var directory = new PbirMaterializationOrchestrationServiceTests.TemporaryDirectory();
        var input = PbirMaterializationOrchestrationServiceTests.CreateInput(directory.Path);
        using var payload = CreatePayload(
            PbirMaterializationRpcContract.PreviewRequestSchemaVersion,
            PbirMaterializationRpcContract.PreviewOperation,
            "phase33-preview",
            input);

        var response = await PbirMaterializationRpcAdapter.CreateForTests().HandleAsync(
            PbirMaterializationRpcContract.PreviewOperation,
            payload.RootElement,
            CancellationToken.None);

        Assert.Equal("absent-destination", response.Outcome);
        Assert.NotNull(response.ValidatedPreview);
        Assert.Empty(response.WrittenFiles);
        Assert.DoesNotContain(response.Diagnostics, diagnostic => diagnostic.Message.Contains(directory.Path, StringComparison.Ordinal));
        Assert.DoesNotContain(response.Diagnostics, diagnostic => diagnostic.Message.Contains("staging", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Adapter_ApplyRequiresExactPreviewAndFreshTransactionAndRecoveryDoesNotMutate()
    {
        using var directory = new PbirMaterializationOrchestrationServiceTests.TemporaryDirectory();
        var input = PbirMaterializationOrchestrationServiceTests.CreateInput(directory.Path);
        var adapter = PbirMaterializationRpcAdapter.CreateForTests();
        using var previewPayload = CreatePayload(
            PbirMaterializationRpcContract.PreviewRequestSchemaVersion,
            PbirMaterializationRpcContract.PreviewOperation,
            "phase33-preview-apply",
            input);
        var preview = await adapter.HandleAsync(
            PbirMaterializationRpcContract.PreviewOperation,
            previewPayload.RootElement,
            CancellationToken.None);

        using var applyPayload = CreatePayload(
            PbirMaterializationRpcContract.ApplyRequestSchemaVersion,
            PbirMaterializationRpcContract.ApplyOperation,
            "phase33-apply",
            input,
            new { validatedPreview = preview.ValidatedPreview, transactionId = "phase33-transaction", applyApproved = true });
        var applied = await adapter.HandleAsync(
            PbirMaterializationRpcContract.ApplyOperation,
            applyPayload.RootElement,
            CancellationToken.None);
        Assert.Equal("applied", applied.Outcome);
        Assert.Equal("phase33-transaction", applied.TransactionId);

        var before = Directory.EnumerateFileSystemEntries(directory.Path, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(directory.Path, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        using var recoveryPayload = CreatePayload(
            PbirMaterializationRpcContract.RecoveryRequestSchemaVersion,
            PbirMaterializationRpcContract.RecoveryOperation,
            "phase33-recovery",
            input,
            new { previewRequestId = "phase33-preview-apply" });
        var recovery = await adapter.HandleAsync(
            PbirMaterializationRpcContract.RecoveryOperation,
            recoveryPayload.RootElement,
            CancellationToken.None);
        var after = Directory.EnumerateFileSystemEntries(directory.Path, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(directory.Path, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal("exact-match", recovery.Outcome);
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Adapter_CancellationBeforeDispatchDoesNotInvokeOrchestration()
    {
        using var directory = new PbirMaterializationOrchestrationServiceTests.TemporaryDirectory();
        var input = PbirMaterializationOrchestrationServiceTests.CreateInput(directory.Path);
        using var payload = CreatePayload(
            PbirMaterializationRpcContract.PreviewRequestSchemaVersion,
            PbirMaterializationRpcContract.PreviewOperation,
            "phase33-cancelled",
            input);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            PbirMaterializationRpcAdapter.CreateForTests().HandleAsync(
                PbirMaterializationRpcContract.PreviewOperation,
                payload.RootElement,
                cancellation.Token));
        Assert.Empty(Directory.EnumerateFileSystemEntries(directory.Path));
    }

    [Fact]
    public void Adapter_DoesNotDependDirectlyOnPhase30WriterServices()
    {
        var fieldTypes = typeof(PbirMaterializationRpcAdapter)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Select(field => field.FieldType)
            .ToArray();
        Assert.DoesNotContain(fieldTypes, type => type.Name.Contains("Writer", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(fieldTypes, type => type.Name.Contains("FileSystem", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(fieldTypes, type => type.Name.Contains("Rollback", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(fieldTypes, type => type.Name == "PbirMaterializationOrchestrationService");
    }

    [Fact]
    public async Task Adapter_ResponseSerializationIsVersionedAndRedacted()
    {
        using var directory = new PbirMaterializationOrchestrationServiceTests.TemporaryDirectory();
        var input = PbirMaterializationOrchestrationServiceTests.CreateInput(directory.Path);
        using var payload = CreatePayload(
            PbirMaterializationRpcContract.PreviewRequestSchemaVersion,
            PbirMaterializationRpcContract.PreviewOperation,
            "phase33-serialization",
            input);
        var response = await PbirMaterializationRpcAdapter.CreateForTests().HandleAsync(
            PbirMaterializationRpcContract.PreviewOperation,
            payload.RootElement,
            CancellationToken.None);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Contains("\"schemaVersion\":\"pbir-local-materialization-response/v1\"", json, StringComparison.Ordinal);
        Assert.Contains("\"outcome\":\"absent-destination\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain(directory.Path, json, StringComparison.Ordinal);
        Assert.DoesNotContain("staging", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("backup", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("journal", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Adapter_MapsEveryPhase31OutcomeToAStableWireValue()
    {
        var adapter = PbirMaterializationRpcAdapter.CreateForTests();
        var expected = new Dictionary<PbirMaterializationOrchestrationOutcome, string>
        {
            [PbirMaterializationOrchestrationOutcome.Absent] = "absent-destination",
            [PbirMaterializationOrchestrationOutcome.Empty] = "empty-destination",
            [PbirMaterializationOrchestrationOutcome.ExactMatch] = "exact-match",
            [PbirMaterializationOrchestrationOutcome.ManagedReplacement] = "managed-replacement",
            [PbirMaterializationOrchestrationOutcome.Conflict] = "conflict",
            [PbirMaterializationOrchestrationOutcome.RecoveryRequired] = "recovery-required",
            [PbirMaterializationOrchestrationOutcome.Applied] = "applied",
            [PbirMaterializationOrchestrationOutcome.StalePreview] = "stale-preview",
            [PbirMaterializationOrchestrationOutcome.InvalidRequest] = "invalid-request",
            [PbirMaterializationOrchestrationOutcome.UnsafeDestination] = "unsafe-destination",
            [PbirMaterializationOrchestrationOutcome.UnsupportedOperation] = "unsupported-operation",
            [PbirMaterializationOrchestrationOutcome.SchemaFailure] = "schema-failure",
            [PbirMaterializationOrchestrationOutcome.TransactionReused] = "transaction-reused",
            [PbirMaterializationOrchestrationOutcome.Cancelled] = "cancelled",
            [PbirMaterializationOrchestrationOutcome.Failure] = "failure"
        };

        foreach (var (outcome, wireValue) in expected)
        {
            var result = new PbirMaterializationOrchestrationResult(
                PbirMaterializationOrchestrationResultContract.SchemaVersionV1,
                "phase33-map",
                outcome,
                null,
                null,
                null,
                false,
                [],
                null,
                null,
                null,
                PbirMaterializationOrchestrationDiagnostics.Empty);
            Assert.Equal(wireValue, adapter.MapForTests(PbirMaterializationRpcContract.PreviewOperation, result).Outcome);
        }
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData(".pbir-design-analyzer")]
    [InlineData("Sales.Report.pbip")]
    [InlineData("Sales\\Report")]
    public async Task Adapter_RejectsUnsafeDestinationsBeforeOrchestration(string target)
    {
        using var directory = new PbirMaterializationOrchestrationServiceTests.TemporaryDirectory();
        var input = PbirMaterializationOrchestrationServiceTests.CreateInput(directory.Path, target);
        using var payload = CreatePayload(
            PbirMaterializationRpcContract.PreviewRequestSchemaVersion,
            PbirMaterializationRpcContract.PreviewOperation,
            "phase33-unsafe",
            input);

        var response = await PbirMaterializationRpcAdapter.CreateForTests().HandleAsync(
            PbirMaterializationRpcContract.PreviewOperation,
            payload.RootElement,
            CancellationToken.None);

        Assert.Equal("invalid-request", response.Outcome);
        Assert.DoesNotContain(response.Diagnostics, diagnostic => diagnostic.Code.StartsWith("PBIR31-", StringComparison.Ordinal));
    }

    private static JsonDocument CreatePayload(
        string schemaVersion,
        string operation,
        string requestId,
        PbirMaterializationOrchestrationInput input)
        => CreatePayload(schemaVersion, operation, requestId, input, null);

    private static JsonDocument CreatePayload(
        string schemaVersion,
        string operation,
        string requestId,
        PbirMaterializationOrchestrationInput input,
        object? extra)
    {
        var payload = new Dictionary<string, object?>
        {
            ["schemaVersion"] = schemaVersion,
            ["requestId"] = requestId,
            ["operation"] = operation,
            ["input"] = input
        };
        if (extra is not null)
        {
            foreach (var property in JsonSerializer.SerializeToElement(extra).EnumerateObject())
            {
                payload[property.Name] = property.Value;
            }
        }
        var json = JsonSerializer.Serialize(payload, PbirMaterializationRpcAdapterSerializerOptions());
        return JsonDocument.Parse(json);
    }

    private static JsonSerializerOptions PbirMaterializationRpcAdapterSerializerOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
