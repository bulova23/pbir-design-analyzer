using System.Security.Cryptography;
using System.Text.Json;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35DProtectedAuditReplayStore
{
    private readonly string _filePath;
    private readonly Phase35ACanonicalJson _canonical = new();

    internal Phase35DProtectedAuditReplayStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("A bounded persistence file is required.", nameof(filePath));
        _filePath = Path.GetFullPath(filePath);
    }

    internal Phase35DPersistedState Load()
    {
        if (!File.Exists(_filePath)) return Empty();
        try
        {
            var state = _canonical.Deserialize<Phase35DPersistedState>(File.ReadAllBytes(_filePath));
            if (state.SchemaVersion != Phase35DContracts.PersistenceV1 || state.StateHash != Hash(state with { StateHash = string.Empty })) throw new InvalidDataException("Protected certification persistence integrity validation failed.");
            if (state.AuditRecords.Select((record, index) => record.Sequence == index + 1).Any(valid => !valid) || state.AuditRecords.Select((record, index) => record.PreviousHash == (index == 0 ? string.Empty : state.AuditRecords[index - 1].CurrentHash)).Any(valid => !valid) || state.ReplayIdentities.GroupBy(item => item.ExecutionId, StringComparer.Ordinal).Any(group => group.Count() != 1)) throw new InvalidDataException("Protected certification persistence sequence or replay validation failed.");
            return state;
        }
        catch (JsonException exception) { throw new InvalidDataException("Protected certification persistence is malformed.", exception); }
    }

    internal void Save(IReadOnlyList<Phase35DPersistedAuditRecord> auditRecords, IReadOnlyList<Phase35CExecutionIdentity> replayIdentities)
    {
        var state = new Phase35DPersistedState(Phase35DContracts.PersistenceV1, auditRecords.ToArray(), replayIdentities.ToArray(), string.Empty);
        state = state with { StateHash = Hash(state) };
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is null) throw new InvalidDataException("Persistence file must have a directory.");
        Directory.CreateDirectory(directory);
        var temp = _filePath + ".tmp";
        File.WriteAllBytes(temp, _canonical.Serialize(state));
        File.Move(temp, _filePath, true);
    }

    internal void AppendAudit(Phase35CAuditRecord record)
    {
        var state = Load();
        if (record.Sequence != state.AuditRecords.Count + 1) throw new InvalidDataException("Audit sequence is not continuous.");
        if (record.PreviousHash != (state.AuditRecords.LastOrDefault()?.CurrentHash ?? string.Empty)) throw new InvalidDataException("Audit previous hash is not continuous.");
        var persisted = new Phase35DPersistedAuditRecord(record.Sequence, record.Event.SessionId, record.Event.ProviderId, record.Event.Name, record.Event.RequestHash, record.Event.OutcomeHash, record.PreviousHash, record.CurrentHash, record.At);
        Save(state.AuditRecords.Append(persisted).ToArray(), state.ReplayIdentities);
    }

    internal void AddReplay(Phase35CExecutionIdentity identity)
    {
        var state = Load();
        if (state.ReplayIdentities.Any(item => item.ExecutionId == identity.ExecutionId)) throw new InvalidDataException("Duplicate execution identity.");
        Save(state.AuditRecords, state.ReplayIdentities.Append(identity).ToArray());
    }

    private Phase35DPersistedState Empty() => new(Phase35DContracts.PersistenceV1, [], [], Hash(new Phase35DPersistedState(Phase35DContracts.PersistenceV1, [], [], string.Empty)));
    private string Hash(Phase35DPersistedState state) => _canonical.Hash(state with { StateHash = string.Empty });
}
