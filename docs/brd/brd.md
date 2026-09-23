# Business Requirements Document (BRD) – FinanceReport

|Élément|Valeur|
|---|---|
|Version|1.1 – MVP|
|Date|23/09/2026|
|Auteur|Guillaume Chataignier|
|Statut|En attente de validation|
|Source|BRD de base + brainstorming du 22/09/2026 (`BRD_enrichi.md`) + revue des maquettes du 23/09/2026|

**Historique des versions**

|Version|Date|Modifications|
|---|---|---|
|1.0|22/09/2026|Version initiale|
|1.1|23/09/2026|Référentiels administrables par l'utilisateur (zones géographiques, secteurs, établissements) : EM-16, EM-02 et EM-05 mis à jour.|

---

## 1. Objectifs du Projet & Contexte

### 1.1 Contexte Métier

L'utilisateur détient des avoirs répartis sur plusieurs établissements et enveloppes : compte courant, livrets d'épargne, compte-titres ordinaire (CTO), PEA, portefeuille crypto. Les investissements portent sur différents types de supports (ETF, actions, obligations, cryptomonnaies).

Aujourd'hui, il n'existe aucune vue consolidée. Il est donc impossible de savoir simplement :

- combien vaut le patrimoine total à une date donnée et comment il évolue ;
- ce que rapporte réellement chaque placement par rapport à son prix d'achat ;
- comment le patrimoine est réparti (par compte, type de support, zone géographique, secteur).

### 1.2 Objectif Principal

Fournir une application web personnelle, exécutée en local, qui permet de :

1. **Suivre l'évolution du patrimoine net dans le temps**, grâce à un historique quotidien consultable sous forme de courbe.
2. **Mesurer la performance des placements**, par le prix de revient unitaire et les plus-values latentes et réalisées.
3. **Visualiser l'allocation**, par compte, type de support, zone géographique et secteur.

L'objectif mesurable du MVP : **après chaque mise à jour des soldes et des cours, l'utilisateur obtient en moins de 2 secondes son patrimoine total, sa variation sur le mois et sa plus-value latente globale.**

### 1.3 Bénéfices Attendus

- Une vue unique et consolidée de tous les comptes, qui remplace les consultations dispersées.
- Un historique fiable et conservé du patrimoine, protégé contre la perte grâce à des sauvegardes versionnées.
- Une mesure objective de la performance, pour éclairer les futures décisions d'investissement.
- Une maîtrise complète des données : stockage local, sans service tiers.

---

## 2. Périmètre du Projet (Scope)

### 2.1 Éléments Inclus (In-Scope) – MVP

- **Authentification** : un compte utilisateur unique, protégé par un mot de passe.
- **Gestion des comptes** : compte courant, livret d'épargne, CTO, PEA, portefeuille crypto, autre, rattachés à un établissement.
- **Gestion des supports** : ETF, action, obligation, crypto, avec une zone géographique et un secteur choisis dans des référentiels.
- **Référentiels administrables** : zones géographiques, secteurs et établissements, créés et modifiés par l'utilisateur.
- **Historique des mouvements** : achat, vente, versement et retrait, consultables et filtrables.
- **Saisie manuelle des cours** des supports.
- **Calcul des positions** : quantité détenue, PRU, valorisation.
- **Calcul des plus-values** latentes et réalisées, par position, par compte et au global.
- **Snapshots quotidiens** du patrimoine, pour constituer l'historique.
- **Sauvegarde versionnée** des fichiers de données.
- **Page d'accueil** avec les indicateurs clés.
- **Courbe d'évolution** du patrimoine net.
- **Tableau de bord** d'analyse, avec graphiques, tableaux et filtres.

### 2.2 Éléments Exclus (Out-of-Scope)

- **Exclus du produit** : budget, suivi des dépenses et catégorisation des transactions bancaires ; usage multi-utilisateur ; immobilier et crédits.
- **Exclus du MVP**, reportés aux lots suivants :
    - **Lot 1 : Performance** – récupération automatique des cours par API, TRI, décomposition versements/marché, vue cascade, classement des positions, performance par achat, rappel de saisie mensuel.
    - **Lot 2 : Allocation et pilotage** – allocation cible, suggestion de rééquilibrage, score de concentration, journal d'investissement, alertes.
    - **Lot 3 : Confort** – affichage responsive mobile, mode discret, export PDF, stress test.
    - **Lot 4 : Priorité basse** – import CSV des courtiers, simulateur de projection, suivi de l'ancienneté du PEA.
- **Exclus du MVP, sans lot planifié** : multi-devises, dividendes et coupons, hébergement sur un serveur distant ; administration des types de compte et des types de support (listes fixes, car des règles de calcul en dépendent).

---

## 3. Exigences Métier (Business Requirements)

### 3.1 Fonctionnalités Clés

- **EM-01 – Authentification** : l'accès à l'application nécessite une connexion avec un identifiant et un mot de passe. Le compte unique est créé au premier lancement.
- **EM-02 – Gestion des comptes** : l'utilisateur crée, modifie et archive ses comptes, en précisant le nom, le type et l'établissement (choisi dans le référentiel des établissements, EM-16).
- **EM-03 – Solde des comptes non-titres** : pour un compte courant ou un livret, l'utilisateur saisit un solde daté.
- **EM-04 – Liquidités des comptes titres** : pour un CTO, un PEA ou un portefeuille crypto, l'utilisateur saisit un solde de liquidités daté.
- **EM-05 – Référentiel des supports** : l'utilisateur crée et modifie les supports, en précisant le nom, le code ISIN ou le ticker, le type (ETF, action, obligation, crypto), la zone géographique et le secteur économique. La zone et le secteur sont choisis dans les référentiels (EM-16).
- **EM-06 – Enregistrement des mouvements** : l'utilisateur enregistre un achat ou une vente (date, compte, support, quantité, prix unitaire, frais), ou un versement ou retrait d'espèces sur un compte titres (date, compte, montant).
- **EM-07 – Consultation des mouvements** : l'utilisateur consulte l'historique des mouvements et le filtre par compte, support, type de mouvement et période.
- **EM-08 – Saisie des cours** : l'utilisateur saisit le cours d'un support à une date donnée.
- **EM-09 – Positions** : le système calcule, pour chaque couple compte et support, la quantité détenue, le PRU, le dernier cours connu et la valorisation.
- **EM-10 – Plus-values** : le système calcule la plus-value latente de chaque position et la plus-value réalisée lors de chaque vente, puis les agrège par compte et au global.
- **EM-11 – Historique du patrimoine** : le système enregistre un snapshot du patrimoine, avec le total et le détail par compte et par position, à chaque modification des données.
- **EM-12 – Sauvegarde** : le système conserve une copie horodatée des fichiers de données avant chaque écriture.
- **EM-13 – Page d'accueil** : elle affiche le patrimoine total, la variation du mois en euros et en pourcentage, la plus-value latente globale et la répartition par type de compte.
- **EM-14 – Courbe d'évolution** : elle affiche le patrimoine net sur 1 mois, 1 an ou depuis le début.
- **EM-15 – Tableau de bord** : il présente les données sous forme de graphiques (répartition par compte, type de support, zone, secteur) et de tableaux (positions, comptes). Des filtres permettent de choisir le compte, le type de support, la zone et le secteur.
- **EM-16 – Référentiels administrables** : l'utilisateur crée, renomme, archive et supprime les valeurs des référentiels des zones géographiques, des secteurs et des établissements. Les zones et les secteurs sont livrés avec une liste de valeurs par défaut. Une valeur déjà utilisée par un support ou un compte ne peut pas être supprimée, seulement archivée.

### 3.2 Règles Métier Fondamentales

- **RM-01 – Devise unique** : tous les montants sont exprimés en euros (EUR).
- **RM-02 – Précision** : les quantités sont stockées avec 8 décimales, les montants et les prix avec 2 décimales. Les prix unitaires des cryptomonnaies peuvent aller jusqu'à 8 décimales.
- **RM-03 – Calcul du PRU** : le PRU suit la méthode du prix moyen pondéré (PMP), frais d'achat inclus :
    - à l'achat : PRU = (quantité détenue × PRU actuel + quantité achetée × prix + frais) ÷ (quantité détenue + quantité achetée) ;
    - une vente ne modifie pas le PRU.
- **RM-04 – Plus-value réalisée** : lors d'une vente, plus-value réalisée = quantité vendue × (prix de vente − PRU) − frais de vente.
- **RM-05 – Plus-value latente** : plus-value latente = quantité détenue × (dernier cours − PRU).
- **RM-06 – Valeur d'un compte titres** : somme des valorisations de ses positions, plus le solde de liquidités saisi.
- **RM-07 – Valeur d'un compte non-titres** : dernier solde saisi.
- **RM-08 – Patrimoine total** : somme des valeurs de tous les comptes non archivés.
- **RM-09 – Vente excédentaire** : une vente portant sur une quantité supérieure à la quantité détenue à la date de la vente est rejetée, avec un message d'erreur.
- **RM-10 – Cours manquant** : une position sans cours saisi est valorisée au PRU. Un indicateur « cours manquant » est alors affiché.
- **RM-11 – Suppression d'un support** : un support lié à au moins un mouvement ne peut pas être supprimé, seulement archivé. Un support archivé n'est plus proposé à la saisie, mais reste visible dans l'historique.
- **RM-12 – Modification rétroactive** : la modification ou la suppression d'un mouvement passé est autorisée. Elle déclenche le recalcul du PRU, des plus-values et des snapshots à partir de la date du mouvement concerné. Si la modification rend une vente ultérieure excédentaire, elle est rejetée (voir RM-09).
- **RM-13 – Snapshot quotidien** : un seul snapshot est conservé par jour calendaire. Toute nouvelle modification dans la journée écrase le snapshot du jour.
- **RM-14 – Variation du mois** : variation = patrimoine actuel − patrimoine du dernier snapshot antérieur au 1er jour du mois en cours. S'il n'existe aucun snapshot antérieur, la variation n'est pas calculée et affiche « N/A ».

---

## 4. Contraintes et Exigences Non Fonctionnelles

### 4.1 Contraintes de Performance

- **Temps de réponse** : moins de 2 secondes pour afficher la page d'accueil et le tableau de bord.
- **Volume cible** : jusqu'à 20 comptes, 200 supports, 10 000 mouvements et 10 ans de snapshots quotidiens (environ 3 650 enregistrements).
- **Exécution** : en local sur le poste de l'utilisateur. L'API .NET et le front Angular sont séparés.

### 4.2 Contraintes de Sécurité et de Conformité

- **Compte unique** : l'identifiant et le mot de passe sont définis au premier lancement. Le mot de passe est haché avec BCrypt et stocké dans un fichier JSON dédié.
- **Session** : un jeton JWT d'une durée de 8 heures. Tous les endpoints de l'API, sauf la connexion et l'initialisation, exigent un jeton valide.
- **Protection contre la force brute** : blocage de 15 minutes après 5 échecs de connexion consécutifs.
- **Mot de passe oublié** : pas de procédure dans l'application. L'utilisateur supprime le fichier d'identifiants, puis recrée le compte au lancement suivant ; les données financières ne sont pas touchées.
- **Réseau** : l'API n'écoute que sur localhost.

### 4.3 Traçabilité et Auditabilité

- **Logs** : Serilog, fichier avec rotation journalière, niveau Information. Les erreurs sont tracées avec leur stack trace. Aucun mot de passe ni jeton n'est écrit dans les logs.
- **Sauvegardes** : avant chaque écriture, une copie horodatée du fichier modifié est créée. Les 100 dernières versions sont conservées par fichier ; les plus anciennes sont supprimées automatiquement.
- **Historisation** : les snapshots quotidiens constituent l'historique du patrimoine. Les mouvements portent une date de création et une date de dernière modification.
- **Stockage** : un fichier JSON par entité (identifiants, comptes, soldes, supports, mouvements, cours, snapshots, zones, secteurs, établissements).

### 4.4 Choix Technologiques Validés

- **Front** : Angular 18 ou plus, ngx-charts (licence MIT) pour les graphiques, interface en français.
- **Back** : .NET 8 LTS, API REST.