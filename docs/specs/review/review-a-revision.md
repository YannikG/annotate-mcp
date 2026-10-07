---
status: accepted
sources:
  - https://github.com/YannikG/annotate-mcp/issues/10
---

# Spec: Review a revision

## Must

- Create exactly one review per revision.
- Keep statuses pending, approved, and changes_requested.
- Let the reviewer continue a pending review from the plan page.
- On request changes, return feedback to the waiting agent.

## Must not

- Attach a second review to the same revision.
- Let the agent treat a pending review or changes requested (`plan_status=rejected`) as approval to implement.

## Flow

Agent submits revision → review is pending → person decides → agent gets approved or changes requested (`plan_status=rejected`) with feedback.

## Verification

Submit a plan via MCP, open the review URL, approve once, confirm `await_plan_review` returns approved. Repeat on a second revision after changes requested and confirm a new review id.
