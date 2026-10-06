# Codex

The server is already listening. [Installation](../README.md) covers `docker run`. The URL is `http://127.0.0.1:24173/mcp`.

## Connect

```bash
codex mcp add annotate --url http://127.0.0.1:24173/mcp
```

That writes `~/.codex/config.toml`:

```toml
[mcp_servers.annotate]
url = "http://127.0.0.1:24173/mcp"
```

## Enforce

Paste this into `~/.codex/AGENTS.md`. A project `AGENTS.md` covers one repo. `AGENTS.override.md` replaces `AGENTS.md` in the same directory when it is present.

```markdown
Before multi-step work, call `annotate_plan`. Send the Review URL to the user before waiting. Do not implement while `plan_status` is pending or rejected. Revise a rejected plan and submit it again with `previousReviewId`.
```
