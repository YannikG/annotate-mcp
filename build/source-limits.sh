#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"
max=500
status=0

check_file() {
  local file="$1"
  local lines
  lines="$(awk 'END { print NR }' "$file")"
  if (( lines > max )); then
    printf '%s has %s lines\n' "$file" "$lines"
    status=1
  fi
}

while IFS= read -r -d '' file; do
  case "$file" in
    */bin/*|*/obj/*|*/Migrations/*|*.g.cs) continue ;;
  esac
  check_file "$file"
done < <(find src tests -type f \( -name '*.cs' -o -name '*.razor' \) -print0)

while IFS= read -r -d '' file; do
  case "$file" in
    */bin/*|*/obj/*) continue ;;
  esac
  check_file "$file"
done < <(find src/Annotate.Web -type f -name '*.css' -print0)

exit "$status"
