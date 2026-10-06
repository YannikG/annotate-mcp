# OpenCode

The server is already listening. [Installation](../README.md) covers `docker run`. The URL is `http://127.0.0.1:24173/mcp`.

## Connect

`~/.config/opencode/opencode.json`:

```json
{
  "mcp": {
    "annotate": {
      "type": "remote",
      "url": "http://127.0.0.1:24173/mcp"
    }
  }
}
```

## Enforce

Paste this into `~/.config/opencode/AGENTS.md`. A project `AGENTS.md` covers one repo. If that file is absent, OpenCode falls back to `CLAUDE.md`.

```markdown
Before multi-step work, call `annotate_plan`. Send the Review URL to the user before waiting. Do not implement while `plan_status` is pending or rejected. Revise a rejected plan and submit it again with `previousReviewId`.
```
