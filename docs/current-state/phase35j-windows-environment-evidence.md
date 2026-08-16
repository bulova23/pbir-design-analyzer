# Phase 35J Windows Environment Evidence

## Attempted execution environment

| Field | Measured value |
|---|---|
| OS | macOS 27.0, Darwin 27.0.0, build 26A5406e |
| architecture | arm64 |
| .NET SDK | 10.0.400; .NET 8 SDK 8.0.416 is installed |
| .NET runtime | .NET 8.0.22 |
| worker type | physical/local Mac host; not Windows, VM, CI Windows worker, or Azure VM |
| account | local macOS account; Windows privilege semantics not applicable |
| runner root | no Windows runner installation was staged on this macOS host; Phase35K stages only fixed repository-owned build output on Windows |
| native API capability | not applicable; Windows APIs were not called |

This environment is explicitly not a Windows containment proof. The cross-targeted `net8.0-windows` runtime compiled, but compilation does not exercise token, process, Job Object, ACL, or Windows process-tree semantics.

## Available infrastructure

The repository has a GitHub Actions `windows-latest` job in `.github/workflows/ci.yml`. Phase35K now supplies executable tests and conditional non-Windows skips, but this session did not run that worker, stage its runner, or publish a Windows evidence artifact. A certified Windows execution remains pending.
