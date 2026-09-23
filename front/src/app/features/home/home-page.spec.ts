import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AreaChartModule } from '@swimlane/ngx-charts';

import { Summary } from '../../core/api/dashboard.api';
import { DonutChart } from '../../shared/charts/donut-chart';
import { HomePage } from './home-page';

const plain = (text: string | null | undefined) => (text ?? '').replace(/\s+/g, ' ').trim();

describe('HomePage', () => {
  const summary: Summary = {
    date: '2026-09-23',
    totalNetWorth: 6235,
    monthVariation: { amount: 235, percent: 3.92, referenceDate: '2026-08-31' },
    unrealizedGain: { amount: 103.2, percent: 11.08 },
    byAccountType: [
      { type: 'LIVRET', amount: 5000, percent: 80.19 },
      { type: 'PEA', amount: 1235, percent: 19.81 },
    ],
    missingPriceCount: 0,
  };

  async function render(data: Summary, points: { date: string; totalNetWorth: number }[]) {
    TestBed.configureTestingModule({
      imports: [HomePage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    // Les graphiques SVG ne sont pas rendus par jsdom : on vérifie les données et les messages.
    TestBed.overrideComponent(HomePage, { remove: { imports: [AreaChartModule, DonutChart] }, add: { schemas: [NO_ERRORS_SCHEMA] } });
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(HomePage);
    await fixture.whenStable();
    http.expectOne('/api/dashboard/summary').flush(data);
    http.expectOne((r) => r.url === '/api/dashboard/history').flush({ period: '1A', points });
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  const tiles = (element: HTMLElement) =>
    [...element.querySelectorAll('.tile')].map((t) => [...t.children].map((c) => plain(c.textContent)).join(' '));

  it('affiche les 4 indicateurs avec leur signe et leur couleur', async () => {
    const element = await render(summary, []);

    expect(tiles(element)).toEqual([
      'Patrimoine total 6 235,00 € Comptes non archivés, au 23/09/2026',
      'Variation du mois +235,00 € Depuis le 31/08/2026',
      'Variation du mois (%) +3,92 % Par rapport au 31/08/2026',
      "Plus-value latente +103,20 € +11,08 % du coût d'acquisition",
    ]);
    expect(element.querySelector('.tile .gain')).not.toBeNull();
  });

  it('affiche N/A quand la variation du mois est incalculable (RG-15)', async () => {
    const element = await render({ ...summary, monthVariation: null }, []);

    expect(tiles(element)[1]).toContain('N/A');
    expect(tiles(element)[2]).toContain('N/A');
  });

  it("signale un historique insuffisant sous 2 points (UC-12)", async () => {
    const element = await render(summary, [{ date: '2026-09-23', totalNetWorth: 6235 }]);

    expect(element.textContent).toContain('Historique insuffisant pour tracer une courbe.');
  });

  it('affiche le bandeau des positions sans cours (UC-11)', async () => {
    const element = await render({ ...summary, missingPriceCount: 1 }, []);

    expect(plain(element.querySelector('.banner')?.textContent)).toContain('1 position(s) sans cours, valorisée(s) au PRU.');
  });

  it('invite à créer un premier compte sans données', async () => {
    const element = await render({ ...summary, totalNetWorth: 0, monthVariation: null, byAccountType: [] }, []);

    expect(element.textContent).toContain('Commencez par créer votre premier compte.');
  });
});
