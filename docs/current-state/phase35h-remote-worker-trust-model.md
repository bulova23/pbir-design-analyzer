# Phase 35H Remote Worker Trust Model

The client signs the canonical typed request with an ephemeral test RSA identity. The worker accepts only the configured client identity, verifies the signature and request hash, and signs every response with the configured worker identity. The client verifies the worker ID and response signature. The harness is not a deployed TLS channel; confidentiality and private-network exposure remain unproven.

The worker does not receive a local `Approved` flag. It independently checks exact fixture certification identity, policy/containment/artifact versions, worker profile, closed workload type, credential-reference shape, and resource bounds. Certification evidence is bound to the request identity and cannot be replaced by metadata.

The future Windows worker profile must add platform-enforced process-tree, token, filesystem, network, environment, memory, CPU, process-count, and output controls. A separate worker image/build/runner identity must be certified and attested. A separate Linux profile is not implied by this proof.
