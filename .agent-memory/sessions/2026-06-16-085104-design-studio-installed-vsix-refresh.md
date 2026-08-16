# 2026-06-16 Design Studio Installed VSIX Refresh

## Goal

Refresh the installed VS Code Insiders extension so Design Studio no longer runs a stale `design-studio.js` bundle that still references `process`.

## What Changed

- Rebuilt the shipped VSIX from the current workspace:
  - `cd vscode-extension && npm run package`
- Reinstalled the resulting artifact into VS Code Insiders:
  - `/Applications/Visual Studio Code - Insiders.app/Contents/Resources/app/bin/code --install-extension /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/vscode-extension/pbir-design-analyzer-0.6.0-darwin-arm64.vsix --force`

## Evidence

- Before reinstall, the installed bundle at `~/.vscode-insiders/extensions/bcrowell.pbir-design-analyzer-0.6.0/webview-dist/design-studio.js` still contained `process.env.NODE_ENV`.
- The workspace bundle at `vscode-extension/webview-dist/design-studio.js` did not contain `process.env.NODE_ENV`.
- After reinstall, the installed bundle was verified directly on disk:
  - `INSTALLED_NO_PROCESS_ENV`
  - `PROCESS_TOKEN_COUNT=0`
  - `SIZE=177818`

## Validation

- `cd vscode-extension && npm run package`
- `/Applications/Visual Studio Code - Insiders.app/Contents/Resources/app/bin/code --install-extension /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/vscode-extension/pbir-design-analyzer-0.6.0-darwin-arm64.vsix --force`

## Remaining Step

- Reload the VS Code Insiders window and reopen Report Design Studio to ensure the running webview host is using the refreshed installed payload instead of any cached script state.
