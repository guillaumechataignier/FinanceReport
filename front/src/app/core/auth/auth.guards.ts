import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { AuthService } from './auth.service';

/** Pages de l'application : session valide exigée. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() ? true : inject(Router).createUrlTree(['/login']);
};

/** Écran de connexion : renvoie vers l'accueil si connecté, vers la création du compte si l'application n'est pas initialisée. */
export const loginGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) {
    return router.createUrlTree(['/']);
  }

  return auth.status().pipe(
    map((status) => (status.initialized ? true : router.createUrlTree(['/setup']))),
    catchError(() => of(true)),
  );
};

/** Écran de création du compte : accessible uniquement tant que l'application n'est pas initialisée (UC-01). */
export const setupGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.status().pipe(
    map((status) => (status.initialized ? router.createUrlTree(['/login']) : true)),
    catchError(() => of(true)),
  );
};
