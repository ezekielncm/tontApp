-- ============================================================================
-- V2: Audit trail enhancement – add tontine_id column for tontine-level chain
-- ============================================================================
-- This migration adds a tontine_id column to audit_entries for efficient
-- tontine-level queries and hash chain verification. Also adds the
-- performance index on (tontine_id, horodatage DESC) for pagination.
-- ============================================================================

-- 1. Add tontine_id column (nullable first for backfill)
ALTER TABLE audit_entries ADD COLUMN tontine_id UUID;

-- 2. Backfill tontine_id from versements
UPDATE audit_entries ae
SET tontine_id = v.tontine_id
FROM versements v
WHERE ae.versement_id = v.id;

-- 3. Make tontine_id NOT NULL after backfill
ALTER TABLE audit_entries ALTER COLUMN tontine_id SET NOT NULL;

-- 4. Add FK constraint
ALTER TABLE audit_entries
    ADD CONSTRAINT fk_audit_tontine FOREIGN KEY (tontine_id) REFERENCES tontines (id);

-- 5. Performance index for pagination: (tontine_id, horodatage DESC)
CREATE INDEX ix_audit_entries_tontine_horodatage
    ON audit_entries (tontine_id, horodatage DESC);

-- 6. Index for acteur_id queries (if not already present)
CREATE INDEX IF NOT EXISTS ix_audit_entries_acteur
    ON audit_entries (acteur_id);

-- 7. INSERT-ONLY rule: prevent UPDATE and DELETE on audit_entries
CREATE OR REPLACE RULE audit_entries_no_update AS
    ON UPDATE TO audit_entries DO INSTEAD NOTHING;

CREATE OR REPLACE RULE audit_entries_no_delete AS
    ON DELETE TO audit_entries DO INSTEAD NOTHING;
