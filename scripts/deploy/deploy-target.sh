#!/usr/bin/env bash
set -euo pipefail

TARGET_ENV="${1:-staging}"
BACKEND_DIR="${2:-$(pwd)}"
GATEWAY_NETWORK="planwriter_gateway"
PROXY_PROJECT="planwriter-proxy"

case "$TARGET_ENV" in
  staging)
    TARGET_COMPOSE="$BACKEND_DIR/docker-compose.staging.yml"
    TARGET_PROJECT="planwriter-staging"
    TARGET_CONTAINERS=(
      "planwriter-stg-sqlserver"
      "planwriter-stg-sql-init"
      "planwriter-stg-api"
      "planwriter-stg-frontend"
    )
    ;;
  production)
    TARGET_COMPOSE="$BACKEND_DIR/docker-compose.production.yml"
    TARGET_PROJECT="planwriter-production"
    TARGET_CONTAINERS=(
      "planwriter-prod-sqlserver"
      "planwriter-prod-sql-init"
      "planwriter-prod-api"
      "planwriter-prod-frontend"
    )
    ;;
  *)
    echo "Ambiente invalido: $TARGET_ENV (use staging ou production)."
    exit 1
    ;;
esac

PROXY_COMPOSE="$BACKEND_DIR/docker-compose.proxy.yml"

if [ ! -f "$TARGET_COMPOSE" ]; then
  echo "Compose nao encontrado: $TARGET_COMPOSE"
  exit 1
fi

if [ ! -f "$PROXY_COMPOSE" ]; then
  echo "Compose nao encontrado: $PROXY_COMPOSE"
  exit 1
fi

if ! docker network inspect "$GATEWAY_NETWORK" >/dev/null 2>&1; then
  docker network create "$GATEWAY_NETWORK" >/dev/null
fi

compose_target() {
  docker compose -p "$TARGET_PROJECT" -f "$TARGET_COMPOSE" "$@"
}

compose_proxy() {
  docker compose -p "$PROXY_PROJECT" -f "$PROXY_COMPOSE" "$@"
}

ensure_project_container() {
  local container_name="$1"
  local expected_project="$2"

  if docker ps -a --format '{{.Names}}' | grep -Fxq "$container_name"; then
    local actual_project
    actual_project="$(docker inspect -f '{{ index .Config.Labels "com.docker.compose.project" }}' "$container_name" 2>/dev/null || true)"

    if [ "$actual_project" != "$expected_project" ]; then
      echo "Recriando $container_name para projeto $expected_project (atual: ${actual_project:-none})"
      docker rm -f "$container_name" >/dev/null
    fi
  fi
}

for container in "${TARGET_CONTAINERS[@]}"; do
  ensure_project_container "$container" "$TARGET_PROJECT"
done

ensure_project_container "planwriter-proxy" "$PROXY_PROJECT"

echo "Subindo ambiente: $TARGET_ENV"
compose_target up -d --build

echo "Garantindo proxy local por hostname (.test)"
compose_proxy up -d

echo "Status do ambiente $TARGET_ENV"
compose_target ps

echo "Status do proxy"
compose_proxy ps
