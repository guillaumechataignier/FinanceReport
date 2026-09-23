import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { SetupPage } from './setup-page';

describe('SetupPage', () => {
  let http: HttpTestingController;

  async function render() {
    TestBed.configureTestingModule({
      imports: [SetupPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(SetupPage);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    const type = async (id: string, value: string) => {
      const input = element.querySelector<HTMLInputElement>(`#${id}`)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };
    const metRules = () => element.querySelectorAll('.rules li.met').length;
    return { fixture, element, type, metRules };
  }

  it('coche les règles au fil de la saisie', async () => {
    const { type, metRules } = await render();
    expect(metRules()).toBe(0);

    await type('username', 'guillaume');
    await type('password', 'motdepasse-2026');
    expect(metRules()).toBe(2);

    await type('passwordConfirmation', 'motdepasse-2026');
    expect(metRules()).toBe(3);
  });

  it("n'appelle pas l'API tant qu'une règle n'est pas respectée", async () => {
    const { fixture, element, type } = await render();
    await type('username', 'guillaume');
    await type('password', 'court');

    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    http.expectNone('/api/auth/setup');
    expect(element.querySelector('[role="status"]')!.textContent).toContain('règles');
  });

  it('renvoie vers la connexion après la création', async () => {
    const { fixture, element, type } = await render();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    await type('username', 'guillaume');
    await type('password', 'motdepasse-2026');
    await type('passwordConfirmation', 'motdepasse-2026');

    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    const request = http.expectOne('/api/auth/setup');
    expect(request.request.body).toEqual({ username: 'guillaume', password: 'motdepasse-2026', passwordConfirmation: 'motdepasse-2026' });
    request.flush({ username: 'guillaume' }, { status: 201, statusText: 'Created' });
    await fixture.whenStable();

    expect(navigate).toHaveBeenCalledWith(['/login'], { state: { created: true } });
  });
});
