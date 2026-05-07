# Contributing

Thanks for contributing to PBIR Design Analyzer.

## How To Submit Feedback

Use the repository's issue forms for all public requests:

- **Bug report** for broken behavior, regressions, or incorrect analysis results
- **Feature request** for new capabilities or enhancements
- **Support question** for usage or configuration help
- **Documentation improvement** for missing or incorrect docs

Each form captures the information needed to reproduce, triage, and track the request.

## How Issues Are Tracked

Every new submission should enter triage with a type label and `status: triage`.

Maintainers then move issues through the following status labels:

- `status: triage` - newly submitted and not reviewed yet
- `status: needs-info` - more information is required from the reporter
- `status: planned` - accepted and queued for future work
- `status: in-progress` - actively being implemented or investigated
- `status: released` - shipped in a tagged release

Type labels indicate the request category:

- `bug`
- `enhancement`
- `question`
- `documentation`

Area labels can be applied during triage to narrow ownership, such as scoring, governance, extension UI, or backend.

## How Requests Get Resolved

When a pull request fixes an issue, link it using a closing keyword in the PR description:

```text
Fixes #123
```

That automatically closes the issue when the PR is merged. If the work ships in a tagged release, maintainers should also apply `status: released` so users can see that the request has been delivered.

## Good Reports

Good issues are specific and reproducible:

- include the extension version
- include VS Code and OS details when relevant
- include clear reproduction steps
- include logs, screenshots, or sample output when possible
- avoid customer-sensitive PBIR data in attachments

## Scope

This public repo is intentionally focused on PBIR Design Analyzer. Requests for unrelated Fabric, TMDL, semantic-model authoring, or AI tooling should not be filed here unless they directly affect this extension.
