import { Routes } from '@angular/router';

import { authGuard, loginGuard, setupGuard } from './core/auth/auth.guards';
import { Shell } from './core/layout/shell';
import { LoginPage } from './features/auth/login-page';
import { SetupPage } from './features/auth/setup-page';
import { PlaceholderPage } from './shared/placeholder-page/placeholder-page';

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
      { path: 'movements', component: PlaceholderPage, data: { title: 'Mouvements' }, title: 'Mouvements · FinanceReport' },
      { path: 'accounts', component: PlaceholderPage, data: { title: 'Comptes' }, title: 'Comptes · FinanceReport' },
      { path: 'securities', component: PlaceholderPage, data: { title: 'Supports' }, title: 'Supports · FinanceReport' },
      { path: 'prices', component: PlaceholderPage, data: { title: 'Cours' }, title: 'Cours · FinanceReport' },
      { path: 'referentials', component: PlaceholderPage, data: { title: 'Référentiels' }, title: 'Référentiels · FinanceReport' },
    ],
  },
  { path: '**', redirectTo: '' },
];
