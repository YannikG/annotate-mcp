#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

render="$tmp/wiki"
mkdir -p "$render"
printf 'stale\n' > "$render/UI.md"
printf 'stale\n' > "$render/Vocabulary.md"
printf 'stale\n' > "$render/Claude-Code.md"
"$root/build/publish-wiki.sh" --render "$render"

sidebar="$render/_Sidebar.md"
if [[ ! -f "$sidebar" ]]; then
  echo "expected _Sidebar.md" >&2
  exit 1
fi
if ! grep -q '^\* \[Installation guide\](Installation-guide)$' "$sidebar"; then
  echo "expected Installation guide as a top-level sidebar page" >&2
  exit 1
fi
for agent in "Claude Code:Claude-Code" "Codex:Codex" "Cursor:Cursor" "OpenCode:OpenCode"; do
  label="${agent%%:*}"
  slug="${agent##*:}"
  if ! grep -q "^  \\* \\[${label}\\](Installation-guide-${slug})$" "$sidebar"; then
    echo "expected ${label} nested under Installation guide" >&2
    cat "$sidebar" >&2
    exit 1
  fi
  if [[ ! -f "$render/Installation-guide-${slug}.md" ]]; then
    echo "expected Installation-guide-${slug}.md" >&2
    exit 1
  fi
done
if grep -q '^\* \[Claude Code\]' "$sidebar"; then
  echo "Claude Code must not be a top-level wiki page" >&2
  exit 1
fi
if ! grep -q '^\* \[Design\](Design)$' "$sidebar"; then
  echo "expected Design on the sidebar" >&2
  exit 1
fi
for gone in UI.md Vocabulary.md Claude-Code.md Codex.md Cursor.md OpenCode.md; do
  if [[ -f "$render/$gone" ]]; then
    echo "wiki must not publish ${gone}" >&2
    exit 1
  fi
done
if grep -q '^## Agents$' "$render/Home.md"; then
  echo "Home must not list agents beside the installation guide" >&2
  exit 1
fi
if grep -q '^# Documentation$' "$render/Home.md"; then
  echo "wiki home must not be the docs index" >&2
  exit 1
fi
if ! grep -q 'https://github.com/frontendxlab/opencode-annotate' "$render/Home.md"; then
  echo "wiki home should explain the project for people using it" >&2
  exit 1
fi
if ! grep -q '](Installation-guide)' "$render/Home.md"; then
  echo "wiki home should link the installation guide" >&2
  exit 1
fi
if ! grep -q 'https://github.com/YannikG/annotate-mcp/wiki' "$root/README.md"; then
  echo "README must link to the wiki" >&2
  exit 1
fi
if ! grep -q 'https://github.com/YannikG/annotate-mcp/blob/main/docs/vocabulary.md' "$render/Development-guide.md"; then
  echo "expected vocabulary to stay a repo file link" >&2
  exit 1
fi
if ! grep -q 'https://github.com/YannikG/annotate-mcp/blob/main/docs/ui.md' "$render/Development-guide.md"; then
  echo "expected UI to stay a repo file link" >&2
  exit 1
fi
if ! grep -q '](Installation-guide-Codex)' "$render/Installation-guide.md"; then
  echo "expected the installation page to link the Codex wiki page" >&2
  exit 1
fi

log="$tmp/git.log"
mkdir -p "$tmp/bin"
cat > "$tmp/bin/git" << 'EOF'
#!/bin/sh
printf '%s\n' "$*" >> "$GIT_LOG"
if [ "$1" = "clone" ]; then
  echo "fatal: repository not found" >&2
  exit 1
fi
exit 0
EOF
chmod +x "$tmp/bin/git"

export GIT_LOG="$log"
export PATH="$tmp/bin:$PATH"
export GITHUB_TOKEN=test-token
export GITHUB_REPOSITORY=YannikG/annotate-mcp

set +e
output="$("$root/build/publish-wiki.sh" 2>&1)"
status=$?
set -e

if [[ -f "$log" ]] && grep -q '^init ' "$log"; then
  echo "clone failure must not run git init" >&2
  cat "$log" >&2
  exit 1
fi

if [[ "$status" -eq 0 ]]; then
  echo "expected publish-wiki.sh to fail when the wiki clone fails" >&2
  printf '%s\n' "$output" >&2
  exit 1
fi

if ! printf '%s\n' "$output" | grep -q 'https://github.com/YannikG/annotate-mcp/wiki'; then
  echo "expected the first-page URL in the error" >&2
  printf '%s\n' "$output" >&2
  exit 1
fi
