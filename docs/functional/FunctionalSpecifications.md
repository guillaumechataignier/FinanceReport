# Spécifications Fonctionnelles – FinanceReport (MVP)

|Élément|Valeur|
|---|---|
|Version|1.3 – MVP|
|Date|23/09/2026|
|Document source|`docs/brd/BRD.md` v1.1|
|Maquettes|Canevas « FinanceReport – Maquettes MVP » (écrans nominaux)|

**Historique des versions**

|Version|Date|Modifications|
|---|---|---|
|1.0|23/09/2026|Version initiale|
|1.1|23/09/2026|Référentiels administrables (zones, secteurs, établissements) : UC-14, RG-24 révisée, RG-30, section 3.11, API des référentiels, écran « Référentiels ». Établissement du compte choisi dans un référentiel. Section UI alignée sur les maquettes.|
|1.2|23/09/2026|Établissement obligatoire sur un compte (UC-03, section 3.2, API, RG-24).|
|1.3|23/09/2026|Alignement API ↔ maquettes : valeur actuelle et dernier solde dans la réponse des comptes, dernier cours dans la réponse des supports, indicateurs « archivé » des valeurs de référentiel portées, `GET` unitaire des comptes, des supports et des mouvements, montant `total` et libellés dans la réponse des mouvements, suppression d'un solde depuis l'écran Comptes.|

---

## 1. Introduction et Contexte Fonctionnel

### 1.1 Contexte Général

FinanceReport est une application web personnelle, exécutée en local. Elle consolide les comptes et les investissements de son unique utilisateur, pour lui permettre de :

- suivre l'évolution de son patrimoine net ;
- mesurer la performance de ses placements (PRU, plus-values) ;
- visualiser son allocation.

Le MVP repose sur une saisie entièrement manuelle : soldes, mouvements et cours. Les données sont stockées dans des fichiers JSON locaux.

### 1.2 Acteurs et Rôles

- **Utilisateur** : le propriétaire des données, seul acteur humain. Il crée son compte d'accès, saisit ses données, administre ses référentiels et consulte les restitutions.
- **Front Angular** : le client de l'API. Il porte l'interface utilisateur et appelle l'API REST.
- **API .NET** : elle porte les règles de gestion, les calculs, la persistance JSON, les sauvegardes et les snapshots.

### 1.3 Glossaire

|Terme|Définition|
|---|---|
|Compte titres|Compte de type `CTO`, `PEA` ou `CRYPTO`. Il détient des positions et un solde de liquidités.|
|Compte espèces|Compte de type `COURANT`, `LIVRET` ou `AUTRE`. Sa valeur est un solde saisi.|
|Support|Instrument financier (ETF, action, obligation, crypto) du référentiel.|
|Mouvement|Opération datée : `ACHAT`, `VENTE`, `VERSEMENT` ou `RETRAIT`.|
|Position|Couple (compte titres, support) dont la quantité détenue est non nulle.|
|PRU|Prix de revient unitaire, calculé en prix moyen pondéré, frais d'achat inclus.|
|Snapshot|Photographie du patrimoine à une date donnée (au plus un par jour).|
|Date d'évaluation|Date à laquelle les valeurs sont calculées (par défaut, aujourd'hui).|
|Référentiel|Liste de valeurs administrée par l'utilisateur : zones géographiques, secteurs, établissements.|
|Établissement|Banque, courtier ou plateforme qui tient un compte.|
|Valeur utilisée|Valeur de référentiel portée par au moins un support (zone, secteur) ou un compte (établissement), archivé ou non.|

---

## 2. Cas d'Utilisation (Use Cases)

### 2.1 [UC-01] Initialiser le compte d'accès

- **Acteur principal** : Utilisateur.
- **Prérequis** : aucun fichier d'identifiants n'existe.
- **Scénario nominal** :
    1. L'utilisateur ouvre l'application. Le front détecte que l'application n'est pas initialisée et affiche l'écran « Création du compte ».
    2. L'utilisateur saisit un identifiant, un mot de passe et sa confirmation.
    3. Le système vérifie les règles de mot de passe, hache le mot de passe et crée le fichier d'identifiants.
    4. L'utilisateur est redirigé vers l'écran de connexion.
- **Scénarios alternatifs / limites** :
    - Le mot de passe ne respecte pas les règles, ou la confirmation diffère : un message d'erreur s'affiche et rien n'est créé.
    - Un fichier d'identifiants existe déjà : l'initialisation est refusée (HTTP 409).
- **Postconditions** : le compte d'accès existe.

### 2.2 [UC-02] Se connecter

- **Acteur principal** : Utilisateur.
- **Prérequis** : le compte d'accès existe.
- **Scénario nominal** :
    1. L'utilisateur saisit son identifiant et son mot de passe.
    2. Le système valide les identifiants et émet un jeton de session valable 8 heures.
    3. L'utilisateur arrive sur la page d'accueil.
- **Scénarios alternatifs / limites** :
    - Identifiants erronés : le message « Identifiant ou mot de passe incorrect » s'affiche et le compteur d'échecs augmente.
    - Cinquième échec consécutif : la connexion est bloquée 15 minutes et la durée restante s'affiche.
    - Jeton expiré pendant l'utilisation : l'utilisateur est redirigé vers l'écran de connexion.
- **Postconditions** : l'utilisateur est authentifié.

### 2.3 [UC-03] Gérer les comptes

- **Acteur principal** : Utilisateur authentifié.
- **Scénario nominal** :
    1. L'utilisateur crée un compte : nom, type et établissement. L'établissement est obligatoire et choisi parmi les établissements actifs du référentiel (UC-14).
    2. L'utilisateur peut modifier le nom et l'établissement d'un compte.
    3. L'utilisateur peut archiver ou désarchiver un compte.
- **Scénarios alternatifs / limites** :
    - Nom vide ou déjà utilisé par un autre compte : rejet.
    - Modification du type d'un compte qui a déjà des mouvements ou des soldes : rejet.
    - Établissement absent, inexistant, ou archivé alors qu'il est nouvellement choisi : rejet (RG-24).
    - Aucun établissement actif : le formulaire invite à en créer un dans l'écran « Référentiels ».
- **Postconditions** : le compte est enregistré, et le snapshot du jour est mis à jour.

### 2.4 [UC-04] Saisir un solde

- **Acteur principal** : Utilisateur authentifié.
- **Prérequis** : un compte non archivé existe.
- **Scénario nominal** :
    1. L'utilisateur choisit un compte, une date et un montant. Pour un compte espèces, le montant est le solde ; pour un compte titres, c'est le solde de liquidités.
    2. Le système enregistre le solde. S'il existe déjà un solde à cette date pour ce compte, il est remplacé.
    3. Les snapshots sont mis à jour.
- **Scénarios alternatifs / limites** : date future ou montant invalide : rejet.
- **Postconditions** : le solde est enregistré et les snapshots sont recalculés.

### 2.5 [UC-05] Gérer le référentiel des supports

- **Acteur principal** : Utilisateur authentifié.
- **Scénario nominal** :
    1. L'utilisateur crée un support : nom, code (ISIN ou ticker), type, zone géographique et secteur. La zone et le secteur sont choisis parmi les valeurs actives des référentiels (UC-14).
    2. L'utilisateur peut modifier le support, l'archiver ou le supprimer.
- **Scénarios alternatifs / limites** :
    - Code déjà utilisé : rejet.
    - Zone ou secteur inexistant, ou archivé alors qu'il est nouvellement choisi : rejet (RG-24).
    - Suppression d'un support lié à au moins un mouvement : rejet, avec la proposition d'archiver.
- **Postconditions** : le référentiel est à jour.

### 2.6 [UC-06] Enregistrer un achat ou une vente

- **Acteur principal** : Utilisateur authentifié.
- **Prérequis** : un compte titres non archivé et un support non archivé existent.
- **Scénario nominal** :
    1. L'utilisateur saisit la date, le compte, le support, le sens (achat ou vente), la quantité, le prix unitaire et les frais.
    2. Le système valide la saisie et, pour une vente, vérifie la quantité disponible.
    3. Le mouvement est enregistré, puis la position, le PRU, les plus-values et les snapshots sont recalculés.
- **Scénarios alternatifs / limites** :
    - Vente supérieure à la quantité détenue à la date de la vente : rejet, avec l'affichage de la quantité disponible.
    - Compte espèces choisi : rejet.
- **Postconditions** : le mouvement est enregistré et les calculs sont à jour.

### 2.7 [UC-07] Enregistrer un versement ou un retrait

- **Acteur principal** : Utilisateur authentifié.
- **Scénario nominal** : l'utilisateur saisit la date, le compte titres et le montant. Le système enregistre le mouvement.
- **Postconditions** : le mouvement est tracé dans l'historique. Il ne modifie pas automatiquement le solde de liquidités (voir RG-22).

### 2.8 [UC-08] Modifier ou supprimer un mouvement

- **Acteur principal** : Utilisateur authentifié.
- **Scénario nominal** :
    1. L'utilisateur sélectionne un mouvement dans l'historique, le modifie ou le supprime, puis confirme.
    2. Le système rejoue tous les mouvements du couple (compte, support) concerné et vérifie qu'aucune vente ne devient excédentaire.
    3. Le système enregistre la modification et recalcule les snapshots à partir de la plus ancienne date concernée.
- **Scénarios alternatifs / limites** : une vente ultérieure deviendrait excédentaire : rejet, avec l'indication du mouvement en conflit.
- **Postconditions** : l'historique, les positions et les snapshots sont cohérents.

### 2.9 [UC-09] Consulter l'historique des mouvements

- **Acteur principal** : Utilisateur authentifié.
- **Scénario nominal** : l'utilisateur affiche la liste des mouvements, triée par date décroissante. Il la filtre par compte, support, type de mouvement et période (date de début, date de fin).
- **Scénarios alternatifs / limites** : aucun résultat : le message « Aucun mouvement pour ces critères » s'affiche.

### 2.10 [UC-10] Saisir un cours

- **Acteur principal** : Utilisateur authentifié.
- **Scénario nominal** :
    1. L'utilisateur choisit un support, une date et un cours.
    2. Le système enregistre le cours. S'il existe déjà un cours à cette date pour ce support, il est remplacé.
    3. Les valorisations et les snapshots sont recalculés.
- **Scénarios alternatifs / limites** : cours inférieur ou égal à 0, ou date future : rejet.

### 2.11 [UC-11] Consulter la page d'accueil

- **Acteur principal** : Utilisateur authentifié.
- **Scénario nominal** : le système affiche le patrimoine total, la variation du mois en euros et en pourcentage, la plus-value latente globale et la répartition par type de compte.
- **Scénarios alternatifs / limites** :
    - Aucune donnée : un message invite à créer un premier compte.
    - Variation non calculable : « N/A » s'affiche.
    - Au moins une position sans cours : un bandeau « X position(s) sans cours, valorisée(s) au PRU » s'affiche.

### 2.12 [UC-12] Consulter la courbe d'évolution

- **Acteur principal** : Utilisateur authentifié.
- **Scénario nominal** : l'utilisateur choisit une période (1 mois, 1 an ou depuis le début). Le système trace le patrimoine total de chaque snapshot de la période.
- **Scénarios alternatifs / limites** : moins de 2 snapshots sur la période : le message « Historique insuffisant pour tracer une courbe » s'affiche.

### 2.13 [UC-13] Consulter le tableau de bord

- **Acteur principal** : Utilisateur authentifié.
- **Scénario nominal** :
    1. L'utilisateur applique des filtres : compte, type de support, zone, secteur.
    2. Le système affiche des graphiques de répartition (par compte, type de support, zone, secteur), le tableau des positions et le tableau des comptes, avec les plus-values latentes et réalisées.
- **Scénarios alternatifs / limites** : aucune donnée pour les filtres : les graphiques sont vides et un message s'affiche.

### 2.14 [UC-14] Gérer les référentiels (zones, secteurs, établissements)

- **Acteur principal** : Utilisateur authentifié.
- **Prérequis** : les référentiels des zones et des secteurs ont été initialisés avec leurs valeurs par défaut (section 3.11).
- **Scénario nominal** :
    1. L'utilisateur ouvre l'écran « Référentiels » et choisit un onglet : Zones géographiques, Secteurs ou Établissements. La liste affiche, pour chaque valeur, son libellé, son code (zones et secteurs), son utilisation (nombre de supports ou de comptes) et son statut.
    2. L'utilisateur crée une valeur : libellé et code pour une zone ou un secteur ; nom pour un établissement.
    3. L'utilisateur modifie le libellé d'une valeur. Le nouveau libellé s'affiche partout (listes, formulaires, tableau de bord). Le code n'est modifiable que tant que la valeur n'est pas utilisée.
    4. L'utilisateur archive ou désarchive une valeur, ou supprime une valeur non utilisée.
- **Scénarios alternatifs / limites** :
    - Libellé vide, trop long, ou déjà utilisé dans le même référentiel : rejet.
    - Code au mauvais format ou déjà utilisé : rejet.
    - Modification du code d'une valeur utilisée : rejet (RG-30).
    - Suppression d'une valeur utilisée : rejet, avec la proposition d'archiver (RG-30).
- **Postconditions** : le référentiel est à jour. Une valeur archivée n'est plus proposée à la saisie, mais les supports et les comptes qui la portent la conservent. Aucun snapshot n'est recalculé, car les snapshots ne stockent ni zone, ni secteur, ni établissement.

---

## 3. Description Détaillée des Fonctionnalités

### 3.1 Authentification

- **Entrées** : identifiant (3 à 50 caractères) et mot de passe (12 caractères minimum).
- **Traitements** : hachage BCrypt du mot de passe. Au login, comparaison du mot de passe avec le hash, puis émission d'un jeton.
- **Sorties** : un jeton et sa date d'expiration, ou une erreur.

### 3.2 Comptes et soldes

- **Types de comptes** (liste fixe, non administrable) :

|Type|Catégorie|Nature de la valeur|
|---|---|---|
|`COURANT`|Espèces|Dernier solde saisi|
|`LIVRET`|Espèces|Dernier solde saisi|
|`AUTRE`|Espèces|Dernier solde saisi|
|`CTO`|Titres|Positions + liquidités|
|`PEA`|Titres|Positions + liquidités|
|`CRYPTO`|Titres|Positions + liquidités|

- **Entrées d'un compte** : nom (obligatoire, unique), type (obligatoire), établissement (obligatoire, id d'un établissement actif du référentiel, RG-24).
- **Entrées d'un solde** : id du compte, date (JJ/MM/AAAA, sans date future) et montant (décimal, 2 décimales). Un solde négatif est autorisé, par exemple pour un découvert.
- **Traitement** : upsert sur la clé (compte, date).

### 3.3 Référentiel des supports

- **Entrées** :
    - nom (obligatoire) ;
    - code (obligatoire, unique, insensible à la casse) ;
    - type (`ETF`, `ACTION`, `OBLIGATION`, `CRYPTO` ; liste fixe, non administrable) ;
    - zone (code d'une zone active du référentiel, RG-24) ;
    - secteur (code d'un secteur actif du référentiel, RG-24).
- **Statuts** : `ACTIF` ou `ARCHIVE`.

### 3.4 Mouvements

- **Entrées** :

|Champ|ACHAT / VENTE|VERSEMENT / RETRAIT|
|---|---|---|
|date|Obligatoire, pas dans le futur|Obligatoire, pas dans le futur|
|compteId|Compte titres non archivé|Compte titres non archivé|
|supportId|Obligatoire, support actif|Interdit|
|quantite|> 0, 8 décimales max|Interdit|
|prixUnitaire|> 0, précision selon RG-02|Interdit|
|frais|≥ 0, 2 décimales max (0 par défaut)|Interdit|
|montant|Interdit|> 0, 2 décimales max|

- **Traitements** : validation, contrôle de la quantité disponible (RG-10), persistance, puis recalcul (RG-13).
- **Sorties** : le mouvement créé avec son id, ou une erreur.

### 3.5 Cours

- **Entrées** : id du support, date (pas dans le futur) et cours (> 0, précision selon RG-02).
- **Traitement** : upsert sur la clé (support, date).

### 3.6 Moteur de calcul

Pour une date d'évaluation D, le moteur calcule :

1. **Pour chaque couple (compte titres, support)** : il rejoue les mouvements ACHAT et VENTE de date ≤ D, dans l'ordre chronologique (même date : ordre de création). Il en déduit la quantité détenue, le PRU (RG-03, RG-04) et la plus-value réalisée cumulée (RG-05).
2. **Le cours retenu** : le cours de date ≤ D le plus récent (RG-23). À défaut, le PRU (RG-11).
3. **La valorisation** = quantité × cours retenu. La plus-value latente suit RG-06.
4. **La valeur de chaque compte** suit RG-07 et RG-08, en retenant le solde de date ≤ D le plus récent (0 si aucun).
5. **Le patrimoine total** suit RG-09.

### 3.7 Snapshots

- **Contenu** : date, patrimoine total, valeur par compte, et, pour chaque position, la quantité, le PRU, le cours retenu, la valorisation et la plus-value latente.
- **Déclenchement** : après chaque écriture réussie (compte, solde, support, mouvement, cours), selon RG-13 et RG-14. Une écriture dans un référentiel (UC-14) ne déclenche aucun recalcul.

### 3.8 Page d'accueil

|Indicateur|Calcul|
|---|---|
|Patrimoine total|RG-09 à la date du jour|
|Variation du mois (€)|RG-15|
|Variation du mois (%)|Variation (€) ÷ patrimoine de référence × 100, 2 décimales. « N/A » si la référence vaut 0 ou n'existe pas.|
|Plus-value latente globale|Somme des plus-values latentes de toutes les positions des comptes non archivés, en € et en % du coût d'acquisition (Σ quantité × PRU)|
|Répartition par type de compte|Somme des valeurs par type de compte (graphique en anneau), en € et en % du total|

### 3.9 Courbe d'évolution

- **Périodes** :
    - `1M` : snapshots des 30 derniers jours ;
    - `1A` : des 365 derniers jours ;
    - `ALL` : tous les snapshots.
- **Axe des abscisses** : date. **Axe des ordonnées** : patrimoine total en euros.

### 3.10 Tableau de bord

- **Filtres** (combinables, multi-sélection) : comptes, types de support, zones, secteurs. Les listes de zones et de secteurs proposent toutes les valeurs des référentiels, archivées comprises, pour que les positions existantes restent filtrables.
- **Portée des filtres** :
    - Les filtres de type de support, de zone et de secteur ne s'appliquent qu'aux positions.
    - Dès qu'un de ces filtres est actif, les comptes espèces et les liquidités sont exclus des restitutions, car ils n'ont ni zone ni secteur.
- **Restitutions** :
    - 4 graphiques de répartition (compte, type de support, zone, secteur), avec la valeur et le pourcentage. Les zones et les secteurs sont affichés avec leur libellé courant. Quand les liquidités sont incluses, elles forment une part « Espèces et liquidités » dans les répartitions par type de support, par zone et par secteur ;
    - le tableau des positions : compte, support, quantité, PRU, cours, date du cours, valorisation, plus-value latente (€ et %), indicateur de cours manquant ;
    - le tableau des comptes : nom, type, valeur, plus-value latente, plus-value réalisée cumulée, avec une ligne de total.

### 3.11 Référentiels administrables

|Référentiel|Champs|Utilisé par|Valeurs initiales|
|---|---|---|---|
|Zones géographiques|code, libellé, statut|Supports (`zone`)|5 valeurs (ci-dessous)|
|Secteurs|code, libellé, statut|Supports (`sector`)|13 valeurs (ci-dessous)|
|Établissements|nom (libellé), statut|Comptes (`institutionId`)|Aucune|

- **Zones initiales** : `EUROPE` (Europe), `AMERIQUE_NORD` (Amérique du Nord), `ASIE_PACIFIQUE` (Asie-Pacifique), `EMERGENTS` (Émergents), `MONDE` (Monde).
- **Secteurs initiaux** : les 11 secteurs GICS – `ENERGIE` (Énergie), `MATERIAUX` (Matériaux), `INDUSTRIE` (Industrie), `CONSO_DISCRETIONNAIRE` (Consommation discrétionnaire), `CONSO_BASE` (Consommation de base), `SANTE` (Santé), `FINANCE` (Finance), `TECHNOLOGIE` (Technologies de l'information), `COMMUNICATION` (Services de communication), `SERVICES_PUBLICS` (Services aux collectivités), `IMMOBILIER` (Immobilier) – plus `DIVERSIFIE` (Diversifié) et `NON_APPLICABLE` (Non applicable, pour les cryptos et les obligations d'État).
- **Initialisation** : les valeurs initiales sont créées au premier démarrage, uniquement si le référentiel n'existe pas encore. Elles sont ensuite modifiables comme les autres valeurs.
- **Utilisation** : nombre de supports (zones, secteurs) ou de comptes (établissements) qui portent la valeur, archivés compris. Elle conditionne la modification du code et la suppression (RG-30).
- **Hors périmètre** : les types de compte et les types de support restent des listes fixes, car des règles de calcul en dépendent (RG-02, RG-07, RG-08, RG-21).

---

## 4. Contrats d'Interface et de Données (API / UI)

### 4.1 Conventions communes

- **Base URL** : `http://localhost:5080/api`.
- **Format** : JSON en UTF-8. Les dates sont au format ISO `YYYY-MM-DD` et les horodatages en ISO 8601 UTC.
- **Décimaux** : les montants, prix et quantités sont transmis en **nombres JSON**.
- **Authentification** : en-tête `Authorization: Bearer <jwt>` sur tous les endpoints, sauf `/auth/status`, `/auth/setup` et `/auth/login`.
- **Format d'erreur unique** :

json

```json
  { "error": "CODE_ERREUR", "message": "Message explicatif en français", "details": { } }
```

- **Codes HTTP** :

|Code|Cas|
|---|---|
|200 / 201 / 204|Succès|
|400|Validation (`VALIDATION_ERROR`)|
|401|Non authentifié ou jeton invalide (`UNAUTHORIZED`)|
|404|Ressource inexistante (`NOT_FOUND`)|
|409|Conflit : doublon, déjà initialisé, support ou valeur de référentiel utilisé (`CONFLICT`)|
|422|Règle métier violée : vente excédentaire (`INSUFFICIENT_QUANTITY`)|
|423|Connexion bloquée (`ACCOUNT_LOCKED`)|
|500|Erreur interne (`INTERNAL_ERROR`)|

### 4.2 Contrats API

#### `GET /api/auth/status`

- **Réponse 200** : `{ "initialized": true }`

#### `POST /api/auth/setup`

- **Requête** : `{ "username": "guillaume", "password": "********", "passwordConfirmation": "********" }`
- **Réponse 201** : `{ "username": "guillaume" }`
- **Erreurs** : 400 `VALIDATION_ERROR`, 409 `CONFLICT` (déjà initialisé).

#### `POST /api/auth/login`

- **Requête** : `{ "username": "guillaume", "password": "********" }`
- **Réponse 200** : `{ "token": "eyJ...", "expiresAt": "2026-09-23T15:24:00Z" }`
- **Erreurs** :
    - 401 `UNAUTHORIZED` ;
    - 423 `ACCOUNT_LOCKED`, avec `details: { "retryAfterSeconds": 812 }`.

#### Comptes

|Méthode|Chemin|Description|
|---|---|---|
|GET|`/api/accounts?includeArchived=false`|Liste des comptes (comptes archivés en dernier)|
|GET|`/api/accounts/{id}`|Détail d'un compte|
|POST|`/api/accounts`|Création|
|PUT|`/api/accounts/{id}`|Modification (nom, établissement ; type sous condition de RG-21)|
|POST|`/api/accounts/{id}/archive`|Archivage|
|POST|`/api/accounts/{id}/unarchive`|Désarchivage|

- **Requête POST/PUT** : `{ "name": "PEA Boursorama", "type": "PEA", "institutionId": "5b7e..." }` (`institutionId` obligatoire)
- **Réponse** :

json

```json
  { "id": "3f2a...", "name": "PEA Boursorama", "type": "PEA", "category": "TITRES",
    "institutionId": "5b7e...", "institutionName": "Boursorama", "institutionArchived": false,
    "archived": false, "currentValue": 1235.00, "cash": 200.00, "cashDate": "2026-09-01" }
```

- `currentValue` : valeur du compte à la date du jour (RG-07, RG-08), calculée aussi pour un compte archivé, qui reste exclu du patrimoine (RG-27).
- `cash` et `cashDate` : dernier solde de date ≤ aujourd'hui (liquidités pour un compte titres) ; `0` et `null` sans solde saisi.
- `institutionArchived` : vrai si l'établissement porté est archivé (affichage « (archivé) »).

#### Soldes

|Méthode|Chemin|Description|
|---|---|---|
|GET|`/api/accounts/{id}/balances`|Historique des soldes du compte|
|PUT|`/api/accounts/{id}/balances/{date}`|Upsert du solde à la date donnée|
|DELETE|`/api/accounts/{id}/balances/{date}`|Suppression|

- **Requête PUT** : `{ "amount": 1520.35 }`
- **Réponse 200** : `{ "accountId": "3f2a...", "date": "2026-09-22", "amount": 1520.35 }`

#### Supports

|Méthode|Chemin|Description|
|---|---|---|
|GET|`/api/securities?includeArchived=false`|Liste|
|GET|`/api/securities/{id}`|Détail d'un support|
|POST|`/api/securities`|Création|
|PUT|`/api/securities/{id}`|Modification|
|POST|`/api/securities/{id}/archive`|Archivage|
|POST|`/api/securities/{id}/unarchive`|Désarchivage|
|DELETE|`/api/securities/{id}`|Suppression (409 si le support est utilisé)|

- **Requête POST/PUT** :

json

```json
  { "name": "Amundi MSCI World", "code": "LU1681043599", "type": "ETF",
    "zone": "MONDE", "sector": "DIVERSIFIE" }
```

- **Réponse** : les champs de la requête, plus `id`, `archived`, `zoneLabel` et `sectorLabel` (libellés courants), `zoneArchived` et `sectorArchived` (valeur portée archivée), `lastPrice` et `lastPriceDate` (dernier cours saisi, arrondi selon RG-29 ; `null` sans cours).

#### Référentiels

`{kind}` ∈ `zones`, `sectors`, `institutions`.

|Méthode|Chemin|Description|
|---|---|---|
|GET|`/api/referentials/{kind}?includeArchived=false`|Liste, triée par libellé, avec l'utilisation|
|POST|`/api/referentials/{kind}`|Création|
|PUT|`/api/referentials/{kind}/{id}`|Modification du libellé, et du code si la valeur n'est pas utilisée|
|POST|`/api/referentials/{kind}/{id}/archive`|Archivage|
|POST|`/api/referentials/{kind}/{id}/unarchive`|Désarchivage|
|DELETE|`/api/referentials/{kind}/{id}`|Suppression (409 si la valeur est utilisée)|

- **Requête POST/PUT (zones, secteurs)** : `{ "code": "MONDE", "label": "Monde" }`
- **Requête POST/PUT (établissements)** : `{ "label": "Boursorama" }`
- **Réponse** :

json

```json
  { "id": "9d4c...", "code": "MONDE", "label": "Monde", "archived": false, "usageCount": 2 }
```

Pour un établissement, `code` vaut `null`.

- **Erreurs** : 400 `VALIDATION_ERROR` (libellé ou format de code), 404 (`kind` ou id inconnu), 409 `CONFLICT` (doublon, code d'une valeur utilisée, suppression d'une valeur utilisée, avec `details: { "usageCount": 2 }`).

#### Mouvements

|Méthode|Chemin|Description|
|---|---|---|
|GET|`/api/movements?accountId=&securityId=&type=&from=&to=`|Liste filtrée, triée par date décroissante (puis ordre de création décroissant)|
|GET|`/api/movements/{id}`|Détail d'un mouvement|
|POST|`/api/movements`|Création|
|PUT|`/api/movements/{id}`|Modification (type non modifiable)|
|DELETE|`/api/movements/{id}`|Suppression|

- **Requête POST (achat ou vente)** :

json

```json
  { "type": "ACHAT", "date": "2026-09-15", "accountId": "3f2a...", "securityId": "8c1d...",
    "quantity": 10.5, "unitPrice": 102.40, "fees": 1.99 }
```

- **Requête POST (versement ou retrait)** :

json

```json
  { "type": "VERSEMENT", "date": "2026-09-01", "accountId": "3f2a...", "amount": 500.00 }
```

- **Réponse 201** : le mouvement, avec `id`, `sequence`, `createdAt` et `updatedAt`, plus `accountName`, `securityName` et `securityCode` (libellés courants) et `total`, le montant de l'opération (q × prix + frais pour un achat, q × prix − frais pour une vente, `amount` pour un versement ou un retrait), arrondi à 2 décimales.
- **Erreur 422** :

json

```json
  { "error": "INSUFFICIENT_QUANTITY",
    "message": "Quantité insuffisante : 4.00000000 disponibles au 2026-09-15",
    "details": { "availableQuantity": 4.0, "conflictingMovementId": "a91b..." } }
```

#### Cours

|Méthode|Chemin|Description|
|---|---|---|
|GET|`/api/securities/{id}/prices`|Historique des cours du support|
|PUT|`/api/securities/{id}/prices/{date}`|Upsert du cours à la date donnée|
|DELETE|`/api/securities/{id}/prices/{date}`|Suppression|

- **Requête PUT** : `{ "price": 104.12 }`

#### Restitutions

|Méthode|Chemin|Description|
|---|---|---|
|GET|`/api/dashboard/summary`|Indicateurs de la page d'accueil|
|GET|`/api/dashboard/history?period=1M\|1A\|ALL`|Points de la courbe|
|GET|`/api/dashboard/positions?accountIds=&securityTypes=&zones=&sectors=`|Tableau des positions et répartitions filtrées|
|GET|`/api/dashboard/accounts?accountIds=`|Tableau des comptes|

- **Réponse de `summary`** :

json

```json
  { "date": "2026-09-23", "totalNetWorth": 84210.55,
    "monthVariation": { "amount": 1250.10, "percent": 1.51, "referenceDate": "2026-08-31" },
    "unrealizedGain": { "amount": 9340.20, "percent": 14.22 },
    "byAccountType": [ { "type": "PEA", "amount": 42000.00, "percent": 49.88 } ],
    "missingPriceCount": 1 }
```

Si la variation n'est pas calculable, `monthVariation` vaut `null`.

- **Réponse de `history`** : `{ "period": "1A", "points": [ { "date": "2026-01-05", "totalNetWorth": 76100.00 } ] }`
- **Réponse de `positions`** :

json

```json
  { "positions": [ { "accountId": "...", "accountName": "PEA", "securityId": "...", "securityName": "...",
      "securityType": "ETF", "zone": "MONDE", "zoneLabel": "Monde", "sector": "DIVERSIFIE",
      "sectorLabel": "Diversifié", "quantity": 10.5, "averageCost": 102.59,
      "price": 104.12, "priceDate": "2026-09-22", "missingPrice": false, "marketValue": 1093.26,
      "unrealizedGain": 16.07, "unrealizedGainPercent": 1.49 } ],
    "allocation": { "byAccount": [], "bySecurityType": [], "byZone": [], "bySector": [] },
    "includesCash": true }
```

### 4.3 Interfaces Utilisateur (UI)

L'interface est en français. Elle comporte un menu latéral : Accueil, Tableau de bord, Mouvements, Comptes, Supports, Cours, Référentiels, puis Déconnexion (avec le rappel de l'identifiant connecté). Les maquettes des écrans nominaux sont dans le canevas « FinanceReport – Maquettes MVP ».

|Écran|Contenu|
|---|---|
|Création du compte|Champs identifiant, mot de passe, confirmation ; rappel des règles du mot de passe ; bouton « Créer »|
|Connexion|Champs identifiant, mot de passe ; bouton « Se connecter » ; zone de message d'erreur ; compte à rebours en cas de blocage|
|Accueil|4 tuiles d'indicateurs (patrimoine total, variation du mois en €, variation du mois en %, plus-value latente) ; courbe d'évolution avec sélecteur 1M / 1A / Tout ; graphique en anneau par type de compte ; bandeau de cours manquants ; raccourci « Nouveau mouvement »|
|Tableau de bord|Barre de filtres ; 4 graphiques de répartition ; tableau des positions (tri sur chaque colonne) ; tableau des comptes avec ligne de total|
|Mouvements|Filtres ; tableau paginé (50 lignes par page) ; bouton « Nouveau mouvement » ; formulaire modal dont les champs dépendent du type (sélecteur Achat / Vente / Versement / Retrait) ; actions Modifier et Supprimer, avec confirmation|
|Comptes|Liste avec le type, la catégorie, l'établissement et la valeur actuelle ; option « Afficher les archivés » ; création et modification (établissement choisi dans le référentiel) ; archivage ; panneau latéral d'historique des soldes, de saisie et de suppression d'un solde (avec confirmation)|
|Supports|Liste avec le code, le type, la zone, le secteur et le statut ; option « Afficher les archivés » ; création et modification (zone et secteur choisis dans les référentiels) ; archivage et suppression|
|Cours|Tableau des supports actifs avec le dernier cours et sa date ; saisie rapide d'un cours par ligne ; panneau d'historique par support|
|Référentiels|Onglets Zones géographiques, Secteurs, Établissements, avec le nombre de valeurs ; liste avec libellé, code, utilisation et statut ; option « Afficher les archivés » ; panneau latéral de création ou de modification (code verrouillé si la valeur est utilisée) ; actions Modifier, Archiver, et Supprimer (proposée uniquement pour une valeur non utilisée)|

- **Affichage des montants** : format français (`1 234,56 €`).
- **Couleurs** : plus-values positives en vert, négatives en rouge.
- **Cours manquant** : icône d'avertissement, avec l'infobulle « Cours manquant, valorisé au PRU ».
- **Listes de choix** : les formulaires ne proposent que les valeurs actives des référentiels. Une valeur archivée déjà portée par un support ou un compte reste affichée dans son formulaire, suivie de la mention « (archivé) ».

---

## 5. Règles de Gestion (Business Rules)

| ID        | Description de la règle de gestion                                                                                                                                                                                                                                                                                                                                                                                                                                | Source / Impact            | Comportement par défaut (fallback)                                                   |
| --------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------- | ------------------------------------------------------------------------------------ |
| **RG-01** | Tous les montants, prix et valorisations sont en euros. Aucun champ devise n'est saisi.                                                                                                                                                                                                                                                                                                                                                                           | RM-01 / Global             | EUR implicite                                                                        |
| **RG-02** | Les quantités ont 8 décimales au maximum ; les montants et les frais, 2 décimales au maximum. Le prix unitaire et le cours ont 2 décimales au maximum, ou 8 si le support est de type `CRYPTO`. Toute saisie qui dépasse la précision est rejetée.                                                                                                                                                                                                                | RM-02 / Saisie             | Rejet 400                                                                            |
| **RG-03** | À l'achat : PRU = (Q × PRU + q × prix + frais) ÷ (Q + q), où Q et PRU sont la quantité et le PRU détenus avant l'achat, et q la quantité achetée.                                                                                                                                                                                                                                                                                                                 | RM-03 / Calcul             | Si Q = 0, PRU = (q × prix + frais) ÷ q                                               |
| **RG-04** | Une vente diminue la quantité, mais ne modifie pas le PRU. Quand la quantité revient à 0, la position disparaît et le PRU repart de zéro au prochain achat.                                                                                                                                                                                                                                                                                                       | RM-03 / Calcul             | —                                                                                    |
| **RG-05** | Plus-value réalisée d'une vente = q × (prix de vente − PRU) − frais de vente. Elle est cumulée par position, par compte et au global.                                                                                                                                                                                                                                                                                                                             | RM-04 / Calcul             | —                                                                                    |
| **RG-06** | Plus-value latente = Q × (cours retenu − PRU). En % : ÷ (Q × PRU) × 100.                                                                                                                                                                                                                                                                                                                                                                                          | RM-05 / Calcul             | Cours retenu selon RG-11                                                             |
| **RG-07** | Valeur d'un compte titres = Σ valorisations de ses positions + dernier solde de liquidités de date ≤ D.                                                                                                                                                                                                                                                                                                                                                           | RM-06 / Calcul             | Liquidités = 0 sans solde saisi                                                      |
| **RG-08** | Valeur d'un compte espèces = dernier solde de date ≤ D.                                                                                                                                                                                                                                                                                                                                                                                                           | RM-07 / Calcul             | 0 sans solde saisi                                                                   |
| **RG-09** | Patrimoine total = Σ valeurs des comptes non archivés.                                                                                                                                                                                                                                                                                                                                                                                                            | RM-08 / Calcul             | 0 sans compte                                                                        |
| **RG-10** | Une vente est rejetée si sa quantité dépasse la quantité détenue à sa date. La quantité détenue est calculée en rejouant les mouvements antérieurs ou simultanés créés avant la vente.                                                                                                                                                                                                                                                                            | RM-09 / Saisie             | Rejet 422 `INSUFFICIENT_QUANTITY`                                                    |
| **RG-11** | Une position sans aucun cours de date ≤ D est valorisée au PRU (plus-value latente nulle). Elle porte l'indicateur `missingPrice = true`.                                                                                                                                                                                                                                                                                                                         | RM-10 / Calcul             | Valorisation au PRU                                                                  |
| **RG-12** | Un support lié à au moins un mouvement ne peut pas être supprimé, seulement archivé. Un support archivé ne peut pas recevoir de nouveau mouvement, mais reste visible dans l'historique et les positions.                                                                                                                                                                                                                                                         | RM-11 / Référentiel        | Rejet 409, archivage proposé                                                         |
| **RG-13** | Toute création, modification ou suppression datée (mouvement, solde, cours) à une date d, et tout changement de compte ou de support, déclenche le recalcul de tous les snapshots existants de date ≥ d, puis l'upsert du snapshot du jour. Une modification qui rend une vente ultérieure excédentaire est rejetée dans son ensemble. Une écriture dans un référentiel (UC-14) ne déclenche aucun recalcul.                                                      | RM-12 / Calcul             | Rejet 422, aucune donnée modifiée                                                    |
| **RG-14** | Il y a au plus un snapshot par jour calendaire (fuseau Europe/Paris). Un nouveau calcul le même jour écrase le snapshot du jour.                                                                                                                                                                                                                                                                                                                                  | RM-13 / Historique         | Écrasement                                                                           |
| **RG-15** | Variation du mois = patrimoine du jour − patrimoine du dernier snapshot de date < 1er jour du mois en cours.                                                                                                                                                                                                                                                                                                                                                      | RM-14 / Accueil            | `null`, affiché « N/A »                                                              |
| **RG-16** | Un seul compte d'accès. L'initialisation n'est possible que si aucun fichier d'identifiants n'existe. Règles du mot de passe : 12 caractères minimum ; identifiant de 3 à 50 caractères.                                                                                                                                                                                                                                                                          | EM-01 / Sécurité           | Rejet 409 si déjà initialisé                                                         |
| **RG-17** | Le jeton JWT est valable 8 heures. Tout endpoint métier appelé sans jeton valide renvoie 401.                                                                                                                                                                                                                                                                                                                                                                     | BRD 4.2 / Sécurité         | Redirection vers la connexion                                                        |
| **RG-18** | Après 5 échecs de connexion consécutifs, toute tentative est refusée pendant 15 minutes, même avec le bon mot de passe. Une connexion réussie remet le compteur à 0.                                                                                                                                                                                                                                                                                              | BRD 4.2 / Sécurité         | Rejet 423                                                                            |
| **RG-19** | Avant chaque écriture d'un fichier de données, une copie horodatée est créée. Au-delà de 100 copies par fichier, les plus anciennes sont supprimées.                                                                                                                                                                                                                                                                                                              | BRD 4.3 / Persistance      | —                                                                                    |
| **RG-20** | Les logs ne contiennent jamais de mot de passe, de hash ni de jeton.                                                                                                                                                                                                                                                                                                                                                                                              | BRD 4.3 / Logs             | —                                                                                    |
| **RG-21** | Achats, ventes, versements, retraits et positions ne sont possibles que sur un compte titres. Le type d'un compte n'est plus modifiable dès qu'il a au moins un mouvement ou un solde.                                                                                                                                                                                                                                                                            | Glossaire / Saisie         | Rejet 400 / 409                                                                      |
| **RG-22** | Les mouvements (achat, vente, versement, retrait) ne modifient pas automatiquement le solde de liquidités, qui reste saisi par l'utilisateur. Les versements et retraits servent uniquement à tracer les apports (utilisés au lot 1).                                                                                                                                                                                                                             | BRD EM-04, RM-06 / Calcul  | —                                                                                    |
| **RG-23** | Le cours retenu pour une date D est le cours de date ≤ D le plus récent. Le solde retenu suit la même règle.                                                                                                                                                                                                                                                                                                                                                      | EM-08, EM-09 / Calcul      | RG-11 / RG-07 / RG-08                                                                |
| **RG-24** | La zone et le secteur d'un support, et l'établissement d'un compte, sont obligatoires et choisis parmi les valeurs des référentiels administrables (UC-14, section 3.11). À la création, ou quand la valeur change, elle doit exister et être active. Un support ou un compte qui porte déjà une valeur archivée la conserve.                                                                                                                                     | EM-05, EM-16 / Référentiel | Rejet 400 si la valeur est absente, inexistante, ou archivée et nouvellement choisie |
| **RG-25** | Aucune date de mouvement, de solde ou de cours ne peut être postérieure à la date du jour.                                                                                                                                                                                                                                                                                                                                                                        | Saisie                     | Rejet 400                                                                            |
| **RG-26** | Le code d'un support (unicité insensible à la casse) et le nom d'un compte sont uniques.                                                                                                                                                                                                                                                                                                                                                                          | Référentiel                | Rejet 409                                                                            |
| **RG-27** | Un compte archivé est exclu du patrimoine total, des restitutions et des listes de saisie. Ses données sont conservées, et le désarchivage le réintègre.                                                                                                                                                                                                                                                                                                          | EM-02 / Calcul             | —                                                                                    |
| **RG-28** | Dès qu'un filtre de type de support, de zone ou de secteur est actif, les comptes espèces et les liquidités sont exclus des restitutions du tableau de bord.                                                                                                                                                                                                                                                                                                      | EM-15 / Restitution        | `includesCash = false`                                                               |
| **RG-29** | Les calculs se font en précision décimale complète. Les résultats sont arrondis à l'affichage et dans les réponses de l'API (au demi supérieur) : 2 décimales pour les montants et les pourcentages ; 2 décimales pour le PRU, ou 8 pour un support `CRYPTO`.                                                                                                                                                                                                     | RM-02 / Calcul             | —                                                                                    |
| **RG-30** | Dans un référentiel : le libellé est obligatoire (1 à 60 caractères) et unique (insensible à la casse) ; le code d'une zone ou d'un secteur est obligatoire, unique et respecte le format `^[A-Z0-9_]{2,40}$`. Le code d'une valeur utilisée n'est plus modifiable. Une valeur utilisée ne peut pas être supprimée, seulement archivée. Une valeur archivée n'est plus proposée à la saisie, reste affichée partout où elle est portée, et peut être désarchivée. | EM-16 / Référentiel        | Rejet 400 (format) / 409 (doublon, valeur utilisée)                                  |