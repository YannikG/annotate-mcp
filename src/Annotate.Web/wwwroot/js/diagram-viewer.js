// Enlarges the SVG selection.js already rendered. The canvas moves into this
// viewer and back; Mermaid is not run again. Blazor does not own the SVG, so
// the dialog shell is built here from the same classes as <Dialog>. Sizing is
// plain CSS: the SVG's viewBox lets width/height: 100% fit it to the stage
// like an image with object-fit: contain, so no zoom or pan logic is needed.
(function () {
  let viewer;
  let canvas;
  let home;
  let returnFocus;

  function canvasOf(block) {
    const id = block.querySelector("[data-block-index]")?.getAttribute("data-block-index") ?? "";
    if (/^\d+$/.test(id)) {
      const moved = document.querySelector(`[data-mermaid-canvas][data-mermaid-home="${id}"]`);
      if (moved) return moved;
    }
    return block.querySelector("[data-mermaid-canvas]");
  }

  function ensureViewer() {
    if (viewer) return viewer;
    viewer = document.createElement("div");
    viewer.setAttribute("data-diagram-viewer", "");
    viewer.hidden = true;
    viewer.innerHTML = `
      <button type="button" class="dialog-backdrop" data-diagram-close aria-label="Close"></button>
      <div class="dialog dialog-diagram" data-diagram-dialog role="dialog" aria-modal="true" aria-labelledby="diagram-viewer-title" tabindex="-1">
        <div class="diagram-viewer-bar">
          <h2 id="diagram-viewer-title">Mermaid diagram</h2>
          <button type="button" class="btn btn-quiet btn-sm" data-diagram-close>Close</button>
        </div>
        <div class="diagram-stage" data-diagram-stage></div>
      </div>`;
    document.body.appendChild(viewer);
    return viewer;
  }

  function close(restoreFocus) {
    if (canvas) {
      canvas.removeAttribute("data-mermaid-home");
      if (home?.parent?.isConnected && !home.parent.querySelector("[data-mermaid-canvas]")) {
        home.parent.insertBefore(canvas, home.next);
      } else {
        canvas.remove();
      }
    }
    canvas = null;
    home = null;
    if (viewer) viewer.hidden = true;
    const focus = returnFocus;
    returnFocus = null;
    if (restoreFocus && focus?.isConnected) focus.focus();
  }

  function open(block) {
    const next = canvasOf(block);
    if (!next?.querySelector("svg")) return;
    if (canvas === next && viewer && !viewer.hidden) return;
    close(false);
    canvas = next;
    const id = block.querySelector("[data-block-index]")?.getAttribute("data-block-index") ?? "";
    if (/^\d+$/.test(id)) canvas.setAttribute("data-mermaid-home", id);
    home = { parent: canvas.parentNode, next: canvas.nextSibling };
    returnFocus = block.querySelector("[data-mermaid-enlarge]");
    ensureViewer().querySelector("[data-diagram-stage]").appendChild(canvas);
    viewer.hidden = false;
    viewer.querySelector("[data-diagram-dialog]").focus();
  }

  document.addEventListener("click", (event) => {
    const target = event.target instanceof Element ? event.target : null;
    if (!target) return;
    const enlarge = target.closest("[data-mermaid-enlarge]");
    if (enlarge) {
      const block = enlarge.closest("[data-mermaid]");
      if (block) open(block);
      return;
    }
    if (target.closest("[data-diagram-close]")) close(true);
  });

  document.addEventListener("keydown", (event) => {
    if (!viewer || viewer.hidden || event.key !== "Escape") return;
    event.preventDefault();
    event.stopPropagation();
    close(true);
  }, true);

  // A theme change re-renders the diagram in place. If that ever fails while
  // enlarged, leave the viewer rather than show an empty stage.
  document.addEventListener("annotate-diagram-rendered", (event) => {
    if (canvas && event.target === canvas && viewer && !viewer.hidden && !canvas.querySelector("svg")) {
      close(true);
    }
  });
})();
