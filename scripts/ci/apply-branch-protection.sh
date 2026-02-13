#!/usr/bin/env bash
set -euo pipefail

if [ -z "${GITHUB_TOKEN:-}" ]; then
  echo "Defina GITHUB_TOKEN com permissao de administracao de repositorio."
  exit 1
fi

REPO="${1:-AlessandroSilveira/PlanWriter}"
BRANCH="${2:-main}"
REQUIRED_CHECK="${3:-Build and Test}"

curl -sS -L \
  -X PUT \
  -H "Accept: application/vnd.github+json" \
  -H "Authorization: Bearer ${GITHUB_TOKEN}" \
  "https://api.github.com/repos/${REPO}/branches/${BRANCH}/protection" \
  -d "{
    \"required_status_checks\": {
      \"strict\": true,
      \"contexts\": [\"${REQUIRED_CHECK}\"]
    },
    \"enforce_admins\": true,
    \"required_pull_request_reviews\": {
      \"dismiss_stale_reviews\": true,
      \"require_code_owner_reviews\": false,
      \"required_approving_review_count\": 1
    },
    \"restrictions\": null,
    \"required_conversation_resolution\": true,
    \"allow_force_pushes\": false,
    \"allow_deletions\": false
  }" >/dev/null

echo "Branch protection aplicada em ${REPO}:${BRANCH} exigindo check '${REQUIRED_CHECK}'."
