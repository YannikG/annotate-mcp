#!/usr/bin/env bash
set -euo pipefail

repo="${GITHUB_REPOSITORY:-YannikG/annotate-mcp}"

gh api --method PATCH "repos/${repo}" \
  -F has_wiki=true \
  -F has_issues=true \
  -F has_pull_requests=true \
  -f pull_request_creation_policy=collaborators_only \
  -F allow_squash_merge=true \
  -F allow_merge_commit=false \
  -F allow_rebase_merge=false >/dev/null

ruleset="$(cat <<'EOF'
{
  "name": "main",
  "target": "branch",
  "enforcement": "active",
  "bypass_actors": [],
  "conditions": {
    "ref_name": {
      "include": ["refs/heads/main"],
      "exclude": []
    }
  },
  "rules": [
    { "type": "deletion" },
    { "type": "non_fast_forward" },
    { "type": "required_linear_history" },
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 0,
        "dismiss_stale_reviews_on_push": false,
        "require_code_owner_review": false,
        "require_last_push_approval": false,
        "required_review_thread_resolution": false
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": false,
        "do_not_enforce_on_create": false,
        "required_status_checks": [
          { "context": "quality" },
          { "context": "contributors" },
          { "context": "issue-link" }
        ]
      }
    }
  ]
}
EOF
)"

existing="$(gh api "repos/${repo}/rulesets" --jq '.[] | select(.name=="main") | .id')"
if [[ -n "$existing" ]]; then
  gh api --method PUT "repos/${repo}/rulesets/${existing}" --input - <<<"$ruleset" >/dev/null
else
  gh api --method POST "repos/${repo}/rulesets" --input - <<<"$ruleset" >/dev/null
fi
