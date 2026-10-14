---
status: accepted
sources:
  - https://github.com/YannikG/annotate-mcp/issues/14
---

# Submit a document via MCP

As an agent, I submit a document revision, give the person the review URL, and wait. I change the document only after they request changes. Read-only means I stop.

## Steps

1. Call `annotate_document` with Markdown, `agent`, and `model` (and `previousReviewId` when revising).
2. Send the review URL to the person.
3. Call `await_document_review` with that review id.
4. Revise when the result is `changes_requested`. Stop when the result is `read_only`.

Spec: [docs/specs/mcp/submit-a-document.md](../../specs/mcp/submit-a-document.md)
