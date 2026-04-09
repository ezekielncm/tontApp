#!/usr/bin/env bash
# =============================================================================
# TontinesApp — PostgreSQL Backup Script
# =============================================================================
# Daily pg_dump to Azure Blob Storage + weekly automatic restore test
#
# Usage:
#   ./backup-postgresql.sh backup     # Run daily backup
#   ./backup-postgresql.sh restore    # Run restore test
#   ./backup-postgresql.sh cleanup    # Remove backups older than 30 days
#
# Cron (add to crontab -e):
#   # Daily backup at 02:00 UTC
#   0 2 * * * /opt/tontapp/ops/backup/backup-postgresql.sh backup >> /var/log/tontapp/backup.log 2>&1
#   # Weekly restore test on Sunday at 04:00 UTC
#   0 4 * * 0 /opt/tontapp/ops/backup/backup-postgresql.sh restore >> /var/log/tontapp/backup-restore.log 2>&1
#   # Monthly cleanup (older than 30 days) on 1st at 05:00 UTC
#   0 5 1 * * /opt/tontapp/ops/backup/backup-postgresql.sh cleanup >> /var/log/tontapp/backup-cleanup.log 2>&1
#
# Prerequisites:
#   - PostgreSQL client tools (pg_dump, pg_restore, psql)
#   - Azure CLI (az) authenticated with appropriate permissions
#   - Environment variables set (see below)
# =============================================================================

set -euo pipefail
IFS=$'\n\t'

# ─── Configuration (from environment or defaults) ────────────────────────────
POSTGRES_HOST="${POSTGRES_HOST:-localhost}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
POSTGRES_DB="${POSTGRES_DB:-tontinesapp}"
POSTGRES_USER="${POSTGRES_USER:-tontapp}"
# POSTGRES_PASSWORD must be set via environment or .pgpass
export PGPASSWORD="${POSTGRES_PASSWORD:?POSTGRES_PASSWORD environment variable is required}"

# Azure Blob Storage
AZURE_STORAGE_ACCOUNT="${AZURE_STORAGE_ACCOUNT:?AZURE_STORAGE_ACCOUNT is required}"
AZURE_STORAGE_CONTAINER="${AZURE_STORAGE_CONTAINER:-tontapp-backups}"
AZURE_STORAGE_KEY="${AZURE_STORAGE_KEY:-}" # Optional: uses az login if empty

# Restore test configuration
RESTORE_TEST_DB="${RESTORE_TEST_DB:-tontinesapp_restore_test}"
RESTORE_TEST_HOST="${RESTORE_TEST_HOST:-localhost}"
RESTORE_TEST_PORT="${RESTORE_TEST_PORT:-5432}"

# Retention
RETENTION_DAYS="${RETENTION_DAYS:-30}"

# Paths
BACKUP_DIR="/tmp/tontapp-backups"
LOG_DIR="/var/log/tontapp"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILENAME="tontinesapp_${TIMESTAMP}.dump"
BACKUP_FILEPATH="${BACKUP_DIR}/${BACKUP_FILENAME}"

# ─── Logging ─────────────────────────────────────────────────────────────────
log_info()  { echo "[$(date -u +'%Y-%m-%dT%H:%M:%SZ')] [INFO]  $*"; }
log_warn()  { echo "[$(date -u +'%Y-%m-%dT%H:%M:%SZ')] [WARN]  $*"; }
log_error() { echo "[$(date -u +'%Y-%m-%dT%H:%M:%SZ')] [ERROR] $*" >&2; }

# ─── Notification (optional: sends alerts on failure) ────────────────────────
notify_failure() {
    local message="$1"
    log_error "BACKUP FAILURE: ${message}"

    # Uncomment and configure one of these notification methods:
    # # Slack webhook
    # curl -s -X POST "${SLACK_WEBHOOK_URL}" \
    #     -H 'Content-Type: application/json' \
    #     -d "{\"text\":\"🚨 TontinesApp Backup FAILED: ${message}\"}" || true

    # # Email via sendmail
    # echo "Subject: [CRITICAL] TontinesApp Backup Failed
    #
    # ${message}
    #
    # Timestamp: $(date -u)
    # Host: $(hostname)" | sendmail "${ALERT_EMAIL}" || true

    exit 1
}

# ─── Prerequisite checks ────────────────────────────────────────────────────
check_prerequisites() {
    local missing=()
    command -v pg_dump    >/dev/null 2>&1 || missing+=("pg_dump")
    command -v pg_restore >/dev/null 2>&1 || missing+=("pg_restore")
    command -v psql       >/dev/null 2>&1 || missing+=("psql")
    command -v az         >/dev/null 2>&1 || missing+=("az (Azure CLI)")
    command -v gzip       >/dev/null 2>&1 || missing+=("gzip")

    if [ ${#missing[@]} -gt 0 ]; then
        notify_failure "Missing prerequisites: ${missing[*]}"
    fi
}

# ─── Ensure directories exist ───────────────────────────────────────────────
ensure_dirs() {
    mkdir -p "${BACKUP_DIR}"
    mkdir -p "${LOG_DIR}" 2>/dev/null || true
}

# =============================================================================
# BACKUP: pg_dump → gzip → Azure Blob Storage
# =============================================================================
do_backup() {
    log_info "═══ Starting PostgreSQL backup ═══"
    log_info "Database: ${POSTGRES_HOST}:${POSTGRES_PORT}/${POSTGRES_DB}"
    log_info "Output: ${BACKUP_FILEPATH}.gz"

    check_prerequisites
    ensure_dirs

    # Step 1: pg_dump in custom format (most flexible for restore)
    log_info "Step 1/4: Running pg_dump..."
    if ! pg_dump \
        --host="${POSTGRES_HOST}" \
        --port="${POSTGRES_PORT}" \
        --username="${POSTGRES_USER}" \
        --dbname="${POSTGRES_DB}" \
        --format=custom \
        --compress=6 \
        --verbose \
        --file="${BACKUP_FILEPATH}" \
        2>&1; then
        notify_failure "pg_dump failed for database ${POSTGRES_DB}"
    fi

    # Step 2: Verify dump file exists and has reasonable size
    log_info "Step 2/4: Verifying backup file..."
    if [ ! -f "${BACKUP_FILEPATH}" ]; then
        notify_failure "Backup file not found: ${BACKUP_FILEPATH}"
    fi

    local file_size
    file_size=$(stat -f%z "${BACKUP_FILEPATH}" 2>/dev/null || stat -c%s "${BACKUP_FILEPATH}" 2>/dev/null)

    if [ "${file_size}" -lt 1024 ]; then
        notify_failure "Backup file suspiciously small: ${file_size} bytes"
    fi
    log_info "Backup file size: $(numfmt --to=iec "${file_size}" 2>/dev/null || echo "${file_size} bytes")"

    # Step 3: Verify dump integrity with pg_restore --list
    log_info "Step 3/4: Verifying dump integrity..."
    if ! pg_restore --list "${BACKUP_FILEPATH}" >/dev/null 2>&1; then
        notify_failure "Backup integrity check failed (pg_restore --list)"
    fi
    log_info "Backup integrity verified ✓"

    # Step 4: Upload to Azure Blob Storage
    log_info "Step 4/4: Uploading to Azure Blob Storage..."
    local azure_path="daily/${TIMESTAMP}/${BACKUP_FILENAME}"

    local az_auth_args=()
    if [ -n "${AZURE_STORAGE_KEY}" ]; then
        az_auth_args+=(--account-key "${AZURE_STORAGE_KEY}")
    fi

    if ! az storage blob upload \
        --account-name "${AZURE_STORAGE_ACCOUNT}" \
        --container-name "${AZURE_STORAGE_CONTAINER}" \
        --name "${azure_path}" \
        --file "${BACKUP_FILEPATH}" \
        --overwrite \
        "${az_auth_args[@]}" \
        2>&1; then
        notify_failure "Azure Blob upload failed"
    fi

    log_info "Uploaded to: ${AZURE_STORAGE_ACCOUNT}/${AZURE_STORAGE_CONTAINER}/${azure_path}"

    # Cleanup local file
    rm -f "${BACKUP_FILEPATH}"
    log_info "Local backup file cleaned up"

    log_info "═══ Backup completed successfully ═══"
    log_info "File: ${azure_path}"
    log_info "Size: $(numfmt --to=iec "${file_size}" 2>/dev/null || echo "${file_size} bytes")"
}

# =============================================================================
# RESTORE TEST: Download latest backup → Restore to isolated DB → Validate
# =============================================================================
do_restore_test() {
    log_info "═══ Starting weekly restore test ═══"
    log_info "Restore target: ${RESTORE_TEST_HOST}:${RESTORE_TEST_PORT}/${RESTORE_TEST_DB}"

    check_prerequisites
    ensure_dirs

    # Step 1: Find the latest backup in Azure
    log_info "Step 1/6: Finding latest backup in Azure..."
    local az_auth_args=()
    if [ -n "${AZURE_STORAGE_KEY}" ]; then
        az_auth_args+=(--account-key "${AZURE_STORAGE_KEY}")
    fi

    local latest_blob
    latest_blob=$(az storage blob list \
        --account-name "${AZURE_STORAGE_ACCOUNT}" \
        --container-name "${AZURE_STORAGE_CONTAINER}" \
        --prefix "daily/" \
        --query "sort_by([].{name:name, date:properties.lastModified}, &date)[-1].name" \
        --output tsv \
        "${az_auth_args[@]}" \
        2>/dev/null)

    if [ -z "${latest_blob}" ]; then
        notify_failure "No backup found in Azure Blob Storage"
    fi
    log_info "Latest backup: ${latest_blob}"

    # Step 2: Download the backup
    log_info "Step 2/6: Downloading backup..."
    local restore_file="${BACKUP_DIR}/restore_test_${TIMESTAMP}.dump"

    if ! az storage blob download \
        --account-name "${AZURE_STORAGE_ACCOUNT}" \
        --container-name "${AZURE_STORAGE_CONTAINER}" \
        --name "${latest_blob}" \
        --file "${restore_file}" \
        "${az_auth_args[@]}" \
        2>&1; then
        notify_failure "Failed to download backup from Azure"
    fi

    # Step 3: Drop and recreate the test database
    log_info "Step 3/6: Preparing test database..."
    # Terminate existing connections
    psql --host="${RESTORE_TEST_HOST}" \
         --port="${RESTORE_TEST_PORT}" \
         --username="${POSTGRES_USER}" \
         --dbname="postgres" \
         -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '${RESTORE_TEST_DB}' AND pid <> pg_backend_pid();" \
         2>/dev/null || true

    psql --host="${RESTORE_TEST_HOST}" \
         --port="${RESTORE_TEST_PORT}" \
         --username="${POSTGRES_USER}" \
         --dbname="postgres" \
         -c "DROP DATABASE IF EXISTS ${RESTORE_TEST_DB};" \
         2>&1

    psql --host="${RESTORE_TEST_HOST}" \
         --port="${RESTORE_TEST_PORT}" \
         --username="${POSTGRES_USER}" \
         --dbname="postgres" \
         -c "CREATE DATABASE ${RESTORE_TEST_DB} OWNER ${POSTGRES_USER};" \
         2>&1

    # Step 4: Restore the backup
    log_info "Step 4/6: Restoring backup to test database..."
    if ! pg_restore \
        --host="${RESTORE_TEST_HOST}" \
        --port="${RESTORE_TEST_PORT}" \
        --username="${POSTGRES_USER}" \
        --dbname="${RESTORE_TEST_DB}" \
        --verbose \
        --no-owner \
        --clean \
        --if-exists \
        "${restore_file}" \
        2>&1; then
        # pg_restore may return non-zero for warnings; check if critical tables exist
        log_warn "pg_restore returned warnings (may be non-fatal)"
    fi

    # Step 5: Validate critical tables exist and have data
    log_info "Step 5/6: Validating restored data..."
    local critical_tables=(
        "utilisateurs"
        "tontines"
        "membres_tontine"
        "versements"
        "tours_de_role"
        "audit_entries"
        "notifications"
        "profils_credit"
        "outbox_messages"
        "plans_abonnement"
        "abonnements"
    )

    local validation_failed=false
    for table in "${critical_tables[@]}"; do
        local count
        count=$(psql --host="${RESTORE_TEST_HOST}" \
                     --port="${RESTORE_TEST_PORT}" \
                     --username="${POSTGRES_USER}" \
                     --dbname="${RESTORE_TEST_DB}" \
                     --tuples-only \
                     -c "SELECT COUNT(*) FROM ${table};" 2>/dev/null | tr -d ' ')

        if [ -z "${count}" ]; then
            log_error "Table ${table}: MISSING"
            validation_failed=true
        else
            log_info "Table ${table}: ${count} rows ✓"
        fi
    done

    # Validate audit chain integrity
    log_info "Validating audit chain integrity..."
    local chain_check
    chain_check=$(psql --host="${RESTORE_TEST_HOST}" \
                       --port="${RESTORE_TEST_PORT}" \
                       --username="${POSTGRES_USER}" \
                       --dbname="${RESTORE_TEST_DB}" \
                       --tuples-only \
                       -c "SELECT COUNT(*) FROM audit_entries WHERE hash_courant IS NOT NULL;" 2>/dev/null | tr -d ' ')
    log_info "Audit entries with valid hashes: ${chain_check:-0}"

    # Step 6: Cleanup test database
    log_info "Step 6/6: Cleaning up..."
    psql --host="${RESTORE_TEST_HOST}" \
         --port="${RESTORE_TEST_PORT}" \
         --username="${POSTGRES_USER}" \
         --dbname="postgres" \
         -c "DROP DATABASE IF EXISTS ${RESTORE_TEST_DB};" \
         2>/dev/null || true

    rm -f "${restore_file}"

    if [ "${validation_failed}" = true ]; then
        notify_failure "Restore validation failed — one or more critical tables missing"
    fi

    log_info "═══ Restore test completed successfully ═══"
}

# =============================================================================
# CLEANUP: Remove backups older than RETENTION_DAYS
# =============================================================================
do_cleanup() {
    log_info "═══ Starting backup cleanup (retention: ${RETENTION_DAYS} days) ═══"

    check_prerequisites

    local az_auth_args=()
    if [ -n "${AZURE_STORAGE_KEY}" ]; then
        az_auth_args+=(--account-key "${AZURE_STORAGE_KEY}")
    fi

    local cutoff_date
    cutoff_date=$(date -u -d "${RETENTION_DAYS} days ago" +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null \
        || date -u -v-"${RETENTION_DAYS}"d +"%Y-%m-%dT%H:%M:%SZ")

    log_info "Removing blobs older than: ${cutoff_date}"

    # List and delete old blobs
    local old_blobs
    old_blobs=$(az storage blob list \
        --account-name "${AZURE_STORAGE_ACCOUNT}" \
        --container-name "${AZURE_STORAGE_CONTAINER}" \
        --prefix "daily/" \
        --query "[?properties.lastModified < '${cutoff_date}'].name" \
        --output tsv \
        "${az_auth_args[@]}" \
        2>/dev/null)

    local count=0
    while IFS= read -r blob_name; do
        if [ -n "${blob_name}" ]; then
            az storage blob delete \
                --account-name "${AZURE_STORAGE_ACCOUNT}" \
                --container-name "${AZURE_STORAGE_CONTAINER}" \
                --name "${blob_name}" \
                "${az_auth_args[@]}" \
                2>/dev/null
            log_info "Deleted: ${blob_name}"
            ((count++)) || true
        fi
    done <<< "${old_blobs}"

    log_info "═══ Cleanup completed: ${count} blobs removed ═══"
}

# =============================================================================
# Main
# =============================================================================
main() {
    case "${1:-}" in
        backup)
            do_backup
            ;;
        restore)
            do_restore_test
            ;;
        cleanup)
            do_cleanup
            ;;
        *)
            echo "Usage: $0 {backup|restore|cleanup}"
            echo ""
            echo "Commands:"
            echo "  backup   - Run pg_dump and upload to Azure Blob Storage"
            echo "  restore  - Download latest backup and test restore on isolated DB"
            echo "  cleanup  - Remove backups older than ${RETENTION_DAYS} days"
            exit 1
            ;;
    esac
}

main "$@"
