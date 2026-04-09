# TontinesApp — Checklist Sécurité OWASP Top 10

> Adaptée au contexte TontinesApp : application financière (tontines), paiements Orange Money,
> API REST .NET 10, PostgreSQL, JWT, SMS Africa's Talking.
>
> **Statut** : ☐ = à vérifier │ ☑ = validé │ ✗ = non applicable

---

## A01:2021 — Broken Access Control

| # | Point de vérification | Statut | Détails / Preuve |
|---|----------------------|--------|-----------------|
| 1 | **IDOR sur les tontines** — Vérifier qu'un utilisateur ne peut accéder qu'à ses propres tontines (`GET /api/v1/tontines/{id}`). Tester avec un ID de tontine d'un autre utilisateur → doit retourner 403 ou 404. | ☐ | |
| 2 | **IDOR sur les versements** — Vérifier qu'un membre ne peut consulter que ses propres versements et ceux de ses tontines. Tester accès croisé entre membres de tontines différentes. | ☐ | |
| 3 | **IDOR sur les profils crédit** — Vérifier qu'un utilisateur ne peut lire que son propre profil crédit (`GET /api/v1/credit/mon-profil`). | ☐ | |
| 4 | **Élévation de privilèges** — Vérifier qu'un membre ne peut pas effectuer des actions gestionnaire (activer tontine, clôturer tour, ajouter membre). | ☐ | |
| 5 | **Accès admin Hangfire** — Le dashboard `/hangfire` est protégé par `HangfireDashboardAuthFilter` et accessible uniquement aux administrateurs. | ☐ | |

## A02:2021 — Cryptographic Failures

| # | Point de vérification | Statut | Détails / Preuve |
|---|----------------------|--------|-----------------|
| 6 | **JWT Secret Key** — La clé JWT fait au minimum 256 bits (32 caractères), stockée en variable d'environnement, jamais dans le code source. Vérifier `.env.example` ne contient pas la vraie clé. | ☐ | |
| 7 | **Mots de passe hashés** — Les mots de passe sont hashés avec bcrypt/Argon2 (jamais en clair ou MD5/SHA-1). Vérifier la table `utilisateurs.mot_de_passe_hash`. | ☐ | |
| 8 | **HTTPS obligatoire** — Tout le trafic en production utilise TLS 1.2+. Vérifier la configuration Nginx/reverse proxy. Redirect HTTP → HTTPS. | ☐ | |
| 9 | **Codes d'invitation hashés** — Les codes d'invitation sont stockés en SHA-256 dans `codes_invitation.code_hash`, jamais en clair. | ☐ | |

## A03:2021 — Injection

| # | Point de vérification | Statut | Détails / Preuve |
|---|----------------------|--------|-----------------|
| 10 | **SQL Injection** — EF Core utilise des requêtes paramétrées. Vérifier qu'il n'y a aucun `FromSqlRaw` ou `ExecuteSqlRaw` avec interpolation directe de chaînes utilisateur. Scanner tout le code Infrastructure. | ☐ | |
| 11 | **NoSQL / JSONB Injection** — Les champs JSONB (`payload`, `details`, `contenu`) sont sérialisés via `System.Text.Json`, jamais par concaténation de chaînes. | ☐ | |
| 12 | **Command Injection** — Aucune exécution de commandes système avec données utilisateur (`Process.Start`, `Runtime.exec`). | ☐ | |

## A04:2021 — Insecure Design

| # | Point de vérification | Statut | Détails / Preuve |
|---|----------------------|--------|-----------------|
| 13 | **Rate Limiting** — L'API dispose d'un rate limiter sur les endpoints critiques : login (max 5 tentatives/min/IP), webhook (configurable), SMS (10/membre/jour). | ☐ | |
| 14 | **Validation des montants** — `Montant` value object rejette les montants < 100 FCFA. Vérifier côté API et domaine. Tester avec montants négatifs, 0, 99, et décimaux excessifs. | ☐ | |
| 15 | **Idempotence webhook** — Le traitement du webhook Orange Money est idempotent : un second appel avec le même `transactionId` ne crée pas de doublon. | ☐ | |

## A05:2021 — Security Misconfiguration

| # | Point de vérification | Statut | Détails / Preuve |
|---|----------------------|--------|-----------------|
| 16 | **Headers de sécurité** — Vérifier la présence de : `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Strict-Transport-Security`, `Content-Security-Policy`. | ☐ | |
| 17 | **CORS restrictif** — CORS autorise uniquement les origines du dashboard (`localhost:3000` en dev, domaine prod). Pas de `Access-Control-Allow-Origin: *` en production. | ☐ | |
| 18 | **Secrets en production** — Aucun secret par défaut (`CHANGE_ME_*`) dans les variables d'environnement. Tous les secrets rotés avant go-live. | ☐ | |

## A07:2021 — Identification and Authentication Failures

| # | Point de vérification | Statut | Détails / Preuve |
|---|----------------------|--------|-----------------|
| 19 | **Refresh Token Rotation** — À chaque rafraîchissement, l'ancien refresh token est invalidé. Vérifier que la réutilisation d'un ancien token est rejetée (401). | ☐ | |
| 20 | **Verrouillage de compte** — Après N tentatives échouées (ex: 5), le compte est temporairement verrouillé. Vérifier le mécanisme anti-brute-force. | ☐ | |

## A08:2021 — Software and Data Integrity Failures

| # | Point de vérification | Statut | Détails / Preuve |
|---|----------------------|--------|-----------------|
| 21 | **Intégrité chaîne d'audit** — Le job `VerifierChaineAuditJob` valide quotidiennement l'intégrité de la chaîne SHA-256 des `audit_entries`. Aucune alerte CRITICAL en staging. | ☐ | |
| 22 | **Signature HMAC webhook** — Le webhook Orange Money valide le header `X-AfricasTalking-Signature` avec `CryptographicOperations.FixedTimeEquals` (comparaison à temps constant). | ☐ | |

## A09:2021 — Security Logging and Monitoring Failures

| # | Point de vérification | Statut | Détails / Preuve |
|---|----------------------|--------|-----------------|
| 23 | **Logging des échecs d'authentification** — Chaque tentative de login échouée est loguée avec l'IP et le numéro de téléphone (sans le mot de passe). | ☐ | |
| 24 | **Alertes Prometheus** — Les alertes critiques sont configurées : taux d'erreur HTTP > 5%, latence p95 > 2s, échecs SMS > 10%, espace disque < 20%. | ☐ | |
| 25 | **Pas de données sensibles dans les logs** — Vérifier que les mots de passe, tokens JWT, clés API, et numéros complets de téléphone ne sont jamais loggés. | ☐ | |

## A10:2021 — Server-Side Request Forgery (SSRF)

| # | Point de vérification | Statut | Détails / Preuve |
|---|----------------------|--------|-----------------|
| 26 | **Pas de SSRF via webhook** — L'endpoint webhook ne fait aucun appel HTTP basé sur des données du payload (pas de callback URL configurable par l'appelant). | ☐ | |

---

## Résumé

| Catégorie OWASP | Points | Statut global |
|-----------------|--------|---------------|
| A01 — Broken Access Control | 5 | ☐ |
| A02 — Cryptographic Failures | 4 | ☐ |
| A03 — Injection | 3 | ☐ |
| A04 — Insecure Design | 3 | ☐ |
| A05 — Security Misconfiguration | 3 | ☐ |
| A07 — Authentication Failures | 2 | ☐ |
| A08 — Integrity Failures | 2 | ☐ |
| A09 — Logging & Monitoring | 3 | ☐ |
| A10 — SSRF | 1 | ☐ |
| **Total** | **26** | |

## Comment utiliser cette checklist

1. **Avant chaque release** : Parcourir tous les points et mettre à jour le statut
2. **Automatisation** : Les points 10, 11, 12 peuvent être vérifiés par analyse statique (CodeQL, SonarQube)
3. **Tests de pénétration** : Les points 1-5, 13, 19-20 nécessitent des tests manuels ou automatisés (OWASP ZAP)
4. **Revue de code** : Les points 6-9, 16-18, 25 sont vérifiés lors de la revue de code et CI/CD

> **Responsable sécurité** : _________________________
> **Date de dernière vérification** : _________________________
> **Prochaine vérification prévue** : _________________________
