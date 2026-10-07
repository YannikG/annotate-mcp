---
status: accepted
sources:
  - https://github.com/YannikG/annotate-mcp/issues/10
---

# Spec: Annotate and request changes

## Must

- Create annotations only from the selection popup (or its keyboard shortcuts).
- Support deletion, replacement, insertion, and comment kinds.
- Persist annotation drafts for a pending review across navigation and reload.
- Submit saved drafts as feedback when the reviewer requests changes.
- Discard draft annotations when the reviewer approves.

## Must not

- Allow annotating acceptance criteria or story context above the plan.
- Keep draft annotations after approve.
- Treat proposed replacement or insertion text as a new selection anchor.

## Flow

Select quote → save annotation draft → request changes → agent receives feedback and revises with `previousReviewId`.

## Verification

On a pending review, add one annotation of each kind, reload, confirm drafts remain, request changes, and confirm the agent wait result includes the feedback.
