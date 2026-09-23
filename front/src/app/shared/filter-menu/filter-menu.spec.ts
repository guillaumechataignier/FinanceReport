import { TestBed } from '@angular/core/testing';

import { FilterMenu } from './filter-menu';

describe('FilterMenu', () => {
  async function render(selected: string[]) {
    TestBed.configureTestingModule({ imports: [FilterMenu] });
    const fixture = TestBed.createComponent(FilterMenu);
    fixture.componentRef.setInput('label', 'Zones');
    fixture.componentRef.setInput('allLabel', 'Toutes');
    fixture.componentRef.setInput('options', [
      { value: 'EUROPE', label: 'Europe' },
      { value: 'MONDE', label: 'Monde' },
    ]);
    fixture.componentRef.setInput('selected', selected);
    await fixture.whenStable();
    return (fixture.nativeElement as HTMLElement).querySelector('button.filter')!.textContent!.replace(/\s+/g, ' ').trim();
  }

  it('résume la sélection', async () => {
    expect(await render([])).toBe('Zones : Toutes');
  });

  it('nomme une valeur unique', async () => {
    expect(await render(['MONDE'])).toBe('Zones : Monde');
  });

  it('compte plusieurs valeurs', async () => {
    expect(await render(['MONDE', 'EUROPE'])).toBe('Zones : 2 sélectionnés');
  });
});
