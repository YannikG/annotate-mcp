# Design

Annotate keeps plans as immutable Markdown revisions and records a review against exactly one revision. `docs/vocabulary.md` defines the words used here.

## Module map

Annotate.Markdown is the shared project (`build/shared-projects.txt`). Annotate.Plans and Annotate.Reviews are features. Annotate.Web is the composition root: the only project that wires features together. Features may reference a shared project. Features do not reference each other.

| Project | References |
| --- | --- |
| Annotate.Markdown | none |
| Annotate.Plans | Annotate.Markdown |
| Annotate.Reviews | Annotate.Markdown |
| Annotate.Web | Annotate.Markdown, Annotate.Plans, Annotate.Reviews |
| Annotate.Markdown.Tests | Annotate.Markdown |
| Annotate.Plans.Tests | Annotate.Plans |
| Annotate.Reviews.Tests | Annotate.Reviews |
| Annotate.Web.Tests | Annotate.Web |
| Annotate.Architecture.Tests | Annotate.Markdown, Annotate.Plans, Annotate.Reviews, Annotate.Web |

Inside a feature the code sits in three layers. Domain has no Entity Framework and no I/O. Application holds the public interface and records. Infrastructure is internal: the DbContext, mappings, and migrations. There is no repository interface. Tests call the public interface and use SQLite in memory.

## Source layout

Annotate.Markdown uses one namespace, `Annotate.Markdown`. Folders are for maintainers. The namespace does not follow the folders.

`Objects/` holds the public records. `Readers/` holds types that walk a span of source and return structure. `Scanners/` holds types that recognise one block kind. `PlanMarkdown` stays at the project root.

A later block scanner goes in `Scanners/`. A later reader goes in `Readers/`. A later public record goes in `Objects/`.

## Host

`dotnet run` binds to 127.0.0.1:24173, loopback only. MCP is at `/mcp`.

The container process listens on 0.0.0.0:24173. Compose publishes 127.0.0.1:24173, and that connection arrives on the container network interface.

One SQLite file, `annotate.db`, lives in `Annotate:DataDir` when that setting is set, otherwise `$XDG_CONFIG_HOME/annotate`, otherwise `~/.config/annotate`. The file uses WAL. Migrations run at startup. Each feature owns its DbContext and its migrations history table. There are no cross-feature foreign keys. Reviews stores `revision_id` as a plain value.

## Tables

`projects(id, folder_path unique among projects that are not archived, display_name, created_at, archived_at nullable)`

Submitting a plan for a folder whose project is archived does not create a plan. The agent is told the project got archived and must ask the user what to do.

An archived plan leaves the active lists: pending, changes requested, recently approved, the dashboard pending list, and the project's active plan count. It stays on its page and appears on the project's Archived tab. Restoring it puts it back. Submitting a further revision of an archived plan inserts nothing. The agent is told the plan is archived, that archive is not a new variant, and that it must ask the user whether to restore the plan or start a new one. A submit with no previous revision still creates a new plan in an active project.

`plans(id, project_id, title, session_id, created_at, updated_at, archived_at nullable)`

`revisions(id, plan_id, number, parent_revision_id, markdown, summary, story_url, acceptance_criteria, agent nullable, model nullable, client_name nullable, client_version nullable, created_at)` — immutable. `agent` and `model` are required on `annotate_plan`. A submit that omits either is refused and inserts nothing. A later revision does not inherit them from its parent. `client_name` and `client_version` come from the MCP client and may be null. Each value is at most 100 characters.

`blocks(revision_id, ordinal, block_key, kind, section_path, source_start, source_end, content_hash)`

`reviews(id, revision_id unique, status pending|approved|changes_requested, decided_at, feedback, created_at)`

`annotations(review_id, ordinal, kind, block_ordinal, start_offset, end_offset, text, replacement, comment, created_at)`

`decision_answers(review_id, fence_id, answer, is_other)`

`preferences.json` in the same directory as `annotate.db`, storing `autoCloseOnSubmit`.

A block key is reused when the content hash and section path are the same. Otherwise, the same section path and the same position within that section reuse the key and mark the block changed. Otherwise the block gets a new key. Revision 1 is all new keys.

## Public surfaces

`PlanMarkdown.Parse` and `PlanMarkdown.Guide`.

`IPlans`: `SubmitAsync`, `ProjectsAsync`, `ArchivedProjectsAsync`, `ProjectAsync`, `RenameProjectAsync`, `ArchiveProjectAsync`, `RestoreProjectAsync`, `DeleteProjectAsync`, `PlansAsync`, `ArchivedPlansAsync`, `ArchivePlanAsync`, `RestorePlanAsync`, `DeletePlanAsync`, `PlanAsync`, `RevisionAsync`, `RevisionActivityAsync`, `DiffAsync`.

`IReviews`: `OpenAsync`, `FindAsync`, `ForRevisionAsync`, `PendingAsync`, `ListAsync`, `WaitAsync`, `ApproveAsync`, `RequestChangesAsync`, `SaveAnnotationsAsync`, `SaveAnswerAsync`, `RemoveRevisionsAsync`.

## Routes

`/`, `/projects`, `/projects/{id}`, `/plans/{id}`, `/revisions/{id}`, `/review/{id}`.

The home page is a dashboard. Its headline metrics and 12-week outcome graph count active projects only. Headline metrics are approval rate, first-pass approval, median time to decision over the last 30 days, median annotations on change requests, and plans plus revisions created in the last 30 days. A full-width list above those metrics shows every plan in an active project whose newest revision is still pending and whose plan is not archived, oldest first, and each title opens that review. The project table lists every project, archived last. Agent and model tables, and the list of plans whose revisions changed agent or model, also count active projects only. Revisions written before attribution existed are grouped as Unknown. Project pages keep status tabs scoped to their own plans. Each tab asks SQLite for one page of 20 plans and for the total counts. The page size is fixed. Pending, Changes requested, and Recently approved list a plan once, by the newest revision's review. An older review stays on its revision and review pages. Archived plans are omitted from those tabs and listed on their own Archived tab. The plan page header archives, restores, and deletes that plan; those actions are not repeated on the list. `archive_plan` is the MCP tool for the same archive. It tells the agent that the tool only archives a finished plan and is not how a new variant is submitted. A new variant remains `annotate_plan` with `previousReviewId`. The approved wait result repeats the plan id. The project directory has its own page. Project plan lists share one item component for title, revision, updated date, and status. The entire card opens the plan page, while the project link stays independently clickable. The dashboard pending title still opens the review. Titles do not change on hover. A copy icon may appear beside a heading. A plan whose newest revision has no review does not appear on the project page. The Plans heading still counts it. Empty lists and side panels share one empty-state component. Plan, revision, and review pages share a breadcrumb whose first link is Projects and whose project name links to that project.

JavaScript is selection.js, the report script, theme.js, clipboard.js, and a local Mermaid bundle. theme.js applies the saved theme from localStorage before first paint and persists the toggle; it is an external file because the content security policy forbids inline scripts. A mermaid fence renders as a diagram. The fence source stays in the plan so it can still be annotated.

Ordinary code fences show their body with a language label, without fence markers. Indentation from the opening fence is removed while each displayed line keeps its original source offsets. Downloaded HTML reports use the same body and language.

Diff and patch fences render as a unified diff with old and new line numbers, markers, and colored rows. Each line's text keeps its Markdown source offsets for annotations. The plan, review, and revision pages use the same block renderer. Wide tables scroll horizontally within the plan instead of squeezing their columns or widening the page.

Decision fences appear in the plan as question cards with their options. Pending reviews provide an Answer button on the card that opens the existing decision dialog; saved answers also appear on the card. Plan and revision pages show the question without editing controls. Questions and option text keep their source offsets for annotations.

The shared navigation stays visible on every route, including while a review loads. It contains navigation links and the theme switch and stays on one row on small screens. Document titles, breadcrumbs, and review status sit above the editor controls rather than competing with commands.

Plan, revision, and review pages share one sticky editor toolbar beneath the document heading. Its neutral surface, compact rounded corners, and soft shadow separate commands from the page. It stays directly beneath the shared navigation while the document scrolls. Navigation and revision selection occupy the left group; panel controls and review submission occupy the right. Annotation commands live in exactly one place: the selection popup that stays beside the quote. The keyboard shortcuts d, r, s, and c trigger the same popup commands. Submission actions remain distinct from annotation commands.

Below 1100px, Contents, Decisions, and Annotations buttons open side-panel flyouts that start closed. Contents opens from the left; the other panels open from the right. Close, Escape, and clicking outside dismiss the flyout. Selecting a contents link or a decision closes its panel. Review submission controls move into an Actions disclosure, while annotation commands remain available in the selection popup. On narrow plan pages the revision selector occupies a second row within the same toolbar. At 1100px and above the panel buttons disappear and the page uses three columns: Contents, the document, and Annotations or the review panels. Side panels stay beneath the sticky toolbar. The layout fills the available page width, with side columns that grow with the viewport to give their contents room.

Editor controls use labelled groups, native buttons, a custom revision dropdown, visible keyboard focus, and normal Tab navigation. Panel buttons expose their open state with `aria-expanded`. The groups do not use the ARIA toolbar role: the [WAI-ARIA toolbar pattern](https://www.w3.org/WAI/ARIA/apg/patterns/toolbar/) requires its own arrow-key navigation, whereas these controls retain native keyboard behavior. Responsive menus keep the same commands and callbacks as their desktop counterparts.

A plan with a pending review shows a Continue review action in its header that leads to the review page, so a pending plan is never a dead end. A plan whose selected revision is approved shows a Download report action in the toolbar that saves the standalone HTML report; a download failure surfaces as an alert. Plan, revision, and review pages show the story link and acceptance criteria above the document. Their right panel lists saved annotations using the same cards as reviews. Side panels use distinct neutral card surfaces, rounded corners, and soft shadows. Headers and empty states have no inset background; annotation cards separate the kind badge, quote, and suggested text. Revision block changes sit below the document in a panel that starts collapsed.

Review annotations also appear in the plan text: deletion and replacement strike out the original, replacement and insertion show the suggested text, and comments highlight their quote. While a note dialog is open, its quote stays marked in the text as the annotation it will become, so the selection is never lost; the mark appears only in the document, not in the annotations panel, until the note is saved. Drafts update immediately and are saved in SQLite as annotations are added, typed, or undone. They survive navigation and reloads while the review stays pending, without feedback or a decision notification. Requesting changes submits the saved annotations once; approval discards draft annotations. Undo removes their marks and saved draft records. A save failure retains the local edit and offers a retry. Saved annotations appear on the review, plan, and revision pages. Proposed text has no source offsets and is excluded from selections; the original text keeps its Markdown offsets.

Downloaded HTML reports embed their own CSS for typography, context, code, tables, responsive layout, and printing. They do not depend on the running app or an external stylesheet.

Later direction, not built: agent tools that replace a block or section by key, and that restore a block from an older revision. The stored shape allows that without a schema change.

## Visual tokens

`docs/ui.md` is the design language: principles, tokens, components, and the rules in `sg-rules/`. Every visual decision lives as a token in `wwwroot/css/tokens.css`, with a light value on `:root` and a dark value on `.dark`. Stylesheets reference tokens; they never carry colour literals, raw z-index values, or `!important`.

Font: system-ui, "Segoe UI", sans-serif. Paper `#f6f8fa` / `#0d1117`. Ink `#1f2328` / `#f0f6fc`. Accent `#0969da` / `#1f6feb`. The navigation band is dark neutral in both themes. Approve `#1a7f37` / `#238636` with white text.

The mark is the word Annotate in text. There is no logo image.
