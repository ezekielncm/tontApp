# TontinesApp — Database Design Notes

## Overview

This document explains the design choices made for the PostgreSQL schema that backs TontinesApp, a SaaS platform for managing rotating savings groups (tontines) in West Africa.

**Stack:** ASP.NET Core 10 · PostgreSQL 16 · Entity Framework Core · Modular Monolith DDD  
**Bounded Contexts:** Auth (IdentityManagement), Tontine, Paiement, Notification, CreditScoring, Billing

---

## 1. Design Choices

### 1.1 UUID Primary Keys (not SERIAL / IDENTITY)

| Choice | Rationale |
|--------|-----------|
| `UUID DEFAULT gen_random_uuid()` | UUIDs are generated client-side in the Domain layer (`Guid.NewGuid()`) before persistence, which is essential for DDD aggregate identity. They avoid round-trip to the DB for ID generation, enable distributed ID generation, and prevent enumeration attacks on API endpoints. PostgreSQL 16's `gen_random_uuid()` (from `pgcrypto`) is a v4 UUID — random and unguessable. |

### 1.2 NUMERIC(15,2) for Monetary Amounts (not DECIMAL, not FLOAT)

| Choice | Rationale |
|--------|-----------|
| `NUMERIC(15,2)` | In PostgreSQL, `NUMERIC` and `DECIMAL` are synonyms — they are the same type internally. We use `NUMERIC` explicitly because it is the SQL standard name. `(15,2)` supports amounts up to 9,999,999,999,999.99 — more than sufficient for XOF (West African CFA Franc) transactions where 1 EUR ≈ 655 XOF. We avoid `FLOAT` / `DOUBLE PRECISION` because floating-point arithmetic introduces rounding errors that are unacceptable for financial data. |

### 1.3 TIMESTAMPTZ (not TIMESTAMP)

| Choice | Rationale |
|--------|-----------|
| `TIMESTAMPTZ` | PostgreSQL stores TIMESTAMPTZ in UTC internally and converts to the session timezone on display. This prevents timezone bugs in a multi-region deployment (Burkina Faso, Côte d'Ivoire, Sénégal are UTC+0/UTC+1). Bare `TIMESTAMP` loses timezone information and causes silent data corruption when servers change timezones. |

### 1.4 Soft Delete via `deleted_at`

| Choice | Rationale |
|--------|-----------|
| Nullable `deleted_at TIMESTAMPTZ` | Soft delete preserves data for audit, regulatory compliance, and accidental deletion recovery. All business queries filter with `WHERE deleted_at IS NULL` (enforced via partial indexes and EF Core global query filters). The `audit_entries` and `outbox_messages` tables do NOT have `deleted_at` because they are immutable/transient respectively. |

### 1.5 snake_case in DB, PascalCase in C#

| Choice | Rationale |
|--------|-----------|
| `snake_case` column names | PostgreSQL folds unquoted identifiers to lowercase. `snake_case` is the PostgreSQL community convention and avoids the need for quoted identifiers. EF Core's `UseSnakeCaseNamingConvention()` (from `EFCore.NamingConventions`) or explicit `HasColumnName()` handles the mapping to PascalCase C# properties. |

### 1.6 CHECK Constraints for Enums (not PostgreSQL ENUM types)

| Choice | Rationale |
|--------|-----------|
| `VARCHAR + CHECK` | PostgreSQL custom ENUM types (`CREATE TYPE`) are difficult to evolve — adding a new value requires `ALTER TYPE ... ADD VALUE` which cannot run inside a transaction in older versions and can break migrations. Using `VARCHAR` with a `CHECK` constraint is simpler to modify, works seamlessly with EF Core string-to-enum conversions, and is the recommended approach for EF Core + PostgreSQL. |

### 1.7 JSONB for Semi-structured Data

| Choice | Rationale |
|--------|-----------|
| `JSONB` for `payload`, `details`, `contenu` (outbox) | JSONB is binary-optimized, supports indexing (GIN), and provides flexibility for event payloads and behavioral details that may evolve over time without schema migrations. It is preferred over `JSON` (text storage) because it enables efficient querying and indexing. |

### 1.8 Immutable Audit Trail (Hash Chain)

| Choice | Rationale |
|--------|-----------|
| `hash_precedent` + `hash_courant` (SHA-256) | Each `audit_entries` row contains the SHA-256 hash of its content concatenated with the previous row's hash, forming a blockchain-like chain. This provides tamper-evidence: if any row is modified, all subsequent hashes become invalid. The chain is verified in the Domain layer (`AuditEntry.VerifyIntegrity()`). The table has no `deleted_at` / `updated_at` — rows are **append-only**. |

### 1.9 Transactional Outbox Pattern

| Choice | Rationale |
|--------|-----------|
| `outbox_messages` table | Domain events are written to the outbox table within the same database transaction as the aggregate changes. A background worker reads and publishes them to the message bus. This guarantees at-least-once delivery without distributed transactions. The `processed_at` column enables cleanup of old messages. |

### 1.10 Foreign Key Delete Strategy: RESTRICT

| Choice | Rationale |
|--------|-----------|
| `ON DELETE RESTRICT` (default) | In a financial system, cascading deletes are dangerous. We use RESTRICT everywhere to prevent accidental data loss. Soft-delete handles "deletion" at the application level. The only exception is `historique_comportement.versement_id` which uses `ON DELETE SET NULL` because a behavioral event can exist even if the payment reference is removed. |

---

## 2. Index Strategy & Justifications

### 2.1 utilisateurs

| Index | Type | Justification |
|-------|------|---------------|
| `ix_utilisateurs_telephone` | Partial B-tree | Login flow does phone lookup; partial index excludes soft-deleted rows for smaller, faster index. |
| `ix_utilisateurs_est_actif` | Partial B-tree | Dashboard filtering of active vs. deactivated users. |

### 2.2 plans_abonnement

| Index | Type | Justification |
|-------|------|---------------|
| `uq_plans_abonnement_code` | Unique | Business rule: plan codes must be unique. |

### 2.3 abonnements

| Index | Type | Justification |
|-------|------|---------------|
| `uq_abonnements_gestionnaire_actif` | Partial unique | Enforces at most one active subscription per manager at the DB level. |
| `ix_abonnements_date_fin` | Partial B-tree | Expiration cron job scans subscriptions by `date_fin` where status is ACTIF. |

### 2.4 tontines

| Index | Type | Justification |
|-------|------|---------------|
| `ix_tontines_gestionnaire` | Partial B-tree | "My tontines" dashboard query per manager. |
| `ix_tontines_statut` | Partial B-tree | Filtering tontines by status (Active, Draft, etc.) on admin dashboards. |

### 2.5 membres_tontine

| Index | Type | Justification |
|-------|------|---------------|
| `uq_membres_tontine_par_tontine` | Partial unique composite | Business rule: a user cannot join the same tontine twice. |
| `ix_membres_tontine_tontine_rang` | Partial composite B-tree | Listing members sorted by rank for round-robin attribution display. |
| `ix_membres_tontine_utilisateur` | Partial B-tree | "My memberships" query for a given user. |

### 2.6 tours_de_role

| Index | Type | Justification |
|-------|------|---------------|
| `uq_tours_tontine_numero` | Partial unique composite | Business rule: round numbers are unique per tontine. |
| `ix_tours_tontine_open` | Partial composite B-tree | Finding the current (non-completed) round for a tontine. |
| `ix_tours_beneficiaire` | Partial B-tree | Lookup: which rounds was a member the beneficiary of? |

### 2.7 versements

| Index | Type | Justification |
|-------|------|---------------|
| `ix_versements_tontine_tour` | Partial composite B-tree | Collection tracking: "show all payments for round X of tontine Y". |
| `ix_versements_membre` | Partial B-tree | Payment history for a specific member. |
| `ix_versements_statut` | Partial B-tree | Pending payments dashboard (filter EN_ATTENTE). |
| `uq_versements_reference_externe` | Partial unique | Prevents duplicate Mobile Money / Orange Money reference numbers for reconciliation. |

### 2.8 audit_entries

| Index | Type | Justification |
|-------|------|---------------|
| `ix_audit_entries_versement` | Composite B-tree | Reconstructing the hash chain for a versement (ordered by timestamp). |
| `ix_audit_entries_action` | B-tree | Querying by action type (e.g., find all "VersementConfirme" entries). |
| `ix_audit_entries_acteur` | B-tree | Audit queries: "show everything actor X did". |

### 2.9 notifications

| Index | Type | Justification |
|-------|------|---------------|
| `ix_notifications_pending` | Partial composite B-tree | Notification worker picks up EN_ATTENTE notifications in FIFO order. |
| `ix_notifications_destinataire` | Partial B-tree | "My notifications" query for a user's notification feed. |

### 2.10 rappel_schedules

| Index | Type | Justification |
|-------|------|---------------|
| `ix_rappel_schedules_pending` | Partial B-tree | Scheduler picks up unsent reminders due before current time. |
| `ix_rappel_schedules_tour` | Partial B-tree | List all reminders scheduled for a round. |

### 2.11 profils_credit

| Index | Type | Justification |
|-------|------|---------------|
| `uq_profils_credit_utilisateur` | Partial unique | Business rule: one credit profile per user. |
| `ix_profils_credit_score` | Partial B-tree (DESC) | Leaderboard / ranking queries showing top-scored users. |

### 2.12 historique_comportement

| Index | Type | Justification |
|-------|------|---------------|
| `ix_hc_profil_credit` | Partial composite B-tree | Score recalculation reads all events for a credit profile ordered by date. |
| `ix_hc_type_evenement` | Partial B-tree | Analytics: filtering by event type (e.g., all late payments). |

### 2.13 outbox_messages

| Index | Type | Justification |
|-------|------|---------------|
| `ix_outbox_messages_pending` | Partial B-tree | Outbox worker picks up unprocessed messages in FIFO order. |
| `ix_outbox_messages_processed` | Partial B-tree | Cleanup job removes old processed messages. |

---

## 3. Table Dependency Order (FK Graph)

```
utilisateurs
├── plans_abonnement
│   └── abonnements (→ utilisateurs, plans_abonnement)
├── tontines (→ utilisateurs)
│   ├── membres_tontine (→ tontines, utilisateurs)
│   │   ├── tours_de_role (→ tontines, membres_tontine)
│   │   │   ├── versements (→ tontines, membres_tontine, tours_de_role)
│   │   │   │   ├── audit_entries (→ versements)
│   │   │   │   └── historique_comportement (→ profils_credit, tontines, tours_de_role, versements)
│   │   │   └── rappel_schedules (→ tontines, tours_de_role)
│   ├── profils_credit (→ utilisateurs)
├── notifications (→ utilisateurs)
└── outbox_messages (standalone)
```

---

## 4. Files Produced

| File | Description |
|------|-------------|
| `db/migrations/V1__initial_schema.sql` | Complete PostgreSQL DDL script (13 tables, indexes, constraints) |
| `src/Infrastructure/Persistence/Migrations/20260408000001_InitialCreate.cs` | EF Core C# migration class |
| `docs/database_design_notes.md` | This document |
