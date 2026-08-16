using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirDeployableMaterializationSchemaValidator
{
    private static readonly Lazy<IReadOnlyDictionary<string, JsonNode>> Schemas = new(LoadSchemas);

    internal IReadOnlyList<PbirDeployableMaterializationDiagnostic> Validate(PbirDeployableArtifact artifact)
    {
        var diagnostics = new List<PbirDeployableMaterializationDiagnostic>();
        foreach (var file in artifact.Files)
        {
            try
            {
                if (!Schemas.Value.TryGetValue(file.SchemaUrl, out var schema))
                {
                    diagnostics.Add(new("PBIRMAT-SCHEMA-001", file.RelativePath, "Artifact schema is not in the pinned offline Microsoft schema set."));
                    continue;
                }

                var instance = JsonNode.Parse(file.Content) ?? throw new InvalidDataException("Artifact JSON is null.");
                if (!Evaluate(schema, instance, file.SchemaUrl))
                {
                    diagnostics.Add(new("PBIRMAT-SCHEMA-002", file.RelativePath, "Artifact does not conform to its pinned offline Microsoft schema."));
                }
            }
            catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or System.Text.Json.JsonException)
            {
                diagnostics.Add(new("PBIRMAT-SCHEMA-003", file.RelativePath, "Artifact schema validation could not complete offline."));
            }
        }

        return diagnostics.OrderBy(value => value.Code, StringComparer.Ordinal).ThenBy(value => value.Path, StringComparer.Ordinal).ToArray();
    }

    private static bool Evaluate(JsonNode schemaNode, JsonNode? instance, string baseSchemaId)
    {
        if (schemaNode is not JsonObject schema || schema.Count == 0) return true;
        if (schema["$ref"] is JsonValue referenceValue)
        {
            var (referencedSchema, referencedBase) = ResolveReference(referenceValue.GetValue<string>(), baseSchemaId);
            if (!Evaluate(referencedSchema, instance, referencedBase)) return false;
        }

        if (schema["type"] is { } typeNode && !MatchesType(typeNode, instance)) return false;
        if (schema["const"] is { } constant && !JsonNode.DeepEquals(constant, instance)) return false;
        if (schema["enum"] is JsonArray enumValues && !enumValues.Any(value => JsonNode.DeepEquals(value, instance))) return false;
        if (instance is JsonValue value && value.TryGetValue<string>(out var text))
        {
            if (schema["maxLength"]?.GetValue<int>() is int maxLength && text.Length > maxLength) return false;
            if (schema["pattern"]?.GetValue<string>() is string pattern && !Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant)) return false;
        }
        if (schema["anyOf"] is JsonArray anyOf && !anyOf.Any(option => option is not null && Evaluate(option, instance, baseSchemaId))) return false;
        if (schema["oneOf"] is JsonArray oneOf && oneOf.Count(option => option is not null && Evaluate(option, instance, baseSchemaId)) != 1) return false;

        if (instance is JsonArray array && schema["items"] is JsonNode itemSchema &&
            array.Any(item => !Evaluate(itemSchema, item, baseSchemaId))) return false;

        if (instance is JsonObject jsonObject)
        {
            if (schema["required"] is JsonArray required && required.Any(name => name is null || !jsonObject.ContainsKey(name.GetValue<string>()))) return false;
            var properties = schema["properties"] as JsonObject;
            if (properties is not null)
            {
                foreach (var property in properties)
                {
                    if (property.Value is not null && jsonObject.TryGetPropertyValue(property.Key, out var propertyValue) &&
                        !Evaluate(property.Value, propertyValue, baseSchemaId)) return false;
                }
            }

            var extraProperties = jsonObject.Where(property => properties is null || !properties.ContainsKey(property.Key));
            if (schema["additionalProperties"] is JsonValue additionalValue &&
                additionalValue.TryGetValue<bool>(out var allowed) && !allowed && extraProperties.Any()) return false;
            if (schema["additionalProperties"] is JsonObject additionalSchema &&
                extraProperties.Any(property => !Evaluate(additionalSchema, property.Value, baseSchemaId))) return false;
        }

        return true;
    }

    private static bool MatchesType(JsonNode typeNode, JsonNode? instance)
    {
        if (typeNode is JsonArray types) return types.Any(type => type is not null && MatchesType(type, instance));
        var type = typeNode.GetValue<string>();
        return type switch
        {
            "null" => instance is null,
            "object" => instance is JsonObject,
            "array" => instance is JsonArray,
            "string" => instance is JsonValue stringValue && stringValue.TryGetValue<string>(out _),
            "boolean" => instance is JsonValue boolValue && boolValue.TryGetValue<bool>(out _),
            "integer" => instance is JsonValue integerValue && integerValue.TryGetValue<long>(out _),
            "number" => instance is JsonValue numberValue && (numberValue.TryGetValue<double>(out _) || numberValue.TryGetValue<decimal>(out _)),
            _ => false
        };
    }

    private static (JsonNode Schema, string BaseId) ResolveReference(string reference, string baseSchemaId)
    {
        var resolved = new Uri(new Uri(baseSchemaId), reference);
        var schemaId = resolved.GetLeftPart(UriPartial.Path);
        if (!Schemas.Value.TryGetValue(schemaId, out var root)) throw new InvalidDataException("Schema reference is not pinned locally.");
        if (string.IsNullOrEmpty(resolved.Fragment)) return (root, schemaId);

        JsonNode? current = root;
        foreach (var rawSegment in resolved.Fragment.TrimStart('#').TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var segment = Uri.UnescapeDataString(rawSegment).Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
            current = current?[segment];
            if (current is null) throw new InvalidDataException("Pinned schema reference fragment is missing.");
        }
        return (current, schemaId);
    }

    private static IReadOnlyDictionary<string, JsonNode> LoadSchemas()
    {
        var assembly = typeof(PbirDeployableMaterializationSchemaValidator).Assembly;
        var schemas = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
        foreach (var resourceName in assembly.GetManifestResourceNames().Where(name => name.Contains("PbirSchemas", StringComparison.Ordinal) && name.EndsWith("schema.json", StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidDataException(resourceName);
            using var reader = new StreamReader(stream);
            var schema = JsonNode.Parse(reader.ReadToEnd()) ?? throw new InvalidDataException($"Schema {resourceName} is null.");
            var id = schema["$id"]?.GetValue<string>() ?? throw new InvalidDataException($"Schema {resourceName} has no $id.");
            schemas.Add(id, schema);
        }
        if (schemas.Count != 8) throw new InvalidDataException($"Expected 8 pinned PBIR schemas, found {schemas.Count}.");
        return schemas;
    }
}
