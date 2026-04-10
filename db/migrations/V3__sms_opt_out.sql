-- ============================================================================
-- V3: Add SMS opt-out preference and notification stats support
-- ============================================================================

-- 1. Add sms_opt_out column to utilisateurs
ALTER TABLE utilisateurs ADD COLUMN sms_opt_out BOOLEAN NOT NULL DEFAULT FALSE;
