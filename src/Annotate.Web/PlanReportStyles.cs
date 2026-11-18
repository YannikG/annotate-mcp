namespace Annotate.Web;

internal static class PlanReportStyles
{
    // The report is a standalone artifact, so it embeds its own copy of the light
    // theme tokens. ReportStyleTests pins these declarations to tokens.css.
    public const string Css =
        """
        :root {
            color-scheme: light;
            --paper: #f6f8fa;
            --ink: #1f2328;
            --line: #d1d9e0;
            --muted: #59636e;
            --surface: #ffffff;
            --surface-sunken: #eaeef2;
            --panel-inset: #f6f8fa;
            --accent: #0969da;
            --accent-ink: #0969da;
            --danger-ink: #a40e26;
            --backdrop: color-mix(in srgb, #1f2328 55%, transparent);
            --shadow-lg: 0 16px 40px rgb(31 35 40 / 0.15);
            --font-sans: system-ui, "Segoe UI", sans-serif;
            --font-mono: ui-monospace, "Cascadia Code", "Segoe UI Mono", monospace;
            --text-xs: 0.75rem;
            --text-sm: 0.875rem;
            --text-lg: 1.125rem;
            --weight-medium: 600;
            --weight-semibold: 650;
            --space-1: 0.25rem;
            --space-2: 0.5rem;
            --space-3: 0.75rem;
            --radius-xs: 4px;
            --radius-sm: 6px;
            --radius-md: 8px;
            --radius-lg: 12px;
            --z-modal: 40;
        }
        * { box-sizing: border-box; }
        body {
            margin: 0;
            background: var(--paper);
            color: var(--ink);
            font-family: var(--font-sans);
            font-size: 16px;
            line-height: 1.65;
            overflow-wrap: anywhere;
        }
        main {
            width: min(100% - 2rem, 52rem);
            margin: 2rem auto;
            padding: clamp(1.25rem, 4vw, 3rem);
            border: 1px solid var(--line);
            border-radius: var(--radius-lg);
            background: var(--surface);
        }
        h1, h2, h3, h4, h5, h6 { line-height: 1.25; letter-spacing: -0.02em; }
        h1 { font-size: clamp(1.7rem, 4vw, 2.2rem); }
        h2 { font-size: 1.5rem; }
        h3 { font-size: 1.2rem; }
        p, ul, ol, pre, blockquote, .table-wrap, details { margin-block: 1rem; }
        a { color: var(--accent); text-underline-offset: 0.15em; }
        a:focus-visible, summary:focus-visible { outline: 2px solid var(--accent); outline-offset: 4px; }
        .report-header { padding-bottom: 1.25rem; border-bottom: 1px solid var(--line); }
        .report-header h1 { margin: 0.5rem 0; }
        .report-label { margin: 0; color: var(--muted); font-size: 0.8rem; font-weight: 700; letter-spacing: 0.06em; text-transform: uppercase; }
        .report-story { margin-bottom: 0; font-size: 0.9rem; }
        .report-story span { font-weight: 650; margin-right: 0.4rem; }
        .report-context { margin-top: 1.5rem; padding: 1rem 1.25rem; border: 1px solid var(--line); border-radius: var(--radius-md); background: var(--panel-inset); }
        .report-context h2 { margin: 0 0 0.5rem; font-size: 0.9rem; letter-spacing: 0; }
        .report-criteria { margin: 0; white-space: pre-wrap; }
        .report-plan { padding-top: 0.5rem; }
        .report-plan h1, .report-plan h2, .report-plan h3 { margin-top: 1.75rem; }
        li { margin-block: 0.3rem; }
        li > ul, li > ol { margin-block: 0.25rem; }
        input[type="checkbox"] { margin-right: 0.5rem; }
        code { font-family: var(--font-mono); font-size: 0.88em; background: var(--surface-sunken); padding: 0.15em 0.3em; border-radius: var(--radius-xs); }
        pre { max-width: 100%; padding: 1rem; overflow-x: auto; background: var(--surface-sunken); border: 1px solid var(--line); border-radius: var(--radius-md); line-height: 1.5; white-space: pre; overflow-wrap: normal; }
        pre code { padding: 0; background: transparent; border-radius: 0; }
        .code-block { margin-block: 1rem; border: 1px solid var(--line); border-radius: var(--radius-md); overflow: hidden; background: var(--surface-sunken); }
        .code-language { padding: 0.5rem 1rem; border-bottom: 1px solid var(--line); color: var(--muted); font: 0.8rem/1.4 var(--font-mono); }
        .code-block pre { margin: 0; border: 0; border-radius: 0; }
        blockquote { margin-inline: 0; padding: 0.25rem 1.25rem; border-left: 3px solid var(--line); background: var(--panel-inset); color: var(--muted); }
        .table-wrap { max-width: 100%; overflow-x: auto; }
        table { width: max-content; min-width: 100%; border-collapse: collapse; font-size: 0.9rem; }
        th, td { padding: 0.65rem 0.8rem; border: 1px solid var(--line); vertical-align: top; overflow-wrap: normal; }
        th { background: var(--surface-sunken); font-weight: 650; }
        tbody tr:nth-child(even) { background: var(--panel-inset); }
        img { max-width: 100%; height: auto; border-radius: var(--radius-sm); }
        hr { margin-block: 2rem; border: 0; border-top: 1px solid var(--line); }
        details { padding: 0.8rem 1rem; border: 1px solid var(--line); border-radius: var(--radius-md); }
        summary { cursor: pointer; font-weight: 650; }
        .visually-hidden {
            position: absolute;
            width: 1px;
            height: 1px;
            padding: 0;
            margin: -1px;
            overflow: hidden;
            clip: rect(0, 0, 0, 0);
            white-space: nowrap;
            border: 0;
        }
        .btn {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: var(--space-2);
            min-height: 40px;
            padding: var(--space-2) var(--space-3);
            border: 1px solid var(--line);
            border-radius: var(--radius-md);
            background: var(--surface);
            color: var(--ink);
            font-family: inherit;
            font-size: var(--text-sm);
            font-weight: var(--weight-medium);
            line-height: 1.3;
            white-space: nowrap;
            cursor: pointer;
        }
        .btn:hover { border-color: var(--accent); color: var(--accent-ink); }
        .btn:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
        .btn-secondary { background: var(--surface); color: var(--ink); }
        .btn-quiet { border-color: transparent; background: transparent; }
        .btn-quiet:hover { background: var(--surface-sunken); }
        .btn-sm {
            min-height: 0;
            padding: var(--space-1) var(--space-2);
            border-radius: var(--radius-sm);
            font-size: var(--text-xs);
        }
        .mermaid-actions { display: flex; justify-content: flex-end; }
        .mermaid-actions:has([hidden]) { display: none; }
        .mermaid-actions .btn { margin-bottom: var(--space-2); }
        .mermaid-canvas { overflow-x: auto; }
        .mermaid-canvas svg { max-width: 100%; height: auto; }
        .mermaid-error { margin: 0.4rem 0 0; color: var(--danger-ink); }
        .mermaid-source { margin-top: var(--space-2); }
        .mermaid-source pre { margin-top: var(--space-2); }
        [data-diagram-viewer] {
            position: fixed;
            inset: 0;
            z-index: var(--z-modal);
            display: grid;
        }
        [data-diagram-viewer][hidden] { display: none; }
        .dialog-backdrop, .dialog { grid-area: 1 / 1; }
        .dialog-backdrop {
            width: 100%;
            height: 100%;
            min-height: 100%;
            margin: 0;
            padding: 0;
            border: 0;
            border-radius: 0;
            background: var(--backdrop);
            cursor: default;
        }
        .dialog {
            place-self: center;
            display: flex;
            flex-direction: column;
            align-items: stretch;
            gap: 0.85rem;
            width: min(32rem, calc(100% - 1.5rem));
            max-height: calc(100dvh - 1.5rem);
            margin: 0;
            padding: 1.25rem;
            overflow: auto;
            border: 1px solid var(--line);
            border-radius: var(--radius-lg);
            background: var(--paper);
            color: var(--ink);
            box-shadow: var(--shadow-lg);
        }
        .dialog h2 { margin: 0; font-size: var(--text-lg); font-weight: var(--weight-semibold); }
        .dialog-diagram {
            width: calc(100% - 1.5rem);
            height: calc(100dvh - 1.5rem);
            max-height: calc(100dvh - 1.5rem);
            overflow: hidden;
        }
        .diagram-viewer-bar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: var(--space-3);
        }
        .diagram-stage {
            position: relative;
            flex: 1;
            min-height: 0;
            overflow: hidden;
            touch-action: none;
            cursor: grab;
            border: 1px solid var(--line);
            border-radius: var(--radius-md);
            background: var(--surface-sunken);
        }
        .diagram-stage.is-panning { cursor: grabbing; }
        [data-diagram-viewer] .mermaid-canvas {
            position: absolute;
            top: 0;
            left: 0;
            overflow: visible;
            transform-origin: 0 0;
        }
        [data-diagram-viewer] .mermaid-canvas svg { display: block; max-width: none; }
        .dialog-group { display: flex; flex-wrap: wrap; gap: var(--space-2); }
        @media (max-width: 480px) {
            main { width: 100%; margin: 0; border: 0; border-radius: 0; }
            ul, ol { padding-left: 1.5rem; }
        }
        @media print {
            body { background: var(--surface); font-size: 11pt; }
            main { width: 100%; max-width: none; margin: 0; padding: 0; border: 0; }
            h1, h2, h3, h4, h5, h6, summary { break-after: avoid; }
            pre { white-space: pre-wrap; overflow-wrap: anywhere; }
            .table-wrap { overflow: visible; }
            table { width: 100%; min-width: 0; table-layout: fixed; }
            th, td { overflow-wrap: anywhere; }
            a { color: inherit; }
        }
        """;
}