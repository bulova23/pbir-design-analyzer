using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35CDurableAuditStore(Func<DateTimeOffset>? clock = null)
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);
    private readonly List<Phase35CAuditRecord> _records = [];
    internal IReadOnlyList<Phase35CAuditRecord> Records => _records.AsReadOnly();

    internal Phase35CAuditRecord Append(Phase35CAuditEvent auditEvent)
    {
        var previous = _records.LastOrDefault()?.CurrentHash ?? string.Empty;
        var sequence = _records.Count + 1;
        var record = new Phase35CAuditRecord(sequence, auditEvent, previous, string.Empty, _clock());
        var current = Hash(record with { CurrentHash = string.Empty });
        record = record with { CurrentHash = current };
        _records.Add(record);
        return record;
    }

    internal Phase35CAuditValidation ValidateChain(IReadOnlyList<Phase35CAuditRecord>? records = null)
    {
        var input = records ?? _records;
        var reasons = new List<string>();
        var previous = string.Empty;
        for (var index = 0; index < input.Count; index++)
        {
            var record = input[index];
            if (record.Sequence != index + 1) reasons.Add("sequence-gap");
            if (record.PreviousHash != previous) reasons.Add("previous-hash-mismatch");
            if (record.CurrentHash != Hash(record with { CurrentHash = string.Empty })) reasons.Add("current-hash-mismatch");
            previous = record.CurrentHash;
        }
        return new(reasons.Count == 0, reasons.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static string Hash(Phase35CAuditRecord record) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(record)))).ToLowerInvariant();
}
