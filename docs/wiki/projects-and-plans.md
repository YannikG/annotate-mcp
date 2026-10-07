# Projects and plans

A project groups plans by a normalised folder path (usually the agent's working directory). You can rename the display name; the path stays fixed.

A plan is a titled sequence of revisions. Each revision is an immutable Markdown snapshot. Archive a plan when the work is done so it leaves the active lists; restore it if you need it back. Delete removes the plan and its reviews. Submitting a further revision on an archived plan does nothing useful for the agent: it is told the plan is archived and must ask whether to restore or start a new one.

`archive_plan` is the MCP tool for the same archive action. A new variant is still `annotate_plan` with `previousReviewId`, not archive.
