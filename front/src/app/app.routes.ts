import { CanMatchFn, Routes } from '@angular/router';

import { authGuard, loginGuard, setupGuard } from './core/auth/auth.guards';
import { Shell } from './core/layout/shell';
import { LoginPage } from './features/auth/login-page';
import { SetupPage } from './features/auth/setup-page';
import { PlaceholderPage } from './shared/placeholder-page/placeholder-page';

const referentialKind: CanMatchFn = (_route, segments) => ['zones', 'sectors', 'institutions'].includes(segments[1]?.path);

/** Les écrans métier sont chargés à la demande ; les écrans d'accès font partie du bundle initial. */
export const routes: Routes = [
  { path: 'setup', component: SetupPage, canActivate: [setupGuard], title: 'Création du compte · FinanceReport' },
  { path: 'login', component: LoginPage, canActivate: [loginGuard], title: 'Connexion · FinanceReport' },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', component: PlaceholderPage, data: { title: 'Accueil' }, title: 'Accueil · FinanceReport' },
      { path: 'dashboard', component: PlaceholderPage, data: { title: 'Tableau de bord' }, title: 'Tableau de bord · FinanceReport' },
      {
        path: 'movements',
        loadComponent: () => import('./features/movements/movements-page').then((m) => m.MovementsPage),
        title: 'Mouvements · FinanceReport',
      },
      {
        path: 'accounts',
        loadComponent: () => import('./features/accounts/accounts-page').then((m) => m.AccountsPage),
        title: 'Comptes · FinanceReport',
      },
      {
        path: 'securities',
        loadComponent: () => import('./features/securities/securities-page').then((m) => m.SecuritiesPage),
        title: 'Supports · FinanceReport',
      },
      {
        path: 'prices',
        loadComponent: () => import('./features/prices/prices-page').then((m) => m.PricesPage),
        title: 'Cours · FinanceReport',
      },
      { path: 'referentials', pathMatch: 'full', redirectTo: 'referentials/zones' },
      {
        path: 'referentials/:kind',
        canMatch: [referentialKind],
        loadComponent: () => import('./features/referentials/referentials-page').then((m) => m.ReferentialsPage),
        title: 'Référentiels · FinanceReport',
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
