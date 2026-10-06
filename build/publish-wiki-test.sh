#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
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
