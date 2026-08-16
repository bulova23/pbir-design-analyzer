# Phase 35I Windows Containment Threat Model

| Threat | Mitigation | Evidence/test | Residual risk / provider blocker |
|---|---|---|---|
| process breakaway or child escape | Job membership, no breakaway default, process limit | direct/nested child tests | Windows execution required; blocks provider until passed |
| Job misconfiguration | one native owner, ordered assignment, query | boundary and Job tests | implementation error remains; blocks provider |
| token misconfiguration | maximum privilege disablement and token evidence | restricted-token/ACL tests | integrity and account authority remain; blocks provider |
| token duplication | no exposed token handles; explicit close | handle review | worker compromise residual; blocks provider |
| handle inheritance | `inheritHandles=false`, no inherited handles | environment/handle test | native API misuse; blocks provider |
| environment leakage | explicit empty environment block | synthetic secret test | runtime-required variables must be certified |
| runner substitution | package/executable hash and relative identity | admission and identity test | TOCTOU remains; blocks provider |
| DLL/search-order hijack | controlled installation root and absolute launch target | path review | atomic identity not proven; blocks provider |
| junction/reparse escape | normalized root/path validation | path test | reparse and ACL deployment proof remains |
| directory ACL weakness | session-owned worker root | Windows ACL test | worker account authority remains |
| worker crash/orphan | kill-on-close Job Object | kill-on-close test | crash timing/unobserved OS state; blocks provider |
| process accounting error | Job query and evidence | accounting test | Windows proof required |
| network access | no Phase35I network mechanism | none | unrestricted network; future blocker |
| signed malicious runner | certification identity and inert-only phase | certification review | signer compromise; blocks provider |

This phase does not claim VM isolation, network isolation, credential access, provider execution, PBIR generation, or publication.
