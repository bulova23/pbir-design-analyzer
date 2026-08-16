using System.Text.Json;
using System.Text.Json.Serialization;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.RpcHost;

internal static class PbirMaterializationRpcValidation
{
    internal const int MaxRequestPayloadBytes = 512 * 1024;
    internal const int MaxResponsePayloadBytes = 2 * 1024 * 1024;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    internal static JsonSerializerOptions SerializerOptions => Options;

    internal static bool HasDuplicateProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name) || HasDuplicateProperties(property.Value))
                {
                    return true;
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                if (HasDuplicateProperties(item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool IsSafeIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 128 &&
        value.All(character => char.IsLetterOrDigit(character) || character is '.' or '_' or ':' or '-');

    internal static bool IsSafeTransactionIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 64 &&
        value.All(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-');

    internal static bool IsSafeDestination(PbirMaterializationOrchestrationInput? input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.OutputBaseDirectory) ||
            !Path.IsPathFullyQualified(input.OutputBaseDirectory) ||
            input.OutputBaseDirectory.Any(char.IsControl))
        {
            return false;
        }

        var target = input.TargetDirectoryName;
        return !string.IsNullOrWhiteSpace(target) && target == target.Normalize() &&
               target is not "." and not ".." &&
               !target.Equals(".pbir-design-analyzer", StringComparison.OrdinalIgnoreCase) &&
               !target.EndsWith(".pbip", StringComparison.OrdinalIgnoreCase) &&
               !target.EndsWith(".SemanticModel", StringComparison.OrdinalIgnoreCase) &&
               !target.Any(char.IsControl) &&
               target.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\', ':']) < 0 &&
               !Path.IsPathRooted(target);
    }

    internal static bool IsSupportedMaterializationInput(PbirMaterializationOrchestrationInput input)
    {
        var request = input.DeployableSerializerRequest;
        var datasetPath = request.DatasetReference?.ByPath?.Path;
        var targetPath = Path.Combine(input.OutputBaseDirectory, input.TargetDirectoryName);
        var baseInfo = new DirectoryInfo(input.OutputBaseDirectory);
        var targetInfo = new DirectoryInfo(targetPath);
        var safeDatasetPath = !string.IsNullOrWhiteSpace(datasetPath) &&
            !Path.IsPathRooted(datasetPath) && !datasetPath.Contains('\\') &&
            !datasetPath.Contains(':') && !datasetPath.Contains("..", StringComparison.Ordinal) &&
            !datasetPath.Any(char.IsControl);

        return request.SchemaVersion == PbirDeployableSerializerRequestContract.SchemaVersionV1 &&
               request.SerializerRequestSchemaVersion == PbirSerializerRequestContract.SchemaVersionV1 &&
               request.TargetFormat == "modernPbir" &&
               request.DefinitionPropertiesSchemaVersion == PbirDeployableSchemaLock.DefinitionPropertiesSchemaVersion &&
               request.DefinitionSchemaVersion == PbirDeployableSchemaLock.DefinitionSchemaVersion &&
               request.LayoutProfileId == "modern-grid-1280x720/v1" &&
               safeDatasetPath &&
               request.SemanticModelInventory is not null &&
               request.SemanticModelInventory.Entries.Select(entry => entry.EntryId).Distinct(StringComparer.Ordinal).Count() == request.SemanticModelInventory.Entries.Count &&
               request.VisualBindings.Select(binding => binding.VisualId).Distinct(StringComparer.Ordinal).Count() == request.VisualBindings.Count &&
               !request.ExecutionPolicy.HasAuthority &&
               Directory.Exists(input.OutputBaseDirectory) &&
               (baseInfo.Attributes & FileAttributes.ReparsePoint) == 0 &&
               (!Directory.Exists(targetPath) || (targetInfo.Attributes & FileAttributes.ReparsePoint) == 0);
    }

    internal static bool IsReadOnlyInput(PbirMaterializationOrchestrationInput? input) =>
        input?.SerializerRequest is
        { ProviderInvocationAllowed: false, DeploymentAllowed: false, MicrosoftSkillsExecutionAllowed: false } &&
        input.DeployableSerializerRequest?.ExecutionPolicy is
        { FilesystemMaterializationAllowed: false, ProviderInvocationAllowed: false,
          MicrosoftSkillsExecutionAllowed: false, ApiInvocationAllowed: false,
          CliInvocationAllowed: false, DeploymentAllowed: false,
          DesktopAutomationAllowed: false, AnalyzerAutomationAllowed: false };

    internal static PbirMaterializationRpcResponse Invalid(string requestId, string operation, string code, string field) =>
        new(
            PbirMaterializationRpcContract.ResponseSchemaVersion,
            requestId,
            operation,
            "invalid-request",
            null,
            null,
            null,
            false,
            [],
            null,
            null,
            null,
            [new(code, field, "The request was rejected safely.")]);

    internal static PbirMaterializationRpcResponse Fault(string requestId, string operation) =>
        new(
            PbirMaterializationRpcContract.ResponseSchemaVersion,
            requestId,
            operation,
            "failure",
            null,
            null,
            null,
            false,
            [],
            null,
            null,
            null,
            [new("PBIR-RPC-FAULT-001", "request", "The local PBIR operation failed safely.")]);
}
