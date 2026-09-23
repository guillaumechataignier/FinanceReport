import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { AuthService, SESSION_KEY } from './auth.service';

describe('AuthService', () => {
  let http: HttpTestingController;

  function create(): AuthService {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    return TestBed.inject(AuthService);
  }

  afterEach(() => sessionStorage.clear());

  it('conserve la session dans le sessionStorage après connexion', () => {
    const auth = create();
    const expiresAt = new Date(Date.now() + 8 * 3600_000).toISOString();

    auth.login('guillaume', 'secret').subscribe();
    http.expectOne('/api/auth/login').flush({ token: 'jeton', expiresAt });

    expect(auth.token()).toBe('jeton');
    expect(auth.username()).toBe('guillaume');
    expect(JSON.parse(sessionStorage.getItem(SESSION_KEY)!)).toEqual({ username: 'guillaume', token: 'jeton', expiresAt });
  });

  it("ignore un jeton expiré", () => {
    sessionStorage.setItem(
      SESSION_KEY,
      JSON.stringify({ username: 'guillaume', token: 'ancien', expiresAt: new Date(Date.now() - 1000).toISOString() }),
    );

    const auth = create();

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.token()).toBeNull();
  });

  it('efface la session et renvoie vers la connexion à la déconnexion', () => {
    sessionStorage.setItem(
      SESSION_KEY,
      JSON.stringify({ username: 'guillaume', token: 'jeton', expiresAt: new Date(Date.now() + 60_000).toISOString() }),
    );
    const auth = create();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    auth.logout();

    expect(auth.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem(SESSION_KEY)).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });
});
