# Spécifications Techniques – FinanceReport (MVP)

|Élément|Valeur|
|---|---|
|Version|1.3 – MVP|
|Date|23/09/2026|
|Documents sources|`docs/brd/BRD.md` v1.1, `docs/functional/FunctionalSpecifications.md` v1.2|

**Historique des versions**

|Version|Date|Modifications|
|---|---|---|
|1.0|23/09/2026|Version initiale|
|1.1|23/09/2026|Référentiels administrables : fichiers `zones.json`, `sectors.json`, `institutions.json`, initialisation par défaut, `ReferentialService`, `institutionId` sur les comptes, suppression des enums `Zone` et `Sector`.|
|1.2|23/09/2026|`institutionId` obligatoire sur un compte.|
|1.3|23/09/2026|Montée de version de la stack : .NET 10 LTS / C# 14 (au lieu de .NET 8, fin de support le 10/11/2026), Angular 22, tests unitaires front sous Vitest (outil par défaut d'Angular 22, à la place de Jasmine/Karma), FluentAssertions 7.x (licence Apache 2.0).|

---

## 1. Choix Technologiques et Architecture

### 1.1 Stack Technique

|Couche|Choix|
|---|---|
|Back-end|C# 14 / .NET 10 LTS, ASP.NET Core Web API (contrôleurs)|
|Front-end|Angular 22 (composants standalone, signals), TypeScript strict|
|UI|Angular Material (composants, tableaux, formulaires)|
|Graphiques|ngx-charts (licence MIT)|
|Persistance|Fichiers JSON locaux (`System.Text.Json`)|
|Authentification|JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`), BCrypt (`BCrypt.Net-Next`, work factor 12)|
|Validation|FluentValidation|
|Logs|Serilog (`Serilog.AspNetCore`, `Serilog.Sinks.File`)|
|Tests back|xUnit, FluentAssertions 7.x, `Microsoft.AspNetCore.Mvc.Testing`|
|Tests front|Vitest (unitaires), Playwright (E2E)|
|Build|dotnet CLI, Angular CLI, npm|

### 1.2 Architecture Générale

L'architecture suit une **Clean Architecture** en 4 projets .NET, avec un front Angular séparé.

```
[Angular SPA :4200] --HTTP/JSON + JWT--> [API ASP.NET Core :5080 (localhost)]
                                             |
                                   Application (services, cas d'usage)
                                             |
                                   Domain (entités, calculs purs)
                                             |
                                   Infrastructure (JSON, backups, auth, logs)
                                             |
                                   ./data/*.json   ./data/backups/   ./logs/
```

- **Couche présentation (`FinanceReport.Api`)** : les contrôleurs REST, le middleware d'erreurs (qui convertit les exceptions au format `{error, message, details}`), la configuration JWT, CORS et Serilog.
- **Couche application (`FinanceReport.Application`)** : les services de cas d'usage (`AccountService`, `MovementService`, `PriceService`, `DashboardService`, `AuthService`, `ReferentialService`), les DTO et validateurs, et l'orchestration « valider → persister → recalculer les snapshots ».
- **Couche domaine (`FinanceReport.Domain`)** : les entités, les enums, les interfaces de repositories et les calculateurs purs (`PositionCalculator`, `ValuationCalculator`), sans aucune dépendance technique.
- **Couche infrastructure (`FinanceReport.Infrastructure`)** : les repositories JSON, le `BackupService`, le `ReferentialSeeder`, le hachage BCrypt, la génération des JWT, le `LoginAttemptTracker` et `IClock`.

**Réseau** :

- Kestrel écoute uniquement sur `http://localhost:5080`.
- CORS autorise uniquement l'origine `http://localhost:4200`.
- En mode « production locale », l'application Angular compilée est servie en fichiers statiques par l'API (`wwwroot`), et CORS devient inutile.

---

## 2. Modèle de Données et Persistance

### 2.1 Organisation des fichiers

Le dossier racine est configurable (`appsettings.json` → `Storage:DataPath`, par défaut `./data`).

```
data/
├── credentials.json
├── accounts.json
├── balances.json
├── securities.json
├── movements.json
├── prices.json
├── snapshots.json
├── zones.json
├── sectors.json
├── institutions.json
└── backups/
    ├── accounts/accounts_20260923T072412345Z.json
    └── ...
```

Chaque fichier (hors `credentials.json`) a une enveloppe commune :

json

```json
{ "schemaVersion": 1, "items": [ ... ] }
```

### 2.2 Schémas des entités

**credentials.json**

json

```json
{ "username": "guillaume", "passwordHash": "$2a$12$...", "jwtSigningKey": "<base64 64 octets>",
  "createdAt": "2026-09-23T07:00:00Z" }
```

La clé de signature JWT est générée aléatoirement (64 octets, `RandomNumberGenerator`) à l'initialisation. Supprimer ce fichier invalide donc aussi tous les jetons existants.

**accounts.json – item**

json

```json
{ "id": "guid", "name": "PEA Boursorama", "type": "PEA", "institutionId": "guid",
  "archived": false, "createdAt": "...", "updatedAt": "..." }
```

`type` ∈ `COURANT | LIVRET | AUTRE | CTO | PEA | CRYPTO`. La catégorie est dérivée : `TITRES` pour CTO, PEA et CRYPTO, `ESPECES` pour les autres. `institutionId` est obligatoire et référence un item de `institutions.json`.

**balances.json – item** (clé unique : `accountId` + `date`)

json

```json
{ "accountId": "guid", "date": "2026-09-22", "amount": 1520.35, "updatedAt": "..." }
```

**securities.json – item**

json

```json
{ "id": "guid", "name": "Amundi MSCI World", "code": "LU1681043599", "type": "ETF",
  "zone": "MONDE", "sector": "DIVERSIFIE", "archived": false, "createdAt": "...", "updatedAt": "..." }
```

`zone` et `sector` référencent le **code** d'un item de `zones.json` et de `sectors.json`. Le code d'une valeur utilisée étant figé (RG-30), cette référence reste stable.

**zones.json / sectors.json / institutions.json – item**

json

```json
{ "id": "guid", "code": "MONDE", "label": "Monde", "archived": false, "createdAt": "...", "updatedAt": "..." }
```

`code` vaut `null` dans `institutions.json`.

**movements.json – item**

json

```json
{ "id": "guid", "type": "ACHAT", "date": "2026-09-15", "accountId": "guid", "securityId": "guid",
  "quantity": 10.5, "unitPrice": 102.40, "fees": 1.99, "amount": null,
  "sequence": 1542, "createdAt": "...", "updatedAt": "..." }
```

`sequence` est un entier croissant attribué à la création (max + 1). Il départage deux mouvements de même date (RG-10) et n'est jamais modifié.

**prices.json – item** (clé unique : `securityId` + `date`)

json

```json
{ "securityId": "guid", "date": "2026-09-22", "price": 104.12, "updatedAt": "..." }
```

**snapshots.json – item** (clé unique : `date`)

json

```json
{ "date": "2026-09-23", "totalNetWorth": 84210.55, "computedAt": "...",
  "accounts": [ { "accountId": "guid", "value": 42000.00, "cash": 120.00 } ],
  "positions": [ { "accountId": "guid", "securityId": "guid", "quantity": 10.5, "averageCost": 102.59,
                   "price": 104.12, "missingPrice": false, "marketValue": 1093.26, "unrealizedGain": 16.07 } ] }
```

### 2.3 Types et sérialisation

- Tous les nombres sont des `decimal` en C# ; aucun `double` n'est utilisé dans les calculs.
- Les dates métier sont des `DateOnly`, sérialisées `YYYY-MM-DD`. Les horodatages sont des `DateTimeOffset` en UTC.
- Les enums sont sérialisés en chaînes (`JsonStringEnumConverter`).
- Côté Angular, les nombres reçus sont des `number`. L'affichage passe par `Intl.NumberFormat('fr-FR')`. Aucun calcul financier n'est fait dans le front.

### 2.4 Référentiels administrables (RG-24, RG-30)

- **Stockage** : un fichier par référentiel (`zones.json`, `sectors.json`, `institutions.json`), avec l'entité commune `ReferentialItem` et l'enum `ReferentialKind` (`Zones`, `Sectors`, `Institutions`). Les enums `Zone` et `Sector` de la v1.0 sont supprimés.
- **Initialisation (`ReferentialSeeder`)** : au démarrage de l'API, pour chaque référentiel dont le fichier n'existe pas, le fichier est créé avec les valeurs initiales de la spécification fonctionnelle (section 3.11) : 5 zones, 13 secteurs, aucun établissement. Un fichier existant n'est jamais modifié, même vide. Aucune sauvegarde n'est prise, puisque le fichier n'existait pas.
- **Codes secteurs initiaux** : `ENERGIE`, `MATERIAUX`, `INDUSTRIE`, `CONSO_DISCRETIONNAIRE`, `CONSO_BASE`, `SANTE`, `FINANCE`, `TECHNOLOGIE`, `COMMUNICATION`, `SERVICES_PUBLICS`, `IMMOBILIER`, `DIVERSIFIE`, `NON_APPLICABLE`.
- **Utilisation (`usageCount`)** : calculée à la volée. Pour une zone ou un secteur, c'est le nombre de supports dont `zone` ou `sector` vaut le code ; pour un établissement, le nombre de comptes dont `institutionId` vaut l'id. Les supports et comptes archivés sont comptés.
- **`ReferentialService`** :
    - `Create` / `Update` : validation (section 3.7), unicité du libellé (insensible à la casse, `StringComparer.OrdinalIgnoreCase` après `Trim`) et du code dans le référentiel ; un changement de code d'une valeur dont `usageCount > 0` lève une `ConflictException` (409).
    - `Delete` : 409 avec `details.usageCount` si la valeur est utilisée, sinon suppression (204).
    - `Archive` / `Unarchive` : sans condition.
    - `EnsureSelectable(kind, reference, previousReference)` : appelé par `SecurityService` et `AccountService`. Si la référence est inchangée, rien n'est vérifié ; sinon, la valeur doit exister et ne pas être archivée, faute de quoi une `ValidationException` (400) est levée.
- **Snapshots** : les écritures dans un référentiel suivent la séquence d'écriture atomique (section 2.5) et la sauvegarde (section 2.6), mais ne déclenchent pas `SnapshotService.Rebuild`, car les snapshots ne stockent ni zone, ni secteur, ni établissement.

### 2.5 Accès concurrent et écriture atomique

`JsonFileStore<T>` (générique, un par fichier) est enregistré en singleton :

- **Lecture** : chargement en mémoire au premier accès, puis cache mémoire. Le fichier est la source de vérité au démarrage.
- **Écriture** : un verrou global `SemaphoreSlim(1,1)`, partagé par tous les stores via `IUnitOfWork`, sérialise chaque opération métier complète (validation + écriture de plusieurs fichiers + snapshots). La séquence est la suivante :
    1. `BackupService.Backup(fichier)` (RG-19) ;
    2. sérialisation vers `fichier.tmp` ;
    3. `File.Move(tmp, fichier, overwrite: true)` (remplacement atomique) ;
    4. mise à jour du cache.
- **Échec** : si une étape échoue, le cache est rechargé depuis le disque et l'erreur est propagée (500). Les fichiers déjà écrits dans l'opération sont restaurés depuis la sauvegarde prise à l'étape 1.

### 2.6 Sauvegardes (RG-19)

- **Emplacement** : `data/backups/{entité}/{entité}_{yyyyMMddTHHmmssfffZ}.json` (les référentiels utilisent `zones`, `sectors` et `institutions` comme nom d'entité).
- **Contenu** : une copie du fichier **avant** modification. Rien n'est copié si le fichier n'existe pas encore.
- **Purge** : après la copie, les fichiers du dossier sont triés par nom décroissant et seuls les 100 premiers sont conservés (`Storage:BackupRetention = 100`).
- `credentials.json` n'est pas sauvegardé, car il contient une clé secrète.

---

## 3. Conception Détaillée des Composants & Algorithmes

### 3.1 PositionCalculator (RG-03, RG-04, RG-05, RG-10)

C'est une fonction pure : `Compute(IEnumerable<Movement> movements, DateOnly asOf) → Dictionary<(AccountId, SecurityId), PositionState>`.

```
positions = {}
pour chaque m dans movements où m.type ∈ {ACHAT, VENTE} et m.date ≤ asOf,
   trié par (m.date ASC, m.sequence ASC) :
    p = positions[(m.accountId, m.securityId)] ?? {Q=0, PRU=0, realized=0}
    si m.type == ACHAT :
        p.PRU = (p.Q × p.PRU + m.quantity × m.unitPrice + m.fees) / (p.Q + m.quantity)
        p.Q   = p.Q + m.quantity
    si m.type == VENTE :
        si m.quantity > p.Q : lever InsufficientQuantityException(m.id, disponible = p.Q, m.date)
        p.realized += m.quantity × (m.unitPrice − p.PRU) − m.fees
        p.Q = p.Q − m.quantity
        si p.Q == 0 : p.PRU = 0          // la position s'éteint (RG-04), le cumul réalisé est conservé
retourner positions
```

- Aucun arrondi intermédiaire : la précision `decimal` est de 28 à 29 chiffres significatifs.
- L'exception porte l'id de la vente en conflit, pour la réponse 422.

### 3.2 Validation d'une écriture de mouvement (RG-10, RG-13)

Pour une création, une modification ou une suppression :

1. Construire la liste candidate, c'est-à-dire la liste actuelle avec la modification appliquée en mémoire.
2. Exécuter `PositionCalculator.Compute(listeCandidate filtrée sur les couples impactés, DateOnly.MaxValue)`. Une modification qui change de compte ou de support impacte l'ancien et le nouveau couple.
3. En cas d'exception, renvoyer 422 : aucun fichier n'est touché.
4. Sinon, persister (section 2.5), puis lancer `SnapshotService.Rebuild(dMin)`, où `dMin` est la plus petite date parmi l'ancienne et la nouvelle date du mouvement.

### 3.3 ValuationCalculator (RG-06 à RG-09, RG-11, RG-23, RG-27)

C'est une fonction pure : `Valuate(asOf, accounts, balances, securities, movements, prices) → Valuation`.

1. `positions = PositionCalculator.Compute(movements, asOf)`, en ne gardant que les couples où Q > 0 et le compte n'est pas archivé.
2. Pour chaque position, le cours est le dernier prix du support de date ≤ asOf (recherche dichotomique sur la liste triée par date). S'il n'y en a pas, `price = PRU` et `missingPrice = true`.
3. `marketValue = Q × price` et `unrealizedGain = Q × (price − PRU)`.
4. Pour chaque compte non archivé :
    - `cash` = dernier solde de date ≤ asOf, ou 0 ;
    - compte titres : `value = Σ marketValue + cash` ;
    - compte espèces : `value = cash`.
5. `totalNetWorth = Σ value`.

Index en mémoire : les prix et les soldes sont groupés par clé et triés par date au chargement, puis reconstruits après chaque écriture.

### 3.4 SnapshotService (RG-13, RG-14)

- `today = IClock.Today`, en fuseau `Europe/Paris` (`TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris")`).
- `Rebuild(DateOnly from)` :
    1. Pour chaque snapshot existant de date ≥ from : recalculer `ValuationCalculator.Valuate(date)` et le remplacer.
    2. Upsert du snapshot `today`.
    3. Une seule écriture de `snapshots.json`.
- Un changement de compte (création, archivage) ou de support (archivage, changement de zone) déclenche `Rebuild(today)`. Les champs zone, secteur et établissement ne sont pas historisés dans les snapshots : les restitutions lisent le référentiel courant. Une écriture dans un référentiel ne déclenche aucun `Rebuild`.
- **Performance** : 10 ans de snapshots représentent au plus 3 650 évaluations. Une valorisation incrémentale est inutile au MVP, car la cible est < 2 s pour 10 000 mouvements. `Rebuild` utilise un seul passage chronologique sur les mouvements (état cumulé) plutôt qu'un appel à `Compute` par date.

### 3.5 DashboardService (RG-15, RG-28, RG-29)

- **`summary`** :
    - `ref` = dernier snapshot de date < `new DateOnly(today.Year, today.Month, 1)` ;
    - `null` s'il n'existe pas, sinon `amount = total − ref.total` et `percent = amount / ref.total × 100` (`null` si `ref.total == 0`).
- **`history`** :
    - bornes : `1M` → `today − 30 j` ; `1A` → `today − 365 j` ; `ALL` → sans borne ;
    - snapshots triés par date ASC ;
    - le front affiche « Historique insuffisant » si moins de 2 points.
- **`positions`** : les filtres sont appliqués en ET entre critères et en OU à l'intérieur d'un critère. `includesCash = aucun filtre type/zone/secteur actif`. Si `includesCash` vaut vrai, les répartitions par compte incluent les comptes espèces et les liquidités, et les répartitions par type de support, zone et secteur ajoutent une part « Espèces et liquidités ». `zoneLabel` et `sectorLabel` sont résolus depuis les référentiels courants, archivés compris.
- **Arrondi (RG-29)** : `Math.Round(x, n, MidpointRounding.AwayFromZero)`, appliqué uniquement dans le mapping vers les DTO.

### 3.6 Authentification (RG-16, RG-17, RG-18, RG-20)

- **`AuthService.Setup`** : si `credentials.json` existe → 409. Sinon : validation, `BCrypt.HashPassword(pwd, 12)`, génération de la clé JWT, écriture du fichier.
- **`AuthService.Login`** :
    1. `LoginAttemptTracker.IsLocked()` → 423 avec `retryAfterSeconds`.
    2. Vérification de l'identifiant (comparaison ordinale) et de `BCrypt.Verify`.
    3. En cas d'échec : `RegisterFailure()`. Au 5e échec consécutif, `lockedUntil = now + 15 min` et le compteur est remis à 0.
    4. En cas de succès : `Reset()`, puis émission d'un JWT HS512 (`sub = username`, `exp = now + 8 h`, `iss/aud = "FinanceReport"`).
- **`LoginAttemptTracker`** : singleton en mémoire. Un redémarrage de l'API lève le blocage, ce qui est acceptable pour un usage local mono-utilisateur. Ce choix est documenté.
- Toutes les routes, sauf `auth/status`, `auth/setup` et `auth/login`, portent `[Authorize]` (politique par défaut `RequireAuthenticatedUser`).
- **Front** :
    - `AuthInterceptor` ajoute l'en-tête `Authorization` et redirige vers `/login` sur une réponse 401 ;
    - `authGuard` protège les routes ;
    - le jeton est stocké dans `sessionStorage` et effacé à la fermeture de l'onglet.

### 3.7 Validation des entrées (RG-02, RG-21, RG-24, RG-25, RG-26, RG-30)

Les validateurs FluentValidation contrôlent :

- **les décimales** : la fonction utilitaire `DecimalPlaces(decimal d)` renvoie `(d.Scale après normalisation)`, et la limite est de 2 ou 8 selon le type de support ;
- **les dates** : `date ≤ IClock.Today` ;
- **les enums** (types de compte, de support, de mouvement) : valeurs dans les listes fixes ;
- **les références obligatoires** : `zone` et `sector` d'un support, `institutionId` d'un compte (non nuls, non vides) ;
- **les référentiels** : libellé obligatoire après `Trim`, 1 à 60 caractères ; code obligatoire pour les zones et les secteurs, au format `^[A-Z0-9_]{2,40}$`, interdit (doit être `null`) pour les établissements ;
- **les champs interdits selon le type de mouvement** : ils doivent être `null`.

L'unicité et les contraintes relationnelles (compte titres, support actif, type de compte figé, valeur de référentiel existante et active via `ReferentialService.EnsureSelectable`) sont vérifiées dans les services.

### 3.8 Structure du Code (Arborescence)

```
FinanceReport/
├── back/
│   ├── FinanceReport.sln
│   ├── src/
│   │   ├── FinanceReport.Api/
│   │   │   ├── Controllers/        # Auth, Accounts, Balances, Securities, Movements, Prices, Dashboard, Referentials
│   │   │   ├── Middleware/         # ErrorHandlingMiddleware
│   │   │   ├── Program.cs          # DI, JWT, CORS, Serilog, Kestrel localhost, ReferentialSeeder au démarrage
│   │   │   └── appsettings.json
│   │   ├── FinanceReport.Application/
│   │   │   ├── Dtos/
│   │   │   ├── Validators/
│   │   │   ├── Services/           # AccountService, MovementService, PriceService, BalanceService,
│   │   │   │                       # SecurityService, SnapshotService, DashboardService, AuthService,
│   │   │   │                       # ReferentialService
│   │   │   └── Exceptions/         # ValidationException, ConflictException, InsufficientQuantityException...
│   │   ├── FinanceReport.Domain/
│   │   │   ├── Entities/           # Account, Balance, Security, Movement, Price, Snapshot, Credentials,
│   │   │   │                       # ReferentialItem
│   │   │   ├── Enums/              # AccountType, SecurityType, MovementType, ReferentialKind
│   │   │   ├── Calculators/        # PositionCalculator, ValuationCalculator
│   │   │   └── Abstractions/       # IRepository<T>, IUnitOfWork, IClock
│   │   └── FinanceReport.Infrastructure/
│   │       ├── Persistence/        # JsonFileStore<T>, repositories, UnitOfWork, ReferentialSeeder
│   │       ├── Backup/             # BackupService
│   │       ├── Security/           # PasswordHasher, JwtTokenService, LoginAttemptTracker
│   │       └── Time/               # ParisClock
│   └── tests/
│       ├── FinanceReport.Domain.Tests/
│       ├── FinanceReport.Application.Tests/
│       └── FinanceReport.Api.IntegrationTests/
└── front/
    └── src/app/
        ├── core/                   # auth (service, interceptor, guard), api clients, models
        ├── shared/                 # pipes (currency-fr, percent-fr), composants communs
        └── features/
            ├── auth/               # setup, login
            ├── home/               # tuiles KPI, anneau, courbe
            ├── dashboard/          # filtres, graphiques, tableaux
            ├── movements/
            ├── accounts/
            ├── securities/
            ├── prices/
            └── referentials/       # onglets zones, secteurs, établissements
```

---

## 4. Exploitation, Sécurité et Observabilité

### 4.1 Stratégie de Logging (Gestion des Logs)

- **Sink** : fichier `logs/financereport-YYYYMMDD.log`, avec `rollingInterval: Day`, `retainedFileCountLimit: 31` et `shared: false`. La console est active en développement.
- **Niveau** : `Information` par défaut ; `Warning` pour `Microsoft.AspNetCore`.
- **Format** : `[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}`.
- **Événements tracés** :
    - chaque requête HTTP (`UseSerilogRequestLogging` : méthode, chemin, statut, durée) ;
    - chaque écriture métier (entité, id, opération), référentiels compris ;
    - l'initialisation d'un référentiel par défaut au démarrage (référentiel, nombre de valeurs) ;
    - chaque reconstruction de snapshots (date de départ, nombre de snapshots, durée) ;
    - chaque échec de connexion et chaque blocage ;
    - chaque exception non gérée, avec sa stack trace (niveau `Error`).
- **RG-20** : les corps de requête ne sont jamais journalisés. Les DTO d'authentification ne sont pas loggés. Un filtre `Destructure` masque les propriétés nommées `password*`, `token` et `*Hash`.

### 4.2 Sécurité et Performance

- **Validation des entrées** : systématique via FluentValidation, et les ids sont des `Guid` validés par le routage. Le paramètre `{kind}` des référentiels est validé contre `ReferentialKind` (404 sinon). La taille des requêtes est limitée à 1 Mo. Les chemins de fichiers ne dérivent jamais d'une entrée utilisateur.
- **Surface réseau** : écoute sur `localhost` uniquement, avec CORS restreint (section 1.2). HTTPS n'est pas requis en local.
- **Secrets** : les mots de passe sont hachés avec BCrypt (work factor 12). La clé JWT est aléatoire et propre à l'installation.
- **Performance cible** : moins de 2 s pour `summary`, `history` et `positions` sur le volume cible (20 comptes, 200 supports, 10 000 mouvements, 3 650 snapshots). Les données sont en cache mémoire après le premier chargement. Un test de performance est défini dans le plan de test (TC-TECH-07).
- **Mot de passe oublié** : supprimer `data/credentials.json` puis relancer l'application. Les autres fichiers sont intacts.