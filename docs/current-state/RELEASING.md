# Releasing PBIR Design Analyzer

The current release target contract is maintained in [vscode-extension/config/release-targets.json](../../vscode-extension/config/release-targets.json). For the current 0.7.0 package, the contract names these artifacts:

- pbir-design-analyzer-0.7.0-win32-x64.vsix
- pbir-design-analyzer-0.7.0-win32-arm64.vsix
- pbir-design-analyzer-0.7.0-linux-x64.vsix
- pbir-design-analyzer-0.7.0-darwin-x64.vsix
- pbir-design-analyzer-0.7.0-darwin-arm64.vsix

This repo publishes releases to two places:

- GitHub Releases, with platform-targeted `.vsix` files attached as downloadable assets
- the VS Code Marketplace, so users can install and update from inside VS Code

For `0.5.0`, Marketplace publication is manual. Do not run an automated publish step as part of final package preparation.

The repo stays source-only. Built `.vsix` files remain ignored in git and are distributed through release assets instead.

## Backend Artifact Ownership

Workstream 4B defines the backend packaging boundary as follows.

Source-owned:

- backend source under `service-dotnet/`
- packaging and rebuild scripts under `vscode-extension/scripts/`
- release and packaging metadata under:
  - `vscode-extension/package.json`
  - `README.md`
  - `docs/current-state/RELEASING.md`

Generated but not source-authored:

- `vscode-extension/backend/rpc/`
  - current-machine local development build output
  - rebuilt by `npm run build` or `npm run build:backend`
  - ignored in git
- `vscode-extension/backend/targets/<target>/rpc/`
  - target-specific packaged backend staging output
  - rebuilt by `npm run package:all` or targeted `build-backend.mjs` invocations
  - intentionally checked in today as reproducible packaging artifacts

Packaging-owned:

- `pbir-design-analyzer-<version>-<target>.vsix`
- the staged `backend/rpc/` payload copied into each VSIX

Do not manually edit generated backend payloads under `backend/rpc/` or `backend/targets/`.

If checked-in target cleanup happens later, ship that as a separate release-process change. Do not mix it into ordinary feature work.

## One-Time Setup

### 1. Create the VS Code Marketplace publisher

The extension manifest currently uses the publisher ID `bcrowell` in [vscode-extension/package.json](../vscode-extension/package.json). The Visual Studio Marketplace publisher must use the same publisher ID before automated publish will succeed.

### 2. Create the Marketplace token

Per the official VS Code publishing guidance, create an Azure DevOps Personal Access Token with:

- Organization: `All accessible organizations`
- Scope: `Marketplace (Manage)`

Store that token as a GitHub Actions repository secret named `VSCE_PAT`.

### 3. GitHub permissions

The release workflow uses the built-in `GITHUB_TOKEN` with `contents: write` permission to create or update GitHub Releases and upload the `.vsix` asset.

## Recommended Release Flow

1. Update the extension version in `vscode-extension/package.json` and `vscode-extension/package-lock.json`.
2. Commit and push the version bump to `main`.
3. Create and push a matching tag:

```bash
git tag v0.1.10
git push origin v0.1.10
```

4. The release preparation flow should:
   - verify the git tag matches the extension version
   - run backend tests
   - run extension and webview tests
   - run backend target verification
   - package five platform-targeted VSIX files:
     - `pbir-design-analyzer-<version>-win32-x64.vsix`
     - `pbir-design-analyzer-<version>-win32-arm64.vsix`
     - `pbir-design-analyzer-<version>-linux-x64.vsix`
     - `pbir-design-analyzer-<version>-darwin-x64.vsix`
     - `pbir-design-analyzer-<version>-darwin-arm64.vsix`
   - upload the VSIX files as workflow artifacts
   - create or update the GitHub Release for the tag with all five assets if desired
   - stop before Marketplace publication so the release owner can upload manually

## Release Triggers

### Automatic release

Push a semantic version tag that matches `v*.*.*`, for example:

```bash
git push origin v0.1.10
```

### Manual release

Use the `Release` workflow from the Actions tab with:

- `tag`: an existing tag, such as `v0.1.10`
- `prerelease`: optional toggle for GitHub pre-releases
- `publish_marketplace`: disable for `0.5.0`

## CI Artifacts

The regular `CI` workflow now:

- runs build and test validation on Ubuntu, Windows, and macOS
- packages the public target-specific VSIX files on Ubuntu
- uploads the generated VSIX set as a short-lived workflow artifact

## Packaging Isolation

The packaging scripts now build each target backend into its own target-specific staging directory and package from a temporary isolated extension root.

- `package:all` is intentionally serial
- a lock file prevents concurrent packaging invocations from reusing mutable staging
- Windows arm64 self-contained backend files cannot contaminate the Windows x64, Linux x64, macOS x64, or macOS arm64 artifacts because each target uses isolated backend staging

## Backend Target Rebuild And Cleanup

Use these commands from `vscode-extension/`:

```bash
npm run build:backend
npm run verify:backend:targets
npm run clean:backend:targets
npm run package:all
```

Command behavior:

- `npm run build:backend`
  - rebuilds `backend/rpc/` for the current host platform only
- `npm run verify:backend:targets`
  - verifies the five supported target directories under `backend/targets/`
  - verifies the runtime-critical backend files for each target
  - fails if a required target is missing or if an unexpected target directory appears
- `npm run clean:backend:targets`
  - removes only the known generated staging directories under `backend/targets/`
  - does not touch backend source or repo-local `service-dotnet/` outputs
- `npm run package:all`
  - rebuilds each target into `backend/targets/<target>/rpc/`
  - stages each target into an isolated temporary extension root
  - emits the five target-specific VSIX files

For a single target rebuild without full packaging:

```bash
cd vscode-extension
node scripts/build-backend.mjs --target darwin-arm64 --output backend/targets/darwin-arm64/rpc
```

Supported targets are:

- `win32-x64`
- `win32-arm64`
- `linux-x64`
- `darwin-x64`
- `darwin-arm64`

## Packaged Runtime Validation

### macOS Intel acceptance policy

The `darwin-x64` package remains part of the five-target release set, but the hosted `macos-13` runner is not a release gate because it can remain queued indefinitely. The CI and release workflows therefore validate the package contents and target contract for `darwin-x64`; runtime acceptance is supplied by the retained local Rosetta packaged-workflow proof. Do not classify this target as unvalidated, and do not reintroduce a queued hosted Intel leg without a supported runner.

Bucket A removed runtime fallback to repo-local `service-dotnet/RpcHost/bin/Debug/...` and `Release/...` outputs.

That means release validation must assume:

- repo-local Debug and Release leftovers are irrelevant to runtime selection
- a packaged VSIX must work from its own staged `backend/rpc/` payload
- packaged-runtime failures must be fixed in packaging or staged assets, not masked by local backend publishes

Recommended validation sequence:

1. Run backend tests.
2. Run extension and webview tests.
3. Run `cd vscode-extension && npm run verify:backend:targets`.
4. Run `cd vscode-extension && npm run package:all`.
5. Install the matching VSIX on the target machine.
6. Open a real PBIR workspace and score a report.
7. Confirm that backend startup succeeds without depending on repo-local `service-dotnet/RpcHost/bin` output.

## Cross-Platform Scoring Gate

Before publishing `0.5.0`, treat score determinism as a release gate:

- the same PBIR report fingerprint must produce the same score output on every supported platform
- theme, locale, newline style, path separators, filesystem traversal order, and machine architecture must not change scoring outcomes
- if two machines produce different scores, do not publish until the fingerprint and score diagnostics explain why

Use the score diagnostics workflow:

1. Run PBIR Design Analyzer: Score Report on the same report copy.
2. Run PBIR Design Analyzer: Copy Score Diagnostics.
3. Capture the JSON from the clipboard or from the PBIR Score Diagnostics output channel.
4. Compare:
   - extension version
   - backend version
   - platform and architecture
   - analyzer type and analyzer profile
   - report fingerprint
   - score
   - issue counts
   - readiness score and readiness band
   - page processing order
   - finding IDs and evidence counts

For a direct repo-side comparison, save each diagnostic payload to disk and run:

```bash
cd vscode-extension
node scripts/compare-score-diagnostics.mjs /path/to/windows.json /path/to/macos.json
```

Expected result:

- matching report fingerprints must produce matching score outputs
- if report fingerprints differ, treat the input copies as non-identical and resolve that first

Manual cross-platform smoke for `0.5.0` should record one diagnostic snapshot per tested platform build.

## Windows ARM64 Note

Windows arm64 is part of the final `0.5.0` package set.

- build it with `npm run package:all` or `node scripts/package-vsix.mjs --target win32-arm64`
- that target intentionally uses a self-contained backend publish for `0.5.0`
- expect the Windows arm64 VSIX to be much larger than the other target-specific packages because it bundles the .NET runtime
- keep it in the manual upload set alongside the framework-dependent Windows x64, Linux x64, macOS x64, and macOS arm64 packages

## Icon Rendering Note

The icon source PNG is transparent and the packaged copy should match it byte-for-byte.

If VS Code shows the icon on a light tile in the extension details page, treat that as VS Code rendering behavior rather than a release blocker.

## Manual Marketplace Upload Checklist

For `0.5.0`, do not run a Marketplace publish command from this repo during release prep.

Manual upload set:

- `pbir-design-analyzer-0.5.0-win32-x64.vsix`
- `pbir-design-analyzer-0.5.0-win32-arm64.vsix`
- `pbir-design-analyzer-0.5.0-linux-x64.vsix`
- `pbir-design-analyzer-0.5.0-darwin-x64.vsix`
- `pbir-design-analyzer-0.5.0-darwin-arm64.vsix`

Before manual upload:

1. Rebuild from a clean packaging state.
2. Inspect each VSIX for target, backend RID, publish model, and version.
3. Confirm the packaged icon path is present.
4. Confirm the Windows arm64 package remains self-contained and the other targets remain framework-dependent.
5. Keep all five packages together for the same `0.5.0` listing.

## Manual Marketplace Upload Procedure For Platform-Targeted VSIX Files

The official VS Code publishing docs confirm two things that matter for `0.5.0`:

- manual upload through the Visual Studio Marketplace publisher management page is supported
- platform-specific extensions are published as separate packages

They do not explicitly document portal-specific behavior for uploading the second, third, fourth, and fifth platform package for the same extension version. Because of that, the safest release guidance for `0.5.0` combines:

- official Marketplace publishing guidance
- the `vsce` implementation behavior used by VS Code extension publishers
- direct inspection of the packaged `TargetPlatform` metadata inside each rebuilt VSIX

### What Is Confirmed

- Each `0.5.0` VSIX carries a target in `extension.vsixmanifest` through the `TargetPlatform` field.
- The official docs describe platform-specific extensions as separate packages.
- The official docs also describe manual VSIX upload through the publisher management page.
- The `vsce` publish implementation checks for duplicate publication using both:
  - extension version
  - target platform

That duplicate check strongly indicates that later uploads for a different target are intended to attach another platform package to the same listing rather than overwrite an existing different-target package.

### What Is Not Explicitly Documented

- no official doc was found that requires a specific manual upload order
- no official doc was found that states, in portal-specific language, whether later uploads replace or append per-target variants

Because that portal behavior is under-documented, upload conservatively and verify after each file.

### Safest Manual Upload Steps

1. Go to the Visual Studio Marketplace publisher management page:
   - `https://marketplace.visualstudio.com/manage/publishers/`
2. Open the existing `bcrowell.pbir-design-analyzer` extension listing.
3. Upload the `0.5.0` packages one at a time.
4. Wait for each upload to finish processing before starting the next upload.
5. Stop immediately if the portal behaves as though it is replacing the whole `0.5.0` release instead of accepting an additional platform package.

Recommended operational order for `0.5.0`:

1. `pbir-design-analyzer-0.5.0-win32-x64.vsix`
2. `pbir-design-analyzer-0.5.0-linux-x64.vsix`
3. `pbir-design-analyzer-0.5.0-darwin-x64.vsix`
4. `pbir-design-analyzer-0.5.0-darwin-arm64.vsix`
5. `pbir-design-analyzer-0.5.0-win32-arm64.vsix`

This order is an operational preference, not a documented Marketplace rule. It front-loads the smaller framework-dependent packages and leaves the large Windows arm64 self-contained package last.

### What To Watch For During Upload

- If the portal accepts the file and continues to show `0.5.0`, proceed to the next target.
- If the portal reports that the same target/version already exists, do not retry that same file.
- If the portal appears to replace the existing uploaded target package set rather than add another target-specific package, stop and do not upload the remaining files until that behavior is understood.

### Practical Risk Call

- Low risk:
  - the Marketplace supports platform-specific extension publication
  - the rebuilt VSIX files contain correct target metadata
- Medium risk:
  - the public docs are clearer about `vsce publish --target` and `vsce publish --packagePath` than about repeated manual portal uploads for one version
- Release recommendation:
  - manual upload is acceptable for `0.5.0`
  - perform it sequentially
  - verify after each upload
  - do not batch-assume portal behavior that the docs do not explicitly state

## Notes

- The release workflow fails if the git tag version and `package.json` version do not match.
- The Marketplace publish step uses `--skip-duplicate` so reruns do not fail if that exact version is already published.
- GitHub Releases are the right place for downloadable `.vsix` files. The repo should not commit them to source control.
