import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ReferentialItem } from '../../core/api/models';
import { ReferentialsPage } from './referentials-page';

describe('ReferentialsPage', () => {
  const zones: ReferentialItem[] = [
    { id: 'eu', code: 'EUROPE', label: 'Europe', archived: false, usageCount: 0 },
    { id: 'mo', code: 'MONDE', label: 'Monde', archived: false, usageCount: 2 },
  ];

  async function render() {
    TestBed.configureTestingModule({
      imports: [ReferentialsPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(ReferentialsPage);
    fixture.componentRef.setInput('kind', 'zones');
    await fixture.whenStable();
    http.expectOne((r) => r.url === '/api/referentials/zones').flush(zones);
    http.expectOne((r) => r.url === '/api/referentials/sectors').flush([]);
    http.expectOne((r) => r.url === '/api/referentials/institutions').flush([]);
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement, http };
  }

  const row = (element: HTMLElement, label: string) =>
    [...element.querySelectorAll('tbody tr')].find((r) => r.textContent?.includes(label))!;

  it("ne propose la suppression que pour une valeur non utilisée (RG-30)", async () => {
    const { element } = await render();

    expect(row(element, 'Europe').querySelector('[aria-label="Supprimer"]')).not.toBeNull();
    expect(row(element, 'Monde').querySelector('[aria-label="Supprimer"]')).toBeNull();
    expect(row(element, 'Monde').textContent).toContain('2 supports');
  });

  it('verrouille le code d\'une valeur utilisée', async () => {
    const { element, fixture } = await render();

    row(element, 'Monde').querySelector<HTMLButtonElement>('[aria-label="Modifier"]')!.click();
    await fixture.whenStable();

    expect(element.querySelector<HTMLInputElement>('#ref-code')!.disabled).toBe(true);
    expect(element.textContent).toContain('Non modifiable : utilisé par 2 supports.');
  });

  it("envoie le libellé et le code d'une nouvelle zone", async () => {
    const { element, fixture, http } = await render();
    [...element.querySelectorAll<HTMLButtonElement>('app-page-header button')].at(-1)!.click();
    await fixture.whenStable();
    const set = (id: string, value: string) => {
      const input = element.querySelector<HTMLInputElement>(id)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    };
    set('#ref-label', ' Afrique ');
    set('#ref-code', 'AFRIQUE');

    element.querySelector('app-side-panel form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    expect(http.expectOne({ method: 'POST', url: '/api/referentials/zones' }).request.body).toEqual({ label: 'Afrique', code: 'AFRIQUE' });
  });
});
