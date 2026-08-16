# Validation Checklist

Pick the narrowest useful validation for the surface you changed:

- Build the VS Code extension frontend when the change is limited to the webview code.
- Run repo tests only when the UI change could affect behavior, rendering logic, or panel wiring.
- Verify that styling changes remain presentation-only and do not alter scoring, trust boundaries, or mutation paths.
- Confirm constrained-width behavior for panels and webviews where layout changed materially.
