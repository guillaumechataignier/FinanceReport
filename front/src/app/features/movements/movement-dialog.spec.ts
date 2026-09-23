import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

import { Account, Security } from '../../core/api/models';
import { MovementDialog, MovementDialogData } from './movement-dialog';

const account = (id: string, type: Account['type'], archived = false) =>
  ({ id, name: id, type, archived }) as Account;
const security = (id: string, type: Security['type'], archived = false) =>
  ({ id, name: id, code: id.toUpperCase(), type, archived }) as Security;

describe('MovementDialog', () => {
  let http: HttpTestingController;
  const close = vi.fn();

  async function render(data: MovementDialogData) {
    close.mockReset();
    TestBed.configureTestingModule({
      imports: [MovementDialog],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: { close } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(MovementDialog);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    const set = async (selector: string, value: string) => {
      const el = element.querySelector<HTMLInputElement | HTMLSelectElement>(selector)!;
      el.value = value;
      el.dispatchEvent(new Event(el instanceof HTMLSelectElement ? 'change' : 'input'));
      await fixture.whenStable();
    };
    const submit = async () => {
      element.querySelector('form')!.dispatchEvent(new Event('submit'));
      await fixture.whenStable();
    };
    return { fixture, element, set, submit };
  }

  const data: MovementDialogData = {
    accounts: [account('pea', 'PEA'), account('livret', 'LIVRET'), account('ancien', 'CTO', true)],
    securities: [security('etf', 'ETF'), security('btc', 'CRYPTO'), security('old', 'ACTION', true)],
  };

  it('ne propose que les comptes titres et les supports actifs (RG-12, RG-21, RG-27)', async () => {
    const { element } = await render(data);

    const accounts = [...element.querySelectorAll('#movement-account option')].map((o) => o.getAttribute('value'));
    const securities = [...element.querySelectorAll('#movement-security option')].map((o) => o.getAttribute('value'));
    expect(accounts).toEqual(['', 'pea']);
    expect(securities).toEqual(['', 'etf', 'btc']);
  });

  it("envoie un achat avec les décimales saisies à la française", async () => {
    const { set, submit } = await render(data);
    await set('#movement-date', '2026-09-15');
    await set('#movement-account', 'pea');
    await set('#movement-security', 'etf');
    await set('#movement-quantity', '10,5');
    await set('#movement-price', '102,40');
    await set('#movement-fees', '1,99');

    await submit();
    const request = http.expectOne('/api/movements');
    expect(request.request.body).toEqual({
      type: 'ACHAT',
      date: '2026-09-15',
      accountId: 'pea',
      securityId: 'etf',
      quantity: 10.5,
      unitPrice: 102.4,
      fees: 1.99,
    });
    request.flush({});
    expect(close).toHaveBeenCalledWith(true);
  });

  it("n'envoie que le montant pour un versement", async () => {
    const { element, set, submit, fixture } = await render(data);
    [...element.querySelectorAll<HTMLButtonElement>('.segmented button')].find((b) => b.textContent?.trim() === 'Versement')!.click();
    await fixture.whenStable();
    expect(element.querySelector('#movement-security')).toBeNull();

    await set('#movement-date', '2026-09-20');
    await set('#movement-account', 'pea');
    await set('#movement-amount', '1 000,00');
    await submit();

    expect(http.expectOne('/api/movements').request.body).toEqual({
      type: 'VERSEMENT',
      date: '2026-09-20',
      accountId: 'pea',
      amount: 1000,
    });
  });

  it('bloque une saisie non numérique sans appeler l\'API', async () => {
    const { element, set, submit } = await render(data);
    await set('#movement-quantity', 'dix');

    await submit();

    http.expectNone('/api/movements');
    expect(element.textContent).toContain("La quantité n'est pas un nombre valide.");
  });
});
