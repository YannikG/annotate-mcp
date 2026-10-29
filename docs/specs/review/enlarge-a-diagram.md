---
status: accepted
sources:
  - https://github.com/YannikG/annotate-mcp/issues/20
---

# Spec: Enlarge a diagram

## Must

- Show Enlarge on a rendered mermaid fence on the plan, document, revision, and review pages.
- Keep Enlarge outside the annotatable source, and hide it until the diagram has rendered.
- Open that diagram in one viewer that nearly fills the viewport, using the SVG already on the page.
- Scale that diagram to fit the viewer, the way an image fits object-fit: contain, with no zoom or pan control.
- Close with Close, Escape, or the backdrop, and put the diagram back in its column.
- Follow the light and dark theme, including a change while the viewer is open.

## Must not

- Render a second SVG, or parse the fence again, to enlarge it.
- Make Enlarge part of the fence source.
- Change the downloaded HTML report, which still shows the fence as source.
- Widen the form dialog used for decisions, confirmation, and notes.

## Flow

Rendered diagram → Enlarge → close → diagram is inline again.

## Verification

Open a plan whose mermaid fence is wider than the column. Enlarge it, confirm it fills the viewer, switch theme, close with Escape, and confirm the diagram is back in the column. Confirm a decision dialog stays the narrow form dialog, and a downloaded report still shows the fence as source.
