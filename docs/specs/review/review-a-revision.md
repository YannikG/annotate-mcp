---
status: accepted
sources:
  - https://github.com/YannikG/annotate-mcp/issues/10
---

# Spec: Review a revision

## Must

- Create exactly one Review per Revision.
- Keep statuses pending, approved, and changes_requested.
- Let the reviewer continue a pending Review from the Plan page.
- On approve, discard Annotation drafts for that Review.
- On request changes, return Feedback to the waiting agent.

## Must not

- Attach a second Review to the same Revision.
- Let the agent treat a pending or rejected Review as approval to implement.

## Flow

Agent submits Revision → Review is pending → person decides → agent gets approved or rejected with Feedback.

## Verification

Submit a Plan via MCP, open the Review URL, approve once, confirm `await_plan_review` returns approved. Repeat on a second Revision after changes requested and confirm a new Review id.
