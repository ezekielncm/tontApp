# Récapitulatif Général Complet — TontinesApp

> Document de référence décrivant l'ensemble du projet TontinesApp : architecture, domaine, infrastructure, tests, déploiement et monitoring.

## 1. Vision du Projet

**TontinesApp** est une plateforme SaaS de digitalisation des tontines africaines (épargne collective rotative). L'objectif est d'éliminer les fraudes, automatiser la gestion, et créer un score crédit basé sur le comportement de paiement des membres.

- **MVP** : 3 mois, 6 sprints de 2 semaines, 50 user stories, 227 story points
- **Cible** : Burkina Faso (SMS, Orange Money, langue française)
- **Modèle** : SaaS freemium (Gratuit / Pro 2 000 FCFA/mois / IMF sur devis)
- **KPIs de succès à J84** : 3 tontines actives, 20+ comptes, 100 000 FCFA traités, > 95% livraison SMS, 0 bug critique, > 99% uptime, ≥ 1 abonnement payant

## 2. Architecture Technique

### Architecture en couches (Clean Architecture + DDD)

```
┌─────────────┐   ┌──────────────┐   ┌──────────────┐
│  Mobile App │   │  Dashboard   │   │   Webhook    │
│ React Native│   │   Next.js    │   │ Orange Money │
└──────┬──────┘   └──────┬───────┘   └──────┬───────┘
       │                 │                   │
       └─────────┬───────┴───────────────────┘
                 ▼
        ┌────────────────┐
        │   API (.NET)   │  ← JWT, Rate Limiting, OpenAPI
        ├────────────────┤
        │  Application   │  ← CQRS Commands/Queries, MediatR
        ├────────────────┤
        │    Domain      │  ← Aggregates, Value Objects, Events
        ├────────────────┤
        │ Infrastructure │  ← EF Core, SMS, Paiements, Jobs
        └───────┬────────┘
                │
   ┌────────────┼────────────┐
   ▼            ▼            ▼
PostgreSQL    Redis       Hangfire
```

### Stack complète

| Composant | Technologie | Version |
|-----------|-------------|---------|
| Backend API | ASP.NET Core | .NET 10 |
| CQRS / Events | MediatR | 14.1.0 |
| ORM / BDD | Entity Framework Core + PostgreSQL | 16-alpine |
| Cache / Sessions | Redis | 7-alpine |
| Jobs asynchrones | Hangfire (PostgreSQL backend) | — |
| Auth | JWT Bearer + Refresh Token + BCrypt | — |
| App mobile | React Native + Expo | SDK 54, RN 0.81 |
| Dashboard web | Next.js + React + Tailwind CSS | 15.2, React 19 |
| SMS | Africa's Talking API | — |
| Mobile Money | Orange Money (webhook HMAC) | — |
| Monitoring | Prometheus + Grafana | v2.53 / v11.1 |
| CI/CD | GitHub Actions → Docker Hub → SSH | — |
| Logs | Serilog (structurés JSON) | 10.0.0 |
| Métriques | prometheus-net | 8.2.1 |

### Patterns architecturaux

- ✅ Clean Architecture (Domain → Application → Infrastructure → API)
- ✅ Domain-Driven Design avec bounded contexts
- ✅ CQRS (Command Query Responsibility Segregation) via MediatR
- ✅ Outbox Pattern pour publication fiable d'événements
- ✅ Repository Pattern pour l'accès aux données
- ✅ Unit of Work Pattern pour les transactions

## 3. Bounded Contexts du Domaine (6 contextes)

### 3.1 TontineManagement — Gestion des tontines

**Aggregate Root** : `Tontine`

**Entities** : Member, Round, Invitation

**Value Objects** :
- TontineId, TontineStatus (Draft / Active / Suspended / Completed)
- TontinePeriodicity (Daily / Weekly / Monthly)
- ContributionAmount, MemberId, RoundId
- StatutMembre (Active / Suspended / Left / Removed)
- ModeAttribution (Séquentiel / Aléatoire / Enchère)
- Reglement (min/max membres, timing, frais)
- InvitationCode, InvitationId

**Domain Events** :
- TontineCreatedEvent, TontineActivatedEvent, TontineStartedEvent, TontineSuspendedEvent
- MemberAddedEvent, MemberRemovedEvent, MemberSuspendedEvent
- RoundOpenedEvent, RoundClosedEvent
- InvitationGeneratedEvent

### 3.2 PaymentManagement — Versements et audit

**Aggregate Root** : `Versement`

**Entities** : AuditEntry (chaîne SHA-256 immuable)

**Value Objects** :
- VersementId, VersementStatus (Initié / Confirmé / Échoué)
- Montant, PayeurId, TourId
- AuditEntryId, AuditAction (VersementCree / VersementConfirme / VersementRejete)

**Port** : `IMobileMoneyGateway` → implémenté par `OrangeMoneyAdapter`

**Domain Events** :
- VersementCreatedEvent, VersementConfirmedEvent, VersementRejectedEvent
- PaiementEnRetardEvent

**Audit Trail** :
- Formule : `SHA256(id|action|acteurId|timestamp|payload|hashPrécédent)`
- Première entrée : `hashPrécédent = SHA256("GENESIS_TONTINESAPP")`
- Vérification quotidienne automatique (job Hangfire 02:00 UTC)

### 3.3 NotificationManagement — SMS et rappels

**Aggregate Root** : `Notification`

**Value Objects** :
- NotificationId, Canal (SMS / PUSH / USSD)
- NotificationType, ContenuMessage (≤ 160 chars)
- NotificationStatus (Pending / Sent / Failed / Delivered), SmsTemplate

**Port** : `ISmsGateway` → implémenté par `AfricasTalkingSmsAdapter`
- Retry ×3 avec backoff exponentiel : 5min / 15min / 1h
- Rate limit : 10 SMS/membre/jour (contourné pour ConfirmationPaiement critique)
- Validation E.164 obligatoire

**Pattern** : Outbox (OutboxProcessor toutes les 30s via Hangfire)
- Notification sauvée en DB dans la même transaction que l'événement
- OutboxProcessor lit et envoie les messages en attente

**Event Handlers** :
- TourOuvertEventHandler → notifie le bénéficiaire
- VersementConfirmeEventHandler → SMS confirmation reçu
- PaiementEnRetardEventHandler → SMS rappel

### 3.4 BillingManagement — Abonnements SaaS

**Aggregate Roots** : `PlanAbonnement` + `Abonnement`

**Plans seed** :

| Plan | Prix | Tontines | Membres |
|------|------|----------|---------|
| Gratuit | 0 FCFA | 1 | 10 |
| Pro | 2 000 FCFA/mois | 10 | Illimité |
| IMF | Sur devis | Illimité | Illimité |

**Fonctionnalités** :
- Facturation mensuelle calendaire
- Période de grâce 3 jours
- Renouvellement automatique via Hangfire (J-3 rappel + J0 débit)
- Cache Redis des limites de plan (`BillingCacheService`)
- `CheckAbonnementFilter` vérifie l'abonnement actif à la création de tontine

**Domain Events** :
- AbonnementCreatedEvent, AbonnementRenouvelleEvent, AbonnementExpireEvent

### 3.5 CreditScoringManagement — Score crédit

**Aggregate Root** : `ProfilCredit`

**Entities** : HistoriqueComportement (historique pour calcul)

**Value Objects** :
- ProfilCreditId, ScoreCalcule
- NiveauRisque (Excellent / Bon / Moyen / Faible)

**Port** : `IScoringEngine` → implémenté par `RegleMetierScoringEngine`

**Formule** :
```
score = (cyclesCompletés × 20) + (tauxPonctualité × 50) + min(anciennetéMois, 24)
```
- Résultat clampé entre 0 et 100
- Recalcul automatique sur chaque `VersementConfirmedEvent`

### 3.6 IdentityManagement — Authentification

**Aggregate Root** : `Utilisateur`

**Value Objects** :
- UtilisateurId, TelephoneId (E.164)
- MotDePasseHash (BCrypt), RoleUtilisateur (MEMBRE / GESTIONNAIRE / ADMIN)

**Domain Events** :
- UtilisateurCreatedEvent, UtilisateurInscritEvent

**Services** :
- IJwtService → génération/validation JWT
- IPasswordHasher → hash BCrypt
- ILoginAttemptService → protection brute force
- IRefreshTokenService → gestion des refresh tokens

## 4. Couche Application (CQRS)

### IdentityManagement
- **Commands** : InscrireUtilisateur, ConnecterUtilisateur, Deconnecter, RefreshToken
- **Queries** : GetUtilisateurByTelephone
- **DTOs** : AuthResult (accessToken, refreshToken, expiresAt)

### TontineManagement
- **Commands** : CreateTontine, ActivateTontine, AddMember, OuvrirTour, CloturerTour, GenererCodeInvitation, RejoindreParCode
- **Queries** : GetTontineById
- **DTOs** : TontineDto

### PaymentManagement
- **Commands** : CreateVersement, InitierVersement, ConfirmVersement, RejeterVersement
- **Queries** : GetVersementsByRound, GetAuditEntries, VerifierAudit
- **Services** : AuditTrailService (génération/vérification chaîne HMAC-SHA256)

### NotificationManagement
- **Commands** : CreateNotification, SendNotification
- **Queries** : GetPendingNotifications
- **Event Handlers** : VersementConfirmeEventHandler, TourOuvertEventHandler, PaiementEnRetardEventHandler

### BillingManagement
- **Commands** : CreateAbonnement, SouscrireAbonnement, RenewAbonnement
- **Queries** : GetPlans, GetAbonnementByGestionnaire

### CreditScoringManagement
- **Queries** : GetProfilCredit
- **Event Handlers** : VersementConfirmeEventHandler (recalcul score)

### Pipeline MediatR
- ValidationBehavior (FluentValidation automatique)
- LoggingBehavior (logs requête/réponse)

## 5. Couche Infrastructure

### Persistence (EF Core + PostgreSQL)
- **TontineDbContext** : DbContext principal, publication d'événements domaine via outbox dans `SaveChangesAsync`
- **8 repositories** : Tontine, Versement, AuditEntry, Notification, Utilisateur, Abonnement, PlanAbonnement, ProfilCredit
- **Configurations** : 7 fichiers EF Core (mapping tables, contraintes)
- **Migration** : `20260408000001_InitialCreate.cs`

### Authentification
- JwtService, PasswordHasher (BCrypt), LoginAttemptService, RefreshTokenService

### SMS
- AfricasTalkingSmsAdapter (retry ×3, backoff exponentiel)
- AfricasTalkingSmsOptions (configuration)

### Paiement
- OrangeMoneyAdapter (IMobileMoneyGateway)
- Validation webhook HMAC-SHA256

### Jobs Hangfire (7 récurrents)

| Job | Classe | Cron | Rôle |
|-----|--------|------|------|
| outbox-processor | OutboxProcessor | `*/30 * * * * *` | Traitement messages outbox |
| rappel-j3-quotidien | RappelJ3Job | 08:00 UTC | Rappel 3j avant deadline |
| rappel-j1-quotidien | RappelJ1Job | 08:00 UTC | Rappel 1j avant deadline |
| recap-hebdomadaire | RecapHebdoJob | Lundi 09:00 UTC | Récapitulatif gestionnaire |
| verifier-chaine-audit | VerifierChaineAuditJob | 02:00 UTC | Vérification intégrité audit |
| rappel-renouvellement-j3 | RappelRenouvellementJ3Job | 08:00 UTC | Rappel renouvellement J-3 |
| renouvellement-abonnement | RenouvellementAbonnementJob | 00:30 UTC | Auto-renouvellement |

### Billing
- CheckAbonnementFilter (vérification à la création de tontine)
- BillingCacheService (cache Redis des limites)

### Monitoring
- PrometheusMiddleware (collecte métriques HTTP)
- TontAppMetrics (8 métriques custom)

## 6. Endpoints API

Base URL : `/api/v1`

### Authentification (`/auth`)

| Méthode | Route | Description |
|---------|-------|-------------|
| POST | `/auth/register` | Inscription (telephone, nom, motDePasse) |
| POST | `/auth/login` | Connexion → accessToken + refreshToken |
| POST | `/auth/refresh` | Renouvellement du token |
| POST | `/auth/logout` | Déconnexion (révoque le refresh token) |

### Tontines (`/tontines`)

| Méthode | Route | Description |
|---------|-------|-------------|
| POST | `/tontines` | Créer une tontine (statut Draft) |
| GET | `/tontines/{id}` | Détail d'une tontine |
| POST | `/tontines/{id}/members` | Ajouter un membre |
| POST | `/tontines/{id}/activate` | Activer la tontine |
| POST | `/tontines/{id}/rounds/open` | Ouvrir un tour |
| POST | `/tontines/{id}/rounds/{roundId}/close` | Clôturer un tour |
| GET | `/tontines/{id}/invitation/generer` | Générer un code d'invitation |
| POST | `/tontines/rejoindre` | Rejoindre via code |

### Audit (`/tontines/{id}/audit`)

| Méthode | Route | Description |
|---------|-------|-------------|
| GET | `/tontines/{id}/audit` | Entrées d'audit paginées |
| GET | `/tontines/{id}/audit/verifier` | Vérifier l'intégrité de la chaîne |

### Billing (`/billing`)

| Méthode | Route | Description |
|---------|-------|-------------|
| GET | `/billing/plans` | Liste des plans (public) |
| POST | `/billing/souscrire` | Souscrire à un plan |
| GET | `/billing/mon-abonnement` | Mon abonnement actuel |

### Autres

| Méthode | Route | Description |
|---------|-------|-------------|
| GET | `/membres/{id}/profil-credit` | Profil crédit d'un membre |
| POST | `/webhooks/orange-money` | Webhook Orange Money (HMAC-SHA256) |
| GET | `/health` | Health check |
| GET | `/metrics` | Métriques Prometheus |
| GET | `/hangfire` | Dashboard Hangfire (admin uniquement) |

## 7. App Mobile (React Native / Expo 54)

### Écrans

| Écran | Fonctionnalité |
|-------|----------------|
| LoginScreen | Authentification par téléphone |
| RegisterScreen | Inscription nouveau membre |
| HomeScreen | Liste des tontines (TontineCard, badges statut) |
| TontineDetailScreen | Progression du tour, badges membres, countdown |
| PaiementScreen | Paiement Orange Money avec polling du statut |
| GestionnaireScreen | Admin : retardataires, relance SMS, clôture tour |
| ProfilScreen | Profil utilisateur + badge score crédit |

### Stack mobile

- **State** : Zustand + SecureStore pour auth
- **Data** : React Query (offline-first, staleTime 5min, gcTime 30min, retry 2)
- **HTTP** : Axios (JWT refresh automatique, timeout 15s)
- **Validation** : Zod
- **Navigation** : React Navigation v7 (AuthStack + AppStack)

### Composants réutilisables (10)

TontineCard, MembreStatusBadge (payé/en_attente/en_retard), ProgressBar, CountdownTimer, SkeletonLoader (3 variantes), PartagerInvitation (expo-sharing), PrimaryButton, FormInput, ErrorBanner, ScoreBadge.

### Services API

- `authService.ts` : login, register, token refresh, logout
- `tontineService.ts` : fetch tontines, membres, tours, profil crédit
- `paymentService.ts` : initier paiement, vérifier statut, webhook

### Formatage

- Montants : `formatMontant()` → `5 000 FCFA` (séparateur milliers + suffixe)
- Seuil d'urgence : URGENCY_THRESHOLD_DAYS = 3

## 8. Dashboard Web (Next.js 15)

### Routes App Router

| Route | Rôle |
|-------|------|
| `/login` | Authentification JWT |
| `/admin` | Dashboard admin SaaS |
| `/admin/tontines` | Gestion de toutes les tontines |
| `/admin/alertes` | Alertes système |
| `/gestionnaire` | Dashboard gestionnaire |
| `/gestionnaire/paiements` | Suivi des paiements |
| `/gestionnaire/audit` | Logs d'audit |

### API Routes (Next.js)

- `/api/auth/login`, `/api/auth/logout`
- `/api/admin/tontines`, `/api/admin/alertes`
- `/api/audit`, `/api/relancer`

### Composants

Sidebar, StatCard, StatusBadge, Pagination, RelancerButton

### Middleware

JWT vérifié via `jose`, RBAC (Admin/Gestionnaire), extraction user info (id, role, nom) depuis le JWT, redirection `/login` si non authentifié.

### Configuration

- Next.js 15.2.9, React 19, Tailwind CSS 3.4
- Standalone output (Docker optimisé)
- TypeScript strict, path aliases `@/*`

## 9. Base de Données

### Migrations SQL

| Fichier | Lignes | Contenu |
|---------|--------|---------|
| `V1__initial_schema.sql` | 408 | Schema complet : utilisateurs, tontines, membres_tontine, versements, tours_de_role, notifications, profils_credit, outbox_messages, plans_abonnement, abonnements, refresh_tokens |
| `V2__audit_trail_tontine_chain.sql` | 38 | Table audit trail avec chaîne SHA-256 |

### Conventions BDD

- **Clés primaires** : UUID (GUID)
- **Montants** : `NUMERIC(15,2)` (pas de float pour l'argent)
- **Horodatage** : `TIMESTAMPTZ` (toujours UTC)
- **Suppression** : Soft delete via `deleted_at`
- **Audit** : Colonnes `created_at`, `updated_at`, `created_by` sur chaque table
- **Payload** : `JSONB` pour données flexibles

## 10. Docker Compose (7 services)

| Service | Image | Port | Health Check |
|---------|-------|------|--------------|
| api | .NET 10 Alpine | 8080 | `curl /health` (30s, 5 retries) |
| hangfire-worker | .NET 10 Alpine | — | `curl /health` (IsWorkerOnly=true) |
| postgres | postgres:16-alpine | 5432 | `pg_isready` |
| redis | redis:7-alpine | 6379 | `redis-cli ping` |
| dashboard | node:20-alpine | 3000 | — |
| prometheus | prom/prometheus:v2.53.0 | 9090 | — |
| grafana | grafana/grafana:11.1.0 | 3001 | — |

**Volumes** : postgres-data, redis-data, prometheus-data, grafana-data
**Réseau** : `tontapp-network` (bridge)

## 11. CI/CD

### Pipeline CI (`ci.yml`)

Déclenché sur push/PR vers `main` / `master` :

1. Build .NET 10
2. Exécution des 473 tests
3. Couverture Coverlet → ReportGenerator (HTML + Badges + TextSummary + Cobertura)
4. Validation seuil domaine ≥ 70% (`scripts/check-domain-coverage.py`)
5. Build image Docker (validation)
6. Upload artifact couverture

### Pipeline Deploy (`deploy.yml`)

Déclenché sur push vers `main` :

1. Build & test
2. Build & push image Docker Hub (tag + latest)
3. SCP `deploy.sh` + `docker-compose.yml` vers VM
4. Exécution SSH : pull image → `docker compose up -d` → health check (10 retries, 6s)
5. **Rollback automatique** si health check échoue (retour au tag précédent)
6. Nettoyage images > 7 jours

### Deploy script (`deploy.sh`)

- Vérifications préalables (IMAGE_TAG, DOCKER_IMAGE)
- Pull nouvelle image
- Mise à jour `.env` avec IMAGE_TAG
- `docker compose up -d`
- Health check en boucle (max 10 retries, timeout 10s)
- Rollback automatique si échec
- Nettoyage images anciennes

## 12. Monitoring

### Métriques Prometheus custom (8)

| Métrique | Type | Labels | Description |
|----------|------|--------|-------------|
| `tontapp_paiements_total` | Counter | status | Paiements tentés |
| `tontapp_sms_total` | Counter | status | SMS envoyés |
| `tontapp_sms_failures_total` | Counter | — | Échecs SMS |
| `tontapp_http_requests_total` | Counter | method, endpoint, status_code | Requêtes HTTP |
| `tontapp_paiement_duration_seconds` | Histogram | — | Durée traitement paiement |
| `tontapp_http_request_duration_seconds` | Histogram | method, endpoint | Latence HTTP |
| `tontapp_tontines_actives` | Gauge | — | Tontines actives |
| `tontapp_membres_total` | Gauge | — | Membres inscrits |

### Alertes Prometheus (5)

| Alerte | Condition | Sévérité | Fenêtre |
|--------|-----------|----------|---------|
| HighErrorRate5xx | > 5 erreurs 5xx/min | CRITICAL | 2 min |
| HighPaymentFailureRate | > 10% échecs paiements | CRITICAL | 15 min |
| HighSmsFailureRate | > 10% échecs SMS | WARNING | 15 min |
| HighApiLatencyP95 | p95 > 2s | WARNING | 5 min |
| HighCpuUsage | CPU > 80% | WARNING | 5 min |

### Grafana

Dashboard `tontapp-overview.json` avec KPIs : requêtes, erreurs, latence, SMS, paiements.

## 13. Sécurité

- ✅ JWT Bearer + Refresh Token (rotation), BCrypt pour hash mots de passe
- ✅ Rate limiting : 100 req/min (fixed) + 60 req/min (sliding) par IP
- ✅ Webhook HMAC-SHA256 (`CryptographicOperations.FixedTimeEquals` pour comparaison constant-time)
- ✅ Webhook idempotent (retraitement silencieux des doublons)
- ✅ Validation E.164 téléphone, SMS rate limit 10/membre/jour
- ✅ Audit trail immuable (chaîne SHA-256 vérifiée quotidiennement, log CRITICAL si compromise)
- ✅ Checklist OWASP Top 10 documentée
- ✅ Secrets via `.env` (jamais en clair dans le code)
- ✅ Protection brute force login (LoginAttemptService)

## 14. Backup et Opérations

### Backup PostgreSQL (`ops/backup/backup-postgresql.sh`)

- **Quotidien** : pg_dump → gzip → Azure Blob Storage (02:00 UTC)
- **Hebdomadaire** : Test de restauration (dimanche 04:00 UTC), validation 11 tables critiques
- **Mensuel** : Nettoyage backups > 30 jours
- **Rétention** : 30 jours

### Tests de charge (`ops/k6/load-test.js`)

| Scénario | VU | Durée | SLO |
|----------|----|-------|-----|
| Login | 50 | 5 min | p95 < 500ms |
| Paiement | 20 | 5 min | p95 < 500ms |
| Webhook | 10 | 5 min | p95 < 500ms |

## 15. Tests (473 tests)

### Répartition

| Projet | Tests | Scope |
|--------|-------|-------|
| DomainUnitsTest | 364 | Aggregates Tontine, Versement, Utilisateur, Notification, Billing, CreditScoring |
| PaymentIntegrationTests | 55 | Flux paiement complet, HMAC, TestContainers PostgreSQL, chaîne audit |
| NotificationTests | 39 | SMS adapter (AfricasTalking), templates ≤ 160 chars, event handlers |
| AuthHandlerTests | 15 | Inscription, connexion, déconnexion, refresh token |

### Stratégie pyramidale

- **70% tests unitaires** (domaine DDD) — value objects, agrégats, invariants
- **20% tests d'intégration** (API + BDD) — TestContainers PostgreSQL réel
- **10% E2E** (mobile)

### Objectifs de couverture

- 80% sur le domaine (`src/Domain/`)
- **70% seuil minimum en CI** (échec pipeline si non atteint)
- 60% global

### Convention de nommage

`MethodName_Scenario_ExpectedResult`

### Outils

| Outil | Version | Usage |
|-------|---------|-------|
| xUnit | 2.9.3 | Framework de tests |
| FluentAssertions | 8.3.0 | Assertions lisibles |
| Moq | 4.20.72 | Mocking |
| Coverlet | 6.0.4 | Couverture de code |
| TestContainers.PostgreSql | 4.5.0 | BDD réelle pour intégration |

### Contraintes

- Pas de `DateTime.Now` → utiliser `DateTime.UtcNow` ou injecter `IClock`
- TestContainers uniquement pour les tests d'intégration
- Tests unitaires : < 100ms par test
- Tests d'intégration : < 5s par test

## 16. Documentation

| Document | Chemin | Description |
|----------|--------|-------------|
| Notes BDD | `docs/database_design_notes.md` | Choix PostgreSQL : UUID PK, NUMERIC(15,2), TIMESTAMPTZ, soft deletes, JSONB |
| Diagramme domaine | `docs/tontApp_Domain.puml` | PlantUML des bounded contexts |
| Incident response | `docs/runbooks/incident-response.md` | Procédures P1-P3 (paiement bloqué, API down, SMS failure) |
| Go-live | `docs/checklists/go-live.md` | 66 points de validation pré-production |
| OWASP Top 10 | `docs/checklists/securite-owasp-top10.md` | 26 points audit sécurité |
| User Stories | `docs/TontinesApp_UserStories_MVP.docx` | 50 user stories MVP, 6 sprints |
| Prompts IA | `docs/TontinesApp_Prompts_IA.docx` | 16 prompts IA ordonnés (P-001 → P-016) |

## 17. Chiffres Clés

| Métrique | Valeur |
|----------|--------|
| Fichiers source backend (.cs) | ~230 |
| Fichiers tests (.cs) | 28 |
| Fichiers dashboard (.ts/.tsx) | ~27 |
| Fichiers mobile (.ts/.tsx) | ~36 |
| Tests unitaires + intégration | 473 |
| Bounded contexts DDD | 6 |
| Aggregates | 8 |
| Domain events | 23 |
| Controllers API | 7 |
| Endpoints API | 18+ |
| Jobs Hangfire récurrents | 7 |
| Services Docker Compose | 7 |
| Alertes Prometheus | 5 |
| Métriques custom | 8 |
| Migrations SQL | 2 |
| User stories MVP | 50 |
| Story points total | 227 |

## 18. Mapping Prompts IA ↔ Implémentation

Les 16 prompts du document `TontinesApp_Prompts_IA.docx` :

| Prompt | Titre | Statut |
|--------|-------|--------|
| P-001 | Schéma BDD complet | ✅ `db/migrations/V1__initial_schema.sql`, `V2__audit_trail` |
| P-002 | Structure projet ASP.NET Core | ✅ 4 couches Clean Architecture |
| P-003 | Docker Compose + CI/CD | ✅ `docker-compose.yml`, `ci.yml`, `deploy.yml` |
| P-004 | Module Auth (inscription, login, JWT) | ✅ AuthController, 4 commands |
| P-005 | Aggregate Tontine | ✅ Tontine aggregate, 7 commands |
| P-006 | Invitations et codes d'accès | ✅ Invitation entity, GenererCode, RejoindreParCode |
| P-007 | Aggregate Versement + Orange Money | ✅ Versement, OrangeMoneyAdapter, webhook |
| P-008 | Audit trail immuable | ✅ AuditEntry SHA-256 chain, VerifierChaineAuditJob |
| P-009 | Notifications SMS + Outbox + Rappels | ✅ NotificationManagement, Outbox, RappelJobs |
| P-010 | App React Native : architecture + auth | ✅ Expo 54, Zustand, React Query, AuthStack |
| P-011 | Écrans Tontine : home, détail, paiement | ✅ 5 écrans app, 10 composants réutilisables |
| P-012 | Module Billing : plans, abonnements | ✅ BillingManagement, 3 plans, Hangfire renewal |
| P-013 | Score crédit v1 | ✅ CreditScoringManagement, formule, ScoreBadge |
| P-014 | Dashboard web admin + monitoring | ✅ Next.js 15, Prometheus, Grafana, 5 alertes |
| P-015 | Tests de charge + sécurité + go-live | ✅ k6, OWASP checklist, go-live 66 points |
| P-016 | Stratégie de tests | ✅ 473 tests, 4 projets, couverture CI ≥ 70% |
