# PBIR Design Analyzer Repository Wiki

This repository-local wiki router is intentionally small and source-linked. It provides the entry point for Atomic Claude context without becoming a memory store.

## Scope

- Repository: `pbir-design-analyzer`
- Primary source tree: repository files and Git history
- Signals: [scan.md](scan.md)
- Atomic configuration: [../../.claude/atomic.toml](../../.claude/atomic.toml)
- Pilot controls: [../../.claude/CLAUDE.local.md](../../.claude/CLAUDE.local.md)

## Context boundary

This wiki and its signals are local repository context only. They are not AI Memory records, are not PostgreSQL-backed, and must not be promoted automatically into shared memory.

## Refresh

Refresh the deterministic repository signals with:

```sh
atomic signals scan
```

Review `scan.md` against the source tree before relying on inferred context. No capture buckets, realm wiki, inter-session bus, or automatic learning surface is enabled for this pilot.
