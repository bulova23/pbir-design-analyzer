using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace Phase35I.InertRunner;

internal static class Program
{
    private static readonly string[] Allowed = ["ReturnSuccess", "ReturnDeterministicHash", "CreateBoundedArtifact", "WaitUntilCancelled", "WaitUntilTimeout", "AttemptChild", "AttemptNestedChild", "BoundedDiagnostics", "RestrictedFileAccessCheck", "ReturnStructuredFailure"];

    private static int Main(string[] args)
    {
        var workload = Parse(args);
        if (workload is null) return Write("InvalidWorkload", false);
        return workload switch
        {
            "ReturnSuccess" => Write("Success", true),
            "ReturnDeterministicHash" => Write(Convert.ToHexString(SHA256.HashData("phase35i-inert"u8.ToArray())).ToLowerInvariant(), true),
            "CreateBoundedArtifact" => Write("bounded-artifact", true),
            "WaitUntilCancelled" or "WaitUntilTimeout" => Wait(),
            "AttemptChild" => Child(false),
            "AttemptNestedChild" => Child(true),
            "BoundedDiagnostics" => Write("diagnostics:bounded", true),
            "RestrictedFileAccessCheck" => Write("access-denied", false),
            "ReturnStructuredFailure" => Write("StructuredFailure", false),
            _ => Write("InvalidWorkload", false)
        };
    }

    private static string? Parse(string[] args)
    {
        if (args.Length != 1 || !args[0].StartsWith("--workload=", StringComparison.Ordinal)) return null;
        var value = args[0]["--workload=".Length..];
        return Allowed.Contains(value, StringComparer.Ordinal) ? value : null;
    }

    private static int Child(bool nested)
    {
        using var child = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, nested ? "--workload=AttemptNestedChild" : "--workload=ReturnSuccess") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true });
        if (child is null) return Write("ChildStartFailed", false);
        child.WaitForExit(5000);
        return Write(child.HasExited ? "ChildAttempted" : "ChildStillRunning", child.HasExited);
    }

    private static int Wait()
    {
        while (true) Thread.Sleep(100);
    }

    private static int Write(string outcome, bool success)
    {
        Console.Out.WriteLine(JsonSerializer.Serialize(new { schemaVersion = "phase35i-inert-runner/v1", outcome, success }));
        return success ? 0 : 7;
    }
}
