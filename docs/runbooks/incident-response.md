# TontinesApp — Runbook de Réponse aux Incidents

> Ce document décrit les procédures de diagnostic et de résolution pour les 3 scénarios
> d'incident les plus critiques de TontinesApp.
>
> **Audience** : Équipe technique (DevOps, développeurs backend)
> **Dernière mise à jour** : Avril 2026

---

## Informations générales

### Contacts d'urgence

| Rôle | Contact | Méthode |
|------|---------|---------|
| Lead technique | ___________________ | Téléphone + SMS |
| DevOps | ___________________ | Téléphone + SMS |
| Africa's Talking Support | support@africastalking.com | Email + Portail |
| Hébergeur (Azure/VPS) | ___________________ | Portail support |

### Accès critiques

| Système | URL / Accès |
|---------|-------------|
| API Production | `https://api.tontinesapp.com` |
| Dashboard Grafana | `https://monitoring.tontinesapp.com:3001` |
| Prometheus | `https://monitoring.tontinesapp.com:9090` |
| Hangfire Dashboard | `https://api.tontinesapp.com/hangfire` |
| Serveur SSH | `ssh deploy@prod.tontinesapp.com` |
| Azure Portal (Backups) | portal.azure.com |
| Africa's Talking Dashboard | account.africastalking.com |

### Niveaux de sévérité

| Niveau | Description | Temps de réponse | Exemples |
|--------|-------------|-----------------|----------|
| **P1 — Critique** | Service complètement indisponible ou paiements bloqués | < 15 min | API down, tous les paiements échouent |
| **P2 — Majeur** | Fonctionnalité critique dégradée | < 1h | SMS en échec, latence > 5s |
| **P3 — Mineur** | Fonctionnalité non-critique dégradée | < 4h | Dashboard lent, job Hangfire en retard |

---

## Scénario 1 : Paiement Bloqué (P1)

### Symptômes
- Un ou plusieurs utilisateurs ne reçoivent pas la confirmation de paiement
- Le versement reste en statut `EN_ATTENTE` indéfiniment
- Le webhook Orange Money n'est pas reçu ou retourne une erreur
- Alertes Prometheus : `tontapp_paiements_total{statut="en_attente"}` en augmentation anormale

### Diagnostic

#### Étape 1 : Vérifier le statut du versement en base

```bash
# Se connecter à PostgreSQL
docker exec -it $(docker ps -q -f name=postgres) psql -U tontapp -d tontinesapp

-- Chercher les versements bloqués (EN_ATTENTE depuis > 30 minutes)
SELECT id, tontine_id, payeur_id, montant, statut, reference_externe,
       date_creation, date_modification
FROM versements
WHERE statut = 'EN_ATTENTE'
  AND date_creation < NOW() - INTERVAL '30 minutes'
ORDER BY date_creation DESC
LIMIT 20;
```

#### Étape 2 : Vérifier les logs du webhook

```bash
# Voir les logs récents du webhook controller
docker logs $(docker ps -q -f name=api) --since 1h 2>&1 | grep -i "webhook\|orange.money\|HMAC"

# Chercher les erreurs spécifiques
docker logs $(docker ps -q -f name=api) --since 1h 2>&1 | grep -E "ERROR|WARN|Invalid HMAC"
```

#### Étape 3 : Vérifier la connectivité Africa's Talking

```bash
# Test de connectivité vers Africa's Talking
curl -s -o /dev/null -w "%{http_code}" https://payments.africastalking.com/health

# Vérifier le statut Africa's Talking
# → https://status.africastalking.com
```

#### Étape 4 : Vérifier l'outbox processor

```bash
# Vérifier les messages outbox non traités
docker exec -it $(docker ps -q -f name=postgres) psql -U tontapp -d tontinesapp \
  -c "SELECT COUNT(*) as pending, MIN(created_at) as oldest
      FROM outbox_messages
      WHERE processed_at IS NULL;"

# Vérifier le job Hangfire outbox
docker logs $(docker ps -q -f name=hangfire-worker) --since 1h 2>&1 | grep -i "outbox"
```

### Résolution

#### Cas A : Webhook non reçu (Africa's Talking n'a pas rappelé)

```bash
# 1. Vérifier l'URL de callback dans le dashboard Africa's Talking
#    → Doit être : https://api.tontinesapp.com/api/v1/webhooks/orange-money

# 2. Vérifier que l'endpoint est accessible publiquement
curl -X POST https://api.tontinesapp.com/api/v1/webhooks/orange-money \
  -H "Content-Type: application/json" \
  -d '{"test": true}'
# Attendu : 401 (HMAC invalide) — prouve que l'endpoint est accessible

# 3. Si l'URL était incorrecte, la corriger dans Africa's Talking Dashboard
#    et attendre le prochain rappel automatique (Africa's Talking retry)
```

#### Cas B : Webhook reçu mais signature invalide

```bash
# Vérifier que le secret HMAC en production correspond à celui configuré
# dans Africa's Talking Dashboard
docker exec $(docker ps -q -f name=api) printenv | grep -i HMAC

# Si mismatch : mettre à jour la variable d'environnement et redémarrer
docker compose up -d api
```

#### Cas C : Versement bloqué — confirmation manuelle (dernier recours)

```bash
# ⚠️ UNIQUEMENT après vérification que le paiement est réellement reçu
# sur le compte Africa's Talking / Orange Money

# 1. Identifier le versement
docker exec -it $(docker ps -q -f name=postgres) psql -U tontapp -d tontinesapp \
  -c "SELECT id, montant, reference_externe FROM versements
      WHERE id = '<VERSEMENT_ID>';"

# 2. Confirmer manuellement via l'API (avec un token admin)
curl -X POST https://api.tontinesapp.com/api/v1/admin/versements/<VERSEMENT_ID>/confirm \
  -H "Authorization: Bearer <ADMIN_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"referenceExterne": "MANUAL_<TRANSACTION_ID>", "raison": "Confirmation manuelle après vérification"}'

# 3. Documenter l'incident dans le système d'audit
```

### Post-incident
- [ ] Identifier la cause racine
- [ ] Vérifier qu'aucun versement n'a été comptabilisé en double
- [ ] Notifier les utilisateurs affectés par SMS
- [ ] Mettre à jour les alertes si nécessaire

---

## Scénario 2 : API Down (P1)

### Symptômes
- L'application mobile affiche "Erreur de connexion"
- Le health check retourne une erreur ou timeout : `curl https://api.tontinesapp.com/health`
- Alertes Prometheus : `up{job="tontapp-api"} == 0`
- Dashboard Grafana montre 0 requêtes

### Diagnostic

#### Étape 1 : Vérifier l'état des containers

```bash
ssh deploy@prod.tontinesapp.com

# Voir l'état de tous les containers
docker compose ps

# Résultat attendu : tous les services "Up (healthy)"
# Chercher : "Exited", "Restarting", "Unhealthy"
```

#### Étape 2 : Vérifier les logs de l'API

```bash
# Dernières 100 lignes de logs
docker logs $(docker ps -aq -f name=api) --tail 100

# Chercher les erreurs critiques
docker logs $(docker ps -aq -f name=api) --since 30m 2>&1 | grep -E "FATAL|CRITICAL|Unhandled|OOM"
```

#### Étape 3 : Vérifier les dépendances

```bash
# PostgreSQL
docker exec $(docker ps -q -f name=postgres) pg_isready -U tontapp
# Attendu : "accepting connections"

# Redis
docker exec $(docker ps -q -f name=redis) redis-cli -a "${REDIS_PASSWORD}" ping
# Attendu : "PONG"

# Espace disque
df -h /
# Alerte si > 90% utilisé

# Mémoire
free -h
# Vérifier si OOM killer a tué un process
dmesg | tail -20 | grep -i "oom\|killed"
```

#### Étape 4 : Vérifier le reverse proxy

```bash
# Nginx
sudo systemctl status nginx
sudo nginx -t
sudo tail -50 /var/log/nginx/error.log

# Vérifier le certificat TLS
echo | openssl s_client -connect api.tontinesapp.com:443 -servername api.tontinesapp.com 2>/dev/null | openssl x509 -noout -dates
```

### Résolution

#### Cas A : Container API crashé

```bash
# 1. Redémarrer le container API
docker compose restart api

# 2. Vérifier qu'il démarre correctement
docker compose logs -f api  # (Ctrl+C après confirmation)

# 3. Vérifier le health check
sleep 45  # Attendre le start_period
curl -f http://localhost:8080/health
```

#### Cas B : PostgreSQL down

```bash
# 1. Vérifier les logs PostgreSQL
docker logs $(docker ps -aq -f name=postgres) --tail 50

# 2. Redémarrer PostgreSQL
docker compose restart postgres

# 3. Attendre que PostgreSQL soit prêt
docker compose exec postgres pg_isready -U tontapp

# 4. Redémarrer l'API (pour reconnecter les pools)
docker compose restart api hangfire-worker
```

#### Cas C : Espace disque plein

```bash
# 1. Identifier ce qui prend de la place
du -sh /var/lib/docker/volumes/*

# 2. Nettoyer les images Docker inutilisées
docker system prune -f

# 3. Nettoyer les logs Docker
truncate -s 0 /var/lib/docker/containers/*/*-json.log

# 4. Vérifier les backups locaux
ls -la /tmp/tontapp-backups/
rm -f /tmp/tontapp-backups/*.dump  # Les backups sont sur Azure

# 5. Redémarrer les services
docker compose up -d
```

#### Cas D : Certificat TLS expiré

```bash
# 1. Renouveler le certificat
sudo certbot renew --force-renewal

# 2. Recharger Nginx
sudo nginx -s reload
```

### Post-incident
- [ ] Vérifier que tous les services sont "Up (healthy)"
- [ ] Vérifier les métriques Grafana (requêtes reprennent)
- [ ] Analyser la cause racine
- [ ] Si down > 5 min : notifier les utilisateurs (SMS ou notification in-app)
- [ ] Mettre en place une alerte préventive si manquante

---

## Scénario 3 : SMS en Échec (P2)

### Symptômes
- Les utilisateurs ne reçoivent pas les SMS de rappel (J-3, J-1) ou de confirmation de paiement
- Alertes Prometheus : `tontapp_sms_total{statut="echec"}` en augmentation
- Table `notifications` avec beaucoup d'entrées en statut `ECHOUE` ou `EN_ATTENTE`

### Diagnostic

#### Étape 1 : Vérifier le taux d'échec SMS

```bash
docker exec -it $(docker ps -q -f name=postgres) psql -U tontapp -d tontinesapp

-- Taux d'échec SMS sur les dernières 24h
SELECT statut, COUNT(*) as total,
       ROUND(COUNT(*)::numeric / SUM(COUNT(*)) OVER () * 100, 1) as pourcentage
FROM notifications
WHERE date_creation > NOW() - INTERVAL '24 hours'
  AND type = 'SMS'
GROUP BY statut
ORDER BY total DESC;

-- Derniers SMS échoués avec détails
SELECT id, destinataire_id, type_notification, statut, details,
       date_creation, derniere_tentative, nombre_tentatives
FROM notifications
WHERE statut = 'ECHOUE'
  AND date_creation > NOW() - INTERVAL '24 hours'
ORDER BY date_creation DESC
LIMIT 20;
```

#### Étape 2 : Vérifier la connectivité Africa's Talking

```bash
# Test API Africa's Talking
curl -s -X GET https://api.africastalking.com/version1/messaging \
  -H "apiKey: ${AFRICASTALKING_API_KEY}" \
  -H "Accept: application/json"

# Vérifier le solde SMS
curl -s -X GET "https://api.africastalking.com/version1/user?username=${AFRICASTALKING_USERNAME}" \
  -H "apiKey: ${AFRICASTALKING_API_KEY}" \
  -H "Accept: application/json"
```

#### Étape 3 : Vérifier les logs de l'adaptateur SMS

```bash
# Logs spécifiques à l'envoi SMS
docker logs $(docker ps -q -f name=api) --since 6h 2>&1 | grep -i "sms\|africastalking\|notification"

# Chercher les erreurs de retry
docker logs $(docker ps -q -f name=hangfire-worker) --since 6h 2>&1 | grep -i "sms\|retry\|notification"
```

#### Étape 4 : Vérifier l'outbox processor

```bash
# Messages en attente dans l'outbox
docker exec -it $(docker ps -q -f name=postgres) psql -U tontapp -d tontinesapp \
  -c "SELECT type, COUNT(*) as pending, MIN(created_at) as oldest
      FROM outbox_messages
      WHERE processed_at IS NULL
      GROUP BY type;"
```

### Résolution

#### Cas A : Solde Africa's Talking épuisé

```bash
# 1. Vérifier le solde
# → Dashboard Africa's Talking : account.africastalking.com

# 2. Recharger le compte Africa's Talking
# → Via le portail de paiement Africa's Talking

# 3. Une fois rechargé, relancer les notifications échouées
docker exec -it $(docker ps -q -f name=postgres) psql -U tontapp -d tontinesapp \
  -c "UPDATE notifications
      SET statut = 'EN_ATTENTE', nombre_tentatives = 0
      WHERE statut = 'ECHOUE'
        AND date_creation > NOW() - INTERVAL '24 hours'
        AND type_notification = 'SMS';"

# L'outbox processor les reprendra automatiquement
```

#### Cas B : Clé API Africa's Talking invalide ou expirée

```bash
# 1. Vérifier la clé actuelle
docker exec $(docker ps -q -f name=api) printenv AFRICASTALKING_API_KEY

# 2. Générer une nouvelle clé dans le dashboard Africa's Talking
# → account.africastalking.com → Settings → API Key

# 3. Mettre à jour la variable d'environnement
# Éditer le fichier .env de production
nano /opt/tontapp/.env
# Mettre à jour AFRICASTALKING_API_KEY=nouvelle_cle

# 4. Redémarrer les services
docker compose up -d api hangfire-worker
```

#### Cas C : Numéros de téléphone invalides

```bash
# Identifier les numéros qui échouent systématiquement
docker exec -it $(docker ps -q -f name=postgres) psql -U tontapp -d tontinesapp \
  -c "SELECT u.telephone, COUNT(*) as echecs
      FROM notifications n
      JOIN utilisateurs u ON n.destinataire_id = u.id
      WHERE n.statut = 'ECHOUE'
        AND n.date_creation > NOW() - INTERVAL '7 days'
      GROUP BY u.telephone
      HAVING COUNT(*) > 3
      ORDER BY echecs DESC;"

# Si format invalide, corriger en E.164 :
# +225XXXXXXXXXX (Côte d'Ivoire), +226XXXXXXXX (Burkina Faso)
```

#### Cas D : Rate limiting atteint (10 SMS/membre/jour)

```bash
# Vérifier les membres ayant atteint la limite
docker exec -it $(docker ps -q -f name=postgres) psql -U tontapp -d tontinesapp \
  -c "SELECT destinataire_id, COUNT(*) as sms_today
      FROM notifications
      WHERE type = 'SMS'
        AND statut IN ('ENVOYE', 'EN_ATTENTE')
        AND date_creation > CURRENT_DATE
      GROUP BY destinataire_id
      HAVING COUNT(*) >= 10
      ORDER BY sms_today DESC;"

# Note : les SMS de type "ConfirmationPaiement" bypass le rate limit
# → Pas d'action nécessaire si seuls les rappels sont bloqués
```

### Post-incident
- [ ] Vérifier que le taux d'échec SMS revient à la normale (< 5%)
- [ ] Si des rappels J-3/J-1 n'ont pas été envoyés, envoyer un SMS groupé de rattrapage
- [ ] Vérifier le solde Africa's Talking et configurer une alerte de solde bas
- [ ] Documenter la cause racine

---

## Procédure commune de clôture d'incident

1. **Pendant l'incident** :
   - [ ] Noter l'heure de début
   - [ ] Identifier la sévérité (P1/P2/P3)
   - [ ] Appliquer le diagnostic et la résolution du scénario correspondant
   - [ ] Communiquer avec l'équipe (canal d'urgence)

2. **Après résolution** :
   - [ ] Noter l'heure de résolution
   - [ ] Vérifier que tous les services fonctionnent normalement
   - [ ] Vérifier les métriques Grafana pendant 30 minutes

3. **Post-mortem (dans les 48h)** :
   - [ ] Rédiger un résumé de l'incident (timeline, impact, cause racine)
   - [ ] Identifier les actions correctives
   - [ ] Mettre à jour ce runbook si nécessaire
   - [ ] Mettre à jour les alertes Prometheus si manquantes
   - [ ] Planifier les améliorations préventives

---

## Commandes de référence rapide

```bash
# État général
docker compose ps
docker compose logs --tail 50

# Santé de l'API
curl -f https://api.tontinesapp.com/health

# Métriques Prometheus
curl -s https://api.tontinesapp.com/metrics | grep tontapp_

# PostgreSQL
docker exec -it $(docker ps -q -f name=postgres) psql -U tontapp -d tontinesapp

# Redis
docker exec -it $(docker ps -q -f name=redis) redis-cli -a "${REDIS_PASSWORD}"

# Redémarrage complet (dernier recours)
docker compose down && docker compose up -d

# Rollback vers la version précédente
# 1. Identifier le tag précédent
docker images | grep tontapp-api
# 2. Modifier IMAGE_TAG dans .env
# 3. docker compose up -d api hangfire-worker
```
