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

  function renderDiagrams() {
    window.annotateDiagrams.render();
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
