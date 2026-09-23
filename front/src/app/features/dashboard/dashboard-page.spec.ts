import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { DashboardPosition, DashboardPositions } from '../../core/api/dashboard.api';
import { DonutChart } from '../../shared/charts/donut-chart';
import { DashboardPage } from './dashboard-page';

const position = (name: string, marketValue: number, missingPrice = false): DashboardPosition => ({
  accountId: 'a',
  accountName: 'PEA Test',
  securityId: name,
  securityName: name,
  securityCode: name.toUpperCase(),
  securityType: 'ETF',
  zone: 'MONDE',
  zoneLabel: 'Monde',
  sector: 'DIVERSIFIE',
  sectorLabel: 'Diversifié',
  quantity: 1,
  averageCost: marketValue,
  price: marketValue,
  priceDate: missingPrice ? null : '2026-09-22',
  missingPrice,
  marketValue,
  unrealizedGain: 0,
  unrealizedGainPercent: 0,
});

describe('DashboardPage', () => {
  let http: HttpTestingController;

  const data = (positions: DashboardPosition[], includesCash = true): DashboardPositions => ({
    positions,
    allocation: { byAccount: [], bySecurityType: [], byZone: [], bySector: [] },
    includesCash,
  });

  async function render() {
    TestBed.configureTestingModule({ imports: [DashboardPage], providers: [provideHttpClient(), provideHttpClientTesting()] });
    TestBed.overrideComponent(DashboardPage, { remove: { imports: [DonutChart] }, add: { schemas: [NO_ERRORS_SCHEMA] } });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(DashboardPage);
    await fixture.whenStable();
    http.expectOne((r) => r.url === '/api/accounts').flush([]);
    http.expectOne((r) => r.url === '/api/referentials/zones').flush([
      { id: 'z', code: 'MONDE', label: 'Monde', archived: true, usageCount: 1 },
    ]);
    http.expectOne((r) => r.url === '/api/referentials/sectors').flush([]);
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  function answer(positions: DashboardPositions) {
    const request = http.expectOne((r) => r.url === '/api/dashboard/positions');
    request.flush(positions);
    http.expectOne((r) => r.url === '/api/dashboard/accounts').flush({ accounts: [], total: { value: 0, unrealizedGain: 0, realizedGain: 0 } });
    return request.request;
  }

  it('trie les positions par valorisation décroissante, puis sur la colonne choisie', async () => {
    const { fixture, element } = await render();
    answer(data([position('Alpha', 100), position('Beta', 300)]));
    await fixture.whenStable();
    const names = () => [...element.querySelectorAll('tbody tr td:nth-child(2) .strong')].map((e) => e.textContent?.trim());
    expect(names()).toEqual(['Beta', 'Alpha']);

    [...element.querySelectorAll<HTMLButtonElement>('th button.sort')].find((b) => b.textContent?.trim() === 'Support')!.click();
    await fixture.whenStable();

    expect(names()).toEqual(['Alpha', 'Beta']);
  });

  it("transmet les filtres à l'API et signale l'exclusion des liquidités (RG-28)", async () => {
    const { fixture, element } = await render();
    answer(data([]));
    await fixture.whenStable();

    fixture.componentInstance['zones'].set(['MONDE']);
    await fixture.whenStable();
    const request = answer(data([position('Alpha', 100)], false));
    await fixture.whenStable();

    expect(request.params.get('zones')).toBe('MONDE');
    expect(element.querySelector('.cash-note')?.textContent).toContain('exclues');
  });

  it('propose les zones archivées dans les filtres (FS §3.10)', async () => {
    const { fixture } = await render();
    answer(data([]));
    await fixture.whenStable();

    expect(fixture.componentInstance['zoneOptions']()).toEqual([{ value: 'MONDE', label: 'Monde (archivé)' }]);
  });

  it('signale une position sans cours', async () => {
    const { fixture, element } = await render();
    answer(data([position('Bitcoin', 505, true)]));
    await fixture.whenStable();

    expect(element.querySelector('[aria-label="Cours manquant, valorisé au PRU"]')).not.toBeNull();
  });
});
