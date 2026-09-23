import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { map, Observable, tap } from 'rxjs';

import { AuthStatus, LoginResponse, Session, SetupRequest } from './auth.models';

export const SESSION_KEY = 'financereport.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly session = signal<Session | null>(readSession());

  readonly username = computed(() => this.session()?.username ?? null);

  /** Jeton courant, ou null s'il est absent ou expiré. */
  token(): string | null {
    const session = this.session();
    return session && new Date(session.expiresAt).getTime() > Date.now() ? session.token : null;
  }

  isAuthenticated(): boolean {
    return this.token() !== null;
  }

  status(): Observable<AuthStatus> {
    return this.http.get<AuthStatus>('/api/auth/status');
  }

  setup(request: SetupRequest): Observable<void> {
    return this.http.post<unknown>('/api/auth/setup', request).pipe(map(() => undefined));
  }

  login(username: string, password: string): Observable<void> {
    return this.http.post<LoginResponse>('/api/auth/login', { username, password }).pipe(
      tap((response) => this.store({ username, token: response.token, expiresAt: response.expiresAt })),
      map(() => undefined),
    );
  }

  /** Efface la session et renvoie vers l'écran de connexion. */
  logout(): void {
    this.store(null);
    void this.router.navigate(['/login']);
  }

  private store(session: Session | null): void {
    this.session.set(session);
    try {
      if (session) {
        sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
      } else {
        sessionStorage.removeItem(SESSION_KEY);
      }
    } catch {
      // Stockage indisponible (navigation privée stricte) : la session reste en mémoire.
    }
  }
}

function readSession(): Session | null {
  try {
    const raw = sessionStorage.getItem(SESSION_KEY);
    return raw ? (JSON.parse(raw) as Session) : null;
  } catch {
    return null;
  }
}
