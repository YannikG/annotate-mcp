---
status: accepted
sources:
  - https://github.com/YannikG/annotate-mcp/issues/10
---

# Submit a plan via MCP

As an agent, I submit a Plan Revision, give the person the Review URL, and wait before implementing.

## Steps

1. Call `annotate_plan` with Markdown, `agent`, and `model` (and `previousReviewId` when revising).
2. Send the Review URL to the person.
3. Call `await_plan_review` with that Review id; poll again if still pending.
4. Implement only when approved; on rejection, revise once and submit again.

Spec: [docs/specs/mcp/submit-a-plan.md](../../specs/mcp/submit-a-plan.md)
