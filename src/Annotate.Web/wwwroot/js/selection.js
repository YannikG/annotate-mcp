(function () {
  function segment(node) {
    const element = node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement;
    if (!element || element.closest("[data-annotation-extra]")) {
      return null;
    }

    const holder = element.closest("[data-block-index]");
    if (!holder) {
      return null;
    }

    const block = Number(holder.getAttribute("data-block-index"));
    const start = Number(holder.getAttribute("data-seg-start"));
    const end = Number(holder.getAttribute("data-seg-end"));
    if (!Number.isInteger(block) || !Number.isInteger(start) || !Number.isInteger(end)) {
      return null;
    }

    return { holder, block, start };
  }

  function localOffset(holder, container, offset) {
    const range = holder.ownerDocument.createRange();
    range.setStart(holder, 0);
    range.setEnd(container, offset);
    return sourceText(range).length;
  }

  function sourceText(range) {
    const fragment = range.cloneContents();
    fragment.querySelectorAll("[data-annotation-extra]").forEach((element) => element.remove());
    return fragment.textContent ?? "";
  }

  function read() {
    const selection = window.getSelection();
    if (!selection || selection.rangeCount === 0 || selection.isCollapsed) {
      return null;
    }

    const range = selection.getRangeAt(0);
    const startSegment = segment(range.startContainer);
    const endSegment = segment(range.endContainer);
    if (!startSegment || !endSegment || startSegment.block !== endSegment.block) {
      return null;
    }

    const text = sourceText(range);
    if (!text) {
      return null;
    }

    const rect = range.getBoundingClientRect();
    return {
      blockOrdinal: startSegment.block,
      start: startSegment.start + localOffset(startSegment.holder, range.startContainer, range.startOffset),
      end: endSegment.start + localOffset(endSegment.holder, range.endContainer, range.endOffset),
      text: text,
      box: { top: rect.top, left: rect.left, bottom: rect.bottom, width: rect.width }
    };
  }

  function place(element) {
    const top = element.getAttribute("data-top");
    const left = element.getAttribute("data-left");
    if (top === null || left === null) {
      return;
    }

    const rect = element.getBoundingClientRect();
    element.style.top = Math.max(8, Math.min(Number(top), window.innerHeight - rect.height - 8)) + "px";
    element.style.left = Math.max(8, Math.min(Number(left), document.documentElement.clientWidth - rect.width - 8)) + "px";
  }

  const diagramSheets = new Map();
  let mermaidPromise;
  let renderNumber = 0;
  const maxDiagramLength = 50000;

  function loadMermaid() {
    if (window.mermaid) {
      return Promise.resolve(window.mermaid);
    }

    if (!mermaidPromise) {
      const src = document.querySelector("meta[name='mermaid-script']")?.content ?? "";
      mermaidPromise = new Promise((resolve, reject) => {
        if (!src) {
          reject(new Error("Could not load the diagram renderer."));
          return;
        }

        const script = document.createElement("script");
        script.src = src;
        script.onload = () => {
          if (window.mermaid) resolve(window.mermaid);
          else reject(new Error("Could not load the diagram renderer."));
        };
        script.onerror = () => reject(new Error("Could not load the diagram renderer."));
        document.head.appendChild(script);
      });
    }

    return mermaidPromise;
  }

  function diagramTheme() {
    return document.querySelector(".shell.dark") ? "dark" : "neutral";
  }

  function sanitizeSvg(root) {
    root.querySelectorAll("script, iframe, object, embed, foreignObject").forEach((element) => element.remove());
    for (const element of [root, ...root.querySelectorAll("*")]) {
      for (const attribute of [...element.attributes]) {
        const name = attribute.name.toLowerCase();
        if (name.startsWith("on") || ((name === "href" || name.endsWith(":href")) && !attribute.value.startsWith("#"))) {
          element.removeAttribute(attribute.name);
        }
      }
    }
  }

  function applyDiagramStyle(canvas, css) {
    const previous = diagramSheets.get(canvas);
    if (!css) {
      if (previous) {
        document.adoptedStyleSheets = document.adoptedStyleSheets.filter((sheet) => sheet !== previous);
        diagramSheets.delete(canvas);
      }
      return;
    }

    const sheet = previous ?? new CSSStyleSheet();
    sheet.replaceSync(css);
    if (!previous) {
      document.adoptedStyleSheets = [...document.adoptedStyleSheets, sheet];
      diagramSheets.set(canvas, sheet);
    }
  }

  function showDiagramError(block, message) {
    const error = block.querySelector("[data-mermaid-error]");
    if (!error) return;
    error.hidden = false;
    error.textContent = message;
  }

  async function renderDiagrams() {
    const blocks = [...document.querySelectorAll("[data-mermaid]")];
    if (blocks.length === 0) return;

    let mermaid;
    try {
      mermaid = await loadMermaid();
    } catch (reason) {
      for (const block of blocks) showDiagramError(block, reason instanceof Error ? reason.message : "Could not render this Mermaid diagram.");
      return;
    }

    mermaid.initialize({
      startOnLoad: false,
      securityLevel: "strict",
      suppressErrorRendering: true,
      htmlLabels: false,
      theme: diagramTheme(),
      flowchart: { htmlLabels: false, nodeSpacing: 40, rankSpacing: 55, padding: 16 }
    });

    for (const block of blocks) {
      const canvas = block.querySelector("[data-mermaid-canvas]");
      const body = block.querySelector("[data-mermaid-body]");
      if (!canvas || !body) continue;
      const source = body.textContent ?? "";
      const key = diagramTheme() + "\n" + source;
      if (canvas.getAttribute("data-rendered") === key) continue;
      canvas.setAttribute("data-rendered", key);
      const error = block.querySelector("[data-mermaid-error]");
      if (error) {
        error.hidden = true;
        error.textContent = "";
      }

      if (source.length > maxDiagramLength) {
        canvas.replaceChildren();
        applyDiagramStyle(canvas, "");
        showDiagramError(block, `Diagram source exceeds ${maxDiagramLength} characters.`);
        continue;
      }

      try {
        const result = await mermaid.render(`plan-mermaid-${renderNumber++}`, source);
        const parsed = new DOMParser().parseFromString(result.svg, "image/svg+xml");
        if (parsed.querySelector("parsererror")) throw new Error("Mermaid returned invalid SVG.");
        let css = [...parsed.querySelectorAll("style")].map((style) => {
          const text = style.textContent ?? "";
          style.remove();
          return text;
        }).join("\n");
        sanitizeSvg(parsed.documentElement);
        // Move presentation styles into the constructed stylesheet: CSP blocks
        // style attributes when the SVG is inserted into the document.
        let styledNumber = 0;
        for (const element of [parsed.documentElement, ...parsed.querySelectorAll("[style]")]) {
          const style = element.getAttribute("style");
          element.removeAttribute("style");
          if (!style) continue;
          const token = `diagram-style-${styledNumber++}`;
          element.classList.add(token);
          css += `\n#${parsed.documentElement.id}.${token}, #${parsed.documentElement.id} .${token} { ${style} }`;
        }
        canvas.replaceChildren(document.importNode(parsed.documentElement, true));
        applyDiagramStyle(canvas, css);
      } catch (reason) {
        canvas.replaceChildren();
        applyDiagramStyle(canvas, "");
        showDiagramError(block, reason instanceof Error ? reason.message : "Could not render this Mermaid diagram.");
      }
    }
  }

  const desktopPanels = window.matchMedia("(min-width: 1100px)");

  function initializePanels() {
    document.querySelectorAll("[data-side-panel]").forEach((panel) => {
      if (desktopPanels.matches) {
        if (panel.matches(":popover-open")) panel.hidePopover();
        panel.removeAttribute("popover");
        panel.setAttribute("role", "region");
      } else {
        panel.setAttribute("popover", "auto");
        panel.setAttribute("role", "dialog");
      }
      document.querySelectorAll(`[data-panel-trigger="${panel.dataset.sidePanel}"]`).forEach((button) => {
        button.setAttribute("aria-expanded", String(panel.matches(":popover-open")));
      });
    });
  }
  document.addEventListener("toggle", (event) => {
    const panel = event.target;
    if (!(panel instanceof Element) || !panel.matches("[data-side-panel]")) return;
    document.querySelectorAll(`[data-panel-trigger="${panel.dataset.sidePanel}"]`).forEach((button) => {
      button.setAttribute("aria-expanded", String(event.newState === "open"));
    });
  }, true);

  // Measure the shared editor toolbar so panels and heading links clear it.
  let observedLayout = [];
  function measureToolbar() {
    const shell = document.querySelector(".shell");
    const main = shell?.querySelector(":scope > main");
    if (!main) return;
    const toolbar = main.querySelector("[data-editor-toolbar]");
    const values = {
      "--panel-header-offset": 0,
      "--panel-toolbar-height": toolbar?.getBoundingClientRect().height ?? 0,
      "--panel-scroll-height": main.clientHeight
    };
    for (const [name, height] of Object.entries(values)) {
      const value = height + "px";
      if (shell.style.getPropertyValue(name) !== value) shell.style.setProperty(name, value);
    }
  }
  const layoutObserver = new ResizeObserver(measureToolbar);
  function observeToolbar() {
    const elements = [...document.querySelectorAll(".shell > main, [data-editor-toolbar]")];
    if (elements.length !== observedLayout.length || elements.some((element, index) => element !== observedLayout[index])) {
      layoutObserver.disconnect();
      elements.forEach((element) => layoutObserver.observe(element));
      observedLayout = elements;
    }
    measureToolbar();
  }

  desktopPanels.addEventListener("change", () => {
    initializePanels();
    measureToolbar();
  });

  document.addEventListener("click", (event) => {
    const target = event.target instanceof Element ? event.target : null;
    const action = target?.closest("[data-contents] a, [data-prompt]");
    const panel = action?.closest("[data-side-panel]");
    if (panel?.matches(":popover-open")) panel.hidePopover();
    if (action?.matches("[data-contents] a") && !event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey) {
      const url = new URL(action.href);
      const heading = document.getElementById(decodeURIComponent(url.hash.slice(1)));
      const main = heading?.closest(".shell > main");
      if (main && url.pathname === location.pathname && url.search === location.search) {
        event.preventDefault();
        const padding = parseFloat(getComputedStyle(main).scrollPaddingTop) || 0;
        main.scrollTop += heading.getBoundingClientRect().top - main.getBoundingClientRect().top - padding;
        history.replaceState(history.state, "", url);
      }
    }
  }, true);

  new MutationObserver(() => {
    initializePanels();
    observeToolbar();
    document.querySelectorAll("[data-top]").forEach(place);
    renderDiagrams();
  }).observe(document.documentElement, {
    subtree: true,
    childList: true,
    attributes: true,
    attributeFilter: ["data-top", "data-left", "class"]
  });

  initializePanels();
  observeToolbar();
  renderDiagrams();

  window.addEventListener("resize", () => document.querySelectorAll("[data-top]").forEach(place));

  window.annotateSelection = { read: read };
})();
