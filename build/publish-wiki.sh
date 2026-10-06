#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
render_only=""
if [[ "${1:-}" == "--render" ]]; then
  render_only="${2:?render directory}"
fi

python3 - "$root" "${render_only}" <<'PY'
import pathlib
import posixpath
import re
import sys

root = pathlib.Path(sys.argv[1])
render_only = sys.argv[2]
pages = {
    "docs/README.md": "Home",
    "docs/installation/README.md": "Installation-guide",
    "docs/installation/claude-code/README.md": "Claude-Code",
    "docs/installation/codex/README.md": "Codex",
    "docs/installation/cursor/README.md": "Cursor",
    "docs/installation/opencode/README.md": "OpenCode",
    "docs/development/README.md": "Development-guide",
    "docs/deployment/README.md": "Manual-deployment",
    "docs/contributing/README.md": "Contribution-guide",
    "docs/design.md": "Design",
    "docs/vocabulary.md": "Vocabulary",
    "docs/ui.md": "UI",
}
link = re.compile(r"\[([^\]]+)\]\(([^)\s]+)\)")
blob = "https://github.com/YannikG/annotate-mcp/blob/main/"

def resolve(source, url):
    path, _, anchor = url.partition("#")
    if path.startswith(("http://", "https://", "mailto:")) or path == "":
        return None
    parent = posixpath.dirname(source)
    resolved = posixpath.normpath(posixpath.join(parent, path))
    suffix = f"#{anchor}" if anchor else ""
    return resolved, suffix

def replace(source, line):
    def one(match):
        text, url = match.group(1), match.group(2)
        resolved = resolve(source, url)
        if resolved is None:
            return match.group(0)
        path, suffix = resolved
        if path in pages:
            return f"[{text}]({pages[path]}{suffix})"
        return f"[{text}]({blob}{path}{suffix})"

    return link.sub(one, line)

def rewrite(source, text):
    fenced = False
    lines = []
    for line in text.splitlines(keepends=True):
        stripped = line.lstrip()
        if stripped.startswith("```"):
            fenced = not fenced
            lines.append(line)
            continue
        lines.append(line if fenced else replace(source, line))
    return "".join(lines)

out_dir = pathlib.Path(render_only) if render_only else None
if out_dir is None:
    sys.exit(0)
out_dir.mkdir(parents=True, exist_ok=True)
for source, page in pages.items():
    body = rewrite(source, (root / source).read_text())
    (out_dir / f"{page}.md").write_text(body)
PY

if [[ -n "$render_only" ]]; then
  exit 0
fi

if [[ -z "${GITHUB_TOKEN:-}" || -z "${GITHUB_REPOSITORY:-}" ]]; then
  echo "GITHUB_TOKEN and GITHUB_REPOSITORY are required." >&2
  exit 1
fi

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
remote="https://x-access-token:${GITHUB_TOKEN}@github.com/${GITHUB_REPOSITORY}.wiki.git"
clone_log="$work/clone.log"
if ! git clone --depth 1 "$remote" "$work/wiki" >"$clone_log" 2>&1; then
  sed "s#${GITHUB_TOKEN}#***#g" "$clone_log" >&2
  echo "Cannot clone ${GITHUB_REPOSITORY}.wiki.git. Create the first wiki page at https://github.com/${GITHUB_REPOSITORY}/wiki, then re-run this workflow." >&2
  exit 1
fi
"$0" --render "$work/wiki"
git -C "$work/wiki" add -A
if git -C "$work/wiki" diff --cached --quiet; then
  exit 0
fi
git -C "$work/wiki" config user.name "github-actions[bot]"
git -C "$work/wiki" config user.email "41898282+github-actions[bot]@users.noreply.github.com"
git -C "$work/wiki" commit -m "Publish the checked-in guides."
branch="$(git -C "$work/wiki" rev-parse --abbrev-ref HEAD)"
git -C "$work/wiki" push origin "$branch"
