#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

FAILED=0
TARGET_FILES=(
  "PlanWriter.API/appsettings.json"
  "docker-compose.yml"
  "docker-compose.staging.yml"
  "docker-compose.production.yml"
  ".env.docker.example"
  "README.docker.md"
)

while IFS= read -r doc_file; do
  TARGET_FILES+=("$doc_file")
done < <(find docs -maxdepth 1 -type f -name '*.md' | sort)

check_forbidden_literal() {
  local pattern="$1"
  local label="$2"
  local matches_file
  matches_file="$(mktemp)"

  if rg -n --hidden --glob '!.git/**' "$pattern" "${TARGET_FILES[@]}" >"$matches_file"; then
    echo "[FAIL] $label"
    cat "$matches_file"
    FAILED=1
  fi

  rm -f "$matches_file"
}

check_forbidden_literal "Str0ng!Senha2024" "Hardcoded SQL default password found"
check_forbidden_literal "SUA_CHAVE_SECRETA_GRANDE_E_UNICA_AQUI" "Hardcoded JWT default key found"
check_forbidden_literal "YourStrong!Passw0rd" "Hardcoded SQL sample password found"

if rg -n '\$\{MSSQL_SA_PASSWORD:-' docker-compose*.yml >/tmp/secret_check_matches.txt; then
  echo "[FAIL] Compose cannot define fallback for MSSQL_SA_PASSWORD."
  cat /tmp/secret_check_matches.txt
  FAILED=1
fi

if rg -n '\$\{JWT_KEY:-' docker-compose*.yml >/tmp/secret_check_matches.txt; then
  echo "[FAIL] Compose cannot define fallback for JWT_KEY."
  cat /tmp/secret_check_matches.txt
  FAILED=1
fi

if [ "$FAILED" -ne 0 ]; then
  echo "Security secret check failed."
  exit 1
fi

echo "Security secret check passed."
