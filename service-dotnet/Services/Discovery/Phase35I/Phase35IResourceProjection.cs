namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35IResourceProjection
{
    internal Phase35IResourceProjectionResult Project(Phase35CResourcePolicy policy)
    {
        var job = new Phase35IJobLimits((int)Math.Ceiling(policy.MaxDuration.TotalSeconds), policy.MaxArtifactCount, 0, true, true);
        var worker = new Phase35IWorkerLimits(policy.MaxResultBytes, policy.MaxArtifactCount, policy.MaxArtifactBytes, policy.ConcurrencyLimit);
        return new(job, worker, $"timeout=job;process-count=job;memory=not-configured;kill-on-close=job;no-breakaway=job;result-bytes=worker;artifact-count=worker;artifact-bytes=worker;concurrency=worker");
    }
}
