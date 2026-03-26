#!/usr/bin/env bash

set -Eeuo pipefail

APP_DIR="${APP_DIR:-$(pwd)}"

if [[ ! -f "$APP_DIR/docker-compose.yml" ]]; then
  echo "docker-compose.yml not found in $APP_DIR" >&2
  exit 1
fi

if [[ ! -f "$APP_DIR/.env" ]]; then
  echo ".env not found in $APP_DIR" >&2
  exit 1
fi

resolve_compose() {
  if docker compose version >/dev/null 2>&1; then
    echo "docker compose"
    return 0
  fi

  if command -v docker-compose >/dev/null 2>&1; then
    echo "docker-compose"
    return 0
  fi

  echo "Docker Compose is required on the VPS." >&2
  exit 1
}

wait_for_health() {
  local app_port="${APP_PORT:-8080}"
  local url="http://127.0.0.1:${app_port}/health"

  if ! command -v curl >/dev/null 2>&1 && ! command -v wget >/dev/null 2>&1; then
    echo "curl or wget not available on the VPS, skipping HTTP health probe."
    return 0
  fi

  for attempt in $(seq 1 30); do
    if command -v curl >/dev/null 2>&1; then
      if curl -fsS "$url" >/dev/null; then
        echo "Health check succeeded on attempt ${attempt}."
        return 0
      fi
    else
      if wget -q -O - "$url" >/dev/null; then
        echo "Health check succeeded on attempt ${attempt}."
        return 0
      fi
    fi

    sleep 5
  done

  echo "Health check failed for ${url}" >&2
  return 1
}

COMPOSE_CMD="$(resolve_compose)"

cd "$APP_DIR"

set -a
# shellcheck disable=SC1091
source "$APP_DIR/.env"
set +a

echo "Deploying MangoTaika from ${APP_DIR}"

${COMPOSE_CMD} pull db || true
${COMPOSE_CMD} up -d --build --remove-orphans

if ! wait_for_health; then
  ${COMPOSE_CMD} ps
  ${COMPOSE_CMD} logs --tail=120 app
  exit 1
fi

${COMPOSE_CMD} ps
