# Claude Code

The server is already listening. [Installation](../README.md) covers `docker run`. The URL is `http://127.0.0.1:24173/mcp`.

## Connect

```bash
claude mcp add --scope user --transport http annotate http://127.0.0.1:24173/mcp
```

That writes `~/.claude.json`. The entry needs `"type": "http"`. An entry with `url` and no `type` is skipped.

```json
{
  "mcpServers": {
    "annotate": {
      "type": "http",
      "url": "http://127.0.0.1:24173/mcp"
    }
  }
}
```

`claude mcp list` should show it connected.

## Enforce

Paste this into `~/.claude/CLAUDE.md`. Use a project `CLAUDE.md` or `AGENTS.md` when the rule should travel with one repo.

```markdown
Before multi-step work, call `annotate_plan`. Send the Review URL to the user before waiting. Do not implement while `plan_status` is pending or rejected. Revise a rejected plan and submit it again with `previousReviewId`.
```
