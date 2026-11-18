#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export PATH="$PWD/node_modules/.bin:$PATH"

if ! command -v ast-grep >/dev/null 2>&1; then
  echo "ast-grep 0.45.3 is required. From the repository root: npm ci" >&2
  exit 1
fi

dotnet format --verify-no-changes
ast-grep scan --config sgconfig.yml
set +e
test_output="$(ast-grep test --config sgconfig.yml --skip-snapshot-tests 2>&1)"
test_status=$?
set -e
printf '%s\n' "$test_output"
if (( test_status != 0 )) || printf '%s\n' "$test_output" | grep -q 'Configuration not found'; then
  echo "ast-grep tests failed." >&2
  exit 1
fi
./build/source-limits.sh
./build/publish-wiki-test.sh
dotnet build
dotnet test
