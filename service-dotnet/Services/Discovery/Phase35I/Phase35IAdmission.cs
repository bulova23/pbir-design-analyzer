namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35IAdmission
{
    internal Phase35IAdmissionDecision Evaluate(Phase35IAdmissionRequest input)
    {
        var failures = new List<Phase35IFailureCode>();
        if (input.RemoteRequest.WorkerProfile != input.WorkerProfile.ProfileId || input.RemoteRequest.Certification.WorkerProfile != input.WorkerProfile.ProfileId) failures.Add(Phase35IFailureCode.WorkerProfileMismatch);
        if (input.WorkerProfile.ContainmentProfileVersion != Phase35IContracts.ContainmentProfileVersion) failures.Add(Phase35IFailureCode.WorkerProfileMismatch);
        if (input.WorkerProfile.RunnerId != input.CertifiedRunner.RunnerId || input.WorkerProfile.RunnerVersion != input.CertifiedRunner.RunnerVersion) failures.Add(Phase35IFailureCode.WorkerProfileMismatch);
        if (input.WorkerProfile.RunnerPackageHash != input.CertifiedRunner.PackageHash || input.WorkerProfile.RunnerExecutableHash != input.CertifiedRunner.ExecutableSha256) failures.Add(Phase35IFailureCode.ExecutableIdentityMismatch);
        if (input.CertifiedRunner.PackageHash.Length != 64 || input.CertifiedRunner.ExecutableSha256.Length != 64 || string.IsNullOrWhiteSpace(input.CertifiedRunner.CertificationEvidenceId)) failures.Add(Phase35IFailureCode.ExecutableIdentityMismatch);
        if (input.RemoteRequest.Workload is not (Phase35HWorkloadType.ReturnSuccess or Phase35HWorkloadType.ReturnDeterministicHash or Phase35HWorkloadType.CreateBoundedArtifact or Phase35HWorkloadType.WaitUntilCancelled or Phase35HWorkloadType.WaitUntilTimeout or Phase35HWorkloadType.ReturnStructuredFailure) || !input.WorkerProfile.SupportedWorkloads.Contains(input.RemoteRequest.Workload)) failures.Add(Phase35IFailureCode.WorkloadNotAllowed);
        if (!input.RemoteRequest.ResourcePolicy.IsValid || !input.WorkerProfile.SupportsMemoryLimit || !input.WorkerProfile.SupportsProcessLimit || !input.WorkerProfile.SupportsTimeoutLimit) failures.Add(Phase35IFailureCode.ResourcePolicyInvalid);
        if (!input.AuditAvailable || string.IsNullOrWhiteSpace(input.RemoteRequest.CorrelationId)) failures.Add(Phase35IFailureCode.AuditCorrelationMissing);
        var projection = new Phase35IResourceProjection().Project(input.RemoteRequest.ResourcePolicy);
        var distinct = failures.Distinct().ToArray();
        return new(distinct.Length == 0, distinct, projection);
    }
}
