# FinanceReport

Application web personnelle, exécutée en local, de suivi de patrimoine et de placements (PRU, plus-values, allocation). Saisie manuelle, persistance en fichiers JSON locaux.

## Documentation

| Document | Chemin |
|---|---|
| BRD | [docs/brd/brd.md](docs/brd/brd.md) |
| Spécifications fonctionnelles | [docs/functional/FunctionalSpecifications.md](docs/functional/FunctionalSpecifications.md) |
| Spécifications techniques | [docs/technical/TechnicalSpecifications.md](docs/technical/TechnicalSpecifications.md) |
| Plan de test | [docs/tests/TestPlan.md](docs/tests/TestPlan.md) |

## Prérequis

- SDK .NET 10
- Node.js 24 et npm

## Lancement en développement

```bash
dotnet run --project back/src/FinanceReport.Api
```

L'API écoute sur `http://localhost:5080`.

```bash
cd front && npm install && npm start
```

Le front est servi sur `http://localhost:4200`.

## Tests

```bash
dotnet test back/FinanceReport.sln
```

```bash
cd front && npm test
```

## Données

Les fichiers de données sont dans `data/` (configurable via `Storage:DataPath`), les sauvegardes dans `data/backups/`, les journaux dans `logs/`. Aucun de ces dossiers n'est versionné.

**Mot de passe oublié** : supprimer `data/credentials.json` puis relancer l'application. Les autres fichiers sont intacts.
