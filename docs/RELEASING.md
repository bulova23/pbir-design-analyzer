# Releasing PBIR Design Analyzer

This repo publishes releases to two places:

- GitHub Releases, with the packaged `.vsix` attached as a downloadable asset
- the VS Code Marketplace, so users can install and update from inside VS Code

The repo stays source-only. Built `.vsix` files remain ignored in git and are distributed through release assets instead.

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

4. The `Release` workflow will:
   - verify the git tag matches the extension version
   - run backend tests
   - run extension and webview tests
   - package `pbir-design-analyzer-<version>.vsix`
   - upload the VSIX as a workflow artifact
   - create or update the GitHub Release for the tag
   - publish the same VSIX to the VS Code Marketplace

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
- `publish_marketplace`: disable only if you want GitHub Release assets without Marketplace publishing

## CI Artifacts

The regular `CI` workflow now uploads the generated `.vsix` as a short-lived workflow artifact. That is useful for validation on pull requests, but it is not the public distribution channel.

## Notes

- The release workflow fails if the git tag version and `package.json` version do not match.
- The Marketplace publish step uses `--skip-duplicate` so reruns do not fail if that exact version is already published.
- GitHub Releases are the right place for downloadable `.vsix` files. The repo should not commit them to source control.
