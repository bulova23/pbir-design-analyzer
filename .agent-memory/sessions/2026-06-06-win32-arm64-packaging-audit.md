# 2026-06-06 Win32 ARM64 Packaging Audit

## Objective

- Explain why `pbir-design-analyzer-0.5.0-win32-arm64.vsix` is much larger than the other target packages.
- Verify whether the Windows ARM64 target is self-contained and whether that packaging is tied to successful Windows 11 ARM scoring.

## Evidence Collected

- `vscode-extension/scripts/build-backend.mjs`
  - `win32-arm64` target map entry sets `selfContained: true`
  - `win32-x64`, `linux-x64`, `darwin-x64`, and `darwin-arm64` set `selfContained: false`
- `service-dotnet/RpcHost/RpcHost.csproj`
  - project default is `<SelfContained>false</SelfContained>`
  - target-specific behavior is coming from the packaging script, not the csproj default
- Backend target directory sizes:
  - `win32-arm64/rpc`: `81M`
  - `darwin-arm64/rpc`: `1.4M`
  - `darwin-x64/rpc`: `1.3M`
  - `linux-x64/rpc`: `1.3M`
  - `win32-x64/rpc`: `1.4M`
- VSIX sizes:
  - `pbir-design-analyzer-0.5.0-win32-arm64.vsix`: `33M`
  - other `0.5.0` target VSIX files: about `1.5M` to `1.6M`
- `win32-arm64` runtime files present:
  - `coreclr.dll`
  - `clrjit.dll`
  - `hostfxr.dll`
  - `hostpolicy.dll`
  - `System.Private.CoreLib.dll`
  - `mscordaccore.dll`
  - `mscordbi.dll`
- `win32-arm64` runtime config:
  - uses `"includedFrameworks": [{"name":"Microsoft.NETCore.App","version":"8.0.27"}]`
- `darwin-arm64` runtime config:
  - uses `"framework": {"name":"Microsoft.NETCore.App","version":"8.0.0"}`
  - this is framework-dependent
- Largest `win32-arm64` backend files:
  - `System.Private.CoreLib.dll` `14.7 MB`
  - `System.Private.Xml.dll` `9.3 MB`
  - `coreclr.dll` `5.8 MB`
  - `System.Linq.Expressions.dll` `4.7 MB`
  - `System.Data.Common.dll` `3.3 MB`
  - `Microsoft.DiaSymReader.Native.arm64.dll` `2.6 MB`
  - `System.Private.DataContractSerialization.dll` `2.4 MB`
  - `System.Security.Cryptography.dll` `2.3 MB`
  - `System.Net.Http.dll` `1.9 MB`
  - `clrjit.dll` `1.7 MB`
- Latest user-provided Windows ARM64 diagnostic payload showed:
  - `platform: "win32"`
  - `architecture: "arm64"`
  - `backendRuntimeId: "win-arm64"`
  - `backendTarget: "win32-arm64"`
  - `resultSource: "freshAnalysis"`
  - successful scoring output was produced

## Conclusion

- `win32-arm64` is intentionally self-contained in the current packaging script.
- The other current target packages are framework-dependent.
- The size delta is caused by the bundled .NET 8 runtime and core runtime libraries in the Windows ARM64 backend payload.
- Windows 11 ARM scoring has been validated through the user-supplied fresh-analysis payload from the `win32-arm64` package.

## Recommendation

- For `0.5.0`, keep `win32-arm64` self-contained unless there is time to validate a framework-dependent Windows ARM64 package on real hardware with the required .NET runtime installed.
- Longer-term, define and document a consistent cross-target packaging policy instead of keeping target-specific behavior implicit in `build-backend.mjs`.
