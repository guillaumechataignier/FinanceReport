import { expect, Page, test } from '@playwright/test';

/**
 * Parcours de recette sur le jeu de données de référence (TestPlan §1.3), saisi entièrement par l'interface,
 * puis contrôle des restitutions attendues. Les étapes partagent la même page et s'exécutent dans l'ordre.
 */
test.describe.configure({ mode: 'serial' });

const USERNAME = 'guillaume';
const PASSWORD = 'motdepasse-solide-2026';

let page: Page;

test.beforeAll(async ({ browser }) => {
  page = await browser.newPage();
});

test.afterAll(async () => {
  await page.close();
});

async function openPanelButton(name: string) {
  await page.getByRole('button', { name }).click();
}

async function sidePanel() {
  return page.locator('app-side-panel');
}

test('TC-FUNC-15 : création du compte d\'accès puis connexion', async () => {
  await page.goto('/');
  await expect(page).toHaveURL(/\/setup$/);

  await page.getByLabel('Identifiant').fill(USERNAME);
  await page.getByLabel('Mot de passe', { exact: true }).fill('trop-court');
  await page.getByLabel('Confirmation du mot de passe').fill('trop-court');
  await page.getByRole('button', { name: 'Créer' }).click();
  await expect(page.getByText('Les règles ci-dessous doivent toutes être respectées.')).toBeVisible();

  await page.getByLabel('Mot de passe', { exact: true }).fill(PASSWORD);
  await page.getByLabel('Confirmation du mot de passe').fill(PASSWORD);
  await page.getByRole('button', { name: 'Créer' }).click();
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByText('Compte créé.')).toBeVisible();

  await page.getByLabel('Identifiant').fill(USERNAME);
  await page.getByLabel('Mot de passe').fill('mauvais-mot-de-passe');
  await page.getByRole('button', { name: 'Se connecter' }).click();
  await expect(page.getByText('Identifiant ou mot de passe incorrect')).toBeVisible();

  await page.getByLabel('Mot de passe').fill(PASSWORD);
  await page.getByRole('button', { name: 'Se connecter' }).click();
  await expect(page.getByRole('heading', { name: 'Bonjour Guillaume' })).toBeVisible();
  await expect(page.getByText('Commencez par créer votre premier compte.')).toBeVisible();
});

test('UC-14 : établissements du JDR', async () => {
  await page.getByRole('link', { name: 'Référentiels' }).click();
  await page.getByRole('link', { name: /Établissements/ }).click();
  for (const name of ['Boursorama', 'Banque Test']) {
    await openPanelButton('Nouvel établissement');
    await (await sidePanel()).getByLabel('Nom').fill(name);
    await (await sidePanel()).getByRole('button', { name: 'Créer' }).click();
    await expect(page.getByRole('cell', { name, exact: true })).toBeVisible();
  }
});

test('UC-03 : comptes du JDR', async () => {
  await page.getByRole('link', { name: 'Comptes' }).click();
  for (const [name, type, institution] of [
    ['PEA Test', 'PEA', 'Boursorama'],
    ['Livret A', 'LIVRET', 'Banque Test'],
    ['Ancien CTO', 'CTO', 'Boursorama'],
  ]) {
    await openPanelButton('Nouveau compte');
    const panel = await sidePanel();
    await panel.getByLabel('Nom').fill(name);
    await panel.getByLabel('Type').selectOption(type);
    await panel.getByLabel('Établissement').selectOption({ label: institution });
    await panel.getByRole('button', { name: 'Créer le compte' }).click();
    await expect(page.getByRole('cell', { name })).toBeVisible();
  }
});

test('UC-05 : supports du JDR', async () => {
  await page.getByRole('link', { name: 'Supports' }).click();
  for (const [name, code, type, zone, sector] of [
    ['ETF Monde', 'LU1681043599', 'ETF', 'Monde', 'Diversifié'],
    ['Bitcoin', 'BTC', 'Crypto', 'Monde', 'Non applicable'],
  ]) {
    await openPanelButton('Nouveau support');
    const panel = await sidePanel();
    await panel.getByLabel('Nom').fill(name);
    await panel.getByLabel('Code (ISIN ou ticker)').fill(code);
    await panel.getByLabel('Type').selectOption({ label: type });
    await panel.getByLabel('Zone géographique').selectOption({ label: zone });
    await panel.getByLabel('Secteur').selectOption({ label: sector });
    await panel.getByRole('button', { name: 'Créer le support' }).click();
    await expect(page.getByRole('cell', { name, exact: true })).toBeVisible();
  }
});

test('UC-06 : mouvements M1 à M3, puis TC-FUNC-08 (vente excédentaire)', async () => {
  await page.getByRole('link', { name: 'Mouvements' }).click();
  const trades: [string, string, string, string, string][] = [
    ['Achat', '2026-01-10', '10', '100,00', '2,00'],
    ['Achat', '2026-02-10', '5', '110,00', '1,00'],
    ['Vente', '2026-03-10', '6', '120,00', '1,50'],
  ];

  for (const [type, date, quantity, price, fees] of trades) {
    await page.getByRole('button', { name: 'Nouveau mouvement' }).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByRole('button', { name: type, exact: true }).click();
    await dialog.getByLabel('Date').fill(date);
    await dialog.getByLabel('Compte titres').selectOption({ label: 'PEA Test' });
    await dialog.getByLabel('Support').selectOption({ label: 'ETF Monde · LU1681043599' });
    await dialog.getByLabel('Quantité').fill(quantity);
    await dialog.getByLabel('Prix unitaire').fill(price);
    await dialog.getByLabel('Frais').fill(fees);
    await dialog.getByRole('button', { name: 'Enregistrer' }).click();
    await expect(dialog).toBeHidden();
  }

  const rows = page.locator('tbody tr');
  await expect(rows).toHaveCount(3);
  await expect(rows.nth(0)).toContainText('718,50 €');
  await expect(rows.nth(2)).toContainText('1 002,00 €');

  await page.getByRole('button', { name: 'Nouveau mouvement' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.getByRole('button', { name: 'Vente', exact: true }).click();
  await dialog.getByLabel('Date').fill('2026-09-20');
  await dialog.getByLabel('Compte titres').selectOption({ label: 'PEA Test' });
  await dialog.getByLabel('Support').selectOption({ label: 'ETF Monde · LU1681043599' });
  await dialog.getByLabel('Quantité').fill('10');
  await dialog.getByLabel('Prix unitaire').fill('115,00');
  await dialog.getByRole('button', { name: 'Enregistrer' }).click();
  await expect(dialog.getByRole('status')).toContainText('Quantité insuffisante : 9 titres disponibles au 20/09/2026.');
  await dialog.getByRole('button', { name: 'Annuler' }).click();
});

test('UC-04 : soldes du JDR et archivage du compte C', async () => {
  await page.getByRole('link', { name: 'Comptes' }).click();
  for (const [account, date, amount] of [
    ['Livret A', '2026-09-01', '5 000,00'],
    ['Ancien CTO', '2026-01-01', '1 000,00'],
    ['PEA Test', '2026-09-01', '200,00'],
  ]) {
    await page.getByRole('row', { name: new RegExp(account) }).getByRole('button', { name: 'Historique des soldes' }).click();
    const panel = await sidePanel();
    await panel.getByLabel('Date').fill(date);
    await panel.getByLabel('Montant').fill(amount);
    await panel.getByRole('button', { name: 'Enregistrer le solde' }).click();
    await expect(panel.getByRole('cell', { name: `${amount} €` })).toBeVisible();
  }

  await page.getByRole('row', { name: /Ancien CTO/ }).getByRole('button', { name: 'Archiver' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Archiver' }).click();
  await expect(page.getByRole('row', { name: /Ancien CTO/ })).toBeHidden();
  // RG-11 : sans cours saisi, S1 est valorisé au PRU : 9 × 103,5333… + 200 de liquidités.
  await expect(page.getByRole('row', { name: /PEA Test/ })).toContainText('1 131,80 €');
});

test('UC-10 : cours de S1 au 22/09/2026', async () => {
  await page.getByRole('link', { name: 'Cours' }).click();
  const row = page.getByRole('row', { name: /ETF Monde/ });
  await row.getByLabel('Date du cours de ETF Monde').fill('2026-09-22');
  await row.getByLabel('Cours de ETF Monde', { exact: true }).fill('115,00');
  await row.getByRole('button', { name: 'Enregistrer' }).click();
  await expect(row).toContainText('22/09/2026');
  await expect(row).toContainText('115,00 €');
});

test('UC-11, TC-FUNC-01 : accueil au centime et montants en euros', async () => {
  await page.getByRole('link', { name: 'Accueil' }).click();
  const tiles = page.locator('.tile');
  await expect(tiles.nth(0)).toContainText('6 235,00 €');
  await expect(tiles.nth(3)).toContainText('+103,20 €');
  await expect(tiles.nth(3)).toContainText("+11,08 % du coût d'acquisition");
  await expect(page.getByLabel('Répartition par type de compte')).toContainText('LIVRET');
  await expect(page.getByLabel('Répartition par type de compte')).toContainText('80,19 %');

  // RG-01 : aucun champ devise dans les formulaires de mouvement, de solde et de cours.
  await page.goto('/movements?new=1');
  const dialog = page.getByRole('dialog');
  await expect(dialog.getByLabel('Prix unitaire')).toBeVisible();
  await expect(dialog.getByLabel(/devise/i)).toHaveCount(0);
  await dialog.getByRole('button', { name: 'Annuler' }).click();

  await page.goto('/accounts');
  await page.getByRole('row', { name: /PEA Test/ }).getByRole('button', { name: 'Historique des soldes' }).click();
  await expect(page.locator('app-side-panel').getByLabel('Montant')).toBeVisible();
  await expect(page.getByLabel(/devise/i)).toHaveCount(0);

  await page.goto('/prices');
  await expect(page.getByLabel('Cours de ETF Monde', { exact: true })).toBeVisible();
  await expect(page.getByLabel(/devise/i)).toHaveCount(0);
});

test('UC-13, TC-FUNC-24 : tableau de bord et exclusion des liquidités', async () => {
  await page.goto('/dashboard');
  const position = page.getByRole('row', { name: /ETF Monde/ });
  await expect(position).toContainText('103,53 €');
  await expect(position).toContainText('1 035,00 €');
  await expect(position).toContainText('+103,20 €');
  await expect(position).toContainText('+11,08 %');
  await expect(page.getByRole('row', { name: /^Total/ })).toContainText('6 235,00 €');
  await expect(page.getByRole('row', { name: /^Total/ })).toContainText('+97,30 €');
  await expect(page.getByText('Espèces et liquidités incluses')).toBeVisible();

  await page.getByRole('button', { name: /Zones/ }).click();
  await page.getByLabel('Monde').check();
  await page.keyboard.press('Escape');
  await expect(page.getByText('Espèces et liquidités exclues (filtre de support actif)')).toBeVisible();
  await expect(page.getByLabel('Par zone')).toContainText('100,00 %');
});

test('RG-17 : déconnexion et accès protégé', async () => {
  await page.getByRole('button', { name: 'Déconnexion' }).click();
  await expect(page).toHaveURL(/\/login$/);

  await page.goto('/accounts');
  await expect(page).toHaveURL(/\/login$/);
});
