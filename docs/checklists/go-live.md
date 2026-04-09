# TontinesApp — Checklist Go-Live

> Checklist complète avant la mise en production. Chaque point doit être vérifié et validé.
>
> **Légende** : ☐ = à faire │ ☑ = validé │ N/A = non applicable
>
> **Date cible go-live** : ___________________
> **Responsable go-live** : ___________________

---

## 1. Infrastructure (12 points)

| # | Point | Statut | Responsable | Date validée |
|---|-------|--------|-------------|-------------|
| 1.1 | Serveur de production provisionné (Azure/VPS) avec spécifications minimales : 4 vCPU, 8 Go RAM, 100 Go SSD | ☐ | | |
| 1.2 | PostgreSQL 16 en production avec réplication activée (primary + standby) | ☐ | | |
| 1.3 | Redis 7 en production avec mot de passe fort et persistence AOF | ☐ | | |
| 1.4 | Docker et Docker Compose installés en production, images buildées et testées | ☐ | | |
| 1.5 | Reverse proxy (Nginx/Caddy) configuré avec TLS 1.2+ (Let's Encrypt ou certificat commercial) | ☐ | | |
| 1.6 | DNS configuré : `api.tontinesapp.com`, `dashboard.tontinesapp.com` pointant vers le serveur | ☐ | | |
| 1.7 | Firewall configuré : seuls ports 80, 443 ouverts publiquement. PostgreSQL (5432) et Redis (6379) uniquement en interne | ☐ | | |
| 1.8 | Sauvegarde PostgreSQL automatique configurée (pg_dump quotidien → Azure Blob Storage) | ☐ | | |
| 1.9 | Test de restauration de sauvegarde réussi sur environnement isolé | ☐ | | |
| 1.10 | Volume Docker persistent pour PostgreSQL et Redis (pas de données perdues au redémarrage) | ☐ | | |
| 1.11 | Health checks configurés et fonctionnels (`/health` retourne 200) | ☐ | | |
| 1.12 | Auto-restart des containers configuré (`restart: unless-stopped`) | ☐ | | |

## 2. Sécurité (10 points)

| # | Point | Statut | Responsable | Date validée |
|---|-------|--------|-------------|-------------|
| 2.1 | Tous les secrets rotés avec de nouvelles valeurs de production (JWT, Redis, PostgreSQL, API keys) | ☐ | | |
| 2.2 | Clés API Africa's Talking de production configurées (pas les clés sandbox) | ☐ | | |
| 2.3 | Clé HMAC webhook Orange Money de production configurée | ☐ | | |
| 2.4 | HTTPS obligatoire — redirection HTTP → HTTPS | ☐ | | |
| 2.5 | Headers de sécurité configurés (HSTS, X-Frame-Options, CSP, X-Content-Type-Options) | ☐ | | |
| 2.6 | CORS restreint aux domaines de production uniquement | ☐ | | |
| 2.7 | Dashboard Hangfire protégé par authentification admin | ☐ | | |
| 2.8 | Rate limiting activé sur les endpoints d'authentification (anti-brute-force) | ☐ | | |
| 2.9 | Scan OWASP Top 10 complété (checklist `securite-owasp-top10.md` validée) | ☐ | | |
| 2.10 | Aucun secret par défaut (`CHANGE_ME_*`) dans les fichiers `.env` de production | ☐ | | |

## 3. Paiements Orange Money (8 points)

| # | Point | Statut | Responsable | Date validée |
|---|-------|--------|-------------|-------------|
| 3.1 | Compte Africa's Talking de production activé et vérifié | ☐ | | |
| 3.2 | Endpoint webhook Orange Money accessible publiquement (`/api/v1/webhooks/orange-money`) | ☐ | | |
| 3.3 | URL de callback webhook configurée dans le dashboard Africa's Talking | ☐ | | |
| 3.4 | Test de paiement de bout en bout réussi en sandbox (initier → webhook → confirmation) | ☐ | | |
| 3.5 | Test de paiement échoué vérifié (rejet, timeout, annulation) | ☐ | | |
| 3.6 | Idempotence du webhook vérifiée (même webhook envoyé 2x → pas de doublon) | ☐ | | |
| 3.7 | Validation HMAC du webhook fonctionnelle en production | ☐ | | |
| 3.8 | Montant minimum (100 FCFA) et validation des devises (XOF) fonctionnels | ☐ | | |

## 4. SMS / Notifications (6 points)

| # | Point | Statut | Responsable | Date validée |
|---|-------|--------|-------------|-------------|
| 4.1 | Sender ID / Short code approuvé par l'opérateur pour le Burkina Faso / Côte d'Ivoire | ☐ | | |
| 4.2 | SMS de rappel J-3 et J-1 testés et reçus correctement | ☐ | | |
| 4.3 | SMS de confirmation de paiement testé | ☐ | | |
| 4.4 | Rate limiting SMS fonctionnel (10 SMS/membre/jour, bypass pour confirmation paiement) | ☐ | | |
| 4.5 | Retry SMS configuré (3 tentatives avec backoff 5/15/60 min) | ☐ | | |
| 4.6 | Format E.164 des numéros de téléphone validé en entrée | ☐ | | |

## 5. Monitoring & Alertes (7 points)

| # | Point | Statut | Responsable | Date validée |
|---|-------|--------|-------------|-------------|
| 5.1 | Prometheus configuré et collecte les métriques (`/metrics` accessible) | ☐ | | |
| 5.2 | Dashboard Grafana importé et fonctionnel (tontapp-overview) | ☐ | | |
| 5.3 | Alertes Prometheus configurées : taux d'erreur HTTP > 5%, latence p95 > 2s | ☐ | | |
| 5.4 | Alerte espace disque < 20% configurée | ☐ | | |
| 5.5 | Alerte échec SMS > 10% configurée | ☐ | | |
| 5.6 | Job `VerifierChaineAuditJob` vérifié (02:00 UTC quotidien, alerte CRITICAL si chaîne compromise) | ☐ | | |
| 5.7 | Logs centralisés et accessibles (stdout/stderr des containers → rotation configurée) | ☐ | | |

## 6. Application & Données (8 points)

| # | Point | Statut | Responsable | Date validée |
|---|-------|--------|-------------|-------------|
| 6.1 | Migrations EF Core appliquées sur la base de production | ☐ | | |
| 6.2 | Données de seed de production insérées (plans d'abonnement : Gratuit, Pro, IMF) | ☐ | | |
| 6.3 | Tests de charge (k6) exécutés avec succès (p95 < 500ms sur tous les endpoints critiques) | ☐ | | |
| 6.4 | Tests unitaires et d'intégration passent à 100% (`dotnet test tontApp.slnx`) | ☐ | | |
| 6.5 | Variables d'environnement de production configurées (voir `.env.example`) | ☐ | | |
| 6.6 | `ASPNETCORE_ENVIRONMENT=Production` configuré | ☐ | | |
| 6.7 | Outbox processor vérifié (messages traités en < 60s) | ☐ | | |
| 6.8 | Jobs Hangfire récurrents vérifiés : RappelJ3, RappelJ1, RecapHebdo, VerifierChaineAudit | ☐ | | |

## 7. Dashboard Next.js (4 points)

| # | Point | Statut | Responsable | Date validée |
|---|-------|--------|-------------|-------------|
| 7.1 | Build de production réussi (`cd dashboard && npm run build`) | ☐ | | |
| 7.2 | Authentification JWT fonctionnelle (Gestionnaire et Admin SaaS) | ☐ | | |
| 7.3 | Variables d'environnement de production configurées (`API_URL`, `JWT_SECRET`, etc.) | ☐ | | |
| 7.4 | Dashboard accessible via le domaine de production avec HTTPS | ☐ | | |

## 8. Mobile App (3 points)

| # | Point | Statut | Responsable | Date validée |
|---|-------|--------|-------------|-------------|
| 8.1 | URL de l'API de production configurée dans l'app mobile | ☐ | | |
| 8.2 | Build de production testé (Expo EAS Build ou équivalent) | ☐ | | |
| 8.3 | Deep links fonctionnels (invitation par code) | ☐ | | |

## 9. Légal & Conformité (5 points)

| # | Point | Statut | Responsable | Date validée |
|---|-------|--------|-------------|-------------|
| 9.1 | **CGU (Conditions Générales d'Utilisation)** rédigées, mentionnant explicitement : aucune garantie sur les micro-prêts (hors scope MVP) | ☐ | | |
| 9.2 | **Politique de confidentialité** rédigée avec mentions RGPD minimales : données collectées, finalité, durée de conservation, droits des utilisateurs (accès, rectification, suppression) | ☐ | | |
| 9.3 | Consentement utilisateur recueilli lors de l'inscription (acceptation CGU + politique de confidentialité) | ☐ | | |
| 9.4 | Mention légale visible dans l'application (raison sociale, adresse, contact) | ☐ | | |
| 9.5 | Registre des traitements de données personnelles documenté (nom, téléphone, historique de paiements, score crédit) | ☐ | | |

## 10. Procédures opérationnelles (3 points)

| # | Point | Statut | Responsable | Date validée |
|---|-------|--------|-------------|-------------|
| 10.1 | Runbook de réponse aux incidents documenté (`docs/runbooks/incident-response.md`) | ☐ | | |
| 10.2 | Procédure de rollback documentée (retour à la version précédente en < 15 min) | ☐ | | |
| 10.3 | Liste des contacts d'urgence (équipe technique, Africa's Talking support, hébergeur) | ☐ | | |

---

## Résumé

| Catégorie | Points | Validés | Restants |
|-----------|--------|---------|----------|
| 1. Infrastructure | 12 | | |
| 2. Sécurité | 10 | | |
| 3. Paiements | 8 | | |
| 4. SMS / Notifications | 6 | | |
| 5. Monitoring | 7 | | |
| 6. Application & Données | 8 | | |
| 7. Dashboard | 4 | | |
| 8. Mobile App | 3 | | |
| 9. Légal & Conformité | 5 | | |
| 10. Procédures | 3 | | |
| **Total** | **66** | | |

## Validation finale

| Critère | Statut |
|---------|--------|
| Tous les points ci-dessus sont ☑ | ☐ |
| Approbation du responsable technique | ☐ |
| Approbation du responsable produit | ☐ |
| Date/heure de mise en production planifiée | ☐ |
| Communication aux utilisateurs bêta | ☐ |

> **Go / No-Go décision** : ☐ GO │ ☐ NO-GO
>
> **Signataire** : _________________________ **Date** : _________________________
