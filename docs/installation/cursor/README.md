# Cursor

The server is already listening. [Installation](../README.md) covers `docker run`. The URL is `http://127.0.0.1:24173/mcp`.

## Connect

`~/.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "annotate": {
      "url": "http://127.0.0.1:24173/mcp"
    }
  }
}
```

## Enforce

Add a user rule in Cursor Settings with the paragraph below. For one repo, use `AGENTS.md` or `.cursor/rules/annotate-plan.mdc` with `alwaysApply: true`.

```markdown
---
description: Call annotate_plan before multi-step work
alwaysApply: true
---

Before multi-step work, call `annotate_plan`. Send the Review URL to the user before waiting. Do not implement while `plan_status` is pending or rejected. Revise a rejected plan and submit it again with `previousReviewId`.
```

The user rule is the paragraph without the front matter.
