// Enlarges the SVG selection.js already rendered. The canvas moves into this
// viewer and back; Mermaid is not run again. Blazor does not own the SVG, so
// the dialog shell is built here from the same classes as <Dialog>.
// Zoom and pan use a constructed stylesheet: the content security policy
// blocks style attributes. Fit waits until the stage has a real size.
(function () {
  const zoomFactor = 1.25;
  let viewer;
  let canvas;
  let home;
  let returnFocus;
  let scale = 1;
  let fitScale = 1;
  let tx = 0;
  let ty = 0;
  let fitted = true;
  let intrinsic = { width: 0, height: 0 };
  let scaleSheet;
  let sheetApplied = false;
  let drag = null;

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
            <button type="button" class="btn btn-secondary btn-sm" data-diagram-zoom="fit">Recenter</button>
            <button type="button" class="btn btn-quiet btn-sm" data-diagram-close>Close</button>
          </div>
        </div>
        <div class="diagram-stage" data-diagram-stage></div>
      </div>`;
    document.body.appendChild(viewer);
    bindStage(viewer.querySelector("[data-diagram-stage]"));
    return viewer;
  }

  function stageBox() {
    const stage = viewer?.querySelector("[data-diagram-stage]");
    if (!stage || stage.clientWidth < 2 || stage.clientHeight < 2) return null;
    return stage;
  }

  function measure(svg) {
    const box = svg.viewBox && svg.viewBox.baseVal;
    if (box && box.width > 0 && box.height > 0) return { width: box.width, height: box.height };
    const bounds = svg.getBBox();
    return { width: Math.max(bounds.width, 1), height: Math.max(bounds.height, 1) };
  }

  function apply() {
    const svg = canvas?.querySelector("svg");
    if (!svg || intrinsic.width <= 0 || intrinsic.height <= 0) return;
    const id = /^[\w-]+$/.test(svg.id) ? `#${svg.id}` : "[data-diagram-viewer] .mermaid-canvas svg";
    scaleSheet ??= new CSSStyleSheet();
    scaleSheet.replaceSync(`
      [data-diagram-viewer] .mermaid-canvas {
        width: ${intrinsic.width}px;
        height: ${intrinsic.height}px;
        transform: translate(${tx}px, ${ty}px) scale(${scale});
      }
      ${id} {
        width: ${intrinsic.width}px;
        height: ${intrinsic.height}px;
        max-width: none;
        max-height: none;
      }
    `);
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
    const stage = stageBox();
    const svg = canvas?.querySelector("svg");
    if (!stage || !svg) return false;
    intrinsic = measure(svg);
    fitScale = Math.min(stage.clientWidth / intrinsic.width, stage.clientHeight / intrinsic.height);
    scale = fitScale;
    tx = (stage.clientWidth - intrinsic.width * scale) / 2;
    ty = (stage.clientHeight - intrinsic.height * scale) / 2;
    fitted = true;
    apply();
    return true;
  }

  function zoomAt(px, py, factor) {
    if (!intrinsic.width || !stageBox()) return;
    const next = Math.min(Math.max(fitScale * 12, 1), Math.max(fitScale * 0.25, scale * factor));
    const contentX = (px - tx) / scale;
    const contentY = (py - ty) / scale;
    scale = next;
    tx = px - contentX * scale;
    ty = py - contentY * scale;
    fitted = false;
    apply();
  }

  function zoomCenter(factor) {
    const stage = stageBox();
    if (!stage) return;
    zoomAt(stage.clientWidth / 2, stage.clientHeight / 2, factor);
  }

  function bindStage(stage) {
    stage.addEventListener("pointerdown", (event) => {
      if (event.button !== 0 || !canvas) return;
      drag = { x: event.clientX, y: event.clientY, tx, ty, id: event.pointerId };
      stage.classList.add("is-panning");
      try {
        stage.setPointerCapture(event.pointerId);
      } catch {
        // A pointer id from a synthetic event cannot be captured. Drag still tracks pointermove.
      }
    });
    stage.addEventListener("pointermove", (event) => {
      if (!drag || drag.id !== event.pointerId) return;
      tx = drag.tx + event.clientX - drag.x;
      ty = drag.ty + event.clientY - drag.y;
      fitted = false;
      apply();
    });
    function endPan(event) {
      if (!drag || drag.id !== event.pointerId) return;
      drag = null;
      stage.classList.remove("is-panning");
    }
    stage.addEventListener("pointerup", endPan);
    stage.addEventListener("pointercancel", endPan);
    stage.addEventListener("wheel", (event) => {
      if (!canvas) return;
      event.preventDefault();
      const rect = stage.getBoundingClientRect();
      zoomAt(event.clientX - rect.left, event.clientY - rect.top, event.deltaY < 0 ? zoomFactor : 1 / zoomFactor);
    }, { passive: false });
  }

  function close(restoreFocus) {
    clearScale();
    drag = null;
    viewer?.querySelector("[data-diagram-stage]")?.classList.remove("is-panning");
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
    ensureViewer().querySelector("[data-diagram-stage]").appendChild(canvas);
    viewer.hidden = false;
    viewer.querySelector("[data-diagram-dialog]").focus();
    requestAnimationFrame(() => requestAnimationFrame(() => fit()));
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
      else zoomCenter(action === "in" ? zoomFactor : 1 / zoomFactor);
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
      zoomCenter(zoomFactor);
    } else if (event.key === "-" || event.key === "_") {
      event.preventDefault();
      zoomCenter(1 / zoomFactor);
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
    if (fitted) fit();
    else apply();
  });

  window.addEventListener("resize", () => {
    if (viewer && !viewer.hidden && fitted) fit();
  });
})();
