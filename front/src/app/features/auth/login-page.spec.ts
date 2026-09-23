import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { LoginPage } from './login-page';

describe('LoginPage', () => {
  let http: HttpTestingController;

  async function render() {
    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(LoginPage);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    const type = (id: string, value: string) => {
      const input = element.querySelector<HTMLInputElement>(`#${id}`)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    };
    const submit = async () => {
      element.querySelector('form')!.dispatchEvent(new Event('submit'));
      await fixture.whenStable();
    };
    return { fixture, element, type, submit };
  }

  afterEach(() => {
    vi.useRealTimers();
    sessionStorage.clear();
  });

  it('masque le mot de passe', async () => {
    const { element } = await render();

    expect(element.querySelector<HTMLInputElement>('#password')!.type).toBe('password');
  });

  it("affiche le message de l'API sur des identifiants refusés", async () => {
    const { fixture, element, type, submit } = await render();
    type('username', 'guillaume');
    type('password', 'mauvais');

    await submit();
    http.expectOne('/api/auth/login').flush(
      { error: 'UNAUTHORIZED', message: 'Identifiant ou mot de passe incorrect' },
      { status: 401, statusText: 'Unauthorized' },
    );
    await fixture.whenStable();

    expect(element.querySelector('[role="status"]')!.textContent).toContain('Identifiant ou mot de passe incorrect');
  });

  it('affiche un compte à rebours pendant le blocage', async () => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    const { fixture, element, type, submit } = await render();
    type('username', 'guillaume');
    type('password', 'motdepasse');

    await submit();
    http.expectOne('/api/auth/login').flush(
      { error: 'ACCOUNT_LOCKED', message: 'Connexion bloquée', details: { retryAfterSeconds: 900 } },
      { status: 423, statusText: 'Locked' },
    );
    await fixture.whenStable();
    expect(element.querySelector('[role="status"]')!.textContent).toContain('15 min 00 s');
    expect(element.querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBe(true);

    vi.advanceTimersByTime(61_000);
    await fixture.whenStable();
    expect(element.querySelector('[role="status"]')!.textContent).toContain('13 min 59 s');
  });

  it("ouvre l'accueil après une connexion réussie", async () => {
    const { fixture, type, submit } = await render();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    type('username', 'guillaume');
    type('password', 'motdepasse-solide');

    await submit();
    http.expectOne('/api/auth/login').flush({ token: 'jeton', expiresAt: new Date(Date.now() + 3600_000).toISOString() });
    await fixture.whenStable();

    expect(navigate).toHaveBeenCalledWith(['/']);
  });
});
