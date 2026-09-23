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

## Lancement (production locale)

```bash
scripts/run.sh
```

Le script compile le front, le copie dans l'API, puis démarre l'application sur `http://localhost:5080`. Les données sont écrites dans `data/` et les journaux dans `logs/`, à la racine du dépôt. Pour relancer sans recompiler le front : `scripts/run.sh --skip-build`.

## Lancement en développement

```bash
dotnet run --project back/src/FinanceReport.Api
```

L'API écoute sur `http://localhost:5080`.

```bash
cd front && npm install && npm start
```

Le front est servi sur `http://localhost:4200` et transmet les appels `/api` à l'API (`front/proxy.conf.json`). Si le port 4200 est déjà pris : `npm start -- --port 4300`.

## Tests

```bash
dotnet test back/FinanceReport.sln
```

```bash
cd front && npm test
```

Tests de bout en bout (Playwright avec le Chrome installé, application compilée et démarrée sur un dossier de données vierge, port 5099) :

```bash
cd front && npm run e2e
```

Les résultats de la recette et la matrice de traçabilité sont dans le [plan de test](docs/tests/TestPlan.md).

## Données

Les fichiers de données sont dans `data/` (configurable via `Storage:DataPath`), les sauvegardes dans `data/backups/`, les journaux dans `logs/`. Aucun de ces dossiers n'est versionné.

**Mot de passe oublié** : supprimer `data/credentials.json` puis relancer l'application. Les autres fichiers sont intacts.
