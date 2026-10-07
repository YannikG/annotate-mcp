---
status: accepted
sources:
  - https://github.com/YannikG/annotate-mcp/issues/10
---

# Spec: Submit a plan via MCP

## Must

- Accept `annotate_plan` and return pending plus a review URL without waiting for the decision.
- Require `agent` and `model` on every submit; refuse and insert nothing when either is missing.
- Accept `previousReviewId` to attach the next revision after changes were requested.
- Return a stored decision from `await_plan_review` after restart when the review is already decided.

## Must not

- Treat `archive_plan` as a way to submit a new revision or variant.
- Instruct the agent to implement while status is pending or changes requested (`plan_status=rejected`).

## Flow

`annotate_plan` → person gets URL → `await_plan_review` → approved or changes requested (`plan_status=rejected`) with feedback → optional resubmit with `previousReviewId`.

## Verification

Call `annotate_plan` without `model` and confirm refusal. Call with both fields, open the URL, decide, and confirm `await_plan_review` matches. After changes requested, resubmit with `previousReviewId` and confirm revision number increments.
