---
status: accepted
sources:
  - https://github.com/YannikG/annotate-mcp/issues/14
---

# Spec: Submit a document via MCP

## Must

- Accept `annotate_document` and return pending plus a review URL without waiting for the person.
- Require `agent` and `model` on every submit. Refuse and insert nothing when either is missing.
- Accept `previousReviewId` to attach the next revision after changes were requested.
- Return `changes_requested` with the feedback from `await_document_review` after the person requests changes.
- Return `read_only` from `await_document_review` when the document is read-only, and refuse a further submit.
- Refuse a submit that changes or removes a locked heading, or the text under it until the next heading, and insert nothing.
- Show `locked: true` or `locked: false` from `list_revision_blocks` for a document revision. Text under a locked heading is `locked: true`.
- Refuse `annotate_block` on a locked heading, on the text under it, or on a read-only document.
- Refuse `await_plan_review` for a document review, and `await_document_review` for a plan review.
- Show the revision dropdown on the document page, in the editor toolbar, and open an older revision at `/documents/{id}/revisions/{revisionId}`.
- Show the selected revision's review status beside the document title, including whether changes were requested.

## Must not

- Return `approved` for a document.
- Treat `archive_document` as a way to submit a new revision.
- Show an Approve control or an Auto-close control on a document review.
- Show a lock control on the document page. Lock and unlock belong to the document review.

## Flow

`annotate_document` → person gets URL → annotate, then request changes, or set read-only → `await_document_review` → `changes_requested` or `read_only` → optional resubmit with `previousReviewId` when changes were requested.

## Verification

Call `annotate_document`, open the review URL, and confirm Approve is absent. Lock a block, confirm a changed revision is refused and `annotate_block` writes nothing. Set read-only and confirm `await_document_review` returns `read_only`.
