# MCP tools

Your agent talks to Annotate through a small set of MCP tools. You mostly see the results: a new revision, a review URL in chat, or feedback after you request changes. This page is what those tools mean when you are reading the product, not a rule sheet for the agent.

| Tool | What you notice |
| --- | --- |
| `annotate_plan` | A new plan or revision shows up, and the agent should paste you a review link. |
| `await_plan_review` | The agent is blocked on your decision for that review. |
| `archive_plan` | A finished plan leaves the active lists, same idea as archive in the UI. |
| `get_plan_markdown_guide` | Used when the agent needs the Markdown shapes the review page understands (tables, diagrams, decision fences). |
| `list_revision_blocks` / `read_revision_block` | Lets the agent inspect stored blocks on a revision by block key. |

Connect and wire the agent from the [installation guide](../installation/README.md). The paste-in rules that make the agent call these tools live on each agent page there.
