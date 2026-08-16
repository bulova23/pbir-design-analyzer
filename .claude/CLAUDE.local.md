# Atomic Claude pilot controls

This repository uses Atomic Claude only as a local repository-context, wiki/signals, static-index, and explicit workflow-guidance layer.

Pilot controls:

- Do not read, write, refresh, or infer facts into `~/.atomic/profile.md`.
- Do not use Atomic or Claude auto-memory, retrospective/self-sharpening learning, inter-session bus, autopilot, reminders, persistent REPLs, or automatic capture.
- Do not install or register hooks, MCP servers, PostgreSQL connections, AI Memory integrations, or governed-memory writes.
- Treat `docs/wiki/`, signals, and `.claude/.atomic-index/` as repository-local context only.
- Durable cross-session memory remains governed by the separate AI Memory system; Atomic context must not be promoted into it automatically.
- Use Atomic commands only when explicitly invoked for repository inspection, context refresh, static indexing, planning, implementation guidance, or verification.
