#!/usr/bin/env bash
set -euo pipefail

TARGET_ENV="${1:-staging}"
BACKEND_DIR="${2:-$(pwd)}"
GATEWAY_NETWORK="planwriter_gateway"

case "$TARGET_ENV" in
  staging)
    TARGET_COMPOSE="$BACKEND_DIR/docker-compose.staging.yml"
    ;;
  production)
    TARGET_COMPOSE="$BACKEND_DIR/docker-compose.production.yml"
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

echo "Subindo ambiente: $TARGET_ENV"
docker compose -f "$TARGET_COMPOSE" up -d --build

echo "Garantindo proxy local por hostname (.test)"
docker compose -f "$PROXY_COMPOSE" up -d

echo "Status do ambiente $TARGET_ENV"
docker compose -f "$TARGET_COMPOSE" ps

echo "Status do proxy"
docker compose -f "$PROXY_COMPOSE" ps
