# tontApp

![CI](https://github.com/ezekielncm/tontApp/actions/workflows/ci.yml/badge.svg)

Application .NET 10 autour de la gestion de tontines, avec une organisation en couches orientée domaine.

## Aperçu

Le projet est structuré pour séparer les responsabilités entre :

- `Api` : point d'entrée HTTP de l'application
- `Application` : logique applicative
- `Domain` : règles métier, entités, événements et objets de valeur
- `Infrastructure` : intégrations techniques
- `tests` : projets de tests

Le domaine métier couvre notamment :

- la gestion des tontines
- la gestion des membres et invitations
- les versements
- les notifications
- les abonnements
- les utilisateurs

## Prérequis

- SDK .NET 10
- Un environnement de développement compatible avec .NET 10
- Docker (pour les tests d'intégration TestContainers)

## Structure du dépôt

```text
src/
  Api/
  Application/
  Domain/
  Infrastructure/
tests/
  DomainUnitsTest/          # Tests unitaires domaine (xUnit + FluentAssertions)
  PaymentIntegrationTests/  # Tests d'intégration (TestContainers + PostgreSQL)
  AuthHandlerTests/         # Tests des handlers d'authentification
  NotificationTests/        # Tests du module notification
```

## Démarrage rapide

Depuis la racine du dépôt :

1. Restaurer les dépendances
2. Lancer l'API

Commandes :

- `dotnet restore`
- `dotnet run --project src/Api/Api.csproj`

## API

L'API ASP.NET Core expose des contrôleurs HTTP.

En environnement de développement, la description OpenAPI est activée.

## Tests

Pour exécuter les tests :

```bash
# Tous les tests
dotnet test tontApp.slnx

# Tests unitaires uniquement
dotnet test tests/DomainUnitsTest/DomainUnitsTest.csproj

# Tests d'intégration (nécessite Docker)
dotnet test tests/PaymentIntegrationTests/PaymentIntegrationTests.csproj

# Tests avec couverture de code (Coverlet)
dotnet test tontApp.slnx --collect:"XPlat Code Coverage"
```

### Stratégie de tests

Stratégie pyramidale :
- **70% tests unitaires** (domaine DDD) – couverture du domaine, value objects, agrégats
- **20% tests d'intégration** (API + BDD) – TestContainers avec PostgreSQL réel
- **10% E2E** (mobile)

**Objectifs de couverture :**
- 80% sur le domaine (`src/Domain/`)
- 70% seuil minimum en CI (fail si non atteint)
- 60% global

### Conventions de nommage des tests

Tous les tests suivent la convention : **`MethodName_Scenario_ExpectedResult`**

Exemples :
- `Create_WithValidParameters_ReturnsTontineInDraftStatus`
- `AddMember_WhenMaxReached_ThrowsInvalidOperationException`
- `Activate_WithExactlyThreeMembers_Succeeds`
- `Confirmer_AlreadyConfirmed_ThrowsInvalidOperationException`
- `ScoreCalcule_FiveCycles_FullPonctualite_ClampedAt100`

### Contraintes techniques

- **Pas de `DateTime.Now`** dans les tests – utiliser `DateTime.UtcNow` ou injecter `IClock`
- **TestContainers uniquement** pour les tests d'intégration (pas les tests unitaires)
- Tests unitaires : < 100ms par test
- Tests d'intégration : < 5s par test
- **FluentAssertions** pour les assertions lisibles
- **Moq** pour le mocking
- **Coverlet** pour la couverture de code

### Outils de test

| Outil | Usage |
|-------|-------|
| xUnit 2.9.3 | Framework de tests |
| FluentAssertions 8.3.0 | Assertions lisibles |
| Moq 4.20.72 | Mocking |
| Coverlet 6.0.4 | Couverture de code |
| TestContainers.PostgreSql 4.5.0 | BDD réelle pour intégration |

## Couverture de code

La couverture est mesurée automatiquement dans le pipeline CI via Coverlet.

Le rapport HTML est disponible comme artifact dans les GitHub Actions.

## État du projet

Ce dépôt constitue une base initiale du projet. Le `README` pourra être enrichi ensuite avec :

- les cas d'usage métier
- les endpoints disponibles
- la configuration de l'infrastructure
- les conventions de contribution
