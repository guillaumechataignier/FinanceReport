import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { AuthService } from './auth.service';

/**
 * Ajoute le jeton aux appels de l'API et renvoie vers la connexion sur un 401 (jeton expiré ou invalide, RG-17).
 * Les 401 des endpoints d'authentification (identifiants refusés) sont laissés à l'écran appelant.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = auth.token();
  const isApi = request.url.startsWith('/api/');
  const authorized = token && isApi ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;

  return next(authorized).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && isApi && !request.url.startsWith('/api/auth/')) {
        auth.logout();
      }

      return throwError(() => error);
    }),
  );
};
