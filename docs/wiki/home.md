# Annotate

A short plan is enough when the change is small. A bigger feature is a different job. The agent writes a plan, you send it back, it revises, you switch to another task, and the only record is the chat. Annotate is an MCP server and a GUI for that loop. The agent submits the plan as Markdown. You read one revision, see which blocks changed, mark what you want different, and the review goes back to the agent. Older revisions stay.

The idea is close to [opencode-annotate](https://github.com/frontendxlab/opencode-annotate): a proper place to look at what an agent produced and send feedback. Annotate speaks MCP, so the client can be Claude Code, Codex, Cursor, OpenCode, or another agent that can call an MCP server. The model provider does not matter.

Use as much of it as you want. Open it when you want a better place to read a plan than the transcript. Stay with it when a feature needs many rounds and you want the revisions and the feedback in one project.

## Use the product

- [How a review works](how-a-review-works.md)
- [Annotating a plan](annotating-a-plan.md)
- [Projects and plans](projects-and-plans.md)
- [Dashboard](dashboard.md)
- [Decision fences](decision-fences.md)
- [MCP tools](mcp-tools.md)

## Start

[Install the server](../installation/README.md). Then connect the agent you use.

- [Claude Code](../installation/claude-code/README.md)
- [Codex](../installation/codex/README.md)
- [Cursor](../installation/cursor/README.md)
- [OpenCode](../installation/opencode/README.md)

The published image does not exist until the first `v*` tag. Until then, [manual deployment](../deployment/README.md) is how you run this checkout.

People changing Annotate start at the [development guide](../development/README.md). Issues and pull requests are in the [contribution guide](../contributing/README.md).
