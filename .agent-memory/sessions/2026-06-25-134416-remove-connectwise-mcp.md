# 2026-06-25 Remove ConnectWise MCP

## Objective

- Remove the local ConnectWise MCP registration from the active Codex configuration and clean up the dependent morning brief automation prompt.

## Work Performed

- Read `AGENTS.md`, repo memory files, and failure-avoidance notes.
- Searched the repository for `connect_wise` / `ConnectWise` references and confirmed there were no in-repo MCP definitions to change.
- Located the active MCP registration in `/Users/bcrowell/.codex/config.toml` as `[mcp_servers.connectwise_manage]`.
- Removed only that MCP server block.
- Updated `/Users/bcrowell/.codex/automations/weekday-morning-brief/automation.toml` to remove the hard-coded MCP endpoint and MCP-specific access wording while preserving the rest of the brief instructions.

## Validation

- Verified `/Users/bcrowell/.codex/config.toml` no longer contains:
  - `connectwise_manage`
  - the removed `mcp-remote` endpoint
- Verified `/Users/bcrowell/.codex/automations/weekday-morning-brief/automation.toml` no longer contains:
  - the removed endpoint URL
  - MCP-specific ConnectWise access wording

## Outcome

- The local Codex config no longer registers the ConnectWise MCP.
- The weekday morning brief automation no longer instructs the model to use the removed MCP endpoint.
- No repository product code was changed.

## Next Recommended Step

- Review whether any archived notes or attachments in `/Users/bcrowell/.codex` still need manual cleanup, but no further config changes are required for active MCP usage.
