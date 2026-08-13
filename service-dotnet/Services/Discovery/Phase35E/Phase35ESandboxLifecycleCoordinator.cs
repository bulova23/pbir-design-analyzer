namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35ESandboxLifecycleCoordinator
{
    internal string CreateWorkingDirectory(string sessionId, string providerId, string auditId, string root)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(auditId) || !Path.IsPathFullyQualified(root)) throw new ArgumentException("sandbox lifecycle identity or root is invalid");
        var safe = string.Join("-", new[] { sessionId, providerId, auditId }.Select(value => new string(value.Where(char.IsLetterOrDigit).ToArray())));
        var path = Path.Combine(root, safe + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    internal bool Cleanup(string path)
    {
        if (!Path.IsPathFullyQualified(path) || !Directory.Exists(path)) return true;
        Directory.Delete(path, true);
        return true;
    }
}
