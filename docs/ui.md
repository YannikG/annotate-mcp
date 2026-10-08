# UI

The design language for Annotate. Read this before touching `Components/` or `wwwroot/css/`. The rules in `sg-rules/` and `build/source-limits.sh` enforce the hard rules; the rest is convention.

## Principles

**One action, one place.** Each action exists in exactly one control. Annotating happens only in the selection popup. Review submission happens only in the review actions (desktop panel or Actions flyout). If an action seems missing somewhere, that is the design, not a gap.

**Tokens, not literals.** Colours, shadows, z-index, spacing, radii, and type sizes come from `tokens.css`. A stylesheet that needs a value the tokens do not have gets a new token, not a literal.

**Components, not markup.** Buttons, badges, dialogs, context blocks, and empty states come from `Components/Ui/`. A view never writes `<button class=...>` or a hand-rolled overlay.

**`data-*` is the contract.** `data-*` attributes are the test and JavaScript hooks. Never rename or remove one without updating its consumers. Styling hooks are classes; behaviour hooks are `data-*`.

**Light and dark are one design.** Every token has a light value on `:root` and a dark value on `.dark`. A feature is not done until both themes render it correctly. The choice persists: `theme.js` applies the saved theme to `<html>` before first paint and stores the toggle in localStorage. Never introduce an inline script for this — the CSP forbids them.

## Hard rules (enforced by sg-rules and build/source-limits.sh)

- Colour literals (`#hex`, `rgb()`, `hsl()`) exist only in `tokens.css`.
- `z-index` outside `tokens.css` is always `var(--z-*)`.
- `!important` is forbidden everywhere.
- Bare element selectors (`button`, `input`, `a`, `textarea`, `kbd`) exist only in `base.css`. Everything else styles classes or `data-*` attributes. There is no native `<select>` anywhere; dropdowns are `<Select>`.
- Every `.css` file stays at or under 500 physical lines.

## Stylesheet map

Stylesheets load in this order from `App.razor`. Each has one job.

| File | Job |
| --- | --- |
| `tokens.css` | Every design decision: colours (light + dark), shadows, z-index, type, space, radius |
| `base.css` | Reset and the only bare-element styles; `.visually-hidden`, `.mono`, the Blazor error boundary |
| `ui.css` | The component library: buttons, badges, tabs, stacks, empty states, notices, alerts, context blocks, breadcrumbs, the select dropdown |
| `cards.css` | Plan and project cards |
| `dashboard.css` | Dashboard metrics, the weekly chart, and the summary tables |
| `dialogs.css` | Dialog shell, backdrops, choices, split buttons |
| `layout.css` | Shell, navigation, footer, page frames, toolbars, grids, responsive breakpoints |
| `panels.css` | Side panels, contents nav, flyouts, the 1100px desktop rules |
| `document.css` | Rendered plan prose: headings, code blocks, mermaid, diff blocks, decision cards |
| `review.css` | Annotation marks in text, annotation cards, the selection popup, the diff list |
| `copy.css` | Desktop hover controls that copy a plan id, a review id, or a heading's review id and block key, and the toast |

Scoped `.razor.css` files remain for Blazor infrastructure only (`MainLayout`, `ReconnectModal`). They follow the same rules.

## Tokens

**Surfaces.** `--paper` (page), `--surface` (raised), `--surface-sunken` (recessed), `--sheet` (cards and dialogs), `--panel-surface` / `--panel-line` / `--panel-inset` (side panels). Ink is `--ink`; secondary text is `--muted`; borders are `--line`.

**Accent and status.** `--accent`, `--accent-ink`, `--accent-soft`, `--on-accent`. Status triples follow the same shape: `--danger*`, `--success*`. Approval is its own pair: `--approve`, `--on-approve`. Focus rings use `--focus`.

**Navigation band.** The top navigation is always accent-coloured, in both themes, and uses its own `--nav-*` token set so contrast never depends on the page theme.

**Annotation marks.** `--mark-*` for comment highlights, deletions, and insertions in the plan text.

**Diff editor.** `--editor-*` for rendered diff fences.

**Overlays.** `--backdrop` for modal dialogs, `--panel-backdrop` for flyout click-catchers.

**Shadows.** `--shadow-sm`, `--shadow-md`, `--shadow-panel`, `--shadow-flyout`, `--shadow-lg`. Pick the smallest that separates the element from what is beneath it. Flyout shadows are directional so they fall across the page: left-anchored flyouts use the mirrored `--shadow-flyout-left`.

**Type.** `--font-sans`, `--font-mono`. Sizes `--text-xs` through `--text-2xl`. Weights `--weight-medium`, `--weight-semibold`, `--weight-bold`. Body text is `--text-base`; secondary text is `--text-sm`; labels and badges are `--text-xs`.

**Space.** `--space-1` (0.25rem) through `--space-7` (3rem). No other spacing values.

**Radius.** `--radius-xs` (4px) for small inline elements, `--radius-sm` (6px), `--radius-md` (8px) for buttons and inputs, `--radius-lg` (12px) for cards and dialogs, `--radius-pill` for badges.

**Layers.** `--z-raised` 1, `--z-sticky` 10 (toolbar), `--z-backdrop` 20, `--z-flyout` 30, `--z-modal` 40, `--z-popover` 50 (selection popup), `--z-system` 100 (reconnect UI). Nothing escapes this scale.

## Components

All live in `src/Annotate.Web/Components/Ui/` and are in scope via `_Imports.razor`.

**`<Button>`** — the only button. `Variant` is `Secondary` (default), `Primary`, `Quiet`, or `Destructive`. `Type` defaults to `button`; pass `Type="submit"` inside forms. Extra attributes (including `data-*` and `class`) splat onto the element. Icon-only buttons add `class="btn-icon"`. Buttons inside an annotation card add `class="btn-sm"`.

**`<Badge>`** — a status pill. `Tone` becomes `data-state`: `pending`, `approved`, `changes`, or `neutral`. Styling keys off `data-state`, so the badge never carries a per-view class.

**`<Select>`** — the only dropdown. A labelled trigger button shows the current value and a chevron; the custom listbox (`role="listbox"`, options as `role="option"`) drops below it, so the open state follows the design system too. Arrows move the selection while open, Enter/Space toggles, Escape or the backdrop closes. Never use a native `<select>` — its option popup cannot be styled. Extra attributes (including `data-*`) splat onto the wrapper.

**`<Dialog>`** — the only modal shell. Renders the overlay, backdrop button, and `role="dialog"` panel with `aria-modal` and `aria-labelledby`. Escape and backdrop click raise `Close`. `LabelledBy` must point at the id of the dialog's heading.

**`<ConfirmDialog>`** — a `Dialog` with title, body, optional `Error` alert, and Cancel/Confirm actions. `ConfirmVariant` defaults to `Primary`; use `Destructive` for irreversible actions. Confirm and Cancel carry `data-confirm` and `data-cancel`.

**`<ContextBlock>`** — a labelled block of revision context (Story, Acceptance criteria, Written by). Extra attributes splat, so `data-criteria` stays on the criteria block.

**`<EmptyState>`** — the only empty state: a title and an optional hint. Lists and side panels share it.

**`<ToastHost>`** — the one toast, mounted in the shell. Any page calls `IToast.Show`. A newer message replaces the one on screen, and the toast leaves on its own.

**`<RevisionContext>`** (`Components/Browse/`) — the Story, acceptance criteria, and written-by section. Plan, review, and revision pages all use it; nobody re-implements it.

**`<DocumentBoard>`** (`Components/Browse/`) — the three-column document shell: contents nav on the left, `Panels` slot on the right, document as child content. Review, plan, and revision pages share it.

**`<AnnotateToolbar>`** (`Components/Review/`) — the selection popup, the only place annotation commands exist. Its four commands are one data table; the keyboard shortcuts in the footer mirror it.

**`StatusTone`** (`Components/Ui/`) — the one mapping between a review status, its display label, and its badge tone. Pages call `StatusTone.Label` / `StatusTone.Of`; views never switch on `ReviewStatus` themselves.

## Downloaded reports

Reports are standalone HTML and cannot link `tokens.css`, so `PlanReportStyles` embeds its own `:root` block mirroring the light theme and uses `var()` everywhere else. `ReportStyleTests` pins every shared token to `tokens.css` and forbids color literals outside the report's `:root` block — change a token and the test tells you to update the report.

## Where actions live

| Action | The one place |
| --- | --- |
| Continue a pending review | The plan page header (Continue review) |
| Download a plan report | The plan page toolbar, when the selected revision is approved (the review page's Approve and Download is part of submitting a review) |
| Annotate (delete, replace, insert, comment) | The selection popup beside the quote |
| Submit review (approve, request changes) | Review actions: right panel on desktop, Actions flyout below 1100px |
| Answer a decision | The decision dialog, opened from the card's Answer button |
| Project edit, archive, delete | The project page header |
| Theme switch | The shared navigation |
| Copy a plan or review id | The editor toolbar copy menu |
| Copy a heading's review id and block key | The copy icon at the start of that heading |
| Reply to an annotation | The reply field on that annotation card |
| Delete an annotation or a reply | Delete on that card, after the confirm dialog |
| Accept an agent note | The Accept pill on that agent note, which turns into Accepted |

The heading copy icon shows the tooltip “Copies the review id and this block id” before the click. Keyboard shortcuts (`d`, `r`, `s`, `c`) trigger the same popup commands; they are not separate UI.

## Layout

Breakpoints: 600px, 700px, 800px, and 1100px. The only structural one is 1100px: below it side panels become flyouts and review submission moves into the Actions disclosure; at or above it the page is three columns. The shared navigation stays on one row at every width. The editor toolbar is sticky beneath the navigation. The footer with keyboard hints shows on plan, revision, and review pages only.

## Accessibility

- Every dialog has `role="dialog"`, `aria-modal="true"`, and `aria-labelledby` pointing at its heading. Escape closes it.
- Buttons are native `<button>` elements with visible text; icon-only buttons keep an accessible name.
- Panel triggers expose `aria-expanded`; tabs use `aria-selected`.
- Alerts use `role="alert"`.
- Focus is always visible; never remove an outline without replacing it.
- Colour is never the only signal: badges and annotation marks pair colour with text or line style.

## Checklist for agents

1. Need a colour, shadow, z-index, spacing, radius, or font size? Use a token. Missing token? Add it to `tokens.css` (both themes) — never a literal elsewhere.
2. Need a button, badge, dialog, context block, or empty state? Use the component. Do not write the markup by hand.
3. Need a new action? Find its one place in the table above. If it has no place, add one place — not two.
4. Need a test or JS hook? Add a `data-*` attribute and leave existing ones alone.
5. Check both themes before handing work back.
6. `build/check.sh` must be green; it runs the style rules.
