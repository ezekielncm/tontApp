// ---------------------------------------------------------------------------
// TontinesApp – EF Core Initial Migration
// ---------------------------------------------------------------------------
// This migration corresponds to the SQL script db/migrations/V1__initial_schema.sql
// It creates all 13 tables for the 6 bounded contexts:
//   Auth, Tontine, Paiement, Notification, CreditScoring, Billing
// ---------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 0. Extension
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pgcrypto\";");

        // ================================================================
        // 1. utilisateurs (Auth / IdentityManagement)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "utilisateurs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                telephone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                nom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                mot_de_passe_hash = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                est_actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_utilisateurs", x => x.id);
                table.CheckConstraint("ck_utilisateurs_role", "role IN ('MEMBRE', 'GESTIONNAIRE', 'ADMIN')");
            });

        migrationBuilder.AddUniqueConstraint(
            name: "uq_utilisateurs_telephone",
            table: "utilisateurs",
            column: "telephone");

        migrationBuilder.CreateIndex(
            name: "ix_utilisateurs_telephone",
            table: "utilisateurs",
            column: "telephone",
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_utilisateurs_est_actif",
            table: "utilisateurs",
            column: "est_actif",
            filter: "deleted_at IS NULL");

        // ================================================================
        // 2. plans_abonnement (Billing)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "plans_abonnement",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                nom = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                montant_mensuel = table.Column<decimal>(type: "numeric(15,2)", nullable: false, defaultValue: 0m),
                devise = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false, defaultValue: "XOF"),
                max_tontines = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                max_membres_par_tontine = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                description = table.Column<string>(type: "text", nullable: true),
                est_actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_plans_abonnement", x => x.id);
                table.CheckConstraint("ck_plans_abonnement_montant", "montant_mensuel >= 0");
            });

        migrationBuilder.AddUniqueConstraint(
            name: "uq_plans_abonnement_code",
            table: "plans_abonnement",
            column: "code");

        // ================================================================
        // 3. abonnements (Billing)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "abonnements",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                gestionnaire_id = table.Column<Guid>(type: "uuid", nullable: false),
                plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                statut = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                montant_mensuel = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                devise = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false, defaultValue: "XOF"),
                date_debut = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                date_fin = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_abonnements", x => x.id);
                table.ForeignKey(
                    name: "fk_abonnements_gestionnaire",
                    column: x => x.gestionnaire_id,
                    principalTable: "utilisateurs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_abonnements_plan",
                    column: x => x.plan_id,
                    principalTable: "plans_abonnement",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_abonnements_statut", "statut IN ('ACTIF', 'EXPIRE', 'ANNULE')");
                table.CheckConstraint("ck_abonnements_montant", "montant_mensuel >= 0");
                table.CheckConstraint("ck_abonnements_dates", "date_fin >= date_debut");
            });

        migrationBuilder.CreateIndex(
            name: "uq_abonnements_gestionnaire_actif",
            table: "abonnements",
            column: "gestionnaire_id",
            unique: true,
            filter: "statut = 'ACTIF' AND deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_abonnements_date_fin",
            table: "abonnements",
            column: "date_fin",
            filter: "statut = 'ACTIF'");

        // ================================================================
        // 4. tontines (Tontine)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "tontines",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                nom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                montant_cotisation = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                devise = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false, defaultValue: "XOF"),
                periodicite = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                statut = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                max_membres = table.Column<int>(type: "integer", nullable: false),
                mode_attribution = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "SEQUENTIEL"),
                gestionnaire_id = table.Column<Guid>(type: "uuid", nullable: false),
                started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tontines", x => x.id);
                table.ForeignKey(
                    name: "fk_tontines_gestionnaire",
                    column: x => x.gestionnaire_id,
                    principalTable: "utilisateurs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_tontines_periodicite", "periodicite IN ('HEBDOMADAIRE', 'BI_MENSUELLE', 'MENSUELLE')");
                table.CheckConstraint("ck_tontines_statut", "statut IN ('BROUILLON', 'ACTIVE', 'SUSPENDUE', 'CLOTUREE', 'ANNULEE')");
                table.CheckConstraint("ck_tontines_mode_attribution", "mode_attribution IN ('SEQUENTIEL', 'ALEATOIRE')");
                table.CheckConstraint("ck_tontines_montant", "montant_cotisation > 0");
                table.CheckConstraint("ck_tontines_max_membres", "max_membres >= 2");
            });

        migrationBuilder.CreateIndex(
            name: "ix_tontines_gestionnaire",
            table: "tontines",
            column: "gestionnaire_id",
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_tontines_statut",
            table: "tontines",
            column: "statut",
            filter: "deleted_at IS NULL");

        // ================================================================
        // 5. membres_tontine (Tontine)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "membres_tontine",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                tontine_id = table.Column<Guid>(type: "uuid", nullable: false),
                utilisateur_id = table.Column<Guid>(type: "uuid", nullable: false),
                nom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                rang = table.Column<int>(type: "integer", nullable: false),
                statut = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "ACTIF"),
                joined_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_membres_tontine", x => x.id);
                table.ForeignKey(
                    name: "fk_membres_tontine_tontine",
                    column: x => x.tontine_id,
                    principalTable: "tontines",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_membres_tontine_utilisateur",
                    column: x => x.utilisateur_id,
                    principalTable: "utilisateurs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_membres_tontine_statut", "statut IN ('ACTIF', 'SUSPENDU')");
                table.CheckConstraint("ck_membres_tontine_rang", "rang >= 1");
            });

        migrationBuilder.CreateIndex(
            name: "uq_membres_tontine_par_tontine",
            table: "membres_tontine",
            columns: new[] { "tontine_id", "utilisateur_id" },
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_membres_tontine_tontine_rang",
            table: "membres_tontine",
            columns: new[] { "tontine_id", "rang" },
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_membres_tontine_utilisateur",
            table: "membres_tontine",
            column: "utilisateur_id",
            filter: "deleted_at IS NULL");

        // ================================================================
        // 6. tours_de_role (Tontine)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "tours_de_role",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                tontine_id = table.Column<Guid>(type: "uuid", nullable: false),
                numero_tour = table.Column<int>(type: "integer", nullable: false),
                beneficiaire_id = table.Column<Guid>(type: "uuid", nullable: false),
                date_prevue = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                date_limite = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                est_complete = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tours_de_role", x => x.id);
                table.ForeignKey(
                    name: "fk_tours_tontine",
                    column: x => x.tontine_id,
                    principalTable: "tontines",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_tours_beneficiaire",
                    column: x => x.beneficiaire_id,
                    principalTable: "membres_tontine",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_tours_numero", "numero_tour >= 1");
                table.CheckConstraint("ck_tours_dates", "date_limite >= date_prevue");
            });

        migrationBuilder.CreateIndex(
            name: "uq_tours_tontine_numero",
            table: "tours_de_role",
            columns: new[] { "tontine_id", "numero_tour" },
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_tours_tontine_open",
            table: "tours_de_role",
            columns: new[] { "tontine_id", "est_complete" },
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_tours_beneficiaire",
            table: "tours_de_role",
            column: "beneficiaire_id",
            filter: "deleted_at IS NULL");

        // ================================================================
        // 7. versements (Paiement)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "versements",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                tontine_id = table.Column<Guid>(type: "uuid", nullable: false),
                membre_id = table.Column<Guid>(type: "uuid", nullable: false),
                tour_id = table.Column<Guid>(type: "uuid", nullable: false),
                montant = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                devise = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false, defaultValue: "XOF"),
                statut = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                reference_externe = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                confirmed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_versements", x => x.id);
                table.ForeignKey(
                    name: "fk_versements_tontine",
                    column: x => x.tontine_id,
                    principalTable: "tontines",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_versements_membre",
                    column: x => x.membre_id,
                    principalTable: "membres_tontine",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_versements_tour",
                    column: x => x.tour_id,
                    principalTable: "tours_de_role",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_versements_statut", "statut IN ('EN_ATTENTE', 'CONFIRME', 'ECHOUE')");
                table.CheckConstraint("ck_versements_montant", "montant > 0");
            });

        migrationBuilder.CreateIndex(
            name: "ix_versements_tontine_tour",
            table: "versements",
            columns: new[] { "tontine_id", "tour_id" },
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_versements_membre",
            table: "versements",
            column: "membre_id",
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_versements_statut",
            table: "versements",
            column: "statut",
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "uq_versements_reference_externe",
            table: "versements",
            column: "reference_externe",
            unique: true,
            filter: "reference_externe IS NOT NULL AND deleted_at IS NULL");

        // ================================================================
        // 8. audit_entries (Paiement – Immutable hash chain)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "audit_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                versement_id = table.Column<Guid>(type: "uuid", nullable: false),
                hash_precedent = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, defaultValue: ""),
                hash_courant = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                horodatage = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                acteur_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_audit_entries", x => x.id);
                table.ForeignKey(
                    name: "fk_audit_versement",
                    column: x => x.versement_id,
                    principalTable: "versements",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        // No deleted_at: audit entries are immutable
        migrationBuilder.CreateIndex(
            name: "ix_audit_entries_versement",
            table: "audit_entries",
            columns: new[] { "versement_id", "horodatage" });

        migrationBuilder.CreateIndex(
            name: "ix_audit_entries_action",
            table: "audit_entries",
            column: "action");

        migrationBuilder.CreateIndex(
            name: "ix_audit_entries_acteur",
            table: "audit_entries",
            column: "acteur_id");

        // ================================================================
        // 9. notifications (Notification)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "notifications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                destinataire_id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                contenu = table.Column<string>(type: "text", nullable: false),
                statut = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "EN_ATTENTE"),
                tentatives_envoi = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                max_tentatives = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                sent_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_notifications", x => x.id);
                table.ForeignKey(
                    name: "fk_notifications_destinataire",
                    column: x => x.destinataire_id,
                    principalTable: "utilisateurs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_notifications_type",
                    "type IN ('RAPPEL_PAIEMENT', 'CONFIRMATION_PAIEMENT', 'OUVERTURE_TOUR', 'CLOTURE_TOUR', 'BIENVENUE', 'SUSPENSION', 'RECAP_HEBDOMADAIRE', 'MESSAGE_PERSONNALISE')");
                table.CheckConstraint("ck_notifications_statut", "statut IN ('EN_ATTENTE', 'ENVOYEE', 'ECHOUEE')");
                table.CheckConstraint("ck_notifications_tentatives", "tentatives_envoi >= 0");
                table.CheckConstraint("ck_notifications_max_tentatives", "max_tentatives >= 1");
            });

        migrationBuilder.CreateIndex(
            name: "ix_notifications_pending",
            table: "notifications",
            columns: new[] { "statut", "created_at" },
            filter: "statut = 'EN_ATTENTE' AND deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_notifications_destinataire",
            table: "notifications",
            column: "destinataire_id",
            filter: "deleted_at IS NULL");

        // ================================================================
        // 10. rappel_schedules (Notification)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "rappel_schedules",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                tontine_id = table.Column<Guid>(type: "uuid", nullable: false),
                tour_id = table.Column<Guid>(type: "uuid", nullable: false),
                type_rappel = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                date_envoi = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                est_envoye = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_rappel_schedules", x => x.id);
                table.ForeignKey(
                    name: "fk_rappel_tontine",
                    column: x => x.tontine_id,
                    principalTable: "tontines",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_rappel_tour",
                    column: x => x.tour_id,
                    principalTable: "tours_de_role",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_rappel_type", "type_rappel IN ('AVANT_ECHEANCE', 'JOUR_J', 'APRES_ECHEANCE')");
            });

        migrationBuilder.CreateIndex(
            name: "ix_rappel_schedules_pending",
            table: "rappel_schedules",
            column: "date_envoi",
            filter: "est_envoye = FALSE AND deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_rappel_schedules_tour",
            table: "rappel_schedules",
            column: "tour_id",
            filter: "deleted_at IS NULL");

        // ================================================================
        // 11. profils_credit (CreditScoring)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "profils_credit",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                utilisateur_id = table.Column<Guid>(type: "uuid", nullable: false),
                cycles_completes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                taux_ponctualite = table.Column<decimal>(type: "numeric(5,4)", nullable: false, defaultValue: 0.0000m),
                anciennete_mois = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                niveau = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "FAIBLE"),
                derniere_maj = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_profils_credit", x => x.id);
                table.ForeignKey(
                    name: "fk_profils_credit_utilisateur",
                    column: x => x.utilisateur_id,
                    principalTable: "utilisateurs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("ck_profils_credit_niveau", "niveau IN ('EXCELLENT', 'BON', 'MOYEN', 'FAIBLE')");
                table.CheckConstraint("ck_profils_credit_taux", "taux_ponctualite >= 0 AND taux_ponctualite <= 1");
                table.CheckConstraint("ck_profils_credit_cycles", "cycles_completes >= 0");
                table.CheckConstraint("ck_profils_credit_anciennete", "anciennete_mois >= 0");
                table.CheckConstraint("ck_profils_credit_score", "score >= 0");
            });

        migrationBuilder.CreateIndex(
            name: "uq_profils_credit_utilisateur",
            table: "profils_credit",
            column: "utilisateur_id",
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_profils_credit_score",
            table: "profils_credit",
            column: "score",
            filter: "deleted_at IS NULL");

        // ================================================================
        // 12. historique_comportement (CreditScoring)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "historique_comportement",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                profil_credit_id = table.Column<Guid>(type: "uuid", nullable: false),
                tontine_id = table.Column<Guid>(type: "uuid", nullable: false),
                tour_id = table.Column<Guid>(type: "uuid", nullable: false),
                versement_id = table.Column<Guid>(type: "uuid", nullable: true),
                type_evenement = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                date_evenement = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                details = table.Column<string>(type: "jsonb", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_historique_comportement", x => x.id);
                table.ForeignKey(
                    name: "fk_hc_profil_credit",
                    column: x => x.profil_credit_id,
                    principalTable: "profils_credit",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_hc_tontine",
                    column: x => x.tontine_id,
                    principalTable: "tontines",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_hc_tour",
                    column: x => x.tour_id,
                    principalTable: "tours_de_role",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_hc_versement",
                    column: x => x.versement_id,
                    principalTable: "versements",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.CheckConstraint("ck_hc_type_evenement",
                    "type_evenement IN ('PAIEMENT_A_TEMPS', 'PAIEMENT_EN_RETARD', 'PAIEMENT_MANQUE', 'CYCLE_COMPLETE', 'SUSPENSION')");
            });

        migrationBuilder.CreateIndex(
            name: "ix_hc_profil_credit",
            table: "historique_comportement",
            columns: new[] { "profil_credit_id", "date_evenement" },
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_hc_type_evenement",
            table: "historique_comportement",
            column: "type_evenement",
            filter: "deleted_at IS NULL");

        // ================================================================
        // 13. outbox_messages (Cross-cutting: Transactional Outbox)
        // ================================================================
        migrationBuilder.CreateTable(
            name: "outbox_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                type_evenement = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                contenu = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                processed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                erreur = table.Column<string>(type: "text", nullable: true),
                nombre_tentatives = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_outbox_messages", x => x.id);
                table.CheckConstraint("ck_outbox_tentatives", "nombre_tentatives >= 0");
            });

        migrationBuilder.CreateIndex(
            name: "ix_outbox_messages_pending",
            table: "outbox_messages",
            column: "created_at",
            filter: "processed_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "ix_outbox_messages_processed",
            table: "outbox_messages",
            column: "processed_at",
            filter: "processed_at IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop in reverse FK-dependency order
        migrationBuilder.DropTable(name: "outbox_messages");
        migrationBuilder.DropTable(name: "historique_comportement");
        migrationBuilder.DropTable(name: "profils_credit");
        migrationBuilder.DropTable(name: "rappel_schedules");
        migrationBuilder.DropTable(name: "notifications");
        migrationBuilder.DropTable(name: "audit_entries");
        migrationBuilder.DropTable(name: "versements");
        migrationBuilder.DropTable(name: "tours_de_role");
        migrationBuilder.DropTable(name: "membres_tontine");
        migrationBuilder.DropTable(name: "tontines");
        migrationBuilder.DropTable(name: "abonnements");
        migrationBuilder.DropTable(name: "plans_abonnement");
        migrationBuilder.DropTable(name: "utilisateurs");
    }
}
