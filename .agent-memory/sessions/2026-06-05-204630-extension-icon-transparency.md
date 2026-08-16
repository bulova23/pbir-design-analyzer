# 2026-06-05 Extension Icon Transparency

## Objective

- Remove the opaque white icon background so the extension icon works cleanly in VS Code dark and light themes.

## Changes

- Updated `vscode-extension/resources/icon.png` to a transparent PNG.
- Preserved the existing green bars and green magnifying glass branding.
- Removed the white outer canvas and pale rounded-square background so only the green logo elements remain visible.
- Applied a very small color/contrast boost so the transparent mark keeps its visual weight.

## Validation

- Verified the exported asset remains a PNG with alpha transparency.
- Checked transparent corner pixels directly.
- Generated dark and light preview composites locally to confirm theme compatibility.

## Outcome

- The icon now allows the VS Code or Marketplace theme background to show through cleanly without the old opaque white square.
