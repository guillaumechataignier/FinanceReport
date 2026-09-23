import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { firstValueFrom, isObservable, Observable, of } from 'rxjs';

import { authGuard, loginGuard, setupGuard } from './auth.guards';
import { AuthService } from './auth.service';

describe('gardes de navigation', () => {
  const auth = { isAuthenticated: vi.fn<() => boolean>(), status: vi.fn() };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), { provide: AuthService, useValue: auth }] });
  });

  async function run(guard: typeof authGuard): Promise<boolean | string> {
    const result = TestBed.runInInjectionContext(() => guard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));
    const value = isObservable(result) ? await firstValueFrom(result as Observable<boolean | UrlTree>) : result;
    return value instanceof UrlTree ? TestBed.inject(Router).serializeUrl(value) : (value as boolean);
  }

  it("protège l'application", async () => {
    auth.isAuthenticated.mockReturnValue(false);
    expect(await run(authGuard)).toBe('/login');

    auth.isAuthenticated.mockReturnValue(true);
    expect(await run(authGuard)).toBe(true);
  });

  it("oriente vers la création du compte tant que l'application n'est pas initialisée", async () => {
    auth.isAuthenticated.mockReturnValue(false);
    auth.status.mockReturnValue(of({ initialized: false }));
    expect(await run(loginGuard)).toBe('/setup');
    expect(await run(setupGuard)).toBe(true);

    auth.status.mockReturnValue(of({ initialized: true }));
    expect(await run(loginGuard)).toBe(true);
    expect(await run(setupGuard)).toBe('/login');
  });

  it("renvoie vers l'accueil un utilisateur déjà connecté", async () => {
    auth.isAuthenticated.mockReturnValue(true);
    expect(await run(loginGuard)).toBe('/');
  });
});
