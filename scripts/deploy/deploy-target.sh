#!/usr/bin/env bash
set -euo pipefail

TARGET_ENV="${1:-staging}"
BACKEND_DIR="${2:-$(pwd)}"
ENV_FILE="$BACKEND_DIR/.env"
GATEWAY_NETWORK="planwriter_gateway"
PROXY_PROJECT="planwriter-proxy"
LOCK_WAIT_SECONDS="${DEPLOY_LOCK_WAIT_SECONDS:-300}"
UP_RETRY_ATTEMPTS="${DEPLOY_UP_RETRY_ATTEMPTS:-5}"
UP_RETRY_DELAY_SECONDS="${DEPLOY_UP_RETRY_DELAY_SECONDS:-3}"

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
LOCK_DIR="${TMPDIR:-/tmp}/planwriter-deploy-${TARGET_ENV}.lock"
LOCK_PID_FILE="$LOCK_DIR/pid"
LOCK_ACQUIRED=0

acquire_deploy_lock() {
  local waited=0

  while ! mkdir "$LOCK_DIR" 2>/dev/null; do
    local holder_pid=""
    if [ -f "$LOCK_PID_FILE" ]; then
      holder_pid="$(cat "$LOCK_PID_FILE" 2>/dev/null || true)"
    fi

    if [ -n "$holder_pid" ] && ! kill -0 "$holder_pid" 2>/dev/null; then
      echo "Lock stale detectado ($LOCK_DIR). Limpando."
      rm -rf "$LOCK_DIR" || true
      continue
    fi

    if [ "$waited" -ge "$LOCK_WAIT_SECONDS" ]; then
      echo "Timeout aguardando lock de deploy: $LOCK_DIR"
      return 1
    fi

    echo "Aguardando lock de deploy ($TARGET_ENV). Tentando novamente em 2s..."
    sleep 2
    waited=$((waited + 2))
  done

  LOCK_ACQUIRED=1
  echo "$$" > "$LOCK_PID_FILE"
}

release_deploy_lock() {
  if [ "$LOCK_ACQUIRED" -eq 1 ]; then
    rm -f "$LOCK_PID_FILE" || true
    rmdir "$LOCK_DIR" || true
  fi
}

trap release_deploy_lock EXIT INT TERM

if [ ! -f "$TARGET_COMPOSE" ]; then
  echo "Compose nao encontrado: $TARGET_COMPOSE"
  exit 1
fi

if [ ! -f "$PROXY_COMPOSE" ]; then
  echo "Compose nao encontrado: $PROXY_COMPOSE"
  exit 1
fi

COMPOSE_ENV_ARGS=()
if [ -f "$ENV_FILE" ]; then
  COMPOSE_ENV_ARGS=(--env-file "$ENV_FILE")
  echo "Usando env file: $ENV_FILE"
else
  echo "Env file nao encontrado em $ENV_FILE. Usando apenas variaveis de ambiente do processo."
fi

acquire_deploy_lock

if ! docker network inspect "$GATEWAY_NETWORK" >/dev/null 2>&1; then
  docker network create "$GATEWAY_NETWORK" >/dev/null
fi

compose_target() {
  docker compose "${COMPOSE_ENV_ARGS[@]}" -p "$TARGET_PROJECT" -f "$TARGET_COMPOSE" "$@"
}

compose_proxy() {
  docker compose "${COMPOSE_ENV_ARGS[@]}" -p "$PROXY_PROJECT" -f "$PROXY_COMPOSE" "$@"
}

compose_target_up_with_retry() {
  local attempt=1
  while [ "$attempt" -le "$UP_RETRY_ATTEMPTS" ]; do
    local output=""
    if output="$(compose_target up -d --build 2>&1)"; then
      printf '%s\n' "$output"
      return 0
    fi

    printf '%s\n' "$output" >&2

    if echo "$output" | grep -Fq "is already in progress" && [ "$attempt" -lt "$UP_RETRY_ATTEMPTS" ]; then
      echo "Docker ainda processa remocao de container. Tentativa $attempt/$UP_RETRY_ATTEMPTS. Nova tentativa em ${UP_RETRY_DELAY_SECONDS}s..."
      sleep "$UP_RETRY_DELAY_SECONDS"
      attempt=$((attempt + 1))
      continue
    fi

    return 1
  done
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
compose_target_up_with_retry

echo "Garantindo proxy local por hostname (.test)"
compose_proxy up -d

echo "Status do ambiente $TARGET_ENV"
compose_target ps

echo "Status do proxy"
compose_proxy ps
