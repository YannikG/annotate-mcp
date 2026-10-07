# MCP tools

| Tool | What it does |
| --- | --- |
| `annotate_plan` | Submits a plan revision. Returns `plan_status=pending` and a review URL immediately. Send that URL to the person before waiting. Requires `agent` and `model` on every call. Use `previousReviewId` for the next variant after changes were requested. |
| `await_plan_review` | Waits (or polls) for a decision. Call only after the person has the review URL. Pending is normal; call again with the same review id. |
| `archive_plan` | Archives a finished plan so it leaves the active lists. Does not submit a revision or a new variant. |
| `get_plan_markdown_guide` | Returns the Markdown the review page can render (tables, diagrams, decision fences). |
| `list_revision_blocks` | Lists blocks on one revision: block key, kind, first line. |
| `read_revision_block` | Returns the Markdown of one block from that list. |

Do not implement while the review is pending or rejected. Revise a rejected plan and submit once with `previousReviewId`.
