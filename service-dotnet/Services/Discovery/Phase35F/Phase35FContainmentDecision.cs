using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PowerBIModelingService.Services.Discovery;

internal enum Phase35FMechanism
{
    NoneLocalMacOs,
    LocalVirtualized,
    RemoteControlled
}

internal enum Phase35FControlState
{
    Enforced,
    PartiallyEnforced,
    Unsupported,
    Unknown,
    NotApplicable
}

internal enum Phase35FFailureCode
{
    PlatformContainmentUnavailable,
    EnforcementCapabilityMissing
}

internal sealed record Phase35FControlCapability(
    string Name,
    bool Required,
    Phase35FControlState State,
    string Mechanism,
    string Proof);

internal sealed record Phase35FPlatformEvidence(
    string OperatingSystem,
    string OsVersion,
    string DarwinVersion,
    string Architecture,
    string RuntimeIdentifier,
    string DotnetVersion);

internal sealed record Phase35FContainmentDecision(
    bool IsAdmitted,
    Phase35FMechanism SelectedMechanism,
    Phase35FFailureCode Failure,
    Phase35FPlatformEvidence Platform,
    IReadOnlyList<Phase35FControlCapability> Capabilities,
    string EvidenceHash);

internal sealed class Phase35FContainmentSelector
{
    internal Phase35FContainmentDecision Evaluate()
    {
        var platform = new Phase35FPlatformEvidence(
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : Environment.OSVersion.Platform.ToString(),
            Environment.OSVersion.VersionString,
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? Environment.OSVersion.Version.ToString() : "not-applicable",
            RuntimeInformation.OSArchitecture.ToString(),
            RuntimeInformation.RuntimeIdentifier,
            Environment.Version.ToString());

        var mechanism = "none-local-macos/v1";
        var capabilities = new[]
        {
            Capability("filesystem-read", mechanism, Phase35FControlState.Unsupported, "No supported local policy boundary can restrict the child to approved roots."),
            Capability("filesystem-write", mechanism, Phase35FControlState.Unsupported, "No supported local policy boundary can restrict writes to the session roots."),
            Capability("network-denial", mechanism, Phase35FControlState.Unsupported, "No proven local policy boundary denies loopback, DNS, TCP, UDP, and Unix-socket access."),
            Capability("child-process-denial", mechanism, Phase35FControlState.Unsupported, "App Sandbox inheritance and helper launch constraints do not prove child creation denial."),
            Capability("environment-isolation", mechanism, Phase35FControlState.Unsupported, "Host-side environment allowlisting is not OS isolation."),
            Capability("process-identity-binding", mechanism, Phase35FControlState.PartiallyEnforced, "Phase35D identity and hash verification occur before launch, but no atomic launch-time identity proof exists."),
            Capability("memory-limit", mechanism, Phase35FControlState.Unsupported, "No selected local mechanism supplies a proven hard memory limit."),
            Capability("cpu-limit", mechanism, Phase35FControlState.Unsupported, "No selected local mechanism supplies a proven hard CPU limit."),
            Capability("execution-timeout", mechanism, Phase35FControlState.PartiallyEnforced, "Phase35E host cancellation and timeout are termination controls, not containment proof."),
            Capability("process-count-limit", mechanism, Phase35FControlState.Unsupported, "No selected local mechanism supplies a proven process-count limit."),
            Capability("secure-termination", mechanism, Phase35FControlState.PartiallyEnforced, "Phase35E owns cancellation, but no admitted contained workload exists to validate orphan resistance."),
            Capability("cleanup-isolation", mechanism, Phase35FControlState.PartiallyEnforced, "Phase35E scopes cleanup directories, but this is not an OS isolation boundary."),
            Capability("stdout-stderr-bounds", mechanism, Phase35FControlState.PartiallyEnforced, "Phase35E bounds captured output after launch; it is not a native resource limit."),
        };

        var evidence = new { mechanism, platform, capabilities, admitted = false, failure = Phase35FFailureCode.PlatformContainmentUnavailable };
        var evidenceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evidence)))).ToLowerInvariant();
        return new(false, Phase35FMechanism.NoneLocalMacOs, Phase35FFailureCode.PlatformContainmentUnavailable, platform, capabilities, evidenceHash);
    }

    private static Phase35FControlCapability Capability(string name, string mechanism, Phase35FControlState state, string proof) => new(name, true, state, mechanism, proof);
}
