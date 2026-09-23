import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  const auth = { token: vi.fn<() => string | null>(), logout: vi.fn() };

  beforeEach(() => {
    auth.token.mockReturnValue('jeton');
    auth.logout.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  it("ajoute le jeton aux appels de l'API", () => {
    http.get('/api/accounts').subscribe();

    expect(backend.expectOne('/api/accounts').request.headers.get('Authorization')).toBe('Bearer jeton');
  });

  it('renvoie vers la connexion sur un 401 métier', () => {
    http.get('/api/accounts').subscribe({ error: () => undefined });
    backend.expectOne('/api/accounts').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logout).toHaveBeenCalled();
  });

  it("laisse les identifiants refusés à l'écran de connexion", () => {
    http.post('/api/auth/login', {}).subscribe({ error: () => undefined });
    backend.expectOne('/api/auth/login').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logout).not.toHaveBeenCalled();
  });
});
