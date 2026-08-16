namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35BDiagnostics(Func<DateTimeOffset>? clock = null)
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);
    private readonly List<Phase35BDiagnosticEvent> _events = [];

    internal IReadOnlyList<Phase35BDiagnosticEvent> Events => _events.ToArray();

    internal void Record(string name, string outcome, Phase35BSession session, params string[] details) =>
        _events.Add(new(name, outcome, session.RequestId, session.ProviderId, details, _clock()));
}
