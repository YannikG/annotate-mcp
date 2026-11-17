// Renders [data-mermaid] blocks on the page. Download calls embed(), which draws
// each fence in the light theme and writes the SVG into the report HTML.
// The saved file does not include this script or the Mermaid bundle.
// Enlarge is diagram-viewer.js: it moves the SVG and does not call Mermaid again.
(function () {
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

  // The viewer moves the canvas out of the block. data-mermaid-home is the block index.
  function diagramCanvas(block) {
    const id = block.querySelector("[data-block-index]")?.getAttribute("data-block-index") ?? "";
    if (/^\d+$/.test(id)) {
      const moved = document.querySelector(`[data-mermaid-canvas][data-mermaid-home="${id}"]`);
      if (moved) return moved;
    }
    return block.querySelector("[data-mermaid-canvas]");
  }

  function syncEnlarge(block, canvas) {
    const enlarge = block.querySelector("[data-mermaid-enlarge]");
    if (enlarge) enlarge.hidden = !canvas.querySelector("svg");
  }

  function settle(block, canvas) {
    syncEnlarge(block, canvas);
    canvas.dispatchEvent(new CustomEvent("annotate-diagram-rendered", { bubbles: true }));
  }

  async function renderDiagrams() {
    const blocks = [...document.querySelectorAll("[data-mermaid]")];
    if (blocks.length === 0) return;

    let mermaid;
    try {
      mermaid = await loadMermaid();
    } catch (reason) {
      for (const block of blocks) {
        showDiagramError(block, reason instanceof Error ? reason.message : "Could not render this Mermaid diagram.");
        const canvas = diagramCanvas(block);
        if (canvas) syncEnlarge(block, canvas);
      }
      return;
    }

    const pageTheme = diagramTheme();
    mermaid.initialize(mermaidOptions(pageTheme));

    for (const block of blocks) {
      const canvas = diagramCanvas(block);
      const body = block.querySelector("[data-mermaid-body]");
      if (!canvas || !body) continue;
      const source = body.textContent ?? "";
      const key = diagramTheme() + "\n" + source;
      if (canvas.getAttribute("data-rendered") === key) {
        syncEnlarge(block, canvas);
        continue;
      }
      canvas.setAttribute("data-rendered", key);
      const error = block.querySelector("[data-mermaid-error]");
      if (error) {
        error.hidden = true;
        error.textContent = "";
      }

      try {
        const drawn = await drawSvg(mermaid, `plan-mermaid-${renderNumber++}`, source);
        canvas.replaceChildren(document.importNode(drawn.svg, true));
        applyDiagramStyle(canvas, drawn.css);
        settle(block, canvas);
      } catch (reason) {
        canvas.replaceChildren();
        applyDiagramStyle(canvas, "");
        showDiagramError(block, reason instanceof Error ? reason.message : "Could not render this Mermaid diagram.");
        settle(block, canvas);
      }
    }
  }

  function mermaidOptions(theme) {
    return {
      startOnLoad: false,
      securityLevel: "strict",
      suppressErrorRendering: true,
      htmlLabels: false,
      theme,
      flowchart: { htmlLabels: false, nodeSpacing: 40, rankSpacing: 55, padding: 16 }
    };
  }

  // Presentation styles move into CSS: the page CSP blocks style attributes.
  async function drawSvg(mermaid, id, source) {
    if (source.length > maxDiagramLength) {
      throw new Error(`Diagram source exceeds ${maxDiagramLength} characters.`);
    }

    const result = await mermaid.render(id, source);
    const parsed = new DOMParser().parseFromString(result.svg, "image/svg+xml");
    if (parsed.querySelector("parsererror")) throw new Error("Mermaid returned invalid SVG.");
    let css = [...parsed.querySelectorAll("style")].map((style) => {
      const text = style.textContent ?? "";
      style.remove();
      return text;
    }).join("\n");
    sanitizeSvg(parsed.documentElement);
    let styledNumber = 0;
    for (const element of [parsed.documentElement, ...parsed.querySelectorAll("[style]")]) {
      const style = element.getAttribute("style");
      element.removeAttribute("style");
      if (!style) continue;
      const token = `diagram-style-${styledNumber++}`;
      element.classList.add(token);
      css += `\n#${parsed.documentElement.id}.${token}, #${parsed.documentElement.id} .${token} { ${style} }`;
    }
    return { svg: parsed.documentElement, css };
  }

  async function embed(html) {
    if (!html.includes("data-mermaid-body")) return html;
    const doc = new DOMParser().parseFromString(html, "text/html");
    const blocks = [...doc.querySelectorAll("[data-mermaid]")];
    if (blocks.length === 0) return html;

    let mermaid;
    try {
      mermaid = await loadMermaid();
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : "Could not render this Mermaid diagram.";
      for (const block of blocks) showDiagramError(block, message);
      return "<!DOCTYPE html>" + doc.documentElement.outerHTML;
    }

    const pageTheme = diagramTheme();
    mermaid.initialize(mermaidOptions("neutral"));
    const css = [];
    try {
      for (const block of blocks) {
        const canvas = block.querySelector("[data-mermaid-canvas]");
        const body = block.querySelector("[data-mermaid-body]");
        if (!canvas || !body) continue;
        try {
          const drawn = await drawSvg(mermaid, `report-mermaid-${renderNumber++}`, body.textContent ?? "");
          canvas.replaceChildren(doc.importNode(drawn.svg, true));
          if (drawn.css) css.push(drawn.css);
          const enlarge = block.querySelector("[data-mermaid-enlarge]");
          if (enlarge) enlarge.hidden = false;
        } catch (reason) {
          showDiagramError(block, reason instanceof Error ? reason.message : "Could not render this Mermaid diagram.");
        }
      }
    } finally {
      mermaid.initialize(mermaidOptions(pageTheme));
    }

    if (css.length > 0) {
      const style = doc.createElement("style");
      style.textContent = css.join("\n").replace(/<\/style/gi, "<\\/style");
      doc.head.appendChild(style);
    }

    return "<!DOCTYPE html>" + doc.documentElement.outerHTML;
  }

  window.annotateDiagrams = { render: renderDiagrams, embed: embed };
})();
