# tontApp

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

## Structure du dépôt

```text
src/
  Api/
  Application/
  Domain/
  Infrastructure/
tests/
  DomainUnitsTest/
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

- `dotnet test`

## État du projet

Ce dépôt constitue une base initiale du projet. Le `README` pourra être enrichi ensuite avec :

- les cas d'usage métier
- les endpoints disponibles
- la configuration de l'infrastructure
- les conventions de contribution
