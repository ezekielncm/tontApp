-- ============================================================================
-- TontinesApp – PostgreSQL 16 – Initial Schema
-- ============================================================================
-- Conventions:
--   • UUID primary keys generated via gen_random_uuid()
--   • TIMESTAMPTZ (with time-zone) everywhere, never bare TIMESTAMP
--   • NUMERIC(15,2) for monetary amounts (exact decimal; no floating-point rounding)
--   • snake_case column names
--   • Soft-delete via nullable deleted_at on every business table
--   • Audit columns (created_at, updated_at, created_by) on all business tables
--   • Immutable audit_entries with SHA-256 hash chain
--   • No business logic in triggers
-- ============================================================================

-- ============================================================================
-- 0. Extensions
-- ============================================================================
CREATE EXTENSION IF NOT EXISTS "pgcrypto"; -- provides gen_random_uuid()

-- ============================================================================
-- 1. utilisateurs  (Bounded Context: Auth / IdentityManagement)
-- ============================================================================
CREATE TABLE utilisateurs (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    telephone       VARCHAR(20) NOT NULL,
    nom             VARCHAR(100) NOT NULL,
    mot_de_passe_hash VARCHAR(256) NOT NULL,
    role            VARCHAR(20) NOT NULL CHECK (role IN ('MEMBRE', 'GESTIONNAIRE', 'ADMIN')),
    est_actif       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT uq_utilisateurs_telephone UNIQUE (telephone)
);

-- Index: fast lookup by phone (login flow)
CREATE INDEX ix_utilisateurs_telephone ON utilisateurs (telephone) WHERE deleted_at IS NULL;
-- Index: filter active users
CREATE INDEX ix_utilisateurs_est_actif ON utilisateurs (est_actif) WHERE deleted_at IS NULL;

-- ============================================================================
-- 2. plans_abonnement  (Bounded Context: Billing)
-- ============================================================================
CREATE TABLE plans_abonnement (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    nom             VARCHAR(50) NOT NULL,
    code            VARCHAR(20) NOT NULL,
    montant_mensuel NUMERIC(15,2) NOT NULL DEFAULT 0,
    devise          VARCHAR(3)  NOT NULL DEFAULT 'XOF',
    max_tontines    INT         NOT NULL DEFAULT 1,
    max_membres_par_tontine INT NOT NULL DEFAULT 10,
    description     TEXT,
    est_actif       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT uq_plans_abonnement_code UNIQUE (code),
    CONSTRAINT ck_plans_abonnement_montant CHECK (montant_mensuel >= 0)
);

-- ============================================================================
-- 3. abonnements  (Bounded Context: Billing)
-- ============================================================================
CREATE TABLE abonnements (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    gestionnaire_id UUID        NOT NULL,
    plan_id         UUID        NOT NULL,
    statut          VARCHAR(20) NOT NULL CHECK (statut IN ('ACTIF', 'EXPIRE', 'ANNULE')),
    montant_mensuel NUMERIC(15,2) NOT NULL,
    devise          VARCHAR(3)  NOT NULL DEFAULT 'XOF',
    date_debut      TIMESTAMPTZ NOT NULL,
    date_fin        TIMESTAMPTZ NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT fk_abonnements_gestionnaire FOREIGN KEY (gestionnaire_id) REFERENCES utilisateurs (id),
    CONSTRAINT fk_abonnements_plan         FOREIGN KEY (plan_id)         REFERENCES plans_abonnement (id),
    CONSTRAINT ck_abonnements_montant CHECK (montant_mensuel >= 0),
    CONSTRAINT ck_abonnements_dates   CHECK (date_fin >= date_debut)
);

-- Index: one active subscription per manager
CREATE UNIQUE INDEX uq_abonnements_gestionnaire_actif
    ON abonnements (gestionnaire_id) WHERE statut = 'ACTIF' AND deleted_at IS NULL;
-- Index: expiration checks (cron job)
CREATE INDEX ix_abonnements_date_fin ON abonnements (date_fin) WHERE statut = 'ACTIF';

-- ============================================================================
-- 4. tontines  (Bounded Context: Tontine)
-- ============================================================================
CREATE TABLE tontines (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    nom                 VARCHAR(100) NOT NULL,
    description         TEXT,
    montant_cotisation  NUMERIC(15,2) NOT NULL,
    devise              VARCHAR(3)  NOT NULL DEFAULT 'XOF',
    periodicite         VARCHAR(20) NOT NULL CHECK (periodicite IN ('HEBDOMADAIRE', 'BI_MENSUELLE', 'MENSUELLE')),
    statut              VARCHAR(20) NOT NULL CHECK (statut IN ('BROUILLON', 'ACTIVE', 'SUSPENDUE', 'CLOTUREE', 'ANNULEE')),
    max_membres         INT         NOT NULL,
    mode_attribution    VARCHAR(20) NOT NULL DEFAULT 'SEQUENTIEL' CHECK (mode_attribution IN ('SEQUENTIEL', 'ALEATOIRE')),
    gestionnaire_id     UUID        NOT NULL,
    started_at          TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by          UUID,
    deleted_at          TIMESTAMPTZ,

    CONSTRAINT fk_tontines_gestionnaire FOREIGN KEY (gestionnaire_id) REFERENCES utilisateurs (id),
    CONSTRAINT ck_tontines_montant     CHECK (montant_cotisation > 0),
    CONSTRAINT ck_tontines_max_membres CHECK (max_membres >= 2)
);

-- Index: list tontines per manager
CREATE INDEX ix_tontines_gestionnaire ON tontines (gestionnaire_id) WHERE deleted_at IS NULL;
-- Index: filter by status (dashboard queries)
CREATE INDEX ix_tontines_statut ON tontines (statut) WHERE deleted_at IS NULL;

-- ============================================================================
-- 5. membres_tontine  (Bounded Context: Tontine)
-- ============================================================================
CREATE TABLE membres_tontine (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tontine_id      UUID        NOT NULL,
    utilisateur_id  UUID        NOT NULL,
    nom             VARCHAR(100) NOT NULL,
    rang            INT         NOT NULL,
    statut          VARCHAR(20) NOT NULL DEFAULT 'ACTIF' CHECK (statut IN ('ACTIF', 'SUSPENDU')),
    joined_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT fk_membres_tontine_tontine    FOREIGN KEY (tontine_id)     REFERENCES tontines (id),
    CONSTRAINT fk_membres_tontine_utilisateur FOREIGN KEY (utilisateur_id) REFERENCES utilisateurs (id),
    CONSTRAINT ck_membres_tontine_rang CHECK (rang >= 1)
);

-- Index: unique member per tontine (one user cannot join twice)
CREATE UNIQUE INDEX uq_membres_tontine_par_tontine
    ON membres_tontine (tontine_id, utilisateur_id) WHERE deleted_at IS NULL;
-- Index: list members of a tontine sorted by rank
CREATE INDEX ix_membres_tontine_tontine_rang ON membres_tontine (tontine_id, rang) WHERE deleted_at IS NULL;
-- Index: list tontines a user belongs to
CREATE INDEX ix_membres_tontine_utilisateur ON membres_tontine (utilisateur_id) WHERE deleted_at IS NULL;

-- ============================================================================
-- 6. tours_de_role  (Bounded Context: Tontine)
-- ============================================================================
CREATE TABLE tours_de_role (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tontine_id      UUID        NOT NULL,
    numero_tour     INT         NOT NULL,
    beneficiaire_id UUID        NOT NULL,
    date_prevue     TIMESTAMPTZ NOT NULL,
    date_limite     TIMESTAMPTZ NOT NULL,
    est_complete    BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT fk_tours_tontine      FOREIGN KEY (tontine_id)      REFERENCES tontines (id),
    CONSTRAINT fk_tours_beneficiaire FOREIGN KEY (beneficiaire_id) REFERENCES membres_tontine (id),
    CONSTRAINT ck_tours_numero       CHECK (numero_tour >= 1),
    CONSTRAINT ck_tours_dates        CHECK (date_limite >= date_prevue)
);

-- Index: unique round number per tontine
CREATE UNIQUE INDEX uq_tours_tontine_numero
    ON tours_de_role (tontine_id, numero_tour) WHERE deleted_at IS NULL;
-- Index: find current/open round
CREATE INDEX ix_tours_tontine_open ON tours_de_role (tontine_id, est_complete) WHERE deleted_at IS NULL;
-- Index: beneficiary lookup
CREATE INDEX ix_tours_beneficiaire ON tours_de_role (beneficiaire_id) WHERE deleted_at IS NULL;

-- ============================================================================
-- 7. versements  (Bounded Context: Paiement)
-- ============================================================================
CREATE TABLE versements (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tontine_id          UUID        NOT NULL,
    membre_id           UUID        NOT NULL,
    tour_id             UUID        NOT NULL,
    montant             NUMERIC(15,2) NOT NULL,
    devise              VARCHAR(3)  NOT NULL DEFAULT 'XOF',
    statut              VARCHAR(20) NOT NULL CHECK (statut IN ('EN_ATTENTE', 'CONFIRME', 'ECHOUE')),
    reference_externe   VARCHAR(100),
    confirmed_at        TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by          UUID,
    deleted_at          TIMESTAMPTZ,

    CONSTRAINT fk_versements_tontine FOREIGN KEY (tontine_id) REFERENCES tontines (id),
    CONSTRAINT fk_versements_membre  FOREIGN KEY (membre_id)  REFERENCES membres_tontine (id),
    CONSTRAINT fk_versements_tour    FOREIGN KEY (tour_id)    REFERENCES tours_de_role (id),
    CONSTRAINT ck_versements_montant CHECK (montant > 0)
);

-- Index: payments per tontine + round (collection tracking)
CREATE INDEX ix_versements_tontine_tour ON versements (tontine_id, tour_id) WHERE deleted_at IS NULL;
-- Index: payment history per member
CREATE INDEX ix_versements_membre ON versements (membre_id) WHERE deleted_at IS NULL;
-- Index: filter by status (pending payments dashboard)
CREATE INDEX ix_versements_statut ON versements (statut) WHERE deleted_at IS NULL;
-- Index: external reference lookup (MoMo/Orange Money reconciliation)
CREATE UNIQUE INDEX uq_versements_reference_externe
    ON versements (reference_externe) WHERE reference_externe IS NOT NULL AND deleted_at IS NULL;

-- ============================================================================
-- 8. audit_entries  (Bounded Context: Paiement – Immutable audit trail)
-- ============================================================================
-- This table is APPEND-ONLY. Rows must never be updated or deleted.
-- The hash chain (hash_precedent → hash_courant) provides tamper-evidence.
CREATE TABLE audit_entries (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    versement_id    UUID        NOT NULL,
    hash_precedent  VARCHAR(64) NOT NULL DEFAULT '',
    hash_courant    VARCHAR(64) NOT NULL,
    horodatage      TIMESTAMPTZ NOT NULL DEFAULT now(),
    acteur_id       VARCHAR(100) NOT NULL,
    action          VARCHAR(50) NOT NULL,
    payload         JSONB       NOT NULL DEFAULT '{}',

    CONSTRAINT fk_audit_versement FOREIGN KEY (versement_id) REFERENCES versements (id)
);

-- NO deleted_at: audit entries are immutable
-- Index: audit trail per versement (chain reconstruction)
CREATE INDEX ix_audit_entries_versement ON audit_entries (versement_id, horodatage);
-- Index: search by action type
CREATE INDEX ix_audit_entries_action ON audit_entries (action);
-- Index: actor-based audit queries
CREATE INDEX ix_audit_entries_acteur ON audit_entries (acteur_id);

-- ============================================================================
-- 9. notifications  (Bounded Context: Notification)
-- ============================================================================
CREATE TABLE notifications (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    destinataire_id     UUID        NOT NULL,
    type                VARCHAR(30) NOT NULL CHECK (type IN (
        'RAPPEL_PAIEMENT', 'CONFIRMATION_PAIEMENT', 'OUVERTURE_TOUR',
        'CLOTURE_TOUR', 'BIENVENUE', 'SUSPENSION', 'RECAP_HEBDOMADAIRE',
        'MESSAGE_PERSONNALISE'
    )),
    contenu             TEXT        NOT NULL,
    statut              VARCHAR(20) NOT NULL DEFAULT 'EN_ATTENTE' CHECK (statut IN ('EN_ATTENTE', 'ENVOYEE', 'ECHOUEE')),
    tentatives_envoi    INT         NOT NULL DEFAULT 0,
    max_tentatives      INT         NOT NULL DEFAULT 3,
    sent_at             TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by          UUID,
    deleted_at          TIMESTAMPTZ,

    CONSTRAINT fk_notifications_destinataire FOREIGN KEY (destinataire_id) REFERENCES utilisateurs (id),
    CONSTRAINT ck_notifications_tentatives   CHECK (tentatives_envoi >= 0),
    CONSTRAINT ck_notifications_max_tentatives CHECK (max_tentatives >= 1)
);

-- Index: pending notifications queue (worker picks these up)
CREATE INDEX ix_notifications_pending
    ON notifications (statut, created_at) WHERE statut = 'EN_ATTENTE' AND deleted_at IS NULL;
-- Index: notifications per user
CREATE INDEX ix_notifications_destinataire ON notifications (destinataire_id) WHERE deleted_at IS NULL;

-- ============================================================================
-- 10. rappel_schedules  (Bounded Context: Notification)
-- ============================================================================
CREATE TABLE rappel_schedules (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tontine_id      UUID        NOT NULL,
    tour_id         UUID        NOT NULL,
    type_rappel     VARCHAR(30) NOT NULL CHECK (type_rappel IN ('AVANT_ECHEANCE', 'JOUR_J', 'APRES_ECHEANCE')),
    date_envoi      TIMESTAMPTZ NOT NULL,
    est_envoye      BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by      UUID,
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT fk_rappel_tontine FOREIGN KEY (tontine_id) REFERENCES tontines (id),
    CONSTRAINT fk_rappel_tour    FOREIGN KEY (tour_id)    REFERENCES tours_de_role (id)
);

-- Index: pending reminders (scheduler picks these up)
CREATE INDEX ix_rappel_schedules_pending
    ON rappel_schedules (date_envoi) WHERE est_envoye = FALSE AND deleted_at IS NULL;
-- Index: reminders per round
CREATE INDEX ix_rappel_schedules_tour ON rappel_schedules (tour_id) WHERE deleted_at IS NULL;

-- ============================================================================
-- 11. profils_credit  (Bounded Context: CreditScoring)
-- ============================================================================
CREATE TABLE profils_credit (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    utilisateur_id      UUID        NOT NULL,
    cycles_completes    INT         NOT NULL DEFAULT 0,
    taux_ponctualite    NUMERIC(5,4) NOT NULL DEFAULT 0.0000,
    anciennete_mois     INT         NOT NULL DEFAULT 0,
    score               INT         NOT NULL DEFAULT 0,
    niveau              VARCHAR(20) NOT NULL DEFAULT 'FAIBLE' CHECK (niveau IN ('EXCELLENT', 'BON', 'MOYEN', 'FAIBLE')),
    derniere_maj        TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by          UUID,
    deleted_at          TIMESTAMPTZ,

    CONSTRAINT fk_profils_credit_utilisateur FOREIGN KEY (utilisateur_id) REFERENCES utilisateurs (id),
    CONSTRAINT ck_profils_credit_taux CHECK (taux_ponctualite >= 0 AND taux_ponctualite <= 1),
    CONSTRAINT ck_profils_credit_cycles CHECK (cycles_completes >= 0),
    CONSTRAINT ck_profils_credit_anciennete CHECK (anciennete_mois >= 0),
    CONSTRAINT ck_profils_credit_score CHECK (score >= 0)
);

-- Index: one credit profile per user
CREATE UNIQUE INDEX uq_profils_credit_utilisateur
    ON profils_credit (utilisateur_id) WHERE deleted_at IS NULL;
-- Index: leaderboard / ranking queries
CREATE INDEX ix_profils_credit_score ON profils_credit (score DESC) WHERE deleted_at IS NULL;

-- ============================================================================
-- 12. historique_comportement  (Bounded Context: CreditScoring)
-- ============================================================================
CREATE TABLE historique_comportement (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    profil_credit_id    UUID        NOT NULL,
    tontine_id          UUID        NOT NULL,
    tour_id             UUID        NOT NULL,
    versement_id        UUID,
    type_evenement      VARCHAR(30) NOT NULL CHECK (type_evenement IN (
        'PAIEMENT_A_TEMPS', 'PAIEMENT_EN_RETARD', 'PAIEMENT_MANQUE',
        'CYCLE_COMPLETE', 'SUSPENSION'
    )),
    date_evenement      TIMESTAMPTZ NOT NULL DEFAULT now(),
    details             JSONB,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by          UUID,
    deleted_at          TIMESTAMPTZ,

    CONSTRAINT fk_hc_profil_credit FOREIGN KEY (profil_credit_id) REFERENCES profils_credit (id),
    CONSTRAINT fk_hc_tontine       FOREIGN KEY (tontine_id)       REFERENCES tontines (id),
    CONSTRAINT fk_hc_tour          FOREIGN KEY (tour_id)          REFERENCES tours_de_role (id),
    CONSTRAINT fk_hc_versement     FOREIGN KEY (versement_id)     REFERENCES versements (id) ON DELETE SET NULL
);

-- Index: history per credit profile (score recalculation)
CREATE INDEX ix_hc_profil_credit ON historique_comportement (profil_credit_id, date_evenement) WHERE deleted_at IS NULL;
-- Index: event type filtering
CREATE INDEX ix_hc_type_evenement ON historique_comportement (type_evenement) WHERE deleted_at IS NULL;

-- ============================================================================
-- 13. outbox_messages  (Cross-cutting: Transactional Outbox pattern)
-- ============================================================================
CREATE TABLE outbox_messages (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    type_evenement  VARCHAR(200) NOT NULL,
    contenu         JSONB       NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    processed_at    TIMESTAMPTZ,
    erreur          TEXT,
    nombre_tentatives INT       NOT NULL DEFAULT 0,

    CONSTRAINT ck_outbox_tentatives CHECK (nombre_tentatives >= 0)
);

-- NO soft-delete: outbox messages are cleaned up after processing
-- Index: unprocessed messages queue (outbox worker)
CREATE INDEX ix_outbox_messages_pending
    ON outbox_messages (created_at) WHERE processed_at IS NULL;
-- Index: cleanup of processed messages
CREATE INDEX ix_outbox_messages_processed
    ON outbox_messages (processed_at) WHERE processed_at IS NOT NULL;
