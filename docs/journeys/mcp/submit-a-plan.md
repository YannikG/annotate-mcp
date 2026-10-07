---
status: accepted
sources:
  - https://github.com/YannikG/annotate-mcp/issues/10
---

# Submit a plan via MCP

As an agent, I submit a plan revision, give the person the review URL, and wait before implementing.

## Steps

1. Call `annotate_plan` with Markdown, `agent`, and `model` (and `previousReviewId` when revising).
2. Send the review URL to the person.
3. Call `await_plan_review` with that review id; poll again if still pending.
4. Implement only when approved; when changes are requested (`plan_status=rejected`), revise once and submit again.

Spec: [docs/specs/mcp/submit-a-plan.md](../../specs/mcp/submit-a-plan.md)
