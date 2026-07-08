# Maquettes Low Fidelity (Wireframes) — TontinesApp

## 1. Application Mobile (Membres)

### Écran : Connexion (LoginScreen)
```text
+-----------------------------------+
|                                   |
|            TontinesApp            |
|                                   |
|   [ Numéro de téléphone (E.164) ] |
|   [ Mot de passe                ] |
|                                   |
|        [ SE CONNECTER ]           |
|                                   |
|      Pas encore de compte ?       |
|          S'inscrire               |
|                                   |
+-----------------------------------+
```

### Écran : Accueil / Liste des Tontines (HomeScreen)
```text
+-----------------------------------+
| = TontinesApp         [ Profil ]  |
|-----------------------------------|
|                                   |
| + Rejoindre une tontine           |
| + Créer une tontine               |
|                                   |
| [ Tontine "Famille Diop" ]        |
|   Status: ACTIVE    [Paiement dû] |
|   Tour 3/10                       |
|   Prochaine échéance: 12 Nov      |
|                                   |
| [ Tontine "Collègues Bureau" ]    |
|   Status: DRAFT                   |
|   Membres 5/10                    |
|                                   |
+-----------------------------------+
```

### Écran : Détail de la Tontine (TontineDetailScreen)
```text
+-----------------------------------+
| < Famille Diop                    |
|-----------------------------------|
| Statut : Active                   |
| Périodicité : Mensuelle           |
| Montant : 50 000 FCFA             |
|                                   |
| --- Tour Actuel (3/10) ---        |
| Bénéficiaire : Awa Ndiaye         |
| Échéance dans : [ 3 jours ]       |
|                                   |
| --- Ma participation ---          |
| Statut : EN ATTENTE               |
|        [ PAYER 50 000 FCFA ]      |
|                                   |
| --- Membres (10) ---              |
| [✓] Jean D.  (Payé)               |
| [!] Moi      (En attente)         |
| [X] Paul M.  (En retard)          |
+-----------------------------------+
```

### Écran : Paiement Orange Money (PaiementScreen)
```text
+-----------------------------------+
| < Paiement                        |
|-----------------------------------|
| Tontine : Famille Diop            |
| Tour : 3 (Bénéficiaire Awa N.)    |
| Montant : 50 000 FCFA             |
|                                   |
| Paiement via Orange Money :       |
| [ Numéro Orange Money       ]     |
|                                   |
|     [ CONFIRMER LE PAIEMENT ]     |
|                                   |
| * Un prompt USSD apparaîtra sur   |
|   votre téléphone pour saisir     |
|   votre code secret.              |
+-----------------------------------+
```

---

## 2. Dashboard Web (Admin & Gestionnaire)

### Vue : Dashboard Gestionnaire (Vue globale)
```text
+-----------------------------------------------------------------------+
| TontinesApp |   [Tontines]   [Paiements]   [Membres]      [Déconnexion] |
|-------------+---------------------------------------------------------|
| [ Dashboard ] | Bienvenue, Gestionnaire                                 |
| [ Mes       ] |                                                         |
| [ Tontines  ] | --- Résumé de vos Tontines ---                          |
|               | [ Actives: 3 ]   [ Membres: 45 ]  [ Fonds: 2.5M FCFA ]  |
|               |                                                         |
|               | --- Alertes / Retards ---                               |
|               | 1. Tontine "Amis d'Enfance" - Tour 2                    |
|               |    - Paul M. en retard (2 jours)     [ Relancer SMS ]   |
|               |    - Sophie L. en retard (1 jour)    [ Relancer SMS ]   |
|               |                                                         |
|               | --- Tontines Récentes ---                               |
|               | [ Famille Diop (Active) ] [ Gérer ]                     |
|               | [ Collègues    (Draft)  ] [ Gérer ]                     |
+-----------------------------------------------------------------------+
```

### Vue : Gestion d'une Tontine (Clôture / Suivi)
```text
+-----------------------------------------------------------------------+
| TontinesApp |   [Tontines]   [Paiements]   [Membres]      [Déconnexion] |
|-------------+---------------------------------------------------------|
| < Retour    | Gestion : Tontine "Famille Diop"                          |
|             |                                                         |
|             | Statut : ACTIVE   |   Tour 3 / 10                       |
|             |                                                         |
|             | --- Progression du Tour 3 ---                           |
|             | [ Bénéficiaire: Awa Ndiaye ]  [ Collecté : 450k / 500k] |
|             | ProgressBar: [=========    ] 90%                        |
|             |                                                         |
|             | --- Membres ---                                         |
|             | 1. Jean D.  - 50 000 FCFA - Payé (10/11)  [ Audit ]     |
|             | 2. Marie S. - 50 000 FCFA - Payé (11/11)  [ Audit ]     |
|             | 3. Paul M.  - 0 FCFA      - En retard     [ Relancer]   |
|             |                                                         |
|             | [ CLÔTURER LE TOUR ] (Grisé si paiements incomplets)      |
+-----------------------------------------------------------------------+
```