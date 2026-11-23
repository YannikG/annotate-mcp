---
status: accepted
sources:
  - https://github.com/YannikG/annotate-mcp/issues/20
  - https://github.com/YannikG/annotate-mcp/issues/22
---

# Spec: Enlarge a diagram

## Must

- Show Enlarge on a rendered mermaid fence on the plan, document, revision, and review pages.
- Keep Enlarge outside the annotatable source, and hide it until the diagram has rendered.
- Open that diagram in one viewer that nearly fills the viewport, using the SVG already on the page.
- Open fitted and centered. Zoom in and zoom out, from the buttons, from `+` and `−`, and from the wheel. Drag to pan. Recenter, and `0`, fit and center it again.
- Close with Close, Escape, or the backdrop, and put the diagram back in its column.
- Follow the light and dark theme, including a change while the viewer is open.
- Draw each mermaid fence in a downloaded plan or document report, and show Enlarge on that SVG.
- Store the diagram as the SVG drawn at download, in the light theme. Enlarge in that file uses the same viewer: fitted and centered, zoom, wheel, drag, Recenter, and close. The file includes the viewer and does not include the Mermaid bundle. It still works with Annotate stopped. A report with no mermaid fence stays free of that script.

## Must not

- Render a second SVG, or parse the fence again, to enlarge it.
- Make Enlarge part of the fence source.
- Widen the form dialog used for decisions, confirmation, and notes.

## Flow

Rendered diagram → Enlarge → zoom or pan → Recenter or close → diagram is inline again.

## Verification

Open a plan whose mermaid fence is wider than the column. Enlarge it, zoom in, drag sideways, Recenter, switch theme, close with Escape, and confirm the diagram is back in the column. Confirm a decision dialog stays the narrow form dialog. Download a report, open the file with Annotate stopped, and confirm the diagram is drawn and Enlarge zooms and recenters it. A report with no mermaid fence has no diagram script.
