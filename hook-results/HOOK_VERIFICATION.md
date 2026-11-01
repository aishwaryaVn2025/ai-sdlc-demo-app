# Copilot Hook Verification

Date: 2026-11-01
Runtime: VS Code Local Agent

## Configuration

- .github/hooks/audit.json
- .github/hooks/log-tool-use.ps1
- .github/hooks/log-tool-use.cjs

## Automatic Execution Evidence

SessionStart:
2026-11-01T14:55:06.928Z

PreToolUse:
2026-11-01T14:55:13.017Z

Tool:
run_in_terminal

## Verification

The Copilot Local Agent automatically triggered both
SessionStart and PreToolUse.

The PowerShell wrapper invoked the Node.js logger,
which wrote structured JSON entries to tool-use.log.

## Limitations

The logger currently records timestamps, event types,
and tool names. It does not capture complete AI prompts
or responses.
