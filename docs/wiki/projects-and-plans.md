# Projects and plans

A project groups plans and documents by a normalised folder path (usually the working directory the agent used). You can rename the display name; the path stays fixed.

While you are browsing a project, a side navigation switches between Plans and Documents. Rename, archive, and delete stay in the project header on both of those pages. The side navigation is not shown once you open a plan, a document, a revision, or a review. The top navigation stays.

A plan is a titled sequence of revisions. Each revision is an immutable Markdown snapshot. Archive a plan when the work is done so it leaves the active lists; restore it if you need it back. Delete removes the plan and its reviews.

A document is the same kind of revision sequence, kept for longer work such as an audit. Its page has the same revision dropdown as a plan page. The title shows whether that revision already had changes requested. You annotate it and request changes. There is no approve action. Read-only freezes the document in the same view as a plan you are not reviewing. Locking a heading keeps that heading and the text under it, until the next heading. A later revision cannot change that text, and an agent cannot annotate it. Selecting that text shows a toast instead of the annotation popup. Archive removes it from the active document list.

While a plan or document is archived, new revisions from the agent do not land on it. Restore it first, or start a fresh one, if you want more rounds.
