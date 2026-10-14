# Projects and plans

A project groups plans and documents by a normalised folder path (usually the working directory the agent used). You can rename the display name; the path stays fixed.

While you are browsing a project, a side navigation switches between Plans and Documents. Rename, archive, and delete stay in the project header on both of those pages. The side navigation is not shown once you open a plan, a document, a revision, or a review. The top navigation stays.

A plan is a titled sequence of revisions. Each revision is an immutable Markdown snapshot. Archive a plan when the work is done so it leaves the active lists; restore it if you need it back. Delete removes the plan and its reviews.

A document is the same kind of revision sequence, kept for longer work such as an audit. Its page has the same revision dropdown as a plan page. The title shows whether that revision already had changes requested. You annotate it and request changes. There is no approve action. Read-only freezes the document, including a review already open in another tab. Locking a heading keeps that heading and the following text until the next heading; another heading with the same title remains a separate section. A later revision cannot change locked text, and an agent cannot annotate it. Selecting that text shows a toast instead of the annotation popup. Download report in the document toolbar saves the revision you are reading as HTML. Archive, restore, and delete ask before changing the document.

While a plan or document is archived, new revisions from the agent do not land on it. Restore it first, or start a fresh one, if you want more rounds.
