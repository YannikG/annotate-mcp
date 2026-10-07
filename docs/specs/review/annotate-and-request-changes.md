---
status: accepted
sources:
  - https://github.com/YannikG/annotate-mcp/issues/10
---

# Spec: Annotate and request changes

## Must

- Create Annotations only from the selection popup (or its keyboard shortcuts).
- Support deletion, replacement, insertion, and comment kinds.
- Persist Annotation drafts for a pending Review across navigation and reload.
- Submit saved drafts as Feedback when the reviewer requests changes.

## Must not

- Allow annotating Acceptance criteria or story context above the plan.
- Keep draft Annotations after approve.
- Treat proposed replacement or insertion text as a new selection anchor.

## Flow

Select quote → save Annotation draft → request changes → agent receives Feedback and revises with `previousReviewId`.

## Verification

On a pending Review, add one Annotation of each kind, reload, confirm drafts remain, request changes, and confirm the agent wait result includes the Feedback.
