# MCP tools

You mostly see the results: a new revision, a review URL in chat, or feedback after you request changes.

| Tool | What you notice |
| --- | --- |
| `annotate_plan` | A new plan or revision shows up, and the agent should paste you a review link. |
| `await_plan_review` | The agent is blocked on your decision for that review. |
| `archive_plan` | A finished plan leaves the active lists, same idea as archive in the UI. |
| `get_plan_markdown_guide` | The agent calls it before a plan with diagrams, tables, or decision fences. |
| `list_revision_blocks` / `read_revision_block` | Lets the agent inspect stored blocks on a revision by block key. |
| `annotate_block` | A reviewing agent comments on one whole block of a pending review. The note starts not accepted. |

Wire the agent from the [Agents](../installation/README.md#agents) table on the installation guide. Paste-in rules live on each agent page linked from there.
