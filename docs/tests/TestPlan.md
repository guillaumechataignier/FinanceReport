# Plan de Test & Validation – FinanceReport (MVP)

|Élément|Valeur|
|---|---|
|Version|1.2 – MVP|
|Date|23/09/2026|
|Documents sources|`docs/functional/FunctionalSpecifications.md` v1.2 (RG-01 à RG-30), `docs/technical/TechnicalSpecifications.md` v1.2|

**Historique des versions**

|Version|Date|Modifications|
|---|---|---|
|1.0|23/09/2026|Version initiale|
|1.1|23/09/2026|Référentiels administrables : JDR complété (établissement E1), TC-FUNC-20 réécrit, ajout de TC-FUNC-28 à TC-FUNC-30, couverture de RG-30 et UC-14.|
|1.2|23/09/2026|Établissement obligatoire : JDR complété (E2, établissement de chaque compte), TC-FUNC-29 réécrit.|

---

## 1. Stratégie de Validation

### 1.1 Objectifs de Validation

- Garantir que **chaque règle de gestion RG-01 à RG-30** est couverte par au moins un cas de test.
- Garantir l'exactitude des calculs financiers (PRU, plus-values, valorisations, variation), au centime près.
- Garantir l'intégrité des données : écritures atomiques, sauvegardes, rejets sans effet de bord.
- Garantir la sécurité de l'accès local (authentification, blocage, absence de secrets dans les logs).

### 1.2 Types de Tests

- **Tests unitaires (TU)**, avec xUnit : `PositionCalculator`, `ValuationCalculator`, validateurs, `DashboardService`, `ReferentialService`, `LoginAttemptTracker`. `IClock` est simulé.
- **Tests d'intégration (TI)**, avec `WebApplicationFactory` et un dossier de données temporaire par test : endpoints, persistance JSON, sauvegardes, snapshots, initialisation des référentiels, JWT, logs.
- **Tests de validation fonctionnelle (E2E/UAT)**, avec Playwright et en recette manuelle : parcours de l'interface, à comparer aux maquettes « FinanceReport – Maquettes MVP ».

### 1.3 Jeu de données de référence (JDR)

Ce jeu est utilisé par plusieurs tests. La date du jour simulée est le **23/09/2026**.

|Élément|Données|
|---|---|
|Référentiels|Zones et secteurs initiaux (spécification fonctionnelle, section 3.11)|
|Établissement E1|« Boursorama », actif|
|Établissement E2|« Banque Test », actif|
|Compte A|« PEA Test », type `PEA`, établissement E1|
|Compte B|« Livret A », type `LIVRET`, établissement E2, solde de 5 000,00 au 01/09/2026|
|Compte C|« Ancien CTO », type `CTO`, établissement E1, liquidités de 1 000,00 au 01/01/2026, **archivé**|
|Support S1|« ETF Monde », code `LU1681043599`, `ETF`, zone `MONDE`, secteur `DIVERSIFIE`|
|Support S2|« Bitcoin », code `BTC`, `CRYPTO`, zone `MONDE`, secteur `NON_APPLICABLE`|
|M1|ACHAT, 10/01/2026, A, S1, quantité 10, prix 100,00, frais 2,00|
|M2|ACHAT, 10/02/2026, A, S1, quantité 5, prix 110,00, frais 1,00|
|M3|VENTE, 10/03/2026, A, S1, quantité 6, prix 120,00, frais 1,50|
|Liquidités A|200,00 au 01/09/2026|
|Cours S1|115,00 au 22/09/2026|

**Résultats attendus sur le JDR** :

|Grandeur|Calcul|Résultat|
|---|---|---|
|PRU après M1|(10 × 100 + 2) ÷ 10|100,20|
|PRU après M2|(10 × 100,20 + 5 × 110 + 1) ÷ 15 = 1 553 ÷ 15|103,5333… (affiché 103,53)|
|PV réalisée M3|6 × (120 − 103,5333…) − 1,50|97,30|
|Quantité après M3|15 − 6|9|
|PV latente S1|9 × (115 − 103,5333…)|103,20|
|PV latente S1 (%)|103,20 ÷ (9 × 103,5333…) = 103,20 ÷ 931,80|11,08 %|
|Valeur compte A|9 × 115 + 200|1 235,00|
|Patrimoine total|1 235 + 5 000 (C archivé exclu)|6 235,00|
|Utilisation de la zone `MONDE`|S1 + S2|2|
|Utilisation des secteurs `DIVERSIFIE` et `NON_APPLICABLE`|S1 ; S2|1 chacun|
|Utilisation des établissements E1 et E2|A + C (archivé compris) ; B|2 ; 1|

---

## 2. Matrice de Traçabilité (Règles ↔️ Tests)

|Règle de gestion|ID test associé|Titre du cas de test|Statut de validation|
|---|---|---|---|
|**RG-01** (EUR unique)|TC-FUNC-01|Absence de champ devise et affichage en euros|À valider|
|**RG-02** (Précision)|TC-FUNC-02|Rejet des saisies dépassant la précision autorisée|À valider|
|**RG-03** (PRU à l'achat)|TC-FUNC-03|Calcul du PRU en PMP frais inclus|À valider|
|**RG-04** (Vente et PRU)|TC-FUNC-03, TC-FUNC-04|PRU inchangé après vente et remise à zéro de la position soldée|À valider|
|**RG-05** (PV réalisée)|TC-FUNC-05|Calcul de la plus-value réalisée|À valider|
|**RG-06** (PV latente)|TC-FUNC-06|Calcul de la plus-value latente en € et en %|À valider|
|**RG-07** (Valeur compte titres)|TC-FUNC-07|Valeur d'un compte titres = positions + liquidités|À valider|
|**RG-08** (Valeur compte espèces)|TC-FUNC-07|Valeur d'un compte espèces = dernier solde|À valider|
|**RG-09** (Patrimoine total)|TC-FUNC-07|Patrimoine total hors comptes archivés|À valider|
|**RG-10** (Vente excédentaire)|TC-FUNC-08|Rejet d'une vente supérieure à la quantité détenue|À valider|
|**RG-11** (Cours manquant)|TC-FUNC-09|Valorisation au PRU et indicateur de cours manquant|À valider|
|**RG-12** (Suppression support)|TC-FUNC-10|Suppression interdite d'un support utilisé et archivage|À valider|
|**RG-13** (Recalcul rétroactif)|TC-FUNC-11, TC-FUNC-12, TC-FUNC-28|Recalcul des snapshots, rejet d'une modification incohérente, absence de recalcul sur un référentiel|À valider|
|**RG-14** (Snapshot quotidien)|TC-FUNC-13|Un seul snapshot par jour, écrasé|À valider|
|**RG-15** (Variation du mois)|TC-FUNC-14|Calcul de la variation du mois et cas N/A|À valider|
|**RG-16** (Compte d'accès unique)|TC-FUNC-15|Initialisation unique du compte d'accès|À valider|
|**RG-17** (JWT 8 h)|TC-TECH-01|Protection des endpoints et expiration du jeton|À valider|
|**RG-18** (Blocage)|TC-FUNC-16|Blocage de 15 minutes après 5 échecs|À valider|
|**RG-19** (Sauvegardes)|TC-TECH-02, TC-FUNC-28|Sauvegarde avant écriture et rétention de 100 versions|À valider|
|**RG-20** (Logs sans secret)|TC-TECH-03|Absence de secrets dans les logs, rotation journalière|À valider|
|**RG-21** (Compte titres requis, type figé)|TC-FUNC-17|Mouvements interdits sur un compte espèces et type figé|À valider|
|**RG-22** (Liquidités non automatiques)|TC-FUNC-18|Les mouvements ne modifient pas les liquidités|À valider|
|**RG-23** (Cours et solde retenus)|TC-FUNC-19|Sélection du dernier cours ou solde à date|À valider|
|**RG-24** (Valeurs issues des référentiels)|TC-FUNC-20, TC-FUNC-29|Zone, secteur ou établissement inexistant ou archivé|À valider|
|**RG-25** (Pas de date future)|TC-FUNC-21|Rejet des dates futures|À valider|
|**RG-26** (Unicité)|TC-FUNC-22|Unicité du code support et du nom de compte|À valider|
|**RG-27** (Compte archivé)|TC-FUNC-07, TC-FUNC-23|Exclusion puis réintégration d'un compte archivé|À valider|
|**RG-28** (Filtres et liquidités)|TC-FUNC-24|Exclusion des espèces quand un filtre de support est actif|À valider|
|**RG-29** (Arrondis)|TC-FUNC-25|Arrondi au demi supérieur à l'affichage uniquement|À valider|
|**RG-30** (Gestion des référentiels)|TC-FUNC-28, TC-FUNC-29|Création, modification, archivage et suppression des valeurs de référentiel|À valider|
|Atomicité (TS 2.5)|TC-TECH-04|Aucune écriture partielle en cas d'échec|À valider|
|Réseau local (TS 1.2)|TC-TECH-05|Écoute sur localhost uniquement et CORS restreint|À valider|
|Format d'erreur (FS 4.1)|TC-TECH-06|Format d'erreur unique et codes HTTP|À valider|
|Performance (BRD 4.1)|TC-TECH-07|Temps de réponse inférieur à 2 s sur le volume cible|À valider|
|Courbe (UC-12)|TC-FUNC-26|Courbe d'évolution par période et historique insuffisant|À valider|
|Historique mouvements (UC-09)|TC-FUNC-27|Filtrage de l'historique des mouvements|À valider|
|Référentiels (UC-14)|TC-FUNC-28, TC-FUNC-29|Gestion des référentiels|À valider|
|Initialisation des référentiels (FS 3.11, TS 2.4)|TC-FUNC-30|Valeurs initiales créées une seule fois|À valider|

---

## 3. Fiches de Test Détaillées (Test Sheets)

### 3.1 Tests Fonctionnels (FUNC)

#### [TC-FUNC-01] Absence de champ devise et affichage en euros

- **Objectif** : vérifier qu'aucune devise n'est saisie et que tous les montants s'affichent en euros.
- **Règles testées** : RG-01
- **Type** : E2E
- **Prérequis** : JDR chargé.
- **Procédure** :
    1. Ouvrir les formulaires de mouvement, de solde et de cours.
    2. Ouvrir l'accueil et le tableau de bord.
- **Résultat attendu** :
    - Aucun champ devise n'existe dans les formulaires.
    - Tous les montants s'affichent au format `1 234,56 €`.

#### [TC-FUNC-02] Rejet des saisies dépassant la précision autorisée

- **Objectif** : vérifier les limites de décimales.
- **Règles testées** : RG-02
- **Type** : TU (validateurs) et TI
- **Données d'entrée et résultat attendu** :

|Cas|Entrée|Attendu|
|---|---|---|
|a|ACHAT sur S1, quantité 1.123456789|400 `VALIDATION_ERROR` sur `quantity`|
|b|ACHAT sur S1, quantité 1.12345678, prix 100.123|400 sur `unitPrice`|
|c|ACHAT sur S2 (crypto), quantité 0.00012345, prix 58000.12345678|201|
|d|ACHAT sur S1, frais 1.999|400 sur `fees`|
|e|Cours de S1 à 104.125|400|
|f|Cours de S2 à 58000.12345678|200|
|g|Solde de 100.001|400|

#### [TC-FUNC-03] Calcul du PRU en PMP frais inclus

- **Objectif** : vérifier le PRU après des achats successifs, puis après une vente.
- **Règles testées** : RG-03, RG-04
- **Type** : TU (`PositionCalculator`) et TI (`/dashboard/positions`)
- **Données d'entrée** : M1, M2, M3 du JDR.
- **Procédure** : calculer la position avec `asOf` = 15/01, puis 15/02, puis 15/03/2026.
- **Résultat attendu** :
    - Au 15/01, PRU = 100,20.
    - Au 15/02, PRU = 103,5333… (API : 103.53).
    - Au 15/03, PRU = 103,5333… (inchangé) et quantité = 9.

#### [TC-FUNC-04] Remise à zéro d'une position soldée

- **Objectif** : vérifier qu'une position vendue en totalité disparaît et que le PRU repart de zéro.
- **Règles testées** : RG-04
- **Type** : TU
- **Données d'entrée** :
    - JDR ;
    - M4 : VENTE, 01/04/2026, A, S1, quantité 9, prix 125,00, frais 0 ;
    - M5 : ACHAT, 01/05/2026, A, S1, quantité 2, prix 130,00, frais 1,00.
- **Résultat attendu** :
    - Au 15/04, la position S1 est absente du tableau des positions (quantité 0).
    - La PV réalisée cumulée vaut 97,30 + 9 × (125 − 103,5333…) = 97,30 + 193,20 = 290,50.
    - Au 15/05, PRU = (2 × 130 + 1) ÷ 2 = 130,50.

#### [TC-FUNC-05] Calcul de la plus-value réalisée

- **Objectif** : vérifier la PV réalisée, frais de vente déduits.
- **Règles testées** : RG-05
- **Type** : TU et TI (`/dashboard/accounts`)
- **Données d'entrée** : JDR.
- **Résultat attendu** : la PV réalisée vaut 97,30 pour S1, pour le compte A et au global.

#### [TC-FUNC-06] Calcul de la plus-value latente en € et en %

- **Règles testées** : RG-06
- **Type** : TU et TI
- **Données d'entrée** : JDR, avec une date d'évaluation au 23/09/2026.
- **Résultat attendu** :
    - `unrealizedGain` = 103.20 et `unrealizedGainPercent` = 11.08 sur la position S1.
    - `summary.unrealizedGain.amount` = 103.20.

#### [TC-FUNC-07] Valeur des comptes et patrimoine total

- **Règles testées** : RG-07, RG-08, RG-09, RG-27
- **Type** : TI
- **Données d'entrée** : JDR.
- **Procédure** : appeler `GET /api/dashboard/accounts`, puis `GET /api/dashboard/summary`.
- **Résultat attendu** :
    - Compte A = 1 235,00 ; compte B = 5 000,00.
    - Le compte C est absent de la liste.
    - `totalNetWorth` = 6 235,00.
    - `byAccountType` : PEA 1 235,00 (19,81 %) et LIVRET 5 000,00 (80,19 %).

#### [TC-FUNC-08] Rejet d'une vente supérieure à la quantité détenue

- **Règles testées** : RG-10
- **Type** : TI et E2E
- **Données d'entrée** : JDR, puis POST d'une VENTE au 20/09/2026 sur A, S1, quantité 10, prix 115.
- **Résultat attendu** :
    - Réponse 422 `INSUFFICIENT_QUANTITY`, avec `details.availableQuantity` = 9.
    - `movements.json` est inchangé et aucune sauvegarde n'a été créée.
    - Dans l'interface, le message affiche la quantité disponible.
- **Variante (même date)** : un ACHAT de 2 et une VENTE de 11, tous deux datés du 20/09 :
    - si l'achat est créé avant la vente, la vente est acceptée (sequence) ;
    - si la vente est saisie seule avant l'achat, elle est rejetée.

#### [TC-FUNC-09] Valorisation au PRU et indicateur de cours manquant

- **Règles testées** : RG-11
- **Type** : TI et E2E
- **Données d'entrée** : JDR, plus un ACHAT au 15/09/2026 sur A, S2, quantité 0.01, prix 50 000,00, frais 5,00. Aucun cours n'est saisi pour S2.
- **Résultat attendu** :
    - Pour la position S2 : PRU = (500 + 5) ÷ 0,01 = 50 500,00 ; `marketValue` = 505,00 ; `unrealizedGain` = 0 ; `missingPrice` = true.
    - `summary.missingPriceCount` = 1.
    - Le bandeau « 1 position(s) sans cours, valorisée(s) au PRU » s'affiche.

#### [TC-FUNC-10] Suppression interdite d'un support utilisé et archivage

- **Règles testées** : RG-12
- **Type** : TI et E2E
- **Procédure** :
    1. `DELETE /api/securities/{S1}`.
    2. `POST /api/securities/{S1}/archive`.
    3. POST d'un ACHAT sur S1.
    4. Créer un support S3 sans mouvement, puis le supprimer.
- **Résultat attendu** :
    1. 409 `CONFLICT`.
    2. 200 ; S1 reste visible dans l'historique et dans les positions.
    3. 400 (support archivé).
    

#### [TC-FUNC-11] Recalcul rétroactif des snapshots

- **Règles testées** : RG-13
- **Type** : TI
- **Prérequis** : JDR, avec des snapshots existants au 01/09/2026 et au 15/09/2026 (générés en simulant `IClock`), plus celui du 23/09.
- **Données d'entrée** : saisir un cours de S1 à 105,00 au 10/09/2026.
- **Résultat attendu** :
    - Le snapshot du 01/09 est inchangé.
    - Le snapshot du 15/09 est recalculé avec le cours de S1 à 105,00, soit une valeur de A de 9 × 105 + 200 = 1 145,00.
    - Le snapshot du 23/09 reste au cours de 115,00.

#### [TC-FUNC-12] Rejet d'une modification rétroactive incohérente

- **Règles testées** : RG-13, RG-10
- **Type** : TI
- **Données d'entrée** : JDR, puis `DELETE /api/movements/{M1}`.
- **Résultat attendu** :
    - 422 `INSUFFICIENT_QUANTITY`, avec `conflictingMovementId` = M3 (au 10/03, 5 sont disponibles pour 6 vendus).
    - Les fichiers `movements.json` et `snapshots.json` sont inchangés.

#### [TC-FUNC-13] Un seul snapshot par jour, écrasé

- **Règles testées** : RG-14
- **Type** : TI
- **Procédure** : le 23/09 (avec `IClock` simulé), saisir successivement un cours de S1 à 115, puis à 118.
- **Résultat attendu** :
    - `snapshots.json` contient un seul élément daté du 23/09.
    - Sa valeur `totalNetWorth` vaut 9 × 118 + 200 + 5 000 = 6 262,00.
- **Variante** : simuler 23:59 puis 00:01 (heure de Paris) : deux snapshots distincts sont créés.

#### [TC-FUNC-14] Calcul de la variation du mois et cas N/A

- **Règles testées** : RG-15
- **Type** : TU (`DashboardService`)
- **Données d'entrée et résultat attendu** :

|Cas|Données|Attendu|
|---|---|---|
|a|Snapshots au 31/08 (6 000,00) et au 23/09 (6 235,00)|`amount` 235.00, `percent` 3.92, `referenceDate` 2026-08-31|
|b|Snapshots au 15/08 (5 800,00) et au 23/09|La référence est le 15/08 ; `amount` 435.00|
|c|Uniquement des snapshots de septembre|`monthVariation` = null, affiché « N/A »|
|d|Snapshot de référence avec un total de 0|`amount` calculé, `percent` null|

#### [TC-FUNC-15] Initialisation unique du compte d'accès

- **Règles testées** : RG-16
- **Type** : TI et E2E
- **Procédure** :
    1. Sans `credentials.json`, `GET /auth/status`.
    2. Setup avec un mot de passe de 11 caractères.
    3. Setup avec une confirmation différente.
    4. Setup valide.
    5. Nouveau setup.
- **Résultat attendu** :
    1. `initialized` = false.
    
    2. 201 ; le fichier contient `passwordHash` au format `$2a$12$…` et aucun mot de passe en clair.
    

#### [TC-FUNC-16] Blocage de 15 minutes après 5 échecs

- **Règles testées** : RG-18
- **Type** : TU (`LoginAttemptTracker`, horloge simulée) et TI
- **Procédure** :
    1. 4 échecs, puis un login valide.
    2. 5 échecs consécutifs.
    3. Un login valide à t + 14 min 59 s.
    4. Un login valide à t + 15 min 01 s.
- **Résultat attendu** :
    1. Le login réussit et le compteur est remis à 0.
    2. Le 5e échec renvoie 401. La tentative suivante renvoie 423, avec `retryAfterSeconds` ≈ 900.
    

#### [TC-FUNC-17] Mouvements interdits sur un compte espèces et type figé

- **Règles testées** : RG-21
- **Type** : TI
- **Procédure** :
    1. POST d'un ACHAT sur le compte B (LIVRET).
    2. POST d'un VERSEMENT sur B.
    3. `PUT /accounts/{A}` avec le type `CTO`.
    4. Créer un compte D (CTO, établissement E1) vide, puis passer son type à `PEA`.
- **Résultat attendu** :
    1. 400.
    

#### [TC-FUNC-18] Les mouvements ne modifient pas les liquidités

- **Règles testées** : RG-22
- **Type** : TI
- **Données d'entrée** : JDR, plus un VERSEMENT de 1 000,00 au 20/09/2026 sur A.
- **Résultat attendu** :
    - Les liquidités de A restent à 200,00 et la valeur de A reste à 1 235,00.
    - Le versement apparaît dans l'historique des mouvements.

#### [TC-FUNC-19] Sélection du dernier cours ou solde à date

- **Règles testées** : RG-23
- **Type** : TU (`ValuationCalculator`)
- **Données d'entrée** : cours de S1 à 100 au 01/09, 110 au 10/09 et 115 au 22/09.
- **Résultat attendu** :
    - Évaluation au 09/09 → 100.
    - Au 10/09 → 110.
    - Au 21/09 → 110.
    - Au 23/09 → 115.
    - Même logique pour les soldes : B vaut 0 au 31/08 et 5 000 au 01/09.

#### [TC-FUNC-20] Zone ou secteur inexistant ou archivé sur un support

- **Règles testées** : RG-24
- **Type** : TI et E2E
- **Procédure** :
    1. POST d'un support avec la zone `AFRIQUE` (absente du référentiel).
    2. POST d'un support avec le secteur `luxe`.
    3. Archiver la zone `EUROPE`, puis POST d'un support avec la zone `EUROPE`.
    4. Archiver la zone `MONDE`, puis `PUT /api/securities/{S1}` en changeant uniquement le nom (zone `MONDE` inchangée).
    5. `PUT /api/securities/{S1}` avec la zone `EUROPE` (archivée).
    6. Ouvrir le formulaire de modification de S1 dans l'interface.
- **Résultat attendu** :
    1. 400.
    
    2. 200 : S1 conserve la zone archivée `MONDE`.
    
    3. La liste des zones ne propose pas `EUROPE` ; la zone actuelle s'affiche « Monde (archivé) ».

#### [TC-FUNC-21] Rejet des dates futures

- **Règles testées** : RG-25
- **Type** : TI
- **Procédure** : avec aujourd'hui = 23/09/2026, envoyer un mouvement, un solde et un cours datés du 24/09/2026, puis les mêmes datés du 23/09/2026.
- **Résultat attendu** : 400 pour les trois saisies datées du 24/09 ; succès pour les trois datées du 23/09.

#### [TC-FUNC-22] Unicité du code support et du nom de compte

- **Règles testées** : RG-26
- **Type** : TI
- **Procédure** :
    1. Créer un support avec le code `lu1681043599` (minuscules).
    2. Créer un compte nommé « Livret A » (type `LIVRET`, établissement E2).
- **Résultat attendu** : 409 dans les deux cas.

#### [TC-FUNC-23] Exclusion puis réintégration d'un compte archivé

- **Règles testées** : RG-27
- **Type** : TI et E2E
- **Procédure** :
    1. Désarchiver C.
    2. Vérifier `summary`.
    3. Archiver A.
    4. Tenter un ACHAT sur A.
- **Résultat attendu** : 2. Total = 6 235 + 1 000 = 7 235,00. 3. Total = 6 000,00 ; A reste visible dans l'historique des mouvements. 4. 400.

#### [TC-FUNC-24] Exclusion des espèces quand un filtre de support est actif

- **Règles testées** : RG-28
- **Type** : TI
- **Procédure** :
    1. `GET /dashboard/positions` sans filtre.
    2. Avec `zones=MONDE`.
    3. Avec `accountIds={A}` seul.
- **Résultat attendu** :
    1. `includesCash` = true ; `byAccount` inclut B et les liquidités de A (total 6 235,00) ; `byZone` contient « Espèces et liquidités » (5 200,00) et « Monde » (1 035,00).
    2. `includesCash` = false ; les répartitions ne contiennent que les positions (1 035,00).
    3. `includesCash` = true ; A vaut 1 235,00.

#### [TC-FUNC-25] Arrondi au demi supérieur à l'affichage uniquement

- **Règles testées** : RG-29
- **Type** : TU
- **Données d'entrée** :
    - valeur interne 0,125 → API 0,13 ;
    - PRU interne 103,5333… → API 103,53 ;
    - enchaînement M1, M2, M3 : la PV réalisée est calculée à partir du PRU non arrondi.
- **Résultat attendu** :
    - Les valeurs d'API sont conformes.
    - La PV réalisée vaut 97,30. Avec le PRU arrondi, on aurait obtenu 6 × (120 − 103,53) − 1,5 = 97,32, ce qui est rejeté.

#### [TC-FUNC-26] Courbe d'évolution par période et historique insuffisant

- **Règles testées** : UC-12
- **Type** : TI et E2E
- **Données d'entrée** : snapshots au 01/01/2026, 20/08/2026, 01/09/2026 et 23/09/2026.
- **Résultat attendu** :
    - `1M` → 2 points (01/09, 23/09).
    - `1A` → 4 points.
    - `ALL` → 4 points, triés par date croissante.
    - Avec un seul snapshot, l'interface affiche « Historique insuffisant pour tracer une courbe ».

#### [TC-FUNC-27] Filtrage de l'historique des mouvements

- **Règles testées** : UC-09
- **Type** : TI
- **Procédure** :
    1. `GET /movements?type=ACHAT`.
    2. `GET /movements?from=2026-02-01&to=2026-03-31`.
    3. `GET /movements?securityId={S2}`.
- **Résultat attendu** :
    1. M2 et M1, dans cet ordre (tri par date décroissante).
    2. M3 et M2.
    3. Liste vide ; l'interface affiche « Aucun mouvement pour ces critères ».

#### [TC-FUNC-28] Gestion des zones et des secteurs

- **Objectif** : vérifier la création, la modification, l'archivage et la suppression des valeurs d'un référentiel.
- **Règles testées** : RG-30, RG-13, RG-19, UC-14
- **Type** : TU (`ReferentialService`), TI et E2E
- **Prérequis** : JDR, avec un snapshot au 23/09.
- **Procédure** :
    1. `POST /api/referentials/sectors` avec `{ "code": "LUXE", "label": "Luxe" }`.
    2. POST avec `{ "code": "luxe", "label": "Luxe 2" }`, puis avec `{ "code": "LUXE_2", "label": "luxe" }`, puis avec `{ "code": "LUXE", "label": "Autre" }`.
    3. `PUT` du secteur `LUXE` avec `{ "code": "LUXURY", "label": "Luxe" }`.
    4. `PUT` de la zone `MONDE` avec `{ "code": "MONDE", "label": "Monde entier" }`.
    5. `PUT` de la zone `MONDE` avec `{ "code": "WORLD", "label": "Monde entier" }`.
    6. `DELETE` de la zone `MONDE`, puis du secteur `LUXURY`.
    7. Archiver le secteur `DIVERSIFIE`, puis `GET /api/referentials/sectors` et `GET /api/dashboard/positions`.
    8. Désarchiver le secteur `DIVERSIFIE`.
- **Résultat attendu** :
    1. 201 ; `usageCount` = 0 ; une sauvegarde de `sectors.json` est créée dans `data/backups/sectors/`.
    2. 400 (format du code) ; 409 (libellé en double, casse ignorée) ; 409 (code en double).
    3. 200 (valeur non utilisée : code modifiable).
    4. 200 ; `zoneLabel` vaut « Monde entier » dans `/securities` et `/dashboard/positions` ; `snapshots.json` est inchangé (aucun recalcul).
    5. 409, avec `details.usageCount` = 2.
    6. 409 pour `MONDE` ; 204 pour `LUXURY`.
    7. Sans `includeArchived`, `DIVERSIFIE` est absent de la liste ; avec `includeArchived=true`, il apparaît avec `archived` = true. S1 conserve le secteur `DIVERSIFIE` et la répartition par secteur est inchangée (1 035,00).
    8. 200 ; `DIVERSIFIE` est à nouveau proposé à la saisie.
- **Contrôle E2E** : dans l'écran « Référentiels », la corbeille n'est proposée que pour les valeurs non utilisées, et le champ code est verrouillé à la modification d'une valeur utilisée.

#### [TC-FUNC-29] Établissement obligatoire d'un compte

- **Règles testées** : RG-24, RG-30, UC-03, UC-14
- **Type** : TI et E2E
- **Procédure** :
    1. `GET /api/referentials/institutions`.
    2. Créer un compte D (CTO) sans `institutionId`, puis avec `institutionId` = `null`.
    3. Créer un compte D (CTO) avec un `institutionId` inexistant.
    4. Créer un compte D (CTO) avec `institutionId` = E2.
    5. `DELETE /api/referentials/institutions/{E1}`.
    6. Archiver E1, puis créer un compte F (CTO) avec `institutionId` = E1.
    7. `PUT /api/accounts/{A}` en changeant uniquement le nom (E1 inchangé).
    8. `PUT /api/accounts/{A}` avec `institutionId` = `null`.
    9. `POST /api/referentials/institutions` avec `{ "code": "BOURSO", "label": "Bourse Direct" }`.
    10. Dans l'interface, archiver E1 et E2, puis ouvrir le formulaire de création d'un compte.
- **Résultat attendu** :
    1. E1 a un `usageCount` = 2 (A et C, archivé compris) ; E2 a un `usageCount` = 1.
    2. 400 dans les deux cas.
    
    3. 201 ; la réponse porte `institutionName` = « Banque Test » ; E2 passe à `usageCount` = 2.
    4. 409, avec `details.usageCount` = 2.
    
    5. 200 ; A conserve E1, affiché « Boursorama (archivé) » dans son formulaire.
    
    6. 400 (le code est interdit pour un établissement).
    7. Aucun établissement n'est proposé ; le formulaire invite à en créer un dans l'écran « Référentiels ».

#### [TC-FUNC-30] Initialisation des référentiels par défaut

- **Règles testées** : FS 3.11, TS 2.4
- **Type** : TI
- **Procédure** :
    1. Démarrer l'API sur un dossier de données vide.
    2. Appeler `GET /api/referentials/zones`, `sectors` et `institutions`.
    3. Supprimer toutes les valeurs non utilisées des zones, puis redémarrer l'API.
    4. Supprimer `sectors.json` à la main, puis redémarrer l'API.
- **Résultat attendu** :
    1. `zones.json`, `sectors.json` et `institutions.json` sont créés ; aucune sauvegarde n'est prise ; l'initialisation est tracée dans les logs.
    2. 5 zones, 13 secteurs (codes de la spécification technique, section 2.4) et 0 établissement.
    3. Les zones supprimées ne sont pas recréées : un fichier existant n'est jamais modifié.
    4. `sectors.json` est recréé avec les 13 valeurs initiales.

---

### 3.2 Tests Techniques (TECH)

#### [TC-TECH-01] Protection des endpoints et expiration du jeton

- **Règles testées** : RG-17
- **Type** : TI
- **Procédure** :
    1. `GET /api/accounts` sans jeton.
    2. Avec un jeton valide.
    3. Avec un jeton dont la date `exp` est dépassée (horloge simulée à + 8 h 01).
    4. Avec un jeton signé par une autre clé.
    5. `GET /api/referentials/zones` sans jeton.
- **Résultat attendu** :
    
    1. 401.
    
    2. 401 ; dans l'interface, redirection vers `/login`.
    
    - Le jeton émis au login porte `exp` = `iat` + 28 800 s.

#### [TC-TECH-02] Sauvegarde avant écriture et rétention de 100 versions

- **Règles testées** : RG-19
- **Type** : TI
- **Procédure** :
    1. Créer un compte (premier fichier `accounts.json`).
    2. Modifier ce compte.
    3. Effectuer 105 modifications supplémentaires.
- **Résultat attendu** :
    
    1. Aucune sauvegarde (le fichier n'existait pas).
    2. Une sauvegarde contenant l'état **avant** modification.
    3. `data/backups/accounts/` contient exactement 100 fichiers, les plus récents.
    
    - Aucune sauvegarde n'est créée pour `credentials.json`.

#### [TC-TECH-03] Absence de secrets dans les logs, rotation journalière

- **Règles testées** : RG-20
- **Type** : TI
- **Procédure** :
    1. Setup, login réussi, login échoué, puis un appel métier.
    2. Rechercher dans les logs le mot de passe en clair, la chaîne `$2a$` et le jeton.
    3. Simuler un changement de jour.
- **Résultat attendu** :
    - Aucune occurrence des secrets.
    - Le fichier `logs/financereport-YYYYMMDD.log` existe, au format `[yyyy-MM-dd HH:mm:ss.fff zzz] [INF] [...]`.
    - Un nouveau fichier est créé le jour suivant.
    - L'échec de connexion est tracé en `WRN`.

#### [TC-TECH-04] Aucune écriture partielle en cas d'échec

- **Règles testées** : TS 2.5
- **Type** : TI (injection de fautes via un `IFileSystem` simulé)
- **Procédure** : faire échouer l'écriture de `snapshots.json` après l'écriture réussie de `movements.json`, lors de la création d'un mouvement.
- **Résultat attendu** :
    - Réponse 500 `INTERNAL_ERROR`.
    - `movements.json` est restauré à son état initial.
    - Le cache est rechargé : un `GET` ne renvoie pas le mouvement.
    - Aucun fichier `.tmp` résiduel.

#### [TC-TECH-05] Écoute sur localhost uniquement et CORS restreint

- **Règles testées** : TS 1.2
- **Type** : TI et manuel
- **Procédure** :
    1. Appeler l'API depuis une autre machine du réseau local, via l'IP du poste.
    2. Envoyer une requête preflight avec `Origin: http://evil.local`.
    3. Envoyer une requête preflight avec `Origin: http://localhost:4200`.
- **Résultat attendu** :
    1. Connexion refusée.
    2. Pas d'en-tête `Access-Control-Allow-Origin`.
    3. En-tête présent.

#### [TC-TECH-06] Format d'erreur unique et codes HTTP

- **Règles testées** : FS 4.1
- **Type** : TI
- **Procédure** : provoquer chaque code d'erreur (400, 401, 404, 409, 422, 423, 500), dont un 404 sur `GET /api/referentials/pays` (référentiel inconnu).
- **Résultat attendu** :
    - Chaque corps respecte `{ error, message, details? }`, avec le code attendu et un message en français.
    - Aucune stack trace n'est exposée dans une réponse 500.

#### [TC-TECH-07] Temps de réponse inférieur à 2 s sur le volume cible

- **Règles testées** : BRD 4.1
- **Type** : TI (performance)
- **Prérequis** : jeu généré de 20 comptes, 200 supports, 10 000 mouvements, 50 000 cours et 3 650 snapshots.
- **Procédure** :
    1. Mesurer `summary`, `history?period=ALL` et `positions` (10 appels chacun, API démarrée à chaud).
    2. Mesurer une modification rétroactive au premier jour, qui recalcule 3 650 snapshots.
- **Résultat attendu** :
    - Le p95 est inférieur à 2 s pour chaque endpoint de restitution.
    - La modification rétroactive se termine en moins de 5 s. Ce seuil technique est proposé et doit être validé.