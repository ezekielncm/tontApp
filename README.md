# TontinesApp

![CI](https://github.com/ezekielncm/tontApp/actions/workflows/ci.yml/badge.svg)

Plateforme SaaS de digitalisation des tontines africaines — épargne collective rotative avec paiement mobile money, audit immuable et score crédit.

> **Vision** : Éliminer les fraudes, automatiser la gestion et créer un score crédit basé sur le comportement de paiement des membres.

## Table des matières

- [Architecture](#architecture)
- [Stack technique](#stack-technique)
- [Structure du dépôt](#structure-du-dépôt)
- [Domaine métier](#domaine-métier)
- [Endpoints API](#endpoints-api)
- [App mobile](#app-mobile)
- [Dashboard web](#dashboard-web)
- [Infrastructure](#infrastructure)
- [Démarrage rapide](#démarrage-rapide)
- [Tests](#tests)
- [CI/CD](#cicd)
- [Monitoring](#monitoring)
- [Documentation](#documentation)

## Architecture

Le projet suit une **Clean Architecture** avec **Domain-Driven Design** (DDD) et un pattern **CQRS** via MediatR :

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

## Stack technique

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

## Structure du dépôt

```text
src/
  Api/                        # Contrôleurs REST, middleware, Program.cs
  Application/                # CQRS commands/queries, handlers, DTOs, validators
  Domain/                     # Aggregates, value objects, events, ports (DDD pur)
  Infrastructure/             # EF Core, repositories, SMS, paiements, jobs, monitoring
tests/
  DomainUnitsTest/            # Tests unitaires domaine (364 tests)
  PaymentIntegrationTests/    # Tests d'intégration avec TestContainers (55 tests)
  AuthHandlerTests/           # Tests handlers authentification (15 tests)
  NotificationTests/          # Tests module notification (39 tests)
dashboard/                    # Dashboard web Next.js 15 (Admin + Gestionnaire)
mobile/                       # App React Native / Expo 54 (Android)
db/migrations/                # Migrations SQL (V1 schema + V2 audit trail)
monitoring/                   # Prometheus alertes + Grafana dashboards
ops/
  k6/                         # Tests de charge (50 VU login, 20 VU paiements)
  backup/                     # Backup PostgreSQL → Azure Blob (30j rétention)
docs/                         # Runbooks, checklists, diagrammes, user stories
scripts/                      # Script CI couverture domaine
```

## Domaine métier

Le domaine est découpé en **6 bounded contexts** :

### TontineManagement — Gestion des tontines

Aggregate `Tontine` avec entités `Member`, `Round`, `Invitation`.

- Cycle de vie : `Draft → Active → Suspended → Completed`
- Modes d'attribution : Séquentiel, Aléatoire, Enchère
- Invitations par code unique (expiration 7 jours)
- Règlement configurable (min/max membres, périodicité, frais)

### PaymentManagement — Versements et audit

Aggregate `Versement` avec entité `AuditEntry` (chaîne SHA-256 immuable).

- Intégration Orange Money via webhook HMAC-SHA256
- Statuts : `Initié → Confirmé / Échoué`
- Chaîne d'audit : `SHA256(id|action|acteur|timestamp|payload|hashPrécédent)`
- Vérification d'intégrité quotidienne automatique

### NotificationManagement — SMS et rappels

Aggregate `Notification` avec Outbox pattern.

- Adaptateur AfricasTalking (retry ×3 : 5min / 15min / 1h)
- Rate limit : 10 SMS/membre/jour (contourné pour confirmations critiques)
- Templates SMS ≤ 160 caractères, validation E.164

### BillingManagement — Abonnements SaaS

Aggregates `PlanAbonnement` + `Abonnement`.

| Plan | Prix | Tontines | Membres |
|------|------|----------|---------|
| Gratuit | 0 FCFA | 1 | 10 |
| Pro | 2 000 FCFA/mois | 10 | Illimité |
| IMF | Sur devis | Illimité | Illimité |

- Facturation mensuelle, période de grâce 3 jours
- Renouvellement automatique via Hangfire
- Cache Redis des limites de plan

### CreditScoringManagement — Score crédit

Aggregate `ProfilCredit` avec `HistoriqueComportement`.

- Formule : `(cycles × 20) + (ponctualité × 50) + min(ancienneté, 24)` → [0, 100]
- Niveaux : Excellent / Bon / Moyen / Faible
- Recalcul automatique sur chaque versement confirmé

### IdentityManagement — Authentification

Aggregate `Utilisateur`.

- Inscription par téléphone (E.164) + mot de passe BCrypt
- JWT Bearer + Refresh Token (rotation)
- Rôles : `MEMBRE`, `GESTIONNAIRE`, `ADMIN`
- Protection brute force (tentatives de login)

## Endpoints API

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

## App mobile

Application **React Native / Expo 54** (Android) avec architecture offline-first.

| Écran | Fonctionnalité |
|-------|----------------|
| LoginScreen | Authentification par téléphone |
| RegisterScreen | Inscription nouveau membre |
| HomeScreen | Liste des tontines (TontineCard, badges) |
| TontineDetailScreen | Progression du tour, badges membres, countdown |
| PaiementScreen | Paiement Orange Money avec polling |
| GestionnaireScreen | Admin : retardataires, relance SMS, clôture tour |
| ProfilScreen | Profil utilisateur + badge score crédit |

**Stack mobile** : Zustand (state), React Query (offline-first, staleTime 5min), Axios (JWT refresh auto), Zod (validation), expo-secure-store (tokens), React Navigation v7.

**Composants réutilisables** : TontineCard, MembreStatusBadge, ProgressBar, CountdownTimer, SkeletonLoader, PartagerInvitation, PrimaryButton, FormInput, ErrorBanner, ScoreBadge.

## Dashboard web

Application **Next.js 15** (App Router) avec React 19 et Tailwind CSS.

| Route | Rôle |
|-------|------|
| `/login` | Authentification JWT |
| `/admin` | Dashboard admin SaaS |
| `/admin/tontines` | Gestion de toutes les tontines |
| `/admin/alertes` | Alertes système |
| `/gestionnaire` | Dashboard gestionnaire |
| `/gestionnaire/paiements` | Suivi des paiements |
| `/gestionnaire/audit` | Logs d'audit |

**Middleware** : JWT via `jose`, RBAC (Admin / Gestionnaire), redirection `/login`.

## Infrastructure

### Docker Compose (7 services)

| Service | Image | Port |
|---------|-------|------|
| api | .NET 10 Alpine | 8080 |
| hangfire-worker | .NET 10 Alpine | — |
| postgres | postgres:16-alpine | 5432 |
| redis | redis:7-alpine | 6379 |
| dashboard | node:20-alpine | 3000 |
| prometheus | prom/prometheus:v2.53.0 | 9090 |
| grafana | grafana/grafana:11.1.0 | 3001 |

### Jobs Hangfire (7 récurrents)

| Job | Fréquence | Rôle |
|-----|-----------|------|
| OutboxProcessor | Toutes les 30s | Traitement des messages outbox (envoi SMS) |
| RappelJ3 | Quotidien 08:00 UTC | Rappel 3 jours avant date limite |
| RappelJ1 | Quotidien 08:00 UTC | Rappel 1 jour avant date limite |
| RecapHebdo | Lundi 09:00 UTC | Récapitulatif hebdomadaire gestionnaire |
| VerifierChaineAudit | Quotidien 02:00 UTC | Vérification intégrité chaîne d'audit |
| RappelRenouvellement | Quotidien 08:00 UTC | Rappel renouvellement abonnement J-3 |
| RenouvellementAbonnement | Quotidien 00:30 UTC | Auto-renouvellement des abonnements |

### Sécurité

- JWT Bearer + Refresh Token (rotation), BCrypt pour hash mots de passe
- Rate limiting : 100 req/min (fixed) + 60 req/min (sliding) par IP
- Webhook HMAC-SHA256 (comparaison constant-time via `CryptographicOperations.FixedTimeEquals`)
- Validation E.164 téléphone, SMS rate limit 10/membre/jour
- Audit trail immuable (chaîne SHA-256 vérifiée quotidiennement)

## Démarrage rapide

### Prérequis

- SDK .NET 10
- Docker (pour PostgreSQL, Redis, et tests d'intégration)
- Node.js 20 (pour dashboard et mobile)

### Avec Docker Compose (recommandé)

```bash
cp .env.example .env
# Modifier les valeurs dans .env (mots de passe, clés API)
docker compose up -d
```

Services disponibles :
- API : http://localhost:8080
- Dashboard : http://localhost:3000
- Grafana : http://localhost:3001
- Prometheus : http://localhost:9090
- Hangfire : http://localhost:8080/hangfire

### Développement local

```bash
# Backend
dotnet restore
dotnet run --project src/Api/Api.csproj

# Dashboard
cd dashboard && npm install && npm run dev

# Mobile
cd mobile && npm install && npx expo start
```

## Tests

**473 tests** répartis sur 4 projets :

```bash
# Tous les tests
dotnet test tontApp.slnx

# Tests unitaires domaine uniquement
dotnet test tests/DomainUnitsTest/DomainUnitsTest.csproj

# Tests d'intégration (nécessite Docker)
dotnet test tests/PaymentIntegrationTests/PaymentIntegrationTests.csproj

# Avec couverture de code
dotnet test tontApp.slnx --collect:"XPlat Code Coverage"
```

### Répartition des tests

| Projet | Tests | Couverture |
|--------|-------|------------|
| DomainUnitsTest | 364 | Aggregates, Value Objects, règles métier |
| PaymentIntegrationTests | 55 | Flux paiement, HMAC, TestContainers PostgreSQL |
| NotificationTests | 39 | SMS adapter, templates, event handlers |
| AuthHandlerTests | 15 | Inscription, connexion, déconnexion, refresh |

### Stratégie

- **70% tests unitaires** (domaine DDD) — value objects, agrégats, invariants
- **20% tests d'intégration** (API + BDD) — TestContainers PostgreSQL réel
- **10% E2E** (mobile)

### Objectifs de couverture

- 80% sur le domaine (`src/Domain/`)
- **70% seuil minimum en CI** (fail si non atteint)
- 60% global

### Convention de nommage

`MethodName_Scenario_ExpectedResult`

```
Create_WithValidParameters_ReturnsTontineInDraftStatus
AddMember_WhenMaxReached_ThrowsInvalidOperationException
Confirmer_AlreadyConfirmed_ThrowsInvalidOperationException
```

### Outils

| Outil | Version | Usage |
|-------|---------|-------|
| xUnit | 2.9.3 | Framework de tests |
| FluentAssertions | 8.3.0 | Assertions lisibles |
| Moq | 4.20.72 | Mocking |
| Coverlet | 6.0.4 | Couverture de code |
| TestContainers.PostgreSql | 4.5.0 | BDD réelle pour intégration |

### Tests de charge

```bash
# k6 (ops/k6/)
k6 run ops/k6/load-test.js
```

Scénarios : 50 VU login, 20 VU paiements, 10 VU webhooks — cible p95 < 500ms.

## CI/CD

### Pipeline CI (`ci.yml`)

Déclenché sur push/PR vers `main` :

1. Build .NET 10
2. Exécution des 473 tests
3. Couverture Coverlet → ReportGenerator (HTML + Cobertura)
4. Validation seuil domaine ≥ 70% (`scripts/check-domain-coverage.py`)
5. Build image Docker (validation)
6. Upload artifact couverture

### Pipeline Deploy (`deploy.yml`)

Déclenché sur push vers `main` :

1. Build & tests
2. Build & push image Docker Hub
3. SCP `deploy.sh` + `docker-compose.yml` vers VM
4. Exécution SSH : pull image → `docker compose up` → health check
5. **Rollback automatique** si health check échoue

## Monitoring

### Métriques Prometheus

| Métrique | Type | Description |
|----------|------|-------------|
| `tontapp_paiements_total` | Counter | Paiements (par statut) |
| `tontapp_sms_total` | Counter | SMS envoyés (par statut) |
| `tontapp_http_requests_total` | Counter | Requêtes HTTP (method, endpoint, status) |
| `tontapp_http_request_duration_seconds` | Histogram | Latence HTTP |
| `tontapp_paiement_duration_seconds` | Histogram | Durée traitement paiement |
| `tontapp_tontines_actives` | Gauge | Tontines actives |
| `tontapp_membres_total` | Gauge | Membres inscrits |

### Alertes

| Alerte | Seuil | Sévérité |
|--------|-------|----------|
| HighErrorRate5xx | > 5 erreurs 5xx/min | CRITICAL |
| HighPaymentFailureRate | > 10% échecs paiements (15min) | CRITICAL |
| HighSmsFailureRate | > 10% échecs SMS (15min) | WARNING |
| HighApiLatencyP95 | p95 > 2s | WARNING |
| HighCpuUsage | CPU > 80% (5min) | WARNING |

### Backup

Backup PostgreSQL automatisé (`ops/backup/backup-postgresql.sh`) :
- Quotidien à 02:00 UTC → Azure Blob Storage
- Test de restauration hebdomadaire (dimanche 04:00 UTC)
- Rétention 30 jours

## Documentation

| Document | Description |
|----------|-------------|
| [`docs/database_design_notes.md`](docs/database_design_notes.md) | Notes de conception BDD (UUID, NUMERIC, TIMESTAMPTZ) |
| [`docs/tontApp_Domain.puml`](docs/tontApp_Domain.puml) | Diagramme PlantUML du domaine |
| [`docs/runbooks/incident-response.md`](docs/runbooks/incident-response.md) | Procédures incidents P1-P3 |
| [`docs/checklists/go-live.md`](docs/checklists/go-live.md) | Checklist go-live (66 points) |
| [`docs/checklists/securite-owasp-top10.md`](docs/checklists/securite-owasp-top10.md) | Audit sécurité OWASP Top 10 |
| [`docs/RECAPITULATIF.md`](docs/RECAPITULATIF.md) | Récapitulatif général complet du projet |

## Licence

Ce projet est distribué sous licence MIT — voir le fichier [LICENSE.txt](LICENSE.txt).
