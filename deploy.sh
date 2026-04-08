#!/usr/bin/env bash
# =============================================================================
# deploy.sh — TontinesApp deployment with automatic rollback
# Executed on the Azure VM via SSH from GitHub Actions.
# Expects: DOCKER_IMAGE and IMAGE_TAG environment variables.
# =============================================================================
set -euo pipefail

# ── Configuration ────────────────────────────────────────────────────────────
APP_DIR="/opt/tontapp"
COMPOSE_FILE="${APP_DIR}/docker-compose.yml"
ENV_FILE="${APP_DIR}/.env"
HEALTH_URL="http://localhost:${API_PORT:-8080}/health"
MAX_RETRIES=10
RETRY_INTERVAL=6
IMAGE_PRUNE_AGE="168h"  # Remove unused images older than 7 days

# ── Helpers ──────────────────────────────────────────────────────────────────
log()  { echo "[$(date +'%Y-%m-%d %H:%M:%S')] $*"; }
fail() { log "ERROR: $*"; exit 1; }

# ── Pre-flight checks ───────────────────────────────────────────────────────
[[ -z "${DOCKER_IMAGE:-}" ]] && fail "DOCKER_IMAGE is not set"
[[ -z "${IMAGE_TAG:-}" ]]    && fail "IMAGE_TAG is not set"
[[ -f "${ENV_FILE}" ]]       || fail ".env file not found at ${ENV_FILE}"

cd "${APP_DIR}"

NEW_TAG="${DOCKER_IMAGE}:${IMAGE_TAG}"

# ── Save current image tag for rollback ──────────────────────────────────────
PREVIOUS_TAG=""
if docker compose -f "${COMPOSE_FILE}" ps --format json 2>/dev/null | grep -q "api"; then
    PREVIOUS_TAG=$(docker compose -f "${COMPOSE_FILE}" images api --format json 2>/dev/null \
        | grep -oP '"Tag":"\K[^"]+' | head -1 || true)
    if [[ -n "${PREVIOUS_TAG}" ]]; then
        PREVIOUS_TAG="${DOCKER_IMAGE}:${PREVIOUS_TAG}"
        log "Previous image: ${PREVIOUS_TAG}"
    fi
fi

# ── Pull new image ───────────────────────────────────────────────────────────
log "Pulling image: ${NEW_TAG}"
docker pull "${NEW_TAG}" || fail "Failed to pull image ${NEW_TAG}"

# ── Update .env with new image tag ───────────────────────────────────────────
if grep -q "^IMAGE_TAG=" "${ENV_FILE}"; then
    sed -i "s|^IMAGE_TAG=.*|IMAGE_TAG=${IMAGE_TAG}|" "${ENV_FILE}"
else
    echo "IMAGE_TAG=${IMAGE_TAG}" >> "${ENV_FILE}"
fi

if grep -q "^DOCKER_REGISTRY=" "${ENV_FILE}"; then
    :
else
    # Extract registry/username from DOCKER_IMAGE (everything before last /)
    REGISTRY_PART=$(echo "${DOCKER_IMAGE}" | rev | cut -d'/' -f2- | rev)
    sed -i "s|^DOCKER_REGISTRY=.*|DOCKER_REGISTRY=${REGISTRY_PART}|" "${ENV_FILE}" 2>/dev/null || true
fi

# ── Deploy ───────────────────────────────────────────────────────────────────
log "Starting services with new image..."
docker compose -f "${COMPOSE_FILE}" up -d --remove-orphans

# ── Health check ─────────────────────────────────────────────────────────────
log "Waiting for health check at ${HEALTH_URL}..."
healthy=false
for i in $(seq 1 ${MAX_RETRIES}); do
    sleep "${RETRY_INTERVAL}"
    if curl -sf "${HEALTH_URL}" > /dev/null 2>&1; then
        healthy=true
        log "Health check passed (attempt ${i}/${MAX_RETRIES})"
        break
    fi
    log "Health check attempt ${i}/${MAX_RETRIES} failed, retrying..."
done

# ── Rollback on failure ─────────────────────────────────────────────────────
if [[ "${healthy}" != "true" ]]; then
    log "Health check failed after ${MAX_RETRIES} attempts!"

    if [[ -n "${PREVIOUS_TAG}" ]]; then
        log "Rolling back to previous image: ${PREVIOUS_TAG}"
        PREV_TAG_ONLY=$(echo "${PREVIOUS_TAG}" | rev | cut -d':' -f1 | rev)
        sed -i "s|^IMAGE_TAG=.*|IMAGE_TAG=${PREV_TAG_ONLY}|" "${ENV_FILE}"
        docker compose -f "${COMPOSE_FILE}" up -d --remove-orphans
        log "Rollback initiated. Check logs: docker compose -f ${COMPOSE_FILE} logs"
    else
        log "No previous image to rollback to."
    fi

    fail "Deployment failed — health check did not pass."
fi

# ── Cleanup ──────────────────────────────────────────────────────────────────
log "Cleaning up unused images..."
docker image prune -f --filter "until=${IMAGE_PRUNE_AGE}" 2>/dev/null || true

log "Deployment successful! Image: ${NEW_TAG}"
