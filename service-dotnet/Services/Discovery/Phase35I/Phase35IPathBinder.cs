namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35IPathBinder
{
    internal string Bind(string workerRoot, string sessionId, Phase35IRunnerIdentity runner)
    {
        if (string.IsNullOrWhiteSpace(workerRoot) || string.IsNullOrWhiteSpace(sessionId) || Path.IsPathRooted(runner.ExecutableRelativePath) || runner.ExecutableRelativePath.Split(['/', '\\']).Any(part => part == "..")) throw new InvalidOperationException(Phase35IFailureCode.ExecutablePathInvalid.ToString());
        var root = Path.GetFullPath(workerRoot);
        var target = Path.GetFullPath(Path.Combine(root, runner.ExecutableRelativePath));
        if (!target.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(Phase35IFailureCode.ExecutablePathInvalid.ToString());
        return target;
    }

    internal string BindSessionRoot(string workerRoot, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Contains("..", StringComparison.Ordinal)) throw new InvalidOperationException("session path is invalid");
        var root = Path.GetFullPath(workerRoot);
        var session = Path.GetFullPath(Path.Combine(root, "sessions", EncodeSessionId(sessionId)));
        if (!session.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("session path escaped worker root");
        return session;
    }

    private static string EncodeSessionId(string sessionId) =>
        string.Concat(sessionId.Select(character =>
            IsUnsafeSessionCharacter(character)
                ? $"%{(int)character:X2}"
                : character.ToString()));

    private static bool IsUnsafeSessionCharacter(char character) =>
        Path.GetInvalidFileNameChars().Contains(character) || character is ':' or '/' or '\\' or '*' or '?' or '"' or '<' or '>' or '|';
}
