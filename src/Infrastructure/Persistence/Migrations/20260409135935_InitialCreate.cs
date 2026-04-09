using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    destinataire_id = table.Column<string>(type: "text", nullable: false),
                    canal = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    contenu_message = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    contenu = table.Column<string>(type: "text", nullable: false),
                    statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tentatives_envoi = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_tentatives = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    date_envoi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_evenement = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contenu = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    erreur = table.Column<string>(type: "text", nullable: true),
                    nombre_tentatives = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plans_abonnement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nom = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prix_mensuel = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    devise = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    max_tontines = table.Column<int>(type: "integer", nullable: false),
                    max_membres_par_tontine = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    est_actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans_abonnement", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profils_credit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    membre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score_valeur = table.Column<int>(type: "integer", nullable: false),
                    score_cycles_completes = table.Column<int>(type: "integer", nullable: false),
                    score_taux_ponctualite = table.Column<double>(type: "double precision", nullable: false),
                    score_anciennete_mois = table.Column<int>(type: "integer", nullable: false),
                    score_niveau = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    score_calcule_le = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    historique_total_versements = table.Column<int>(type: "integer", nullable: false),
                    historique_versements_ponctuels = table.Column<int>(type: "integer", nullable: false),
                    historique_cycles_completes = table.Column<int>(type: "integer", nullable: false),
                    historique_date_premier_versement = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    historique_dernier_versement = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    historique_id = table.Column<Guid>(type: "uuid", nullable: false),
                    donnees_insuffisantes = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profils_credit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tontines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    montant_cotisation = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    devise = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    periodicite = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    max_membres = table.Column<int>(type: "integer", nullable: false),
                    mode_attribution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    min_membres_activation = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tontines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "utilisateurs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    telephone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mot_de_passe_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    est_actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utilisateurs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "versements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tontine_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tour_id = table.Column<Guid>(type: "uuid", nullable: false),
                    membre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    montant = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    devise = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reference_externe = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    hash_precedent = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    hash_courant = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_versements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "abonnements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gestionnaire_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    montant_mensuel = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    devise = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    date_debut = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_fin_grace = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    renouvellement_auto = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    dernier_transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abonnements", x => x.id);
                    table.ForeignKey(
                        name: "FK_abonnements_plans_abonnement_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plans_abonnement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "codes_invitation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    date_expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    nombre_usages_max = table.Column<int>(type: "integer", nullable: false),
                    nombre_usages_actuels = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tontine_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_codes_invitation", x => x.id);
                    table.ForeignKey(
                        name: "FK_codes_invitation_tontines_tontine_id",
                        column: x => x.tontine_id,
                        principalTable: "tontines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "membres_tontine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    rang = table.Column<int>(type: "integer", nullable: false),
                    statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    utilisateur_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tontine_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membres_tontine", x => x.id);
                    table.ForeignKey(
                        name: "FK_membres_tontine_tontines_tontine_id",
                        column: x => x.tontine_id,
                        principalTable: "tontines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tours_de_role",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_tour = table.Column<int>(type: "integer", nullable: false),
                    beneficiaire_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_prevue = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_limite = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    est_complete = table.Column<bool>(type: "boolean", nullable: false),
                    tontine_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tours_de_role", x => x.id);
                    table.ForeignKey(
                        name: "FK_tours_de_role_tontines_tontine_id",
                        column: x => x.tontine_id,
                        principalTable: "tontines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    versement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tontine_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    acteur_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    horodatage = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    hash_precedent = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    hash_courant = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_entries_versements_versement_id",
                        column: x => x.versement_id,
                        principalTable: "versements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "plans_abonnement",
                columns: new[] { "id", "code", "description", "devise", "est_actif", "max_membres_par_tontine", "max_tontines", "nom", "prix_mensuel" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "GRATUIT", "Plan gratuit : 1 tontine, 10 membres max", "XOF", true, 10, 1, "Gratuit", 0m },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "PRO", "Plan Pro : 10 tontines, membres illimités - 2000 FCFA/mois", "XOF", true, 2147483647, 10, "Pro", 2000m },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "IMF", "Plan IMF : sur devis, tontines et membres illimités", "XOF", true, 2147483647, 2147483647, "IMF", 0m }
                });

            migrationBuilder.CreateIndex(
                name: "ix_abonnements_date_fin",
                table: "abonnements",
                column: "date_fin");

            migrationBuilder.CreateIndex(
                name: "ix_abonnements_gestionnaire",
                table: "abonnements",
                column: "gestionnaire_id");

            migrationBuilder.CreateIndex(
                name: "IX_abonnements_plan_id",
                table: "abonnements",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_abonnements_statut",
                table: "abonnements",
                column: "statut");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_acteur",
                table: "audit_entries",
                column: "acteur_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_action",
                table: "audit_entries",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_tontine_horodatage",
                table: "audit_entries",
                columns: new[] { "tontine_id", "horodatage" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_versement_id",
                table: "audit_entries",
                column: "versement_id");

            migrationBuilder.CreateIndex(
                name: "ix_codes_invitation_code_hash",
                table: "codes_invitation",
                column: "code_hash");

            migrationBuilder.CreateIndex(
                name: "IX_codes_invitation_tontine_id",
                table: "codes_invitation",
                column: "tontine_id");

            migrationBuilder.CreateIndex(
                name: "IX_membres_tontine_tontine_id",
                table: "membres_tontine",
                column: "tontine_id");

            migrationBuilder.CreateIndex(
                name: "uq_plans_abonnement_code",
                table: "plans_abonnement",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_profils_credit_membre",
                table: "profils_credit",
                column: "membre_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tours_de_role_tontine_id",
                table: "tours_de_role",
                column: "tontine_id");

            migrationBuilder.CreateIndex(
                name: "IX_utilisateurs_telephone",
                table: "utilisateurs",
                column: "telephone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_versements_tontine_tour",
                table: "versements",
                columns: new[] { "tontine_id", "tour_id" });

            migrationBuilder.CreateIndex(
                name: "uq_versements_reference_externe",
                table: "versements",
                column: "reference_externe",
                unique: true,
                filter: "reference_externe IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abonnements");

            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.DropTable(
                name: "codes_invitation");

            migrationBuilder.DropTable(
                name: "membres_tontine");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "profils_credit");

            migrationBuilder.DropTable(
                name: "tours_de_role");

            migrationBuilder.DropTable(
                name: "utilisateurs");

            migrationBuilder.DropTable(
                name: "plans_abonnement");

            migrationBuilder.DropTable(
                name: "versements");

            migrationBuilder.DropTable(
                name: "tontines");
        }
    }
}
