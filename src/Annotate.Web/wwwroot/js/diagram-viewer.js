// Enlarges the SVG selection.js already rendered. The canvas moves into this
// viewer and back; Mermaid is not run again. Blazor does not own the SVG, so
// the dialog shell is built here from the same classes as <Dialog>.
(function () {
  const zoomFactor = 1.25;
  let viewer;
  let canvas;
  let home;
  let returnFocus;
  let scale = 1;
  let fitScale = 1;
  let fitted = true;
  let intrinsic = { width: 0, height: 0 };
  let scaleSheet;
  let sheetApplied = false;

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
          <div class="dialog-group">
            <button type="button" class="btn btn-secondary btn-sm" data-diagram-zoom="out">Zoom out</button>
            <button type="button" class="btn btn-secondary btn-sm" data-diagram-zoom="in">Zoom in</button>
            <button type="button" class="btn btn-secondary btn-sm" data-diagram-zoom="fit">Fit</button>
            <button type="button" class="btn btn-quiet btn-sm" data-diagram-close>Close</button>
          </div>
        </div>
        <div class="diagram-stage" data-diagram-stage></div>
      </div>`;
    document.body.appendChild(viewer);
    return viewer;
  }

  function measure(svg) {
    const box = svg.viewBox && svg.viewBox.baseVal;
    if (box && box.width > 0 && box.height > 0) return { width: box.width, height: box.height };
    const width = Number.parseFloat(svg.getAttribute("width") ?? "");
    const height = Number.parseFloat(svg.getAttribute("height") ?? "");
    if (width > 0 && height > 0) return { width, height };
    const rect = svg.getBoundingClientRect();
    return { width: Math.max(rect.width, 1), height: Math.max(rect.height, 1) };
  }

  function limits() {
    return {
      min: Math.min(fitScale, 0.25),
      max: Math.max(fitScale * 8, 1)
    };
  }

  function applyScale() {
    const svg = canvas?.querySelector("svg");
    if (!svg || intrinsic.width <= 0 || intrinsic.height <= 0) return;
    const width = Math.max(1, Math.round(intrinsic.width * scale));
    const height = Math.max(1, Math.round(intrinsic.height * scale));
    const target = /^[\w-]+$/.test(svg.id) ? `#${svg.id}` : "[data-diagram-viewer] .mermaid-canvas svg";
    scaleSheet ??= new CSSStyleSheet();
    scaleSheet.replaceSync(`${target} { width: ${width}px; height: ${height}px; max-width: none; }`);
    if (!sheetApplied) {
      document.adoptedStyleSheets = [...document.adoptedStyleSheets, scaleSheet];
      sheetApplied = true;
    }
  }

  function clearScale() {
    if (!sheetApplied || !scaleSheet) return;
    document.adoptedStyleSheets = document.adoptedStyleSheets.filter((sheet) => sheet !== scaleSheet);
    sheetApplied = false;
  }

  function fit() {
    const svg = canvas?.querySelector("svg");
    const stage = viewer?.querySelector("[data-diagram-stage]");
    if (!svg || !stage) return;
    intrinsic = measure(svg);
    if (stage.clientWidth <= 0 || stage.clientHeight <= 0) return;
    fitScale = Math.min(stage.clientWidth / intrinsic.width, stage.clientHeight / intrinsic.height);
    scale = fitScale;
    fitted = true;
    applyScale();
  }

  function zoom(direction) {
    if (!canvas || !viewer || viewer.hidden) return;
    const bound = limits();
    const next = direction === "in" ? scale * zoomFactor : scale / zoomFactor;
    scale = Math.min(bound.max, Math.max(bound.min, next));
    fitted = false;
    applyScale();
  }

  function close(restoreFocus) {
    clearScale();
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
    if (canvas === next && viewer && !viewer.hidden) {
      fit();
      return;
    }
    close(false);
    canvas = next;
    const id = block.querySelector("[data-block-index]")?.getAttribute("data-block-index") ?? "";
    if (/^\d+$/.test(id)) canvas.setAttribute("data-mermaid-home", id);
    home = { parent: canvas.parentNode, next: canvas.nextSibling };
    returnFocus = block.querySelector("[data-mermaid-enlarge]");
    const stage = ensureViewer().querySelector("[data-diagram-stage]");
    stage.appendChild(canvas);
    viewer.hidden = false;
    fit();
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
    const zoomButton = target.closest("[data-diagram-zoom]");
    if (zoomButton) {
      const action = zoomButton.getAttribute("data-diagram-zoom");
      if (action === "fit") fit();
      else zoom(action === "in" ? "in" : "out");
      return;
    }
    if (target.closest("[data-diagram-close]")) close(true);
  });

  document.addEventListener("keydown", (event) => {
    if (!viewer || viewer.hidden) return;
    if (event.key === "Escape") {
      event.preventDefault();
      event.stopPropagation();
      close(true);
      return;
    }
    if (event.metaKey || event.ctrlKey || event.altKey) return;
    const typing = event.target instanceof Element
      && event.target.closest("input, textarea, select, [contenteditable='true']");
    if (typing) return;
    if (event.key === "+" || event.key === "=") {
      event.preventDefault();
      zoom("in");
    } else if (event.key === "-" || event.key === "_") {
      event.preventDefault();
      zoom("out");
    } else if (event.key === "0") {
      event.preventDefault();
      fit();
    }
  }, true);

  document.addEventListener("annotate-diagram-rendered", (event) => {
    if (!canvas || event.target !== canvas || !viewer || viewer.hidden) return;
    if (!canvas.querySelector("svg")) {
      close(true);
      return;
    }
    intrinsic = measure(canvas.querySelector("svg"));
    if (fitted) fit();
    else applyScale();
  });

  window.addEventListener("resize", () => {
    if (viewer && !viewer.hidden && fitted) fit();
  });
})();
