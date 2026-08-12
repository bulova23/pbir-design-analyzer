namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35CFakeArtifactScanner(Phase35CScannerClassification classification) : IPhase35CArtifactScanner
{
    public Phase35CArtifactScanResult Scan(Phase35CArtifactDescriptor artifact) => new(classification, "fake-scanner/v1");
}

internal sealed class Phase35CArtifactSafetyPipeline(IPhase35CArtifactScanner scanner)
{
    internal Phase35CArtifactSafetyResult Evaluate(Phase35CArtifactDescriptor artifact, Phase35CArtifactSafetyPolicy policy)
    {
        if (!artifact.IdentityValid || !artifact.RedactionValid) return new(Phase35CArtifactDisposition.Rejected, ["artifact-identity-or-redaction-invalid"]);
        if (!policy.AllowedKinds.Contains(artifact.Kind) || artifact.SizeBytes < 0 || artifact.SizeBytes > policy.MaxArtifactBytes) return new(Phase35CArtifactDisposition.Rejected, ["artifact-type-or-size-invalid"]);
        var scan = scanner.Scan(artifact);
        return scan.Classification switch
        {
            Phase35CScannerClassification.Clean => new(Phase35CArtifactDisposition.Accepted, []),
            Phase35CScannerClassification.Suspicious => new(Phase35CArtifactDisposition.Quarantined, ["scanner-suspicious"]),
            Phase35CScannerClassification.Malformed => new(Phase35CArtifactDisposition.Rejected, ["scanner-malformed"]),
            Phase35CScannerClassification.Unsupported => new(Phase35CArtifactDisposition.Rejected, ["scanner-unsupported"]),
            Phase35CScannerClassification.Failure => new(Phase35CArtifactDisposition.Rejected, ["scanner-failure"]),
            _ => new(Phase35CArtifactDisposition.Rejected, ["scanner-unknown"])
        };
    }
}
